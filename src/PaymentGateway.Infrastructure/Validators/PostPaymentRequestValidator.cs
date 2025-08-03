using FluentValidation;

using PaymentGateway.Infrastructure.Resources;

namespace PaymentGateway.Infrastructure.Validators;

internal sealed class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    private readonly HashSet<string> _validCurrencies = ["USD", "EUR", "GBP"];

    public PostPaymentRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage(ValidationMessages.CardNumberRequired)
            .Length(14, 19).WithMessage(ValidationMessages.CardNumberLength)
            .Must(BeNumericOnly).WithMessage(ValidationMessages.CardNumberNumericOnly);

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12).WithMessage(ValidationMessages.ExpiryMonthRange);

        RuleFor(x => x)
            .Must(HaveValidExpiryDate).WithMessage(ValidationMessages.ExpiryDateFuture)
            .When(x => x.ExpiryMonth is >= 1 and <= 12);

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage(ValidationMessages.CurrencyRequired)
            .Length(3).WithMessage(ValidationMessages.CurrencyLength)
            .Must(BeValidCurrency).WithMessage(ValidationMessages.CurrencyValid);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(ValidationMessages.AmountPositive);

        RuleFor(x => x.Cvv)
            .NotEmpty().WithMessage(ValidationMessages.CvvRequired)
            .Length(3, 4).WithMessage(ValidationMessages.CvvLength)
            .Must(BeNumericOnly).WithMessage(ValidationMessages.CvvNumericOnly);
    }

    private static bool BeNumericOnly(string? value)
    {
        return !string.IsNullOrEmpty(value) && value.All(char.IsDigit);
    }

    private bool BeValidCurrency(string? currency)
    {
        return !string.IsNullOrEmpty(currency) && 
               _validCurrencies.Contains(currency, StringComparer.InvariantCultureIgnoreCase);
    }

    private static bool HaveValidExpiryDate(PostPaymentRequest request)
    {
        try
        {
            DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly expiryDate = new DateOnly(request.ExpiryYear, request.ExpiryMonth, 1).AddMonths(1).AddDays(-1);
            return expiryDate > currentDate;
        }
        catch
        {
            return false;
        }
    }
}