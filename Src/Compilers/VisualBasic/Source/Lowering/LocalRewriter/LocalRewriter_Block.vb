' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode

            ' Static locals should be removed from the list of locals,
            ' they are replaced with fields.
            If Not node.LocalsOpt.IsDefaultOrEmpty Then
                Dim i As Integer
                Dim builder As ArrayBuilder(Of LocalSymbol) = Nothing

                For i = 0 To node.LocalsOpt.Length - 1
                    If node.LocalsOpt(i).IsStatic Then
                        builder = ArrayBuilder(Of LocalSymbol).GetInstance()
                        Exit For
                    End If
                Next

                If builder IsNot Nothing Then
                    builder.AddRange(node.LocalsOpt, i)

                    For i = i + 1 To node.LocalsOpt.Length - 1
                        If Not node.LocalsOpt(i).IsStatic Then
                            builder.Add(node.LocalsOpt(i))
                        End If
                    Next

                    Debug.Assert(builder.Count < node.LocalsOpt.Length)

                    node = node.Update(node.StatementListSyntax, builder.ToImmutableAndFree(), node.Statements)
                End If
            End If

            If GenerateDebugInfo Then
                Dim builder As ArrayBuilder(Of BoundStatement) = Nothing

                ' method block needs to get a sequence point inside the method scope, 
                ' but before starting any statements
                Dim asMethod = TryCast(node.Syntax, MethodBlockBaseSyntax)
                If asMethod IsNot Nothing Then
                    builder = ArrayBuilder(Of BoundStatement).GetInstance
                    Dim methodStatement As MethodBaseSyntax = asMethod.Begin

                    ' For methods we want the span of the statement without any leading attributes.
                    ' The span begins at the first modifier or the 'Sub'/'Function' keyword.
                    ' There is no need to do this adjustment for lambdas because they cannot
                    ' have attributes.

                    Dim firstModifierOrKeyword As SyntaxToken

                    If methodStatement.Modifiers.Count > 0 Then
                        firstModifierOrKeyword = methodStatement.Modifiers(0)
                    Else
                        firstModifierOrKeyword = methodStatement.Keyword
                    End If

                    Dim statementSpanWithoutAttributes = TextSpan.FromBounds(firstModifierOrKeyword.SpanStart, methodStatement.Span.End)

                    builder.Add(New BoundSequencePointWithSpan(methodStatement, Nothing, statementSpanWithoutAttributes))
                Else
                    Dim asLambda = TryCast(node.Syntax, LambdaExpressionSyntax)
                    If asLambda IsNot Nothing Then
                        builder = ArrayBuilder(Of BoundStatement).GetInstance
                        builder.Add(New BoundSequencePoint(asLambda.Begin, Nothing))
                    End If
                End If

                If builder IsNot Nothing Then
                    For Each s In node.Statements
                        Dim rewrittenStatement = TryCast(Visit(s), BoundStatement)
                        If rewrittenStatement IsNot Nothing Then
                            builder.Add(rewrittenStatement)
                        End If
                    Next

                    Return New BoundBlock(node.Syntax, node.StatementListSyntax, node.LocalsOpt, builder.ToImmutableAndFree())
                End If
            End If

            Return MyBase.VisitBlock(node)
        End Function
    End Class
End Namespace