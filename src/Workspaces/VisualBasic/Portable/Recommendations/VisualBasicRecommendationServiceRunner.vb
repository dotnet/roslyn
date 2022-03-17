' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Recommendations
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Recommendations
    Friend Class VisualBasicRecommendationServiceRunner
        Inherits AbstractRecommendationServiceRunner(Of VisualBasicSyntaxContext)

        Public Sub New(context As VisualBasicSyntaxContext, filterOutOfScopeLocals As Boolean, cancellationToken As CancellationToken)
            MyBase.New(context, filterOutOfScopeLocals, cancellationToken)
        End Sub

        Public Overrides Function GetRecommendedSymbols() As RecommendedSymbols
            Return New RecommendedSymbols(GetSymbols())
        End Function

        Private Overloads Function GetSymbols() As ImmutableArray(Of ISymbol)
            If _context.SyntaxTree.IsInNonUserCode(_context.Position, _cancellationToken) OrElse
               _context.SyntaxTree.IsInSkippedText(_context.Position, _cancellationToken) Then
                Return ImmutableArray(Of ISymbol).Empty
            End If

            Dim node = _context.TargetToken.Parent
            If _context.IsGlobalStatementContext Then
                Return GetSymbolsForGlobalStatementContext()
            ElseIf _context.IsRightOfNameSeparator Then
                If node.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                    Return GetSymbolsForMemberAccessExpression(DirectCast(node, MemberAccessExpressionSyntax))
                ElseIf node.Kind = SyntaxKind.QualifiedName Then
                    Return GetSymbolsForQualifiedNameSyntax(DirectCast(node, QualifiedNameSyntax))
                End If
            ElseIf _context.SyntaxTree.IsQueryIntoClauseContext(_context.Position, _context.TargetToken, _cancellationToken) Then
                Return GetUnqualifiedSymbolsForQueryIntoContext()
            ElseIf _context.IsAnyExpressionContext OrElse
                   _context.IsSingleLineStatementContext OrElse
                   _context.IsNameOfContext Then
                Return GetUnqualifiedSymbolsForExpressionOrStatementContext()
            ElseIf _context.IsTypeContext OrElse _context.IsNamespaceContext Then
                Return GetUnqualifiedSymbolsForType()
            ElseIf _context.SyntaxTree.IsLabelContext(_context.Position, _context.TargetToken, _cancellationToken) Then
                Return GetUnqualifiedSymbolsForLabelContext()
            ElseIf _context.SyntaxTree.IsRaiseEventContext(_context.Position, _context.TargetToken, _cancellationToken) Then
                Return GetUnqualifiedSymbolsForRaiseEvent()
            ElseIf _context.TargetToken.IsKind(SyntaxKind.ForKeyword) Then
                Dim symbols = GetUnqualifiedSymbolsForExpressionOrStatementContext().WhereAsArray(AddressOf IsWritableFieldOrLocal)
                Return symbols
            ElseIf _context.IsNamespaceDeclarationNameContext Then
                Return GetSymbolsForNamespaceDeclarationNameContext(Of NamespaceBlockSyntax)()
            End If

            Return ImmutableArray(Of ISymbol).Empty
        End Function

        Public Overrides Function TryGetExplicitTypeOfLambdaParameter(lambdaSyntax As SyntaxNode, ordinalInLambda As Integer, <NotNullWhen(True)> ByRef explicitLambdaParameterType As ITypeSymbol) As Boolean
            Dim lambdaExpressionSyntax = DirectCast(lambdaSyntax, LambdaExpressionSyntax)
            Dim parameters = lambdaExpressionSyntax.SubOrFunctionHeader.ParameterList.Parameters
            If parameters.Count > ordinalInLambda Then
                Dim parameterSyntax = parameters(ordinalInLambda)
                If parameterSyntax.AsClause IsNot Nothing Then
                    explicitLambdaParameterType = _context.SemanticModel.GetTypeInfo(parameterSyntax.AsClause.Type, _cancellationToken).Type
                    Return explicitLambdaParameterType IsNot Nothing
                End If
            End If

            Return False
        End Function

        Private Function IsWritableFieldOrLocal(symbol As ISymbol) As Boolean
            If symbol.Kind() = SymbolKind.Field Then
                Dim field = DirectCast(symbol, IFieldSymbol)
                Return Not field.IsReadOnly AndAlso Not field.IsConst
            End If

            If symbol.Kind() = SymbolKind.Local Then
                Dim local = DirectCast(symbol, ILocalSymbol)
                Return Not local.IsConst
            End If

            Return False
        End Function

        Private Function GetSymbolsForGlobalStatementContext() As ImmutableArray(Of ISymbol)
            Return _context.SemanticModel.LookupSymbols(_context.TargetToken.Span.End)
        End Function

        Private Function GetUnqualifiedSymbolsForQueryIntoContext() As ImmutableArray(Of ISymbol)
            Dim symbols = _context.SemanticModel _
                .LookupSymbols(_context.TargetToken.SpanStart, includeReducedExtensionMethods:=True)

            Return ImmutableArray(Of ISymbol).CastUp(
                symbols.OfType(Of IMethodSymbol)().
                        Where(Function(m) m.IsAggregateFunction()).
                        ToImmutableArray())
        End Function

        Private Function GetUnqualifiedSymbolsForLabelContext() As ImmutableArray(Of ISymbol)
            Return _context.SemanticModel.LookupLabels(_context.TargetToken.SpanStart)
        End Function

        Private Function GetUnqualifiedSymbolsForRaiseEvent() As ImmutableArray(Of ISymbol)
            Dim containingType = _context.SemanticModel.GetEnclosingSymbol(_context.Position, _cancellationToken).ContainingType

            Return _context.SemanticModel _
                .LookupSymbols(_context.Position, container:=containingType) _
                .WhereAsArray(Function(s) s.Kind = SymbolKind.Event AndAlso Equals(s.ContainingType, containingType))
        End Function

        Private Function GetUnqualifiedSymbolsForType() As ImmutableArray(Of ISymbol)
            Dim symbols = _context.SemanticModel.LookupNamespacesAndTypes(_context.TargetToken.SpanStart)
            Return FilterToValidAccessibleSymbols(symbols)
        End Function

        Private Function GetUnqualifiedSymbolsForExpressionOrStatementContext() As ImmutableArray(Of ISymbol)
            Dim lookupPosition = _context.TargetToken.SpanStart
            If _context.FollowsEndOfStatement Then
                lookupPosition = _context.Position
            End If

            Dim symbols = If(
                Not _context.IsNameOfContext AndAlso _context.TargetToken.Parent.IsInStaticContext(),
                _context.SemanticModel.LookupStaticMembers(lookupPosition),
                _context.SemanticModel.LookupSymbols(lookupPosition))

            If _filterOutOfScopeLocals Then
                symbols = symbols.WhereAsArray(Function(symbol) Not symbol.IsInaccessibleLocal(_context.Position))
            End If

            ' GitHub #4428: When the user is typing a predicate (eg. "Enumerable.Range(0,10).Select($$")
            ' "Func(Of" tends to get in the way of typing "Function". Exclude System.Func from expression
            ' contexts, except within GetType
            If Not _context.TargetToken.IsKind(SyntaxKind.OpenParenToken) OrElse
               Not _context.TargetToken.Parent.IsKind(SyntaxKind.GetTypeExpression) Then
                symbols = symbols.WhereAsArray(Function(s) Not IsInEligibleDelegate(s))
            End If

            ' Hide backing fields and events
            Return symbols.WhereAsArray(Function(s) FilterEventsAndGeneratedSymbols(Nothing, s))
        End Function

        Private Shared Function IsInEligibleDelegate(s As ISymbol) As Boolean
            If s.IsDelegateType() Then
                Dim typeSymbol = DirectCast(s, ITypeSymbol)
                Return typeSymbol.SpecialType <> SpecialType.System_Delegate
            End If

            Return False
        End Function

        Private Function GetSymbolsForQualifiedNameSyntax(node As QualifiedNameSyntax) As ImmutableArray(Of ISymbol)
            ' We're in a name-only context, since if we were an expression we'd be a
            ' MemberAccessExpressionSyntax. Thus, let's do other namespaces and types.
            Dim leftHandSymbolInfo = _context.SemanticModel.GetSymbolInfo(node.Left, _cancellationToken)
            Dim leftHandSymbol = TryCast(leftHandSymbolInfo.Symbol, INamespaceOrTypeSymbol)
            Dim couldBeMergedNamespace = ContainsNamespaceCandidateSymbols(leftHandSymbolInfo)

            If leftHandSymbol Is Nothing AndAlso Not couldBeMergedNamespace Then
                Return ImmutableArray(Of ISymbol).Empty
            End If

            Dim symbols As ImmutableArray(Of ISymbol)
            If couldBeMergedNamespace Then
                symbols = leftHandSymbolInfo.CandidateSymbols.OfType(Of INamespaceSymbol)() _
                    .SelectMany(Function(n) _context.SemanticModel.LookupNamespacesAndTypes(node.SpanStart, n)) _
                    .ToImmutableArray()
            Else
                symbols = _context.SemanticModel _
                    .LookupNamespacesAndTypes(position:=node.SpanStart, container:=leftHandSymbol)

                If _context.IsNamespaceDeclarationNameContext Then
                    Dim declarationSyntax = node.GetAncestor(Of NamespaceBlockSyntax)
                    symbols = symbols.WhereAsArray(Function(symbol) IsNonIntersectingNamespace(symbol, declarationSyntax))
                End If
            End If

            Return FilterToValidAccessibleSymbols(symbols)
        End Function

        Private Function GetSymbolsForMemberAccessExpression(node As MemberAccessExpressionSyntax) As ImmutableArray(Of ISymbol)
            Dim leftExpression = node.GetExpressionOfMemberAccessExpression(allowImplicitTarget:=True)
            If leftExpression Is Nothing Then
                Return ImmutableArray(Of ISymbol).Empty
            End If

            Dim leftHandTypeInfo = _context.SemanticModel.GetTypeInfo(leftExpression, _cancellationToken)
            Dim leftHandSymbolInfo = _context.SemanticModel.GetSymbolInfo(leftExpression, _cancellationToken)

            ' https://github.com/dotnet/roslyn/issues/9087: Try to speculatively bind a type as an expression for My namespace
            ' We'll get a type contained in the My Namespace if this is successful
            If leftHandTypeInfo.Type IsNot Nothing AndAlso leftHandTypeInfo.Type.Equals(leftHandSymbolInfo.Symbol) Then
                Dim leftHandSpeculativeBinding = _context.SemanticModel.GetSpeculativeSymbolInfo(_context.Position, leftExpression, SpeculativeBindingOption.BindAsExpression)
                If leftHandSpeculativeBinding.Symbol IsNot Nothing AndAlso
                    leftHandSpeculativeBinding.Symbol.ContainingNamespace?.IsMyNamespace(_context.SemanticModel.Compilation) Then
                    leftHandSymbolInfo = leftHandSpeculativeBinding
                End If
            End If

            Dim excludeInstance = False
            Dim excludeShared = True ' do not show shared members by default
            Dim useBaseReferenceAccessibility = False
            Dim inNameOfExpression = node.IsParentKind(SyntaxKind.NameOfExpression)

            Dim container As ISymbol = leftHandTypeInfo.Type
            If container Is Nothing AndAlso TypeOf (leftHandTypeInfo.ConvertedType) Is IArrayTypeSymbol Then
                container = leftHandTypeInfo.ConvertedType
            End If

            If container.IsErrorType() AndAlso leftHandSymbolInfo.Symbol IsNot Nothing Then
                ' TODO remove this when 531549 which causes leftHandTypeInfo to be an error type is fixed
                container = leftHandSymbolInfo.Symbol.GetSymbolType()
            End If

            Dim couldBeMergedNamespace = False

            If leftHandSymbolInfo.Symbol IsNot Nothing Then
                Dim firstSymbol = leftHandSymbolInfo.Symbol

                If firstSymbol.Kind = SymbolKind.Alias Then
                    firstSymbol = DirectCast(firstSymbol, IAliasSymbol).Target
                End If

                Select Case firstSymbol.Kind
                    Case SymbolKind.TypeParameter
                        ' 884060: We don't allow invocations off type parameters.
                        Return ImmutableArray(Of ISymbol).Empty
                    Case SymbolKind.NamedType, SymbolKind.Namespace
                        excludeInstance = True
                        excludeShared = False
                        container = DirectCast(firstSymbol, INamespaceOrTypeSymbol)
                End Select

                If firstSymbol.Kind = SymbolKind.Parameter Then
                    Dim parameter = DirectCast(firstSymbol, IParameterSymbol)

                    If parameter.IsMe Then
                        excludeShared = False
                        ' case:
                        '    MyBase.
                        useBaseReferenceAccessibility = Not parameter.Type.Equals(container)
                    End If

                    container = parameter
                End If

                ' Check for color color
                Dim speculativeTypeBinding = _context.SemanticModel.GetSpeculativeTypeInfo(_context.Position, leftExpression, SpeculativeBindingOption.BindAsTypeOrNamespace)
                Dim speculativeAliasBinding = _context.SemanticModel.GetSpeculativeAliasInfo(_context.Position, leftExpression, SpeculativeBindingOption.BindAsTypeOrNamespace)
                If TypeOf leftHandSymbolInfo.Symbol IsNot INamespaceOrTypeSymbol AndAlso speculativeAliasBinding Is Nothing AndAlso Equals(firstSymbol.GetSymbolType(), speculativeTypeBinding.Type) Then
                    excludeShared = False
                    excludeInstance = False
                End If

                If inNameOfExpression Then
                    excludeInstance = False
                End If

                If container Is Nothing OrElse TryCast(container, ITypeSymbol)?.TypeKind = TypeKind.Enum Then
                    excludeShared = False ' need to allow shared members for enums
                End If
            Else
                couldBeMergedNamespace = ContainsNamespaceCandidateSymbols(leftHandSymbolInfo)
            End If

            If container Is Nothing AndAlso Not couldBeMergedNamespace Then
                Return ImmutableArray(Of ISymbol).Empty
            End If

            Debug.Assert((Not excludeInstance OrElse Not excludeShared) OrElse
                         (inNameOfExpression AndAlso Not excludeInstance AndAlso Not excludeShared))

            Debug.Assert(Not excludeInstance OrElse Not useBaseReferenceAccessibility)

            ' On null conditional access, members of T for a Nullable(Of T) should be recommended
            Dim unwrapNullable = _context.TargetToken.GetPreviousToken().IsKind(SyntaxKind.QuestionToken)

            ' No completion on types/namespace after conditional access
            If leftExpression.Parent.IsKind(SyntaxKind.ConditionalAccessExpression) AndAlso
                (couldBeMergedNamespace OrElse leftHandSymbolInfo.GetBestOrAllSymbols().FirstOrDefault().MatchesKind(SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Alias)) Then
                Return ImmutableArray(Of ISymbol).Empty
            End If

            Dim position = node.SpanStart
            Dim symbols As ImmutableArray(Of ISymbol)
            If couldBeMergedNamespace Then
                symbols = leftHandSymbolInfo.CandidateSymbols _
                    .OfType(Of INamespaceSymbol) _
                    .SelectMany(Function(n) LookupSymbolsInContainer(n, position, excludeInstance)) _
                    .ToImmutableArray()
            Else
                symbols = GetMemberSymbols(container, position, excludeInstance, useBaseReferenceAccessibility, unwrapNullable, isForDereference:=False)
            End If

            If excludeShared Then
                symbols = symbols.WhereAsArray(Function(s) Not s.IsShared)
            End If

            ' If the left expression is Me, MyBase or MyClass and we're the first statement of constructor,
            ' we should filter out the parenting constructor. Otherwise, we should filter out all constructors.
            If leftExpression.IsMeMyBaseOrMyClass() AndAlso node.IsFirstStatementInCtor() Then
                Dim parentingCtor = GetEnclosingCtor(node)
                Debug.Assert(parentingCtor IsNot Nothing)

                symbols = symbols.WhereAsArray(Function(s) Not s.Equals(parentingCtor))
            Else
                symbols = symbols.WhereAsArray(Function(s) Not s.IsConstructor())
            End If

            ' If the left expression is My.MyForms, we should filter out all non-property symbols
            If leftHandSymbolInfo.Symbol IsNot Nothing AndAlso
               leftHandSymbolInfo.Symbol.IsMyFormsProperty(_context.SemanticModel.Compilation) Then

                symbols = symbols.WhereAsArray(Function(s) s.Kind = SymbolKind.Property)
            End If

            ' Also filter out operators
            symbols = symbols.WhereAsArray(Function(s) s.Kind <> SymbolKind.Method OrElse DirectCast(s, IMethodSymbol).MethodKind <> MethodKind.UserDefinedOperator)

            ' Filter events and generated members
            symbols = symbols.WhereAsArray(Function(s) FilterEventsAndGeneratedSymbols(node, s))

            ' Never show the enum backing field
            symbols = symbols.WhereAsArray(Function(s) s.Kind <> SymbolKind.Field OrElse Not s.ContainingType.IsEnumType() OrElse s.Name <> WellKnownMemberNames.EnumBackingFieldName)

            Return symbols
        End Function

        Private Shared Function ContainsNamespaceCandidateSymbols(symbolInfo As SymbolInfo) As Boolean
            Return symbolInfo.CandidateSymbols.Any() AndAlso symbolInfo.CandidateSymbols.All(Function(s) s.IsNamespace())
        End Function

        ''' <summary>
        ''' In MemberAccessExpression Contexts, filter out event symbols (except for NameOf context), except inside AddRemoveHandler Statements
        ''' Also, filter out any implicitly declared members generated by event declaration or property declaration
        ''' </summary>
        Private Function FilterEventsAndGeneratedSymbols(node As MemberAccessExpressionSyntax, s As ISymbol) As Boolean
            If s.Kind = SymbolKind.Event AndAlso Not _context.IsNameOfContext Then
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

        Private Function GetEnclosingCtor(node As MemberAccessExpressionSyntax) As IMethodSymbol
            Dim symbol = _context.SemanticModel.GetEnclosingSymbol(node.SpanStart, _cancellationToken)

            While symbol IsNot Nothing
                Dim method = TryCast(symbol, IMethodSymbol)
                If method IsNot Nothing AndAlso method.MethodKind = MethodKind.Constructor Then
                    Return method
                End If
            End While

            Return Nothing
        End Function

        Private Function FilterToValidAccessibleSymbols(symbols As ImmutableArray(Of ISymbol)) As ImmutableArray(Of ISymbol)
            ' If this is an Inherits or Implements statement, we filter out symbols which do not recursively contain accessible, valid types.
            Dim inheritsContext = IsInheritsStatementContext(_context.TargetToken)
            Dim implementsContext = IsImplementsStatementContext(_context.TargetToken)

            If inheritsContext OrElse implementsContext Then

                Dim typeBlock = _context.TargetToken.Parent?.FirstAncestorOrSelf(Of TypeBlockSyntax)()
                If typeBlock IsNot Nothing Then
                    Dim typeOrAssemblySymbol As ISymbol = _context.SemanticModel.GetDeclaredSymbol(typeBlock, _cancellationToken)
                    If typeOrAssemblySymbol Is Nothing Then
                        typeOrAssemblySymbol = _context.SemanticModel.Compilation.Assembly
                    End If

                    Dim isInterface = TryCast(typeOrAssemblySymbol, ITypeSymbol)?.TypeKind = TypeKind.Interface

                    If inheritsContext Then

                        ' In an interface's Inherits statement, only show interfaces.
                        If isInterface Then
                            Return symbols.WhereAsArray(Function(s) IsValidAccessibleInterfaceOrContainer(s, typeOrAssemblySymbol))
                        End If

                        Return symbols.WhereAsArray(Function(s) IsValidAccessibleClassOrContainer(s, typeOrAssemblySymbol))

                    Else ' implementsContext

                        ' In an interface's Implements statement, show nothing.
                        If isInterface Then
                            Return ImmutableArray(Of ISymbol).Empty
                        End If

                        Return symbols.WhereAsArray(Function(s) IsValidAccessibleInterfaceOrContainer(s, typeOrAssemblySymbol))
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
                If type.TypeKind = TypeKind.Class AndAlso Not type.IsSealed AndAlso Not Equals(type, within) Then
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

            If namedTypeSymbol.TypeKind = TypeKind.Class AndAlso Not namedTypeSymbol.IsSealed AndAlso Not Equals(namedTypeSymbol, within) Then
                Return True
            End If

            Return namedTypeSymbol.GetMembers() _
                .OfType(Of INamedTypeSymbol)() _
                .Where(Function(m) m.IsAccessibleWithin(within)) _
                .Any(Function(m) IsOrContainsValidAccessibleClass(m, within))
        End Function
    End Class
End Namespace
