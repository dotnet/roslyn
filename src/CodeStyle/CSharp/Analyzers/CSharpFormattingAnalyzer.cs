// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpFormattingAnalyzer : AbstractFormattingAnalyzer
    {
        private readonly EditorConfigOptionsApplier _editorConfigOptionsApplier = new EditorConfigOptionsApplier();

        protected override ISyntaxFormattingService SyntaxFormattingService
            => new CSharpSyntaxFormattingService();

        protected override OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext)
        {
            return _editorConfigOptionsApplier.ApplyConventions(optionSet, codingConventionContext.CurrentConventions);
        }
    }
}
