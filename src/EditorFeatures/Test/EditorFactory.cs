// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Test.EditorUtilities
{
    public static class EditorFactory
    {
        public static ITextBuffer CreateBuffer(
            ExportProvider exportProvider,
            params string[] lines)
        {
            return CreateBuffer("text", exportProvider, lines);
        }

        public static ITextBuffer CreateBuffer(
            string contentType,
            ExportProvider exportProvider,
            params string[] lines)
        {
            var text = LinesToFullText(lines);
            var intContentType = exportProvider.GetExportedValue<IContentTypeRegistryService>().GetContentType(contentType);
            var buffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(intContentType);
            buffer.Replace(new Span(0, 0), text);
            return buffer;
        }

        public static IWpfTextView CreateView(
            ExportProvider exportProvider,
            params string[] lines)
        {
            return CreateView("text", exportProvider, lines);
        }

        public static IWpfTextView CreateView(
            string contentType,
            ExportProvider exportProvider,
            params string[] lines)
        {
            TestWorkspace.ResetThreadAffinity();

            var buffer = CreateBuffer(contentType, exportProvider, lines);
            return exportProvider.GetExportedValue<ITextEditorFactoryService>().CreateTextView(buffer);
        }

        public static string LinesToFullText(params string[] lines)
        {
            var builder = new StringBuilder();
            var isFirst = true;
            foreach (var line in lines)
            {
                if (!isFirst)
                {
                    builder.AppendLine();
                }

                isFirst = false;
                builder.Append(line);
            }

            return builder.ToString();
        }
    }
}
