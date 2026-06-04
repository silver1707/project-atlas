using System.Data.Common;
using Atlas.Api.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Atlas.Api.Infrastructure;

public sealed class TenantSessionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantSession(connection, CancellationToken.None).GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await SetTenantSession(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private async Task SetTenantSession(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select
              set_config('atlas.company_id', @company_id, false),
              set_config('atlas.branch_id', @branch_id, false),
              set_config('atlas.user_id', @user_id, false)
            """;

        var company = command.CreateParameter();
        company.ParameterName = "company_id";
        company.Value = tenantContext.CompanyId.ToString("D");
        command.Parameters.Add(company);

        var branch = command.CreateParameter();
        branch.ParameterName = "branch_id";
        branch.Value = tenantContext.BranchId.ToString("D");
        command.Parameters.Add(branch);

        var user = command.CreateParameter();
        user.ParameterName = "user_id";
        user.Value = tenantContext.UserId;
        command.Parameters.Add(user);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
