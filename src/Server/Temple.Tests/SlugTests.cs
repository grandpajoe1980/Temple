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

    [Fact]
    public void Empty_String_Returns_Empty()
    {
        Assert.Equal(string.Empty, Slug.From(""));
    }

    [Fact]
    public void Null_Returns_Empty()
    {
        Assert.Equal(string.Empty, Slug.From(null!));
    }

    [Fact]
    public void Whitespace_Returns_Empty()
    {
        Assert.Equal(string.Empty, Slug.From("   "));
    }

    [Fact]
    public void Multiple_Dashes_Are_Collapsed()
    {
        Assert.Equal("hello-world", Slug.From("Hello---World"));
    }

    [Fact]
    public void Leading_And_Trailing_Dashes_Are_Removed()
    {
        Assert.Equal("hello", Slug.From("---Hello---"));
    }

    [Fact]
    public void Unicode_Characters_Are_Replaced_With_Dashes()
    {
        Assert.Equal("h-ll", Slug.From("Hëllö"));
    }

    [Fact]
    public void Very_Long_String_Is_Truncated_To_80_Characters()
    {
        var longString = new string('a', 100);
        var slug = Slug.From(longString);
        
        Assert.Equal(80, slug.Length);
        Assert.Equal(new string('a', 80), slug);
    }

    [Fact]
    public void Truncation_Preserves_Valid_Slug_Format()
    {
        var longString = new string('a', 85) + "---" + new string('b', 10);
        var slug = Slug.From(longString);
        
        Assert.Equal(80, slug.Length);
        Assert.False(slug.EndsWith("-"));
    }
}

