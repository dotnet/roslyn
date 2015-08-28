' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#If False Then
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Shared.Extensions
Imports Roslyn.Services.VisualBasic.Extensions
Imports Roslyn.Utilities

Namespace Roslyn.Services.VisualBasic.RemoveUnnecessaryImports
    Partial Friend Class VisualBasicRemoveUnnecessaryImportsService

        ''' <summary>
        ''' TODO(cyrusn): 
        ''' 
        ''' 1) Handle queries.  They may bind to extension methods and should cause extensions to be
        '''    considered used.
        ''' 2) Currently we will mark a using as used if an identifier binds to a member of that
        '''     namespace that the using pulls.  This is valid in most cases except for when we're also
        '''     *inside* that namespace.  i.e. "using Foo; namespace Foo { ... }".  In this case we do
        '''     want to remove the "using Foo;" as it's not actually removed. 
        ''' 3) Handle extern aliases.  We want to remove them as well if they're not used.
        ''' 4) Async/Await/GetAwaiter.
        ''' 5) Extension methods in foreach.  (Check on this.)
        ''' 6) Extension methods in Collection initializers.  (Check on this.)
        ''' 7) Dispose method in using.  (Check on this.)
        ''' </summary>
        Private Class Visitor
            Inherits SyntaxVisitor

            Private ReadOnly semanticModel As SemanticModel
            Private ReadOnly cancellationToken As CancellationToken
            Private ReadOnly unnecessaryImports As HashSet(Of ImportsClauseSyntax)

            ' These stacks model information about the usings respective to their nesting in the
            ' file.  i.e. the usings for the CompilationUnit are at the bottom of the stack, and as
            ' we visit deeper into namespaces their using info gets pushed on these stacks.  In
            ' practice these stacks will likely always be 1 level deep, and will rarely get to 2
            ' levels or beyond. 
            '
            ' The stacks contain dictionaries who are keyed by the names that the using directives
            ' have now made available.  i.e. when you have "using System" it makes names like
            ' "DateTime" available for the code. Likewise, "using System.Linq" makes the extension
            ' method names "Select, Where, etc." available. These dictionaries can then be used to
            ' quickly ask "should we bother even binding this identifier" as there is no point if
            ' there are no usings that would have even pulled in that identifier. The dictionary
            ' maps from type names to the using directives that pulled those type names in.  In
            ' practice these will usually be 1:1.  However, occasionally you will have things like {
            ' Timer -> [ System.Timers; System.Windows.Forms; System.Threading ] }.  
            '
            ' When we bind something like "Timer" and we get a symbol back, we will then see what
            ' namespace that symbol came from.  If it came from a namespace brought in by one of
            ' those using directives, then we then consider that directive 'used'.  
            Private ReadOnly typeNameToClauses As Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax)) = New Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax))()
            Private ReadOnly namespaceNameToClauses As Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax)) = New Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax))()
            Private ReadOnly typeMemberNameToClauses As Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax)) = New Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax))()
            Private ReadOnly extensionNameToClauses As Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax)) = New Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax))()
            Private ReadOnly aliasNameToClauses As Dictionary(Of String, HashSet(Of AliasImportsClauseSyntax)) = New Dictionary(Of String, HashSet(Of AliasImportsClauseSyntax))()

            Private Shared ReadOnly CreateMembersSet As Func(Of String, HashSet(Of MembersImportsClauseSyntax)) = Function(s) New HashSet(Of MembersImportsClauseSyntax)()
            Private Shared ReadOnly CreateAliasSet As Func(Of String, HashSet(Of AliasImportsClauseSyntax)) = Function(s) New HashSet(Of AliasImportsClauseSyntax)()

            Public Sub New(semanticModel As SemanticModel,
                           possiblyUnnecessaryImports As HashSet(Of ImportsClauseSyntax),
                           cancellationToken As CancellationToken)
                Me.semanticModel = semanticModel
                Me.unnecessaryImports = possiblyUnnecessaryImports
                Me.cancellationToken = cancellationToken
            End Sub

            Private Sub PopulateDictionaries(compilationUnit As CompilationUnitSyntax)
                For Each statement In compilationUnit.Imports
                    For Each clause In statement.ImportsClauses
                        cancellationToken.ThrowIfCancellationRequested()

                        If TypeOf clause Is MembersImportsClauseSyntax Then
                            HandleMemberImportsClause(DirectCast(clause, MembersImportsClauseSyntax))
                        ElseIf TypeOf clause Is AliasImportsClauseSyntax Then
                            HandleAliasImportsClause(DirectCast(clause, AliasImportsClauseSyntax))
                        End If
                    Next
                Next
            End Sub

            Private Sub HandleMemberImportsClause(clause As MembersImportsClauseSyntax)
                Dim semanticInfo = semanticModel.GetSymbolInfo(clause.Name, cancellationToken)

                Dim namespaceOrType = TryCast(semanticInfo.Symbol, NamespaceOrTypeSymbol)
                If namespaceOrType Is Nothing Then
                    Return
                End If

                If namespaceOrType.Kind = SymbolKind.NamedType Then
                    ' If they're importing a type, then that brings nested types, members and
                    ' extension methods into scope.
                    For Each childSymbol In namespaceOrType.GetMembers()
                        cancellationToken.ThrowIfCancellationRequested()

                        If childSymbol.Kind = SymbolKind.NamedType Then
                            typeNameToClauses.GetOrAdd(childSymbol.Name, CreateMembersSet).Add(clause)
                        Else
                            typeMemberNameToClauses.GetOrAdd(childSymbol.Name, CreateMembersSet).Add(clause)

                            If childSymbol.IsExtensionMethod() Then
                                extensionNameToClauses.GetOrAdd(childSymbol.Name, CreateMembersSet).Add(clause)
                            End If
                        End If
                    Next
                Else
                    ' If they're importing a namespace, then that brings nested types, nested
                    ' namespaces, module members, and extension methods into scope.
                    For Each childSymbol In namespaceOrType.GetMembers()
                        cancellationToken.ThrowIfCancellationRequested()

                        If childSymbol.Kind = SymbolKind.Namespace Then
                            namespaceNameToClauses.GetOrAdd(childSymbol.Name, CreateMembersSet).Add(clause)
                        ElseIf childSymbol.Kind = SymbolKind.NamedType Then
                            typeNameToClauses.GetOrAdd(childSymbol.Name, CreateMembersSet).Add(clause)

                            Dim attributeName As String = Nothing
                            If childSymbol.Name.TryGetWithoutAttributeSuffix(False, attributeName) Then
                                typeNameToClauses.GetOrAdd(attributeName, CreateMembersSet).Add(clause)
                            End If

                            Dim typeSymbol = DirectCast(childSymbol, NamedTypeSymbol)

                            If typeSymbol.TypeKind = TypeKind.Module Then
                                For Each typeMember In typeSymbol.GetMembers()
                                    cancellationToken.ThrowIfCancellationRequested()
                                    typeMemberNameToClauses.GetOrAdd(typeMember.Name, CreateMembersSet).Add(clause)

                                    If typeMember.IsExtensionMethod() Then
                                        extensionNameToClauses.GetOrAdd(typeMember.Name, CreateMembersSet).Add(clause)
                                    End If
                                Next
                            End If

                            If typeSymbol.TypeKind = TypeKind.Class AndAlso typeSymbol.IsShared Then
                                For Each typeMember In typeSymbol.GetMembers()
                                    cancellationToken.ThrowIfCancellationRequested()
                                    If typeMember.IsExtensionMethod() Then
                                        extensionNameToClauses.GetOrAdd(typeMember.Name, CreateMembersSet).Add(clause)
                                    End If
                                Next
                            End If
                        End If
                    Next
                End If
            End Sub

            Private Sub HandleAliasImportsClause(clause As AliasImportsClauseSyntax)
                Dim aliasName = clause.Alias.ValueText
                If aliasNameToClauses.ContainsKey(aliasName) Then
                    unnecessaryImports.Remove(clause)
                Else
                    aliasNameToClauses.GetOrAdd(aliasName, CreateAliasSet).Add(clause)
                End If
            End Sub

            Public Overrides Sub DefaultVisit(node As SyntaxNode)
                cancellationToken.ThrowIfCancellationRequested()
                If unnecessaryImports.Count = 0 Then
                    Return
                End If

                For Each child In node.ChildNodesAndTokens()
                    If child.IsNode Then
                        Visit(child.AsNode())
                    End If
                Next
            End Sub

            Public Overrides Sub VisitCompilationUnit(node As CompilationUnitSyntax)
                PopulateDictionaries(node)
                MyBase.VisitCompilationUnit(node)
            End Sub

            Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
                VisitSimpleName(node)
            End Sub

            Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
                MyBase.VisitGenericName(node)
                VisitSimpleName(node)
            End Sub

            Private Sub VisitSimpleName(node As SimpleNameSyntax)
                cancellationToken.ThrowIfCancellationRequested()
                Dim result =
                    TryCheckTypeName(node) OrElse
                    TryCheckNamespaceName(node) OrElse
                    TryCheckAliasName(node) OrElse
                    TryCheckExtensionName(node) OrElse
                    TryCheckTypeMemberName(node)
            End Sub

            Private Function TryCheckTypeName(node As SimpleNameSyntax) As Boolean
                Dim result = False
                If ShouldCheck(typeNameToClauses, node.Identifier.ValueText) AndAlso
                   Not node.IsRightSideOfDotOrBang() Then
                    ' Bind the node to a symbol.  If it binds to a relevant symbol mark the appropriate
                    ' using directive as necessary.
                    Dim semanticInfo = semanticModel.GetSymbolInfo(node)
                    For Each symbol In semanticInfo.GetBestOrAllSymbols()
                        If symbol.IsConstructor() Then
                            symbol = symbol.ContainingType
                        End If

                        If symbol.Kind = SymbolKind.NamedType Then
                            result = result OrElse MarkNecessaryNamespaceClause(symbol, typeNameToClauses)
                            result = result OrElse MarkNecessaryTypeClause(symbol, typeNameToClauses)
                        End If
                    Next
                End If

                Return result
            End Function

            Private Function TryCheckNamespaceName(node As SimpleNameSyntax) As Boolean
                Dim result = False
                If ShouldCheck(namespaceNameToClauses, node.Identifier.ValueText) AndAlso
                   Not node.IsRightSideOfDotOrBang() Then
                    ' Bind the node to a symbol.  If it binds to a relevant symbol mark the appropriate
                    ' using directive as necessary.
                    Dim semanticInfo = semanticModel.GetSymbolInfo(node)
                    For Each symbol In semanticInfo.GetBestOrAllSymbols()
                        If symbol.Kind = SymbolKind.Namespace Then
                            result = result OrElse MarkNecessaryNamespaceClause(symbol, namespaceNameToClauses)
                        End If
                    Next
                End If

                Return result
            End Function

            Private Function TryCheckAliasName(node As SimpleNameSyntax) As Boolean
                Dim result = False
                If ShouldCheck(aliasNameToClauses, node.Identifier.ValueText) AndAlso
                   Not node.IsRightSideOfDotOrBang() Then

                    ' Bind the node to a symbol.  If it binds to a relevant symbol mark the appropriate
                    ' using directive as necessary.
                    Dim semanticInfo = semanticModel.GetSymbolInfo(node)
                    For Each symbol In semanticInfo.GetBestOrAllSymbols().Concat(semanticModel.GetAliasInfo(node)).WhereNotNull()
                        If symbol.Kind = SymbolKind.Alias Then

                            Dim clauses = GetClauses(aliasNameToClauses, symbol.Name)
                            result = result OrElse unnecessaryImports.Remove(clauses.FirstOrDefault())
                        End If
                    Next
                End If

                Return result
            End Function

            Private Function TryCheckExtensionName(node As SimpleNameSyntax) As Boolean
                Dim result = False
                If ShouldCheck(extensionNameToClauses, node.Identifier.ValueText) Then
                    ' Bind the node to a symbol.  If it binds to a relevant symbol mark the appropriate
                    ' using directive as necessary.
                    Dim semanticInfo = semanticModel.GetSymbolInfo(node)
                    For Each symbol In semanticInfo.GetBestOrAllSymbols()
                        If symbol.IsReducedExtension() Then
                            result = result OrElse MarkNecessaryTypeClause(symbol, extensionNameToClauses)
                            result = result OrElse MarkNecessaryNamespaceClause(symbol, extensionNameToClauses)
                        End If
                    Next
                End If

                Return result
            End Function

            Private Function TryCheckTypeMemberName(node As SimpleNameSyntax) As Boolean
                Dim result = False
                If ShouldCheck(typeMemberNameToClauses, node.Identifier.ValueText) AndAlso
                   Not node.IsRightSideOfDotOrBang() Then
                    ' Bind the node to a symbol.  If it binds to a relevant symbol mark the appropriate
                    ' using directive as necessary.
                    Dim semanticInfo = semanticModel.GetSymbolInfo(node)
                    For Each symbol In semanticInfo.GetBestOrAllSymbols()
                        result = result OrElse MarkNecessaryTypeClause(symbol, typeMemberNameToClauses)
                        result = result OrElse MarkNecessaryNamespaceClause(symbol, typeMemberNameToClauses)
                    Next
                End If

                Return result
            End Function

            Private Function GetClauses(Of TImportsClause As ImportsClauseSyntax)(dict As Dictionary(Of String, HashSet(Of TImportsClause)),
                                        name As String) As IEnumerable(Of TImportsClause)
                Return If(dict.ContainsKey(name), dict(name), SpecializedCollections.EmptyEnumerable(Of TImportsClause))
            End Function

            Private Function MarkNecessaryTypeClause(symbol As ISymbol, dict As Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax))) As Boolean
                Dim containingType = symbol.ContainingType

                If containingType IsNot Nothing Then
                    ' Find the first directive associated with the name of this symbol that also binds
                    ' to the containing namespace of this symbol.  We now consider that directive used.
                    Dim possibleUsings = Me.GetClauses(dict, symbol.Name)

                    Dim usingDirective = possibleUsings.FirstOrDefault(
                        Function(u) containingType.Equals(semanticModel.GetSymbolInfo(u.Name, cancellationToken).Symbol))
                    Return unnecessaryImports.Remove(usingDirective)
                End If

                Return False
            End Function

            Private Function MarkNecessaryNamespaceClause(symbol As ISymbol, dict As Dictionary(Of String, HashSet(Of MembersImportsClauseSyntax))) As Boolean
                ' The containing namespace for a type will be a ModuleNamespaceSymbol.  However, we
                ' want the CompilationNamespaceSymbol as that's what the using directive will bind
                ' to.
                Dim compilation = semanticModel.Compilation
                Dim namespaceSymbol = compilation.GetCompilationNamespace(symbol.ContainingNamespace)
                ' Find the first directive associated with the name of this symbol that also binds
                ' to the containing namespace of this symbol.  We now consider that directive used.
                Dim possibleUsings = GetClauses(dict, symbol.Name)
                Dim nameWithoutAttribute As String = Nothing
                If symbol.Name.TryGetWithoutAttributeSuffix(nameWithoutAttribute) Then
                    possibleUsings = possibleUsings.Concat(GetClauses(dict, nameWithoutAttribute))
                End If

                Dim usingDirective = possibleUsings.FirstOrDefault(Function(u) namespaceSymbol.Equals(semanticModel.GetSymbolInfo(u.Name, cancellationToken).Symbol))
                Return unnecessaryImports.Remove(usingDirective)
            End Function

            ''' <summary>
            ''' There is no point binding to a node with name 'N' if we already think the imports
            ''' that brought in 'N' are 'used'.  We should only bind the names for things that are
            ''' coming from imports that are still potentially unused that we want to know more
            ''' about.
            ''' </summary>
            Private Function ShouldCheck(Of TImportsClause As ImportsClauseSyntax)(dict As Dictionary(Of String, HashSet(Of TImportsClause)),
                                                                                   plainName As String) As Boolean
                Dim clauses As HashSet(Of TImportsClause) = Nothing
                If dict.TryGetValue(plainName, clauses) Then
                    For Each clause In clauses
                        If unnecessaryImports.Contains(clause) Then
                            Return True
                        End If
                    Next
                End If

                Return False
            End Function

            Public Overrides Sub VisitFromClause(node As Compilers.VisualBasic.FromClauseSyntax)
                MyBase.VisitFromClause(node)
                VisitQueryClause(node, "Select", "SelectMany")
            End Sub

            Public Overrides Sub VisitSelectClause(node As Compilers.VisualBasic.SelectClauseSyntax)
                MyBase.VisitSelectClause(node)
                VisitQueryClause(node, "Select", "SelectMany")
            End Sub

            Public Overrides Sub VisitFunctionAggregation(node As Compilers.VisualBasic.FunctionAggregationSyntax)
                MyBase.VisitFunctionAggregation(node)

                Dim model = DirectCast(Me.semanticModel, SemanticModel)
                Dim symbolInfo = model.GetSymbolInfo(node, cancellationToken)
                ProcessExtensionMethodSymbolInfo(symbolInfo)
            End Sub

            Public Overrides Sub VisitAggregateClause(node As Compilers.VisualBasic.AggregateClauseSyntax)
                MyBase.VisitAggregateClause(node)

                If ShouldCheck(extensionNameToClauses, "Aggregate") Then
                    ' Bind the node to a symbol.  If it binds to a relevant symbol mark the
                    ' appropriate using directive as necessary.
                    Dim model = DirectCast(Me.semanticModel, SemanticModel)
                    Dim symbolInfo = model.GetAggregateClauseSymbolInfo(node, cancellationToken)
                    ProcessExtensionMethodSymbolInfo(symbolInfo.Select1)
                    ProcessExtensionMethodSymbolInfo(symbolInfo.Select2)
                End If
            End Sub

            Private Sub VisitQueryClause(node As QueryClauseSyntax, ParamArray names As String())
                ' Don't even bother if the name doesn't come from a using that we're still
                ' uncertain about. Extension names must be the name of a member access node.
                If names.Any(Function(n) ShouldCheck(extensionNameToClauses, n)) Then
                    ' Bind the node to a symbol.  If it binds to a relevant symbol mark the
                    ' appropriate using directive as necessary.
                    Dim model = DirectCast(Me.semanticModel, SemanticModel)
                    Dim symbolInfo = model.GetSymbolInfo(node, cancellationToken)
                    ProcessExtensionMethodSymbolInfo(symbolInfo)
                End If
            End Sub

            Private Sub ProcessExtensionMethodSymbolInfo(info As SymbolInfo)
                For Each symbol In info.GetBestOrAllSymbols()
                    If symbol.IsReducedExtension() Then
                        MarkNecessaryNamespaceClause(symbol, extensionNameToClauses)
                    End If
                Next
            End Sub
        End Class
    End Class
End Namespace
#End If
