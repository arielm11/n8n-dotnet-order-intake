using Microsoft.Data.SqlClient;

namespace OrderIntake.Api.Data;

public sealed class SqlConnectionFactory(string connectionString)
{
    public SqlConnection Create() => new(connectionString);
}