using Careersity.Application.Identity.Requests;
using FluentValidation;

namespace Careersity.Application.Identity.Validators;

internal static class PasswordRules
{
    internal static IRuleBuilderOptions<T, string> SecurePassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().MinimumLength(10).MaximumLength(128)
        .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
        .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
        .Matches("[0-9]").WithMessage("Password must contain a digit.")
        .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");
}
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320); RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100); RuleFor(x => x.LastName).NotEmpty().MaximumLength(100); RuleFor(x => x.Password).SecurePassword(); RuleFor(x => x.ConfirmPassword).Equal(x => x.Password); }
}
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{ public LoginRequestValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320); RuleFor(x => x.Password).NotEmpty().MaximumLength(128); } }
public sealed class RefreshAccessTokenRequestValidator : AbstractValidator<RefreshAccessTokenRequest>
{ public RefreshAccessTokenRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty(); }
public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{ public LogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty(); }
public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{ public UpdateMyProfileRequestValidator() { RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100); RuleFor(x => x.LastName).NotEmpty().MaximumLength(100); } }
public sealed class ChangeMyPasswordRequestValidator : AbstractValidator<ChangeMyPasswordRequest>
{ public ChangeMyPasswordRequestValidator() { RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128); RuleFor(x => x.NewPassword).SecurePassword(); RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword); } }
