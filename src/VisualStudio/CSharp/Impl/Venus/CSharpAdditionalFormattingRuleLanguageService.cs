// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Venus
{
    [ExportLanguageService(typeof(IAdditionalFormattingRuleLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpAdditionalFormattingRuleLanguageService : IAdditionalFormattingRuleLanguageService
    {
        [ImportingConstructor]
        public CSharpAdditionalFormattingRuleLanguageService()
        {
        }

        public AbstractFormattingRule GetAdditionalCodeGenerationRule()
        {
            return BlankLineInGeneratedMethodFormattingRule.Instance;
        }
    }
}
