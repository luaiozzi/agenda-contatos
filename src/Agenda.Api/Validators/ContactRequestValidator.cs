using System.Text.RegularExpressions;
using Agenda.Api.Dtos;
using Agenda.Api.Helpers;
using FluentValidation;

namespace Agenda.Api.Validators;

public class ContactRequestValidator : AbstractValidator<ContactRequest>
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ContactRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .Must(name => name!.Trim().Length >= 2).WithMessage("O nome deve ter pelo menos 2 caracteres.")
            .Must(name => name!.Trim().Length <= 100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .Must(email => email!.Trim().Length <= 150).WithMessage("O e-mail deve ter no máximo 150 caracteres.")
            .Must(email => EmailRegex.IsMatch(email!.Trim())).WithMessage("Informe um e-mail válido.");

        RuleFor(x => x.Phone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O telefone é obrigatório.")
            .Must(phone => PhoneNormalizer.IsValid(phone))
            .WithMessage("Telefone inválido. Use DDD + número, ex.: (19) 99999-8888.");
    }
}
