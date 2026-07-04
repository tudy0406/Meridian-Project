namespace Meridian.Api.Common.Exceptions;

/// <summary>
/// Base class for expected, business-level failures. These are translated into
/// clean HTTP responses by the global exception middleware, so raw exception
/// details are never leaked to clients.
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    public virtual string ErrorCode => GetType().Name.Replace("Exception", string.Empty);

    protected AppException(string message) : base(message) { }
}

/// <summary>Requested resource does not exist.</summary>
public sealed class NotFoundException : AppException
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public NotFoundException(string message) : base(message) { }
    public static NotFoundException For(string entity, object id) =>
        new($"{entity} with id '{id}' was not found.");
}

/// <summary>Authenticated user lacks permission / ownership for the operation.</summary>
public sealed class ForbiddenException : AppException
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public ForbiddenException(string message = "You are not allowed to perform this action.") : base(message) { }
}

/// <summary>Authentication failed or is required.</summary>
public sealed class UnauthorizedException : AppException
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public UnauthorizedException(string message = "Authentication failed.") : base(message) { }
}

/// <summary>Request conflicts with current state (e.g. duplicate email).</summary>
public sealed class ConflictException : AppException
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public ConflictException(string message) : base(message) { }
}

/// <summary>A business rule was violated (invalid but well-formed request).</summary>
public sealed class BusinessRuleException : AppException
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Input validation failed. Carries per-field error messages.</summary>
public sealed class ValidationException : AppException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
