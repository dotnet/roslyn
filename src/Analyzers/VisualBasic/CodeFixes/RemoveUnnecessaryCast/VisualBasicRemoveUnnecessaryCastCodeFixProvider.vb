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
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryCast

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateEndConstruct)>
    Partial Friend Class VisualBasicRemoveUnnecessaryCastCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            RegisterCodeFix(context, AnalyzersResources.Remove_Unnecessary_Cast, NameOf(AnalyzersResources.Remove_Unnecessary_Cast))
            Return Task.CompletedTask
        End Function

        Private Shared Function IsUnnecessaryCast(node As ExpressionSyntax, model As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim castExpression = TryCast(node, CastExpressionSyntax)
            If castExpression IsNot Nothing Then
                Return castExpression.IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
            End If

            Dim predefinedCastExpression = TryCast(node, PredefinedCastExpressionSyntax)
            If predefinedCastExpression IsNot Nothing Then
                Return predefinedCastExpression.IsUnnecessaryCast(model, assumeCallKeyword:=True, cancellationToken:=cancellationToken)
            End If

            Return False
        End Function

        Protected Overrides Async Function FixAllAsync(
            document As Document,
            diagnostics As ImmutableArray(Of Diagnostic),
            editor As SyntaxEditor,
            fallbackOptions As CodeActionOptionsProvider,
            cancellationToken As CancellationToken) As Task

            ' VB parsing is extremely hairy.  Unlike C#, it can be very dangerous to go and remove a
            ' cast.  For example, if the cast is at the statement level, it may contain an
            ' expression that itself is not legal on its own at the top level (see below for an
            ' example of this).  Similarly, removing the cast may make VB parse following code
            ' differently.
            '
            ' In order to deal with all these concerns safely, we first complexify the surrounding
            ' statements containing the casts we want to remove.  *Then* we  remove the casts from
            ' inside that.
            '
            ' As an example, consider:                  DirectCast(New Goo(), IGoo).Blah() This is
            ' legal code, but this is not:              New Goo().Blah()
            '
            ' (because 'new' cannot start a statement).
            ' So we need to instead generate:           Call New Goo().Blah()

            Dim originalCastNodes = diagnostics.SelectAsArray(
                Function(d) DirectCast(d.AdditionalLocations(0).FindNode(getInnermostNodeForTie:=True, cancellationToken), ExpressionSyntax))

            ' Keep track of the all the casts we want to fix up.  We'll fix them up at the end
            ' after we've done all other manipulation.
            Dim trackedRoot = editor.OriginalRoot.TrackNodes(originalCastNodes)
            Dim trackedDocument = document.WithSyntaxRoot(trackedRoot)

            ' Now, go and expand all the containing statements of the nodes we want to edit.
            ' This is necessary to ensure that the code remains parseable and preserves semantics.
            Dim expandedRoot = Await ExpandSurroundingStatementsAsync(trackedDocument, originalCastNodes, cancellationToken).ConfigureAwait(False)
            Dim expandedDocument = document.WithSyntaxRoot(expandedRoot)

            Dim removedRoot = Await RemoveCastsAsync(
                expandedDocument, originalCastNodes, cancellationToken).ConfigureAwait(False)

            editor.ReplaceNode(editor.OriginalRoot, removedRoot)
        End Function

        Private Shared Async Function RemoveCastsAsync(
                document As Document, originalCastNodes As ImmutableArray(Of ExpressionSyntax),
                cancellationToken As CancellationToken) As Task(Of SyntaxNode)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            ' Now, find the cast nodes again in the expanded document
            Dim currentCastNodes = root.GetCurrentNodes(originalCastNodes)

            Dim innerEditor = New SyntaxEditor(root, document.Project.Solution.Services)
            Await innerEditor.ApplyExpressionLevelSemanticEditsAsync(
                document, currentCastNodes.ToImmutableArray(),
                Function(semanticModel, castExpression) IsUnnecessaryCast(castExpression, semanticModel, cancellationToken),
                Function(unused, currentRoot, castExpression)
                    Dim newCastExpression = Uncast(castExpression).WithAdditionalAnnotations(Formatter.Annotation)
                    Return currentRoot.ReplaceNode(castExpression, newCastExpression)
                End Function,
                cancellationToken).ConfigureAwait(False)

            Return innerEditor.GetChangedRoot()
        End Function

        Private Shared Async Function ExpandSurroundingStatementsAsync(
                document As Document, originalNodes As ImmutableArray(Of ExpressionSyntax),
                cancellationToken As CancellationToken) As Task(Of SyntaxNode)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            ' Note: we not only get the containing statement, but also the next statement after
            ' that.  That's because the removal of the parens in the cast may then cause parsing
            ' problems with VB consuming the following line into the current line.  This is most
            ' common with query clauses.  By complexifying the next statement, we prevent that from
            ' happening.
            Dim trackedNodes = root.GetCurrentNodes(originalNodes)
            Dim containingAndNextStatements = trackedNodes.SelectMany(
                Function(n)
                    Dim containingStatement = n.GetAncestorOrThis(Of StatementSyntax)
                    Dim nextStatement = containingStatement.GetNextStatement()
                    Return If(nextStatement Is Nothing,
                        {containingStatement},
                        {containingStatement, nextStatement})
                End Function).Distinct()

            Dim editor = New SyntaxEditor(root, document.Project.Solution.Services)

            For Each containingStatement In containingAndNextStatements
                Dim expandedStatement = Await Simplifier.ExpandAsync(containingStatement, document, cancellationToken:=cancellationToken).ConfigureAwait(False)
                editor.ReplaceNode(containingStatement, expandedStatement)
            Next

            Return editor.GetChangedRoot()
        End Function

        Private Shared Function Uncast(old As ExpressionSyntax) As ExpressionSyntax
            ' parenthesize the uncasted value to help ensure any proper parsing. The excess
            ' parens will be removed if unnecessary. 
            Dim castExpression = TryCast(old, CastExpressionSyntax)
            If castExpression IsNot Nothing Then
                Return castExpression.Uncast().Parenthesize()
            End If

            Dim predefinedCastExpression = TryCast(old, PredefinedCastExpressionSyntax)
            If predefinedCastExpression IsNot Nothing Then
                Return predefinedCastExpression.Uncast().Parenthesize()
            End If

            Throw ExceptionUtilities.UnexpectedValue(old)
        End Function
    End Class
End Namespace
