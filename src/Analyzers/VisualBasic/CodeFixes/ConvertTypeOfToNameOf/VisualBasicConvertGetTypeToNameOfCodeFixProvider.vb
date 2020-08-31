﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ConvertTypeOfToNameOf), [Shared]>
    Friend Class VisualBasicConvertGetTypeToNameOfCodeFixProvider
        Inherits AbstractConvertTypeOfToNameOfCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetCodeFixTitle() As String
            Return VisualBasicCodeFixesResources.Convert_GetType_to_NameOf
        End Function

        Protected Overrides Function GetSymbolTypeExpression(semanticModel As SemanticModel, node As SyntaxNode) As SyntaxNode

            Dim expression = DirectCast(node, MemberAccessExpressionSyntax).Expression
            Dim type = DirectCast(expression, GetTypeExpressionSyntax).Type
            Dim symbolType = semanticModel.GetSymbolInfo(type).Symbol.GetSymbolType()
            Dim symbolExpression = symbolType.GenerateExpressionSyntax()

            If TypeOf symbolExpression Is IdentifierNameSyntax OrElse TypeOf symbolExpression Is MemberAccessExpressionSyntax Then
                Return symbolExpression
            End If

            If TypeOf symbolExpression Is QualifiedNameSyntax Then
                Dim qualifiedName = DirectCast(symbolExpression, QualifiedNameSyntax)
                Return SyntaxFactory.SimpleMemberAccessExpression(qualifiedName.Left, qualifiedName.Right) _
                    .WithAdditionalAnnotations(Simplifier.Annotation)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
