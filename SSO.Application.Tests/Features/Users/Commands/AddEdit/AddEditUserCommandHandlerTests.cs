using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using SSO.Application.Features.Users.Commands.AddEdit;
using SSO.Application.Interfaces.Services;
using SSO.Application.Interfaces.Services.Features;
using SSO.Domain.Entities;
using SSO.Common.Wrapper;
using FluentAssertions;
using OpenIddict.Abstractions;
using SSO.Application.Tests.Helpers;
using SSO.Application.Features.Roles.Commands.AddEdit;

namespace SSO.Application.Tests.Features.Users.Commands.AddEdit
{
    [TestClass]
    public class AddEditUserCommandHandlerTests
    {
        private Mock<ILogger<AddEditRolesCommandHandler>> _mockLogger;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<IMailService> _mockMailService;
        private Mock<IOpenIddictAuthorizationManager> _mockAuthorizationManager;
        private Mock<IRoleService> _mockRoleService;
        private Mock<IEmailTemplateService> _mockEmailTemplateService;
        private AddEditUserCommandHandler _handler;

        [TestInitialize]
        public void Initialize()
        {
            _mockLogger = new Mock<ILogger<AddEditRolesCommandHandler>>();
            _mockUserManager = MockHelpers.MockUserManager(new List<ApplicationUser>());
            _mockMailService = new Mock<IMailService>();
            _mockAuthorizationManager = new Mock<IOpenIddictAuthorizationManager>();
            _mockRoleService = new Mock<IRoleService>();
            _mockEmailTemplateService = new Mock<IEmailTemplateService>();

            _handler = new AddEditUserCommandHandler(
                _mockLogger.Object,
                _mockUserManager.Object,
                _mockMailService.Object,
                _mockAuthorizationManager.Object,
                _mockRoleService.Object,
                _mockEmailTemplateService.Object);
        }

        [TestMethod]
        public async Task Handle_CreateNewUser_Success()
        {
            // Arrange
            var command = new AddEditUserCommand
            {
                UserName = "newuser",
                Email = "new@example.com",
                Name = "New User",
                Password = "Password123!",
                ActivateUser = true,
                AutoConfirmEmail = true,
                TenantId = Guid.NewGuid()
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName))
                .ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(command.Email))
                .ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), command.Password), Times.Once);
        }

        [TestMethod]
        public async Task Handle_CreateExistingUserName_ReturnsFail()
        {
            // Arrange
            var command = new AddEditUserCommand
            {
                UserName = "existinguser",
                Email = "new@example.com"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(command.UserName))
                .ReturnsAsync(new ApplicationUser());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Messages.Should().Contain($"Username {command.UserName} is already taken.");
        }

        [TestMethod]
        public async Task Handle_UpdateExistingUser_Success()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new AddEditUserCommand
            {
                Id = userId,
                UserName = "updateduser",
                Email = "updated@example.com",
                Name = "Updated User",
                ActivateUser = true,
                TenantId = Guid.NewGuid()
            };

            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Messages.Should().Contain("User updated successfully.");
            _mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_UserNotFoundForUpdate_ReturnsFail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new AddEditUserCommand { Id = userId };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Messages.Should().Contain("User not found.");
        }
    }
}
