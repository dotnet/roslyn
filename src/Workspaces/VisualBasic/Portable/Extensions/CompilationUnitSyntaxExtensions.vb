' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module CompilationUnitSyntaxExtensions
        <Extension>
        Public Function CanAddImportsStatements(contextNode As SyntaxNode, cancellationToken As CancellationToken) As Boolean
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

            Dim comparer = If(placeSystemNamespaceFirst,
                              ImportsStatementComparer.SystemFirstInstance,
                              ImportsStatementComparer.NormalInstance)

            Dim [imports] = AddImportsStatements(root, importsStatements)

            ' If the user likes to have their Imports statements unsorted, allow them to
            If root.Imports.IsSorted(comparer) Then
                [imports].Sort(comparer)
            End If

            ' If any using we added was moved to the first location, then take the trivia from
            ' the start of the first token And add it to the using we added.  This way things
            ' Like #define's and #r's will stay above the using.
            If root.Imports.Count = 0 OrElse [imports](0) IsNot root.Imports(0) Then
                Dim firstToken = root.GetFirstToken

                ' Move the leading directives from the first directive to the New using.
                Dim firstImport = [imports](0).WithLeadingTrivia(firstToken.LeadingTrivia.Where(Function(t) Not t.IsKind(SyntaxKind.DocumentationCommentTrivia) AndAlso Not t.IsElastic))

                ' Remove the leading directives from the first token.
                Dim newFirstToken = firstToken.WithLeadingTrivia(firstToken.LeadingTrivia.Where(Function(t) t.IsKind(SyntaxKind.DocumentationCommentTrivia) OrElse t.IsElastic))

                ' Remove the leading trivia from the first token from the tree.
                root = root.ReplaceToken(firstToken, newFirstToken)

                ' Create the New list of imports
                Dim finalImports = New List(Of ImportsStatementSyntax)
                finalImports.Add(firstImport)
                finalImports.AddRange(root.Imports)
                finalImports.AddRange(importsStatements.Except({[imports](0)}))
                finalImports.Sort(comparer)
                [imports] = finalImports
            End If

            Return root.WithImports([imports].ToSyntaxList).WithAdditionalAnnotations(annotations)
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
