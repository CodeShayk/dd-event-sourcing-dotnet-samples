// Chapter 21 — FluentValidation validators (Layer 1: API boundary)
using FluentValidation;
using BankAccount.Api.Models;

namespace BankAccount.Api.Validators;

public sealed class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    public DepositRequestValidator()
    {
        RuleFor(r => r.Amount)
            .GreaterThan(0)
            .WithMessage("Deposit amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Single deposit cannot exceed £1,000,000.");

        RuleFor(r => r.Description)
            .NotEmpty()
            .WithMessage("Deposit description is required.")
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters.");
    }
}

public sealed class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
{
    public WithdrawRequestValidator()
    {
        RuleFor(r => r.Amount)
            .GreaterThan(0)
            .WithMessage("Withdrawal amount must be greater than zero.")
            .LessThanOrEqualTo(100_000)
            .WithMessage("Single withdrawal cannot exceed £100,000.");

        RuleFor(r => r.Description)
            .NotEmpty()
            .WithMessage("Withdrawal description is required.")
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters.");
    }
}

public sealed class OpenAccountRequestValidator : AbstractValidator<OpenAccountRequest>
{
    public OpenAccountRequestValidator()
    {
        RuleFor(r => r.AccountNumber)
            .NotEmpty()
            .WithMessage("Account number is required.")
            .Matches(@"^[A-Z]{3}-\d{3,10}$")
            .WithMessage("Account number must match the format ACC-NNNN.");

        RuleFor(r => r.AccountHolder)
            .NotEmpty()
            .WithMessage("Account holder name is required.")
            .MaximumLength(100)
            .WithMessage("Account holder name cannot exceed 100 characters.");

        RuleFor(r => r.InitialBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Initial balance cannot be negative.");
    }
}
