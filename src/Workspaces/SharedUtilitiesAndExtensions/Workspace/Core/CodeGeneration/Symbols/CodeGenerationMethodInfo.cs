// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal class CodeGenerationMethodInfo
{
    private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationMethodInfo> s_methodToInfoMap = new();

    private readonly bool _isNew;
    private readonly bool _isUnsafe;
    private readonly bool _isPartial;
    private readonly bool _isAsync;
    private readonly ImmutableArray<SyntaxNode> _statements;
    private readonly ImmutableArray<SyntaxNode> _handlesExpressions;

    private CodeGenerationMethodInfo(
        bool isNew,
        bool isUnsafe,
        bool isPartial,
        bool isAsync,
        ImmutableArray<SyntaxNode> statements,
        ImmutableArray<SyntaxNode> handlesExpressions)
    {
        _isNew = isNew;
        _isUnsafe = isUnsafe;
        _isPartial = isPartial;
        _isAsync = isAsync;
        _statements = statements.NullToEmpty();
        _handlesExpressions = handlesExpressions.NullToEmpty();
    }

    public static void Attach(
        IMethodSymbol method,
        bool isNew,
        bool isUnsafe,
        bool isPartial,
        bool isAsync,
        ImmutableArray<SyntaxNode> statements,
        ImmutableArray<SyntaxNode> handlesExpressions)
    {
        var info = new CodeGenerationMethodInfo(isNew, isUnsafe, isPartial, isAsync, statements, handlesExpressions);
        s_methodToInfoMap.Add(method, info);
    }

    private static CodeGenerationMethodInfo GetInfo(IMethodSymbol method)
    {
        s_methodToInfoMap.TryGetValue(method, out var info);
        return info;
    }

    public static ImmutableArray<SyntaxNode> GetStatements(IMethodSymbol method)
        => GetStatements(GetInfo(method));

    public static ImmutableArray<SyntaxNode> GetHandlesExpressions(IMethodSymbol method)
        => GetHandlesExpressions(GetInfo(method));

    public static bool GetIsNew(IMethodSymbol method)
        => GetIsNew(GetInfo(method));

    public static bool GetIsUnsafe(IMethodSymbol method)
        => GetIsUnsafe(GetInfo(method));

    public static bool GetIsPartial(IMethodSymbol method)
        => GetIsPartial(GetInfo(method));

    public static bool GetIsAsyncMethod(IMethodSymbol method)
        => GetIsAsyncMethod(GetInfo(method));

    private static ImmutableArray<SyntaxNode> GetStatements(CodeGenerationMethodInfo info)
        => info?._statements ?? [];

    private static ImmutableArray<SyntaxNode> GetHandlesExpressions(CodeGenerationMethodInfo info)
        => info?._handlesExpressions ?? [];

    private static bool GetIsNew(CodeGenerationMethodInfo info)
        => info != null && info._isNew;

    private static bool GetIsUnsafe(CodeGenerationMethodInfo info)
        => info != null && info._isUnsafe;

    private static bool GetIsPartial(CodeGenerationMethodInfo info)
        => info != null && info._isPartial;

    private static bool GetIsAsyncMethod(CodeGenerationMethodInfo info)
        => info != null && info._isAsync;
}
