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
                ? await base.SaveChangesAsync(cancellationToken)
                : await base.SaveChangesAsync(_currentUserService.UserName, cancellationToken);
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
