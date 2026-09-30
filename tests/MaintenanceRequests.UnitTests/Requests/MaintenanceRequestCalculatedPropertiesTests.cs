using MaintenanceRequests.Domain.Requests;
using static MaintenanceRequests.UnitTests.Requests.RequestFactory;

namespace MaintenanceRequests.UnitTests.Requests;

public class MaintenanceRequestCalculatedPropertiesTests
{
    // Expected values copied from the specification table, not from StatusTransitions.
    public static TheoryData<RequestStatus, RequestStatus[], bool> Expectations() => new()
    {
        { RequestStatus.Pending, [RequestStatus.InProgress, RequestStatus.Cancelled], true },
        { RequestStatus.InProgress, [RequestStatus.OnHold, RequestStatus.Resolved, RequestStatus.Cancelled], true },
        { RequestStatus.OnHold, [RequestStatus.InProgress, RequestStatus.Cancelled], true },
        { RequestStatus.Resolved, [], false },
        { RequestStatus.Cancelled, [], false }
    };

    [Theory]
    [MemberData(nameof(Expectations))]
    public void CalculatedProperties_MatchSpecificationForEachStatus(
        RequestStatus status, RequestStatus[] expectedTransitions, bool expectedCanBeAssigned)
    {
        var request = CreateIn(status);

        Assert.Equal(expectedTransitions.Order(), request.AllowedTransitions.Order());
        Assert.Equal(expectedCanBeAssigned, request.CanBeAssigned);
    }
}
