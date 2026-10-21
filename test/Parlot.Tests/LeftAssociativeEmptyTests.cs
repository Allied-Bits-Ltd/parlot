using Parlot.Fluent;
using Xunit;
using static Parlot.Fluent.Parsers;

namespace Parlot.Tests;

public class LeftAssociativeEmptyTests
{
    [Theory]
    [InlineData(false, "x+x+y", 3, 4)]
    [InlineData(true, "x+x+y", 3, 4)]
    [InlineData(false, "y", 1, 0)]
    [InlineData(true, "y", 1, 0)]
    [InlineData(false, "x+", 2, 2)]
    [InlineData(true, "x+", 2, 2)]
    public void StopsWithoutProgress(bool withContext, string text, int expected, int offset)
    {
        var operand = Literals.Text("x").Optional().Then(static _ => 1);
        var op = Literals.Text("+").Optional();
        var calls = 0;
        var parser = withContext
            ? operand.LeftAssociative((op, (ParseContext _, int a, int b) => { calls++; return a + b; }))
            : operand.LeftAssociative((op, (int a, int b) => { calls++; return a + b; }));

        var context = new ParseContext(new Scanner(text));
        var result = new ParseResult<int>();
        Assert.True(parser.Parse(context, ref result));
        Assert.Equal(expected, result.Value);
        Assert.Equal(offset, context.Scanner.Cursor.Offset);
        Assert.Equal(expected - 1, calls);
    }
}
