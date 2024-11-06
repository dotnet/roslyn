// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationPropertyInfo
{
    private static readonly ConditionalWeakTable<IPropertySymbol, CodeGenerationPropertyInfo> s_propertyToInfoMap = new();

    private readonly bool _isNew;
    private readonly bool _isUnsafe;
    private readonly SyntaxNode _initializer;

    private CodeGenerationPropertyInfo(
        bool isNew,
        bool isUnsafe,
        SyntaxNode initializer)
    {
        _isNew = isNew;
        _isUnsafe = isUnsafe;
        _initializer = initializer;
    }

    public static void Attach(
        IPropertySymbol property,
        bool isNew,
        bool isUnsafe,
        SyntaxNode initializer)
    {
        var info = new CodeGenerationPropertyInfo(isNew, isUnsafe, initializer);
        s_propertyToInfoMap.Add(property, info);
    }

    private static CodeGenerationPropertyInfo GetInfo(IPropertySymbol property)
    {
        s_propertyToInfoMap.TryGetValue(property, out var info);
        return info;
    }

    public static SyntaxNode GetInitializer(CodeGenerationPropertyInfo info)
        => info?._initializer;

    public static SyntaxNode GetInitializer(IPropertySymbol property)
        => GetInitializer(GetInfo(property));

    public static bool GetIsNew(IPropertySymbol property)
        => GetIsNew(GetInfo(property));

    public static bool GetIsUnsafe(IPropertySymbol property)
        => GetIsUnsafe(GetInfo(property));

    private static bool GetIsNew(CodeGenerationPropertyInfo info)
        => info != null && info._isNew;

    private static bool GetIsUnsafe(CodeGenerationPropertyInfo info)
        => info != null && info._isUnsafe;
}
