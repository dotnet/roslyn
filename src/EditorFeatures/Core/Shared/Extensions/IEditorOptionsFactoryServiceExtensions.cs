// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            SourceText text;
            if (document.TryGetText(out text))
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
