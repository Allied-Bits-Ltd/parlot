#if !AOT_COMPILATION
using Parlot.Compilation;
using System.Linq.Expressions;
#endif

namespace Parlot.Fluent;

/// <summary>
/// Doesn't parse anything and fails parsing.
/// </summary>
public sealed class Fail<T> : Parser<T>
#if !AOT_COMPILATION
    , ICompilable
#endif
{
    public Fail()
    {
        Name = "Fail";
    }

    public override bool Parse(ParseContext context, ref ParseResult<T> result)
    {
        context.EnterParser(this);

        context.ExitParser(this);
        return false;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        return context.CreateCompilationResult<T>(false, Expression.Constant(default(T), typeof(T)));
    }
#endif
}
