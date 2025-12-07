#if !AOT_COMPILATION
using Parlot.Compilation;
#endif
using System;
#if !AOT_COMPILATION
using System.Linq.Expressions;
using System.Reflection;
#endif

namespace Parlot.Fluent;

/// <summary>
/// Selects a parser instance at runtime and delegates parsing to it.
/// </summary>
/// <typeparam name="C">The concrete <see cref="ParseContext" /> type to use.</typeparam>
/// <typeparam name="T">The output parser type.</typeparam>
public sealed class Select<C, T> : Parser<T>
#if !AOT_COMPILATION
    , ICompilable
#endif
    where C : ParseContext
{
#if !AOT_COMPILATION
    private static readonly MethodInfo _parse = typeof(Parser<T>).GetMethod(nameof(Parse), [typeof(ParseContext), typeof(ParseResult<T>).MakeByRefType()])!;
#endif
    private readonly Func<C, Parser<T>> _selector;

    public Select(Func<C, Parser<T>> selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    public override bool Parse(ParseContext context, ref ParseResult<T> result)
    {
        context.EnterParser(this);

        var nextParser = _selector((C)context);

        if (nextParser == null)
        {
            context.ExitParser(this);
            return false;
        }

        var parsed = new ParseResult<T>();

        if (nextParser.Parse(context, ref parsed))
        {
            result.Set(parsed.Start, parsed.End, parsed.Value);

            context.ExitParser(this);
            return true;
        }

        context.ExitParser(this);
        return false;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        var result = context.CreateCompilationResult<T>();
        var parserVariable = Expression.Variable(typeof(Parser<T>), $"select{context.NextNumber}");
        var parseResult = Expression.Variable(typeof(ParseResult<T>), $"value{context.NextNumber}");

        var selectorInvoke = Expression.Invoke(
            Expression.Constant(_selector),
            Expression.Convert(context.ParseContext, typeof(C)));

        var body = Expression.Block(
            [parserVariable, parseResult],
            Expression.Assign(parserVariable, selectorInvoke),
            Expression.IfThen(
                Expression.NotEqual(parserVariable, Expression.Constant(null, typeof(Parser<T>))),
                Expression.Block(
                    Expression.Assign(
                        result.Success,
                        Expression.Call(parserVariable, _parse, context.ParseContext, parseResult)),
                    context.DiscardResult
                        ? Expression.Empty()
                        : Expression.IfThen(
                            result.Success,
                            Expression.Assign(result.Value, Expression.Field(parseResult, nameof(ParseResult<T>.Value))))
                )));

        result.Body.Add(body);

        return result;
    }
#endif
    public override string ToString() => "(Select)";
}
