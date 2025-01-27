// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationConstructorInfo
{
    private static readonly ConditionalWeakTable<IMethodSymbol, CodeGenerationConstructorInfo> s_constructorToInfoMap = new();

    private readonly bool _isPrimaryConstructor;
    private readonly bool _isUnsafe;
    private readonly string _typeName;
    private readonly ImmutableArray<SyntaxNode> _baseConstructorArguments;
    private readonly ImmutableArray<SyntaxNode> _thisConstructorArguments;
    private readonly ImmutableArray<SyntaxNode> _statements;

    private CodeGenerationConstructorInfo(
        bool isPrimaryConstructor,
        bool isUnsafe,
        string typeName,
        ImmutableArray<SyntaxNode> statements,
        ImmutableArray<SyntaxNode> baseConstructorArguments,
        ImmutableArray<SyntaxNode> thisConstructorArguments)
    {
        _isPrimaryConstructor = isPrimaryConstructor;
        _isUnsafe = isUnsafe;
        _typeName = typeName;
        _statements = statements;
        _baseConstructorArguments = baseConstructorArguments;
        _thisConstructorArguments = thisConstructorArguments;
    }

    public static void Attach(
        IMethodSymbol constructor,
        bool isPrimaryConstructor,
        bool isUnsafe,
        string typeName,
        ImmutableArray<SyntaxNode> statements,
        ImmutableArray<SyntaxNode> baseConstructorArguments,
        ImmutableArray<SyntaxNode> thisConstructorArguments)
    {
        var info = new CodeGenerationConstructorInfo(isPrimaryConstructor, isUnsafe, typeName, statements, baseConstructorArguments, thisConstructorArguments);
        s_constructorToInfoMap.Add(constructor, info);
    }

    private static CodeGenerationConstructorInfo? GetInfo(IMethodSymbol method)
    {
        s_constructorToInfoMap.TryGetValue(method, out var info);
        return info;
    }

    public static ImmutableArray<SyntaxNode> GetThisConstructorArgumentsOpt(IMethodSymbol constructor)
        => GetThisConstructorArgumentsOpt(GetInfo(constructor));

    public static ImmutableArray<SyntaxNode> GetBaseConstructorArgumentsOpt(IMethodSymbol constructor)
        => GetBaseConstructorArgumentsOpt(GetInfo(constructor));

    public static ImmutableArray<SyntaxNode> GetStatements(IMethodSymbol constructor)
        => GetStatements(GetInfo(constructor));

    public static string GetTypeName(IMethodSymbol constructor)
        => GetTypeName(GetInfo(constructor), constructor);

    public static bool GetIsUnsafe(IMethodSymbol constructor)
        => GetIsUnsafe(GetInfo(constructor));

    public static bool GetIsPrimaryConstructor(IMethodSymbol constructor)
        => GetIsPrimaryConstructor(GetInfo(constructor));

    private static ImmutableArray<SyntaxNode> GetThisConstructorArgumentsOpt(CodeGenerationConstructorInfo? info)
        => info?._thisConstructorArguments ?? default;

    private static ImmutableArray<SyntaxNode> GetBaseConstructorArgumentsOpt(CodeGenerationConstructorInfo? info)
        => info?._baseConstructorArguments ?? default;

    private static ImmutableArray<SyntaxNode> GetStatements(CodeGenerationConstructorInfo? info)
        => info?._statements ?? default;

    private static string GetTypeName(CodeGenerationConstructorInfo? info, IMethodSymbol constructor)
        => info == null ? constructor.ContainingType.Name : info._typeName;

    private static bool GetIsUnsafe(CodeGenerationConstructorInfo? info)
        => info?._isUnsafe ?? false;

    private static bool GetIsPrimaryConstructor(CodeGenerationConstructorInfo? info)
        => info?._isPrimaryConstructor ?? false;
}
