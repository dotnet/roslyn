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
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MoveToTopOfFile), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementInterface)>
    Partial Friend Class MoveToTopOfFileCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30465 As String = "BC30465" ' 'Imports' statements must precede any declarations.
        Friend Const BC30637 As String = "BC30637" ' Assembly or Module attribute statements must precede any declarations in a file.
        Friend Const BC30627 As String = "BC30627" ' 'Option' statements must precede any declarations or 'Imports' statements.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30465, BC30637, BC30627)
            End Get
        End Property

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            ' Fix All is not supported for this code fix
            ' https://github.com/dotnet/roslyn/issues/34471
            Return Nothing
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span) Then
                Return
            End If

            Dim node = token _
                .GetAncestors(Of DeclarationStatementSyntax) _
                .FirstOrDefault(Function(c) c.Span.IntersectsWith(span))

            If node Is Nothing Then
                Return
            End If

            Dim compilationUnit = CType(root, CompilationUnitSyntax)

            Dim result = SpecializedCollections.EmptyEnumerable(Of CodeAction)()
            If node.Kind = SyntaxKind.ImportsStatement Then
                Dim importsStatement = DirectCast(node, ImportsStatementSyntax)
                If Not compilationUnit.Imports.Contains(importsStatement) Then
                    If DeclarationsExistAfterImports(node, compilationUnit) OrElse compilationUnit.Attributes.Any(Function(a) a.SpanStart < node.SpanStart) Then
                        result = CreateActionForImports(document, importsStatement, compilationUnit, cancellationToken)
                    End If
                End If
            End If

            If node.Kind = SyntaxKind.OptionStatement Then
                Dim optionStatement = DirectCast(node, OptionStatementSyntax)
                If Not compilationUnit.Options.Contains(optionStatement) Then
                    result = CreateActionForOptions(document, optionStatement, compilationUnit, cancellationToken)
                End If
            End If

            If node.Kind = SyntaxKind.AttributesStatement Then
                Dim attributesStatement = DirectCast(node, AttributesStatementSyntax)
                If Not compilationUnit.Attributes.Contains(attributesStatement) Then
                    result = CreateActionForAttribute(document, attributesStatement, compilationUnit, cancellationToken)
                End If
            End If

            If result IsNot Nothing Then
                context.RegisterFixes(result, context.Diagnostics)
            End If
        End Function

        Private Shared Function DeclarationsExistAfterImports(node As SyntaxNode, root As CompilationUnitSyntax) As Boolean
            Return root.Members.Any(
                Function(m) m IsNot node AndAlso
                        Not m.IsKind(SyntaxKind.OptionStatement, SyntaxKind.AttributesStatement) AndAlso
                        node.Span.End > m.SpanStart)
        End Function

        Private Shared Function CreateActionForImports(document As Document, node As ImportsStatementSyntax, root As CompilationUnitSyntax, cancellationToken As CancellationToken) As IEnumerable(Of CodeAction)
            Dim destinationLine As Integer = 0
            If root.Imports.Any() Then
                destinationLine = FindLastContiguousStatement(root.Imports, root.GetLeadingBannerAndPreprocessorDirectives())
            ElseIf root.Options.Any() Then
                destinationLine = root.Options.Last().GetLocation().GetLineSpan().EndLinePosition.Line + 1
            End If

            If DestinationPositionIsHidden(root, destinationLine, cancellationToken) Then
                Return Nothing
            End If

            Return {
                New MoveToLineCodeAction(document,
                                         node.ImportsKeyword,
                                         destinationLine,
                                         MoveStatement("Imports", destinationLine)),
                New RemoveStatementCodeAction(document, node, DeleteStatement("Imports"))
            }

        End Function

        Private Shared Function CreateActionForOptions(document As Document, node As OptionStatementSyntax, root As CompilationUnitSyntax, cancellationToken As CancellationToken) As IEnumerable(Of CodeAction)
            Dim destinationLine = FindLastContiguousStatement(root.Options, root.GetLeadingBannerAndPreprocessorDirectives())

            If DestinationPositionIsHidden(root, destinationLine, cancellationToken) Then
                Return Nothing
            End If

            Return {
                New MoveToLineCodeAction(document,
                                         node.OptionKeyword,
                                         destinationLine,
                                         MoveStatement("Option", destinationLine)),
                New RemoveStatementCodeAction(document, node, DeleteStatement("Option"))
            }
        End Function

        Private Shared Function CreateActionForAttribute(document As Document, node As AttributesStatementSyntax, root As CompilationUnitSyntax, cancellationToken As CancellationToken) As IEnumerable(Of CodeAction)
            Dim destinationLine As Integer = 0
            If root.Attributes.Any() Then
                destinationLine = FindLastContiguousStatement(root.Attributes, root.GetLeadingBannerAndPreprocessorDirectives())
            ElseIf root.Imports.Any() Then
                destinationLine = root.Imports.Last().GetLocation().GetLineSpan().EndLinePosition.Line + 1
            ElseIf root.Options.Any() Then
                destinationLine = root.Options.Last().GetLocation().GetLineSpan().EndLinePosition.Line + 1
            End If

            If DestinationPositionIsHidden(root, destinationLine, cancellationToken) Then
                Return Nothing
            End If

            Return {
                New MoveToLineCodeAction(document,
                                         node.GetFirstToken(),
                                         destinationLine,
                                         MoveStatement("Attribute", destinationLine)),
                New RemoveStatementCodeAction(document, node, DeleteStatement("Attribute"))
            }
        End Function

        Private Shared Function FindLastContiguousStatement(nodes As IEnumerable(Of SyntaxNode), trivia As IEnumerable(Of SyntaxTrivia)) As Integer
            If Not nodes.Any() Then
                Dim lastBannerText = trivia.LastOrDefault(Function(t) t.IsKind(SyntaxKind.CommentTrivia))
                If lastBannerText = Nothing Then
                    Return 0
                Else
                    Return lastBannerText.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                End If
            End If

            ' Advance through the list of nodes until we find one that doesn't start on the
            ' line immediately following the one before it.
            Dim expectedLine = nodes.First().GetLocation().GetLineSpan().StartLinePosition.Line
            For Each node In nodes
                Dim actualLine = node.GetLocation().GetLineSpan().StartLinePosition.Line

                If actualLine <> expectedLine Then
                    Exit For
                End If

                expectedLine += 1
            Next

            Return expectedLine
        End Function

        Private Shared Function MoveStatement(kind As String, line As Integer) As String
            Return String.Format(VBFeaturesResources.Move_the_0_statement_to_line_1, kind, line + 1)
        End Function

        Private Shared Function DeleteStatement(kind As String) As String
            Return String.Format(VBFeaturesResources.Delete_the_0_statement2, kind)
        End Function

        Private Shared Function DestinationPositionIsHidden(root As CompilationUnitSyntax, destinationLine As Integer, cancellationToken As CancellationToken) As Boolean
            Dim text = root.GetText()
            Dim position = text.Lines(destinationLine).Start
            Return root.SyntaxTree.IsHiddenPosition(destinationLine, cancellationToken)
        End Function
    End Class
End Namespace
