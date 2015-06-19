' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Performance
    ''' <summary>Provides a C# analyzer that finds empty array allocations that can be replaced with a call to Array.Empty.</summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicEmptyArrayDiagnosticAnalyzer
        Inherits EmptyArrayDiagnosticAnalyzer

        ''' <summary>
        ''' The kinds of array creation syntax kinds to be analyzed.  We care about ArrayCreationExpression (e.g. "New Byte(1) { }" or "New Byte() { 1, 2 }")
        ''' and CollectionInitializer (e.g. "Dim arr as Byte() = { }")
        ''' </summary>
        Private Shared ReadOnly s_expressionKinds() As SyntaxKind = {SyntaxKind.ArrayCreationExpression, SyntaxKind.CollectionInitializer}

        ''' <summary>Called once at session start to register actions in the analysis context.</summary>
        ''' <param name="context">The analysis context.</param>
        Friend Overrides Sub RegisterSyntaxNodeAction(context As CompilationStartAnalysisContext)
            context.RegisterSyntaxNodeAction(
                Sub(ctx)
                    ' We can't replace array allocations in attributes, as they're persisted to metadata
                    If ctx.Node.Ancestors().Any(Function(a) TypeOf a Is AttributeSyntax) Then
                        Return
                    End If

                    Dim initializerExpr As CollectionInitializerSyntax = Nothing
                    Dim isArrayCreationExpression As Boolean = (ctx.Node.Kind() = SyntaxKind.ArrayCreationExpression)
                    If isArrayCreationExpression Then
                        ' This Is an ArrayCreationExpression (e.g. "New Byte(2)" Or "New Byte() { 1, 2, 3 }").
                        ' If it has any array bounds, make sure it has only 1 and that it's a simple argument.
                        Dim arrayCreationExpr As ArrayCreationExpressionSyntax = CType(ctx.Node, ArrayCreationExpressionSyntax)
                        If arrayCreationExpr.ArrayBounds IsNot Nothing Then
                            If arrayCreationExpr.ArrayBounds.Arguments.Count = 1 Then
                                ' Parse out the numerical length from the array bounds specifier.  Whereas in C# 0 length has a 0 as the rank specifier,
                                ' in Visual Basic it's the upper bound, and thus zero length has a -1 as the array bound.
                                Dim sizeSpecifier As SimpleArgumentSyntax = TryCast(arrayCreationExpr.ArrayBounds.Arguments(0), SimpleArgumentSyntax)
                                If sizeSpecifier IsNot Nothing Then
                                    Dim unaryExpression As UnaryExpressionSyntax = TryCast(sizeSpecifier.Expression, UnaryExpressionSyntax)
                                    If unaryExpression IsNot Nothing AndAlso unaryExpression.OperatorToken.Kind() = SyntaxKind.MinusToken Then
                                        Dim literal As LiteralExpressionSyntax = TryCast(unaryExpression.Operand, LiteralExpressionSyntax)
                                        If literal IsNot Nothing AndAlso TypeOf literal.Token.Value Is Integer AndAlso CType(literal.Token.Value, Integer) = 1 Then
                                            Report(ctx, arrayCreationExpr)
                                        End If
                                    End If
                                End If
                            End If

                            Return
                        End If

                        ' We couldn't determine the length based on the rank specifier.
                        ' We'll try to do so based on any initializer expression the array has.
                        initializerExpr = arrayCreationExpr.Initializer

                    Else
                        ' This is an CollectionInitializerSyntax (e.g. { 1, 2 }).  However, we only want to examine it if it's not part of a larger ArrayCreationExpressionSyntax;
                        ' otherwise, we'll end up flagging it twice and recommending incorrect transforms.  We also only want to examine it if it's not part of an object collection
                        ' initializer, as then we're not actually creating an array.
                        Select Case ctx.Node.Parent.Kind()
                            Case SyntaxKind.ArrayCreationExpression
                            Case SyntaxKind.ObjectCollectionInitializer
                                Return
                            Case Else
                                initializerExpr = TryCast(ctx.Node, CollectionInitializerSyntax)
                        End Select
                    End If

                    ' If we needed to examine the initializer in order to get the size and if it's 0, 
                    ' report it, but make sure to report the right syntax node.
                    If initializerExpr IsNot Nothing AndAlso initializerExpr.Initializers.Count = 0 Then
                        Report(ctx, If(isArrayCreationExpression, ctx.Node, initializerExpr))
                    End If

                End Sub, s_expressionKinds)
        End Sub

    End Class
End Namespace
