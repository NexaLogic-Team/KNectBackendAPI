using FluentValidation;
using Knect.Application.Jobs.DTOs;
using Knect.Domain.Enums;

namespace Knect.Application.Jobs.Validators;

public sealed class CreateJobRequestValidator : AbstractValidator<CreateJobRequest>
{
    public CreateJobRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ExperienceId).NotEmpty();
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.EmploymentType).MaximumLength(60);
        RuleFor(x => x.Currency).MaximumLength(10).When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x).Must(x => !x.SalaryMin.HasValue || !x.SalaryMax.HasValue || x.SalaryMin <= x.SalaryMax)
            .WithMessage("Minimum salary cannot be greater than maximum salary.");
        RuleFor(x => x.Status).Must(status => status is null || status.Value == JobStatus.Draft || status.Value == JobStatus.Published || status.Value == JobStatus.Paused)
            .WithMessage("Only draft, published, or paused statuses are valid for creation.");
        RuleFor(x => x.ExpiresAt).Must(expiresAt => !expiresAt.HasValue || expiresAt.Value > DateTime.UtcNow)
            .WithMessage("Application deadline must be in the future.");
    }
}

public sealed class UpdateJobRequestValidator : AbstractValidator<UpdateJobRequest>
{
    public UpdateJobRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Title));
        RuleFor(x => x.Description).NotEmpty().When(x => x.Description is not null).WithMessage("Description cannot be empty.");
        RuleFor(x => x.CategoryId).NotEmpty().When(x => x.CategoryId.HasValue).WithMessage("Category is required.");
        RuleFor(x => x.ExperienceId).NotEmpty().When(x => x.ExperienceId.HasValue).WithMessage("Experience level is required.");
        RuleFor(x => x.Location).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Location));
        RuleFor(x => x.EmploymentType).MaximumLength(60).When(x => !string.IsNullOrWhiteSpace(x.EmploymentType));
        RuleFor(x => x.Currency).MaximumLength(10).When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x).Must(x => !x.SalaryMin.HasValue || !x.SalaryMax.HasValue || x.SalaryMin <= x.SalaryMax)
            .WithMessage("Minimum salary cannot be greater than maximum salary.");
        RuleFor(x => x.ExpiresAt).Must(expiresAt => !expiresAt.HasValue || expiresAt.Value > DateTime.UtcNow)
            .WithMessage("Application deadline must be in the future.");
    }
}

public sealed class JobSearchRequestValidator : AbstractValidator<JobSearchRequest>
{
    public JobSearchRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Search).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Search));
        RuleFor(x => x.Location).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Location));
        RuleFor(x => x.EmploymentType).MaximumLength(60).When(x => !string.IsNullOrWhiteSpace(x.EmploymentType));
        RuleFor(x => x.MinSalary).GreaterThanOrEqualTo(0).When(x => x.MinSalary.HasValue);
        RuleFor(x => x.MaxSalary).GreaterThanOrEqualTo(0).When(x => x.MaxSalary.HasValue);
        RuleFor(x => x).Must(x => !x.MinSalary.HasValue || !x.MaxSalary.HasValue || x.MinSalary <= x.MaxSalary)
            .WithMessage("Minimum salary cannot be greater than maximum salary.");
    }
}
