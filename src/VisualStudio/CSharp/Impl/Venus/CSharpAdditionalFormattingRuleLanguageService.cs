// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Venus;

[ExportLanguageService(typeof(IAdditionalFormattingRuleLanguageService), LanguageNames.CSharp), Shared]
internal sealed class CSharpAdditionalFormattingRuleLanguageService : IAdditionalFormattingRuleLanguageService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpAdditionalFormattingRuleLanguageService()
    {
    }

    public AbstractFormattingRule GetAdditionalCodeGenerationRule()
        => BlankLineInGeneratedMethodFormattingRule.Instance;
}
