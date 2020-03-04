// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IEditorOptionsFactoryServiceExtensions
    {
        public static IEditorOptions GetEditorOptions(this IEditorOptionsFactoryService editorOptionsFactory, SourceText text)
        {
            var textBuffer = text.Container.TryGetTextBuffer();
            if (textBuffer != null)
            {
                return editorOptionsFactory.GetOptions(textBuffer);
            }

            return editorOptionsFactory.GlobalOptions;
        }

        public static IEditorOptions GetEditorOptions(this IEditorOptionsFactoryService editorOptionsFactory, Document document)
        {
            if (document.TryGetText(out var text))
            {
                return editorOptionsFactory.GetEditorOptions(text);
            }

            return editorOptionsFactory.GlobalOptions;
        }

        // This particular section is commented for future reference if there arises a need to implement a option serializer in the editor layer
        // public static IOptionService GetFormattingOptions(this IEditorOptionsFactoryService editorOptionsFactory, Document document)
        // {
        //     return CreateOptions(editorOptionsFactory.GetEditorOptions(document));
        // }

        // private static IOptionService CreateOptions(IEditorOptions editorOptions)
        // {
        //     return new FormattingOptions(
        //        !editorOptions.IsConvertTabsToSpacesEnabled(),
        //        editorOptions.GetTabSize(),
        //        editorOptions.GetIndentSize());
        // }
    }
}
