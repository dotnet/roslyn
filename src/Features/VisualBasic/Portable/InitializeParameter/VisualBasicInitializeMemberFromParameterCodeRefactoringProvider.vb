﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.InitializeParameter
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter), [Shared]>
    <ExtensionOrder(Before:=NameOf(VisualBasicAddParameterCheckCodeRefactoringProvider))>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.Wrapping)>
    Friend Class VisualBasicInitializeMemberFromParameterCodeRefactoringProvider
        Inherits AbstractInitializeMemberFromParameterCodeRefactoringProvider(Of
            TypeBlockSyntax,
            ParameterSyntax,
            StatementSyntax,
            ExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function IsFunctionDeclaration(node As SyntaxNode) As Boolean
            Return InitializeParameterHelpers.IsFunctionDeclaration(node)
        End Function

        Protected Overrides Function TryGetLastStatement(blockStatement As IBlockOperation) As SyntaxNode
            Return InitializeParameterHelpers.TryGetLastStatement(blockStatement)
        End Function

        Protected Overrides Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination)
        End Function

        Protected Overrides Sub InsertStatement(editor As SyntaxEditor, functionDeclaration As SyntaxNode, returnsVoid As Boolean, statementToAddAfterOpt As SyntaxNode, statement As StatementSyntax)
            InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, statementToAddAfterOpt, statement)
        End Sub

        ' Fields are public by default in VB, except in the case of classes and modules.
        Protected Overrides Function DetermineDefaultFieldAccessibility(containingType As INamedTypeSymbol) As Accessibility
            Return If(containingType.TypeKind = TypeKind.Class Or containingType.TypeKind = TypeKind.Module, Accessibility.Private, Accessibility.Public)
        End Function

        ' Properties are always public by default in VB.
        Protected Overrides Function DetermineDefaultPropertyAccessibility() As Accessibility
            Return Accessibility.Public
        End Function

        Protected Overrides Function GetBody(functionDeclaration As SyntaxNode) As SyntaxNode
            Return InitializeParameterHelpers.GetBody(functionDeclaration)
        End Function

        Protected Overrides Function GetAccessorBody(accessor As IMethodSymbol, cancellationToken As CancellationToken) As SyntaxNode
            If accessor.DeclaringSyntaxReferences.Length = 0 Then
                Return Nothing
            End If

            Dim reference = accessor.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken)
            Return TryCast(TryCast(reference, AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)
        End Function

        Protected Overrides Function RemoveThrowNotImplemented(propertySyntax As SyntaxNode) As SyntaxNode
            Dim propertyBlock = TryCast(propertySyntax, PropertyBlockSyntax)
            If propertyBlock IsNot Nothing Then
                Dim accessors = SyntaxFactory.List(propertyBlock.Accessors.Select(Function(a) RemoveThrowNotImplemented(a)))
                Return propertyBlock.WithAccessors(accessors)
            End If

            Return propertySyntax
        End Function

        Private Overloads Shared Function RemoveThrowNotImplemented(accessorBlock As AccessorBlockSyntax) As AccessorBlockSyntax
            Return accessorBlock.WithStatements(Nothing)
        End Function

        Protected Overrides Function TryUpdateTupleAssignment(blockStatement As IBlockOperation, parameter As IParameterSymbol, fieldOrProperty As ISymbol, editor As SyntaxEditor) As Boolean
            ' Not supported in VB
            Return False
        End Function
    End Class
End Namespace
