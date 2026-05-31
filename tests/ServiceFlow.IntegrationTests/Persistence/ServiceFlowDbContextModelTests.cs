using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.IntegrationTests.Persistence;

public sealed class ServiceFlowDbContextModelTests
{
    [Fact]
    public void ServiceRequestRowVersion_IsConfiguredForOptimisticConcurrency()
    {
        var options = new DbContextOptionsBuilder<ServiceFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=serviceflow_model_test;Username=serviceflow;Password=serviceflow")
            .Options;

        using var dbContext = new ServiceFlowDbContext(options);
        var entityType = dbContext.Model.FindEntityType(typeof(ServiceRequest));
        var rowVersion = entityType?.FindProperty(nameof(ServiceRequest.RowVersion));

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
        Assert.Equal(
            "xmin",
            rowVersion.GetColumnName(StoreObjectIdentifier.Table("service_requests", schema: null)));
    }
}
