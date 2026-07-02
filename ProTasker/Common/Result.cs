namespace ProTasker.Common
{
    public class Result
    {
        public ResultStatus Status { get; }
        public string? Error { get; }

        public bool IsSuccess => Status == ResultStatus.Success;

        protected Result(ResultStatus status, string? error)
        {
            Status = status;
            Error = error;
        }

        public static Result Success() => new(ResultStatus.Success, null);
        public static Result NotFound(string? error = null) => new(ResultStatus.NotFound, error);
        public static Result Conflict(string? error = null) => new(ResultStatus.Conflict, error);
        public static Result Unauthorized(string? error = null) => new(ResultStatus.Unauthorized, error);
        public static Result Validation(string? error = null) => new(ResultStatus.Validation, error);
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(ResultStatus status, T? value, string? error)
            : base(status, error)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(ResultStatus.Success, value, null);
        public static new Result<T> NotFound(string? error = null) => new(ResultStatus.NotFound, default, error);
        public static new Result<T> Conflict(string? error = null) => new(ResultStatus.Conflict, default, error);
        public static new Result<T> Unauthorized(string? error = null) => new(ResultStatus.Unauthorized, default, error);
        public static new Result<T> Validation(string? error = null) => new(ResultStatus.Validation, default, error);

    }
}
