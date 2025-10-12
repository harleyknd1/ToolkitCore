using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ToolkitCore;

/// <summary>Represents a result from an operation, including success status and error information.</summary>
[PublicAPI]
public readonly record struct Result
{
    private Result(bool success, [CanBeNull] string error = null, [CanBeNull] string details = null, [CanBeNull] Exception exception = null)
    {
        Success = success;
        Error = error;
        Details = details;
        Exception = exception;
    }

    /// <summary>Indicates whether the operation represented by this result was successful.</summary>
    /// <remarks>
    ///     A value of <c>true</c> signifies that the operation completed successfully. If <c>false</c>, the operation
    ///     failed, and additional information about the failure may be available through properties such as
    ///     <see cref="Error" />, <see cref="Details" />, or <see cref="Exception" />.
    /// </remarks>
    public bool Success { get; }

    /// <summary>Gets the error message associated with a failed operation result.</summary>
    /// <remarks>
    ///     This property provides an optional error message that describes the reason for failure. It will be null when
    ///     the operation is successful.
    /// </remarks>
    [CanBeNull]
    public string Error { get; }

    /// <summary>
    ///     Provides additional descriptive information about the operation's result. Typically contains detailed context
    ///     or explanation, particularly in failure scenarios.
    /// </summary>
    [CanBeNull]
    public string Details { get; }

    /// <summary>Gets the exception associated with a failed operation.</summary>
    /// <remarks>
    ///     This property holds an exception object if the operation failed due to an exception. If no exception was
    ///     associated with the failure, this property is null. It can be used to provide additional context and debugging
    ///     information for the error.
    /// </remarks>
    [CanBeNull]
    public Exception Exception { get; }

    /// <summary>Indicates whether the operation represented by this result was a failure.</summary>
    /// <remarks>
    ///     A value of <c>true</c> signifies that the operation did not complete successfully. This is the inverse of the
    ///     <see cref="Success" /> property. Additional details about the failure, such as error messages or exceptions, may be
    ///     accessible through properties like <see cref="Error" />, <see cref="Details" />, or <see cref="Exception" />.
    /// </remarks>
    public bool Failure => !Success;

    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful result of type <see cref="Result" />.</returns>
    public static Result Ok()
    {
        return new Result(true);
    }

    /// <summary>Represents the successful result of an operation.</summary>
    /// <returns>A <see cref="Result" /> indicating a successful operation.</returns>
    public static Result<T> Ok<T>(T value)
    {
        return new Result<T>(value, success: true);
    }

    /// <summary>Creates a failure result with the specified error message.</summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A <see cref="Result" /> instance indicating failure and containing the provided error message.</returns>
    public static Result Fail(string error)
    {
        return new Result(success: false, error);
    }

    /// <summary>Creates a failure result with the given exception, including the exception message and additional details.</summary>
    /// <param name="ex">The exception detailing the failure reason.</param>
    /// <returns>A <see cref="Result" /> indicating failure with associated exception details.</returns>
    public static Result Fail(Exception ex)
    {
        return new Result(success: false, ex.Message, exception: ex);
    }

    /// <summary>Creates a failure <see cref="Result" /> with a specified error message and details.</summary>
    /// <param name="error">The error message describing what caused the failure.</param>
    /// <param name="details">Additional details about the failure.</param>
    /// <returns>A failure <see cref="Result" /> initialized with the given error message and details.</returns>
    public static Result Fail(string error, string details)
    {
        return new Result(success: false, error, details);
    }

    /// <summary>Creates a failure result with the specified error message and exception details.</summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="ex">The exception associated with the failure.</param>
    /// <returns>A <see cref="Result" /> object indicating failure with the specified error and exception details.</returns>
    public static Result Fail(string error, [CanBeNull] Exception ex)
    {
        return new Result(success: false, error, exception: ex);
    }

    /// <summary>Creates a failed <see cref="Result" /> instance with the specified error message.</summary>
    /// <param name="error">The error message describing the reason for failure.</param>
    /// <returns>A <see cref="Result" /> instance indicating failure with the provided error message.</returns>
    public static Result<T> Fail<T>(string error)
    {
        return new Result<T>(value: default, success: false, error);
    }

    /// <summary>Creates a failure result with a specified error message.</summary>
    /// <param name="ex">The exception associated with the failure.</param>
    /// <returns>A <see cref="Result" /> instance indicating failure with the specified error message.</returns>
    public static Result<T> Fail<T>(Exception ex)
    {
        return new Result<T>(value: default, success: false, ex.Message, exception: ex);
    }

    /// <summary>Creates a failed <see cref="Result" /> with the specified error message.</summary>
    /// <param name="error">The error message describing the reason for failure.</param>
    /// <param name="details">Additional details about the error.</param>
    /// <returns>A failed <see cref="Result" /> containing the provided error message.</returns>
    public static Result<T> Fail<T>(string error, string details)
    {
        return new Result<T>(value: default, success: false, error, details);
    }

    /// <summary>Creates a failure result with the specified error message.</summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="ex">The exception associated with the failure.</param>
    /// <returns>A <see cref="Result" /> instance representing a failed operation.</returns>
    public static Result<T> Fail<T>(string error, [CanBeNull] Exception ex)
    {
        return new Result<T>(value: default, success: false, error, exception: ex);
    }

    public static implicit operator Result(Exception ex)
    {
        return Fail(ex);
    }
}

/// <summary>Represents a result that includes a value of type T along with operation status information.</summary>
[PublicAPI]
public readonly record struct Result<T>
{
    internal Result([CanBeNull] T value, bool success, [CanBeNull] string error = null, [CanBeNull] string details = null, [CanBeNull] Exception exception = null)
    {
        Value = value;
        Success = success;
        Error = error;
        Details = details;
        Exception = exception;
    }

    /// <summary>Gets the value associated with the operation result.</summary>
    /// <remarks>
    ///     The value of type <typeparamref name="T" /> represents the data returned when the operation completes
    ///     successfully. If the <see cref="Success" /> property is <c>false</c>, this property may be <c>null</c> or its
    ///     default value based on the type.
    /// </remarks>
    [CanBeNull]
    public T Value { get; }

    /// <summary>Represents the successful outcome of an operation.</summary>
    /// <remarks>
    ///     When set to <c>true</c>, this signifies that the associated operation completed without errors. In case of a
    ///     value of <c>false</c>, additional failure-related information can be inspected via properties such as
    ///     <see cref="Error" /> or <see cref="Exception" />.
    /// </remarks>
    public bool Success { get; }

    /// <summary>Provides error information associated with the result of an operation that failed.</summary>
    /// <remarks>
    ///     Contains a description of the error that occurred. This property is only populated if the operation was
    ///     unsuccessful, indicated by <c>Success</c> being <c>false</c>. Further context about the failure may be provided
    ///     through additional properties such as <c>Details</c> or <c>Exception</c>.
    /// </remarks>
    [CanBeNull]
    public string Error { get; }

    /// <summary>Provides additional details or context about the result of the operation.</summary>
    /// <remarks>
    ///     This property may contain supplementary information regarding the result, which can help clarify the outcome
    ///     or provide context in case of an error. It is typically used in conjunction with the <see cref="Error" /> property
    ///     to offer more descriptive insights about a failure.
    /// </remarks>
    [CanBeNull]
    public string Details { get; }

    /// <summary>Provides details about an exception that occurred during the operation.</summary>
    /// <remarks>
    ///     This property holds the <see cref="Exception" /> instance that caused the failure. It is populated when the
    ///     operation fails due to an exception. Check this property for detailed information about the error.
    /// </remarks>
    [CanBeNull]
    public Exception Exception { get; }

    /// <summary>Indicates whether the operation represented by this result was a failure.</summary>
    /// <remarks>
    ///     A value of <c>true</c> signifies that the operation did not complete successfully. This is the logical inverse
    ///     of <see cref="Success" />. Additional details about the failure may be available through properties such as
    ///     <see cref="Error" />, <see cref="Details" />, or <see cref="Exception" />.
    /// </remarks>
    public bool Failure => !Success;

    public void Deconstruct(out bool success, [CanBeNull] out T value)
    {
        success = Success;
        value = Value;
    }

    /// <summary>
    ///     Maps the current result to a new result of type <typeparamref name="TResult" /> using the provided mapping
    ///     function.
    /// </summary>
    /// <param name="mapper">
    ///     A function that maps the value of the current result to a new result of type
    ///     <typeparamref name="TResult" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Result{TResult}" /> containing the mapped value if the operation was successful; otherwise, a
    ///     failure result with the original error and exception.
    /// </returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return Success ? Result.Ok(mapper(Value!)) : Result.Fail<TResult>(Error ?? "Mapping failed", Exception);
    }

    /// <summary>
    ///     Asynchronously maps the current result's value to a new result of type <typeparamref name="TResult" /> using
    ///     the specified asynchronous mapper function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result value after mapping.</typeparam>
    /// <param name="mapper">The asynchronous function to transform the current result value.</param>
    /// <returns>
    ///     A new <see cref="Result{TResult}" /> representing the mapped value if the operation is successful, or a
    ///     failure result if the current result is not successful.
    /// </returns>
    public async Task<Result<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> mapper)
    {
        return Success ? Result.Ok(await mapper(Value!)) : Result.Fail<TResult>(Error ?? "Mapping failed", Exception);
    }

    public static implicit operator bool(Result<T> result)
    {
        return result.Success;
    }

    public static implicit operator Result(Result<T> result)
    {
        return result.Success ? Result.Ok() : Result.Fail(result.Error ?? "Operation failed", result.Exception);
    }
}