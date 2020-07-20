' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ConvertTypeOfToNameOf), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ConvertTypeOfToNameOf)>
    Friend Class VisualBasicConvertGetTypeToNameOfCodeFixProvider
        Inherits AbstractConvertTypeOfToNameOfCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetCodeFixTitle(visualbasic As String, csharp As String) As String
            Return visualbasic
        End Function

        Protected Overrides Function GetSymbolType(semanticModel As SemanticModel, node As SyntaxNode) As ITypeSymbol
            Dim expression = DirectCast(node, MemberAccessExpressionSyntax).Expression
            Dim type = DirectCast(expression, GetTypeExpressionSyntax).Type
            Return semanticModel.GetSymbolInfo(type).Symbol.GetSymbolType()
        End Function
    End Class
End Namespace
