' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Analyzers.FixAnalyzers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.FixAnalyzers

    ''' <summary>
    ''' A <see cref="CodeFixProvider"/> that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique across all registered code actions by this fixer.
    ''' This enables the <see cref="FixAllProvider"/> to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.
    ''' This analyzer catches violations of this requirement in the code actions registered by a fixer that supports <see cref="FixAllProvider"/>.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicFixerWithFixAllAnalyzer
        Inherits FixerWithFixAllAnalyzer(Of SyntaxKind)
        Protected Overrides Function GetCompilationAnalyzer(codeFixProviderSymbol As INamedTypeSymbol, getFixAllProvider As IMethodSymbol, codeActionSymbol As INamedTypeSymbol, createMethods As ImmutableHashSet(Of IMethodSymbol), equivalenceKeyProperty As IPropertySymbol) As CompilationAnalyzer
            Return New CSharpCompilationAnalyzer(codeFixProviderSymbol, getFixAllProvider, codeActionSymbol, createMethods, equivalenceKeyProperty)
        End Function

        Private NotInheritable Class CSharpCompilationAnalyzer
            Inherits CompilationAnalyzer
            Public Sub New(codeFixProviderSymbol As INamedTypeSymbol, getFixAllProvider As IMethodSymbol, codeActionSymbol As INamedTypeSymbol, createMethods As ImmutableHashSet(Of IMethodSymbol), equivalenceKeyProperty As IPropertySymbol)
                MyBase.New(codeFixProviderSymbol, getFixAllProvider, codeActionSymbol, createMethods, equivalenceKeyProperty)
            End Sub

            Protected Overrides ReadOnly Property GetInvocationKind As SyntaxKind
                Get
                    Return SyntaxKind.InvocationExpression
                End Get
            End Property

            Protected Overrides ReadOnly Property GetObjectCreationKind As SyntaxKind
                Get
                    Return SyntaxKind.ObjectCreationExpression
                End Get
            End Property

            Protected Overrides Function HasNonNullArgumentForParameter(node As SyntaxNode, parameter As IParameterSymbol, indexOfParameter As Integer, model As SemanticModel, cancellationToken As CancellationToken) As Boolean
                Dim invocation = DirectCast(node, InvocationExpressionSyntax)
                If invocation.ArgumentList Is Nothing Then
                    Return False
                End If

                Dim seenNamedArgument = False
                Dim indexOfArgument = 0
                For Each argument In invocation.ArgumentList.Arguments
                    If argument.IsNamed Then
                        seenNamedArgument = True
                        Dim simpleArgument = TryCast(argument, SimpleArgumentSyntax)
                        If simpleArgument IsNot Nothing AndAlso parameter.Name.Equals(simpleArgument.NameColonEquals.Name.Identifier.ValueText) Then
                            Return Not HasNullConstantValue(simpleArgument.Expression, model, cancellationToken)
                        End If
                    ElseIf Not seenNamedArgument Then
                        If indexOfArgument = indexOfParameter Then
                            Return Not HasNullConstantValue(argument.GetExpression, model, cancellationToken)
                        End If

                        indexOfArgument += 1
                    End If
                Next

                Return False
            End Function
        End Class
    End Class
End Namespace
