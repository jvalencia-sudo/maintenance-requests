namespace MaintenanceRequests.Infrastructure.Persistence.Configurations;

internal static class CheckConstraintSql
{
    /// <summary>
    /// Builds <c>column IN ('A', 'B', ...)</c> from the enum names, so the database
    /// constraint can never drift from the domain enum. NULL passes, as in any CHECK.
    /// </summary>
    public static string In<TEnum>(string column)
        where TEnum : struct, Enum =>
        $"{column} IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(name => $"'{name}'"))})";
}
