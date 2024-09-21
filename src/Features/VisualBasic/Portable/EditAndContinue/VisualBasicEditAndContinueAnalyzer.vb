' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Friend NotInheritable Class VisualBasicEditAndContinueAnalyzer
        Inherits AbstractEditAndContinueAnalyzer

        <ExportLanguageServiceFactory(GetType(IEditAndContinueAnalyzer), LanguageNames.VisualBasic), [Shared]>
        Private NotInheritable Class Factory
            Implements ILanguageServiceFactory

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
                Return New VisualBasicEditAndContinueAnalyzer(testFaultInjector:=Nothing)
            End Function
        End Class

        ' Public for testing purposes
        Public Sub New(Optional testFaultInjector As Action(Of SyntaxNode) = Nothing)
            MyBase.New(testFaultInjector)
        End Sub

#Region "Syntax Analysis"

        Friend Overrides Function TryFindMemberDeclaration(rootOpt As SyntaxNode, node As SyntaxNode, activeSpan As TextSpan, <Out> ByRef declarations As OneOrMany(Of SyntaxNode)) As Boolean
            Dim current = node
            While current IsNot rootOpt
                Select Case current.Kind
                    Case SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.ConstructorBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock
                        declarations = OneOrMany.Create(current)
                        Return True

                    Case SyntaxKind.PropertyStatement
                        ' Property a As Integer = 1
                        ' Property a As New T
                        If Not current.Parent.IsKind(SyntaxKind.PropertyBlock) Then
                            declarations = OneOrMany.Create(current)
                            Return True
                        End If

                    Case SyntaxKind.VariableDeclarator
                        If current.Parent.IsKind(SyntaxKind.FieldDeclaration) Then

                            Dim variableDeclarator = CType(current, VariableDeclaratorSyntax)
                            If variableDeclarator.Names.Count = 1 Then
                                declarations = OneOrMany.Create(Of SyntaxNode)(variableDeclarator.Names(0))
                            Else
                                declarations = OneOrMany.Create(variableDeclarator.Names.SelectAsArray(Function(n) CType(n, SyntaxNode)))
                            End If

                            Return True
                        End If

                    Case SyntaxKind.ModifiedIdentifier
                        If current.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) Then
                            declarations = OneOrMany.Create(current)
                            Return True
                        End If
                End Select

                current = current.Parent
            End While

            declarations = Nothing
            Return False
        End Function

        ''' <returns>
        ''' Given a node representing a declaration or a top-level edit node returns:
        ''' - <see cref="MethodBlockBaseSyntax"/> for methods, constructors, operators and accessors.
        ''' - <see cref="ExpressionSyntax"/> for auto-properties and fields with initializer or AsNew clause.
        ''' - <see cref="ArgumentListSyntax"/> for fields with array initializer, e.g. "Dim a(1) As Integer".
        ''' A null reference otherwise.
        ''' </returns>
        Friend Overrides Function TryGetDeclarationBody(node As SyntaxNode, symbol As ISymbol) As MemberBody
            Return SyntaxUtilities.TryGetDeclarationBody(node)
        End Function

        Friend Overrides Function IsDeclarationWithSharedBody(declaration As SyntaxNode, member As ISymbol) As Boolean
            If declaration.Kind = SyntaxKind.ModifiedIdentifier AndAlso declaration.Parent.Kind = SyntaxKind.VariableDeclarator Then
                Dim variableDeclarator = CType(declaration.Parent, VariableDeclaratorSyntax)
                Return variableDeclarator.Names.Count > 1 AndAlso variableDeclarator.Initializer IsNot Nothing OrElse SyntaxUtilities.HasAsNewClause(variableDeclarator)
            End If

            Return False
        End Function

        Protected Overrides Function GetVariableUseSites(roots As IEnumerable(Of SyntaxNode), localOrParameter As ISymbol, model As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode)
            Debug.Assert(TypeOf localOrParameter Is IParameterSymbol OrElse TypeOf localOrParameter Is ILocalSymbol OrElse TypeOf localOrParameter Is IRangeVariableSymbol)

            ' Not supported (it's non trivial to find all places where "this" is used):
            Debug.Assert(Not localOrParameter.IsThisParameter())

            Return From root In roots
                   From node In root.DescendantNodesAndSelf()
                   Where node.IsKind(SyntaxKind.IdentifierName)
                   Let identifier = DirectCast(node, IdentifierNameSyntax)
                   Where String.Equals(DirectCast(identifier.Identifier.Value, String), localOrParameter.Name, StringComparison.OrdinalIgnoreCase) AndAlso
                         If(model.GetSymbolInfo(identifier, cancellationToken).Symbol?.Equals(localOrParameter), False)
                   Select node
        End Function

        Friend Shared Function FindStatementAndPartner(
            span As TextSpan,
            body As SyntaxNode,
            partnerBody As SyntaxNode,
            <Out> ByRef partnerStatement As SyntaxNode,
            <Out> ByRef statementPart As Integer) As SyntaxNode

            Dim position = span.Start

            If Not body.FullSpan.Contains(position) Then
                ' invalid position, let's find a labeled node that encompasses the body:
                position = body.SpanStart
            End If

            Dim node As SyntaxNode = Nothing
            If partnerBody IsNot Nothing Then
                FindLeafNodeAndPartner(body, position, partnerBody, node, partnerStatement)
            Else
                node = body.FindToken(position).Parent
                partnerStatement = Nothing
            End If

            ' In some cases active statements may start at the same position.
            ' Consider a nested lambda: 
            '   Function(a) [|[|Function(b)|] a + b|]
            ' There are 2 active statements, one spanning the the body of the outer lambda and 
            ' the other on the nested lambda's header.
            ' Find the parent whose span starts at the same position but it's length is at least as long as the active span's length.
            While node.Span.Length < span.Length AndAlso node.Parent.SpanStart = position
                node = node.Parent
                partnerStatement = partnerStatement?.Parent
            End While

            Debug.Assert(node IsNot Nothing)

            While node IsNot body AndAlso
                  Not SyntaxComparer.Statement.HasLabel(node) AndAlso
                  Not LambdaUtilities.IsLambdaBodyStatementOrExpression(node)

                node = node.Parent
                If partnerStatement IsNot Nothing Then
                    partnerStatement = partnerStatement.Parent
                End If
            End While

            ' In case of local variable declaration an active statement may start with a modified identifier.
            ' If it is a declaration with a simple initializer we want the resulting statement to be the declaration, 
            ' not the identifier.
            If node.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso
               node.Parent.IsKind(SyntaxKind.VariableDeclarator) AndAlso
               DirectCast(node.Parent, VariableDeclaratorSyntax).Names.Count = 1 Then
                node = node.Parent
            End If

            Return node
        End Function

        Friend Overrides Function IsClosureScope(node As SyntaxNode) As Boolean
            Return LambdaUtilities.IsClosureScope(node)
        End Function

        Friend Overrides Function GetCapturedParameterScope(methodOrLambda As SyntaxNode) As SyntaxNode
            Return methodOrLambda
        End Function

        Protected Overrides Function FindEnclosingLambdaBody(encompassingAncestor As SyntaxNode, node As SyntaxNode) As LambdaBody
            While node IsNot encompassingAncestor And node IsNot Nothing
                Dim body As SyntaxNode = Nothing
                If LambdaUtilities.IsLambdaBodyStatementOrExpression(node, body) Then
                    Return SyntaxUtilities.CreateLambdaBody(body)
                End If

                node = node.Parent
            End While

            Return Nothing
        End Function

        Protected Overrides Function ComputeTopLevelMatch(oldCompilationUnit As SyntaxNode, newCompilationUnit As SyntaxNode) As Match(Of SyntaxNode)
            Return SyntaxComparer.TopLevel.ComputeMatch(oldCompilationUnit, newCompilationUnit)
        End Function

        Protected Overrides Function ComputeParameterMap(oldDeclaration As SyntaxNode, newDeclaration As SyntaxNode) As BidirectionalMap(Of SyntaxNode)?
            Dim oldParameterLists = GetDeclarationParameterLists(oldDeclaration)
            Dim newParameterLists = GetDeclarationParameterLists(newDeclaration)

            Dim primaryMatch = GetTopLevelMatch(oldParameterLists.Primary, newParameterLists.Primary)
            Dim secondaryMatch = GetTopLevelMatch(oldParameterLists.Secondary, newParameterLists.Secondary)

            If primaryMatch Is Nothing AndAlso secondaryMatch Is Nothing Then
                Return Nothing
            End If

            Dim map = BidirectionalMap(Of SyntaxNode).FromMatch(If(primaryMatch, secondaryMatch))

            If primaryMatch IsNot Nothing AndAlso secondaryMatch IsNot Nothing Then
                map = map.WithMatch(secondaryMatch)
            End If

            Return map
        End Function

        Private Shared Function GetTopLevelMatch(oldNode As SyntaxNode, newNode As SyntaxNode) As Match(Of SyntaxNode)
            Return If(oldNode IsNot Nothing AndAlso newNode IsNot Nothing, SyntaxComparer.TopLevel.ComputeMatch(oldNode, newNode), Nothing)
        End Function

        Private Shared Function GetDeclarationParameterLists(declaration As SyntaxNode) As (Primary As SyntaxNode, Secondary As SyntaxNode)
            Select Case declaration.Kind
                ' Indexer accessor may have two parameter lists: one on the property and ther other on the accessor
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock
                    Return (DirectCast(declaration.Parent, PropertyBlockSyntax).PropertyStatement.ParameterList,
                            DirectCast(declaration, AccessorBlockSyntax).AccessorStatement.ParameterList)

                Case SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return (DirectCast(declaration, AccessorBlockSyntax).AccessorStatement.ParameterList, Nothing)
            End Select

            Return (declaration.GetParameterList(), Nothing)
        End Function
#End Region

#Region "Syntax And Semantic Utils"
        Protected Overrides Function IsNamespaceDeclaration(node As SyntaxNode) As Boolean
            ' An edit can operate on either just the statement (update) or the whole block (move, insert, delete).
            Return node.IsKind(SyntaxKind.NamespaceStatement, SyntaxKind.NamespaceBlock)
        End Function

        Private Shared Function IsTypeDeclaration(node As SyntaxNode) As Boolean
            Return TypeOf node Is TypeBlockSyntax OrElse TypeOf node Is DelegateStatementSyntax OrElse TypeOf node Is EnumBlockSyntax
        End Function

        Protected Overrides Function IsCompilationUnitWithGlobalStatements(node As SyntaxNode) As Boolean
            Return False
        End Function

        Protected Overrides Function IsGlobalStatement(node As SyntaxNode) As Boolean
            Return False
        End Function

        Protected Overrides Iterator Function GetTopLevelTypeDeclarations(compilationUnit As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim stack = ArrayBuilder(Of SyntaxList(Of StatementSyntax)).GetInstance()

            stack.Add(DirectCast(compilationUnit, CompilationUnitSyntax).Members)

            While stack.Count > 0
                Dim members = stack.Last()
                stack.RemoveLast()

                For Each member In members
                    If IsTypeDeclaration(member) Then
                        Yield member
                    End If

                    Dim namespaceBlock = TryCast(member, NamespaceBlockSyntax)
                    If namespaceBlock IsNot Nothing Then
                        stack.Add(namespaceBlock.Members)
                    End If
                Next
            End While
        End Function

        Protected Overrides ReadOnly Property LineDirectiveKeyword As String
            Get
                Return "ExternalSource"
            End Get
        End Property

        Protected Overrides ReadOnly Property LineDirectiveSyntaxKind As UShort
            Get
                Return SyntaxKind.ExternalSourceDirectiveTrivia
            End Get
        End Property

        Protected Overrides Function GetSyntaxSequenceEdits(oldNodes As ImmutableArray(Of SyntaxNode), newNodes As ImmutableArray(Of SyntaxNode)) As IEnumerable(Of SequenceEdit)
            Return SyntaxComparer.GetSequenceEdits(oldNodes, newNodes)
        End Function

        Friend Overrides ReadOnly Property EmptyCompilationUnit As SyntaxNode
            Get
                Return SyntaxFactory.CompilationUnit()
            End Get
        End Property

        Friend Overrides Function ExperimentalFeaturesEnabled(tree As SyntaxTree) As Boolean
            ' There are no experimental features at this time.
            Return False
        End Function

        Protected Overrides Function StatementLabelEquals(node1 As SyntaxNode, node2 As SyntaxNode) As Boolean
            Return SyntaxComparer.Statement.GetLabelImpl(node1) = SyntaxComparer.Statement.GetLabelImpl(node2)
        End Function

        Private Shared Function ChildrenCompiledInBody(node As SyntaxNode) As Boolean
            Return Not node.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) AndAlso
                   Not node.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) AndAlso
                   Not node.IsKind(SyntaxKind.MultiLineSubLambdaExpression) AndAlso
                   Not node.IsKind(SyntaxKind.SingleLineSubLambdaExpression)
        End Function

        Protected Overrides Function TryGetEnclosingBreakpointSpan(token As SyntaxToken, <Out> ByRef span As TextSpan) As Boolean
            Return BreakpointSpans.TryGetClosestBreakpointSpan(token.Parent, token.SpanStart, minLength:=token.Span.Length, span)
        End Function

        Protected Overrides Function TryGetActiveSpan(node As SyntaxNode, statementPart As Integer, minLength As Integer, <Out> ByRef span As TextSpan) As Boolean
            Return BreakpointSpans.TryGetClosestBreakpointSpan(node, node.SpanStart, minLength, span)
        End Function

        Protected Overrides Iterator Function EnumerateNearStatements(statement As SyntaxNode) As IEnumerable(Of ValueTuple(Of SyntaxNode, Integer))
            Dim direction As Integer = +1
            Dim nodeOrToken As SyntaxNodeOrToken = statement
            Dim propertyOrFieldModifiers As SyntaxTokenList? = GetFieldOrPropertyModifiers(statement)

            While True
                ' If the current statement is the last statement of if-block or try-block statements 
                ' pretend there are no siblings following it.
                Dim lastBlockStatement As SyntaxNode = Nothing
                If nodeOrToken.Parent IsNot Nothing Then
                    If nodeOrToken.Parent.IsKind(SyntaxKind.MultiLineIfBlock) Then
                        lastBlockStatement = DirectCast(nodeOrToken.Parent, MultiLineIfBlockSyntax).Statements.LastOrDefault()
                    ElseIf nodeOrToken.Parent.IsKind(SyntaxKind.SingleLineIfStatement) Then
                        lastBlockStatement = DirectCast(nodeOrToken.Parent, SingleLineIfStatementSyntax).Statements.LastOrDefault()
                    ElseIf nodeOrToken.Parent.IsKind(SyntaxKind.TryBlock) Then
                        lastBlockStatement = DirectCast(nodeOrToken.Parent, TryBlockSyntax).Statements.LastOrDefault()
                    End If
                End If

                If direction > 0 Then
                    If lastBlockStatement IsNot Nothing AndAlso nodeOrToken.AsNode() Is lastBlockStatement Then
                        nodeOrToken = Nothing
                    Else
                        nodeOrToken = nodeOrToken.GetNextSibling()
                    End If
                Else
                    nodeOrToken = nodeOrToken.GetPreviousSibling()
                    If lastBlockStatement IsNot Nothing AndAlso nodeOrToken.AsNode() Is lastBlockStatement Then
                        nodeOrToken = Nothing
                    End If
                End If

                If nodeOrToken.RawKind = 0 Then
                    Dim parent = statement.Parent
                    If parent Is Nothing Then
                        Return
                    End If

                    If direction > 0 Then
                        nodeOrToken = statement
                        direction = -1
                        Continue While
                    End If

                    If propertyOrFieldModifiers.HasValue Then
                        Yield (statement, -1)
                    End If

                    nodeOrToken = parent
                    statement = parent
                    propertyOrFieldModifiers = GetFieldOrPropertyModifiers(statement)
                    direction = +1
                End If

                Dim node = nodeOrToken.AsNode()
                If node Is Nothing Then
                    Continue While
                End If

                If propertyOrFieldModifiers.HasValue Then
                    Dim nodeModifiers = GetFieldOrPropertyModifiers(node)

                    If Not nodeModifiers.HasValue OrElse
                       propertyOrFieldModifiers.Value.Any(SyntaxKind.SharedKeyword) <> nodeModifiers.Value.Any(SyntaxKind.SharedKeyword) Then
                        Continue While
                    End If
                End If

                Yield (node, DefaultStatementPart)
            End While
        End Function

        Private Shared Function GetFieldOrPropertyModifiers(node As SyntaxNode) As SyntaxTokenList?
            If node.IsKind(SyntaxKind.FieldDeclaration) Then
                Return DirectCast(node, FieldDeclarationSyntax).Modifiers
            ElseIf node.IsKind(SyntaxKind.PropertyStatement) Then
                Return DirectCast(node, PropertyStatementSyntax).Modifiers
            Else
                Return Nothing
            End If
        End Function

        Private Shared Function AreEquivalentIgnoringLambdaBodies(left As SyntaxNode, right As SyntaxNode) As Boolean
            ' usual case
            If SyntaxFactory.AreEquivalent(left, right) Then
                Return True
            End If

            Return LambdaUtilities.AreEquivalentIgnoringLambdaBodies(left, right)
        End Function

        Protected Overrides Function AreEquivalentActiveStatements(oldStatement As SyntaxNode, newStatement As SyntaxNode, statementPart As Integer) As Boolean
            If oldStatement.RawKind <> newStatement.RawKind Then
                Return False
            End If

            ' Dim a,b,c As <NewExpression>
            ' We need to check the actual initializer expression in addition to the identifier.
            If HasMultiInitializer(oldStatement) Then
                Return AreEquivalentIgnoringLambdaBodies(oldStatement, newStatement) AndAlso
                       AreEquivalentIgnoringLambdaBodies(DirectCast(oldStatement.Parent, VariableDeclaratorSyntax).AsClause,
                                                         DirectCast(newStatement.Parent, VariableDeclaratorSyntax).AsClause)
            End If

            Select Case oldStatement.Kind
                Case SyntaxKind.SubNewStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    ' Header statements are nops. Changes in the header statements are changes in top-level surface
                    ' which should not be reported as active statement rude edits.
                    Return True

                Case Else
                    Return AreEquivalentIgnoringLambdaBodies(oldStatement, newStatement)
            End Select
        End Function

        Private Shared Function HasMultiInitializer(modifiedIdentifier As SyntaxNode) As Boolean
            Return modifiedIdentifier.Parent.IsKind(SyntaxKind.VariableDeclarator) AndAlso
                   DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax).Names.Count > 1
        End Function

        Protected Overrides Function AreEquivalentImpl(oldToken As SyntaxToken, newToken As SyntaxToken) As Boolean
            Return SyntaxFactory.AreEquivalent(oldToken, newToken)
        End Function

        Friend Overrides Function IsInterfaceDeclaration(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.InterfaceBlock)
        End Function

        Friend Overrides Function IsRecordDeclaration(node As SyntaxNode) As Boolean
            ' No records in VB
            Return False
        End Function

        Friend Overrides Function TryGetContainingTypeDeclaration(node As SyntaxNode) As SyntaxNode
            Return node.Parent.FirstAncestorOrSelf(Of TypeBlockSyntax)() ' TODO: EnbumBlock?
        End Function

        Friend Overrides Function IsDeclarationWithInitializer(declaration As SyntaxNode) As Boolean
            Select Case declaration.Kind
                Case SyntaxKind.ModifiedIdentifier
                    Debug.Assert(declaration.Parent.IsKind(SyntaxKind.VariableDeclarator) OrElse
                                 declaration.Parent.IsKind(SyntaxKind.Parameter))

                    If Not declaration.Parent.IsKind(SyntaxKind.VariableDeclarator) Then
                        Return False
                    End If

                    Dim declarator = DirectCast(declaration.Parent, VariableDeclaratorSyntax)

                    Dim identifier = DirectCast(declaration, ModifiedIdentifierSyntax)
                    Return identifier.ArrayBounds IsNot Nothing OrElse
                           GetInitializerExpression(declarator.Initializer, declarator.AsClause) IsNot Nothing

                Case SyntaxKind.PropertyStatement
                    Dim propertyStatement = DirectCast(declaration, PropertyStatementSyntax)
                    Return GetInitializerExpression(propertyStatement.Initializer, propertyStatement.AsClause) IsNot Nothing

                Case Else
                    Return False
            End Select
        End Function

        Friend Overrides Function IsPrimaryConstructorDeclaration(declaration As SyntaxNode) As Boolean
            Return False
        End Function

        Private Shared Function GetInitializerExpression(equalsValue As EqualsValueSyntax, asClause As AsClauseSyntax) As ExpressionSyntax
            If equalsValue IsNot Nothing Then
                Return equalsValue.Value
            End If

            If asClause IsNot Nothing AndAlso asClause.IsKind(SyntaxKind.AsNewClause) Then
                Return DirectCast(asClause, AsNewClauseSyntax).NewExpression
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' VB symbols return references that represent the declaration statement.
        ''' The node that represents the whole declaration (the block) is the parent node if it exists.
        ''' For example, a method with a body is represented by a SubBlock/FunctionBlock while a method without a body
        ''' is represented by its declaration statement.
        ''' </summary>
        Protected Overrides Function GetSymbolDeclarationSyntax(symbol As ISymbol, selector As Func(Of ImmutableArray(Of SyntaxReference), SyntaxReference), cancellationToken As CancellationToken) As SyntaxNode
            ' Invoke method of a delegate type doesn't have DeclaringSyntaxReferences
            Dim syntaxRefs As ImmutableArray(Of SyntaxReference)

            If symbol Is symbol.ContainingType?.DelegateInvokeMethod Then
                syntaxRefs = symbol.ContainingType.DeclaringSyntaxReferences
                If syntaxRefs.IsEmpty Then
                    Dim parameter = DirectCast(symbol, IMethodSymbol).Parameters.First()
                    Return parameter.DeclaringSyntaxReferences.Single().GetSyntax(cancellationToken).Parent.Parent
                End If
            Else
                syntaxRefs = symbol.DeclaringSyntaxReferences
            End If

            Dim syntax = selector(syntaxRefs)?.GetSyntax(cancellationToken)
            If syntax Is Nothing Then
                Return Nothing
            End If

            Dim parent = syntax.Parent

            Select Case syntax.Kind
                ' declarations that always have block

                Case SyntaxKind.NamespaceStatement
                    Debug.Assert(parent.Kind = SyntaxKind.NamespaceBlock)
                    Return parent

                Case SyntaxKind.ClassStatement
                    Debug.Assert(parent.Kind = SyntaxKind.ClassBlock)
                    Return parent

                Case SyntaxKind.StructureStatement
                    Debug.Assert(parent.Kind = SyntaxKind.StructureBlock)
                    Return parent

                Case SyntaxKind.InterfaceStatement
                    Debug.Assert(parent.Kind = SyntaxKind.InterfaceBlock)
                    Return parent

                Case SyntaxKind.ModuleStatement
                    Debug.Assert(parent.Kind = SyntaxKind.ModuleBlock)
                    Return parent

                Case SyntaxKind.EnumStatement
                    Debug.Assert(parent.Kind = SyntaxKind.EnumBlock)
                    Return parent

                Case SyntaxKind.SubNewStatement
                    Debug.Assert(parent.Kind = SyntaxKind.ConstructorBlock)
                    Return parent

                Case SyntaxKind.OperatorStatement
                    Debug.Assert(parent.Kind = SyntaxKind.OperatorBlock)
                    Return parent

                Case SyntaxKind.GetAccessorStatement
                    Debug.Assert(parent.Kind = SyntaxKind.GetAccessorBlock)
                    Return parent

                Case SyntaxKind.SetAccessorStatement
                    Debug.Assert(parent.Kind = SyntaxKind.SetAccessorBlock)
                    Return parent

                Case SyntaxKind.AddHandlerAccessorStatement
                    Debug.Assert(parent.Kind = SyntaxKind.AddHandlerAccessorBlock)
                    Return parent

                Case SyntaxKind.RemoveHandlerAccessorStatement
                    Debug.Assert(parent.Kind = SyntaxKind.RemoveHandlerAccessorBlock)
                    Return parent

                Case SyntaxKind.RaiseEventAccessorStatement
                    Debug.Assert(parent.Kind = SyntaxKind.RaiseEventAccessorBlock)
                    Return parent

                ' declarations that may or may not have block

                Case SyntaxKind.SubStatement
                    Return If(parent.Kind = SyntaxKind.SubBlock, parent, syntax)

                Case SyntaxKind.FunctionStatement
                    Return If(parent.Kind = SyntaxKind.FunctionBlock, parent, syntax)

                Case SyntaxKind.PropertyStatement
                    Return If(parent.Kind = SyntaxKind.PropertyBlock, parent, syntax)

                Case SyntaxKind.EventStatement
                    Return If(parent.Kind = SyntaxKind.EventBlock, parent, syntax)

                ' declarations that never have a block

                Case SyntaxKind.ModifiedIdentifier
                    ' Field defined in a field declaration, or a locla variable defined in local declaration, For Each, For, Using, etc.
                    Return syntax

                Case SyntaxKind.VariableDeclarator
                    ' fields are represented by ModifiedIdentifier:
                    Throw ExceptionUtilities.UnexpectedValue(syntax.Kind)

                Case Else
                    Return syntax
            End Select
        End Function

        Protected Overrides Function GetDeclaredSymbol(model As SemanticModel, declaration As SyntaxNode, cancellationToken As CancellationToken) As ISymbol
            Return model.GetDeclaredSymbol(declaration, cancellationToken)
        End Function

        Friend Overrides Function IsConstructorWithMemberInitializers(symbol As ISymbol, cancellationToken As CancellationToken) As Boolean
            Dim method = TryCast(symbol, IMethodSymbol)
            If method Is Nothing OrElse (method.MethodKind <> MethodKind.Constructor AndAlso method.MethodKind <> MethodKind.SharedConstructor) Then
                Return False
            End If

            ' static constructor has initializers:
            If method.IsStatic Then
                Return True
            End If

            ' Default constructor has initializers unless the type is a struct.
            ' Instance member initializers in a struct are not supported in VB.
            If method.IsImplicitlyDeclared Then
                Return method.ContainingType.TypeKind <> TypeKind.Struct
            End If

            Dim ctor = TryCast(symbol.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken).Parent, ConstructorBlockSyntax)
            If ctor Is Nothing Then
                Return False
            End If

            ' Constructor includes field initializers if the first statement 
            ' isn't a call to another constructor of the declaring class or module.

            If ctor.Statements.Count = 0 Then
                Return True
            End If

            Dim firstStatement = ctor.Statements.First
            If Not firstStatement.IsKind(SyntaxKind.ExpressionStatement) Then
                Return True
            End If

            Dim expressionStatement = DirectCast(firstStatement, ExpressionStatementSyntax)
            If Not expressionStatement.Expression.IsKind(SyntaxKind.InvocationExpression) Then
                Return True
            End If

            Dim invocation = DirectCast(expressionStatement.Expression, InvocationExpressionSyntax)
            If Not invocation.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Return True
            End If

            Dim memberAccess = DirectCast(invocation.Expression, MemberAccessExpressionSyntax)
            If Not memberAccess.Name.IsKind(SyntaxKind.IdentifierName) OrElse
               Not memberAccess.Name.Identifier.IsKind(SyntaxKind.IdentifierToken) Then
                Return True
            End If

            ' Note that ValueText returns "New" for both New and [New]
            If Not String.Equals(memberAccess.Name.Identifier.ToString(), "New", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If

            Return memberAccess.Expression.IsKind(SyntaxKind.MyBaseKeyword)
        End Function

        Friend Overrides Function IsPartial(type As INamedTypeSymbol) As Boolean
            Dim syntaxRefs = type.DeclaringSyntaxReferences
            Return syntaxRefs.Length > 1 OrElse
                   DirectCast(syntaxRefs.Single().GetSyntax(), TypeStatementSyntax).Modifiers.Any(SyntaxKind.PartialKeyword)
        End Function

        Protected Overrides Function GetEditedSymbols(
            editKind As EditKind,
            oldNode As SyntaxNode,
            newNode As SyntaxNode,
            oldModel As SemanticModel,
            newModel As SemanticModel,
            cancellationToken As CancellationToken) As OneOrMany(Of (oldSymbol As ISymbol, newSymbol As ISymbol))

            Dim oldSymbols = OneOrMany(Of ISymbol).Empty
            Dim newSymbols = OneOrMany(Of ISymbol).Empty

            If oldNode IsNot Nothing AndAlso Not TryGetSyntaxNodesForEdit(editKind, oldNode, oldModel, oldSymbols, cancellationToken) OrElse
               newNode IsNot Nothing AndAlso Not TryGetSyntaxNodesForEdit(editKind, newNode, newModel, newSymbols, cancellationToken) Then
                Return OneOrMany(Of (ISymbol, ISymbol)).Empty
            End If

            Debug.Assert(Not oldSymbols.IsEmpty OrElse Not newSymbols.IsEmpty)

            If oldSymbols.Count <= 1 AndAlso newSymbols.Count <= 1 Then
                Return OneOrMany.Create((oldSymbols.FirstOrDefault(), newSymbols.FirstOrDefault()))
            End If

            ' This only occurs when field identifiers are deleted/inserted/reordered from/to/within their variable declarator list,
            ' or their shared initializer is updated. The particular inserted and deleted fields will be represented by separate edits,
            ' but the AsNew clause of the declarator may have been updated as well, which needs to update the remaining (matching) fields.
            Return OneOrMany.Create(PairSymbols(oldSymbols, newSymbols).ToImmutableArray())
        End Function

        Private Shared Iterator Function PairSymbols(
            oldSymbols As OneOrMany(Of ISymbol),
            newSymbols As OneOrMany(Of ISymbol)) As IEnumerable(Of (ISymbol, ISymbol))

            For Each oldSymbol In oldSymbols
                Dim newSymbol = newSymbols.FirstOrDefault(Function(s, o) CaseInsensitiveComparison.Equals(s.Name, o.Name), oldSymbol)
                If newSymbol IsNot Nothing Then
                    Yield (oldSymbol, newSymbol)
                End If
            Next
        End Function

        Protected Overrides Sub AddSymbolEdits(
            ByRef result As TemporaryArray(Of (ISymbol, ISymbol, EditKind)),
            editKind As EditKind,
            oldNode As SyntaxNode,
            oldSymbol As ISymbol,
            newNode As SyntaxNode,
            newSymbol As ISymbol,
            oldModel As SemanticModel,
            newModel As SemanticModel,
            topMatch As Match(Of SyntaxNode),
            editMap As IReadOnlyDictionary(Of SyntaxNode, EditKind),
            symbolCache As SymbolInfoCache,
            cancellationToken As CancellationToken)

            Debug.Assert(oldSymbol IsNot Nothing OrElse newSymbol IsNot Nothing)

            If oldNode.IsKind(SyntaxKind.Parameter, SyntaxKind.TypeParameter) OrElse
               oldNode.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso oldNode.IsParentKind(SyntaxKind.Parameter) OrElse
               newNode.IsKind(SyntaxKind.Parameter, SyntaxKind.TypeParameter) OrElse
               newNode.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso newNode.IsParentKind(SyntaxKind.Parameter) Then

                ' parameter list, member, Or type declaration
                Dim oldContainingMemberOrType = GetParameterContainingMemberOrType(oldNode, newNode, oldModel, topMatch.ReverseMatches, cancellationToken)
                Dim newContainingMemberOrType = GetParameterContainingMemberOrType(newNode, oldNode, newModel, topMatch.Matches, cancellationToken)

                Dim matchingNewContainingMemberOrType = GetSemanticallyMatchingNewSymbol(oldContainingMemberOrType, newContainingMemberOrType, newModel, symbolCache, cancellationToken)

                ' Any change to a constraint should be analyzed as an update of the type parameter
                Dim isTypeConstraint = TypeOf oldNode Is TypeParameterConstraintClauseSyntax OrElse
                                       TypeOf newNode Is TypeParameterConstraintClauseSyntax

                ' If the signature of a property changed or its parameter has been renamed we need to update all its accessors
                Dim oldPropertySymbol = TryCast(oldContainingMemberOrType, IPropertySymbol)
                Dim newPropertySymbol = TryCast(newContainingMemberOrType, IPropertySymbol)

                If oldPropertySymbol IsNot Nothing AndAlso
                   newPropertySymbol IsNot Nothing AndAlso
                   (IsMemberOrDelegateReplaced(oldPropertySymbol, newPropertySymbol) OrElse
                    oldSymbol IsNot Nothing AndAlso newSymbol IsNot Nothing AndAlso oldSymbol.Name <> newSymbol.Name) Then

                    AddMemberUpdate(result, oldPropertySymbol.GetMethod, newPropertySymbol.GetMethod, matchingNewContainingMemberOrType)
                    AddMemberUpdate(result, oldPropertySymbol.SetMethod, newPropertySymbol.SetMethod, matchingNewContainingMemberOrType)
                End If

                AddMemberUpdate(result, oldContainingMemberOrType, newContainingMemberOrType, matchingNewContainingMemberOrType)

                If matchingNewContainingMemberOrType IsNot Nothing Then
                    ' Map parameter to the corresponding semantically matching member.
                    ' Since the signature of the member matches we can direcly map by parameter ordinal.
                    If oldSymbol.Kind = SymbolKind.Parameter Then
                        newSymbol = matchingNewContainingMemberOrType.GetParameters()(DirectCast(oldSymbol, IParameterSymbol).Ordinal)
                    ElseIf oldSymbol.Kind = SymbolKind.TypeParameter Then
                        newSymbol = matchingNewContainingMemberOrType.GetTypeParameters()(DirectCast(oldSymbol, ITypeParameterSymbol).Ordinal)
                    End If
                End If

                result.Add((oldSymbol, newSymbol, If(isTypeConstraint, EditKind.Update, editKind)))

                Return
            End If

            Select Case editKind
                Case EditKind.Reorder
                    If oldSymbol Is Nothing OrElse newSymbol Is Nothing Then
                        Return
                    End If

                    result.Add((oldSymbol.ContainingSymbol, newSymbol.ContainingSymbol, EditKind.Update))

                Case EditKind.Delete
                    result.Add((oldSymbol, Nothing, editKind))

                Case EditKind.Insert
                    result.Add((Nothing, newSymbol, editKind))

                Case EditKind.Update
                    ' Updates of a property/indexer/event node might affect its accessors.
                    ' Return all affected symbols for these updates so that the changes in the accessor bodies get analyzed.

                    Dim oldPropertySymbol = TryCast(oldSymbol, IPropertySymbol)
                    Dim newPropertySymbol = TryCast(newSymbol, IPropertySymbol)
                    If oldPropertySymbol IsNot Nothing AndAlso newPropertySymbol IsNot Nothing Then
                        ' Note: a signature change does not affect the property itself.
                        result.Add((oldPropertySymbol, newPropertySymbol, EditKind.Update))

                        If oldPropertySymbol.GetMethod IsNot Nothing OrElse newPropertySymbol.GetMethod IsNot Nothing Then
                            If DiffersInAccessibilityModifiers(oldPropertySymbol.GetMethod, newPropertySymbol.GetMethod) OrElse
                               IsMemberOrDelegateReplaced(oldPropertySymbol, newPropertySymbol) Then
                                result.Add((oldPropertySymbol.GetMethod, newPropertySymbol.GetMethod, editKind))
                            End If
                        End If

                        If oldPropertySymbol.SetMethod IsNot Nothing OrElse newPropertySymbol.SetMethod IsNot Nothing Then
                            If DiffersInAccessibilityModifiers(oldPropertySymbol.SetMethod, newPropertySymbol.SetMethod) OrElse
                               IsMemberOrDelegateReplaced(oldPropertySymbol, newPropertySymbol) Then
                                result.Add((oldPropertySymbol.SetMethod, newPropertySymbol.SetMethod, editKind))
                            End If
                        End If

                        Return
                    End If

                    Dim oldEventSymbol = TryCast(oldSymbol, IEventSymbol)
                    Dim newEventSymbol = TryCast(newSymbol, IEventSymbol)
                    If oldEventSymbol IsNot Nothing AndAlso newEventSymbol IsNot Nothing Then
                        result.Add((oldEventSymbol, newEventSymbol, EditKind.Update))

                        If oldEventSymbol.AddMethod IsNot Nothing OrElse newEventSymbol.AddMethod IsNot Nothing Then
                            If IsMemberOrDelegateReplaced(oldEventSymbol, newEventSymbol) Then
                                result.Add((oldEventSymbol.AddMethod, newEventSymbol.AddMethod, editKind))
                            End If
                        End If

                        If oldEventSymbol.RemoveMethod IsNot Nothing OrElse newEventSymbol.RemoveMethod IsNot Nothing Then
                            If IsMemberOrDelegateReplaced(oldEventSymbol, newEventSymbol) Then
                                result.Add((oldEventSymbol.RemoveMethod, newEventSymbol.RemoveMethod, editKind))
                            End If
                        End If

                        If oldEventSymbol.RaiseMethod IsNot Nothing OrElse newEventSymbol.RaiseMethod IsNot Nothing Then
                            ' change in event type does not affect Raise method, but rename does
                            If oldEventSymbol.Name <> newEventSymbol.Name Then
                                result.Add((oldEventSymbol.RaiseMethod, newEventSymbol.RaiseMethod, editKind))
                            End If
                        End If

                        Return
                    End If

                    result.Add((oldSymbol, newSymbol, editKind))

                Case EditKind.Move
                    Contract.ThrowIfNull(oldNode)
                    Contract.ThrowIfNull(newNode)
                    Contract.ThrowIfNull(oldModel)

                    Debug.Assert(oldNode.RawKind = newNode.RawKind)
                    If Not IsTypeDeclaration(oldNode) Then
                        Return
                    End If

                    result.Add((oldSymbol, newSymbol, editKind))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(editKind)
            End Select
        End Sub

        Private Shared Function DiffersInAccessibilityModifiers(oldMethod As IMethodSymbol, newMethod As IMethodSymbol) As Boolean
            Return oldMethod IsNot Nothing AndAlso
               newMethod IsNot Nothing AndAlso
               oldMethod.DeclaredAccessibility <> newMethod.DeclaredAccessibility
        End Function

        Private Function TryGetSyntaxNodesForEdit(
            editKind As EditKind,
            node As SyntaxNode,
            model As SemanticModel,
            <Out> ByRef symbols As OneOrMany(Of ISymbol),
            cancellationToken As CancellationToken) As Boolean

            Select Case node.Kind()
                Case SyntaxKind.ImportsStatement,
                     SyntaxKind.NamespaceBlock,
                     SyntaxKind.NamespaceStatement
                    Return False

                Case SyntaxKind.VariableDeclarator
                    ' Initializer or As clause update
                    Dim variableDeclarator = CType(node, VariableDeclaratorSyntax)
                    If variableDeclarator.Names.Count > 1 Then
                        symbols = OneOrMany.Create(variableDeclarator.Names.SelectAsArray(Function(n) GetDeclaredSymbol(model, n, cancellationToken)))
                        Return True
                    End If

                    node = variableDeclarator.Names(0)

                Case SyntaxKind.FieldDeclaration
                    ' Attribute or modifier update
                    If editKind = EditKind.Update Then
                        Dim field = CType(node, FieldDeclarationSyntax)
                        If field.Declarators.Count = 1 AndAlso field.Declarators(0).Names.Count = 1 Then
                            node = field.Declarators(0).Names(0)
                        Else
                            symbols = OneOrMany.Create(
                                (From declarator In field.Declarators
                                 From name In declarator.Names
                                 Select GetDeclaredSymbol(model, name, cancellationToken)).ToImmutableArray())

                            Return True
                        End If
                    End If

            End Select

            Dim symbol = GetDeclaredSymbol(model, node, cancellationToken)
            If symbol Is Nothing Then
                Return False
            End If

            symbols = OneOrMany.Create(symbol)
            Return True
        End Function

        Private Function GetParameterContainingMemberOrType(node As SyntaxNode, otherNode As SyntaxNode, model As SemanticModel, fromOtherMap As IReadOnlyDictionary(Of SyntaxNode, SyntaxNode), cancellationToken As CancellationToken) As ISymbol
            Debug.Assert(node Is Nothing OrElse
                         node.IsKind(SyntaxKind.Parameter, SyntaxKind.TypeParameter) OrElse
                         node.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso node.IsParentKind(SyntaxKind.Parameter) OrElse
                         TypeOf node Is TypeParameterConstraintClauseSyntax)

            ' parameter list, member, or type declaration
            Dim declaration As SyntaxNode = Nothing
            If node Is Nothing Then
                fromOtherMap.TryGetValue(GetContainingDeclaration(otherNode), declaration)
            Else
                declaration = GetContainingDeclaration(node)
            End If

            Return If(declaration IsNot Nothing, GetDeclaredSymbol(model, declaration, cancellationToken), Nothing)
        End Function

        Private Shared Function GetContainingDeclaration(node As SyntaxNode) As SyntaxNode
            Return If(node.IsKind(SyntaxKind.ModifiedIdentifier), node.Parent.Parent.Parent, node.Parent.Parent)
        End Function

        Friend Overrides ReadOnly Property IsLambda As Func(Of SyntaxNode, Boolean)
            Get
                Return AddressOf LambdaUtilities.IsLambda
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotLambda As Func(Of SyntaxNode, Boolean)
            Get
                Return AddressOf LambdaUtilities.IsNotLambda
            End Get
        End Property

        Friend Overrides Function IsNestedFunction(node As SyntaxNode) As Boolean
            Return TypeOf node Is LambdaExpressionSyntax
        End Function

        Friend Overrides Function IsLocalFunction(node As SyntaxNode) As Boolean
            Return False
        End Function

        Friend Overrides Function IsGenericLocalFunction(node As SyntaxNode) As Boolean
            Return False
        End Function

        Friend Overrides Function TryGetLambdaBodies(node As SyntaxNode, <Out> ByRef body1 As LambdaBody, <Out> ByRef body2 As LambdaBody) As Boolean
            Dim bodyNode1 As SyntaxNode = Nothing
            Dim bodyNode2 As SyntaxNode = Nothing
            If LambdaUtilities.TryGetLambdaBodies(node, bodyNode1, bodyNode2) Then
                body1 = SyntaxUtilities.CreateLambdaBody(bodyNode1)
                body2 = If(bodyNode2 IsNot Nothing, SyntaxUtilities.CreateLambdaBody(bodyNode2), Nothing)
                Return True
            End If

            Return False
        End Function

        Friend Overrides Function GetLambdaExpressionSymbol(model As SemanticModel, lambdaExpression As SyntaxNode, cancellationToken As CancellationToken) As IMethodSymbol
            Dim lambdaExpressionSyntax = DirectCast(lambdaExpression, LambdaExpressionSyntax)

            ' The semantic model only returns the lambda symbol for positions that are within the body of the lambda (not the header)
            Return DirectCast(model.GetEnclosingSymbol(lambdaExpressionSyntax.SubOrFunctionHeader.Span.End, cancellationToken), IMethodSymbol)
        End Function

        Friend Overrides Function GetContainingQueryExpression(node As SyntaxNode) As SyntaxNode
            Return node.FirstAncestorOrSelf(Of QueryExpressionSyntax)
        End Function

        Friend Overrides Function QueryClauseLambdasTypeEquivalent(oldModel As SemanticModel, oldNode As SyntaxNode, newModel As SemanticModel, newNode As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Select Case oldNode.Kind
                Case SyntaxKind.AggregateClause
                    Dim oldInfo = oldModel.GetAggregateClauseSymbolInfo(DirectCast(oldNode, AggregateClauseSyntax), cancellationToken)
                    Dim newInfo = newModel.GetAggregateClauseSymbolInfo(DirectCast(newNode, AggregateClauseSyntax), cancellationToken)
                    Return MemberOrDelegateSignaturesEquivalent(oldInfo.Select1.Symbol, newInfo.Select1.Symbol) AndAlso
                           MemberOrDelegateSignaturesEquivalent(oldInfo.Select2.Symbol, newInfo.Select2.Symbol)

                Case SyntaxKind.CollectionRangeVariable
                    Dim oldInfo = oldModel.GetCollectionRangeVariableSymbolInfo(DirectCast(oldNode, CollectionRangeVariableSyntax), cancellationToken)
                    Dim newInfo = newModel.GetCollectionRangeVariableSymbolInfo(DirectCast(newNode, CollectionRangeVariableSyntax), cancellationToken)
                    Return MemberOrDelegateSignaturesEquivalent(oldInfo.AsClauseConversion.Symbol, newInfo.AsClauseConversion.Symbol) AndAlso
                           MemberOrDelegateSignaturesEquivalent(oldInfo.SelectMany.Symbol, newInfo.SelectMany.Symbol) AndAlso
                           MemberOrDelegateSignaturesEquivalent(oldInfo.ToQueryableCollectionConversion.Symbol, newInfo.ToQueryableCollectionConversion.Symbol)

                Case SyntaxKind.FunctionAggregation
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, FunctionAggregationSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, FunctionAggregationSyntax), cancellationToken)
                    Return MemberOrDelegateSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case SyntaxKind.ExpressionRangeVariable
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, ExpressionRangeVariableSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, ExpressionRangeVariableSyntax), cancellationToken)
                    Return MemberOrDelegateSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, OrderingSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, OrderingSyntax), cancellationToken)
                    Return MemberOrDelegateSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case SyntaxKind.FromClause,
                     SyntaxKind.WhereClause,
                     SyntaxKind.SkipClause,
                     SyntaxKind.TakeClause,
                     SyntaxKind.SkipWhileClause,
                     SyntaxKind.TakeWhileClause,
                     SyntaxKind.GroupByClause,
                     SyntaxKind.SimpleJoinClause,
                     SyntaxKind.GroupJoinClause,
                     SyntaxKind.SelectClause
                    Dim oldInfo = oldModel.GetSymbolInfo(DirectCast(oldNode, QueryClauseSyntax), cancellationToken)
                    Dim newInfo = newModel.GetSymbolInfo(DirectCast(newNode, QueryClauseSyntax), cancellationToken)
                    Return MemberOrDelegateSignaturesEquivalent(oldInfo.Symbol, newInfo.Symbol)

                Case Else
                    Return True
            End Select
        End Function
#End Region

#Region "Diagnostic Info"
        Protected Overrides ReadOnly Property ErrorDisplayFormat As SymbolDisplayFormat
            Get
                Return SymbolDisplayFormat.VisualBasicShortErrorMessageFormat
            End Get
        End Property

        Protected Overrides Function TryGetDiagnosticSpan(node As SyntaxNode, editKind As EditKind) As TextSpan?
            Return TryGetDiagnosticSpanImpl(node, editKind)
        End Function

        Protected Overloads Shared Function GetDiagnosticSpan(node As SyntaxNode, editKind As EditKind) As TextSpan
            Return If(TryGetDiagnosticSpanImpl(node, editKind), node.Span)
        End Function

        Private Shared Function TryGetDiagnosticSpanImpl(node As SyntaxNode, editKind As EditKind) As TextSpan?
            Return TryGetDiagnosticSpanImpl(node.Kind, node, editKind)
        End Function

        Protected Overrides Function GetBodyDiagnosticSpan(node As SyntaxNode, editKind As EditKind) As TextSpan
            Return GetDiagnosticSpan(node, editKind)
        End Function

        ' internal for testing; kind is passed explicitly for testing as well
        Friend Shared Function TryGetDiagnosticSpanImpl(kind As SyntaxKind, node As SyntaxNode, editKind As EditKind) As TextSpan?
            Select Case kind
                Case SyntaxKind.CompilationUnit
                    Dim unit = DirectCast(node, CompilationUnitSyntax)

                    Dim globalNode = unit.ChildNodes().FirstOrDefault()
                    If globalNode Is Nothing Then
                        Return Nothing
                    End If

                    Return GetDiagnosticSpan(globalNode, editKind)

                Case SyntaxKind.OptionStatement,
                     SyntaxKind.ImportsStatement
                    Return node.Span

                Case SyntaxKind.NamespaceBlock
                    Return GetDiagnosticSpan(DirectCast(node, NamespaceBlockSyntax).NamespaceStatement)

                Case SyntaxKind.NamespaceStatement
                    Return GetDiagnosticSpan(DirectCast(node, NamespaceStatementSyntax))

                Case SyntaxKind.ClassBlock,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock
                    Return GetDiagnosticSpan(DirectCast(node, TypeBlockSyntax).BlockStatement)

                Case SyntaxKind.ClassStatement,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.ModuleStatement
                    Return GetDiagnosticSpan(DirectCast(node, TypeStatementSyntax))

                Case SyntaxKind.EnumBlock
                    Return TryGetDiagnosticSpanImpl(DirectCast(node, EnumBlockSyntax).EnumStatement, editKind)

                Case SyntaxKind.EnumStatement
                    Dim enumStatement = DirectCast(node, EnumStatementSyntax)
                    Return GetDiagnosticSpan(enumStatement.Modifiers, enumStatement.EnumKeyword, enumStatement.Identifier)

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return GetDiagnosticSpan(DirectCast(node, MethodBlockBaseSyntax).BlockStatement)

                Case SyntaxKind.EventBlock
                    Return GetDiagnosticSpan(DirectCast(node, EventBlockSyntax).EventStatement)

                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.EventStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement
                    Return GetDiagnosticSpan(DirectCast(node, MethodBaseSyntax))

                Case SyntaxKind.PropertyBlock
                    Return GetDiagnosticSpan(DirectCast(node, PropertyBlockSyntax).PropertyStatement)

                Case SyntaxKind.PropertyStatement
                    Return GetDiagnosticSpan(DirectCast(node, PropertyStatementSyntax))

                Case SyntaxKind.FieldDeclaration
                    Dim fieldDeclaration = DirectCast(node, FieldDeclarationSyntax)
                    Return GetDiagnosticSpan(fieldDeclaration.Modifiers, fieldDeclaration.Declarators.First, fieldDeclaration.Declarators.Last)

                Case SyntaxKind.VariableDeclarator,
                     SyntaxKind.ModifiedIdentifier,
                     SyntaxKind.EnumMemberDeclaration,
                     SyntaxKind.TypeParameterSingleConstraintClause,
                     SyntaxKind.TypeParameterMultipleConstraintClause,
                     SyntaxKind.ClassConstraint,
                     SyntaxKind.StructureConstraint,
                     SyntaxKind.NewConstraint,
                     SyntaxKind.TypeConstraint
                    Return node.Span

                Case SyntaxKind.TypeParameter
                    Return DirectCast(node, TypeParameterSyntax).Identifier.Span

                Case SyntaxKind.TypeParameterList,
                     SyntaxKind.ParameterList,
                     SyntaxKind.AttributeList,
                     SyntaxKind.SimpleAsClause
                    If editKind = EditKind.Delete Then
                        Return TryGetDiagnosticSpanImpl(node.Parent, editKind)
                    Else
                        Return node.Span
                    End If

                Case SyntaxKind.AttributesStatement,
                     SyntaxKind.Attribute
                    Return node.Span

                Case SyntaxKind.Parameter
                    Dim parameter = DirectCast(node, ParameterSyntax)
                    Return GetDiagnosticSpan(parameter.Modifiers, parameter.Identifier, parameter)

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return GetDiagnosticSpan(DirectCast(node, LambdaExpressionSyntax).SubOrFunctionHeader)

                Case SyntaxKind.MultiLineIfBlock
                    Dim ifStatement = DirectCast(node, MultiLineIfBlockSyntax).IfStatement
                    Return GetDiagnosticSpan(ifStatement.IfKeyword, ifStatement.Condition, ifStatement.ThenKeyword)

                Case SyntaxKind.ElseIfBlock
                    Dim elseIfStatement = DirectCast(node, ElseIfBlockSyntax).ElseIfStatement
                    Return GetDiagnosticSpan(elseIfStatement.ElseIfKeyword, elseIfStatement.Condition, elseIfStatement.ThenKeyword)

                Case SyntaxKind.SingleLineIfStatement
                    Dim ifStatement = DirectCast(node, SingleLineIfStatementSyntax)
                    Return GetDiagnosticSpan(ifStatement.IfKeyword, ifStatement.Condition, ifStatement.ThenKeyword)

                Case SyntaxKind.SingleLineElseClause
                    Return DirectCast(node, SingleLineElseClauseSyntax).ElseKeyword.Span

                Case SyntaxKind.TryBlock
                    Return DirectCast(node, TryBlockSyntax).TryStatement.TryKeyword.Span

                Case SyntaxKind.CatchBlock
                    Return DirectCast(node, CatchBlockSyntax).CatchStatement.CatchKeyword.Span

                Case SyntaxKind.FinallyBlock
                    Return DirectCast(node, FinallyBlockSyntax).FinallyStatement.FinallyKeyword.Span

                Case SyntaxKind.SyncLockBlock
                    Return DirectCast(node, SyncLockBlockSyntax).SyncLockStatement.Span

                Case SyntaxKind.WithBlock
                    Return DirectCast(node, WithBlockSyntax).WithStatement.Span

                Case SyntaxKind.UsingBlock
                    Return DirectCast(node, UsingBlockSyntax).UsingStatement.Span

                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock
                    Return DirectCast(node, DoLoopBlockSyntax).DoStatement.Span

                Case SyntaxKind.WhileBlock
                    Return DirectCast(node, WhileBlockSyntax).WhileStatement.Span

                Case SyntaxKind.ForEachBlock,
                     SyntaxKind.ForBlock
                    Return DirectCast(node, ForOrForEachBlockSyntax).ForOrForEachStatement.Span

                Case SyntaxKind.AwaitExpression
                    Return DirectCast(node, AwaitExpressionSyntax).AwaitKeyword.Span

                Case SyntaxKind.AnonymousObjectCreationExpression
                    Dim newWith = DirectCast(node, AnonymousObjectCreationExpressionSyntax)
                    Return TextSpan.FromBounds(newWith.NewKeyword.Span.Start,
                                               newWith.Initializer.WithKeyword.Span.End)

                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(node, LambdaExpressionSyntax).SubOrFunctionHeader.Span

                Case SyntaxKind.QueryExpression
                    Return TryGetDiagnosticSpanImpl(DirectCast(node, QueryExpressionSyntax).Clauses.First(), editKind)

                Case SyntaxKind.WhereClause
                    Return DirectCast(node, WhereClauseSyntax).WhereKeyword.Span

                Case SyntaxKind.SelectClause
                    Return DirectCast(node, SelectClauseSyntax).SelectKeyword.Span

                Case SyntaxKind.FromClause
                    Return DirectCast(node, FromClauseSyntax).FromKeyword.Span

                Case SyntaxKind.AggregateClause
                    Return DirectCast(node, AggregateClauseSyntax).AggregateKeyword.Span

                Case SyntaxKind.LetClause
                    Return DirectCast(node, LetClauseSyntax).LetKeyword.Span

                Case SyntaxKind.SimpleJoinClause
                    Return DirectCast(node, SimpleJoinClauseSyntax).JoinKeyword.Span

                Case SyntaxKind.GroupJoinClause
                    Dim groupJoin = DirectCast(node, GroupJoinClauseSyntax)
                    Return TextSpan.FromBounds(groupJoin.GroupKeyword.SpanStart, groupJoin.JoinKeyword.Span.End)

                Case SyntaxKind.GroupByClause
                    Return DirectCast(node, GroupByClauseSyntax).GroupKeyword.Span

                Case SyntaxKind.FunctionAggregation
                    Return node.Span

                Case SyntaxKind.CollectionRangeVariable,
                     SyntaxKind.ExpressionRangeVariable
                    Return TryGetDiagnosticSpanImpl(node.Parent, editKind)

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Dim partition = DirectCast(node, PartitionWhileClauseSyntax)
                    Return TextSpan.FromBounds(partition.SkipOrTakeKeyword.SpanStart, partition.WhileKeyword.Span.End)

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return node.Span

                Case SyntaxKind.JoinCondition
                    Return DirectCast(node, JoinConditionSyntax).EqualsKeyword.Span

                Case Else
                    Return node.Span
            End Select
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(ifKeyword As SyntaxToken, condition As SyntaxNode, thenKeywordOpt As SyntaxToken) As TextSpan
            Return TextSpan.FromBounds(ifKeyword.Span.Start,
                                       If(thenKeywordOpt.RawKind <> 0, thenKeywordOpt.Span.End, condition.Span.End))
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(node As NamespaceStatementSyntax) As TextSpan
            Return TextSpan.FromBounds(node.NamespaceKeyword.SpanStart, node.Name.Span.End)
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(node As TypeStatementSyntax) As TextSpan
            Return GetDiagnosticSpan(node.Modifiers,
                                     node.DeclarationKeyword,
                                     If(node.TypeParameterList, CType(node.Identifier, SyntaxNodeOrToken)))
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(modifiers As SyntaxTokenList, start As SyntaxNodeOrToken, endNode As SyntaxNodeOrToken) As TextSpan
            Return TextSpan.FromBounds(If(modifiers.Count <> 0, modifiers.First.SpanStart, start.SpanStart),
                                       endNode.Span.End)
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(header As MethodBaseSyntax) As TextSpan
            Dim startToken As SyntaxToken
            Dim endToken As SyntaxToken

            If header.Modifiers.Count > 0 Then
                startToken = header.Modifiers.First
            Else
                Select Case header.Kind
                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        startToken = DirectCast(header, DelegateStatementSyntax).DelegateKeyword

                    Case SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        startToken = DirectCast(header, DeclareStatementSyntax).DeclareKeyword

                    Case Else
                        startToken = header.DeclarationKeyword
                End Select
            End If

            If header.ParameterList IsNot Nothing Then
                endToken = header.ParameterList.CloseParenToken
            Else
                Select Case header.Kind
                    Case SyntaxKind.SubStatement,
                         SyntaxKind.FunctionStatement
                        endToken = DirectCast(header, MethodStatementSyntax).Identifier

                    Case SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement
                        endToken = DirectCast(header, DeclareStatementSyntax).LibraryName.Token

                    Case SyntaxKind.OperatorStatement
                        endToken = DirectCast(header, OperatorStatementSyntax).OperatorToken

                    Case SyntaxKind.SubNewStatement
                        endToken = DirectCast(header, SubNewStatementSyntax).NewKeyword

                    Case SyntaxKind.PropertyStatement
                        endToken = DirectCast(header, PropertyStatementSyntax).Identifier

                    Case SyntaxKind.EventStatement
                        endToken = DirectCast(header, EventStatementSyntax).Identifier

                    Case SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        endToken = DirectCast(header, DelegateStatementSyntax).Identifier

                    Case SyntaxKind.SetAccessorStatement,
                         SyntaxKind.GetAccessorStatement,
                         SyntaxKind.AddHandlerAccessorStatement,
                         SyntaxKind.RemoveHandlerAccessorStatement,
                         SyntaxKind.RaiseEventAccessorStatement
                        endToken = header.DeclarationKeyword

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(header.Kind)
                End Select
            End If

            Return TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End)
        End Function

        Private Overloads Shared Function GetDiagnosticSpan(lambda As LambdaHeaderSyntax) As TextSpan
            Dim startToken = If(lambda.Modifiers.Count <> 0, lambda.Modifiers.First, lambda.DeclarationKeyword)
            Dim endToken As SyntaxToken

            If lambda.ParameterList IsNot Nothing Then
                endToken = lambda.ParameterList.CloseParenToken
            Else
                endToken = lambda.DeclarationKeyword
            End If

            Return TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End)
        End Function

        Friend Overrides Function GetLambdaParameterDiagnosticSpan(lambda As SyntaxNode, ordinal As Integer) As TextSpan
            Select Case lambda.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(lambda, LambdaExpressionSyntax).SubOrFunctionHeader.ParameterList.Parameters(ordinal).Identifier.Span

                Case Else
                    Return lambda.Span
            End Select
        End Function

        Friend Overrides Function GetDisplayName(symbol As INamedTypeSymbol) As String
            Select Case symbol.TypeKind
                Case TypeKind.Structure
                    Return VBFeaturesResources.structure_
                Case TypeKind.Module
                    Return VBFeaturesResources.module_
                Case Else
                    Return MyBase.GetDisplayName(symbol)
            End Select
        End Function

        Friend Overrides Function GetDisplayName(symbol As IMethodSymbol) As String
            Select Case symbol.MethodKind
                Case MethodKind.StaticConstructor
                    Return VBFeaturesResources.Shared_constructor
                Case MethodKind.LambdaMethod
                    Return VBFeaturesResources.Lambda
                Case Else
                    Return MyBase.GetDisplayName(symbol)
            End Select
        End Function

        Friend Overrides Function GetDisplayName(symbol As IPropertySymbol) As String
            If symbol.IsWithEvents Then
                Return VBFeaturesResources.WithEvents_field
            End If

            Return MyBase.GetDisplayName(symbol)
        End Function

        Protected Overrides Function TryGetDisplayName(node As SyntaxNode, editKind As EditKind) As String
            Return TryGetDisplayNameImpl(node, editKind)
        End Function

        Protected Overloads Shared Function GetDisplayName(node As SyntaxNode, editKind As EditKind) As String
            Dim result = TryGetDisplayNameImpl(node, editKind)

            If result Is Nothing Then
                Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End If

            Return result
        End Function

        Protected Overrides Function GetBodyDisplayName(node As SyntaxNode, Optional editKind As EditKind = EditKind.Update) As String
            Return GetDisplayName(node, editKind)
        End Function

        Private Shared Function TryGetDisplayNameImpl(node As SyntaxNode, editKind As EditKind) As String
            Select Case node.Kind
                ' top-level

                Case SyntaxKind.OptionStatement
                    Return VBFeaturesResources.option_

                Case SyntaxKind.ImportsStatement
                    Return VBFeaturesResources.import

                Case SyntaxKind.NamespaceBlock,
                     SyntaxKind.NamespaceStatement
                    Return FeaturesResources.namespace_

                Case SyntaxKind.ClassBlock,
                     SyntaxKind.ClassStatement
                    Return FeaturesResources.class_

                Case SyntaxKind.StructureBlock,
                     SyntaxKind.StructureStatement
                    Return VBFeaturesResources.structure_

                Case SyntaxKind.InterfaceBlock,
                     SyntaxKind.InterfaceStatement
                    Return FeaturesResources.interface_

                Case SyntaxKind.ModuleBlock,
                     SyntaxKind.ModuleStatement
                    Return VBFeaturesResources.module_

                Case SyntaxKind.EnumBlock,
                     SyntaxKind.EnumStatement
                    Return FeaturesResources.enum_

                Case SyntaxKind.DelegateSubStatement,
                     SyntaxKind.DelegateFunctionStatement
                    Return FeaturesResources.delegate_

                Case SyntaxKind.FieldDeclaration
                    Dim declaration = DirectCast(node, FieldDeclarationSyntax)
                    Return If(declaration.Modifiers.Any(SyntaxKind.WithEventsKeyword), VBFeaturesResources.WithEvents_field,
                           If(declaration.Modifiers.Any(SyntaxKind.ConstKeyword), FeaturesResources.const_field, FeaturesResources.field))

                Case SyntaxKind.VariableDeclarator,
                     SyntaxKind.ModifiedIdentifier
                    Return TryGetDisplayNameImpl(node.Parent, editKind)

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DeclareFunctionStatement
                    Return FeaturesResources.method

                Case SyntaxKind.OperatorBlock,
                     SyntaxKind.OperatorStatement
                    Return FeaturesResources.operator_

                Case SyntaxKind.ConstructorBlock
                    Return If(CType(node, ConstructorBlockSyntax).SubNewStatement.Modifiers.Any(SyntaxKind.SharedKeyword), VBFeaturesResources.Shared_constructor, FeaturesResources.constructor)

                Case SyntaxKind.SubNewStatement
                    Return If(CType(node, SubNewStatementSyntax).Modifiers.Any(SyntaxKind.SharedKeyword), VBFeaturesResources.Shared_constructor, FeaturesResources.constructor)

                Case SyntaxKind.PropertyBlock

                    Return FeaturesResources.property_

                Case SyntaxKind.PropertyStatement
                    Return If(node.IsParentKind(SyntaxKind.PropertyBlock),
                        FeaturesResources.property_,
                        FeaturesResources.auto_property)

                Case SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement
                    Return FeaturesResources.event_

                Case SyntaxKind.EnumMemberDeclaration
                    Return FeaturesResources.enum_value

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement
                    Return FeaturesResources.property_accessor

                Case SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return FeaturesResources.event_accessor

                Case SyntaxKind.TypeParameterSingleConstraintClause,
                     SyntaxKind.TypeParameterMultipleConstraintClause,
                     SyntaxKind.ClassConstraint,
                     SyntaxKind.StructureConstraint,
                     SyntaxKind.NewConstraint,
                     SyntaxKind.TypeConstraint
                    Return FeaturesResources.type_constraint

                Case SyntaxKind.SimpleAsClause
                    Return VBFeaturesResources.as_clause

                Case SyntaxKind.TypeParameterList
                    Return VBFeaturesResources.type_parameters

                Case SyntaxKind.TypeParameter
                    Return FeaturesResources.type_parameter

                Case SyntaxKind.ParameterList
                    Return VBFeaturesResources.parameters

                Case SyntaxKind.Parameter
                    Return FeaturesResources.parameter

                Case SyntaxKind.AttributeList,
                     SyntaxKind.AttributesStatement
                    Return VBFeaturesResources.attributes

                Case SyntaxKind.Attribute
                    Return FeaturesResources.attribute

                ' statement-level

                Case SyntaxKind.TryBlock
                    Return VBFeaturesResources.Try_block

                Case SyntaxKind.CatchBlock
                    Return VBFeaturesResources.Catch_clause

                Case SyntaxKind.FinallyBlock
                    Return VBFeaturesResources.Finally_clause

                Case SyntaxKind.UsingBlock
                    Return If(editKind = EditKind.Update, VBFeaturesResources.Using_statement, VBFeaturesResources.Using_block)

                Case SyntaxKind.WithBlock
                    Return If(editKind = EditKind.Update, VBFeaturesResources.With_statement, VBFeaturesResources.With_block)

                Case SyntaxKind.SyncLockBlock
                    Return If(editKind = EditKind.Update, VBFeaturesResources.SyncLock_statement, VBFeaturesResources.SyncLock_block)

                Case SyntaxKind.ForEachBlock
                    Return If(editKind = EditKind.Update, VBFeaturesResources.For_Each_statement, VBFeaturesResources.For_Each_block)

                Case SyntaxKind.OnErrorGoToMinusOneStatement,
                     SyntaxKind.OnErrorGoToZeroStatement,
                     SyntaxKind.OnErrorResumeNextStatement,
                     SyntaxKind.OnErrorGoToLabelStatement
                    Return VBFeaturesResources.On_Error_statement

                Case SyntaxKind.ResumeStatement,
                     SyntaxKind.ResumeNextStatement,
                     SyntaxKind.ResumeLabelStatement
                    Return VBFeaturesResources.Resume_statement

                Case SyntaxKind.YieldStatement
                    Return VBFeaturesResources.Yield_statement

                Case SyntaxKind.AwaitExpression
                    Return VBFeaturesResources.Await_expression

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return VBFeaturesResources.Lambda

                Case SyntaxKind.WhereClause
                    Return VBFeaturesResources.Where_clause

                Case SyntaxKind.SelectClause
                    Return VBFeaturesResources.Select_clause

                Case SyntaxKind.FromClause
                    Return VBFeaturesResources.From_clause

                Case SyntaxKind.AggregateClause
                    Return VBFeaturesResources.Aggregate_clause

                Case SyntaxKind.LetClause
                    Return VBFeaturesResources.Let_clause

                Case SyntaxKind.SimpleJoinClause
                    Return VBFeaturesResources.Join_clause

                Case SyntaxKind.GroupJoinClause
                    Return VBFeaturesResources.Group_Join_clause

                Case SyntaxKind.GroupByClause
                    Return VBFeaturesResources.Group_By_clause

                Case SyntaxKind.FunctionAggregation
                    Return VBFeaturesResources.Function_aggregation

                Case SyntaxKind.CollectionRangeVariable,
                     SyntaxKind.ExpressionRangeVariable
                    Return TryGetDisplayNameImpl(node.Parent, editKind)

                Case SyntaxKind.TakeWhileClause
                    Return VBFeaturesResources.Take_While_clause

                Case SyntaxKind.SkipWhileClause
                    Return VBFeaturesResources.Skip_While_clause

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return VBFeaturesResources.Ordering_clause

                Case SyntaxKind.JoinCondition
                    Return VBFeaturesResources.Join_condition

                Case Else
                    Return Nothing
            End Select
        End Function

#End Region

#Region "Top-level Syntactic Rude Edits"
        Private Structure EditClassifier

            Private ReadOnly _analyzer As VisualBasicEditAndContinueAnalyzer
            Private ReadOnly _diagnostics As ArrayBuilder(Of RudeEditDiagnostic)
            Private ReadOnly _match As Match(Of SyntaxNode)
            Private ReadOnly _oldNode As SyntaxNode
            Private ReadOnly _newNode As SyntaxNode
            Private ReadOnly _kind As EditKind
            Private ReadOnly _span As TextSpan?

            Public Sub New(analyzer As VisualBasicEditAndContinueAnalyzer,
                           diagnostics As ArrayBuilder(Of RudeEditDiagnostic),
                           oldNode As SyntaxNode,
                           newNode As SyntaxNode,
                           kind As EditKind,
                           Optional match As Match(Of SyntaxNode) = Nothing,
                           Optional span As TextSpan? = Nothing)

                _analyzer = analyzer
                _diagnostics = diagnostics
                _oldNode = oldNode
                _newNode = newNode
                _kind = kind
                _span = span
                _match = match
            End Sub

            Private Sub ReportError(kind As RudeEditKind)
                _diagnostics.Add(New RudeEditDiagnostic(
                    kind,
                    span:=GetSpan(),
                    node:=If(_newNode, _oldNode),
                    arguments:={GetDisplayName(If(_newNode, _oldNode), EditKind.Update)}))
            End Sub

            Private Function GetSpan() As TextSpan
                If _span.HasValue Then
                    Return _span.Value
                End If

                If _newNode Is Nothing Then
                    Return _analyzer.GetDeletedNodeDiagnosticSpan(_match.Matches, _oldNode)
                Else
                    Return GetDiagnosticSpan(_newNode, _kind)
                End If
            End Function

            Public Sub ClassifyEdit()
                Select Case _kind
                    Case EditKind.Delete
                        ClassifyDelete(_oldNode)
                        Return

                    Case EditKind.Update
                        ClassifyUpdate(_newNode)
                        Return

                    Case EditKind.Move
                        ClassifyMove(_newNode)
                        Return

                    Case EditKind.Insert
                        ClassifyInsert(_newNode)
                        Return

                    Case EditKind.Reorder
                        ClassifyReorder(_oldNode, _newNode)
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(_kind)
                End Select
            End Sub

#Region "Move and Reorder"
            Private Sub ClassifyMove(newNode As SyntaxNode)
                Select Case newNode.Kind
                    Case SyntaxKind.ModifiedIdentifier,
                         SyntaxKind.VariableDeclarator
                        ' Identifier can be moved within the same type declaration.
                        ' Determine validity of such change in semantic analysis.
                        Return

                    Case SyntaxKind.NamespaceBlock,
                         SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.EnumBlock,
                         SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement
                        Return

                    Case Else
                        ReportError(RudeEditKind.Move)
                End Select
            End Sub

            Private Sub ClassifyReorder(oldNode As SyntaxNode, newNode As SyntaxNode)
                Select Case newNode.Kind
                    Case SyntaxKind.OptionStatement,
                         SyntaxKind.ImportsStatement,
                         SyntaxKind.AttributesStatement,
                         SyntaxKind.NamespaceBlock,
                         SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.EnumBlock,
                         SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement,
                         SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.DeclareSubStatement,
                         SyntaxKind.DeclareFunctionStatement,
                         SyntaxKind.ConstructorBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.PropertyBlock,
                         SyntaxKind.EventBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock,
                         SyntaxKind.ClassConstraint,
                         SyntaxKind.StructureConstraint,
                         SyntaxKind.NewConstraint,
                         SyntaxKind.TypeConstraint,
                         SyntaxKind.AttributeList,
                         SyntaxKind.Attribute,
                         SyntaxKind.Parameter
                        ' We'll ignore these edits. A general policy is to ignore edits that are only discoverable via reflection.
                        Return

                    Case SyntaxKind.SubStatement,
                         SyntaxKind.FunctionStatement
                        ' Interface methods. We could allow reordering of non-COM interface methods.
                        Debug.Assert(oldNode.Parent.IsKind(SyntaxKind.InterfaceBlock) AndAlso newNode.Parent.IsKind(SyntaxKind.InterfaceBlock))
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.PropertyStatement,
                         SyntaxKind.FieldDeclaration,
                         SyntaxKind.EventStatement
                        ' Maybe we could allow changing order of field declarations unless the containing type layout is sequential,
                        ' and it's not a COM interface.
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.EnumMemberDeclaration
                        ' To allow this change we would need to check that values of all fields of the enum 
                        ' are preserved, or make sure we can update all method bodies that accessed those that changed.
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.TypeParameter
                        ReportError(RudeEditKind.Move)
                        Return

                    Case SyntaxKind.ModifiedIdentifier,
                         SyntaxKind.VariableDeclarator
                        ' Identifier can be moved within the same type declaration.
                        ' Determine validity of such change in semantic analysis.
                        Return

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(newNode.Kind)
                End Select
            End Sub

#End Region

#Region "Insert"
            Private Sub ClassifyInsert(node As SyntaxNode)
                Select Case node.Kind
                    Case SyntaxKind.OptionStatement
                        ReportError(RudeEditKind.Insert)
                        Return

                    Case SyntaxKind.AttributesStatement
                        ' Module/assembly attribute
                        ReportError(RudeEditKind.Insert)
                        Return

                    Case SyntaxKind.Attribute
                        ' Only module/assembly attributes are rude
                        If node.Parent.IsParentKind(SyntaxKind.AttributesStatement) Then
                            ReportError(RudeEditKind.Insert)
                        End If

                        Return

                    Case SyntaxKind.AttributeList
                        ' Only module/assembly attributes are rude
                        If node.IsParentKind(SyntaxKind.AttributesStatement) Then
                            ReportError(RudeEditKind.Insert)
                        End If

                        Return
                End Select
            End Sub

#End Region

#Region "Delete"
            Private Sub ClassifyDelete(oldNode As SyntaxNode)
                Select Case oldNode.Kind
                    Case SyntaxKind.OptionStatement,
                         SyntaxKind.AttributesStatement
                        ReportError(RudeEditKind.Delete)
                        Return

                    Case SyntaxKind.AttributeList
                        ' Only module/assembly attributes are rude edits
                        If oldNode.IsParentKind(SyntaxKind.AttributesStatement) Then
                            ReportError(RudeEditKind.Insert)
                        End If

                        Return

                    Case SyntaxKind.Attribute
                        ' Only module/assembly attributes are rude edits
                        If oldNode.Parent.IsParentKind(SyntaxKind.AttributesStatement) Then
                            ReportError(RudeEditKind.Insert)
                        End If

                        Return
                End Select
            End Sub
#End Region

#Region "Update"
            Private Sub ClassifyUpdate(newNode As SyntaxNode)
                Select Case newNode.Kind
                    Case SyntaxKind.OptionStatement
                        ReportError(RudeEditKind.Update)
                        Return

                    Case SyntaxKind.AttributesStatement
                        ReportError(RudeEditKind.Update)
                        Return

                    Case SyntaxKind.Attribute
                        ' Only module/assembly attributes are rude edits
                        If newNode.Parent.IsParentKind(SyntaxKind.AttributesStatement) Then
                            ReportError(RudeEditKind.Update)
                        End If

                        Return
                End Select
            End Sub
#End Region
        End Structure

        Friend Overrides Sub ReportTopLevelSyntacticRudeEdits(
            diagnostics As ArrayBuilder(Of RudeEditDiagnostic),
            match As Match(Of SyntaxNode),
            edit As Edit(Of SyntaxNode),
            editMap As Dictionary(Of SyntaxNode, EditKind))

            ' For most nodes we ignore Insert and Delete edits if their parent was also inserted or deleted, respectively.
            ' For ModifiedIdentifiers though we check the grandparent instead because variables can move across 
            ' VariableDeclarators. Moving a variable from a VariableDeclarator that only has a single variable results in
            ' deletion of that declarator. We don't want to report that delete. Similarly for moving to a new VariableDeclarator.

            If edit.Kind = EditKind.Delete AndAlso
               edit.OldNode.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso
               edit.OldNode.Parent.IsKind(SyntaxKind.VariableDeclarator) Then

                If HasEdit(editMap, edit.OldNode.Parent.Parent, EditKind.Delete) Then
                    Return
                End If

            ElseIf edit.Kind = EditKind.Insert AndAlso
                   edit.NewNode.IsKind(SyntaxKind.ModifiedIdentifier) AndAlso
                   edit.NewNode.Parent.IsKind(SyntaxKind.VariableDeclarator) Then

                If HasEdit(editMap, edit.NewNode.Parent.Parent, EditKind.Insert) Then
                    Return
                End If

            ElseIf HasParentEdit(editMap, edit) Then
                Return
            End If

            Dim classifier = New EditClassifier(Me, diagnostics, edit.OldNode, edit.NewNode, edit.Kind, match)
            classifier.ClassifyEdit()
        End Sub

        Friend Overrides Function HasUnsupportedOperation(nodes As IEnumerable(Of SyntaxNode), <Out> ByRef unsupportedNode As SyntaxNode, <Out> ByRef rudeEdit As RudeEditKind) As Boolean
            ' Disallow editing the body even if the change is only in trivia.
            ' The compiler might not emit equivallent IL for these constructs (e.g. different names of backing fields for static locals).

            For Each node In nodes
                Select Case node.Kind()
                    Case SyntaxKind.AggregateClause,
                         SyntaxKind.GroupByClause,
                         SyntaxKind.SimpleJoinClause,
                         SyntaxKind.GroupJoinClause
                        unsupportedNode = node
                        rudeEdit = RudeEditKind.ComplexQueryExpression
                        Return True

                    Case SyntaxKind.LocalDeclarationStatement
                        Dim declaration = DirectCast(node, LocalDeclarationStatementSyntax)
                        If declaration.Modifiers.Any(SyntaxKind.StaticKeyword) Then
                            unsupportedNode = node
                            rudeEdit = RudeEditKind.UpdateStaticLocal
                            Return True
                        End If
                End Select
            Next

            unsupportedNode = Nothing
            rudeEdit = RudeEditKind.None
            Return False
        End Function

#End Region

#Region "Semantic Rude Edits"
        Protected Overrides Function AreHandledEventsEqual(oldMethod As IMethodSymbol, newMethod As IMethodSymbol) As Boolean
            Return oldMethod.HandledEvents.SequenceEqual(
                newMethod.HandledEvents,
                Function(x, y)
                    Return x.HandlesKind = y.HandlesKind AndAlso
                           SymbolsEquivalent(x.EventContainer, y.EventContainer) AndAlso
                           SymbolsEquivalent(x.EventSymbol, y.EventSymbol) AndAlso
                           SymbolsEquivalent(x.WithEventsSourceProperty, y.WithEventsSourceProperty)
                End Function)
        End Function

        Friend Overrides Sub ReportInsertedMemberSymbolRudeEdits(diagnostics As ArrayBuilder(Of RudeEditDiagnostic), newSymbol As ISymbol, newNode As SyntaxNode, insertingIntoExistingContainingType As Boolean)
            Dim kind = GetInsertedMemberSymbolRudeEditKind(newSymbol, insertingIntoExistingContainingType)

            If kind <> RudeEditKind.None Then
                diagnostics.Add(New RudeEditDiagnostic(
                    kind,
                    GetDiagnosticSpan(newNode, EditKind.Insert),
                    newNode,
                    arguments:={GetDisplayName(newNode, EditKind.Insert)}))
            End If
        End Sub

        Private Shared Function GetInsertedMemberSymbolRudeEditKind(newSymbol As ISymbol, insertingIntoExistingContainingType As Boolean) As RudeEditKind
            Select Case newSymbol.Kind
                Case SymbolKind.Method
                    Dim method = DirectCast(newSymbol, IMethodSymbol)

                    ' Inserting P/Invoke into a new or existing type is not allowed.
                    If method.GetDllImportData() IsNot Nothing Then
                        Return RudeEditKind.InsertDllImport
                    End If

                    ' Inserting method with handles clause into a new or existing type is not allowed.
                    If Not method.HandledEvents.IsEmpty Then
                        Return RudeEditKind.InsertHandlesClause
                    End If

                Case SymbolKind.NamedType
                    Dim type = CType(newSymbol, INamedTypeSymbol)

                    ' Inserting module is not allowed.
                    If type.TypeKind = TypeKind.Module Then
                        Return RudeEditKind.Insert
                    End If

                    Return RudeEditKind.None
            End Select

            ' All rude edits below only apply when inserting into an existing type (not when the type itself is inserted):
            If Not insertingIntoExistingContainingType Then
                Return RudeEditKind.None
            End If

            ' Inserting virtual or interface member is not allowed.
            If newSymbol.IsVirtual Or newSymbol.IsOverride Or newSymbol.IsAbstract Then
                Return RudeEditKind.InsertVirtual
            End If

            Select Case newSymbol.Kind
                Case SymbolKind.Method
                    Dim method = DirectCast(newSymbol, IMethodSymbol)

                    ' Inserting operator to an existing type is not allowed.
                    If method.MethodKind = MethodKind.Conversion OrElse method.MethodKind = MethodKind.UserDefinedOperator Then
                        Return RudeEditKind.InsertOperator
                    End If

                    Return RudeEditKind.None

                Case SymbolKind.Field
                    ' Inserting a field into an enum is not allowed.
                    If newSymbol.ContainingType.TypeKind = TypeKind.Enum Then
                        Return RudeEditKind.Insert
                    End If

                    Return RudeEditKind.None
            End Select

            Return RudeEditKind.None
        End Function

#End Region

#Region "Exception Handling Rude Edits"

        Protected Overrides Function GetExceptionHandlingAncestors(node As SyntaxNode, root As SyntaxNode, isNonLeaf As Boolean) As List(Of SyntaxNode)
            Dim result = New List(Of SyntaxNode)()

            While node IsNot root
                Dim kind = node.Kind

                Select Case kind
                    Case SyntaxKind.TryBlock
                        If isNonLeaf Then
                            result.Add(node)
                        End If

                    Case SyntaxKind.CatchBlock,
                         SyntaxKind.FinallyBlock
                        result.Add(node)
                        Debug.Assert(node.Parent.Kind = SyntaxKind.TryBlock)
                        node = node.Parent

                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.StructureBlock
                        ' stop at type declaration
                        Exit While
                End Select

                ' stop at lambda
                If LambdaUtilities.IsLambda(node) Then
                    Exit While
                End If

                Debug.Assert(node.Parent IsNot Nothing)
                node = node.Parent
            End While

            Return result
        End Function

        Friend Overrides Sub ReportEnclosingExceptionHandlingRudeEdits(diagnostics As ArrayBuilder(Of RudeEditDiagnostic),
                                                                       exceptionHandlingEdits As IEnumerable(Of Edit(Of SyntaxNode)),
                                                                       oldStatement As SyntaxNode,
                                                                       newStatementSpan As TextSpan)
            For Each edit In exceptionHandlingEdits
                Debug.Assert(edit.Kind <> EditKind.Update OrElse edit.OldNode.RawKind = edit.NewNode.RawKind)

                If edit.Kind <> EditKind.Update OrElse Not AreExceptionHandlingPartsEquivalent(edit.OldNode, edit.NewNode) Then
                    AddAroundActiveStatementRudeDiagnostic(diagnostics, edit.OldNode, edit.NewNode, newStatementSpan)
                End If
            Next
        End Sub

        Private Shared Function AreExceptionHandlingPartsEquivalent(oldNode As SyntaxNode, newNode As SyntaxNode) As Boolean
            Select Case oldNode.Kind
                Case SyntaxKind.TryBlock
                    Dim oldTryBlock = DirectCast(oldNode, TryBlockSyntax)
                    Dim newTryBlock = DirectCast(newNode, TryBlockSyntax)
                    Return SyntaxFactory.AreEquivalent(oldTryBlock.FinallyBlock, newTryBlock.FinallyBlock) AndAlso
                           SyntaxFactory.AreEquivalent(oldTryBlock.CatchBlocks, newTryBlock.CatchBlocks)

                Case SyntaxKind.CatchBlock,
                     SyntaxKind.FinallyBlock
                    Return SyntaxFactory.AreEquivalent(oldNode, newNode)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(oldNode.Kind)
            End Select
        End Function

        ''' <summary>
        ''' An active statement (leaf or not) inside a "Catch" makes the Catch part readonly.
        ''' An active statement (leaf or not) inside a "Finally" makes the whole Try/Catch/Finally part read-only.
        ''' An active statement (non leaf)    inside a "Try" makes the Catch/Finally part read-only.
        ''' </summary>
        Protected Overrides Function GetExceptionHandlingRegion(node As SyntaxNode, <Out> ByRef coversAllChildren As Boolean) As TextSpan
            Select Case node.Kind
                Case SyntaxKind.TryBlock
                    Dim tryBlock = DirectCast(node, TryBlockSyntax)
                    coversAllChildren = False

                    If tryBlock.CatchBlocks.Count = 0 Then
                        Debug.Assert(tryBlock.FinallyBlock IsNot Nothing)
                        Return TextSpan.FromBounds(tryBlock.FinallyBlock.SpanStart, tryBlock.EndTryStatement.Span.End)
                    End If

                    Return TextSpan.FromBounds(tryBlock.CatchBlocks.First().SpanStart, tryBlock.EndTryStatement.Span.End)

                Case SyntaxKind.CatchBlock
                    coversAllChildren = True
                    Return node.Span

                Case SyntaxKind.FinallyBlock
                    coversAllChildren = True
                    Return DirectCast(node.Parent, TryBlockSyntax).Span

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

#End Region

#Region "State Machines"

        Friend Overrides Function IsStateMachineMethod(declaration As SyntaxNode) As Boolean
            Return SyntaxUtilities.IsAsyncMethodOrLambda(declaration) OrElse
                   SyntaxUtilities.IsIteratorMethodOrLambda(declaration)
        End Function

        Friend Shared Function GetStateMachineInfo(body As SyntaxNode) As StateMachineInfo
            ' In VB declaration and body are represented by the same node for both lambdas and methods (unlike C#)
            If SyntaxUtilities.IsAsyncMethodOrLambda(body) Then
                Return New StateMachineInfo(IsAsync:=True, IsIterator:=False, HasSuspensionPoints:=SyntaxUtilities.GetAwaitExpressions(body).Any())
            ElseIf SyntaxUtilities.IsIteratorMethodOrLambda(body) Then
                Return New StateMachineInfo(IsAsync:=False, IsIterator:=True, HasSuspensionPoints:=SyntaxUtilities.GetYieldStatements(body).Any())
            Else
                Return StateMachineInfo.None
            End If
        End Function

        Friend Overrides Sub ReportStateMachineSuspensionPointRudeEdits(diagnosticContext As DiagnosticContext, oldNode As SyntaxNode, newNode As SyntaxNode)
            ' TODO: changes around suspension points (foreach, lock, using, etc.)

            If newNode.IsKind(SyntaxKind.AwaitExpression) AndAlso oldNode.IsKind(SyntaxKind.AwaitExpression) Then
                Dim oldContainingStatementPart = FindContainingStatementPart(oldNode)
                Dim newContainingStatementPart = FindContainingStatementPart(newNode)

                ' If the old statement has spilled state and the new doesn't, the edit is ok. We'll just not use the spilled state.
                If Not SyntaxFactory.AreEquivalent(oldContainingStatementPart, newContainingStatementPart) AndAlso
                   Not HasNoSpilledState(newNode, newContainingStatementPart) Then
                    diagnosticContext.Report(RudeEditKind.AwaitStatementUpdate, newContainingStatementPart.Span)
                End If
            End If
        End Sub

        Private Shared Function FindContainingStatementPart(node As SyntaxNode) As SyntaxNode
            Dim statement = TryCast(node, StatementSyntax)

            While statement Is Nothing
                Select Case node.Parent.Kind()
                    Case SyntaxKind.ForStatement,
                         SyntaxKind.ForEachStatement,
                         SyntaxKind.IfStatement,
                         SyntaxKind.WhileStatement,
                         SyntaxKind.SimpleDoStatement,
                         SyntaxKind.SelectStatement,
                         SyntaxKind.UsingStatement
                        Return node
                End Select

                If LambdaUtilities.IsLambdaBodyStatementOrExpression(node) Then
                    Return node
                End If

                node = node.Parent
                statement = TryCast(node, StatementSyntax)
            End While

            Return statement
        End Function

        Private Shared Function HasNoSpilledState(awaitExpression As SyntaxNode, containingStatementPart As SyntaxNode) As Boolean
            Debug.Assert(awaitExpression.IsKind(SyntaxKind.AwaitExpression))

            ' There is nothing within the statement part surrounding the await expression.
            If containingStatementPart Is awaitExpression Then
                Return True
            End If

            Select Case containingStatementPart.Kind()
                Case SyntaxKind.ExpressionStatement,
                     SyntaxKind.ReturnStatement
                    Dim expression = GetExpressionFromStatementPart(containingStatementPart)

                    ' Await <expr>
                    ' Return Await <expr>
                    If expression Is awaitExpression Then
                        Return True
                    End If

                    ' <ident> = Await <expr>
                    ' Return <ident> = Await <expr>
                    Return IsSimpleAwaitAssignment(expression, awaitExpression)

                Case SyntaxKind.VariableDeclarator
                    ' <ident> = Await <expr> in using, for, etc
                    ' EqualsValue -> VariableDeclarator
                    Return awaitExpression.Parent.Parent Is containingStatementPart

                Case SyntaxKind.LoopUntilStatement,
                     SyntaxKind.LoopWhileStatement,
                     SyntaxKind.DoUntilStatement,
                     SyntaxKind.DoWhileStatement
                    ' Until Await <expr>
                    ' UntilClause -> LoopUntilStatement
                    Return awaitExpression.Parent.Parent Is containingStatementPart

                Case SyntaxKind.LocalDeclarationStatement
                    ' Dim <ident> = Await <expr>
                    ' EqualsValue -> VariableDeclarator -> LocalDeclarationStatement
                    Return awaitExpression.Parent.Parent.Parent Is containingStatementPart
            End Select

            Return IsSimpleAwaitAssignment(containingStatementPart, awaitExpression)
        End Function

        Private Shared Function GetExpressionFromStatementPart(statement As SyntaxNode) As ExpressionSyntax
            Select Case statement.Kind()
                Case SyntaxKind.ExpressionStatement
                    Return DirectCast(statement, ExpressionStatementSyntax).Expression

                Case SyntaxKind.ReturnStatement
                    Return DirectCast(statement, ReturnStatementSyntax).Expression

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(statement.Kind())
            End Select
        End Function

        Private Shared Function IsSimpleAwaitAssignment(node As SyntaxNode, awaitExpression As SyntaxNode) As Boolean
            If node.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                Dim assignment = DirectCast(node, AssignmentStatementSyntax)
                Return assignment.Left.IsKind(SyntaxKind.IdentifierName) AndAlso assignment.Right Is awaitExpression
            End If

            Return False
        End Function
#End Region

#Region "Rude Edits around Active Statement"

        Friend Overrides Sub ReportOtherRudeEditsAroundActiveStatement(diagnostics As ArrayBuilder(Of RudeEditDiagnostic),
                                                                       forwardMap As IReadOnlyDictionary(Of SyntaxNode, SyntaxNode),
                                                                       oldActiveStatement As SyntaxNode,
                                                                       oldBody As DeclarationBody,
                                                                       oldModel As SemanticModel,
                                                                       newActiveStatement As SyntaxNode,
                                                                       newBody As DeclarationBody,
                                                                       newModel As SemanticModel,
                                                                       isNonLeaf As Boolean,
                                                                       cancellationToken As CancellationToken)

            Dim onErrorOrResumeStatement = FindOnErrorOrResumeStatement(newBody)
            If onErrorOrResumeStatement IsNot Nothing Then
                AddAroundActiveStatementRudeDiagnostic(diagnostics, oldActiveStatement, onErrorOrResumeStatement, newActiveStatement.Span)
            End If

            ReportRudeEditsForAncestorsDeclaringInterStatementTemps(diagnostics, forwardMap, oldActiveStatement, oldBody.EncompassingAncestor, oldModel, newActiveStatement, newBody.EncompassingAncestor, newModel, cancellationToken)
        End Sub

        Private Shared Function FindOnErrorOrResumeStatement(newBody As DeclarationBody) As SyntaxNode
            For Each newRoot In newBody.RootNodes
                For Each node In newRoot.DescendantNodes(AddressOf ChildrenCompiledInBody)
                    Select Case node.Kind
                        Case SyntaxKind.OnErrorGoToLabelStatement,
                         SyntaxKind.OnErrorGoToMinusOneStatement,
                         SyntaxKind.OnErrorGoToZeroStatement,
                         SyntaxKind.OnErrorResumeNextStatement,
                         SyntaxKind.ResumeStatement,
                         SyntaxKind.ResumeNextStatement,
                         SyntaxKind.ResumeLabelStatement
                            Return node
                    End Select
                Next
            Next

            Return Nothing
        End Function

        Private Sub ReportRudeEditsForAncestorsDeclaringInterStatementTemps(diagnostics As ArrayBuilder(Of RudeEditDiagnostic),
                                                                            forwardMap As IReadOnlyDictionary(Of SyntaxNode, SyntaxNode),
                                                                            oldActiveStatement As SyntaxNode,
                                                                            oldEncompassingAncestor As SyntaxNode,
                                                                            oldModel As SemanticModel,
                                                                            newActiveStatement As SyntaxNode,
                                                                            newEncompassingAncestor As SyntaxNode,
                                                                            newModel As SemanticModel,
                                                                            cancellationToken As CancellationToken)

            ' Rude Edits for Using/SyncLock/With/ForEach statements that are added/updated around an active statement.
            ' Although such changes are technically possible, they might lead to confusion since 
            ' the temporary variables these statements generate won't be properly initialized.
            '
            ' We use a simple algorithm to match each New node with its old counterpart.
            ' If all nodes match this algorithm Is linear, otherwise it's quadratic.
            ' 
            ' Unlike exception regions matching where we use LCS, we allow reordering of the statements.

            ReportUnmatchedStatements(Of SyncLockBlockSyntax)(diagnostics, forwardMap, oldActiveStatement, oldEncompassingAncestor, oldModel, newActiveStatement, newEncompassingAncestor, newModel,
                nodeSelector:=Function(node) node.IsKind(SyntaxKind.SyncLockBlock),
                getTypedNodes:=Function(n) OneOrMany.Create(Of SyntaxNode)(n.SyncLockStatement.Expression),
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.SyncLockStatement.Expression, n2.SyncLockStatement.Expression),
                areSimilar:=Nothing,
                cancellationToken)

            ReportUnmatchedStatements(Of WithBlockSyntax)(diagnostics, forwardMap, oldActiveStatement, oldEncompassingAncestor, oldModel, newActiveStatement, newEncompassingAncestor, newModel,
                nodeSelector:=Function(node) node.IsKind(SyntaxKind.WithBlock),
                getTypedNodes:=Function(n) OneOrMany.Create(Of SyntaxNode)(n.WithStatement.Expression),
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.WithStatement.Expression, n2.WithStatement.Expression),
                areSimilar:=Nothing,
                cancellationToken)

            ReportUnmatchedStatements(Of UsingBlockSyntax)(diagnostics, forwardMap, oldActiveStatement, oldEncompassingAncestor, oldModel, newActiveStatement, newEncompassingAncestor, newModel,
                nodeSelector:=Function(node) node.IsKind(SyntaxKind.UsingBlock),
                getTypedNodes:=Function(n) OneOrMany.Create(Of SyntaxNode)(n.UsingStatement.Expression),
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.UsingStatement.Expression, n2.UsingStatement.Expression),
                areSimilar:=Nothing,
                cancellationToken)

            ReportUnmatchedStatements(Of ForEachBlockSyntax)(diagnostics, forwardMap, oldActiveStatement, oldEncompassingAncestor, oldModel, newActiveStatement, newEncompassingAncestor, newModel,
                nodeSelector:=Function(node) node.IsKind(SyntaxKind.ForEachBlock),
                getTypedNodes:=Function(n) OneOrMany.Create(Of SyntaxNode)(n.ForEachStatement.Expression),
                areEquivalent:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(n1.ForOrForEachStatement, n2.ForOrForEachStatement),
                areSimilar:=Function(n1, n2) AreEquivalentIgnoringLambdaBodies(DirectCast(n1.ForOrForEachStatement, ForEachStatementSyntax).ControlVariable,
                                                                               DirectCast(n2.ForOrForEachStatement, ForEachStatementSyntax).ControlVariable),
                cancellationToken)
        End Sub

#End Region
    End Class
End Namespace
