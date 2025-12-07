#if !AOT_COMPILATION
using Parlot.Compilation;
#endif

using Parlot.Rewriting;
using System;
#if !AOT_COMPILATION
#if NET
using System.Linq;
#endif
using System.Linq.Expressions;
#endif

namespace Parlot.Fluent;

public sealed class ElseError<T> : Parser<T>
#if !AOT_COMPILATION
    , ICompilable
#endif
{
    private readonly Parser<T> _parser;
    private readonly string _message;

    public ElseError(Parser<T> parser, string message)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _message = message;
    }

    public override bool Parse(ParseContext context, ref ParseResult<T> result)
    {
        context.EnterParser(this);

        if (!_parser.Parse(context, ref result))
        {
            context.ExitParser(this);
            throw new ParseException(_message, context.Scanner.Cursor.Position);
        }

        context.ExitParser(this);
        return true;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        var result = context.CreateCompilationResult<T>(true);

        // parse1 instructions
        // success = true
        //
        // if (parser1.Success)
        // {
        //   value = parser1.Value
        // }
        // else
        // {
        //    throw new ParseException(_message, context.Scanner.Cursor.Position);
        // }
        //

        var parserCompileResult = _parser.Build(context, requireResult: true);

        var block = Expression.Block(
            parserCompileResult.Variables,
            parserCompileResult.Body
            .Append(
                context.DiscardResult
                        ? Expression.Empty()
                        : Expression.Assign(result.Value, parserCompileResult.Value))
                .Append(
                    Expression.IfThenElse(
                        parserCompileResult.Success,
                        context.DiscardResult
                            ? Expression.Empty()
                            : Expression.Assign(result.Value, parserCompileResult.Value),
                        context.ThrowParseException(Expression.Constant(_message))


                ))
        );

        result.Body.Add(block);

        return result;
    }
#endif

    public override string ToString() => $"{_parser} (ElseError)";
}

public sealed class Error<T> : Parser<T>
#if !AOT_COMPILATION
    , ICompilable
#endif
{
    private readonly Parser<T> _parser;
    private readonly string _message;

    public Error(Parser<T> parser, string message)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _message = message;
    }

    public override bool Parse(ParseContext context, ref ParseResult<T> result)
    {
        context.EnterParser(this);

        if (_parser.Parse(context, ref result))
        {
            context.ExitParser(this);
            throw new ParseException(_message, context.Scanner.Cursor.Position);
        }

        context.ExitParser(this);
        return false;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        var result = context.CreateCompilationResult<T>();

        // parse1 instructions
        // success = false;
        //
        // if (parser1.Success)
        // {
        //    value = parser1.Value;
        //    throw new ParseException(_message, context.Scanner.Cursor.Position);
        // }

        var parserCompileResult = _parser.Build(context, requireResult: false);

        var block = Expression.Block(
            parserCompileResult.Variables,
            parserCompileResult.Body
                .Append(
                    Expression.IfThen(
                        parserCompileResult.Success,
                        context.ThrowParseException(Expression.Constant(_message))
                    )
                )
        );

        result.Body.Add(block);

        return result;
    }
#endif

    public override string ToString() => $"{_parser} (Error)";
}

public sealed class Error<T, U> : Parser<U>,
#if !AOT_COMPILATION
    ICompilable,
#endif
    ISeekable
{
    private readonly Parser<T> _parser;
    private readonly string _message;

    public bool CanSeek { get; }

    public char[] ExpectedChars { get; } = [];

    public bool SkipWhitespace { get; }

    public Error(Parser<T> parser, string message)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _message = message;

        if (_parser is ISeekable seekable)
        {
            CanSeek = seekable.CanSeek;
            ExpectedChars = seekable.ExpectedChars;
            SkipWhitespace = seekable.SkipWhitespace;
        }
    }

    public override bool Parse(ParseContext context, ref ParseResult<U> result)
    {
        context.EnterParser(this);

        var parsed = new ParseResult<T>();

        if (_parser.Parse(context, ref parsed))
        {
            context.ExitParser(this);
            throw new ParseException(_message, context.Scanner.Cursor.Position);
        }

        context.ExitParser(this);
        return false;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        var result = context.CreateCompilationResult<U>();

        // parse1 instructions
        // success = false;
        //
        // if (parser1.Success)
        // {
        //    throw new ParseException(_message, context.Scanner.Cursor.Position);
        // }

        var parserCompileResult = _parser.Build(context, requireResult: false);

        var block = Expression.Block(
            parserCompileResult.Variables,
            parserCompileResult.Body
                .Append(
                    Expression.IfThen(
                        parserCompileResult.Success,
                        context.ThrowParseException(Expression.Constant(_message))
                    )
                )
        );

        result.Body.Add(block);

        return result;
    }
#endif

    public override string ToString() => $"{_parser} (Error)";
}
