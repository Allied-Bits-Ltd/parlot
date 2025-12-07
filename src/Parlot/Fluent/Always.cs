#if !AOT_COMPILATION
using Parlot.Compilation;
using System.Linq.Expressions;
#endif

namespace Parlot.Fluent;

/// <summary>
/// Doesn't parse anything and return the default value.
/// </summary>
public sealed class Always<T> : Parser<T>
#if !AOT_COMPILATION
    , ICompilable
#endif
{
    private readonly T _value;

    public Always(T value)
    {
        Name = "Always";
        _value = value;
    }

    public override bool Parse(ParseContext context, ref ParseResult<T> result)
    {
        context.EnterParser(this);

        result.Set(context.Scanner.Cursor.Offset, context.Scanner.Cursor.Offset, _value);

        context.ExitParser(this);
        return true;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        return context.CreateCompilationResult<T>(true, Expression.Constant(_value, typeof(T)));
    }
#endif
}
