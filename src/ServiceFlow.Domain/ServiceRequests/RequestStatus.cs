namespace ServiceFlow.Domain.ServiceRequests;

public enum RequestStatus
{
    New = 1,
    InProgress = 2,
    WaitingForCustomer = 3,
    Resolved = 4,
    Closed = 5
}
