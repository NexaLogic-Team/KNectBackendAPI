using FluentValidation;
using Knect.Application.Profiles.DTOs;

namespace Knect.Application.Profiles.Validators;

public sealed class EmployerProfileValidator : AbstractValidator<UpdateEmployerProfileRequest>
{
    public EmployerProfileValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.CompanyDescription)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.CompanyDescription));

        RuleFor(x => x.Industry)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Industry));

        RuleFor(x => x.CompanySize)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.CompanySize));

        RuleFor(x => x.Website)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.Address)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.Country)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Country));

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.ContactPhone)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));

        RuleFor(x => x.LogoUrl)
            .MaximumLength(1024)
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));
    }
}
