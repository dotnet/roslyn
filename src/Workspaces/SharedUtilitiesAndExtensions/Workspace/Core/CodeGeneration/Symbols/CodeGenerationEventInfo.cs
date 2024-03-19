// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal class CodeGenerationEventInfo
{
    private static readonly ConditionalWeakTable<IEventSymbol, CodeGenerationEventInfo> s_eventToInfoMap = new();

    private readonly bool _isUnsafe;
    private CodeGenerationEventInfo(bool isUnsafe)
        => _isUnsafe = isUnsafe;

    public static void Attach(IEventSymbol @event, bool isUnsafe)
    {
        var info = new CodeGenerationEventInfo(isUnsafe);
        s_eventToInfoMap.Add(@event, info);
    }

    private static CodeGenerationEventInfo GetInfo(IEventSymbol @event)
    {
        s_eventToInfoMap.TryGetValue(@event, out var info);
        return info;
    }

    public static bool GetIsUnsafe(IEventSymbol @event)
        => GetIsUnsafe(GetInfo(@event));

    private static bool GetIsUnsafe(CodeGenerationEventInfo info)
        => info != null && info._isUnsafe;
}
