' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.FullyQualify
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FullyQualify), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddUsingOrImport)>
    Friend Class VisualBasicFullyQualifyCodeFixProvider
        Inherits AbstractFullyQualifyCodeFixProvider

        ''' <summary>
        ''' Type xxx is not defined
        ''' </summary>
        Friend Const BC30002 = "BC30002"

        ''' <summary>
        ''' Error 'x' is not declared
        ''' </summary>
        Friend Const BC30451 = "BC30451"

        ''' <summary>
        ''' 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        ''' </summary>
        Friend Const BC30561 = "BC30561"

        ''' <summary>
        ''' Namespace or type specified in imports cannot be found
        ''' </summary>
        Friend Const BC40056 = "BC40056"

        ''' <summary>
        ''' 'A' has no type parameters and so cannot have type arguments.
        ''' </summary>
        Friend Const BC32045 = "BC32045"

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30002, BC30451, BC30561, BC40056, BC32045)
            End Get
        End Property

        Protected Overrides ReadOnly Property IgnoreCase As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function CanFullyQualify(diagnostic As Diagnostic, ByRef node As SyntaxNode) As Boolean
            Dim qn = TryCast(node, QualifiedNameSyntax)
            If qn IsNot Nothing Then
                node = GetLeftMostSimpleName(qn)
            End If

            Dim simpleName = TryCast(node, SimpleNameSyntax)
            If simpleName Is Nothing Then
                Return False
            End If

            If Not simpleName.LooksLikeStandaloneTypeName() Then
                Return False
            End If

            If Not simpleName.CanBeReplacedWithAnyName() Then
                Return False
            End If

            Return True
        End Function

        Private Function GetLeftMostSimpleName(qn As QualifiedNameSyntax) As SimpleNameSyntax
            While (qn IsNot Nothing)
                Dim left = qn.Left
                Dim simpleName = TryCast(left, SimpleNameSyntax)
                If simpleName IsNot Nothing Then
                    Return simpleName
                End If
                qn = TryCast(left, QualifiedNameSyntax)
            End While
            Return Nothing
        End Function

        Protected Overrides Function ReplaceNode(node As SyntaxNode, containerName As String, cancellationToken As CancellationToken) As SyntaxNode
            Dim simpleName = DirectCast(node, SimpleNameSyntax)

            Dim leadingTrivia = simpleName.GetLeadingTrivia()
            Dim newName = simpleName.WithLeadingTrivia(CType(Nothing, SyntaxTriviaList))
            Dim qualifiedName = SyntaxFactory.QualifiedName(left:=SyntaxFactory.ParseName(containerName), right:=newName)
            qualifiedName = qualifiedName.WithLeadingTrivia(leadingTrivia)

            qualifiedName = qualifiedName.WithAdditionalAnnotations(Formatter.Annotation, CaseCorrector.Annotation)

            Dim tree = simpleName.SyntaxTree
            Return tree.GetRoot(cancellationToken).ReplaceNode(simpleName, qualifiedName)
        End Function
    End Class
End Namespace
