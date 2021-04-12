' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

' This is deliberately declared in the global namespace so that it will always be discoverable (regardless of Imports).
Friend Module Extensions

    ''' <summary>
    ''' This method is provided as a convenience for testing the SemanticModel.GetDeclaredSymbol implementation.
    ''' </summary>
    ''' <param name="node">This parameter will be type checked, and a NotSupportedException will be thrown if the type is not currently supported by an overload of GetDeclaredSymbol.</param>
    <Extension()>
    Friend Function GetDeclaredSymbolFromSyntaxNode(model As SemanticModel, node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As Symbol
        If Not (
            TypeOf node Is AggregationRangeVariableSyntax OrElse
            TypeOf node Is AnonymousObjectCreationExpressionSyntax OrElse
            TypeOf node Is SimpleImportsClauseSyntax OrElse
            TypeOf node Is CatchStatementSyntax OrElse
            TypeOf node Is CollectionRangeVariableSyntax OrElse
            TypeOf node Is EnumBlockSyntax OrElse
            TypeOf node Is EnumMemberDeclarationSyntax OrElse
            TypeOf node Is EnumStatementSyntax OrElse
            TypeOf node Is EventBlockSyntax OrElse
            TypeOf node Is ExpressionRangeVariableSyntax OrElse
            TypeOf node Is ForEachStatementSyntax OrElse
            TypeOf node Is FieldInitializerSyntax OrElse
            TypeOf node Is ForStatementSyntax OrElse
            TypeOf node Is LabelStatementSyntax OrElse
            TypeOf node Is MethodBaseSyntax OrElse
            TypeOf node Is MethodBlockSyntax OrElse
            TypeOf node Is ModifiedIdentifierSyntax OrElse
            TypeOf node Is NamespaceBlockSyntax OrElse
            TypeOf node Is NamespaceStatementSyntax OrElse
            TypeOf node Is ParameterSyntax OrElse
            TypeOf node Is PropertyBlockSyntax OrElse
            TypeOf node Is PropertyStatementSyntax OrElse
            TypeOf node Is TypeBlockSyntax OrElse
            TypeOf node Is TypeParameterSyntax OrElse
            TypeOf node Is TypeStatementSyntax) _
        Then
            Throw New NotSupportedException("This node type is not supported.")
        End If

        Return DirectCast(model.GetDeclaredSymbol(node, cancellationToken), Symbol)
    End Function

End Module
