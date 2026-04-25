// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Components;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal readonly struct BuilderVariableName(int index) : IWriteableValue
{
    public static BuilderVariableName Default => new(1);

    public int Index { get; } = index;

    public int Length => Index switch
    {
        1 => ComponentsApi.RenderTreeBuilder.BuilderParameter.Length,
        _ => ComponentsApi.RenderTreeBuilder.BuilderParameter.Length + Index.CountDigits()
    };

    public void WriteTo(CodeWriter writer)
    {
        if (Index == 1)
        {
            writer.Write(ComponentsApi.RenderTreeBuilder.BuilderParameter);
        }
        else
        {
            writer.Write(ComponentsApi.RenderTreeBuilder.BuilderParameter);
            writer.WriteIntegerLiteral(Index);
        }
    }

    internal string GetDebuggerDisplay()
        => Index == 1
            ? ComponentsApi.RenderTreeBuilder.BuilderParameter
            : $"{ComponentsApi.RenderTreeBuilder.BuilderParameter}{Index}";
}
