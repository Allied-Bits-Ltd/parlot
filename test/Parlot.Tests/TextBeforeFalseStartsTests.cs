using Parlot.Fluent;
using Xunit;
using static Parlot.Fluent.Parsers;

namespace Parlot.Tests;

public class TextBeforeFalseStartsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(4000)]
    public void FindsDelimiterAfterFalseStarts(int length)
    {
        var prefix = new string('-', length).Replace("--", "a-");
        var parser = AnyCharBefore(Literals.Text("-->").Or(Literals.Text("==>")), canBeEmpty: true);

        Assert.True(parser.TryParse(prefix + "-->", out var value));
        Assert.Equal(prefix, value.ToString());
        Assert.True(parser.TryParse(prefix + "==>", out value));
        Assert.Equal(prefix, value.ToString());
        Assert.True(parser.TryParse(prefix, out value));
        Assert.Equal(prefix, value.ToString());
    }
}
