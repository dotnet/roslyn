// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        protected override CSharpMethodExtractor CreateMethodExtractor(CSharpSelectionResult selectionResult, bool localFunction)
        {
            return new CSharpMethodExtractor(selectionResult, localFunction);
        }
    }
}
