' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryImports

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryImportsDiagnosticAnalyzer
        Inherits RemoveUnnecessaryImportsDiagnosticAnalyzerBase
        Private Shared ReadOnly s_TitleAndMessageFormat As LocalizableString =
            New LocalizableResourceString(NameOf(VBFeaturesResources.RemoveUnnecessaryImportsDiagnosticTitle), VBFeaturesResources.ResourceManager, GetType(VBFeaturesResources.VBFeaturesResources))

        Protected Overrides Function GetTitleAndMessageFormatForClassificationIdDescriptor() As LocalizableString
            Return s_TitleAndMessageFormat
        End Function

        Protected Overrides Function GetUnnecessaryImports(semanticModel As SemanticModel, root As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of SyntaxNode)
            Return VisualBasicRemoveUnnecessaryImportsService.GetUnnecessaryImports(semanticModel, root, cancellationToken)
        End Function

        Protected Overrides Function GetFixableDiagnosticSpans(nodes As IEnumerable(Of SyntaxNode), tree As SyntaxTree, Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of TextSpan)
            ' Create one fixable diagnostic that contains the entire Imports list.
            Return SpecializedCollections.SingletonEnumerable(Of TextSpan)(tree.GetCompilationUnitRoot().Imports.GetContainedSpan())
        End Function

        Protected Overrides Function GetLastTokenDelegateForContiguousSpans() As Func(Of SyntaxNode, SyntaxToken)
            Return Function(n)
                       Dim lastToken = n.GetLastToken()
                       Return If(lastToken.GetNextToken().Kind = SyntaxKind.CommaToken,
                              lastToken.GetNextToken(),
                              lastToken)
                   End Function
        End Function
    End Class
End Namespace
