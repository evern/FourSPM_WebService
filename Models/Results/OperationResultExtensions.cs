using FourSPM_WebService.Models.Shared;

namespace FourSPM_WebService.Models.Results
{
    public static class OperationResultExtensions
    {
        public static OperationResult<T> Success<T>(this OperationResult<T> _, T result)
        {
            return new OperationResult<T>
            {
                Status = OperationStatus.Success,
                Result = result
            };
        }

        public static OperationResult<T> Error<T>(this OperationResult<T> _, string message)
        {
            return new OperationResult<T>
            {
                Status = OperationStatus.Error,
                Message = message
            };
        }
    }
}
