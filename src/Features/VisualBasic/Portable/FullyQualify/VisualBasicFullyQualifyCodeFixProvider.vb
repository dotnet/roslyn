' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.FullyQualify
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FullyQualify), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    Friend Class VisualBasicFullyQualifyCodeFixProvider
        Inherits AbstractFullyQualifyCodeFixProvider(Of SimpleNameSyntax)

        ''' <summary>
        ''' Type xxx is not defined
        ''' </summary>
        Private Const BC30002 = "BC30002"

        ''' <summary>
        ''' Error 'x' is not declared
        ''' </summary>
        Private Const BC30451 = "BC30451"

        ''' <summary>
        ''' 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        ''' </summary>
        Private Const BC30561 = "BC30561"

        ''' <summary>
        ''' Namespace or type specified in imports cannot be found
        ''' </summary>
        Private Const BC40056 = "BC40056"

        ''' <summary>
        ''' 'A' has no type parameters and so cannot have type arguments.
        ''' </summary>
        Private Const BC32045 = "BC32045"

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC30002, IDEDiagnosticIds.UnboundIdentifierId, BC30451, BC30561, BC40056, BC32045)

        Protected Overrides Function CanFullyQualify(diagnostic As Diagnostic, node As SyntaxNode, ByRef simpleName As SimpleNameSyntax) As Boolean
            Dim qn = TryCast(node, QualifiedNameSyntax)
            If qn IsNot Nothing Then
                node = GetLeftMostSimpleName(qn)
            End If

            simpleName = TryCast(node, SimpleNameSyntax)
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

        Private Shared Function GetLeftMostSimpleName(qn As QualifiedNameSyntax) As SimpleNameSyntax
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

        Protected Overrides Async Function ReplaceNodeAsync(simpleName As SimpleNameSyntax, containerName As String, resultingSymbolIsType As Boolean, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Dim leadingTrivia = simpleName.GetLeadingTrivia()
            Dim newName = simpleName.WithLeadingTrivia(CType(Nothing, SyntaxTriviaList))

            Dim qualifiedName = SyntaxFactory.QualifiedName(left:=SyntaxFactory.ParseName(containerName), right:=newName).
                WithLeadingTrivia(leadingTrivia).
                WithAdditionalAnnotations(Formatter.Annotation, CaseCorrector.Annotation)

            Dim tree = simpleName.SyntaxTree
            Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            Return root.ReplaceNode(simpleName, qualifiedName)
        End Function
    End Class
End Namespace
