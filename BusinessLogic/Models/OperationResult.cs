namespace BusinessLogic.Models;

/// <summary>
/// Represents the result of an operation, indicating success or failure with an optional error message.
/// </summary>
/// <param name="Success">Indicates whether the operation succeeded.</param>
/// <param name="ErrorMessage">An optional error message if the operation failed.</param>
public record OperationResult(bool Success, string? ErrorMessage = null)
{
    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    public static OperationResult Ok() => new(true);

    /// <summary>
    /// Creates a failed operation result with the specified error message.
    /// </summary>
    /// <param name="message">The error message describing why the operation failed.</param>
    public static OperationResult Fail(string message) => new(false, message);

    /// <summary>
    /// Implicitly converts the operation result to a boolean for easy success checking.
    /// </summary>
    public static implicit operator bool(OperationResult result) => result.Success;
}
