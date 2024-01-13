' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class SymbolDisplayVisitor
        Protected Overrides Function ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes() As Boolean
            Dim token = SemanticModelOpt.SyntaxTree.GetRoot().FindToken(PositionOpt)
            Dim startNode = token.Parent

            Return SyntaxFacts.IsInNamespaceOrTypeContext(TryCast(startNode, ExpressionSyntax)) OrElse Me.InNamespaceOrType
        End Function

        Private Sub MinimallyQualify(symbol As INamespaceSymbol, emittedName As String, parentsEmittedName As String)
            Debug.Assert(symbol.ContainingNamespace IsNot Nothing OrElse symbol.IsGlobalNamespace)

            ' NOTE(cyrusn): We only call this once we've already checked if there is an alias that
            ' corresponds to this namespace. 

            If symbol.IsGlobalNamespace Then
                ' nothing to add for global namespace itself
                Return
            End If

            Dim visitedParent = False

            ' Check if the name of this namespace binds to the same namespace symbol.  If so,
            ' then that's all we need to add.  Otherwise, we will add the minimally qualified
            ' version of our parent, and then add ourselves to that.
            Dim symbols = If(
                ShouldRestrictMinimallyQualifyLookupToNamespacesAndTypes(),
                SemanticModelOpt.LookupNamespacesAndTypes(PositionOpt, name:=symbol.Name),
                SemanticModelOpt.LookupSymbols(PositionOpt, name:=symbol.Name))
            Dim lookupResult As NamespaceSymbol = Nothing

            If symbols.Length = 1 Then
                lookupResult = TryCast(symbols(0), NamespaceSymbol)
            End If

            ' It is possible to get NamespaceSymbol with compilation extent here.
            ' Let's check this case.
            If lookupResult IsNot Nothing Then
                Debug.Assert(lookupResult.Extent.Kind = NamespaceKind.Module OrElse lookupResult.Extent.Kind = NamespaceKind.Compilation)

                If lookupResult IsNot symbol AndAlso lookupResult.Extent.Kind = NamespaceKind.Compilation Then
                    Dim ns = TryCast(symbol, NamespaceSymbol)

                    If ns IsNot Nothing AndAlso ns.Extent.Kind <> NamespaceKind.Compilation AndAlso
                       lookupResult.Extent.Compilation.GetCompilationNamespace(ns) Is lookupResult Then
                        lookupResult = ns
                    End If
                End If
            End If

            If lookupResult IsNot symbol Then
                ' Just the name alone didn't bind properly.  Add our minimally qualified parent
                ' (if we have one), a dot, and then our name.
                Dim containingNamespace = symbol.ContainingNamespace
                If containingNamespace IsNot Nothing Then
                    If containingNamespace.IsGlobalNamespace Then

                        Debug.Assert(Format.GlobalNamespaceStyle = SymbolDisplayGlobalNamespaceStyle.Included OrElse
                                          Format.GlobalNamespaceStyle = SymbolDisplayGlobalNamespaceStyle.Omitted OrElse
                                          Format.GlobalNamespaceStyle = SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining)

                        If Format.GlobalNamespaceStyle = SymbolDisplayGlobalNamespaceStyle.Included Then
                            AddGlobalNamespace(containingNamespace)
                            AddOperator(SyntaxKind.DotToken)

                            visitedParent = True
                        End If
                    Else
                        VisitNamespace(containingNamespace, parentsEmittedName)

                        AddOperator(SyntaxKind.DotToken)
                        visitedParent = True
                    End If
                End If
            End If

            Builder.Add(CreatePart(SymbolDisplayPartKind.NamespaceName, symbol, emittedName, visitedParent))
        End Sub

        Private Sub MinimallyQualify(symbol As INamedTypeSymbol)
            ' NOTE(cyrusn): We only call this once we've already checked if there is an alias or
            ' special type that corresponds to this type.
            '
            ' We first start by trying to bind just our name and type arguments.  If they bind to the
            ' symbol that we were constructed from.  Otherwise, we get the minimal name of our
            ' parent, add a dot, and then add ourselves.

            Dim visitedParents As Boolean = False

            If Not (symbol.IsAnonymousType OrElse symbol.IsTupleType) Then
                If Not NameBoundSuccessfullyToSameSymbol(symbol) Then
                    If IncludeNamedType(symbol.ContainingType) Then
                        symbol.ContainingType.Accept(NotFirstVisitor)
                        AddOperator(SyntaxKind.DotToken)
                    ElseIf symbol.ContainingNamespace IsNot Nothing Then
                        If symbol.ContainingNamespace.IsGlobalNamespace Then
                            ' Error symbols are put into the global namespace if the compiler has no
                            ' better guess for it, so we shouldn't go spitting it everywhere.
                            If symbol.TypeKind <> TypeKind.Error Then
                                AddKeyword(SyntaxKind.GlobalKeyword)
                                AddOperator(SyntaxKind.DotToken)
                            End If
                        Else
                            symbol.ContainingNamespace.Accept(NotFirstVisitor)
                            AddOperator(SyntaxKind.DotToken)
                        End If
                    End If

                    visitedParents = True
                End If
            End If

            AddNameAndTypeArgumentsOrParameters(symbol, visitedParents)
        End Sub

        Private Function TryAddAlias(
            symbol As INamespaceOrTypeSymbol,
            builder As ArrayBuilder(Of SymbolDisplayPart)) As Boolean

            Dim [alias] = GetAliasSymbol(symbol)
            If Not [alias] Is Nothing Then
                ' We must verify that the alias actually binds back to the thing it's aliasing. It's
                ' possible there's another symbol with the same name as the alias that binds first

                Dim aliasName = [alias].Name
                Dim boundSymbols = SemanticModelOpt.LookupNamespacesAndTypes(PositionOpt, name:=aliasName)

                If boundSymbols.Length = 1 Then
                    Dim boundAlias = TryCast(boundSymbols(0), IAliasSymbol)
                    If boundAlias IsNot Nothing AndAlso boundAlias.Target.Equals(symbol) Then
                        builder.Add(CreatePart(SymbolDisplayPartKind.AliasName, [alias], aliasName, False))
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Private Function GetAliasSymbol(symbol As INamespaceOrTypeSymbol) As IAliasSymbol
            If Not Me.IsMinimizing Then
                Return Nothing
            End If

            Dim token = SemanticModelOpt.SyntaxTree.GetRoot().FindToken(PositionOpt)
            Dim startNode = token.Parent

            ' NOTE(cyrusn): If we're in an imports clause, we can't use aliases.
            Dim clause = startNode.AncestorsAndSelf().OfType(Of ImportsClauseSyntax).FirstOrDefault()
            If clause IsNot Nothing Then
                Return Nothing
            End If

            Dim compilation = SemanticModelOpt.Compilation

            Dim sourceModule = DirectCast(compilation.SourceModule, SourceModuleSymbol)
            Dim sourceFile = sourceModule.TryGetSourceFile(DirectCast(GetSyntaxTree(SemanticModelOpt), VisualBasicSyntaxTree))
            Debug.Assert(sourceFile IsNot Nothing)

            If Not sourceFile.AliasImportsOpt Is Nothing Then
                For Each [alias] In sourceFile.AliasImportsOpt.Values
                    If [alias].Alias.Target = DirectCast(symbol, NamespaceOrTypeSymbol) Then
                        Return [alias].Alias
                    End If
                Next
            End If

            Return Nothing
        End Function

        Private Function GetSyntaxTree(semanticModel As SemanticModel) As SyntaxTree
            If Not semanticModel.IsSpeculativeSemanticModel Then
                Return semanticModel.SyntaxTree
            End If

            Debug.Assert(semanticModel.ParentModel IsNot Nothing)
            Debug.Assert(Not semanticModel.ParentModel.IsSpeculativeSemanticModel)
            Return semanticModel.ParentModel.SyntaxTree
        End Function

        Private Function RemoveAttributeSuffixIfNecessary(symbol As INamedTypeSymbol, symbolName As String) As String
            If Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix) AndAlso IsDerivedFromAttributeType(symbol) Then

                Dim nameWithoutAttributeSuffix As String = Nothing
                If symbolName.TryGetWithoutAttributeSuffix(False, nameWithoutAttributeSuffix) Then
                    Dim token = SyntaxFactory.ParseToken(nameWithoutAttributeSuffix)
                    If token.Kind = SyntaxKind.IdentifierToken Then
                        symbolName = nameWithoutAttributeSuffix
                    End If
                End If
            End If

            Return symbolName
        End Function

        Private Function IsDerivedFromAttributeType(ByVal derivedType As INamedTypeSymbol) As Boolean
            Return SemanticModelOpt IsNot Nothing AndAlso
                DirectCast(derivedType, NamedTypeSymbol).IsOrDerivedFromWellKnownClass(WellKnownType.System_Attribute,
                                                                                       DirectCast(SemanticModelOpt.Compilation, VisualBasicCompilation),
                                                                                       useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
        End Function
    End Class
End Namespace
