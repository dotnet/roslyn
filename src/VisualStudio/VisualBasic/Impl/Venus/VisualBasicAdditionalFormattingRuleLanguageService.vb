' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Venus
    <ExportLanguageService(GetType(IAdditionalFormattingRuleLanguageService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAdditionalFormattingRuleLanguageService
        Implements IAdditionalFormattingRuleLanguageService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function GetAdditionalCodeGenerationRule() As AbstractFormattingRule Implements IAdditionalFormattingRuleLanguageService.GetAdditionalCodeGenerationRule
            Return LineAdjustmentFormattingRule.Instance
        End Function
    End Class
End Namespace
