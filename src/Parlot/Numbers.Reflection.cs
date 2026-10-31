using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Parlot;

// Runtime grammar construction and the generic delegate path are not emitted into
// standalone consumers, which call the statically dispatched helpers in Numbers.cs.
public static partial class Numbers
{
#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse<TNumber>(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out TNumber value)
        where TNumber : INumber<TNumber>
    {
        // INumberBase<TSelf>.TryParse accepts NumberStyles, so the number options are honored without reflection.
#pragma warning disable CS8601 // Possible null reference assignment.
        return TNumber.TryParse(s, style, provider, out value);
#pragma warning restore CS8601
    }
#endif

    // The overloads every build of Numbers.TryParse provides, which a downlevel consumer can call
    internal static bool HasTryParseOverload(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal)
            || type == typeof(BigInteger);
    }

    internal static bool HasTryParseRadixOverload(Type type)
    {
        return type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(BigInteger);
    }

    /// <summary>
    /// Gets the Numbers.TryParse method for a specific numeric type.
    /// </summary>
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Looks up the TryParse methods of the type by reflection.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("May create a generic method instantiation at run time.")]
#endif
    internal static MethodInfo GetTryParseMethod(Type type)
    {
        var method = typeof(Numbers).GetMethod(
            nameof(TryParse),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(ReadOnlySpan<char>), typeof(NumberStyles), typeof(IFormatProvider), type.MakeByRefType() },
            modifiers: null);

#if NET8_0_OR_GREATER
        if (method is null && ImplementsINumber(type))
        {
            method = _genericTryParseMethod?.MakeGenericMethod(type);
        }
#endif

        return method ?? throw new NotSupportedException($"Numbers.TryParse is not available for type '{type}'.");
    }

#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Looks up the TryParse methods of the type by reflection.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("May create a generic method instantiation at run time.")]
#endif
    internal static MethodInfo GetTryParseMethod<T>()
        => GetTryParseMethod(typeof(T));

#if NET8_0_OR_GREATER
    private static readonly MethodInfo? _genericTryParseMethod = typeof(Numbers)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(m => m.IsGenericMethodDefinition && m.Name == nameof(TryParse) && m.GetParameters().Length == 4);

#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Reads the interfaces of the type by reflection.")]
#endif
    private static bool ImplementsINumber(Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INumber<>));
#endif
}
