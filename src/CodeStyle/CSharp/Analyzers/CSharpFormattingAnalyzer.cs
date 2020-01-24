﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
