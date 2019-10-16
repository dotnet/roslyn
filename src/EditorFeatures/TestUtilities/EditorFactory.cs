// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;

namespace Roslyn.Test.EditorUtilities
{
    public static class EditorFactory
    {
        public static ITextBuffer CreateBuffer(
            ExportProvider exportProvider,
            params string[] lines)
        {
            var contentType = exportProvider.GetExportedValue<ITextBufferFactoryService>().TextContentType;

            return CreateBuffer(exportProvider, contentType, lines);
        }

        public static ITextBuffer CreateBuffer(
            ExportProvider exportProvider,
            IContentType contentType,
            params string[] lines)
        {
            var text = LinesToFullText(lines);
            return exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(text, contentType);
        }

        public static DisposableTextView CreateView(
            ExportProvider exportProvider,
            params string[] lines)
        {
            var contentType = exportProvider.GetExportedValue<ITextBufferFactoryService>().TextContentType;
            return CreateView(exportProvider, contentType, lines);
        }

        public static DisposableTextView CreateView(
            ExportProvider exportProvider,
            IContentType contentType,
            params string[] lines)
        {
            WpfTestRunner.RequireWpfFact($"Creates an {nameof(IWpfTextView)} through {nameof(EditorFactory)}.{nameof(CreateView)}");

            var buffer = CreateBuffer(exportProvider, contentType, lines);
            return exportProvider.GetExportedValue<ITextEditorFactoryService>().CreateDisposableTextView(buffer);
        }

        public static string LinesToFullText(params string[] lines)
        {
            return string.Join("\r\n", lines);
        }
    }
}
