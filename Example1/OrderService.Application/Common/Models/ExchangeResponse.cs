using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace OrderService.Application.Common.Models;

public class ExchangeResponse<T>
{
    [MemberNotNullWhen(true, nameof(Data))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Success { get; init; }
    
    public T? Data { get; init; }
    public ExchangeError? Error { get; init; }
    public int? StatusCode { get; init; }

    public static ExchangeResponse<T> CreateSuccess(T data, HttpStatusCode? statusCode = null)
    {
        return new ExchangeResponse<T>
        {
            Success = true,
            Data = data,
            StatusCode = (int?)statusCode
        };
    }

    public static ExchangeResponse<T> CreateError(string errorCode, string errorMessage, HttpStatusCode? statusCode = null)
    {
        return new ExchangeResponse<T>
        {
            Success = false,
            Error = new ExchangeError
            {
                Code = errorCode,
                Message = errorMessage
            },
            StatusCode = (int?)statusCode
        };
    }

    public static ExchangeResponse<T> CreateError(ExchangeError error, HttpStatusCode? statusCode = null)
    {
        return new ExchangeResponse<T>
        {
            Success = false,
            Error = error,
            StatusCode = (int?)statusCode
        };
    }
}

/// <summary>
/// Represents an error from an exchange API call.
/// </summary>
public class ExchangeError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
