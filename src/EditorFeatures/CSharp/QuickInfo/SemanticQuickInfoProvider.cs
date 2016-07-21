// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.CodeAnalysis.Editor.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(PredefinedQuickInfoProviderNames.Semantic, LanguageNames.CSharp)]
    internal class SemanticQuickInfoProvider : AbstractSemanticQuickInfoProvider
    {
        [ImportingConstructor]
        public SemanticQuickInfoProvider(
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IGlyphService glyphService,
            ClassificationTypeMap typeMap)
            : base(projectionBufferFactoryService, editorOptionsFactoryService,
                   textEditorFactoryService, glyphService, typeMap)
        {
        }

        protected override bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return !token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
        }
    }
}
