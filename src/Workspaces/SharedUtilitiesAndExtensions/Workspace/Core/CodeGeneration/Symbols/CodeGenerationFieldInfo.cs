// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationFieldInfo
{
    private static readonly ConditionalWeakTable<IFieldSymbol, CodeGenerationFieldInfo> s_fieldToInfoMap = new();

    private readonly bool _isUnsafe;
    private readonly bool _isWithEvents;
    private readonly SyntaxNode _initializer;

    private CodeGenerationFieldInfo(
        bool isUnsafe,
        bool isWithEvents,
        SyntaxNode initializer)
    {
        _isUnsafe = isUnsafe;
        _isWithEvents = isWithEvents;
        _initializer = initializer;
    }

    public static void Attach(
        IFieldSymbol field,
        bool isUnsafe,
        bool isWithEvents,
        SyntaxNode initializer)
    {
        var info = new CodeGenerationFieldInfo(isUnsafe, isWithEvents, initializer);
        s_fieldToInfoMap.Add(field, info);
    }

    private static CodeGenerationFieldInfo GetInfo(IFieldSymbol field)
    {
        s_fieldToInfoMap.TryGetValue(field, out var info);
        return info;
    }

    private static bool GetIsUnsafe(CodeGenerationFieldInfo info)
        => info != null && info._isUnsafe;

    public static bool GetIsUnsafe(IFieldSymbol field)
        => GetIsUnsafe(GetInfo(field));

    private static bool GetIsWithEvents(CodeGenerationFieldInfo info)
        => info != null && info._isWithEvents;

    public static bool GetIsWithEvents(IFieldSymbol field)
        => GetIsWithEvents(GetInfo(field));

    private static SyntaxNode GetInitializer(CodeGenerationFieldInfo info)
        => info?._initializer;

    public static SyntaxNode GetInitializer(IFieldSymbol field)
        => GetInitializer(GetInfo(field));
}
