namespace MaintenanceRequests.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    public User(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;
}
