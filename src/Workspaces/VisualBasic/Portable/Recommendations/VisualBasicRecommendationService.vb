' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Recommendations
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Recommendations
    <ExportLanguageService(GetType(IRecommendationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRecommendationService
        Inherits AbstractRecommendationService

        Protected Overrides Async Function GetRecommendedSymbolsAtPositionWorkerAsync(
            workspace As Workspace,
            semanticModel As SemanticModel,
            position As Integer,
            options As OptionSet,
            cancellationToken As CancellationToken
        ) As Tasks.Task(Of Tuple(Of IEnumerable(Of ISymbol), AbstractSyntaxContext))

            Dim context = Await VisualBasicSyntaxContext.CreateContextAsync(workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)

            Dim filterOutOfScopeLocals = options.GetOption(RecommendationOptions.FilterOutOfScopeLocals, semanticModel.Language)
            Dim symbols = GetSymbolsWorker(context, filterOutOfScopeLocals, cancellationToken)

            Dim hideAdvancedMembers = options.GetOption(RecommendationOptions.HideAdvancedMembers, semanticModel.Language)
            symbols = symbols.FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, semanticModel.Compilation)

            Return Tuple.Create(Of IEnumerable(Of ISymbol), AbstractSyntaxContext)(symbols, context)
        End Function

        Private Function GetSymbolsWorker(
            context As VisualBasicSyntaxContext,
            filterOutOfScopeLocals As Boolean,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            If context.SyntaxTree.IsInNonUserCode(context.Position, cancellationToken) OrElse
               context.SyntaxTree.IsInSkippedText(context.Position, cancellationToken) Then
                Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
            End If

            Dim node = context.TargetToken.Parent
            If context.IsGlobalStatementContext Then
                Return GetSymbolsForGlobalStatementContext(context, cancellationToken)
            ElseIf context.IsRightOfNameSeparator Then
                If node.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                    Return GetSymbolsForMemberAccessExpression(context, DirectCast(node, MemberAccessExpressionSyntax), cancellationToken)
                ElseIf node.Kind = SyntaxKind.QualifiedName Then
                    Return GetSymbolsForQualifiedNameSyntax(context, DirectCast(node, QualifiedNameSyntax), cancellationToken)
                End If
            ElseIf context.SyntaxTree.IsQueryIntoClauseContext(context.Position, context.TargetToken, cancellationToken) Then
                Return GetUnqualifiedSymbolsForQueryIntoContext(context, cancellationToken)
            ElseIf context.IsAnyExpressionContext OrElse
                   context.IsSingleLineStatementContext OrElse
                   context.IsNameOfContext Then
                Return GetUnqualifiedSymbolsForExpressionOrStatementContext(context, filterOutOfScopeLocals, cancellationToken)
            ElseIf context.IsTypeContext OrElse context.IsNamespaceContext Then
                Return GetUnqualifiedSymbolsForType(context, cancellationToken)
            ElseIf context.SyntaxTree.IsLabelContext(context.Position, context.TargetToken, cancellationToken) Then
                Return GetUnqualifiedSymbolsForLabelContext(context, cancellationToken)
            ElseIf context.SyntaxTree.IsRaiseEventContext(context.Position, context.TargetToken, cancellationToken) Then
                Return GetUnqualifiedSymbolsForRaiseEvent(context, cancellationToken)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
        End Function

        Private Function GetSymbolsForGlobalStatementContext(
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)
            Return context.SemanticModel.LookupSymbols(context.TargetToken.Span.End)
        End Function

        Private Function GetUnqualifiedSymbolsForQueryIntoContext(
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            Dim symbols = context.SemanticModel _
                .LookupSymbols(context.TargetToken.SpanStart, includeReducedExtensionMethods:=True)

            Return symbols.OfType(Of IMethodSymbol)().Where(Function(m) m.IsAggregateFunction())
        End Function

        Private Function GetUnqualifiedSymbolsForLabelContext(
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            Return context.SemanticModel _
                .LookupLabels(context.TargetToken.SpanStart)
        End Function

        Private Function GetUnqualifiedSymbolsForRaiseEvent(
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            Dim containingType = context.SemanticModel.GetEnclosingSymbol(context.Position, cancellationToken).ContainingType

            Return context.SemanticModel _
                .LookupSymbols(context.Position, container:=containingType) _
                .Where(Function(s) s.Kind = SymbolKind.Event AndAlso s.ContainingType Is containingType)
        End Function

        Private Function GetUnqualifiedSymbolsForType(
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            Dim symbols = context.SemanticModel _
                .LookupNamespacesAndTypes(context.TargetToken.SpanStart)

            Return FilterToValidAccessibleSymbols(symbols, context, cancellationToken)
        End Function

        Private Function GetUnqualifiedSymbolsForExpressionOrStatementContext(
            context As VisualBasicSyntaxContext,
            filterOutOfScopeLocals As Boolean,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            Dim lookupPosition = context.TargetToken.SpanStart
            If context.FollowsEndOfStatement Then
                lookupPosition = context.Position
            End If

            Dim symbols As IEnumerable(Of ISymbol) = If(
                Not context.IsNameOfContext AndAlso context.TargetToken.Parent.IsInStaticContext(),
                context.SemanticModel.LookupStaticMembers(lookupPosition),
                context.SemanticModel.LookupSymbols(lookupPosition))

            If filterOutOfScopeLocals Then
                symbols = symbols.Where(Function(symbol) Not symbol.IsInaccessibleLocal(context.Position))
            End If

            ' GitHub #4428: When the user is typing a predicate (eg. "Enumerable.Range(0,10).Select($$")
            ' "Func(Of" tends to get in the way of typing "Function". Exclude System.Func from expression
            ' contexts, except within GetType
            If Not context.TargetToken.IsKind(SyntaxKind.OpenParenToken) OrElse
                    Not context.TargetToken.Parent.IsKind(SyntaxKind.GetTypeExpression) Then

                symbols = symbols.Where(Function(s) Not IsInEligibleDelegate(s))
            End If


            ' Hide backing fields and events
            Return symbols.Where(Function(s) FilterEventsAndGeneratedSymbols(Nothing, s))
        End Function

        Private Function IsInEligibleDelegate(s As ISymbol) As Boolean
            If s.IsDelegateType() Then
                Dim typeSymbol = DirectCast(s, ITypeSymbol)
                Return typeSymbol.SpecialType <> SpecialType.System_Delegate
            End If

            Return False
        End Function

        Private Function GetSymbolsForQualifiedNameSyntax(
            context As VisualBasicSyntaxContext,
            node As QualifiedNameSyntax,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            ' We shouldn't show completion if we're inside of a namespace statement.
            If context.TargetToken.Parent.FirstAncestorOrSelf(Of NamespaceStatementSyntax)() IsNot Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
            End If

            ' We're in a name-only context, since if we were an expression we'd be a
            ' MemberAccessExpressionSyntax. Thus, let's do other namespaces and types.
            Dim leftHandSymbolInfo = context.SemanticModel.GetSymbolInfo(node.Left, cancellationToken)
            Dim leftHandSymbol = TryCast(leftHandSymbolInfo.Symbol, INamespaceOrTypeSymbol)
            Dim couldBeMergedNamespace = ContainsNamespaceCandidateSymbols(leftHandSymbolInfo)

            If leftHandSymbol Is Nothing AndAlso Not couldBeMergedNamespace Then
                Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
            End If

            Dim symbols As IEnumerable(Of ISymbol)
            If couldBeMergedNamespace Then
                symbols = leftHandSymbolInfo.CandidateSymbols.OfType(Of INamespaceSymbol)() _
                    .SelectMany(Function(n) context.SemanticModel.LookupNamespacesAndTypes(node.SpanStart, n))
            Else
                symbols = context.SemanticModel _
                    .LookupNamespacesAndTypes(position:=node.SpanStart, container:=leftHandSymbol)
            End If

            Return FilterToValidAccessibleSymbols(symbols, context, cancellationToken)
        End Function

        Private Function GetSymbolsForMemberAccessExpression(
            context As VisualBasicSyntaxContext,
            node As MemberAccessExpressionSyntax,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            Dim leftExpression = node.GetExpressionOfMemberAccessExpression()
            If leftExpression Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
            End If

            Dim leftHandTypeInfo = context.SemanticModel.GetTypeInfo(leftExpression, cancellationToken)
            Dim leftHandSymbolInfo = context.SemanticModel.GetSymbolInfo(leftExpression, cancellationToken)

            Dim excludeInstance = False
            Dim excludeShared = True ' do not show shared members by default
            Dim useBaseReferenceAccessibility = False
            Dim inNameOfExpression = node.IsParentKind(SyntaxKind.NameOfExpression)

            Dim container = DirectCast(leftHandTypeInfo.Type, INamespaceOrTypeSymbol)
            If leftHandTypeInfo.Type.IsErrorType AndAlso leftHandSymbolInfo.Symbol IsNot Nothing Then
                ' TODO remove this when 531549 which causes leftHandTypeInfo to be an error type is fixed
                container = leftHandSymbolInfo.Symbol.GetSymbolType()
            End If

            Dim couldBeMergedNamespace = False

            If leftHandSymbolInfo.Symbol IsNot Nothing Then

                Dim firstSymbol = leftHandSymbolInfo.Symbol

                Select Case firstSymbol.Kind
                    Case SymbolKind.TypeParameter
                        ' 884060: We don't allow invocations off type parameters.
                        Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
                    Case SymbolKind.NamedType, SymbolKind.Namespace
                        excludeInstance = True
                        excludeShared = False
                        container = DirectCast(firstSymbol, INamespaceOrTypeSymbol)
                    Case SymbolKind.Alias
                        excludeInstance = True
                        excludeShared = False
                        container = DirectCast(firstSymbol, IAliasSymbol).Target
                    Case SymbolKind.Parameter
                        Dim parameter = DirectCast(firstSymbol, IParameterSymbol)

                        If parameter.IsMe Then
                            excludeShared = False
                        End If

                        ' case:
                        '    MyBase.
                        If parameter.IsMe AndAlso parameter.Type IsNot container Then
                            useBaseReferenceAccessibility = True
                        End If
                End Select

                ' Check for color color
                Dim speculativeTypeBinding = context.SemanticModel.GetSpeculativeTypeInfo(context.Position, leftExpression, SpeculativeBindingOption.BindAsTypeOrNamespace)
                Dim speculativeAliasBinding = context.SemanticModel.GetSpeculativeAliasInfo(context.Position, leftExpression, SpeculativeBindingOption.BindAsTypeOrNamespace)
                If TypeOf leftHandSymbolInfo.Symbol IsNot INamespaceOrTypeSymbol AndAlso speculativeAliasBinding Is Nothing AndAlso firstSymbol.GetSymbolType() Is speculativeTypeBinding.Type Then
                    excludeShared = False
                    excludeInstance = False
                End If

                If inNameOfExpression Then
                    excludeInstance = False
                End If

                If container Is Nothing OrElse container.IsType AndAlso DirectCast(container, ITypeSymbol).TypeKind = TypeKind.Enum Then
                    excludeShared = False ' need to allow shared members for enums
                End If

            Else
                couldBeMergedNamespace = ContainsNamespaceCandidateSymbols(leftHandSymbolInfo)
            End If

            If container Is Nothing AndAlso Not couldBeMergedNamespace Then
                Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
            End If

            Debug.Assert((Not excludeInstance OrElse Not excludeShared) OrElse
                         (inNameOfExpression AndAlso Not excludeInstance AndAlso Not excludeShared))

            Debug.Assert(Not excludeInstance OrElse Not useBaseReferenceAccessibility)

            If context.TargetToken.GetPreviousToken().IsKind(SyntaxKind.QuestionToken) Then
                Dim type = TryCast(container, INamedTypeSymbol)
                If type?.ConstructedFrom.SpecialType = SpecialType.System_Nullable_T Then
                    container = type.GetTypeArguments().First()
                End If
            End If

            ' No completion on types/namespace after conditional access
            If leftExpression.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) AndAlso
                (couldBeMergedNamespace OrElse leftHandSymbolInfo.GetBestOrAllSymbols().FirstOrDefault().MatchesKind(SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Alias)) Then
                Return SpecializedCollections.EmptyCollection(Of ISymbol)()
            End If

            Dim position = node.SpanStart
            Dim symbols As IEnumerable(Of ISymbol)
            If couldBeMergedNamespace Then
                symbols = leftHandSymbolInfo.CandidateSymbols _
                    .OfType(Of INamespaceSymbol) _
                    .SelectMany(Function(n) LookupSymbolsInContainer(n, context.SemanticModel, position, excludeInstance))
            Else
                symbols = If(
                    useBaseReferenceAccessibility,
                    context.SemanticModel.LookupBaseMembers(position),
                    LookupSymbolsInContainer(container, context.SemanticModel, position, excludeInstance)).AsEnumerable()
            End If

            If excludeShared Then
                symbols = symbols.Where(Function(s) Not s.IsShared)
            End If

            ' If the left expression is Me, MyBase or MyClass and we're the first statement of constructor,
            ' we should filter out the parenting constructor. Otherwise, we should filter out all constructors.
            If leftExpression.IsMeMyBaseOrMyClass() AndAlso node.IsFirstStatementInCtor() Then
                Dim parentingCtor = GetEnclosingCtor(context.SemanticModel, node, cancellationToken)
                Debug.Assert(parentingCtor IsNot Nothing)

                symbols = symbols.Where(Function(s) Not s.Equals(parentingCtor)).ToList()
            Else
                symbols = symbols.Where(Function(s) Not s.IsConstructor()).ToList()
            End If

            ' If the left expression is My.MyForms, we should filter out all non-property symbols
            If leftHandSymbolInfo.Symbol IsNot Nothing AndAlso
               leftHandSymbolInfo.Symbol.IsMyFormsProperty(context.SemanticModel.Compilation) Then

                symbols = symbols.Where(Function(s) s.Kind = SymbolKind.Property)
            End If

            ' Also filter out operators
            symbols = symbols.Where(Function(s) s.Kind <> SymbolKind.Method OrElse DirectCast(s, IMethodSymbol).MethodKind <> MethodKind.UserDefinedOperator)

            ' Filter events and generated members
            symbols = symbols.Where(Function(s) FilterEventsAndGeneratedSymbols(node, s))

            ' Never show the enum backing field
            symbols = symbols.Where(Function(s) s.Kind <> SymbolKind.Field OrElse Not s.ContainingType.IsEnumType() OrElse s.Name <> WellKnownMemberNames.EnumBackingFieldName)

            Return symbols
        End Function

        Private Shared Function ContainsNamespaceCandidateSymbols(symbolInfo As SymbolInfo) As Boolean
            Return symbolInfo.CandidateSymbols.Any() AndAlso symbolInfo.CandidateSymbols.All(Function(s) s.IsNamespace())
        End Function

        Private Function LookupSymbolsInContainer(container As INamespaceOrTypeSymbol, semanticModel As SemanticModel, position As Integer, excludeInstance As Boolean) As ImmutableArray(Of ISymbol)
            Return If(
                    excludeInstance,
                    semanticModel.LookupStaticMembers(position, container),
                    semanticModel.LookupSymbols(position, container, includeReducedExtensionMethods:=True))
        End Function

        ''' <summary>
        ''' In MemberAccessExpression Contexts, filter out event symbols, except inside AddRemoveHandler Statements
        ''' Also, filter out any implicitly declared members generated by event declaration or property declaration
        ''' </summary>
        Private Shared Function FilterEventsAndGeneratedSymbols(node As MemberAccessExpressionSyntax, s As ISymbol) As Boolean
            If s.Kind = SymbolKind.Event Then
                Return node IsNot Nothing AndAlso node.GetAncestor(Of AddRemoveHandlerStatementSyntax) IsNot Nothing
            ElseIf s.Kind = SymbolKind.Field AndAlso s.IsImplicitlyDeclared Then
                Dim associatedSymbol = DirectCast(s, IFieldSymbol).AssociatedSymbol
                If associatedSymbol IsNot Nothing Then
                    Return associatedSymbol.Kind <> SymbolKind.Event AndAlso associatedSymbol.Kind <> SymbolKind.Property
                End If
            ElseIf s.Kind = SymbolKind.NamedType AndAlso s.IsImplicitlyDeclared Then
                Return Not TypeOf DirectCast(s, INamedTypeSymbol).AssociatedSymbol Is IEventSymbol
            End If
            Return True
        End Function

        Private Shared Function GetEnclosingCtor(
            semanticModel As SemanticModel,
            node As MemberAccessExpressionSyntax,
            cancellationToken As CancellationToken
        ) As IMethodSymbol

            Dim symbol = semanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken)

            While symbol IsNot Nothing
                Dim method = TryCast(symbol, IMethodSymbol)
                If method IsNot Nothing AndAlso method.MethodKind = MethodKind.Constructor Then
                    Return method
                End If
            End While

            Return Nothing
        End Function

        Private Function FilterToValidAccessibleSymbols(
            symbols As IEnumerable(Of ISymbol),
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken
        ) As IEnumerable(Of ISymbol)

            ' If this is an Inherits or Implements statement, we filter out symbols which do not recursively contain accessible, valid types.
            Dim inheritsContext = IsInheritsStatementContext(context.TargetToken)
            Dim implementsContext = IsImplementsStatementContext(context.TargetToken)

            If inheritsContext OrElse implementsContext Then

                Dim typeBlock = context.TargetToken.Parent?.FirstAncestorOrSelf(Of TypeBlockSyntax)()
                If typeBlock IsNot Nothing Then
                    Dim typeOrAssemblySymbol As ISymbol = context.SemanticModel.GetDeclaredSymbol(typeBlock)
                    If typeOrAssemblySymbol Is Nothing Then
                        typeOrAssemblySymbol = context.SemanticModel.Compilation.Assembly
                    End If

                    Dim isInterface = TryCast(typeOrAssemblySymbol, ITypeSymbol)?.TypeKind = TypeKind.Interface

                    If inheritsContext Then

                        ' In an interface's Inherits statement, only show interfaces.
                        If isInterface Then
                            Return symbols.Where(Function(s) IsValidAccessibleInterfaceOrContainer(s, typeOrAssemblySymbol))
                        End If

                        Return symbols.Where(Function(s) IsValidAccessibleClassOrContainer(s, typeOrAssemblySymbol))

                    Else ' implementsContext

                        ' In an interface's Implements statement, show nothing.
                        If isInterface Then
                            Return SpecializedCollections.EmptyEnumerable(Of ISymbol)()
                        End If

                        Return symbols.Where(Function(s) IsValidAccessibleInterfaceOrContainer(s, typeOrAssemblySymbol))
                    End If
                End If
            End If

            Return symbols
        End Function

        Private Shared Function IsInheritsStatementContext(token As SyntaxToken) As Boolean
            If token.IsChildToken(Of InheritsStatementSyntax)(Function(n) n.InheritsKeyword) Then
                Return True
            End If

            Return token.IsChildToken(Of QualifiedNameSyntax)(Function(n) n.DotToken) AndAlso
                   token.Parent?.FirstAncestorOrSelf(Of InheritsStatementSyntax) IsNot Nothing
        End Function

        Private Shared Function IsImplementsStatementContext(token As SyntaxToken) As Boolean
            If token.IsChildToken(Of ImplementsStatementSyntax)(Function(n) n.ImplementsKeyword) Then
                Return True
            End If

            Return token.IsChildToken(Of QualifiedNameSyntax)(Function(n) n.DotToken) AndAlso
                   token.Parent?.FirstAncestorOrSelf(Of ImplementsStatementSyntax) IsNot Nothing
        End Function

        Private Function IsValidAccessibleInterfaceOrContainer(symbol As ISymbol, within As ISymbol) As Boolean
            If symbol.Kind = SymbolKind.Alias Then
                symbol = DirectCast(symbol, IAliasSymbol).Target
            End If

            Dim namespaceSymbol = TryCast(symbol, INamespaceSymbol)
            If namespaceSymbol IsNot Nothing Then
                Return namespaceSymbol.GetMembers().Any(Function(m) IsValidAccessibleInterfaceOrContainer(m, within))
            End If

            Dim namedTypeSymbol = TryCast(symbol, INamedTypeSymbol)
            If namedTypeSymbol Is Nothing Then
                Return False
            End If

            Return namedTypeSymbol.TypeKind = TypeKind.Interface OrElse
                   namedTypeSymbol _
                       .GetAccessibleMembersInThisAndBaseTypes(Of INamedTypeSymbol)(within) _
                       .Any(Function(m) IsOrContainsValidAccessibleInterface(m, within))
        End Function

        Private Function IsOrContainsValidAccessibleInterface(namespaceOrTypeSymbol As INamespaceOrTypeSymbol, within As ISymbol) As Boolean
            If namespaceOrTypeSymbol.Kind = SymbolKind.Namespace Then
                Return IsValidAccessibleInterfaceOrContainer(namespaceOrTypeSymbol, within)
            End If

            Dim namedTypeSymbol = TryCast(namespaceOrTypeSymbol, INamedTypeSymbol)
            If namedTypeSymbol Is Nothing Then
                Return False
            End If

            If namedTypeSymbol.TypeKind = TypeKind.Interface Then
                Return True
            End If

            Return namedTypeSymbol.GetMembers() _
                .OfType(Of INamedTypeSymbol)() _
                .Where(Function(m) m.IsAccessibleWithin(within)) _
                .Any(Function(m) IsOrContainsValidAccessibleInterface(m, within))
        End Function

        Private Function IsValidAccessibleClassOrContainer(symbol As ISymbol, within As ISymbol) As Boolean
            If symbol.Kind = SymbolKind.Alias Then
                symbol = DirectCast(symbol, IAliasSymbol).Target
            End If

            Dim type = TryCast(symbol, ITypeSymbol)

            If type IsNot Nothing Then
                If type.TypeKind = TypeKind.Class AndAlso Not type.IsSealed AndAlso type IsNot within Then
                    Return True
                End If

                If type.TypeKind = TypeKind.Class OrElse
                   type.TypeKind = TypeKind.Module OrElse
                   type.TypeKind = TypeKind.Struct Then

                    Return type.GetAccessibleMembersInThisAndBaseTypes(Of INamedTypeSymbol)(within).Any(Function(m) IsOrContainsValidAccessibleClass(m, within))
                End If
            End If

            Dim namespaceSymbol = TryCast(symbol, INamespaceSymbol)
            If namespaceSymbol IsNot Nothing Then
                Return namespaceSymbol.GetMembers().Any(Function(m) IsValidAccessibleClassOrContainer(m, within))
            End If

            Return False
        End Function

        Private Function IsOrContainsValidAccessibleClass(namespaceOrTypeSymbol As INamespaceOrTypeSymbol, within As ISymbol) As Boolean
            If namespaceOrTypeSymbol.Kind = SymbolKind.Namespace Then
                Return IsValidAccessibleClassOrContainer(namespaceOrTypeSymbol, within)
            End If

            Dim namedTypeSymbol = TryCast(namespaceOrTypeSymbol, INamedTypeSymbol)
            If namedTypeSymbol Is Nothing Then
                Return False
            End If

            If namedTypeSymbol.TypeKind = TypeKind.Class AndAlso Not namedTypeSymbol.IsSealed AndAlso namedTypeSymbol IsNot within Then
                Return True
            End If

            Return namedTypeSymbol.GetMembers() _
                .OfType(Of INamedTypeSymbol)() _
                .Where(Function(m) m.IsAccessibleWithin(within)) _
                .Any(Function(m) IsOrContainsValidAccessibleClass(m, within))
        End Function

    End Class
End Namespace
