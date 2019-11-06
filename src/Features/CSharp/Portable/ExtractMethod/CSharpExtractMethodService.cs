// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    [Export(typeof(IExtractMethodService)), Shared]
    [ExportLanguageService(typeof(IExtractMethodService), LanguageNames.CSharp)]
    internal class CSharpExtractMethodService : AbstractExtractMethodService<CSharpSelectionValidator, CSharpMethodExtractor, CSharpSelectionResult>
    {
        [ImportingConstructor]
        public CSharpExtractMethodService()
        {
        }

        protected override CSharpSelectionValidator CreateSelectionValidator(SemanticDocument document, TextSpan textSpan, OptionSet options)
        {
            return new CSharpSelectionValidator(document, textSpan, options);
        }

        protected override CSharpMethodExtractor CreateMethodExtractor(CSharpSelectionResult selectionResult, bool extractLocalFunction, bool preferStatic)
        {
            return new CSharpMethodExtractor(selectionResult, extractLocalFunction, preferStatic);
        }
    }
}
