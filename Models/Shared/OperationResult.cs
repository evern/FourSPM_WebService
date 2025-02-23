namespace FourSPM_WebService.Models.Shared;

public enum OperationStatus
{
    None,
    Success,
    NotFound,
    NoAccess,
    Created,
    Updated,
    Validation,
    Error
}
public class OperationResult
{
    public string? Message { get; set; }
    public OperationStatus Status { get; set; }

    public static OperationResult Success()
    {
        return new OperationResult { Status = OperationStatus.Success };
    }
}

public class OperationResult<TResult> : OperationResult
{
    public TResult? Result { get; set; }
}
