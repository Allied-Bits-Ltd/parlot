using Parlot.Rewriting;
using Parlot.SourceGeneration;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Parlot.Fluent;

public enum StringLiteralQuotes
{
    Single,
    Double,
    Backtick,
    SingleOrDouble,
    Custom
}

public sealed class StringLiteral : Parser<TextSpan>, ISeekable, ISourceable
{
    static readonly char[] SingleQuotes = ['\''];
    static readonly char[] DoubleQuotes = ['\"'];
    static readonly char[] Backtick = ['`'];
    static readonly char[] SingleOrDoubleQuotes = ['\'', '\"'];

    private readonly StringLiteralQuotes _quotes;
    private readonly bool _returnDecoded;
    public StringLiteral(StringLiteralQuotes quotes, bool returnDecoded = true)
    {
        _quotes = quotes;

        ExpectedChars = _quotes switch
        {
            StringLiteralQuotes.Single => SingleQuotes,
            StringLiteralQuotes.Double => DoubleQuotes,
            StringLiteralQuotes.Backtick => Backtick,
            StringLiteralQuotes.SingleOrDouble => SingleOrDoubleQuotes,
            _ => throw new InvalidOperationException()
        };

        _returnDecoded = returnDecoded;

        Name = "StringLiteral";
    }

    public StringLiteral(char quote)
    {
        _quotes = quote switch
        {
            '\'' => StringLiteralQuotes.Single,
            '\"' => StringLiteralQuotes.Double,
            '`' => StringLiteralQuotes.Backtick,
            _ => StringLiteralQuotes.Custom,
        };

        ExpectedChars = [quote];

        _returnDecoded = true;

        Name = "StringLiteral";
    }

    public bool CanSeek { get; } = true;

    public char[] ExpectedChars { get; }

    public bool SkipWhitespace { get; }

    public override bool Parse(ParseContext context, ref ParseResult<TextSpan> result)
    {
        context.EnterParser(this);

        var cursor = context.Scanner.Cursor;
        var start = cursor.Offset;

        // Any of the quotes can start the string, the other kinds have a single one
        var quote = ExpectedChars[0];

        if (_quotes == StringLiteralQuotes.SingleOrDouble && cursor.Current == '"')
        {
            quote = '"';
        }

        if (!context.Scanner.ReadQuotedString(quote, out _, out var hasEscape))
        {
            context.ExitParser(this);
            return false;
        }

        var end = cursor.Offset;

        result.Set(start, end, _returnDecoded
            ? CreateResult(context.Scanner.Buffer, start, end, hasEscape)
            : new TextSpan(context.Scanner.Buffer, start + 1, end - start - 2));

        context.ExitParser(this);
        return true;
    }

    /// <summary>
    /// Builds the value of a string read by the scanner, without its quotes.
    /// </summary>
    /// <remarks>
    /// The scanner knows whether the string has escape sequences, so a string without any isn't read again
    /// and references the input buffer.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TextSpan CreateResult(string buffer, int start, int end, bool hasEscape)
    {
        return hasEscape
            ? new TextSpan(Character.DecodeStringInternal(buffer.AsSpan(start + 1, end - start - 2)))
            : new TextSpan(buffer, start + 1, end - start - 2);
    }


    public SourceResult GenerateSource(SourceGenerationContext context)
    {
        ThrowHelper.ThrowIfNull(context, nameof(context));

        var result = context.CreateResult(typeof(TextSpan));
        var cursorName = context.CursorName;
        var scannerName = context.ScannerName;

        var startName = $"start{context.NextNumber()}";
        var endName = $"end{context.NextNumber()}";

        result.Body.Add($"var {startName} = {cursorName}.Offset;");

        // Generate the appropriate read method call based on quote type
        var readMethod = _quotes switch
        {
            StringLiteralQuotes.Single => $"{scannerName}.ReadSingleQuotedString()",
            StringLiteralQuotes.Double => $"{scannerName}.ReadDoubleQuotedString()",
            StringLiteralQuotes.SingleOrDouble => $"{scannerName}.ReadQuotedString()",
            StringLiteralQuotes.Backtick => $"{scannerName}.ReadBacktickString()",
            StringLiteralQuotes.Custom => $"{scannerName}.ReadQuotedString(new char[] {{ {string.Join(", ", ExpectedChars.Select(c => $"'{c}'"))} }})",
            _ => throw new InvalidOperationException()
        };

        result.Body.Add($"if ({readMethod})");
        result.Body.Add("{");
        result.Body.Add($"    var {endName} = {cursorName}.Offset;");
        result.Body.Add($"    {result.SuccessVariable} = true;");
        result.Body.Add($"    {result.ValueVariable} = global::Parlot.Character.DecodeString({scannerName}.Buffer, {startName} + 1, {endName} - {startName} - 2);");
        result.Body.Add("}");

        return result;
    }
}
