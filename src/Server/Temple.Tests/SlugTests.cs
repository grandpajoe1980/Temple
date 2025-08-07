using Temple.Domain.Shared;

namespace Temple.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("My Test Tenant", "my-test-tenant")]
    [InlineData("  Spaces  In  Name ", "spaces-in-name")]
    [InlineData("MixedCASE123", "mixedcase123")]
    [InlineData("Symbols*&^%$#@!", "symbols")]
    public void GeneratesExpected(string input, string expected)
    {
        Assert.Equal(expected, Slug.From(input));
    }
}
