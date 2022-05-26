' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module CompilationUnitSyntaxExtensions
        <Extension>
        Public Function CanAddImportsStatements(contextNode As SyntaxNode, allowInHiddenRegions As Boolean, cancellationToken As CancellationToken) As Boolean
            If contextNode.GetAncestor(Of ImportsStatementSyntax)() IsNot Nothing Then
                Return False
            End If

            If allowInHiddenRegions Then
                Return True
            End If

            Dim root = contextNode.GetAncestorOrThis(Of CompilationUnitSyntax)()
            If root.Imports.Count > 0 Then
                Dim start = root.Imports.First.SpanStart
                Dim [end] = root.Imports.Last.Span.End

                Return Not contextNode.SyntaxTree.OverlapsHiddenPosition(TextSpan.FromBounds(start, [end]), cancellationToken)
            Else
                Dim start = 0
                Dim [end] = If(root.Members.Count > 0,
                               root.Members.First.GetFirstToken().Span.End,
                               root.Span.End)

                Return Not contextNode.SyntaxTree.OverlapsHiddenPosition(TextSpan.FromBounds(start, [end]), cancellationToken)
            End If
        End Function

        <Extension()>
        Public Function AddImportsStatement(root As CompilationUnitSyntax,
                                            importStatement As ImportsStatementSyntax,
                                            placeSystemNamespaceFirst As Boolean,
                                            ParamArray annotations As SyntaxAnnotation()) As CompilationUnitSyntax
            Return root.AddImportsStatements({importStatement}, placeSystemNamespaceFirst, annotations)
        End Function

        <Extension()>
        Public Function AddImportsStatements(root As CompilationUnitSyntax,
                                            importsStatements As IList(Of ImportsStatementSyntax),
                                            placeSystemNamespaceFirst As Boolean,
                                            ParamArray annotations As SyntaxAnnotation()) As CompilationUnitSyntax
            If importsStatements.Count = 0 Then
                Return root
            End If

            Dim systemFirstInstance = ImportsStatementComparer.SystemFirstInstance
            Dim normalInstance = ImportsStatementComparer.NormalInstance

            Dim comparers = If(placeSystemNamespaceFirst,
                               (systemFirstInstance, normalInstance),
                               (normalInstance, systemFirstInstance))

            Dim [imports] = AddImportsStatements(root, importsStatements)

            ' First, see if the imports were sorted according to the user's preference.  If so,
            ' keep the same sorting after we add the import.  However, if the imports weren't sorted
            ' according to their preference, then see if they're sorted in the other way.  If so
            ' preserve that sorting as well.  That way if the user is working with a file that 
            ' was written on a machine with a different default, the imports will stay in a 
            ' reasonable order.
            If root.Imports.IsSorted(comparers.Item1) Then
                [imports].Sort(comparers.Item1)
            ElseIf root.Imports.IsSorted(comparers.Item2) Then
                [imports].Sort(comparers.Item2)
            End If

            root = AddImportHelpers.MoveTrivia(
                VisualBasicSyntaxFacts.Instance, root, root.Imports, [imports])

            Return root.WithImports(
                [imports].Select(Function(u) u.WithAdditionalAnnotations(annotations)).ToSyntaxList())
        End Function

        Private Function AddImportsStatements(root As CompilationUnitSyntax, importsStatements As IList(Of ImportsStatementSyntax)) As List(Of ImportsStatementSyntax)
            ' We need to try and not place the using inside of a directive if possible.
            Dim [imports] = New List(Of ImportsStatementSyntax)
            Dim importsLength = root.Imports.Count
            Dim endOfList = importsLength - 1
            Dim startOfLastDirective = -1
            Dim endOfLastDirective = -1
            For index = 0 To endOfList
                If root.Imports(index).GetLeadingTrivia().Any(Function(trivia) trivia.IsKind(SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseIfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia)) Then
                    startOfLastDirective = index
                End If

                If root.Imports(index).GetLeadingTrivia().Any(Function(trivia) trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia)) Then
                    endOfLastDirective = index
                End If
            Next

            ' if the entire using Is in a directive Or there Is a using list at the end outside of the directive add the using at the end, 
            ' else place it before the last directive.
            [imports].AddRange(root.Imports)
            If (startOfLastDirective = 0 AndAlso (endOfLastDirective = endOfList OrElse endOfLastDirective = -1)) OrElse
                (startOfLastDirective = -1 AndAlso endOfLastDirective = -1) OrElse
                (endOfLastDirective <> endOfList AndAlso endOfLastDirective <> -1) Then
                [imports].AddRange(importsStatements)
            Else
                [imports].InsertRange(startOfLastDirective, importsStatements)
            End If

            Return [imports]
        End Function
    End Module
End Namespace
