namespace ServiceFlow.Application.Common;

public abstract class ServiceFlowApplicationException : Exception
{
    protected ServiceFlowApplicationException(string message)
        : base(message)
    {
    }
}
