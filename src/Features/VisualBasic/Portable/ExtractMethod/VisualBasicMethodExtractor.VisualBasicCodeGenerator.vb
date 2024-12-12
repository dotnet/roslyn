' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Partial Private MustInherit Class VisualBasicCodeGenerator
            Inherits CodeGenerator(Of StatementSyntax, StatementSyntax, VisualBasicCodeGenerationOptions)

            Private ReadOnly _methodName As SyntaxToken

            Public Shared Async Function GenerateResultAsync(
                    insertionPoint As InsertionPoint,
                    selectionResult As VisualBasicSelectionResult,
                    analyzerResult As AnalyzerResult,
                    options As ExtractMethodGenerationOptions,
                    cancellationToken As CancellationToken) As Task(Of GeneratedCode)
                Dim generator = Create(selectionResult, analyzerResult, options)
                Return Await generator.GenerateAsync(insertionPoint, cancellationToken).ConfigureAwait(False)
            End Function

            Public Shared Function Create(
                    selectionResult As VisualBasicSelectionResult,
                    analyzerResult As AnalyzerResult,
                    options As ExtractMethodGenerationOptions) As VisualBasicCodeGenerator
                If selectionResult.SelectionInExpression Then
                    Return New ExpressionCodeGenerator(selectionResult, analyzerResult, options)
                End If

                If selectionResult.IsExtractMethodOnSingleStatement() Then
                    Return New SingleStatementCodeGenerator(selectionResult, analyzerResult, options)
                End If

                If selectionResult.IsExtractMethodOnMultipleStatements() Then
                    Return New MultipleStatementsCodeGenerator(selectionResult, analyzerResult, options)
                End If

                Throw ExceptionUtilities.UnexpectedValue(selectionResult)
            End Function

            Protected Sub New(
                    selectionResult As VisualBasicSelectionResult,
                    analyzerResult As AnalyzerResult,
                    options As ExtractMethodGenerationOptions)
                MyBase.New(selectionResult, analyzerResult, options, localFunction:=False)
                Contract.ThrowIfFalse(Me.SemanticDocument Is selectionResult.SemanticDocument)

                Me._methodName = CreateMethodName().WithAdditionalAnnotations(MethodNameAnnotation)
            End Sub

            Protected Overrides Function UpdateMethodAfterGenerationAsync(originalDocument As SemanticDocument, methodSymbol As IMethodSymbol, cancellationToken As CancellationToken) As Task(Of SemanticDocument)
                Return Task.FromResult(originalDocument)
            End Function

            Protected Overrides Function ShouldLocalFunctionCaptureParameter(node As SyntaxNode) As Boolean
                Return False
            End Function

            Protected Overrides Function GenerateMethodDefinition(insertionPointNode As SyntaxNode, cancellationToken As CancellationToken) As IMethodSymbol
                Dim statements = CreateMethodBody(insertionPointNode, cancellationToken)

                Dim methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes:=ImmutableArray(Of AttributeData).Empty,
                    accessibility:=Accessibility.Private,
                    modifiers:=CreateMethodModifiers(),
                    returnType:=Me.AnalyzerResult.ReturnType,
                    refKind:=RefKind.None,
                    explicitInterfaceImplementations:=Nothing,
                    name:=_methodName.ToString(),
                    typeParameters:=CreateMethodTypeParameters(),
                    parameters:=CreateMethodParameters(),
                    statements:=statements.CastArray(Of SyntaxNode))

                Return Me.MethodDefinitionAnnotation.AddAnnotationToSymbol(
                    Formatter.Annotation.AddAnnotationToSymbol(methodSymbol))
            End Function

            Protected Overrides Async Function GenerateBodyForCallSiteContainerAsync(
                    insertionPointNode As SyntaxNode,
                    container As SyntaxNode,
                    cancellationToken As CancellationToken) As Task(Of SyntaxNode)
                Dim variableMapToRemove = CreateVariableDeclarationToRemoveMap(AnalyzerResult.GetVariablesToMoveIntoMethodDefinition(cancellationToken), cancellationToken)
                Dim firstStatementToRemove = GetFirstStatementOrInitializerSelectedAtCallSite()
                Dim lastStatementToRemove = GetLastStatementOrInitializerSelectedAtCallSite()

                Contract.ThrowIfFalse(firstStatementToRemove.Parent Is lastStatementToRemove.Parent)

                Dim statementsToInsert = Await CreateStatementsToInsertAtCallSiteAsync(
                    insertionPointNode, cancellationToken).ConfigureAwait(False)

                Dim callSiteGenerator = New CallSiteContainerRewriter(
                    container,
                    variableMapToRemove,
                    firstStatementToRemove,
                    lastStatementToRemove,
                    statementsToInsert)

                Return container.CopyAnnotationsTo(callSiteGenerator.Generate()).WithAdditionalAnnotations(Formatter.Annotation)
            End Function

            Private Async Function CreateStatementsToInsertAtCallSiteAsync(
                    insertionPointNode As SyntaxNode, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of StatementSyntax))
                Dim semanticModel = SemanticDocument.SemanticModel
                Dim postProcessor = New PostProcessor(semanticModel, insertionPointNode.SpanStart)

                Dim statements = AddSplitOrMoveDeclarationOutStatementsToCallSite(cancellationToken)
                statements = postProcessor.MergeDeclarationStatements(statements)
                statements = AddAssignmentStatementToCallSite(statements, cancellationToken)
                statements = Await AddInvocationAtCallSiteAsync(statements, cancellationToken).ConfigureAwait(False)
                statements = AddReturnIfUnreachable(statements)

                Return statements
            End Function

            Private Function CreateMethodNameForInvocation() As SimpleNameSyntax
                If AnalyzerResult.MethodTypeParametersInDeclaration.Count = 0 Then
                    Return SyntaxFactory.IdentifierName(_methodName)
                End If

                Return SyntaxFactory.GenericName(_methodName, SyntaxFactory.TypeArgumentList(arguments:=CreateMethodCallTypeVariables()))
            End Function

            Private Function CreateMethodCallTypeVariables() As SeparatedSyntaxList(Of TypeSyntax)
                Contract.ThrowIfTrue(AnalyzerResult.MethodTypeParametersInDeclaration.Count = 0)

                ' propagate any type variable used in extracted code
                Dim typeVariables = (From methodTypeParameter In AnalyzerResult.MethodTypeParametersInDeclaration
                                     Select SyntaxFactory.ParseTypeName(methodTypeParameter.Name)).ToList()

                Return SyntaxFactory.SeparatedList(typeVariables)
            End Function

            Protected Overrides Function GetCallSiteContainerFromOutermostMoveInVariable(cancellationToken As CancellationToken) As SyntaxNode
                Dim outmostVariable = GetOutermostVariableToMoveIntoMethodDefinition(cancellationToken)
                If outmostVariable Is Nothing Then
                    Return Nothing
                End If

                Dim idToken = outmostVariable.GetIdentifierTokenAtDeclaration(SemanticDocument)
                Dim declStatement = idToken.GetAncestor(Of LocalDeclarationStatementSyntax)()

                Contract.ThrowIfNull(declStatement)
                Contract.ThrowIfFalse(declStatement.Parent.IsStatementContainerNode())

                Return declStatement.Parent
            End Function

            Private Function CreateMethodModifiers() As DeclarationModifiers
                Dim isShared = False

                If Not Me.AnalyzerResult.UseInstanceMember AndAlso
                   Not Me.SelectionResult.IsUnderModuleBlock() AndAlso
                   Not Me.SelectionResult.ContainsInstanceExpression() Then
                    isShared = True
                End If

                Dim isAsync = Me.SelectionResult.ShouldPutAsyncModifier()

                Return New DeclarationModifiers(isStatic:=isShared, isAsync:=isAsync)
            End Function

            Public Overrides Function GetNewMethodStatements(
                    insertionPointNode As SyntaxNode, cancellationToken As CancellationToken) As OperationStatus(Of ImmutableArray(Of SyntaxNode))
                Dim statements = CreateMethodBody(insertionPointNode, cancellationToken)
                Dim status = CheckActiveStatements(statements)
                Return status.With(statements.CastArray(Of SyntaxNode))
            End Function

            Private Function CreateMethodBody(insertionPointNode As SyntaxNode, cancellationToken As CancellationToken) As ImmutableArray(Of StatementSyntax)
                Dim statements = GetInitialStatementsForMethodDefinitions()
                statements = SplitOrMoveDeclarationIntoMethodDefinition(insertionPointNode, statements, cancellationToken)
                statements = MoveDeclarationOutFromMethodDefinition(statements, cancellationToken)

                Dim emptyStatements = ImmutableArray(Of StatementSyntax).Empty
                Dim returnStatements = AppendReturnStatementIfNeeded(emptyStatements)

                statements = statements.Concat(returnStatements)

                Dim semanticModel = SemanticDocument.SemanticModel

                statements = PostProcessor.RemoveDeclarationAssignmentPattern(statements)
                statements = PostProcessor.RemoveInitializedDeclarationAndReturnPattern(statements)

                Return statements
            End Function

            Private Shared Function CheckActiveStatements(statements As ImmutableArray(Of StatementSyntax)) As OperationStatus
                Dim count = statements.Count()
                If count = 0 Then
                    Return OperationStatus.NoActiveStatement
                End If

                If count = 1 Then
                    Dim returnStatement = TryCast(statements(0), ReturnStatementSyntax)
                    If returnStatement IsNot Nothing AndAlso returnStatement.Expression Is Nothing Then
                        Return OperationStatus.NoActiveStatement
                    End If
                End If

                For Each statement In statements
                    Dim localDeclStatement = TryCast(statement, LocalDeclarationStatementSyntax)
                    If localDeclStatement Is Nothing Then
                        'found one
                        Return OperationStatus.SucceededStatus
                    End If

                    For Each variableDecl In localDeclStatement.Declarators
                        If variableDecl.Initializer IsNot Nothing Then
                            'found one
                            Return OperationStatus.SucceededStatus
                        ElseIf TypeOf variableDecl.AsClause Is AsNewClauseSyntax Then
                            'found one
                            Return OperationStatus.SucceededStatus
                        End If
                    Next
                Next

                Return OperationStatus.NoActiveStatement
            End Function

            Private Function MoveDeclarationOutFromMethodDefinition(statements As ImmutableArray(Of StatementSyntax), cancellationToken As CancellationToken) As ImmutableArray(Of StatementSyntax)
                Dim variableToRemoveMap = CreateVariableDeclarationToRemoveMap(
                    Me.AnalyzerResult.GetVariablesToMoveOutToCallSiteOrDelete(cancellationToken), cancellationToken)

                Dim declarationStatements = New List(Of StatementSyntax)()

                For Each statement In statements

                    Dim declarationStatement = TryCast(statement, LocalDeclarationStatementSyntax)
                    If declarationStatement Is Nothing Then
                        ' if given statement is not decl statement, do nothing.
                        declarationStatements.Add(statement)
                        Continue For
                    End If

                    Dim expressionStatements = New List(Of StatementSyntax)()
                    Dim variableDeclarators = New List(Of VariableDeclaratorSyntax)()
                    Dim triviaList = New List(Of SyntaxTrivia)()

                    If Not variableToRemoveMap.ProcessLocalDeclarationStatement(declarationStatement, expressionStatements, variableDeclarators, triviaList) Then
                        declarationStatements.Add(statement)
                        Continue For
                    End If

                    If variableDeclarators.Count = 0 AndAlso
                       triviaList.Any(Function(t) t.Kind <> SyntaxKind.WhitespaceTrivia AndAlso t.Kind <> SyntaxKind.EndOfLineTrivia) Then
                        ' well, there are trivia associated with the node.
                        ' we can't just delete the node since then, we will lose
                        ' the trivia. unfortunately, it is not easy to attach the trivia
                        ' to next token. for now, create an empty statement and associate the
                        ' trivia to the statement

                        ' TODO : think about a way to trivia attached to next token
                        Dim emptyStatement = SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxKind.EmptyToken).WithLeadingTrivia(SyntaxFactory.TriviaList(triviaList)))
                        declarationStatements.Add(emptyStatement)

                        triviaList.Clear()
                    End If

                    ' return survived var decls
                    If variableDeclarators.Count > 0 Then
                        Dim localStatement =
                            SyntaxFactory.LocalDeclarationStatement(
                                declarationStatement.Modifiers,
                                SyntaxFactory.SeparatedList(variableDeclarators)).WithPrependedLeadingTrivia(triviaList)

                        declarationStatements.Add(localStatement)
                        triviaList.Clear()
                    End If

                    ' return any expression statement if there was any
                    For Each expressionStatement In expressionStatements
                        declarationStatements.Add(expressionStatement)
                    Next expressionStatement
                Next

                Return declarationStatements.ToImmutableArray()
            End Function

            Private Function SplitOrMoveDeclarationIntoMethodDefinition(
                    insertionPointNode As SyntaxNode,
                    statements As ImmutableArray(Of StatementSyntax),
                    cancellationToken As CancellationToken) As ImmutableArray(Of StatementSyntax)
                Dim semanticModel = Me.SemanticDocument.SemanticModel
                Dim postProcessor = New PostProcessor(semanticModel, insertionPointNode.SpanStart)

                Dim declStatements = CreateDeclarationStatements(AnalyzerResult.GetVariablesToSplitOrMoveIntoMethodDefinition(cancellationToken), cancellationToken)
                declStatements = postProcessor.MergeDeclarationStatements(declStatements)

                Return declStatements.Concat(statements)
            End Function

            Protected Overrides Function CreateIdentifier(name As String) As SyntaxToken
                Return SyntaxFactory.Identifier(name)
            End Function

            Protected Overrides Function CreateReturnStatement(Optional identifierName As String = Nothing) As StatementSyntax
                If String.IsNullOrEmpty(identifierName) Then
                    Return SyntaxFactory.ReturnStatement()
                End If

                Return SyntaxFactory.ReturnStatement(expression:=SyntaxFactory.IdentifierName(identifierName))
            End Function

            Protected Overrides Function LastStatementOrHasReturnStatementInReturnableConstruct() As Boolean
                Dim lastStatement = GetLastStatementOrInitializerSelectedAtCallSite()
                Dim container = lastStatement.GetAncestorsOrThis(Of SyntaxNode).Where(Function(n) n.IsReturnableConstruct()).FirstOrDefault()
                If container Is Nothing Then
                    ' case such as field initializer
                    Return False
                End If

                Dim statements = container.GetStatements()
                If statements.Count = 0 Then
                    ' such as expression lambda
                    Return False
                End If

                If statements.Last() Is lastStatement Then
                    Return True
                End If

                Dim index = statements.IndexOf(lastStatement)
                Return statements(index + 1).IsKind(SyntaxKind.ReturnStatement, SyntaxKind.ExitSubStatement)
            End Function

            Protected Overrides Function CreateCallSignature() As ExpressionSyntax
                Dim methodName = CreateMethodNameForInvocation().WithAdditionalAnnotations(Simplifier.Annotation)

                Dim methodExpression =
                    If(Me.AnalyzerResult.UseInstanceMember AndAlso Me.ExtractMethodGenerationOptions.SimplifierOptions.QualifyMethodAccess.Value,
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.MeExpression(), SyntaxFactory.Token(SyntaxKind.DotToken), methodName),
                        DirectCast(methodName, ExpressionSyntax))

                Dim arguments = New List(Of ArgumentSyntax)()
                For Each argument In AnalyzerResult.MethodParameters
                    arguments.Add(SyntaxFactory.SimpleArgument(GetIdentifierName(argument.Name)))
                Next argument

                Dim invocation = SyntaxFactory.InvocationExpression(
                    methodExpression, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)))

                If Me.SelectionResult.ShouldPutAsyncModifier() Then
                    If Me.SelectionResult.ShouldCallConfigureAwaitFalse() Then
                        If AnalyzerResult.ReturnType.GetMembers().Any(
                        Function(x)
                            Dim method = TryCast(x, IMethodSymbol)
                            If method Is Nothing Then
                                Return False
                            End If

                            If Not CaseInsensitiveComparison.Equals(method.Name, NameOf(Task.ConfigureAwait)) Then
                                Return False
                            End If

                            If method.Parameters.Length <> 1 Then
                                Return False
                            End If

                            Return method.Parameters(0).Type.SpecialType = SpecialType.System_Boolean
                        End Function) Then

                            invocation = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                invocation,
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName(NameOf(Task.ConfigureAwait))),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(Of ArgumentSyntax)(
                                SyntaxFactory.SimpleArgument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.FalseLiteralExpression,
                                        SyntaxFactory.Token(SyntaxKind.FalseKeyword))))))
                        End If
                    End If

                    Return SyntaxFactory.AwaitExpression(invocation)
                End If

                Return invocation
            End Function

            Private Shared Function GetIdentifierName(name As String) As ExpressionSyntax
                Dim bracket = SyntaxFacts.MakeHalfWidthIdentifier(name.First) = "[" AndAlso SyntaxFacts.MakeHalfWidthIdentifier(name.Last) = "]"
                If bracket Then
                    Dim unescaped = name.Substring(1, name.Length() - 2)
                    Return SyntaxFactory.IdentifierName(SyntaxFactory.BracketedIdentifier(unescaped))
                End If

                Return SyntaxFactory.IdentifierName(name)
            End Function

            Protected Overrides Function CreateAssignmentExpressionStatement(identifier As SyntaxToken, rvalue As ExpressionSyntax) As StatementSyntax
                Return identifier.CreateAssignmentExpressionStatementWithValue(rvalue)
            End Function

            Protected Overrides Function CreateDeclarationStatement(
                    variable As VariableInfo,
                    givenInitializer As ExpressionSyntax,
                    cancellationToken As CancellationToken) As StatementSyntax

                Dim shouldInitializeWithNothing = (variable.GetDeclarationBehavior(cancellationToken) = DeclarationBehavior.MoveOut OrElse variable.GetDeclarationBehavior(cancellationToken) = DeclarationBehavior.SplitOut) AndAlso
                                                  (variable.ParameterModifier = ParameterBehavior.Out)

                Dim initializer = If(givenInitializer, If(shouldInitializeWithNothing, SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword)), Nothing))

                Dim variableType = variable.GetVariableType()
                Dim typeNode = variableType.GenerateTypeSyntax()

                Dim names = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(variable.Name)))
                Dim modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.DimKeyword))
                Dim equalsValue = If(initializer Is Nothing, Nothing, SyntaxFactory.EqualsValue(value:=initializer))
                Dim declarators = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(names, SyntaxFactory.SimpleAsClause(type:=typeNode), equalsValue))

                Return SyntaxFactory.LocalDeclarationStatement(modifiers, declarators)
            End Function

            Protected Overrides Async Function CreateGeneratedCodeAsync(newDocument As SemanticDocument, cancellationToken As CancellationToken) As Task(Of GeneratedCode)
                ' in hybrid code cases such as extract method, formatter will have some difficulties on where it breaks lines in two.
                ' here, we explicitly insert newline at the end of auto generated method decl's begin statement so that anchor knows how to find out
                ' indentation of inserted statements (from users code) with user code style preserved
                Dim root = newDocument.Root
                Dim methodDefinition = root.GetAnnotatedNodes(Of MethodBlockBaseSyntax)(Me.MethodDefinitionAnnotation).First()
                Dim lastTokenOfBeginStatement = methodDefinition.BlockStatement.GetLastToken(includeZeroWidth:=True)

                Dim newMethodDefinition = methodDefinition.ReplaceToken(
                    lastTokenOfBeginStatement,
                    lastTokenOfBeginStatement.WithAppendedTrailingTrivia(
                        SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticCarriageReturnLineFeed)))

                newDocument = Await newDocument.WithSyntaxRootAsync(root.ReplaceNode(methodDefinition, newMethodDefinition), cancellationToken).ConfigureAwait(False)

                Return Await MyBase.CreateGeneratedCodeAsync(newDocument, cancellationToken).ConfigureAwait(False)
            End Function
        End Class
    End Class
End Namespace
