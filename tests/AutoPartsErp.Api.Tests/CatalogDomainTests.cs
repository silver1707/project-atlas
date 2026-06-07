using Atlas.Api.Catalog;
using FluentAssertions;

namespace Atlas.Api.Tests;

public sealed class CatalogDomainTests
{
    [Fact]
    public void Product_keeps_auto_parts_identifiers_as_strings_and_raises_events()
    {
        var companyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var branchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var product = AutoPartProduct.Create(new CreateProductRequest(
            " nk-123 ",
            "Pastilha de freio dianteira",
            "7890000000001",
            "87083019",
            "0105700",
            "UN",
            "UN",
            "Nakata",
            "Freios",
            "Nakata",
            "nkf123",
            "04465-0K290",
            "0",
            1.2m,
            null,
            null,
            null,
            """{"material":"ceramica"}""",
            "[]",
            100m,
            50m), companyId, branchId);

        product.Sku.Should().Be("NK-123");
        product.OeCode.Should().Be("04465-0K290");
        product.Ncm.Should().Be("87083019");
        product.Cest.Should().Be("0105700");
        product.SuggestedPrice.Should().Be(150m);
        product.DomainEvents.Should().ContainSingle(x => x is ProductCreated);
    }
}
