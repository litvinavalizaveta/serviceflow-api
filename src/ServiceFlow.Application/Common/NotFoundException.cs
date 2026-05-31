namespace ServiceFlow.Application.Common;

public sealed class NotFoundException : ServiceFlowApplicationException
{
    public NotFoundException(string resourceName, Guid id)
        : base($"{resourceName} with id '{id}' was not found.")
    {
        ResourceName = resourceName;
        Id = id;
    }

    public string ResourceName { get; }

    public Guid Id { get; }
}
