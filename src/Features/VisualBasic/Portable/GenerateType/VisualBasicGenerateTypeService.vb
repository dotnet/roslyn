' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
Imports Microsoft.CodeAnalysis.GenerateType
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateType
    <ExportLanguageService(GetType(IGenerateTypeService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateTypeService
        Inherits AbstractGenerateTypeService(Of VisualBasicGenerateTypeService, SimpleNameSyntax, ObjectCreationExpressionSyntax, ExpressionSyntax, TypeBlockSyntax, ArgumentSyntax)

        Private Shared ReadOnly s_annotation As SyntaxAnnotation = New SyntaxAnnotation

        Protected Overrides ReadOnly Property DefaultFileExtension As String
            Get
                Return ".vb"
            End Get
        End Property

        Protected Overrides Function GenerateParameterNames(semanticModel As SemanticModel, arguments As IList(Of ArgumentSyntax)) As IList(Of String)
            Return semanticModel.GenerateParameterNames(arguments)
        End Function

        Protected Overrides Function GetLeftSideOfDot(simpleName As SimpleNameSyntax) As ExpressionSyntax
            Return simpleName.GetLeftSideOfDot()
        End Function

        Protected Overrides Function IsArrayElementType(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.ArrayCreationExpression)
        End Function

        Protected Overrides Function IsInCatchDeclaration(expression As ExpressionSyntax) As Boolean
            Return False
        End Function

        Protected Overrides Function IsInInterfaceList(expression As ExpressionSyntax) As Boolean
            If TypeOf expression Is TypeSyntax AndAlso
                      expression.IsParentKind(SyntaxKind.ImplementsStatement) Then
                Return True
            End If

            If TypeOf expression Is TypeSyntax AndAlso
               expression.IsParentKind(SyntaxKind.TypeConstraint) AndAlso
               expression.Parent.IsParentKind(SyntaxKind.TypeParameterMultipleConstraintClause) Then
                ' TODO: Code Coverage
                Dim typeConstraint = DirectCast(expression.Parent, TypeConstraintSyntax)
                Dim constraintClause = DirectCast(typeConstraint.Parent, TypeParameterMultipleConstraintClauseSyntax)
                Dim index = constraintClause.Constraints.IndexOf(typeConstraint)
                Return index > 0
            End If

            Return False
        End Function

        Protected Overrides Function IsInValueTypeConstraintContext(semanticModel As SemanticModel, expression As Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax, cancellationToken As System.Threading.CancellationToken) As Boolean
            ' TODO(cyrusn) implement this
            Return False
        End Function

        Protected Overrides Function TryGetArgumentList(
                objectCreationExpression As ObjectCreationExpressionSyntax,
                ByRef argumentList As IList(Of ArgumentSyntax)) As Boolean
            If objectCreationExpression IsNot Nothing AndAlso
               objectCreationExpression.ArgumentList IsNot Nothing Then
                argumentList = objectCreationExpression.ArgumentList.Arguments.ToList()
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function TryGetNameParts(expression As ExpressionSyntax,
                                                     ByRef nameParts As IList(Of String)) As Boolean
            Return expression.TryGetNameParts(nameParts)
        End Function

        Protected Overrides Function TryInitializeState(
                document As SemanticDocument, simpleName As SimpleNameSyntax, cancellationToken As CancellationToken, ByRef generateTypeServiceStateOptions As GenerateTypeServiceStateOptions) As Boolean
            generateTypeServiceStateOptions = New GenerateTypeServiceStateOptions()

            If simpleName.IsParentKind(SyntaxKind.DictionaryAccessExpression) Then
                Return False
            End If

            Dim nameOrMemberAccessExpression As ExpressionSyntax = Nothing
            If simpleName.IsRightSideOfDot() Then
                nameOrMemberAccessExpression = DirectCast(simpleName.Parent, ExpressionSyntax)
                If Not (TypeOf simpleName.GetLeftSideOfDot() Is NameSyntax) Then
                    Return False
                End If
            Else
                nameOrMemberAccessExpression = simpleName
            End If

            generateTypeServiceStateOptions.NameOrMemberAccessExpression = nameOrMemberAccessExpression

            If TypeOf nameOrMemberAccessExpression.Parent Is BinaryExpressionSyntax Then
                Return False
            End If

            Dim syntaxTree = document.SyntaxTree
            Dim semanticModel = document.SemanticModel
            If Not SyntaxFacts.IsInNamespaceOrTypeContext(nameOrMemberAccessExpression) Then
                generateTypeServiceStateOptions.IsDelegateAllowed = False

                Dim position = nameOrMemberAccessExpression.SpanStart
                Dim isExpressionContext = syntaxTree.IsExpressionContext(position, cancellationToken)
                Dim isStatementContext = syntaxTree.IsSingleLineStatementContext(position, cancellationToken)
                Dim isExpressionOrStatementContext = isExpressionContext OrElse isStatementContext

                If isExpressionOrStatementContext Then
                    If Not simpleName.IsLeftSideOfDot() Then

                        If nameOrMemberAccessExpression Is Nothing OrElse Not nameOrMemberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                            Return False
                        End If

                        Dim leftSymbol = semanticModel.GetSymbolInfo(DirectCast(nameOrMemberAccessExpression, MemberAccessExpressionSyntax).Expression).Symbol
                        Dim token = simpleName.GetLastToken().GetNextToken()
                        If leftSymbol Is Nothing OrElse
                            Not leftSymbol.IsKind(SymbolKind.Namespace) OrElse
                            Not token.IsKind(SyntaxKind.DotToken) Then
                            Return False
                        Else
                            generateTypeServiceStateOptions.IsMembersWithModule = True
                            generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess = True
                        End If
                    End If

                    If Not generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess AndAlso
                        Not SyntaxFacts.IsInNamespaceOrTypeContext(simpleName) Then
                        Dim token = simpleName.GetLastToken().GetNextToken()
                        If token.IsKind(SyntaxKind.DotToken) AndAlso
                            simpleName.Parent Is token.Parent Then
                            generateTypeServiceStateOptions.IsMembersWithModule = True
                            generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess = True
                        End If
                    End If
                End If
            End If

            If nameOrMemberAccessExpression.Parent.IsKind(SyntaxKind.InvocationExpression) Then
                Return False
            End If

            ' Check if module could be an option
            Dim nextToken = simpleName.GetLastToken().GetNextToken()
            If simpleName.IsLeftSideOfDot() OrElse nextToken.IsKind(SyntaxKind.DotToken) Then
                If simpleName.IsRightSideOfDot() Then
                    Dim parent = TryCast(simpleName.Parent, QualifiedNameSyntax)

                    If parent IsNot Nothing Then
                        Dim leftSymbol = semanticModel.GetSymbolInfo(parent.Left).Symbol

                        If leftSymbol IsNot Nothing And leftSymbol.IsKind(SymbolKind.Namespace) Then
                            generateTypeServiceStateOptions.IsMembersWithModule = True
                        End If
                    End If
                End If
            End If

            If SyntaxFacts.IsInNamespaceOrTypeContext(nameOrMemberAccessExpression) Then

                ' In Namespace or Type Context we cannot have Interface, Enum, Delegate as part of the Left Expression of a QualifiedName
                If nextToken.IsKind(SyntaxKind.DotToken) Then
                    generateTypeServiceStateOptions.IsInterfaceOrEnumNotAllowedInTypeContext = True
                    generateTypeServiceStateOptions.IsDelegateAllowed = False
                    generateTypeServiceStateOptions.IsMembersWithModule = True
                End If

                ' Case : Class Foo(of T as MyType)
                If nameOrMemberAccessExpression.GetAncestors(Of TypeConstraintSyntax).Any() Then
                    generateTypeServiceStateOptions.IsClassInterfaceTypes = True
                    Return True
                End If

                ' Case : Custom Event E As Foo
                ' Case : Public Event F As Foo
                If nameOrMemberAccessExpression.GetAncestors(Of EventStatementSyntax)().Any() Then
                    ' Case : Foo
                    ' Only Delegate
                    If simpleName.Parent IsNot Nothing AndAlso TypeOf simpleName.Parent IsNot QualifiedNameSyntax Then
                        generateTypeServiceStateOptions.IsDelegateOnly = True
                        Return True
                    End If

                    ' Case : Something.Foo ...
                    If TypeOf nameOrMemberAccessExpression Is QualifiedNameSyntax Then

                        ' Case : NSOrSomething.GenType.Foo
                        If nextToken.IsKind(SyntaxKind.DotToken) Then
                            If nameOrMemberAccessExpression.Parent IsNot Nothing AndAlso TypeOf nameOrMemberAccessExpression.Parent Is QualifiedNameSyntax Then
                                Return True
                            Else
                                Contract.Fail("Cannot reach this point")
                            End If
                        Else
                            ' Case : NSOrSomething.GenType
                            generateTypeServiceStateOptions.IsDelegateOnly = True
                            Return True
                        End If
                    End If
                End If

                ' Case : Public WithEvents G As Delegate1
                Dim fieldDecl = nameOrMemberAccessExpression.GetAncestor(Of FieldDeclarationSyntax)()
                If fieldDecl IsNot Nothing AndAlso fieldDecl.GetModifiers().Any(Function(n) n.Kind() = SyntaxKind.WithEventsKeyword) Then
                    generateTypeServiceStateOptions.IsClassInterfaceTypes = True
                    Return True
                End If

                ' No Enum Type Generation in AddHandler or RemoverHandler Statement
                If nameOrMemberAccessExpression.GetAncestors(Of AccessorStatementSyntax)().Any() Then
                    If Not nextToken.IsKind(SyntaxKind.DotToken) AndAlso
                    nameOrMemberAccessExpression.IsParentKind(SyntaxKind.SimpleAsClause) AndAlso
                    nameOrMemberAccessExpression.Parent.IsParentKind(SyntaxKind.Parameter) AndAlso
                    nameOrMemberAccessExpression.Parent.Parent.IsParentKind(SyntaxKind.ParameterList) AndAlso
                    (nameOrMemberAccessExpression.Parent.Parent.Parent.IsParentKind(SyntaxKind.AddHandlerAccessorStatement) OrElse
                    nameOrMemberAccessExpression.Parent.Parent.Parent.IsParentKind(SyntaxKind.RemoveHandlerAccessorStatement)) Then
                        generateTypeServiceStateOptions.IsDelegateOnly = True
                        Return True
                    End If

                    generateTypeServiceStateOptions.IsEnumNotAllowed = True
                End If
            Else
                ' MemberAccessExpression
                If nameOrMemberAccessExpression.GetAncestors(Of UnaryExpressionSyntax)().Any(Function(n) n.IsKind(SyntaxKind.AddressOfExpression)) Then
                    generateTypeServiceStateOptions.IsEnumNotAllowed = True
                End If

                ' Check to see if the expression is part of Invocation Expression
                If (nameOrMemberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) OrElse (nameOrMemberAccessExpression.Parent IsNot Nothing AndAlso nameOrMemberAccessExpression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression))) _
                    AndAlso nameOrMemberAccessExpression.IsLeftSideOfDot() Then
                    Dim outerMostMemberAccessExpression As ExpressionSyntax = Nothing
                    If nameOrMemberAccessExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                        outerMostMemberAccessExpression = nameOrMemberAccessExpression
                    Else
                        Debug.Assert(nameOrMemberAccessExpression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression))
                        outerMostMemberAccessExpression = DirectCast(nameOrMemberAccessExpression.Parent, ExpressionSyntax)
                    End If

                    outerMostMemberAccessExpression = outerMostMemberAccessExpression.GetAncestorsOrThis(Of ExpressionSyntax)().SkipWhile(Function(n) n IsNot Nothing AndAlso n.IsKind(SyntaxKind.SimpleMemberAccessExpression)).FirstOrDefault()
                    If outerMostMemberAccessExpression IsNot Nothing AndAlso TypeOf outerMostMemberAccessExpression Is InvocationExpressionSyntax Then
                        generateTypeServiceStateOptions.IsEnumNotAllowed = True
                    End If
                End If
            End If

            ' New MyDelegate(AddressOf foo)
            ' New NS.MyDelegate(Function(n) n)
            If TypeOf nameOrMemberAccessExpression.Parent Is ObjectCreationExpressionSyntax Then
                Dim objectCreationExpressionOpt = DirectCast(nameOrMemberAccessExpression.Parent, ObjectCreationExpressionSyntax)
                generateTypeServiceStateOptions.ObjectCreationExpressionOpt = objectCreationExpressionOpt

                ' Interface and Enum not allowed
                generateTypeServiceStateOptions.IsInterfaceOrEnumNotAllowedInTypeContext = True

                If objectCreationExpressionOpt.ArgumentList IsNot Nothing Then
                    If objectCreationExpressionOpt.ArgumentList.CloseParenToken.IsMissing Then
                        Return False
                    End If

                    ' Get the Method Symbol for Delegate to be created
                    ' Currently simple argument is the only argument that can be fed to the Object Creation for Delegate Creation
                    If generateTypeServiceStateOptions.IsDelegateAllowed AndAlso
                        objectCreationExpressionOpt.ArgumentList.Arguments.Count = 1 AndAlso
                        TypeOf objectCreationExpressionOpt.ArgumentList.Arguments(0) Is SimpleArgumentSyntax Then
                        Dim simpleArgumentExpression = DirectCast(objectCreationExpressionOpt.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression

                        If simpleArgumentExpression.IsKind(SyntaxKind.AddressOfExpression) Then
                            generateTypeServiceStateOptions.DelegateCreationMethodSymbol = GetMemberGroupIfPresent(semanticModel, DirectCast(simpleArgumentExpression, UnaryExpressionSyntax).Operand, cancellationToken)
                        ElseIf (simpleArgumentExpression.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) OrElse
                                simpleArgumentExpression.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) OrElse
                                simpleArgumentExpression.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
                                simpleArgumentExpression.IsKind(SyntaxKind.SingleLineSubLambdaExpression)) Then
                            generateTypeServiceStateOptions.DelegateCreationMethodSymbol = TryCast(semanticModel.GetSymbolInfo(simpleArgumentExpression, cancellationToken).Symbol, IMethodSymbol)
                        End If
                    ElseIf objectCreationExpressionOpt.ArgumentList.Arguments.Count <> 1 Then
                        generateTypeServiceStateOptions.IsDelegateAllowed = False
                    End If
                End If

                Dim initializers = TryCast(objectCreationExpressionOpt.Initializer, ObjectMemberInitializerSyntax)
                If initializers IsNot Nothing Then
                    For Each initializer In initializers.Initializers.ToList()
                        Dim namedFieldInitializer = TryCast(initializer, NamedFieldInitializerSyntax)
                        If namedFieldInitializer IsNot Nothing Then
                            generateTypeServiceStateOptions.PropertiesToGenerate.Add(namedFieldInitializer.Name)
                        End If
                    Next
                End If
            End If

            Dim variableDeclarator As VariableDeclaratorSyntax = Nothing
            If generateTypeServiceStateOptions.IsDelegateAllowed Then

                ' Dim f As MyDel = ...
                ' Dim f as NS.MyDel = ...
                If nameOrMemberAccessExpression.IsParentKind(SyntaxKind.SimpleAsClause) AndAlso
                   nameOrMemberAccessExpression.Parent.IsParentKind(SyntaxKind.VariableDeclarator) Then
                    variableDeclarator = DirectCast(nameOrMemberAccessExpression.Parent.Parent, VariableDeclaratorSyntax)

                    If variableDeclarator.Initializer IsNot Nothing AndAlso variableDeclarator.Initializer.Value IsNot Nothing Then
                        Dim expression = variableDeclarator.Initializer.Value
                        If expression.IsKind(SyntaxKind.AddressOfExpression) Then
                            ' ... = AddressOf Foo
                            generateTypeServiceStateOptions.DelegateCreationMethodSymbol = GetMemberGroupIfPresent(semanticModel, DirectCast(expression, UnaryExpressionSyntax).Operand, cancellationToken)
                        Else
                            If TypeOf expression Is LambdaExpressionSyntax Then
                                '... = Lambda
                                Dim type = semanticModel.GetTypeInfo(expression, cancellationToken).Type
                                If type IsNot Nothing AndAlso type.IsDelegateType() Then
                                    generateTypeServiceStateOptions.DelegateCreationMethodSymbol = DirectCast(type, INamedTypeSymbol).DelegateInvokeMethod
                                End If

                                Dim symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol
                                If symbol IsNot Nothing AndAlso symbol.IsKind(SymbolKind.Method) Then
                                    generateTypeServiceStateOptions.DelegateCreationMethodSymbol = DirectCast(symbol, IMethodSymbol)
                                End If
                            End If
                        End If
                    End If
                ElseIf TypeOf nameOrMemberAccessExpression.Parent Is CastExpressionSyntax Then
                    ' Case: Dim s1 = DirectCast(AddressOf foo, Myy)
                    '       Dim s2 = TryCast(AddressOf foo, Myy)
                    '       Dim s3 = CType(AddressOf foo, Myy)
                    Dim expressionToBeCasted = DirectCast(nameOrMemberAccessExpression.Parent, CastExpressionSyntax).Expression
                    If expressionToBeCasted.IsKind(SyntaxKind.AddressOfExpression) Then
                        ' ... = AddressOf Foo
                        generateTypeServiceStateOptions.DelegateCreationMethodSymbol = GetMemberGroupIfPresent(semanticModel, DirectCast(expressionToBeCasted, UnaryExpressionSyntax).Operand, cancellationToken)
                    End If
                End If
            End If

            Return True
        End Function

        Private Function GetMemberGroupIfPresent(semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As IMethodSymbol
            If expression Is Nothing Then
                Return Nothing
            End If

            Dim memberGroup = semanticModel.GetMemberGroup(expression, cancellationToken)
            If memberGroup.Count <> 0 Then
                Return If(memberGroup.ElementAt(0).IsKind(SymbolKind.Method), DirectCast(memberGroup.ElementAt(0), IMethodSymbol), Nothing)
            End If

            Return Nothing
        End Function

        Public Overrides Function GetRootNamespace(options As CompilationOptions) As String
            Return DirectCast(options, VisualBasicCompilationOptions).RootNamespace
        End Function

        Protected Overloads Overrides Function GetTypeParameters(state As State,
                                                                 semanticModel As SemanticModel,
                                                                 cancellationToken As CancellationToken) As IList(Of ITypeParameterSymbol)
            If TypeOf state.SimpleName Is GenericNameSyntax Then
                Dim genericName = DirectCast(state.SimpleName, GenericNameSyntax)
                Dim typeArguments = If(state.SimpleName.Arity = genericName.TypeArgumentList.Arguments.Count,
                    genericName.TypeArgumentList.Arguments.OfType(Of SyntaxNode)().ToList(),
                    Enumerable.Repeat(Of SyntaxNode)(Nothing, state.SimpleName.Arity))
                Return Me.GetTypeParameters(state, semanticModel, typeArguments, cancellationToken)
            End If

            Return SpecializedCollections.EmptyList(Of ITypeParameterSymbol)()
        End Function

        Protected Overrides Function IsInVariableTypeContext(expression As Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.SimpleAsClause)
        End Function

        Protected Overrides Function DetermineTypeToGenerateIn(semanticModel As SemanticModel,
                                                               simpleName As SimpleNameSyntax,
                                                               cancellationToken As CancellationToken) As INamedTypeSymbol
            Dim typeBlock = simpleName.GetAncestorsOrThis(Of TypeBlockSyntax).
                                          Where(Function(t) t.Members.Count > 0).
                                          FirstOrDefault(Function(t) simpleName.SpanStart >= t.Members.First().SpanStart AndAlso
                                                             simpleName.Span.End <= t.Members.Last().Span.End)
            Return If(typeBlock Is Nothing, Nothing, TryCast(semanticModel.GetDeclaredSymbol(typeBlock.BlockStatement, cancellationToken), INamedTypeSymbol))
        End Function

        Protected Overrides Function GetAccessibility(state As State, semanticModel As SemanticModel, intoNamespace As Boolean, cancellationToken As CancellationToken) As Accessibility
            Dim accessibility = DetermineDefaultAccessibility(state, semanticModel, intoNamespace, cancellationToken)
            If Not state.IsTypeGeneratedIntoNamespaceFromMemberAccess Then
                Dim accessibilityConstraint = semanticModel.DetermineAccessibilityConstraint(
                    TryCast(state.NameOrMemberAccessExpression, TypeSyntax), cancellationToken)

                If accessibilityConstraint = Accessibility.Public OrElse
                   accessibilityConstraint = Accessibility.Internal Then
                    accessibility = accessibilityConstraint
                End If
            End If

            Return accessibility
        End Function

        Protected Overrides Function DetermineArgumentType(semanticModel As SemanticModel, argument As ArgumentSyntax, cancellationToken As CancellationToken) As ITypeSymbol
            Return argument.DetermineType(semanticModel, cancellationToken)
        End Function

        Protected Overrides Function IsConversionImplicit(compilation As Compilation, sourceType As ITypeSymbol, targetType As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(sourceType, targetType).IsWidening
        End Function

        Public Overrides Async Function GetOrGenerateEnclosingNamespaceSymbolAsync(namedTypeSymbol As INamedTypeSymbol, containers() As String, selectedDocument As Document, selectedDocumentRoot As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Tuple(Of INamespaceSymbol, INamespaceOrTypeSymbol, Location))
            Dim compilationUnit = DirectCast(selectedDocumentRoot, CompilationUnitSyntax)
            Dim semanticModel = Await selectedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            If containers.Length <> 0 Then
                ' Search the NS declaration in the root
                Dim containerList = New List(Of String)(containers)
                Dim enclosingNamespace = GetDeclaringNamespace(containerList, 0, compilationUnit)
                If enclosingNamespace IsNot Nothing Then
                    Dim enclosingNamespaceSymbol = semanticModel.GetSymbolInfo(enclosingNamespace.Name)
                    If enclosingNamespaceSymbol.Symbol IsNot Nothing Then
                        Return Tuple.Create(DirectCast(enclosingNamespaceSymbol.Symbol, INamespaceSymbol),
                                            DirectCast(namedTypeSymbol, INamespaceOrTypeSymbol),
                                            DirectCast(enclosingNamespace.Parent, NamespaceBlockSyntax).EndNamespaceStatement.GetLocation())
                        Return Nothing
                    End If
                End If
            End If

            Dim globalNamespace = semanticModel.GetEnclosingNamespace(0, cancellationToken)
            Dim rootNamespaceOrType = namedTypeSymbol.GenerateRootNamespaceOrType(containers)
            Dim lastMember = compilationUnit.Members.LastOrDefault()
            Dim afterThisLocation As Location = Nothing

            ' Add at the end
            If lastMember Is Nothing Then
                afterThisLocation = semanticModel.SyntaxTree.GetLocation(New TextSpan())
            Else
                afterThisLocation = semanticModel.SyntaxTree.GetLocation(New TextSpan(lastMember.Span.End, 0))
            End If

            Return Tuple.Create(globalNamespace,
                                rootNamespaceOrType,
                                afterThisLocation)
        End Function

        Private Function GetDeclaringNamespace(containers As List(Of String), indexDone As Integer, compilationUnit As CompilationUnitSyntax) As NamespaceStatementSyntax
            For Each member In compilationUnit.Members
                Dim namespaceDeclaration = GetDeclaringNamespace(containers, 0, member)
                If namespaceDeclaration IsNot Nothing Then
                    Return namespaceDeclaration
                End If
            Next

            Return Nothing
        End Function

        Private Function GetDeclaringNamespace(containers As List(Of String), indexDone As Integer, localRoot As SyntaxNode) As NamespaceStatementSyntax
            Dim namespaceBlock = TryCast(localRoot, NamespaceBlockSyntax)
            If namespaceBlock IsNot Nothing Then
                Dim matchingNamesCount = MatchingNamesFromNamespaceName(containers, indexDone, namespaceBlock.NamespaceStatement)

                If matchingNamesCount = -1 Then
                    Return Nothing
                End If

                If containers.Count = indexDone + matchingNamesCount Then
                    Return namespaceBlock.NamespaceStatement
                Else
                    indexDone += matchingNamesCount
                End If

                For Each member In namespaceBlock.Members
                    Dim resultantNamespace = GetDeclaringNamespace(containers, indexDone, member)
                    If resultantNamespace IsNot Nothing Then
                        Return resultantNamespace
                    End If
                Next

                Return Nothing
            End If

            Return Nothing

        End Function

        Private Function MatchingNamesFromNamespaceName(containers As List(Of String), indexDone As Integer, namespaceStatementSyntax As NamespaceStatementSyntax) As Integer

            If namespaceStatementSyntax Is Nothing Then
                Return -1
            End If

            Dim namespaceContainers = New List(Of String)()
            GetNamespaceContainers(namespaceStatementSyntax.Name, namespaceContainers)

            If namespaceContainers.Count + indexDone > containers.Count OrElse
                Not IdentifierMatches(indexDone, namespaceContainers, containers) Then
                Return -1
            End If

            Return namespaceContainers.Count
        End Function

        Private Function IdentifierMatches(indexDone As Integer, namespaceContainers As List(Of String), containers As List(Of String)) As Boolean
            For index = 0 To namespaceContainers.Count - 1
                If Not namespaceContainers(index).Equals(containers(indexDone + index), StringComparison.OrdinalIgnoreCase) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Sub GetNamespaceContainers(name As NameSyntax, namespaceContainers As List(Of String))
            If TypeOf name Is QualifiedNameSyntax Then
                GetNamespaceContainers(DirectCast(name, QualifiedNameSyntax).Left, namespaceContainers)
                namespaceContainers.Add(DirectCast(name, QualifiedNameSyntax).Right.Identifier.ValueText)
            ElseIf TypeOf name Is SimpleNameSyntax Then
                namespaceContainers.Add(DirectCast(name, SimpleNameSyntax).Identifier.ValueText)
            Else
                Debug.Assert(TypeOf name Is GlobalNameSyntax)
                namespaceContainers.Add(DirectCast(name, GlobalNameSyntax).GlobalKeyword.ValueText)
            End If
        End Sub

        Friend Overrides Function TryGetBaseList(expression As ExpressionSyntax, ByRef typeKindValue As TypeKindOptions) As Boolean
            typeKindValue = TypeKindOptions.AllOptions
            If expression Is Nothing Then
                Return False
            End If

            Dim node As SyntaxNode = expression
            While node IsNot Nothing
                If TypeOf node Is InheritsStatementSyntax Then
                    If node.Parent IsNot Nothing AndAlso TypeOf node.Parent Is InterfaceBlockSyntax Then
                        typeKindValue = TypeKindOptions.Interface
                        Return True
                    End If

                    typeKindValue = TypeKindOptions.Class
                    Return True
                ElseIf TypeOf node Is ImplementsStatementSyntax Then
                    typeKindValue = TypeKindOptions.Interface
                    Return True
                End If

                node = node.Parent
            End While

            Return False
        End Function

        Friend Overrides Function IsPublicOnlyAccessibility(expression As ExpressionSyntax, project As Project) As Boolean
            If expression Is Nothing Then
                Return False
            End If

            If GeneratedTypesMustBePublic(project) Then
                Return True
            End If

            Dim node As SyntaxNode = expression
            While node IsNot Nothing
                ' Types in BaseList, Type Constraint or Member Types cannot be of more restricted accessibility than the declaring type
                If TypeOf node Is InheritsOrImplementsStatementSyntax AndAlso
                    node.Parent IsNot Nothing AndAlso
                    TypeOf node.Parent Is TypeBlockSyntax Then

                    Return IsAllContainingTypeBlocksPublic(node.Parent)
                End If

                If TypeOf node Is TypeParameterListSyntax AndAlso
                        node.Parent IsNot Nothing AndAlso TypeOf node.Parent Is TypeStatementSyntax AndAlso
                        node.Parent.Parent IsNot Nothing AndAlso TypeOf node.Parent.Parent Is TypeBlockSyntax Then
                    Return IsAllContainingTypeBlocksPublic(node.Parent.Parent)
                End If

                If TypeOf node Is EventStatementSyntax AndAlso
                        node.Parent IsNot Nothing AndAlso TypeOf node.Parent Is TypeBlockSyntax Then
                    Return IsAllContainingTypeBlocksPublic(node)
                End If

                If TypeOf node Is FieldDeclarationSyntax AndAlso
                    node.Parent IsNot Nothing AndAlso TypeOf node.Parent Is TypeBlockSyntax Then
                    Return IsAllContainingTypeBlocksPublic(node)
                End If

                node = node.Parent
            End While

            Return False
        End Function

        Private Function IsAllContainingTypeBlocksPublic(node As SyntaxNode) As Boolean
            ' Make sure all the Ancestoral Type Blocks are Declared with Public Access Modifiers
            Dim containingTypeBlocks = node.GetAncestorsOrThis(Of TypeBlockSyntax)()
            If containingTypeBlocks.Count() = 0 Then
                Return True
            Else
                Return containingTypeBlocks.All(Function(typeBlock) typeBlock.GetModifiers().Any(Function(n) n.Kind() = SyntaxKind.PublicKeyword))
            End If
        End Function

        Friend Overrides Function IsGenericName(expression As SimpleNameSyntax) As Boolean
            If expression Is Nothing Then
                Return False
            End If

            Dim node = TryCast(expression, GenericNameSyntax)
            Return node IsNot Nothing
        End Function

        Friend Overrides Function IsSimpleName(expression As ExpressionSyntax) As Boolean
            Return TypeOf expression Is SimpleNameSyntax
        End Function

        Friend Overrides Async Function TryAddUsingsOrImportToDocumentAsync(updatedSolution As Solution, modifiedRoot As SyntaxNode, document As Document, simpleName As SimpleNameSyntax, includeUsingsOrImports As String, cancellationToken As CancellationToken) As Task(Of Solution)
            ' Nothing to include
            If String.IsNullOrWhiteSpace(includeUsingsOrImports) Then
                Return updatedSolution
            End If

            Dim placeSystemNamespaceFirst = document.Project.Solution.Workspace.Options.GetOption(OrganizerOptions.PlaceSystemNamespaceFirst, document.Project.Language)
            Dim root As SyntaxNode = Nothing
            If (modifiedRoot Is Nothing) Then
                root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Else
                root = modifiedRoot
            End If

            If TypeOf root Is CompilationUnitSyntax Then
                Dim compilationRoot = DirectCast(root, CompilationUnitSyntax)
                Dim memberImportsClause = SyntaxFactory.SimpleImportsClause(
                    name:=SyntaxFactory.ParseName(includeUsingsOrImports))
                Dim lastToken = memberImportsClause.GetLastToken()
                Dim lastTokenWithEndOfLineTrivia = lastToken.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)

                ' Replace the token the line carriage
                memberImportsClause = memberImportsClause.ReplaceToken(lastToken, lastTokenWithEndOfLineTrivia)

                Dim newImport = SyntaxFactory.ImportsStatement(
                    importsClauses:=SyntaxFactory.SingletonSeparatedList(Of ImportsClauseSyntax)(memberImportsClause))

                ' Check if the imports is already present
                Dim importsClauses = compilationRoot.Imports.Select(Function(n) n.ImportsClauses)
                For Each importClause In importsClauses
                    For Each import In importClause
                        If TypeOf import Is SimpleImportsClauseSyntax Then
                            Dim membersImport = DirectCast(import, SimpleImportsClauseSyntax)
                            If membersImport.Name IsNot Nothing AndAlso membersImport.Name.ToString().Equals(memberImportsClause.Name.ToString()) Then
                                Return updatedSolution
                            End If
                        End If
                    Next
                Next

                ' Check if the GFU is triggered from the namespace same as the imports namespace
                If Await IsWithinTheImportingNamespaceAsync(document, simpleName.SpanStart, includeUsingsOrImports, cancellationToken).ConfigureAwait(False) Then
                    Return updatedSolution
                End If

                Dim addedCompilationRoot = compilationRoot.AddImportsStatement(newImport, placeSystemNamespaceFirst, Formatter.Annotation, Simplifier.Annotation)
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(document.Id, addedCompilationRoot, PreservationMode.PreserveIdentity)
            End If

            Return updatedSolution
        End Function

        Private Function GetPropertyType(propIdentifierName As SimpleNameSyntax,
                                         semanticModel As SemanticModel,
                                         typeInference As ITypeInferenceService,
                                         cancellationToken As CancellationToken) As ITypeSymbol

            Dim fieldInitializer = TryCast(propIdentifierName.Parent, NamedFieldInitializerSyntax)
            If fieldInitializer IsNot Nothing Then
                Return typeInference.InferType(semanticModel, fieldInitializer.Name, True, cancellationToken)
            End If
            Return Nothing
        End Function

        Private Function GenerateProperty(propertyName As SimpleNameSyntax, typeSymbol As ITypeSymbol) As IPropertySymbol
            Return CodeGenerationSymbolFactory.CreatePropertySymbol(
                            attributes:=SpecializedCollections.EmptyList(Of AttributeData),
                            accessibility:=Accessibility.Public,
                            modifiers:=New DeclarationModifiers(),
                            explicitInterfaceSymbol:=Nothing,
                            name:=propertyName.ToString,
                            type:=typeSymbol,
                            parameters:=Nothing,
                            getMethod:=Nothing,
                            setMethod:=Nothing,
                            isIndexer:=False)
        End Function

        Friend Overrides Function TryGenerateProperty(propertyName As SimpleNameSyntax,
                                                      semanticModel As SemanticModel,
                                                      typeInferenceService As ITypeInferenceService,
                                                      cancellationToken As CancellationToken,
                                                      ByRef propertySymbol As IPropertySymbol) As Boolean
            propertySymbol = Nothing
            Dim typeSymbol = GetPropertyType(propertyName, semanticModel, typeInferenceService, cancellationToken)
            If typeSymbol Is Nothing OrElse TypeOf typeSymbol Is IErrorTypeSymbol Then
                propertySymbol = GenerateProperty(propertyName, semanticModel.Compilation.ObjectType)
                Return True
            End If

            propertySymbol = GenerateProperty(propertyName, typeSymbol)
            Return True
        End Function

        Friend Overrides Function GetDelegatingConstructor(document As SemanticDocument,
                                                           objectCreation As ObjectCreationExpressionSyntax,
                                                           namedType As INamedTypeSymbol,
                                                           candidates As ISet(Of IMethodSymbol),
                                                           cancellationToken As CancellationToken) As IMethodSymbol
            Dim model = document.SemanticModel
            Dim oldNode = objectCreation _
                .AncestorsAndSelf(ascendOutOfTrivia:=False) _
                .Where(Function(node) SpeculationAnalyzer.CanSpeculateOnNode(node)) _
                .LastOrDefault()

            Dim typeNameToReplace = objectCreation.Type
            Dim newTypeName = namedType.GenerateTypeSyntax()
            Dim newObjectCreation = objectCreation.WithType(newTypeName).WithAdditionalAnnotations(s_annotation)
            Dim newNode = oldNode.ReplaceNode(objectCreation, newObjectCreation)

            Dim speculativeModel = SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(oldNode, newNode, model)
            If speculativeModel IsNot Nothing Then
                newObjectCreation = DirectCast(newNode.GetAnnotatedNodes(s_annotation).Single(), ObjectCreationExpressionSyntax)
                Dim symbolInfo = speculativeModel.GetSymbolInfo(newObjectCreation, cancellationToken)
                Dim parameterTypes As IList(Of ITypeSymbol) = GetSpeculativeArgumentTypes(speculativeModel, newObjectCreation)
                Return GenerateConstructorHelpers.GetDelegatingConstructor(
                    document, symbolInfo, candidates, namedType, parameterTypes)
            End If

            Return Nothing
        End Function

        Private Shared Function GetSpeculativeArgumentTypes(model As SemanticModel, newObjectCreation As ObjectCreationExpressionSyntax) As IList(Of ITypeSymbol)
            Return If(newObjectCreation.ArgumentList Is Nothing,
                      SpecializedCollections.EmptyList(Of ITypeSymbol),
                      newObjectCreation.ArgumentList.Arguments.Select(
                          Function(a)
                              Return If(a.GetExpression() Is Nothing, Nothing, model.GetTypeInfo(a.GetExpression()).ConvertedType)
                          End Function).ToList())
        End Function
    End Class
End Namespace
