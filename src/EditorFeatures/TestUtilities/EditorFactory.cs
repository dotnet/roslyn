// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;

namespace Roslyn.Test.EditorUtilities;

public static class EditorFactory
{
    public static ITextBuffer2 CreateBuffer(
        ExportProvider exportProvider,
        params string[] lines)
    {
        var contentType = exportProvider.GetExportedValue<ITextBufferFactoryService>().TextContentType;

        return CreateBuffer(exportProvider, contentType, lines);
    }

    public static ITextBuffer2 CreateBuffer(
        ExportProvider exportProvider,
        IContentType contentType,
        params string[] lines)
    {
        var text = LinesToFullText(lines);

        // The overload of CreateTextBuffer that takes just a string doesn't initialize the whitespace tracking logic in the editor,
        // so calls to IIndentationManagerService won't work correctly. Tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1005541.
        using var reader = new StringReader(text);
        return (ITextBuffer2)exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(reader, contentType);
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

    public static DisposableTextView CreateView(
        ExportProvider exportProvider,
        IContentType contentType,
        ImmutableArray<string> roles)
    {
        WpfTestRunner.RequireWpfFact($"Creates an {nameof(IWpfTextView)} through {nameof(EditorFactory)}.{nameof(CreateView)}");

        var buffer = CreateBuffer(exportProvider, contentType);
        return exportProvider.GetExportedValue<ITextEditorFactoryService>().CreateDisposableTextView(buffer, roles);
    }

    public static string LinesToFullText(params string[] lines)
        => string.Join("\r\n", lines);
}
