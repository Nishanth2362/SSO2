using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Contract;
using SSO.Domain.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static SSO.Common.Constants.Permission.Permissions;

namespace SSO.Infrastructure.Contexts
{
    public class ApplicationDbContext : AuditableContext, IDataProtectionKeyContext
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeService _dateTimeService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService = null, IDateTimeService dateTimeService = null)
            : base(options)
        {
            _currentUserService = currentUserService;
            _dateTimeService = dateTimeService;
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<Tenants> Tenants => Set<Tenants>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<Subscriptions> Subscriptions => Set<Subscriptions>();
        public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
        public DbSet<TenantLicense> TenantLicenses => Set<TenantLicense>();
        public DbSet<ApplicationClient> Clients => Set<ApplicationClient>();
        public DbSet<ApplicationScope> Scopes => Set<ApplicationScope>();
        public DbSet<ApplicationClientScope> ApplicationClientScopes => Set<ApplicationClientScope>();
        public DbSet<ApplicationScopePermission> ApplicationScopePermissions => Set<ApplicationScopePermission>();
        public DbSet<ApplicationAuthorization> ApplicationAuthorizations => Set<ApplicationAuthorization>();
        public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<TenantClient> TenantClients => Set<TenantClient>();

        /// <summary>Authentication security events — failed logins, lockouts, OTP failures, access denials.</summary>
        public DbSet<LoginSecurityEvent> LoginSecurityEvents => Set<LoginSecurityEvent>();

        /// <summary>Restricted Page Access Protocol transactions (single-use scoped ephemeral sessions).</summary>
        public DbSet<ManagementTransaction> ManagementTransactions => Set<ManagementTransaction>();

        /// <summary>Restricted Page Access Protocol configurable settings.</summary>
        public DbSet<RpapSetting> RpapSettings => Set<RpapSetting>();

        /// <summary>Token lifetime configuration settings (Access Token, Refresh Token, Authorization Code).</summary>
        public DbSet<TokenLifetimeSetting> TokenLifetimeSettings => Set<TokenLifetimeSetting>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
        {
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IAuditableEntity>? entry in ChangeTracker.Entries<IAuditableEntity>().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedOn = _dateTimeService.NowUtc;
                        entry.Entity.CreatedBy = _currentUserService.UserName;
                        entry.Entity.IPAddress = _currentUserService.IpAddress;
                        break;

                    case EntityState.Modified:
                        entry.Entity.LastModifiedOn = _dateTimeService.NowUtc;
                        entry.Entity.LastModifiedBy = _currentUserService.UserName;
                        entry.Entity.IPAddress = _currentUserService.IpAddress;
                        break;
                }
            }
            return string.IsNullOrEmpty(_currentUserService.UserName)
                ? await base.SaveChangesAsync(userId: null!, remarks: null, cancellationToken: cancellationToken)
                : await base.SaveChangesAsync(userId: _currentUserService.UserName, remarks: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// SaveChanges overload that carries a user-supplied remarks/description string into the audit trail.
        /// Call this from UnitOfWork.Commit(remarks: ...) for edit operations.
        /// </summary>
        public async Task<int> SaveChangesAsync(string? remarks, CancellationToken cancellationToken = new())
        {
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IAuditableEntity>? entry in ChangeTracker.Entries<IAuditableEntity>().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedOn = _dateTimeService.NowUtc;
                        entry.Entity.CreatedBy = _currentUserService.UserName;
                        entry.Entity.IPAddress = _currentUserService.IpAddress;
                        break;

                    case EntityState.Modified:
                        entry.Entity.LastModifiedOn = _dateTimeService.NowUtc;
                        entry.Entity.LastModifiedBy = _currentUserService.UserName;
                        entry.Entity.IPAddress = _currentUserService.IpAddress;
                        break;
                }
            }
            return string.IsNullOrEmpty(_currentUserService.UserName)
                ? await base.SaveChangesAsync(userId: null!, remarks: remarks, cancellationToken: cancellationToken)
                : await base.SaveChangesAsync(userId: _currentUserService.UserName, remarks: remarks, cancellationToken: cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // 🔹 REQUIRED – adds OpenIddict default entities
            builder.UseOpenIddict<ApplicationClient, ApplicationAuthorization, ApplicationScope, ApplicationToken, Guid>();
            // Apply ONLY for MySQL provider
            Console.WriteLine($"Provider =  {Database.ProviderName}");
            if (Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
            {
                ConfigureOpenIddictForMySql(builder);
            }
            builder.Entity<Domain.Entities.Tenants>()
                .HasIndex(x => x.Code)
                .IsUnique();
            builder.Entity<Domain.Entities.Tenants>()
               .HasIndex(x => x.Name)
               .IsUnique();
            builder.Entity<RolePermission>()
                .HasKey(x => new { x.RoleId, x.PermissionId, x.ApplicationClientId });
            builder.Entity<RolePermission>()
                .Property(x => x.RoleId).ValueGeneratedNever();
            builder.Entity<RolePermission>()
                .Property(x => x.PermissionId).ValueGeneratedNever();
            builder.Entity<RolePermission>()
                .Property(x => x.ApplicationClientId).ValueGeneratedNever();

            builder.Entity<ApplicationClientScope>()
                .HasKey(x => new { x.ClientId, x.ScopeId });
            builder.Entity<ApplicationClientScope>()
                .Property(x => x.ClientId).ValueGeneratedNever();
            builder.Entity<ApplicationClientScope>()
                .Property(x => x.ScopeId).ValueGeneratedNever();

            builder.Entity<ApplicationScopePermission>()
                .HasKey(x => new { x.ScopeId, x.PermissionId });
            builder.Entity<ApplicationScopePermission>()
                .Property(x => x.ScopeId).ValueGeneratedNever();
            builder.Entity<ApplicationScopePermission>()
                .Property(x => x.PermissionId).ValueGeneratedNever();

            builder.Entity<Invoice>(entity =>
            {
                entity.HasMany(i => i.Payments)
                    .WithOne(p => p.Invoice)
                    .HasForeignKey(p => p.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationUserRole>(entity =>
            {
                entity.HasOne(ur => ur.Tenant)
                    .WithMany()
                    .HasForeignKey(ur => ur.TenantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne<Tenants>()
                    .WithMany()
                    .HasForeignKey(u => u.TenantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<ApplicationRole>(entity =>
            {
                entity.HasOne(r => r.Tenant)
                    .WithMany()
                    .HasForeignKey(r => r.TenantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

           
            builder.Entity<TenantClient>()
                .HasOne(tc => tc.Tenant)
                .WithMany(t => t.TenantClients)
                .HasForeignKey(tc => tc.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TenantClient>()
                .HasOne(tc => tc.ApplicationClient)
                .WithMany(ac => ac.TenantClients)
                .HasForeignKey(tc => tc.ApplicationClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationAuthorization>(entity =>
            {
                entity.HasOne<Tenants>()
                    .WithMany()
                    .HasForeignKey(a => a.TenantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<ApplicationToken>(entity =>
            {
                entity.HasOne<Tenants>()
                    .WithMany()
                    .HasForeignKey(t => t.TenantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            builder.Entity<TenantLicense>(entity =>
            {
                entity.HasOne<Tenants>()
                    .WithMany()
                    .HasForeignKey(tl => tl.TenantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ─── LoginSecurityEvent ──────────────────────────────────────────────
            builder.Entity<LoginSecurityEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.IpAddress, e.OccurredAtUtc });
                entity.HasIndex(e => e.OccurredAtUtc);
                entity.Property(e => e.EventType).HasConversion<byte>();
                entity.Property(e => e.ClientId).HasMaxLength(100);
                entity.Property(e => e.UserName).HasMaxLength(256);
                entity.Property(e => e.IpAddress).HasMaxLength(45);   // supports IPv6
                entity.Property(e => e.CountryCode).HasMaxLength(5);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.DeviceType).HasMaxLength(20);
                entity.Property(e => e.BrowserName).HasMaxLength(100);
                entity.Property(e => e.OsName).HasMaxLength(100);
            });

            // ─── ManagementTransaction (RPAP) ──────────────────────────────────
            builder.Entity<ManagementTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.LaunchTokenHash);
                entity.HasIndex(e => e.ResultCodeHash);
                entity.HasIndex(e => new { e.TenantId, e.Status });
                entity.HasIndex(e => new { e.ClientId, e.Status });
                entity.HasIndex(e => e.ExpiresOn);

                entity.Property(e => e.ClientId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Scope).HasMaxLength(100).IsRequired();
                entity.Property(e => e.LaunchTokenHash).HasMaxLength(128).IsRequired();
                entity.Property(e => e.TargetUrl).HasMaxLength(500).IsRequired();
                entity.Property(e => e.CallbackUrl).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.State).HasMaxLength(500);
                entity.Property(e => e.ResultCodeHash).HasMaxLength(128);
                entity.Property(e => e.ConsumedIpAddress).HasMaxLength(45);
                entity.Property(e => e.ConsumedUserAgent).HasMaxLength(500);
                entity.Property(e => e.RevokedReason).HasMaxLength(500);
                entity.Property(e => e.RevokedBy).HasMaxLength(256);
            });

        }

        private void ConfigureOpenIddictForMySql(ModelBuilder builder)
        {
            // ================= Applications =================
            builder.Entity<ApplicationClient>(entity =>
            {
                entity.Property(x => x.ClientId).HasMaxLength(100);
                entity.Property(x => x.ClientSecret).HasMaxLength(200);

                entity.Property(x => x.ClientType).HasMaxLength(50);
                entity.Property(x => x.ConsentType).HasMaxLength(50);
                entity.Property(x => x.ApplicationType).HasMaxLength(50);

                entity.Property(x => x.DisplayName).HasMaxLength(200);

                // JSON / large fields → LONGTEXT
                entity.Property(x => x.DisplayNames).HasColumnType("longtext");
                entity.Property(x => x.Permissions).HasColumnType("longtext");
                entity.Property(x => x.PostLogoutRedirectUris).HasColumnType("longtext");
                entity.Property(x => x.RedirectUris).HasColumnType("longtext");
                entity.Property(x => x.Requirements).HasColumnType("longtext");
                entity.Property(x => x.Properties).HasColumnType("longtext");

                entity.Property(x => x.ConcurrencyToken).HasMaxLength(50);
            });

            // ================= Authorizations =================
            builder.Entity<ApplicationAuthorization>(entity =>
            {
                entity.Property(x => x.Status).HasMaxLength(50);

                // IMPORTANT: increase size
                entity.Property(x => x.Subject).HasMaxLength(200);

                // 🔥 MUST be large (OpenIddict uses long values)
                entity.Property(x => x.Type).HasMaxLength(200);

                // JSON fields
                entity.Property(x => x.Scopes).HasColumnType("longtext");
                entity.Property(x => x.Properties).HasColumnType("longtext");

                entity.Property(x => x.ConcurrencyToken).HasMaxLength(50);
            });

            // ================= Tokens =================
            builder.Entity<ApplicationToken>(entity =>
            {
                entity.Property(x => x.Status).HasMaxLength(50);

                // IMPORTANT
                entity.Property(x => x.Subject).HasMaxLength(200);

                // 🔥 CRITICAL FIX (your error source)
                entity.Property(x => x.Type).HasMaxLength(200);

                entity.Property(x => x.ReferenceId).HasMaxLength(100);

                // 🔥 MUST be LONGTEXT (JWT can exceed 4k easily)
                entity.Property(x => x.Payload).HasColumnType("longtext");

                // JSON metadata
                entity.Property(x => x.Properties).HasColumnType("longtext");

                entity.Property(x => x.ConcurrencyToken).HasMaxLength(50);

                // Optional custom fields (SAFE)
                entity.Property(x => x.DeviceInfo).HasColumnType("longtext").IsRequired(false);
            });

            // ================= Scopes =================
            builder.Entity<ApplicationScope>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(100);
                entity.Property(x => x.DisplayName).HasMaxLength(200);

                // JSON / localization
                entity.Property(x => x.DisplayNames).HasColumnType("longtext");

                entity.Property(x => x.Description).HasMaxLength(500);
                entity.Property(x => x.Descriptions).HasColumnType("longtext");

                // 🔥 IMPORTANT (resource mapping)
                entity.Property(x => x.Resources).HasColumnType("longtext");

                entity.Property(x => x.Properties).HasColumnType("longtext");

                entity.Property(x => x.ConcurrencyToken).HasMaxLength(50);

                // Custom
                entity.Property(x => x.ScopeType).HasMaxLength(50);
            });
        }
    }
}
