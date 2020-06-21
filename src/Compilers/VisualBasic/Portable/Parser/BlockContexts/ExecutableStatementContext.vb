' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the ExecutableStatementContext. The base class
' for all blocks that contain statements.
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend MustInherit Class ExecutableStatementContext
        Inherits DeclarationContext

        Friend Sub New(contextKind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(contextKind, statement, prevContext)
        End Sub

        Friend NotOverridable Overrides Function Parse() As StatementSyntax
            Return Parser.ParseStatementInMethodBody()
        End Function

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext
            If Parser.IsDeclarationStatement(node.Kind) Then
                ' VS 314714
                ' When we have specifiers or attributes preceding an invalid method declaration,
                ' we want an error of ERRID_InvInsideEndsProc reported, so that this file is decompiled
                ' to no state.  This will remove the task list error (no state) added in FindEndProc.
                ' Without this fix, an invalid identifier error will be reported, and the file will
                ' not decompile far enough.  The task list error will incorrectly stay around.
                '
                ' Note: This bug addresses the requirement that this method (ParseStatementInMethodBody)
                '       should report ERRID_InvInsideEndsProc in exactly the same cases that FindEndProc
                '       does

                'End the current block and add the block to the context above
                'This must end not only the current block but the current method.
                Dim declarationContext = FindNearest(Function(s) SyntaxFacts.IsMethodBlock(s) OrElse
                                                                 s = SyntaxKind.ConstructorBlock OrElse
                                                                 s = SyntaxKind.OperatorBlock OrElse
                                                                 SyntaxFacts.IsAccessorBlock(s))
                If declarationContext IsNot Nothing Then
                    Dim context = declarationContext.PrevBlock
                    RecoverFromMissingEnd(context)
                    'Let the outer context process this statement
                    Return context.ProcessSyntax(Parser.ReportSyntaxError(node, ERRID.ERR_InvInsideEndsProc))
                Else
                    ' we are in a block in a top-level code:
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_InvInsideBlock, SyntaxFacts.GetBlockName(BlockKind))
                    Return MyBase.ProcessSyntax(node)
                End If

            Else
                Select Case node.Kind
                    Case _
                        SyntaxKind.InheritsStatement,
                        SyntaxKind.ImplementsStatement,
                        SyntaxKind.OptionStatement,
                        SyntaxKind.ImportsStatement

                        Dim declarationContext = FindNearest(Function(s) SyntaxFacts.IsMethodBlock(s) OrElse
                                                                 s = SyntaxKind.ConstructorBlock OrElse
                                                                 s = SyntaxKind.OperatorBlock OrElse
                                                                 SyntaxFacts.IsAccessorBlock(s) OrElse
                                                                 SyntaxFacts.IsMultiLineLambdaExpression(s) OrElse
                                                                 SyntaxFacts.IsSingleLineLambdaExpression(s))

                        If declarationContext IsNot Nothing Then
                            ' in a method or multiline lambda expression:
                            node = Parser.ReportSyntaxError(node, ERRID.ERR_InvInsideProc)
                        Else
                            ' we are in a block or in top-level code:
                            node = Parser.ReportSyntaxError(node, ERRID.ERR_InvInsideBlock, SyntaxFacts.GetBlockName(BlockKind))
                        End If

                        Add(node)
                        Return Me

                    Case Else
                        Dim newContext = TryProcessExecutableStatement(node)
                        Return If(newContext, MyBase.ProcessSyntax(node))
                End Select

            End If
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing
            Select Case node.Kind
                ' these are errors, but ParseStatementInMethodBody accepts them for error recovery
                Case _
                    SyntaxKind.OptionStatement,
                    SyntaxKind.ImportsStatement,
                    SyntaxKind.InheritsStatement,
                    SyntaxKind.ImplementsStatement,
                    SyntaxKind.NamespaceStatement
                    Return UseSyntax(node, newContext)

                Case _
                    SyntaxKind.ClassStatement,
                    SyntaxKind.StructureStatement,
                    SyntaxKind.ModuleStatement,
                    SyntaxKind.InterfaceStatement
                    ' Reuse as long as the statement does not have modifiers.
                    ' These statements parse differently when they appear at the top level and when they appear within a method body.
                    ' Within a method body, if the statement begins with a modifier then the statement is parsed as a variable declaration (with an error).
                    If Not DirectCast(node, TypeStatementSyntax).Modifiers.Any() Then
                        Return UseSyntax(node, newContext)
                    Else
                        newContext = Me
                        Return LinkResult.NotUsed
                    End If

                Case SyntaxKind.EnumStatement
                    ' Reuse as long as the statement does not have modifiers
                    ' These statements parse differently when they appear at the top level and when they appear within a method body.
                    ' Within a method body, if the statement begins with a modifier then the statement is parsed as a variable declaration (with an error).
                    If Not DirectCast(node, EnumStatementSyntax).Modifiers.Any() Then
                        Return UseSyntax(node, newContext)
                    Else
                        newContext = Me
                        Return LinkResult.NotUsed
                    End If

                Case _
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.DeclareSubStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.DelegateSubStatement
                    ' Reuse as long as the statement does not have modifiers
                    ' These statements parse differently when they appear at the top level and when they appear within a method body.
                    ' Within a method body, if the statement begins with a dim/const then the statement is parsed as a variable declaration (with an error).
                    If Not DirectCast(node, MethodBaseSyntax).Modifiers.Any() Then
                        Return UseSyntax(node, newContext)
                    Else
                        newContext = Me
                        Return LinkResult.NotUsed
                    End If

                Case _
                    SyntaxKind.SubStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.EventStatement
                    ' Reuse as long as the statement does not have dim or const
                    If Not DirectCast(node, MethodBaseSyntax).Modifiers.Any(SyntaxKind.DimKeyword, SyntaxKind.ConstKeyword) Then
                        Return UseSyntax(node, newContext)
                    Else
                        newContext = Me
                        Return LinkResult.NotUsed
                    End If

                ' these blocks cannot happen in current context so we should crumble them
                ' on next pass we will give error on the first statement
                Case _
                    SyntaxKind.SubBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock,
                    SyntaxKind.EventBlock,
                    SyntaxKind.AddHandlerAccessorBlock,
                    SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock,
                    SyntaxKind.NamespaceBlock,
                    SyntaxKind.ClassBlock,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.EnumBlock,
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.CaseBlock,
                    SyntaxKind.CaseElseBlock,
                    SyntaxKind.CatchBlock,
                    SyntaxKind.FinallyBlock,
                    SyntaxKind.ElseBlock,
                    SyntaxKind.ElseIfBlock,
                    SyntaxKind.SingleLineElseClause,
                    SyntaxKind.AttributeList,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.FieldDeclaration

                    newContext = Me
                    Return LinkResult.Crumble

                Case _
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.GetAccessorStatement,
                    SyntaxKind.AddHandlerAccessorStatement,
                    SyntaxKind.RemoveHandlerAccessorStatement,
                    SyntaxKind.RaiseEventAccessorStatement

                    ' Don't reuse a set statement. Set/Get are parsed differently in declarations and executable contexts
                    newContext = Me
                    Return LinkResult.NotUsed

                Case Else
                    Return TryLinkStatement(node, newContext)
            End Select
        End Function

        Friend Overrides Function ProcessStatementTerminator(lambdaContext As BlockContext) As BlockContext
            Dim kind = Parser.CurrentToken.Kind
            Dim singleLine = IsSingleLine

            If singleLine Then
                Select Case kind
                    Case SyntaxKind.StatementTerminatorToken, SyntaxKind.EndOfFileToken
                        ' A single-line statement is terminated at the end of the line.
                        Dim context = EndBlock(Nothing)
                        Return context.ProcessStatementTerminator(lambdaContext)
                End Select
            End If

            Dim allowLeadingMultiline = False
            Select Case kind
                Case SyntaxKind.StatementTerminatorToken
                    allowLeadingMultiline = True
                Case SyntaxKind.ColonToken
                    allowLeadingMultiline = Not IsSingleLine
            End Select

            If lambdaContext Is Nothing OrElse
                Parser.IsNextStatementInsideLambda(Me, lambdaContext, allowLeadingMultiline) Then
                ' More statements within the block so the statement
                ' terminator can be consumed.
                Parser.ConsumeStatementTerminator(colonAsSeparator:=singleLine)
                Return Me
            Else
                ' The following statement is considered outside the enclosing lambda, so the lambda
                ' should be terminated but the statement terminator should not be consumed
                ' since it represents the end of a containing expression statement.
                Return EndLambda()
            End If
        End Function

        Friend Overrides ReadOnly Property IsSingleLine As Boolean
            Get
                Return PrevBlock.IsSingleLine
            End Get
        End Property

    End Class

End Namespace
