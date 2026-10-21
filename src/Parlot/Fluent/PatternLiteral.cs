using Parlot.SourceGeneration;
using System;

namespace Parlot.Fluent;

public sealed class PatternLiteral : Parser<TextSpan>, ISourceable
{
    private readonly Func<char, bool> _predicate;
    private readonly int _minSize;
    private readonly int _maxSize;

    public PatternLiteral(Func<char, bool> predicate, int minSize = 1, int maxSize = 0)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _minSize = minSize;
        _maxSize = maxSize;

        Name = "PatternLiteral";
    }

    public override bool Parse(ParseContext context, ref ParseResult<TextSpan> result)
    {
        context.EnterParser(this);

        if (ParseSpan(context.Scanner.Cursor, _predicate, _minSize, _maxSize, out var span))
        {
            result.Set(span.Offset, span.Offset + span.Length, span);

            context.ExitParser(this);
            return true;
        }

        context.ExitParser(this);
        return false;
    }

    private static bool ParseSpan(Cursor cursor, Func<char, bool> predicate, int minSize, int maxSize, out TextSpan span)
    {
        if (cursor.Eof || !predicate(cursor.Current))
        {
            if (minSize > 0)
            {
                span = default;
                return false;
            }

            // A pattern without a minimum size matches nothing, like AnyOf does, and leaves the cursor in place
            span = new TextSpan(cursor.Buffer, cursor.Offset, 0);
            return true;
        }

        // The chars are counted first such that the cursor only moves once, and not at all when the size constraint is not met.

        var cursorSpan = cursor.Span;
        var limit = maxSize > 0 && maxSize < cursorSpan.Length ? maxSize : cursorSpan.Length;
        var size = 1;
        var newLines = cursorSpan[0] == '\n';

        while (size < limit)
        {
            var c = cursorSpan[size];

            if (!predicate(c))
            {
                break;
            }

            newLines |= c == '\n';
            size++;
        }

        if (size >= minSize)
        {
            var start = cursor.Offset;

            // The line and column only need to be tracked char by char when there are new lines
            if (newLines)
            {
                cursor.Advance(size);
            }
            else
            {
                cursor.AdvanceNoNewLines(size);
            }

            span = new TextSpan(cursor.Buffer, start, size);
            return true;
        }

        span = default;
        return false;
    }


    public SourceResult GenerateSource(SourceGenerationContext context)
    {
        ThrowHelper.ThrowIfNull(context, nameof(context));

        var result = context.CreateResult(typeof(TextSpan));
        var cursorName = context.CursorName;
        var scannerName = context.ScannerName;

        var startName = $"start{context.NextNumber()}";
        var sizeName = $"size{context.NextNumber()}";

        result.Body.Add($"var {startName} = {cursorName}.Position;");
        result.Body.Add($"var {sizeName} = 0;");

        // Register the predicate lambda
        var predicateLambda = context.RegisterLambda(_predicate);

        result.Body.Add("while (true)");
        result.Body.Add("{");
        result.Body.Add($"    if ({cursorName}.Eof) break;");
        result.Body.Add($"    if (!{predicateLambda}({cursorName}.Current)) break;");
        result.Body.Add($"    {cursorName}.Advance();");
        result.Body.Add($"    {sizeName}++;");
        if (_maxSize > 0)
        {
            result.Body.Add($"    if ({sizeName} == {_maxSize}) break;");
        }
        result.Body.Add("}");

        result.Body.Add($"if ({sizeName} < {_minSize})");
        result.Body.Add("{");
        result.Body.Add($"    {cursorName}.ResetPosition({startName});");
        result.Body.Add("}");
        result.Body.Add("else");
        result.Body.Add("{");
        result.Body.Add($"    var end{context.NextNumber()} = {cursorName}.Offset;");
        if (!context.DiscardResult)
        {
            result.Body.Add($"    {result.ValueVariable} = new global::Parlot.TextSpan({scannerName}.Buffer, {startName}.Offset, end{context.NextNumber() - 1} - {startName}.Offset);");
        }
        result.Body.Add($"    {result.SuccessVariable} = true;");
        result.Body.Add("}");

        return result;
    }
}
