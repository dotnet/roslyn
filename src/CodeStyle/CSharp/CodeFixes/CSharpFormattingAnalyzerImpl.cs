// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal class CSharpFormattingAnalyzerImpl : AbstractFormattingAnalyzerImpl
    {
        private readonly EditorConfigOptionsApplier _editorConfigOptionsApplier = new EditorConfigOptionsApplier();

        public CSharpFormattingAnalyzerImpl(DiagnosticDescriptor descriptor)
            : base(descriptor)
        {
        }

        protected override OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext)
        {
            return _editorConfigOptionsApplier.ApplyConventions(optionSet, codingConventionContext.CurrentConventions, LanguageNames.CSharp);
        }
    }
}
