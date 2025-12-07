#if !AOT_COMPILATION
using Parlot.Compilation;

namespace Parlot.Fluent;

public interface ISkippableSequenceParser
{
    SkippableCompilationResult[] BuildSkippableParsers(CompilationContext context);
}

#endif
