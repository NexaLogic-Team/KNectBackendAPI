using FluentValidation;
using Knect.Application.Profiles.DTOs;

namespace Knect.Application.Profiles.Validators;

public sealed class UserProfileValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UserProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Location)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Location));

        RuleFor(x => x.ProfessionalSummary)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.ProfessionalSummary));

        RuleFor(x => x.Skills)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Skills));

        RuleFor(x => x.WorkExperience)
            .MaximumLength(4000)
            .When(x => !string.IsNullOrWhiteSpace(x.WorkExperience));

        RuleFor(x => x.Education)
            .MaximumLength(4000)
            .When(x => !string.IsNullOrWhiteSpace(x.Education));

        RuleFor(x => x.ProfileImageUrl)
            .MaximumLength(1024)
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));

        RuleFor(x => x.CvFileName)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.CvFileName));
    }
}
