namespace ServiceFlow.Application.Common;

public sealed class ForbiddenOperationException : ServiceFlowApplicationException
{
    public ForbiddenOperationException(string message)
        : base(message)
    {
    }
}
