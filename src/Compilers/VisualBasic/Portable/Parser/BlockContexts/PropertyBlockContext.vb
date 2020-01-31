' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class PropertyBlockContext
        Inherits DeclarationContext

        Private ReadOnly _isPropertyBlock As Boolean

        Friend Sub New(statement As StatementSyntax, prevContext As BlockContext, isPropertyBlock As Boolean)
            MyBase.New(SyntaxKind.PropertyBlock, statement, prevContext)

            _isPropertyBlock = isPropertyBlock
        End Sub

        Private ReadOnly Property IsPropertyBlock As Boolean
            Get
                Return _isPropertyBlock OrElse Statements.Count > 0
            End Get
        End Property

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode

            Dim beginBlockStmt As PropertyStatementSyntax = Nothing
            Dim endBlockStmt As EndBlockStatementSyntax = DirectCast(endStmt, EndBlockStatementSyntax)

            GetBeginEndStatements(beginBlockStmt, endBlockStmt)

            Dim accessors = _statements.ToList(Of AccessorBlockSyntax)()
            FreeStatements()

            ' We can only get here if this is a block property but still check accessor count.
            If accessors.Any Then
                ' Only auto properties can be initialized.  If there is a Get or Set accessor then it is an error.
                beginBlockStmt = ReportErrorIfHasInitializer(beginBlockStmt)
            End If

            Return SyntaxFactory.PropertyBlock(beginBlockStmt, accessors, endBlockStmt)
        End Function

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Select Case node.Kind
                Case SyntaxKind.GetAccessorStatement
                    Return New MethodBlockContext(SyntaxKind.GetAccessorBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.SetAccessorStatement
                    ' Checks for duplicate GET/SET are deferred to declared per Dev10 code
                    Return New MethodBlockContext(SyntaxKind.SetAccessorBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock
                    ' Handle any block created by this context
                    Add(node)

                Case Else
                    ' TODO - In Dev10, the code tries to report ERRID_PropertyMemberSyntax.  This would occur prior to auto properties
                    ' when the statement was a malformed declaration.  However, due to autoproperties this no longer seems possible.
                    ' test should confirm that this error can no longer be produced in Dev10. Is it worth trying to preserve this behavior?

                    Dim context As BlockContext = EndBlock(Nothing)
                    Debug.Assert(context Is PrevBlock)

                    If IsPropertyBlock Then
                        ' Property blocks can only contain Get and Set.
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_InvInsideEndsProperty)
                    End If

                    ' Let the outer context process this statement
                    Return context.ProcessSyntax(node)

            End Select
            Return Me
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing

            If KindEndsBlock(node.Kind) Then
                Return UseSyntax(node, newContext)
            End If

            Select Case node.Kind

                Case _
                    SyntaxKind.GetAccessorStatement,
                    SyntaxKind.SetAccessorStatement
                    Return UseSyntax(node, newContext)

                Case SyntaxKind.GetAccessorBlock,
                    SyntaxKind.SetAccessorBlock
                    Return UseSyntax(node, newContext, DirectCast(node, AccessorBlockSyntax).End.IsMissing)

                Case Else
                    newContext = Me
                    Return LinkResult.Crumble
            End Select
        End Function

        Friend Overrides Function EndBlock(endStmt As StatementSyntax) As BlockContext

            If IsPropertyBlock OrElse endStmt IsNot Nothing Then
                Return MyBase.EndBlock(endStmt)
            End If

            ' This is an auto property.  Do not create a block.  Just add the property statement to the outer block
            ' TODO - Consider changing the kind to AutoProperty.  For now auto properties are just PropertyStatement
            ' whose parent is not a PropertyBlock. Don't create a missing end for an auto property

            Debug.Assert(PrevBlock IsNot Nothing)
            Debug.Assert(Statements.Count = 0)

            Dim beginBlockStmt As PropertyStatementSyntax = DirectCast(BeginStatement, PropertyStatementSyntax)

            ' Check if auto property has params
            If beginBlockStmt.ParameterList IsNot Nothing AndAlso beginBlockStmt.ParameterList.Parameters.Count > 0 Then
                beginBlockStmt = New PropertyStatementSyntax(beginBlockStmt.Kind,
                                                             beginBlockStmt.AttributeLists.Node,
                                                             beginBlockStmt.Modifiers.Node,
                                                             beginBlockStmt.PropertyKeyword,
                                                             beginBlockStmt.Identifier,
                                                             Parser.ReportSyntaxError(beginBlockStmt.ParameterList, ERRID.ERR_AutoPropertyCantHaveParams),
                                                             beginBlockStmt.AsClause,
                                                             beginBlockStmt.Initializer,
                                                             beginBlockStmt.ImplementsClause)
            End If

            'Add auto property to Prev context. DO NOT call ProcessSyntax because that will create a PropertyContext.
            '  Just add the statement to the context.
            Dim context = PrevBlock
            context.Add(beginBlockStmt)
            Return context
        End Function

        Friend Shared Function ReportErrorIfHasInitializer(propertyStatement As PropertyStatementSyntax) As PropertyStatementSyntax

            If propertyStatement.Initializer IsNot Nothing OrElse
               (
                   propertyStatement.AsClause IsNot Nothing AndAlso
                   TryCast(propertyStatement.AsClause, AsNewClauseSyntax) IsNot Nothing
               ) Then

                propertyStatement = Parser.ReportSyntaxError(propertyStatement, ERRID.ERR_InitializedExpandedProperty)

                'TODO - In Dev10 resources there is an unused resource ERRID.ERR_NewExpandedProperty for the new case.
                ' Is it worthwhile adding to Dev12?
            End If

            Return propertyStatement
        End Function

    End Class

End Namespace
