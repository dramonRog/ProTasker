using Microsoft.AspNetCore.Mvc;

namespace ProTasker.Common
{
    public static class ResultCastExtensions
    {
        public static ActionResult<T> CastToResultCode<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                return new OkObjectResult(result.Value);
            return CreateErrorResult(result);
        }

        public static IActionResult CastToResultCode(this Result result)
        {
            if (result.IsSuccess)
                return new NoContentResult();
            return CreateErrorResult(result);
        }

        private static ActionResult CreateErrorResult(Result result)
        {
            var errorResponse = new { Message = result.Error };

            return result.Status switch
            {
                ResultStatus.NotFound => new NotFoundObjectResult(errorResponse),
                ResultStatus.Unauthorized => new UnauthorizedObjectResult(errorResponse),
                ResultStatus.Validation => new BadRequestObjectResult(errorResponse),
                ResultStatus.Conflict => new ConflictObjectResult(errorResponse),
                ResultStatus.Forbidden => new ObjectResult(errorResponse) { StatusCode = StatusCodes.Status403Forbidden },
                _ => new ObjectResult(errorResponse) { StatusCode = StatusCodes.Status500InternalServerError }
            };
        }
    }
}
