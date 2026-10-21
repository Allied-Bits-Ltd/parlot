using Parlot.Fluent;
using Xunit;
using static Parlot.Fluent.Parsers;

namespace Parlot.Tests;

public class CursorRestoredOnFailureTests
{
    private static Parser<string> Create(string scenario)
    {
        var next = Literals.Text("b");
        var parser = scenario switch
        {
            "eof" => Literals.Text("ab").Eof(),
            "switch" => Literals.Text("a").Switch((_, _) => 0, next),
            "null" => Literals.Text("a").Switch(static (_, _) => -1, next),
            "consume" => AnyCharBefore(Literals.Char(','), consumeDelimiter: true).Then(static x => x.ToString()),
            "keep" => AnyCharBefore(Literals.Char(',')).Then(static x => x.ToString()),
            _ => throw new System.InvalidOperationException()
        };
        return parser;
    }

    [Theory]
    [InlineData("eof", "abc")]
    [InlineData("switch", "ac")]
    [InlineData("null", "ab")]
    [InlineData("consume", ",abc")]
    [InlineData("keep", ",abc")]
    public void FailureRestoresOffsetLineAndColumn(string scenario, string text)
    {
        var parser = Create(scenario);
        var context = new ParseContext(new Scanner("!!" + text));
        context.Scanner.Cursor.Advance(2);
        var start = context.Scanner.Cursor.Position;
        var result = new ParseResult<string>();

        Assert.False(parser.Parse(context, ref result));
        Assert.Equal(start, context.Scanner.Cursor.Position);

        var alternative = parser.Or(Literals.Text(text));
        Assert.True(alternative.Parse(context, ref result));
        Assert.Equal(text, result.Value);
    }

    [Fact]
    public void SuccessfulCasesStillMatch()
    {
        Assert.True(Create("eof").TryParse("ab", out var value));
        Assert.Equal("ab", value);
        Assert.True(Create("switch").TryParse("ab", out value));
        Assert.Equal("b", value);
        Assert.True(Create("consume").TryParse("abc,", out value));
        Assert.Equal("abc", value);
    }
    [Theory]
    [InlineData("eof", "abc")]
    [InlineData("switch", "ac")]
    [InlineData("null", "ab")]
    [InlineData("consume", ",abc")]
    public void DiscardedFailureRestoresCursor(string scenario, string text)
    {
        var parser = (Parser<char>)Literals.Char('!').AndSkip(Create(scenario));
        var context = new ParseContext(new Scanner("!!" + text));
        context.Scanner.Cursor.Advance();
        var start = context.Scanner.Cursor.Position;
        var result = new ParseResult<char>();
        Assert.False(parser.Parse(context, ref result));
        Assert.Equal(start, context.Scanner.Cursor.Position);
    }

}
