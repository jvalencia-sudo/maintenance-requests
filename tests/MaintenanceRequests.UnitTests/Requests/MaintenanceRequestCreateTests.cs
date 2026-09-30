using MaintenanceRequests.Domain.Exceptions;
using MaintenanceRequests.Domain.Requests;
using static MaintenanceRequests.UnitTests.Requests.RequestFactory;

namespace MaintenanceRequests.UnitTests.Requests;

public class MaintenanceRequestCreateTests
{
    [Fact]
    public void Create_WithValidData_StartsPendingWithCreatedEntry()
    {
        var request = CreateValid();

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(ValidTitle, request.Title);
        Assert.Equal(ValidDescription, request.Description);
        Assert.Equal(RequestCategory.Equipment, request.Category);
        Assert.Equal(RequestPriority.High, request.Priority);
        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Equal(RequesterId, request.RequesterId);
        Assert.Null(request.AssigneeId);
        Assert.Equal(Now, request.CreatedAt);

        var entry = Assert.Single(request.History);
        Assert.Equal(HistoryEventType.Created, entry.EventType);
        Assert.Null(entry.FromStatus);
        Assert.Equal(RequestStatus.Pending, entry.ToStatus);
        Assert.Equal(RequesterId, entry.ActorId);
        Assert.Equal(Now, entry.OccurredAt);
    }

    [Fact]
    public void Create_TrimsTitleAndDescription()
    {
        var request = MaintenanceRequest.Create(
            $"  {ValidTitle}  ",
            $"\t{ValidDescription}\n",
            RequestCategory.Software,
            RequestPriority.Low,
            RequesterId,
            Now);

        Assert.Equal(ValidTitle, request.Title);
        Assert.Equal(ValidDescription, request.Description);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(120)]
    public void Create_TitleAtLengthLimit_IsAccepted(int length)
    {
        var title = new string('a', length);

        var request = CreateWithTitle(title);

        Assert.Equal(title, request.Title);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(121)]
    public void Create_TitleOutsideLengthLimit_IsRejected(int length)
    {
        AssertRejected("title", () => CreateWithTitle(new string('a', length)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("  abcd  ")]
    public void Create_TitleEmptyOrTooShortAfterTrim_IsRejected(string? title)
    {
        AssertRejected("title", () => CreateWithTitle(title!));
    }

    [Fact]
    public void Create_TitleWithThreeEmojis_IsRejected()
    {
        // 3 emojis are 6 UTF-16 code units but only 3 runes, below the 5 minimum.
        const string title = "🔧🔧🔧";
        Assert.Equal(6, title.Length);

        AssertRejected("title", () => CreateWithTitle(title));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(2000)]
    public void Create_DescriptionAtLengthLimit_IsAccepted(int length)
    {
        var description = new string('a', length);

        var request = CreateWithDescription(description);

        Assert.Equal(description, request.Description);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(2001)]
    public void Create_DescriptionOutsideLengthLimit_IsRejected(int length)
    {
        AssertRejected("description", () => CreateWithDescription(new string('a', length)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("          ")]
    public void Create_DescriptionEmptyOrWhitespace_IsRejected(string? description)
    {
        AssertRejected("description", () => CreateWithDescription(description!));
    }

    [Fact]
    public void Create_UndefinedCategory_IsRejected()
    {
        AssertRejected("category", () => MaintenanceRequest.Create(
            ValidTitle, ValidDescription, (RequestCategory)999, RequestPriority.Medium, RequesterId, Now));
    }

    [Fact]
    public void Create_UndefinedPriority_IsRejected()
    {
        AssertRejected("priority", () => MaintenanceRequest.Create(
            ValidTitle, ValidDescription, RequestCategory.Other, (RequestPriority)999, RequesterId, Now));
    }

    private static MaintenanceRequest CreateWithTitle(string title) => MaintenanceRequest.Create(
        title, ValidDescription, RequestCategory.Infrastructure, RequestPriority.Medium, RequesterId, Now);

    private static MaintenanceRequest CreateWithDescription(string description) => MaintenanceRequest.Create(
        ValidTitle, description, RequestCategory.Infrastructure, RequestPriority.Medium, RequesterId, Now);

    private static void AssertRejected(string expectedField, Func<MaintenanceRequest> create)
    {
        var exception = Assert.Throws<DomainValidationException>(() => create());
        Assert.Equal(expectedField, exception.Field);
    }
}
