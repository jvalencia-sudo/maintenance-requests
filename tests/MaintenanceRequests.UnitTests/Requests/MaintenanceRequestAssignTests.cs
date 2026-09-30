using MaintenanceRequests.Domain.Exceptions;
using MaintenanceRequests.Domain.Requests;
using static MaintenanceRequests.UnitTests.Requests.RequestFactory;

namespace MaintenanceRequests.UnitTests.Requests;

public class MaintenanceRequestAssignTests
{
    private static readonly Guid ActorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FirstAssigneeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SecondAssigneeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset AssignedAt = Now.AddHours(1);
    private static readonly DateTimeOffset ReassignedAt = Now.AddHours(3);

    [Theory]
    [InlineData(RequestStatus.Pending)]
    [InlineData(RequestStatus.InProgress)]
    [InlineData(RequestStatus.OnHold)]
    public void Assign_FirstTime_SetsAssigneeAndAppendsEntryWithoutPrevious(RequestStatus status)
    {
        var request = CreateIn(status);
        var historyCount = request.History.Count;

        request.Assign(FirstAssigneeId, ActorId, AssignedAt);

        Assert.Equal(FirstAssigneeId, request.AssigneeId);
        Assert.Equal(historyCount + 1, request.History.Count);

        var entry = request.History.Last();
        Assert.Equal(HistoryEventType.AssigneeChanged, entry.EventType);
        Assert.Null(entry.PreviousAssigneeId);
        Assert.Equal(FirstAssigneeId, entry.NewAssigneeId);
        Assert.Equal(ActorId, entry.ActorId);
        Assert.Equal(AssignedAt, entry.OccurredAt);
        Assert.Null(entry.FromStatus);
        Assert.Null(entry.ToStatus);
    }

    [Fact]
    public void Assign_Reassignment_RecordsPreviousAndNewAssignee()
    {
        var request = CreateValid();
        request.Assign(FirstAssigneeId, ActorId, AssignedAt);

        request.Assign(SecondAssigneeId, ActorId, ReassignedAt);

        Assert.Equal(SecondAssigneeId, request.AssigneeId);

        var entry = request.History.Last();
        Assert.Equal(HistoryEventType.AssigneeChanged, entry.EventType);
        Assert.Equal(FirstAssigneeId, entry.PreviousAssigneeId);
        Assert.Equal(SecondAssigneeId, entry.NewAssigneeId);
        Assert.Equal(ActorId, entry.ActorId);
        Assert.Equal(ReassignedAt, entry.OccurredAt);
    }

    [Fact]
    public void Assign_SameAssignee_ThrowsAndLeavesRequestUnchanged()
    {
        var request = CreateValid();
        request.Assign(FirstAssigneeId, ActorId, AssignedAt);
        var historyBefore = request.History.ToList();

        var exception = Assert.Throws<SameAssigneeException>(
            () => request.Assign(FirstAssigneeId, ActorId, ReassignedAt));

        Assert.Equal(FirstAssigneeId, exception.AssigneeId);
        Assert.Equal(FirstAssigneeId, request.AssigneeId);
        Assert.Equal(historyBefore, request.History);
    }

    [Theory]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Cancelled)]
    public void Assign_TerminalStatus_ThrowsAndLeavesRequestUnchanged(RequestStatus status)
    {
        var request = CreateIn(status);
        var historyBefore = request.History.ToList();

        var exception = Assert.Throws<RequestClosedException>(
            () => request.Assign(FirstAssigneeId, ActorId, AssignedAt));

        Assert.Equal(status, exception.Status);
        Assert.Null(request.AssigneeId);
        Assert.Equal(historyBefore, request.History);
    }
}
