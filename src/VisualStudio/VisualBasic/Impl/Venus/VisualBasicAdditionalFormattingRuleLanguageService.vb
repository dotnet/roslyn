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
        Private Shared ReadOnly s_rule As New LineAdjustmentFormattingRule()
        Public Function GetAdditionalCodeGenerationRule() As IFormattingRule Implements IAdditionalFormattingRuleLanguageService.GetAdditionalCodeGenerationRule
            Return s_rule
        End Function
    End Class
End Namespace
