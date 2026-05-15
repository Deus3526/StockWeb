namespace NewStock.Exceptions;

/// <summary>
/// 攜帶要回傳的 HTTP 狀態碼，由 ExceptionHandlingMiddleware 寫入回應。
/// </summary>
public sealed class HttpStatusCodeException : Exception
{
    public int StatusCode { get; }

    public HttpStatusCodeException(int statusCode, string message)
        : base(message)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(statusCode, 400);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(statusCode, 599);
        StatusCode = statusCode;
    }

    public HttpStatusCodeException(int statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(statusCode, 400);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(statusCode, 599);
        StatusCode = statusCode;
    }
}