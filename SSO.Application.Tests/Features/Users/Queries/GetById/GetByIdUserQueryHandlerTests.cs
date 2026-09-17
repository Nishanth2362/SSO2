using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using SSO.Application.Features.Users.Queries.GetById;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Domain.Entities;
using SSO.Common.Wrapper;
using FluentAssertions;
using OpenIddict.Abstractions;

namespace SSO.Application.Tests.Features.Users.Queries.GetById
{
    [TestClass]
    public class GetByIdUserQueryHandlerTests
    {
        private Mock<IUnitOfWork<Guid>> _mockUnitOfWork;
        private Mock<ILogger<GetByIdUserQueryHandler>> _mockLogger;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<IOpenIddictAuthorizationManager> _mockAuthorizationManager;
        private GetByIdUserQueryHandler _handler;

        [TestInitialize]
        public void Initialize()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<Guid>>();
            _mockLogger = new Mock<ILogger<GetByIdUserQueryHandler>>();
            _mockUserManager = Helpers.MockHelpers.MockUserManager(new List<ApplicationUser>());
            _mockAuthorizationManager = new Mock<IOpenIddictAuthorizationManager>();
            
            _handler = new GetByIdUserQueryHandler(
                _mockUnitOfWork.Object,
                _mockUserManager.Object,
                _mockLogger.Object,
                _mockAuthorizationManager.Object);
        }

        [TestMethod]
        public async Task Handle_UserExists_ReturnsSuccessResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com",
                Name = "Test User",
                TenantId = Guid.NewGuid(),
                CreatedOn = DateTime.UtcNow,
                LastModifiedOn = DateTime.UtcNow,
                IsActive = true
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            
            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Admin" });

            _mockAuthorizationManager.Setup(m => m.FindAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Collections.Immutable.ImmutableArray<string>?>()))
                .Returns(TestAsyncEnumerable.Empty<object>()); // Mocking IAsyncEnumerable

            var query = new GetByIdUserQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.UserName.Should().Be(user.UserName);
            result.Data.Roles.Should().Contain("Admin");
        }

        [TestMethod]
        public async Task Handle_UserDoesNotExist_ReturnsFailResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser)null);

            var query = new GetByIdUserQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Messages.Should().Contain("User not found.");
        }

        [TestMethod]
        public async Task Handle_ExceptionOccurs_ReturnsFailResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
                .ThrowsAsync(new Exception("Database error"));

            var query = new GetByIdUserQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Messages.Should().Contain("An error occurred while retrieving the user.");
        }
    }

    public static class TestAsyncEnumerable
    {
        public static async IAsyncEnumerable<T> Empty<T>()
        {
            yield break;
        }
    }
}
