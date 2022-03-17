' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class DocumentationCommentCrefBinder
        Inherits DocumentationCommentBinder

        Private Shared Function CrefReferenceIsLegalForLegacyMode(nameFromCref As TypeSyntax) As Boolean
            Select Case nameFromCref.Kind
                Case SyntaxKind.QualifiedName,
                     SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName,
                     SyntaxKind.PredefinedType
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Private Function BindNameInsideCrefReferenceInLegacyMode(nameFromCref As TypeSyntax, preserveAliases As Boolean, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            ' This binding mode is used for cref-references without signatures and 
            ' emulate Dev11 behavior

            If Not CrefReferenceIsLegalForLegacyMode(nameFromCref) Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            ' The name which is either an upper-level (parent is the cref attribute)
            ' or is part of upper-level qualified name should be searched. All other
            ' names will be bound using regular BindType calls

            Dim name As VisualBasicSyntaxNode = nameFromCref.Parent
            While name IsNot Nothing And name.Kind <> SyntaxKind.CrefReference
                If name.Kind <> SyntaxKind.QualifiedName Then
                    ' Not a top-level name or a top-level qualified name part
                    Dim result As Symbol = If(preserveAliases,
                                              BindTypeOrAliasSyntax(nameFromCref, BindingDiagnosticBag.Discarded),
                                              BindTypeSyntax(nameFromCref, BindingDiagnosticBag.Discarded))
                    Return ImmutableArray.Create(Of Symbol)(result)
                End If

                name = name.Parent
            End While

            Debug.Assert(name IsNot Nothing)

            ' This is an upper-level name, we check is it has any 
            ' diagnostics and if it does we return empty symbol set
            If nameFromCref.ContainsDiagnostics() Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            Dim symbols = ArrayBuilder(Of Symbol).GetInstance()

            Select Case nameFromCref.Kind
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName
                    BindSimpleNameForCref(DirectCast(nameFromCref, SimpleNameSyntax), symbols, preserveAliases, useSiteInfo, False)

                Case SyntaxKind.PredefinedType
                    BindPredefinedTypeForCref(DirectCast(nameFromCref, PredefinedTypeSyntax), symbols)

                Case SyntaxKind.QualifiedName
                    BindQualifiedNameForCref(DirectCast(nameFromCref, QualifiedNameSyntax), symbols, preserveAliases, useSiteInfo)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(nameFromCref.Kind)
            End Select

            RemoveOverriddenMethodsAndProperties(symbols)

            Return symbols.ToImmutableAndFree()
        End Function

        Private Sub BindQualifiedNameForCref(node As QualifiedNameSyntax, symbols As ArrayBuilder(Of Symbol), preserveAliases As Boolean, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            Dim allowColorColor As Boolean = True

            Dim left As NameSyntax = node.Left
            Select Case left.Kind
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName
                    BindSimpleNameForCref(DirectCast(left, SimpleNameSyntax), symbols, preserveAliases, useSiteInfo, True)

                Case SyntaxKind.QualifiedName
                    BindQualifiedNameForCref(DirectCast(left, QualifiedNameSyntax), symbols, preserveAliases, useSiteInfo)
                    allowColorColor = False

                Case SyntaxKind.GlobalName
                    symbols.Add(Me.Compilation.GlobalNamespace)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(left.Kind)
            End Select

            If symbols.Count <> 1 Then
                ' This is an error, we don't know what symbol to search for the right name
                ' It does not matter if the ambiguous symbols were actually found
                symbols.Clear()
                Return
            End If

            Dim singleSymbol As Symbol = symbols(0)
            symbols.Clear()

            ' We found one single symbol, we need to search for the 'right' 
            ' name in the context of this symbol
            Dim right As SimpleNameSyntax = node.Right

            If right.Kind = SyntaxKind.GenericName Then
                ' Generic name
                Dim genericName = DirectCast(right, GenericNameSyntax)
                BindSimpleNameForCref(genericName.Identifier.ValueText,
                                      genericName.TypeArgumentList.Arguments.Count,
                                      symbols,
                                      preserveAliases,
                                      useSiteInfo,
                                      containingSymbol:=singleSymbol,
                                      allowColorColor:=allowColorColor)

                If symbols.Count <> 1 Then
                    ' Don't do any construction in case nothing was found or
                    ' there is ambiguity
                    Return
                End If
                symbols(0) = ConstructGenericSymbolWithTypeArgumentsForCref(symbols(0), genericName)

            Else
                ' Simple identifier name
                Debug.Assert(right.Kind = SyntaxKind.IdentifierName)
                Dim identifierName As String = DirectCast(right, IdentifierNameSyntax).Identifier.ValueText

                ' Search for 0 arity first
                BindSimpleNameForCref(identifierName,
                                      0,
                                      symbols,
                                      preserveAliases,
                                      useSiteInfo,
                                      containingSymbol:=singleSymbol,
                                      allowColorColor:=allowColorColor)
                If symbols.Count > 0 Then
                    Return
                End If

                ' Search with any arity, if we find the single result, it is going to be 
                ' selected as the right one, otherwise we will return ambiguous result
                BindSimpleNameForCref(identifierName,
                                      -1,
                                      symbols,
                                      preserveAliases,
                                      useSiteInfo,
                                      containingSymbol:=singleSymbol,
                                      allowColorColor:=allowColorColor)
            End If
        End Sub

        Private Sub LookupSimpleNameInContainingSymbol(containingSymbol As Symbol,
                                                       allowColorColor As Boolean,
                                                       name As String,
                                                       arity As Integer,
                                                       preserveAliases As Boolean,
                                                       lookupResult As LookupResult,
                                                       options As LookupOptions,
                                                       <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
lAgain:
            Select Case containingSymbol.Kind
                Case SymbolKind.Namespace
                    LookupMember(lookupResult, DirectCast(containingSymbol, NamespaceSymbol), name, arity, options, useSiteInfo)

                Case SymbolKind.Alias
                    If Not preserveAliases Then
                        containingSymbol = DirectCast(containingSymbol, AliasSymbol).Target
                        GoTo lAgain
                    End If

                Case SymbolKind.NamedType, SymbolKind.ArrayType
                    LookupMember(lookupResult, DirectCast(containingSymbol, TypeSymbol), name, arity, options, useSiteInfo)

                Case SymbolKind.Property
                    If allowColorColor Then
                        ' Check for Color Color case
                        Dim [property] = DirectCast(containingSymbol, PropertySymbol)
                        Dim propertyType As TypeSymbol = [property].Type
                        If IdentifierComparison.Equals([property].Name, propertyType.Name) Then
                            containingSymbol = propertyType
                            GoTo lAgain
                        End If
                    End If

                Case SymbolKind.Field
                    If allowColorColor Then
                        ' Check for Color Color case
                        Dim field = DirectCast(containingSymbol, FieldSymbol)
                        Dim fieldType As TypeSymbol = field.Type
                        If IdentifierComparison.Equals(field.Name, fieldType.Name) Then
                            containingSymbol = fieldType
                            GoTo lAgain
                        End If
                    End If

                Case SymbolKind.Method
                    If allowColorColor Then
                        ' Check for Color Color case
                        Dim method = DirectCast(containingSymbol, MethodSymbol)
                        If Not method.IsSub Then
                            Dim returnType As TypeSymbol = method.ReturnType
                            If IdentifierComparison.Equals(method.Name, returnType.Name) Then
                                containingSymbol = returnType
                                GoTo lAgain
                            End If
                        End If
                    End If

                Case Else
                    ' Nothing can be found in context of these symbols

            End Select
        End Sub

        Private Sub BindSimpleNameForCref(name As String,
                                          arity As Integer,
                                          symbols As ArrayBuilder(Of Symbol),
                                          preserveAliases As Boolean,
                                          <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
                                          Optional containingSymbol As Symbol = Nothing,
                                          Optional allowColorColor As Boolean = False,
                                          Optional typeOrNamespaceOnly As Boolean = False)

            Debug.Assert(Not String.IsNullOrEmpty(name))

            If String.IsNullOrEmpty(name) Then
                ' Return an empty symbol collection in error scenario
                Return
            End If

            Dim options = LookupOptions.UseBaseReferenceAccessibility Or
                          LookupOptions.MustNotBeReturnValueVariable Or
                          LookupOptions.IgnoreExtensionMethods Or
                          LookupOptions.MustNotBeLocalOrParameter Or
                          LookupOptions.NoSystemObjectLookupForInterfaces Or
                          LookupOptions.IgnoreAccessibility

            If arity < 0 Then
                options = options Or LookupOptions.AllMethodsOfAnyArity
            End If

            If typeOrNamespaceOnly Then
                options = options Or LookupOptions.NamespacesOrTypesOnly
            End If

            Dim lookupResult As LookupResult = LookupResult.GetInstance()

            If containingSymbol Is Nothing Then
                Me.Lookup(lookupResult, name, arity, options, useSiteInfo)
            Else
                LookupSimpleNameInContainingSymbol(containingSymbol,
                                               allowColorColor,
                                               name,
                                               arity,
                                               preserveAliases,
                                               lookupResult,
                                               options,
                                               useSiteInfo)
            End If

            If Not lookupResult.IsGoodOrAmbiguous OrElse Not lookupResult.HasSymbol Then
                lookupResult.Free()
                Return
            End If

            CreateGoodOrAmbiguousFromLookupResultAndFree(lookupResult, symbols, preserveAliases)
        End Sub

        Private Sub BindSimpleNameForCref(node As SimpleNameSyntax,
                                          symbols As ArrayBuilder(Of Symbol),
                                          preserveAliases As Boolean,
                                          <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
                                          typeOrNamespaceOnly As Boolean)

            ' Name syntax of Cref should not have diagnostics
            If node.ContainsDiagnostics Then
                ' Return an empty symbol collection in case there is any syntax diagnostics
                Return
            End If

            If node.Kind = SyntaxKind.GenericName Then
                ' Generic name
                Dim genericName = DirectCast(node, GenericNameSyntax)

                BindSimpleNameForCref(genericName.Identifier.ValueText,
                                      genericName.TypeArgumentList.Arguments.Count,
                                      symbols,
                                      preserveAliases,
                                      useSiteInfo,
                                      typeOrNamespaceOnly:=typeOrNamespaceOnly)

                If symbols.Count <> 1 Then
                    ' Don't do any construction in case nothing was found or there is an ambiguity
                    Return
                End If

                symbols(0) = ConstructGenericSymbolWithTypeArgumentsForCref(symbols(0), genericName)

            Else
                ' Simple identifier name
                Debug.Assert(node.Kind = SyntaxKind.IdentifierName)
                Dim identifier As SyntaxToken = DirectCast(node, IdentifierNameSyntax).Identifier
                Dim identifierName As String = identifier.ValueText

                ' Search for 0 arity first
                BindSimpleNameForCref(identifierName,
                                      0,
                                      symbols,
                                      preserveAliases,
                                      useSiteInfo,
                                      typeOrNamespaceOnly:=typeOrNamespaceOnly)

                If symbols.Count > 0 Then
                    Return
                End If

                ' Search with any arity, if we find the single result, it is going to be 
                ' selected as the right one, otherwise we will return ambiguous result
                BindSimpleNameForCref(identifierName,
                                      -1,
                                      symbols,
                                      preserveAliases,
                                      useSiteInfo,
                                      typeOrNamespaceOnly:=typeOrNamespaceOnly)
            End If
        End Sub

        Private Sub BindPredefinedTypeForCref(node As PredefinedTypeSyntax,
                                              symbols As ArrayBuilder(Of Symbol))

            ' Name syntax of Cref should not have diagnostics
            If node.ContainsDiagnostics Then
                ' Return an empty symbol collection in case there is any syntax diagnostics
                Return
            End If

            Dim type As SpecialType
            Select Case node.Keyword.Kind
                Case SyntaxKind.ObjectKeyword
                    type = SpecialType.System_Object
                Case SyntaxKind.BooleanKeyword
                    type = SpecialType.System_Boolean
                Case SyntaxKind.DateKeyword
                    type = SpecialType.System_DateTime
                Case SyntaxKind.CharKeyword
                    type = SpecialType.System_Char
                Case SyntaxKind.StringKeyword
                    type = SpecialType.System_String
                Case SyntaxKind.DecimalKeyword
                    type = SpecialType.System_Decimal
                Case SyntaxKind.ByteKeyword
                    type = SpecialType.System_Byte
                Case SyntaxKind.SByteKeyword
                    type = SpecialType.System_SByte
                Case SyntaxKind.UShortKeyword
                    type = SpecialType.System_UInt16
                Case SyntaxKind.ShortKeyword
                    type = SpecialType.System_Int16
                Case SyntaxKind.UIntegerKeyword
                    type = SpecialType.System_UInt32
                Case SyntaxKind.IntegerKeyword
                    type = SpecialType.System_Int32
                Case SyntaxKind.ULongKeyword
                    type = SpecialType.System_UInt64
                Case SyntaxKind.LongKeyword
                    type = SpecialType.System_Int64
                Case SyntaxKind.SingleKeyword
                    type = SpecialType.System_Single
                Case SyntaxKind.DoubleKeyword
                    type = SpecialType.System_Double
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Keyword.Kind)
            End Select

            ' We discard diagnostics in case 
            symbols.Add(Me.GetSpecialType(type, node, BindingDiagnosticBag.Discarded))
        End Sub

        Private Function ConstructGenericSymbolWithTypeArgumentsForCref(genericSymbol As Symbol, genericName As GenericNameSyntax) As Symbol
            Select Case genericSymbol.Kind
                Case SymbolKind.Method
                    Dim method = DirectCast(genericSymbol, MethodSymbol)
                    Debug.Assert(method.Arity = genericName.TypeArgumentList.Arguments.Count)
                    Return method.Construct(BingTypeArgumentsForCref(genericName.TypeArgumentList.Arguments))

                Case SymbolKind.NamedType, SymbolKind.ErrorType
                    Dim type = DirectCast(genericSymbol, NamedTypeSymbol)
                    Debug.Assert(type.Arity = genericName.TypeArgumentList.Arguments.Count)
                    Return type.Construct(BingTypeArgumentsForCref(genericName.TypeArgumentList.Arguments))

                Case SymbolKind.Alias
                    Dim [alias] = DirectCast(genericSymbol, AliasSymbol)
                    Return ConstructGenericSymbolWithTypeArgumentsForCref([alias].Target, genericName)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(genericSymbol.Kind)
            End Select
        End Function

        Private Function BingTypeArgumentsForCref(args As SeparatedSyntaxList(Of TypeSyntax)) As ImmutableArray(Of TypeSymbol)
            Dim result(args.Count - 1) As TypeSymbol
            For i = 0 To args.Count - 1
                result(i) = Me.BindTypeSyntax(args(i), BindingDiagnosticBag.Discarded)
            Next
            Return result.AsImmutableOrNull()
        End Function

        Private Shared Sub CreateGoodOrAmbiguousFromLookupResultAndFree(lookupResult As LookupResult, result As ArrayBuilder(Of Symbol), preserveAliases As Boolean)
            Dim di As DiagnosticInfo = lookupResult.Diagnostic

            If TypeOf di Is AmbiguousSymbolDiagnostic Then
                ' Several ambiguous symbols wrapped in 'AmbiguousSymbolDiagnostic'
                Debug.Assert(lookupResult.Kind = LookupResultKind.Ambiguous)
                Debug.Assert(lookupResult.Symbols.Count = 1)

                Dim symbols As ImmutableArray(Of Symbol) = DirectCast(di, AmbiguousSymbolDiagnostic).AmbiguousSymbols
                Debug.Assert(symbols.Length > 1)

                If preserveAliases Then
                    result.AddRange(symbols)
                Else
                    For Each sym In symbols
                        result.Add(UnwrapAlias(sym))
                    Next
                End If

            Else
                If preserveAliases Then
                    result.AddRange(lookupResult.Symbols)
                Else
                    For Each sym In lookupResult.Symbols
                        result.Add(sym.UnwrapAlias())
                    Next
                End If
            End If

            lookupResult.Free()
        End Sub

    End Class

End Namespace

