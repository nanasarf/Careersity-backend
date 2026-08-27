using Careersity.Application.YouTube.Requests;
using FluentValidation;

namespace Careersity.Application.YouTube.Validators;

public sealed class YouTubeSearchRequestValidator : AbstractValidator<YouTubeSearchRequest>
{
    public YouTubeSearchRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MaxResults).InclusiveBetween(1, 25);
        RuleFor(x => x.PageToken).MaximumLength(500)
            .Matches("^[A-Za-z0-9_-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.PageToken));
    }
}
