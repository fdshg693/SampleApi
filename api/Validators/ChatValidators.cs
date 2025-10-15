using FluentValidation;
using SampleApi.Models;

namespace SampleApi.Validators;

/// <summary>
/// ChatRequestのバリデーター
/// </summary>
public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Messages)
            .NotEmpty().WithMessage("メッセージは必須です")
            .Must(messages => messages.Count > 0).WithMessage("少なくとも1つのメッセージが必要です");

        RuleForEach(x => x.Messages)
            .SetValidator(new ChatMessageValidator());

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("モデル名は100文字以内で入力してください")
            .When(x => !string.IsNullOrEmpty(x.Model));
    }
}

/// <summary>
/// ChatMessageのバリデーター
/// </summary>
public class ChatMessageValidator : AbstractValidator<ChatMessage>
{
    public ChatMessageValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("ロールは必須です")
            .Must(role => role is "user" or "assistant" or "system")
            .WithMessage("ロールは 'user', 'assistant', 'system' のいずれかである必要があります");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("メッセージ内容は必須です")
            .MaximumLength(5000).WithMessage("メッセージは5000文字以内で入力してください");
    }
}
