// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FixFormatting)]
    [Shared]
    internal class CSharpFormattingCodeFixProvider : AbstractFormattingCodeFixProvider
    {
        private readonly EditorConfigOptionsApplier _editorConfigOptionsApplier = new EditorConfigOptionsApplier();

        protected override ISyntaxFormattingService SyntaxFormattingService => new CSharpSyntaxFormattingService();

        protected override OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext)
        {
            return _editorConfigOptionsApplier.ApplyConventions(optionSet, codingConventionContext.CurrentConventions);
        }
    }
}
