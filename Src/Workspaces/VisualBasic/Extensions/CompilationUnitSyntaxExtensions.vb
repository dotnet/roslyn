' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
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

            Dim usings As SyntaxList(Of ImportsStatementSyntax)
            If root.Imports.IsSorted(comparer) Then
                ' already sorted.  find where it should go into the list.
                usings = root.Imports

                For Each newImport In importsStatements
                    Dim list = New List(Of ImportsStatementSyntax)(usings)
                    Dim index = list.BinarySearch(newImport, comparer)
                    index = If(index < 0, Not index, index)

                    usings = Insert(usings, index, newImport)
                Next
            Else
                usings = root.Imports.InsertRange(root.Imports.Count, importsStatements.ToArray())
            End If

            Dim newRoot = root.WithImports(usings)

            Return newRoot.WithAdditionalAnnotations(annotations)
        End Function

        Private Function Insert(usings As SyntaxList(Of ImportsStatementSyntax), index As Integer, newImport As ImportsStatementSyntax) As SyntaxList(Of ImportsStatementSyntax)
            If index = 0 AndAlso usings.Count > 0 Then
                ' take any leading trivia from the existing first using and add it to the new using
                ' that is being added.
                Dim firstImport = usings.First()
                Dim firstToken = firstImport.GetFirstToken()

                Dim leadingTrivia = firstToken.LeadingTrivia
                firstImport = firstImport.ReplaceToken(
                    firstToken,
                    firstToken.WithLeadingTrivia())

                Dim statements = New List(Of ImportsStatementSyntax)
                statements.Add(firstImport)
                statements.AddRange(usings.Skip(1))

                usings = SyntaxFactory.List(statements)
                newImport = newImport.ReplaceToken(
                    newImport.GetFirstToken(),
                    newImport.GetFirstToken().WithLeadingTrivia(leadingTrivia))
            End If

            Return usings.Insert(index, newImport)
        End Function
    End Module
End Namespace