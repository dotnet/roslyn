' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Iterator
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Iterator

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.ChangeToYield), [Shared]>
    Friend Class VisualBasicChangeToYieldCodeFixProvider
        Inherits AbstractIteratorCodeFixProvider

        Friend Const BC36942 As String = "BC36942" ' error BC36942 : To return a value from an Iterator function, use 'Yield' rather than 'Return'. 
        Friend Shared ReadOnly Ids As ImmutableArray(Of String) = ImmutableArray.Create(BC36942)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Ids
            End Get
        End Property

        Protected Overrides Function GetCodeFixAsync(root As SyntaxNode, node As SyntaxNode, document As Document, diagnostics As Diagnostic, cancellationToken As CancellationToken) As Task(Of CodeAction)
            If Not node.IsKind(SyntaxKind.ReturnStatement) Then
                Return SpecializedTasks.Null(Of CodeAction)()
            End If

            Dim returnStatement = TryCast(node, ReturnStatementSyntax)
            Dim yieldStatement = SyntaxFactory.YieldStatement(returnStatement.Expression).WithAdditionalAnnotations(Formatter.Annotation)

            root = root.ReplaceNode(returnStatement, yieldStatement)

            Return Task.FromResult(
                CodeAction.Create(
                    VisualBasicCodeFixesResources.Replace_Return_with_Yield,
                    Function(c) Task.FromResult(document.WithSyntaxRoot(root)),
                    NameOf(VisualBasicCodeFixesResources.Replace_Return_with_Yield)))
        End Function
    End Class
End Namespace

