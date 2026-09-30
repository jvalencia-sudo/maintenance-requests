namespace MaintenanceRequests.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    public User(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;
}
