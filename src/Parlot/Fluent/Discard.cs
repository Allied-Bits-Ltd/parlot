#if !AOT_COMPILATION
using Parlot.Compilation;
using System.Linq.Expressions;
#endif
using System;

namespace Parlot.Fluent;

/// <summary>
/// Doesn't parse anything and return the default value.
/// </summary>
[Obsolete("Use the Then parser instead.")]
public sealed class Discard<T, U> : Parser<U>
#if !AOT_COMPILATION
    , ICompilable
#endif
{
    private readonly Parser<T> _parser;
    private readonly U _value;

    public Discard(Parser<T> parser, U value)
    {
        _parser = parser;
        _value = value;
    }

    public override bool Parse(ParseContext context, ref ParseResult<U> result)
    {
        context.EnterParser(this);

        var parsed = new ParseResult<T>();

        if (_parser.Parse(context, ref parsed))
        {
            result.Set(parsed.Start, parsed.End, _value);

            context.ExitParser(this);
            return true;
        }

        context.ExitParser(this);
        return false;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        var result = context.CreateCompilationResult<U>(false, Expression.Constant(_value, typeof(U)));

        var parserCompileResult = _parser.Build(context);

        // success = false;
        // value = _value;
        //
        // parser instructions
        //
        // success = parser.success;

        result.Body.Add(
            Expression.Block(
                parserCompileResult.Variables,
                Expression.Block(parserCompileResult.Body),
                Expression.Assign(result.Success, parserCompileResult.Success)
                )
            );

        return result;
    }
#endif
    public override string ToString() => $"{_parser} (Discard)";
}
