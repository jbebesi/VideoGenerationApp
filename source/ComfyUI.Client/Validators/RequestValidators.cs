using FluentValidation;
using ComfyUI.Client.Models.Requests;

namespace ComfyUI.Client.Validators;

/// <summary>
/// Validator for PromptRequest
/// </summary>
public class PromptRequestValidator : AbstractValidator<PromptRequest>
{
    public PromptRequestValidator()
    {
        RuleFor(x => x.Prompt)
            .NotNull()
            .WithMessage("Prompt is required")
            .NotEmpty()
            .WithMessage("Prompt cannot be empty");

        RuleFor(x => x.PromptId)
            .Must(BeValidGuidWhenProvided)
            .WithMessage("PromptId must be a valid GUID when provided");

        RuleFor(x => x.Number)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Number.HasValue)
            .WithMessage("Number must be greater than or equal to 0");

        RuleFor(x => x.ClientId)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.ClientId))
            .WithMessage("ClientId cannot be empty when provided");
    }

    private static bool BeValidGuidWhenProvided(string? promptId)
    {
        if (string.IsNullOrEmpty(promptId))
            return true;
        
        return Guid.TryParse(promptId, out _);
    }
}

/// <summary>
/// Validator for QueueRequest
/// </summary>
public class QueueRequestValidator : AbstractValidator<QueueRequest>
{
    public QueueRequestValidator()
    {
        RuleFor(x => x)
            .Must(HaveAtLeastOneOperation)
            .WithMessage("At least one operation (clear or delete) must be specified");

        RuleFor(x => x.Delete)
            .Must(NotBeEmptyWhenProvided)
            .When(x => x.Delete != null)
            .WithMessage("Delete array cannot be empty when provided");
    }

    private static bool HaveAtLeastOneOperation(QueueRequest request)
    {
        return request.Clear.HasValue || (request.Delete != null && request.Delete.Length > 0);
    }

    private static bool NotBeEmptyWhenProvided(string[]? delete)
    {
        return delete == null || delete.Length > 0;
    }
}

/// <summary>
/// Validator for LogSubscriptionRequest
/// </summary>
public class LogSubscriptionRequestValidator : AbstractValidator<LogSubscriptionRequest>
{
    public LogSubscriptionRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("ClientId is required");
    }
}

/// <summary>
/// Validator for FreeMemoryRequest
/// </summary>
public class FreeMemoryRequestValidator : AbstractValidator<FreeMemoryRequest>
{
    public FreeMemoryRequestValidator()
    {
        RuleFor(x => x)
            .Must(HaveAtLeastOneOperation)
            .WithMessage("At least one operation (unload_models or free_memory) must be specified");
    }

    private static bool HaveAtLeastOneOperation(FreeMemoryRequest request)
    {
        return request.UnloadModels.HasValue || request.FreeMemory.HasValue;
    }
}

/// <summary>
/// Validator for HistoryRequest
/// </summary>
public class HistoryRequestValidator : AbstractValidator<HistoryRequest>
{
    public HistoryRequestValidator()
    {
        RuleFor(x => x)
            .Must(HaveAtLeastOneOperation)
            .WithMessage("At least one operation (clear or delete) must be specified");

        RuleFor(x => x.Delete)
            .Must(NotBeEmptyWhenProvided)
            .When(x => x.Delete != null)
            .WithMessage("Delete array cannot be empty when provided");
    }

    private static bool HaveAtLeastOneOperation(HistoryRequest request)
    {
        return request.Clear.HasValue || (request.Delete != null && request.Delete.Length > 0);
    }

    private static bool NotBeEmptyWhenProvided(string[]? delete)
    {
        return delete == null || delete.Length > 0;
    }
}