' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetAdditionalCodeGenerationRule() As AbstractFormattingRule Implements IAdditionalFormattingRuleLanguageService.GetAdditionalCodeGenerationRule
            Return LineAdjustmentFormattingRule.Instance
        End Function
    End Class
End Namespace
