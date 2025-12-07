#if !AOT_COMPILATION
using Parlot.Compilation;
using System.Linq.Expressions;
#endif
using System;

namespace Parlot.Fluent;

public sealed class Not<T> : Parser<T>
#if !AOT_COMPILATION
    , ICompilable
#endif
{
    private readonly Parser<T> _parser;

    public Not(Parser<T> parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public override bool Parse(ParseContext context, ref ParseResult<T> result)
    {
        context.EnterParser(this);

        var start = context.Scanner.Cursor.Position;

        if (!_parser.Parse(context, ref result))
        {
            context.ExitParser(this);
            return true;
        }

        context.Scanner.Cursor.ResetPosition(start);

        context.ExitParser(this);
        return false;
    }

#if !AOT_COMPILATION
    public CompilationResult Compile(CompilationContext context)
    {
        var result = context.CreateCompilationResult<T>();

        // var start = context.Scanner.Cursor.Position;

        var start = context.DeclarePositionVariable(result);

        var parserCompileResult = _parser.Build(context);

        // success = false;
        //
        // parser instructions
        //
        // if (parser.succcess)
        // {
        //     context.Scanner.Cursor.ResetPosition(start);
        // }
        // else
        // {
        //     success = true;
        // }
        //

        result.Body.Add(
            Expression.Block(
                parserCompileResult.Variables,
                Expression.Block(parserCompileResult.Body),
                Expression.IfThenElse(
                    parserCompileResult.Success,
                    context.ResetPosition(start),
                    Expression.Assign(result.Success, Expression.Constant(true, typeof(bool)))
                    )
                )
            );

        return result;
    }
#endif
    public override string ToString() => $"Not ({_parser})";
}
