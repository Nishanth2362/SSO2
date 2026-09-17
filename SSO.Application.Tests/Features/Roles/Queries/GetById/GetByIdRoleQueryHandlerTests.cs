using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using SSO.Application.Features.Roles.Queries.GetById;
using SSO.Application.Responses.Features;
using SSO.Domain.Entities;
using SSO.Common.Wrapper;
using FluentAssertions;
using SSO.Application.Tests.Helpers;

namespace SSO.Application.Tests.Features.Roles.Queries.GetById
{
    [TestClass]
    public class GetByIdRoleQueryHandlerTests
    {
        private Mock<ILogger<GetByIdRoleQueryHandler>> _mockLogger;
        private Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private GetByIdRoleQueryHandler _handler;

        [TestInitialize]
        public void Initialize()
        {
            _mockLogger = new Mock<ILogger<GetByIdRoleQueryHandler>>();
            _mockRoleManager = MockHelpers.MockRoleManager<ApplicationRole>();
            
            _handler = new GetByIdRoleQueryHandler(
                _mockLogger.Object,
                _mockRoleManager.Object);
        }

        [TestMethod]
        public async Task Handle_RoleExists_ReturnsSuccessResult()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var role = new ApplicationRole
            {
                Id = roleId,
                Name = "Admin",
                Description = "Administrator role",
                TenantId = Guid.NewGuid(),
                IsSystemRole = true
            };

            _mockRoleManager.Setup(m => m.FindByIdAsync(roleId.ToString()))
                .ReturnsAsync(role);

            var query = new GetByIdRoleQuery { Id = roleId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be("Admin");
        }

        [TestMethod]
        public async Task Handle_RoleDoesNotExist_ReturnsFailResult()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _mockRoleManager.Setup(m => m.FindByIdAsync(roleId.ToString()))
                .ReturnsAsync((ApplicationRole)null);

            var query = new GetByIdRoleQuery { Id = roleId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Messages.Should().Contain("Role not found.");
        }

        [TestMethod]
        public async Task Handle_ExceptionOccurs_ReturnsFailResult()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _mockRoleManager.Setup(m => m.FindByIdAsync(roleId.ToString()))
                .ThrowsAsync(new Exception("Database error"));

            var query = new GetByIdRoleQuery { Id = roleId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Messages.Should().Contain("An error occurred while retrieving the role. Please try again later.");
        }
    }
}
