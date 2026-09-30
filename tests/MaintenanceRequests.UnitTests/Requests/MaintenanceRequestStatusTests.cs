using MaintenanceRequests.Domain.Exceptions;
using MaintenanceRequests.Domain.Requests;
using static MaintenanceRequests.UnitTests.Requests.RequestFactory;

namespace MaintenanceRequests.UnitTests.Requests;

public class MaintenanceRequestStatusTests
{
    private const int ActorId = 2;

    private static readonly DateTimeOffset ChangedAt = Now.AddHours(2);

    // Copied from the specification on purpose. Never derive it from StatusTransitions:
    // that would test the map against itself.
    private static readonly (RequestStatus From, RequestStatus To)[] Allowed =
    [
        (RequestStatus.Pending, RequestStatus.InProgress),
        (RequestStatus.Pending, RequestStatus.Cancelled),
        (RequestStatus.InProgress, RequestStatus.OnHold),
        (RequestStatus.InProgress, RequestStatus.Resolved),
        (RequestStatus.InProgress, RequestStatus.Cancelled),
        (RequestStatus.OnHold, RequestStatus.InProgress),
        (RequestStatus.OnHold, RequestStatus.Cancelled)
    ];

    public static TheoryData<RequestStatus, RequestStatus> AllowedTransitions()
    {
        var data = new TheoryData<RequestStatus, RequestStatus>();
        foreach (var (from, to) in Allowed)
        {
            data.Add(from, to);
        }

        return data;
    }

    public static TheoryData<RequestStatus, RequestStatus> RejectedTransitions()
    {
        var data = new TheoryData<RequestStatus, RequestStatus>();
        foreach (var from in Enum.GetValues<RequestStatus>())
        {
            foreach (var to in Enum.GetValues<RequestStatus>())
            {
                if (!Allowed.Contains((from, to)))
                {
                    data.Add(from, to);
                }
            }
        }

        return data;
    }

    [Fact]
    public void TransitionTables_CoverAllTwentyFiveCombinations()
    {
        Assert.Equal(7, AllowedTransitions().Count());
        Assert.Equal(18, RejectedTransitions().Count());
    }

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void ChangeStatus_AllowedTransition_UpdatesStatusAndAppendsEntry(RequestStatus from, RequestStatus to)
    {
        var request = CreateIn(from);
        var historyCount = request.History.Count;

        request.ChangeStatus(to, ActorId, ChangedAt);

        Assert.Equal(to, request.Status);
        Assert.Equal(historyCount + 1, request.History.Count);

        var entry = request.History.Last();
        Assert.Equal(HistoryEventType.StatusChanged, entry.EventType);
        Assert.Equal(from, entry.FromStatus);
        Assert.Equal(to, entry.ToStatus);
        Assert.Equal(ActorId, entry.ActorId);
        Assert.Equal(ChangedAt, entry.OccurredAt);
        Assert.Null(entry.PreviousAssigneeId);
        Assert.Null(entry.NewAssigneeId);
    }

    [Theory]
    [MemberData(nameof(RejectedTransitions))]
    public void ChangeStatus_RejectedTransition_ThrowsAndLeavesRequestUnchanged(RequestStatus from, RequestStatus to)
    {
        var request = CreateIn(from);
        var historyBefore = request.History.ToList();

        var exception = Assert.Throws<InvalidStatusTransitionException>(
            () => request.ChangeStatus(to, ActorId, ChangedAt));

        Assert.Equal(from, exception.From);
        Assert.Equal(to, exception.To);
        Assert.Equal(from, request.Status);
        Assert.Equal(historyBefore, request.History);
    }
}
