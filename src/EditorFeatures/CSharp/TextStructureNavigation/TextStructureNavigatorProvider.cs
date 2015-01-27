// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.TextStructureNavigation
{
    [Export(typeof(ITextStructureNavigatorProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class TextStructureNavigatorProvider : AbstractTextStructureNavigatorProvider
    {
        [ImportingConstructor]
        internal TextStructureNavigatorProvider(
            ITextStructureNavigatorSelectorService selectorService,
            IContentTypeRegistryService contentTypeService,
            IWaitIndicator waitIndicator)
            : base(selectorService, contentTypeService, waitIndicator)
        {
        }

        protected override bool ShouldSelectEntireTriviaFromStart(SyntaxTrivia trivia)
        {
            return trivia.IsRegularOrDocComment();
        }

        protected override bool IsWithinNaturalLanguage(SyntaxToken token, int position)
        {
            switch (token.Kind())
            {
                case SyntaxKind.StringLiteralToken:
                    // Before the " is considered outside the string
                    // TODO: does this handle verbatim strings correctly?
                    return position > token.SpanStart;

                case SyntaxKind.CharacterLiteralToken:
                    // Before the ' is considered outside the character
                    return position != token.SpanStart;

                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.XmlTextLiteralToken:
                    return true;
            }

            return false;
        }
    }
}
