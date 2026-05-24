// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Components;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal readonly struct FormNameVariableName(int index, int builderIndex) : IWriteableValue
{
    public static FormNameVariableName Default => new(0, 1);

    public int Index { get; } = index;
    public int BuilderIndex { get; } = builderIndex;

    public void WriteTo(CodeWriter writer)
    {
        if (BuilderIndex == 1 && Index == 0)
        {
            writer.Write(ComponentsApi.RenderTreeBuilder.FormNameVariableName);
        }
        else
        {
            writer.Write(ComponentsApi.RenderTreeBuilder.FormNameVariableName);
            writer.WriteIntegerLiteral(BuilderIndex);
            writer.Write("_");
            writer.WriteIntegerLiteral(Index);
        }
    }

    internal string GetDebuggerDisplay()
        => BuilderIndex == 1 && Index == 0
            ? ComponentsApi.RenderTreeBuilder.FormNameVariableName
            : $"{ComponentsApi.RenderTreeBuilder.FormNameVariableName}{BuilderIndex}_{Index}";
}
