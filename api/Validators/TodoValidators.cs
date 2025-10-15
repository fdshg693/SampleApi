using FluentValidation;
using SampleApi.Models;

namespace SampleApi.Validators;

/// <summary>
/// CreateTodoRequestのバリデーター
/// </summary>
public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("説明は1000文字以内で入力してください")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

/// <summary>
/// UpdateTodoRequestのバリデーター
/// </summary>
public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内で入力してください")
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("説明は1000文字以内で入力してください")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
