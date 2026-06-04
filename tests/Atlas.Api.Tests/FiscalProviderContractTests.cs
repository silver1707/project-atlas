using Atlas.Api.Fiscal;
using FluentAssertions;

namespace Atlas.Api.Tests;

public sealed class FiscalProviderContractTests
{
    [Fact]
    public async Task Sandbox_provider_returns_protocol_for_authorization_cancellation_query_and_manifestation()
    {
        var provider = new SandboxFiscalProvider();
        var document = new FiscalDocument
        {
            CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Model = "55",
            Number = "1",
            Series = "1",
            Provider = "sandbox",
            Environment = "homologation",
            XmlHash = "hash",
            XmlStorageUri = "/tmp/hash.xml",
            IssuedAt = DateTimeOffset.UtcNow
        };

        var authorization = await provider.AuthorizeAsync(document, "<xml />", CancellationToken.None);
        authorization.Success.Should().BeTrue();
        authorization.Protocol.Should().NotBeNullOrWhiteSpace();

        document.RegisterProviderResponse(authorization);
        var query = await provider.QueryAsync(document, CancellationToken.None);
        query.Success.Should().BeTrue();
        query.Protocol.Should().Be(document.AuthorizationProtocol);

        var cancellation = await provider.CancelAsync(document, new CancelFiscalDocumentRequest("sandbox", "Erro operacional"), CancellationToken.None);
        cancellation.Success.Should().BeTrue();

        var manifestation = await provider.ManifestRecipientAsync(new RecipientManifestationRequest("sandbox", "35260123456780001955500100000000011000000010", "ciencia_operacao", "", "homologation"), CancellationToken.None);
        manifestation.Success.Should().BeTrue();
    }
}
