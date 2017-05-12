// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.Options
{
    public sealed class EditorConfigSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
    {
        public ImmutableArray<AdditionalText> EditorConfigFiles { get; }

        public EditorConfigSyntaxTreeOptionsProvider(ImmutableArray<AdditionalText> editorConfigFiles)
        {
            EditorConfigFiles = editorConfigFiles.NullToEmpty();
        }

        public EditorConfigSyntaxTreeOptionsProvider AddEditorConfigFiles(IEnumerable<AdditionalText> editorConfigFiles)
        {
            return new EditorConfigSyntaxTreeOptionsProvider(this.EditorConfigFiles.AddRange(editorConfigFiles));
        }

        public EditorConfigSyntaxTreeOptionsProvider RemoveEditorConfigFiles(IEnumerable<AdditionalText> editorConfigFiles)
        {
            return new EditorConfigSyntaxTreeOptionsProvider(this.EditorConfigFiles.RemoveRange(editorConfigFiles));
        }
    }
}
