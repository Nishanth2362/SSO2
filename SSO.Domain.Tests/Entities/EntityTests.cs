using SSO.Domain.Entities;
using FluentAssertions;

namespace SSO.Domain.Tests.Entities
{
    [TestClass]
    public class EntityTests
    {
        [TestMethod]
        public void CreateTenant_ShouldSetProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenant = new Tenants
            {
                Id = id,
                Code = "acme",
                Name = "Acme Corp",
                IsActive = true
            };

            // Assert
            tenant.Id.Should().Be(id);
            tenant.Code.Should().Be("acme");
            tenant.Name.Should().Be("Acme Corp");
            tenant.IsActive.Should().BeTrue();
        }

        [TestMethod]
        public void CreateApplicationUser_ShouldSetProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = id,
                UserName = "testuser",
                Email = "test@example.com",
                TenantId = tenantId,
                Name = "Test User"
            };

            // Assert
            user.Id.Should().Be(id);
            user.UserName.Should().Be("testuser");
            user.TenantId.Should().Be(tenantId);
        }

        [TestMethod]
        public void CreateSubscription_ShouldSetProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var subscription = new Subscriptions
            {
                Id = id,
                Name = "Monthly",
                Price = 100,
                IsActive = true
            };

            // Assert
            subscription.Id.Should().Be(id);
            subscription.Name.Should().Be("Monthly");
            subscription.Price.Should().Be(100);
        }
    }
}
