// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using static Microsoft.AspNetCore.Razor.Language.CodeGeneration.CodeWriterExtensions;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed partial class ScopeStack
{
    private sealed class Entry : IDisposable
    {
        private readonly int _builderIndex;
        private readonly CSharpCodeWritingScope _scope;

        private int _renderModeIndex;
        private int _formNameIndex;

        public BuilderVariableName BuilderVariableName => new(_builderIndex);
        public RenderModeVariableName RenderModeVariableName => new(_renderModeIndex, _builderIndex);
        public FormNameVariableName FormNameVariableName => new(_formNameIndex, _builderIndex);

        private Entry(int builderIndex, CodeRenderingContext? context)
        {
            _builderIndex = builderIndex;

            if (context is not null)
            {
                _scope = context.CodeWriter.BuildLambda(BuilderVariableName);
            }
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        public static Entry CreateFirst()
            => new(1, context: null);

        public Entry Next(CodeRenderingContext context)
            => new(_builderIndex + 1, context);

        public void IncrementRenderMode()
        {
            _renderModeIndex++;
        }

        public void IncrementFormName()
        {
            _formNameIndex++;
        }
    }
}
