' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Microsoft.CodeAnalysis.Performance

    ''' <summary>Provides a code fix for the EmptyArrayDiagnosticAnalyzer.</summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:="BasicEmptyArrayCodeFixProvider"), [Shared]>
    Public NotInheritable Class BasicEmptyArrayCodeFixProvider
        Inherits CodeFixProviderBase

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(RoslynDiagnosticIds.UseArrayEmptyRuleId)
            End Get
        End Property

        Protected Overrides Function GetCodeFixDescription(ruleId As String) As String
            Debug.Assert(ruleId = EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor.Id)
            Return EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor.Description.ToString(CultureInfo.CurrentUICulture)
        End Function

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Friend Overrides Function GetUpdatedDocumentAsync(document As Document, model As SemanticModel, root As SyntaxNode, nodeToFix As SyntaxNode, diagnosticId As String, cancellationToken As CancellationToken) As Task(Of Document)
            ' Get the type of the array being fixed.
            Dim elementType As TypeSyntax = GetArrayElementType(nodeToFix)

            ' Then replace the array creation with a call to Array.Empty(Of T) Or SpecializedCollections.EmptyArray(Of T).
            If elementType IsNot Nothing Then
                Dim syntax As InvocationExpressionSyntax = InvokeStaticGenericParameterlessMethod(
                    model.Compilation.GetTypeByMetadataName(BasicEmptyArrayDiagnosticAnalyzer.ArrayTypeName),
                    EmptyArrayDiagnosticAnalyzer.ArrayEmptyMethodName,
                    elementType.WithLeadingTrivia(SyntaxFactory.WhitespaceTrivia(" ")).WithoutTrailingTrivia())

                If nodeToFix.HasLeadingTrivia Then
                    syntax = syntax.WithLeadingTrivia(nodeToFix.GetLeadingTrivia())
                End If
                If nodeToFix.HasTrailingTrivia Then
                    syntax = syntax.WithTrailingTrivia(nodeToFix.GetTrailingTrivia())
                End If

                If syntax IsNot Nothing Then
                    root = root.ReplaceNode(nodeToFix, syntax)
                    document = document.WithSyntaxRoot(root)
                End If
            End If

            ' Return the (potentially New) document.
            Return Task.FromResult(document)
        End Function


        ''' <summary>Gets the TypeSyntax from a syntax node representing an empty array construction's element type.</summary>
        ''' <param name="nodeToFix">The syntax node.</param>
        ''' <returns>The TypeSyntax if it could be extracted; otherwise, null.</returns>
        Private Shared Function GetArrayElementType(nodeToFix As SyntaxNode) As TypeSyntax
            Dim aces As ArrayCreationExpressionSyntax = TryCast(nodeToFix, ArrayCreationExpressionSyntax)
            If aces IsNot Nothing Then
                Return If(aces.RankSpecifiers.Count > 0, SyntaxFactory.ArrayType(aces.Type, aces.RankSpecifiers), aces.Type)
            End If

            Dim sn As SyntaxNode = TryCast(nodeToFix, CollectionInitializerSyntax)
            Dim i As Integer = 0
            While i < 2 AndAlso sn IsNot Nothing
                i = i + 1
                sn = sn.Parent
            End While

            Dim vds As VariableDeclaratorSyntax = TryCast(sn, VariableDeclaratorSyntax)
            If vds IsNot Nothing AndAlso vds.AsClause IsNot Nothing Then
                Dim arrayType As ArrayTypeSyntax = TryCast(vds.AsClause.Type, ArrayTypeSyntax)
                If arrayType IsNot Nothing AndAlso arrayType.RankSpecifiers.Count >= 1 AndAlso arrayType.RankSpecifiers(0).Rank = 1 Then
                    Return If(arrayType.RankSpecifiers.Count > 1,
                        SyntaxFactory.ArrayType(arrayType.ElementType, SyntaxFactory.List(arrayType.RankSpecifiers.Skip(1))),
                        arrayType.ElementType)
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>Create an invocation expression for typeSymbol.methodName&lt;genericParameter&gt;()".</summary>
        ''' <param name="typeSymbol">The type on which to invoke the static method.</param>
        ''' <param name="methodName">The name of the static, parameterless, generic method.</param>
        ''' <param name="genericParameter">the type to use for the method's generic parameter.</param>
        ''' <returns>The resulting invocation expression.</returns>
        Private Shared Function InvokeStaticGenericParameterlessMethod(typeSymbol As INamedTypeSymbol, methodName As String, genericParameter As TypeSyntax) As InvocationExpressionSyntax
            If typeSymbol Is Nothing OrElse methodName Is Nothing OrElse genericParameter Is Nothing Then
                Return Nothing
            End If

            Return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.ParseName(typeSymbol.ContainingNamespace.ToDisplayString()),
                            SyntaxFactory.IdentifierName(typeSymbol.Name)).WithAdditionalAnnotations(Simplification.Simplifier.Annotation),
                        SyntaxFactory.Token(SyntaxKind.DotToken),
                        SyntaxFactory.GenericName(methodName,
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(genericParameter)))))
        End Function
    End Class

End Namespace
