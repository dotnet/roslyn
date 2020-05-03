' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the DeclarationContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend MustInherit Class DeclarationContext
        Inherits BlockContext

        Friend Sub New(kind As SyntaxKind, statement As StatementSyntax, context As BlockContext)
            MyBase.New(kind, statement, context)
        End Sub

        Friend Overrides Function Parse() As StatementSyntax
            Return Parser.ParseDeclarationStatement()
        End Function

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext

            Dim kind As SyntaxKind = node.Kind
            Dim methodBlockKind As SyntaxKind
            Dim methodBase As MethodBaseSyntax

            Select Case kind
                Case SyntaxKind.NamespaceStatement
                    ' davidsch - This error check used to be in ParseDeclarations
                    'In dev10 the error was only on the keyword and not the full statement

                    Dim reportAnError As Boolean = True

                    Dim infos As DiagnosticInfo() = node.GetDiagnostics()

                    If infos IsNot Nothing Then
                        For Each info In infos
                            Select Case info.Code
                                Case ERRID.ERR_NamespaceNotAtNamespace, ERRID.ERR_InvInsideEndsProc
                                    reportAnError = False
                                    Exit For
                            End Select
                        Next
                    End If

                    If reportAnError Then
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_NamespaceNotAtNamespace)
                    End If

                    Dim context = Me.PrevBlock
                    RecoverFromMissingEnd(context)

                    'Let the outer context process this statement
                    Return context.ProcessSyntax(node)

                Case SyntaxKind.ModuleStatement
                    'In dev10 the error was only on the keyword and not the full statement
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_ModuleNotAtNamespace)
                    Return New TypeBlockContext(SyntaxKind.ModuleBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.EnumStatement
                    Return New EnumDeclarationBlockContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.ClassStatement
                    Return New TypeBlockContext(SyntaxKind.ClassBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.StructureStatement
                    Return New TypeBlockContext(SyntaxKind.StructureBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.InterfaceStatement
                    Return New InterfaceDeclarationBlockContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.SubStatement
                    methodBlockKind = SyntaxKind.SubBlock
                    GoTo HandleMethodBase

                Case SyntaxKind.SubNewStatement
                    methodBlockKind = SyntaxKind.ConstructorBlock
                    GoTo HandleMethodBase

                Case SyntaxKind.FunctionStatement
                    methodBlockKind = SyntaxKind.FunctionBlock
HandleMethodBase:
                    If Not Parser.IsFirstStatementOnLine(node.GetFirstToken) Then
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_MethodMustBeFirstStatementOnLine)
                    End If

                    ' Don't create blocks for MustOverride Sub, Function, and New methods
                    methodBase = DirectCast(node, MethodBaseSyntax)
                    If Not methodBase.Modifiers.Any(SyntaxKind.MustOverrideKeyword) Then
                        Return New MethodBlockContext(methodBlockKind, methodBase, Me)
                    End If
                    Add(node)

                Case SyntaxKind.OperatorStatement
                    If Not Parser.IsFirstStatementOnLine(node.GetFirstToken) Then
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_MethodMustBeFirstStatementOnLine)
                    End If

                    ' It is a syntax error to have an operator in a module
                    ' This error is reported in declared in Dev10
                    If Me.BlockKind = SyntaxKind.ModuleBlock Then
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_OperatorDeclaredInModule)
                    End If

                    Return New MethodBlockContext(SyntaxKind.OperatorBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.PropertyStatement
                    ' ===== Make an initial attempt at determining if this might be an auto or expanded property
                    ' We can determine which it is in some cases by looking at the specifiers.
                    Dim propertyStatement = DirectCast(node, PropertyStatementSyntax)
                    Dim modifiers = propertyStatement.Modifiers
                    Dim isPropertyBlock As Boolean = False

                    If modifiers.Any Then
                        If modifiers.Any(SyntaxKind.MustOverrideKeyword) Then

                            ' Mustoverride mean that the property doesn't have any implementation
                            ' This cannot be an auto property so the property may not be initialized.

                            ' Check if initializer exists.  Only auto properties may have initialization.
                            node = PropertyBlockContext.ReportErrorIfHasInitializer(DirectCast(node, PropertyStatementSyntax))

                            Add(node)
                            Exit Select

                        Else
                            isPropertyBlock = modifiers.Any(SyntaxKind.DefaultKeyword,
                                                            SyntaxKind.IteratorKeyword)
                        End If
                    End If

                    Return New PropertyBlockContext(propertyStatement, Me, isPropertyBlock)

                Case _
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.GetAccessorStatement,
                    SyntaxKind.AddHandlerAccessorStatement,
                    SyntaxKind.RemoveHandlerAccessorStatement,
                    SyntaxKind.RaiseEventAccessorStatement
                    'TODO - davidsch - ParseSpecifierDeclaration reports ERRID_ExpectedDeclaration but ParsePropertyOrEventProcedureDefinition reports ERRID_Syntax.
                    '  ERRID_ExpectedDeclaration seems like a better error message. 

                    node = Parser.ReportSyntaxError(node, ERRID.ERR_ExpectedDeclaration)
                    Add(node)

                Case SyntaxKind.EventStatement
                    Dim eventDecl = DirectCast(node, EventStatementSyntax)

                    ' Per Dev10, build a block event only if all the requirements for one are met, 
                    ' i.e. custom modifier and a real AsClause
                    If eventDecl.CustomKeyword IsNot Nothing Then
                        If eventDecl.AsClause.AsKeyword.IsMissing Then
                            ' 'Custom' modifier invalid on event declared without an explicit delegate type.

                            ' davidsch - Dev10 put this error on the custom keyword.  
                            node = Parser.ReportSyntaxError(eventDecl, ERRID.ERR_CustomEventRequiresAs)
                        Else
                            Return New EventBlockContext(eventDecl, Me)
                        End If
                    End If
                    Add(node)

                Case SyntaxKind.AttributesStatement
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_AttributeStmtWrongOrder)
                    Add(node)

                Case SyntaxKind.OptionStatement
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_OptionStmtWrongOrder)
                    Add(node)

                Case SyntaxKind.ImportsStatement
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_ImportsMustBeFirst)
                    Add(node)

                Case SyntaxKind.InheritsStatement
                    Dim beginStatement = Me.BeginStatement

                    If beginStatement IsNot Nothing AndAlso beginStatement.Kind = SyntaxKind.InterfaceStatement Then
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_BadInterfaceOrderOnInherits)
                    Else
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_InheritsStmtWrongOrder)
                    End If

                    Add(node)

                Case SyntaxKind.ImplementsStatement
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_ImplementsStmtWrongOrder)
                    Add(node)

                Case _
                    SyntaxKind.EnumBlock,
                    SyntaxKind.ClassBlock,
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.NamespaceBlock,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.SubBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.EventBlock

                    'TODO - Does parser create Constructors?  Verify that code was ported correctly.
                    ' Handle any block that can be created by this context
                    Add(node)

                Case _
                    SyntaxKind.EmptyStatement,
                    SyntaxKind.IncompleteMember,
                    SyntaxKind.FieldDeclaration,
                    SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.DeclareSubStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.EnumMemberDeclaration

                    Add(node)

                Case SyntaxKind.LabelStatement
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_InvOutsideProc)
                    Add(node)

                Case _
                    SyntaxKind.EndStatement,
                    SyntaxKind.StopStatement
                    ' an error has already been reported by ParseGroupEndStatement
                    Add(node)

                Case Else
                    ' Do not report an error on End statements - it is reported on the block begin statement 
                    ' or an error that the beginning statement is missing is reported.
                    If Not SyntaxFacts.IsEndBlockLoopOrNextStatement(node.Kind) Then
                        Debug.Assert(IsExecutableStatementOrItsPart(node))
                        node = Parser.ReportSyntaxError(node, ERRID.ERR_ExecutableAsDeclaration)
                    End If

                    Add(node)

            End Select

            Return Me
        End Function

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing

            ' block-ending statements are always safe to reuse and are very common
            If KindEndsBlock(node.Kind) Then
                Return UseSyntax(node, newContext)
            End If

            Select Case node.Kind

                Case _
                    SyntaxKind.NamespaceStatement,
                    SyntaxKind.ModuleStatement,
                    SyntaxKind.EnumStatement,
                    SyntaxKind.ClassStatement,
                    SyntaxKind.StructureStatement,
                    SyntaxKind.InterfaceStatement,
                    SyntaxKind.SubStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.GetAccessorStatement,
                    SyntaxKind.AddHandlerAccessorStatement,
                    SyntaxKind.RemoveHandlerAccessorStatement,
                    SyntaxKind.RaiseEventAccessorStatement,
                    SyntaxKind.EventStatement,
                    SyntaxKind.AttributesStatement,
                    SyntaxKind.OptionStatement,
                    SyntaxKind.ImportsStatement,
                    SyntaxKind.InheritsStatement,
                    SyntaxKind.ImplementsStatement

                    Return UseSyntax(node, newContext)

                Case _
                    SyntaxKind.FieldDeclaration,
                    SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.DeclareSubStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.EnumMemberDeclaration

                    Return UseSyntax(node, newContext)

                Case _
                    SyntaxKind.ClassBlock,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.InterfaceBlock

                    Return UseSyntax(node, newContext, DirectCast(node, TypeBlockSyntax).EndBlockStatement.IsMissing)

                Case SyntaxKind.EnumBlock

                    Return UseSyntax(node, newContext, DirectCast(node, EnumBlockSyntax).EndEnumStatement.IsMissing)

                Case _
                    SyntaxKind.SubBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.FunctionBlock
                    Return UseSyntax(node, newContext, DirectCast(node, MethodBlockBaseSyntax).End.IsMissing)

                Case SyntaxKind.OperatorBlock
                    If Me.BlockKind = SyntaxKind.ModuleBlock Then
                        ' Crumble if this is in a module block for correct error processing
                        newContext = Me
                        Return LinkResult.Crumble
                    End If
                    Return UseSyntax(node, newContext, DirectCast(node, OperatorBlockSyntax).End.IsMissing)

                Case SyntaxKind.EventBlock
                    Return UseSyntax(node, newContext, DirectCast(node, EventBlockSyntax).EndEventStatement.IsMissing)

                Case SyntaxKind.PropertyBlock
                    Return UseSyntax(node, newContext, DirectCast(node, PropertyBlockSyntax).EndPropertyStatement.IsMissing)

                Case _
                    SyntaxKind.NamespaceBlock,
                    SyntaxKind.ModuleBlock
                    ' Crumble to handle errors for NamespaceBlock and ModuleBlock.
                    newContext = Me
                    Return LinkResult.Crumble

                ' single line If gets parsed as unexpected If + some garbage
                ' when in declaration context so cannot be reused
                ' see bug 901645 
                Case SyntaxKind.SingleLineIfStatement
                    newContext = Me
                    Return LinkResult.Crumble

                Case SyntaxKind.LocalDeclarationStatement
                    ' Crumble so they become field declarations
                    newContext = Me
                    Return LinkResult.Crumble

                ' TODO: need to add all executable statements that ParseDeclarationStatement handle as unexpected executables
                ' Move error reporting from ParseDeclarationStatement to the context.
                Case SyntaxKind.IfStatement
                    node = Parser.ReportSyntaxError(node, ERRID.ERR_ExecutableAsDeclaration)
                    Return TryUseStatement(node, newContext)

                    ' by default statements are not handled in declaration context
                Case Else
                    newContext = Me
                    Return LinkResult.NotUsed

            End Select
        End Function

        Friend Overrides Function RecoverFromMismatchedEnd(statement As StatementSyntax) As BlockContext
            Debug.Assert(statement IsNot Nothing)
            ' // The end construct is extraneous. Report an error and leave
            ' // the current context alone.

            Dim ErrorId As ERRID = ERRID.ERR_Syntax
            Dim stmtKind = statement.Kind

            Select Case (stmtKind)

                Case SyntaxKind.EndIfStatement
                    ErrorId = ERRID.ERR_EndIfNoMatchingIf

                Case SyntaxKind.EndWithStatement
                    ErrorId = ERRID.ERR_EndWithWithoutWith

                Case SyntaxKind.EndSelectStatement
                    ErrorId = ERRID.ERR_EndSelectNoSelect

                Case SyntaxKind.EndWhileStatement
                    ErrorId = ERRID.ERR_EndWhileNoWhile

                Case SyntaxKind.SimpleLoopStatement, SyntaxKind.LoopWhileStatement, SyntaxKind.LoopUntilStatement
                    ErrorId = ERRID.ERR_LoopNoMatchingDo

                Case SyntaxKind.NextStatement
                    ErrorId = ERRID.ERR_NextNoMatchingFor

                Case SyntaxKind.EndSubStatement
                    ErrorId = ERRID.ERR_InvalidEndSub

                Case SyntaxKind.EndFunctionStatement
                    ErrorId = ERRID.ERR_InvalidEndFunction

                Case SyntaxKind.EndOperatorStatement
                    ErrorId = ERRID.ERR_InvalidEndOperator

                Case SyntaxKind.EndPropertyStatement
                    ErrorId = ERRID.ERR_InvalidEndProperty

                Case SyntaxKind.EndGetStatement
                    ErrorId = ERRID.ERR_InvalidEndGet

                Case SyntaxKind.EndSetStatement
                    ErrorId = ERRID.ERR_InvalidEndSet

                Case SyntaxKind.EndEventStatement
                    ErrorId = ERRID.ERR_InvalidEndEvent

                Case SyntaxKind.EndAddHandlerStatement
                    ErrorId = ERRID.ERR_InvalidEndAddHandler

                Case SyntaxKind.EndRemoveHandlerStatement
                    ErrorId = ERRID.ERR_InvalidEndRemoveHandler

                Case SyntaxKind.EndRaiseEventStatement
                    ErrorId = ERRID.ERR_InvalidEndRaiseEvent

                Case SyntaxKind.EndStructureStatement
                    ErrorId = ERRID.ERR_EndStructureNoStructure

                Case SyntaxKind.EndEnumStatement
                    ErrorId = ERRID.ERR_InvalidEndEnum

                Case SyntaxKind.EndInterfaceStatement
                    ErrorId = ERRID.ERR_InvalidEndInterface

                Case SyntaxKind.EndTryStatement
                    ErrorId = ERRID.ERR_EndTryNoTry

                Case SyntaxKind.EndClassStatement
                    ErrorId = ERRID.ERR_EndClassNoClass

                Case SyntaxKind.EndModuleStatement
                    ErrorId = ERRID.ERR_EndModuleNoModule

                Case SyntaxKind.EndNamespaceStatement
                    ErrorId = ERRID.ERR_EndNamespaceNoNamespace

                Case SyntaxKind.EndUsingStatement
                    ErrorId = ERRID.ERR_EndUsingWithoutUsing

                Case SyntaxKind.EndSyncLockStatement
                    ErrorId = ERRID.ERR_EndSyncLockNoSyncLock

#If UNDONE Then
                        'TODO - davidsch - handle regions
                    Case NodeKind.EndRegionStatement
                        ErrorId = ERRID.ERR_EndRegionNoRegion
#End If

                Case SyntaxKind.EmptyStatement, SyntaxKind.IncompleteMember
                    ErrorId = ERRID.ERR_UnrecognizedEnd

                Case Else
                    Throw New ArgumentException("Statement must be an end block statement")

            End Select

            'TODO 
            '  Should ReportSyntaxError be on the Parser or can it be made static
            ' If the parser isn't needed for more than this then remove it from the context.
            statement = Parser.ReportSyntaxError(statement, ErrorId)
            Return ProcessSyntax(statement)
        End Function

        Friend Overrides Function EndBlock(endStmt As StatementSyntax) As BlockContext
            Dim blockSyntax = CreateBlockSyntax(endStmt)
            Dim context = PrevBlock.ProcessSyntax(blockSyntax)
            Return context
        End Function

        Friend Overrides Function ProcessStatementTerminator(lambdaContext As BlockContext) As BlockContext
            Parser.ConsumeStatementTerminator(colonAsSeparator:=False)
            Return Me
        End Function

        Friend Overrides ReadOnly Property IsSingleLine As Boolean
            Get
                Return False
            End Get
        End Property

    End Class

End Namespace

