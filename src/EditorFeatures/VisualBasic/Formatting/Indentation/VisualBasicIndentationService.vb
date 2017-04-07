' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
    <ExportLanguageService(GetType(ISynchronousIndentationService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicIndentationService
        Inherits AbstractIndentationService

        Private Shared ReadOnly s_instance As IFormattingRule = New SpecialFormattingRule()

        Protected Overrides Function GetSpecializedIndentationFormattingRule() As IFormattingRule
            Return s_instance
        End Function

        Protected Overrides Function GetIndenter(syntaxFacts As ISyntaxFactsService,
                                                 syntaxTree As SyntaxTree,
                                                 lineToBeIndented As TextLine,
                                                 formattingRules As IEnumerable(Of IFormattingRule),
                                                 optionSet As OptionSet,
                                                 cancellationToken As CancellationToken) As AbstractIndenter
            Return New Indenter(syntaxFacts, syntaxTree, formattingRules, optionSet, lineToBeIndented, cancellationToken)
        End Function
    End Class
End Namespace