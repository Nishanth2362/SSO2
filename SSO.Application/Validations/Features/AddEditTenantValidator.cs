using SSO.Application.Features.Tenants.Commands.AddEdit;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Validations.Features
{
    public class AddEditTenantValidator : IRequestValidator<AddEditTenentCommand>
    {
        public async Task<IEnumerable<ValidationError>> ValidateAsync(AddEditTenentCommand request, CancellationToken ct)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(request.Name),
                    ErrorMessage = "Tenant Name is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(request.Code),
                    ErrorMessage = "Code is required"
                });
            }

            return errors;
        }
    }
}
