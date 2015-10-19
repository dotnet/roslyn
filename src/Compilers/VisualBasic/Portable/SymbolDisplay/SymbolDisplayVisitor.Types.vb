' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class SymbolDisplayVisitor
        Public Overrides Sub VisitArrayType(symbol As IArrayTypeSymbol)
            'See spec section 12.1 for the order of rank specifiers
            'e.g. int[][,][,,] is stored as
            '     ArrayType
            '         Rank = 1
            '         ElementType = ArrayType
            '             Rank = 2
            '             ElementType = ArrayType
            '                 Rank = 3
            '                 ElementType = int

            If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.ReverseArrayRankSpecifiers) Then
                ' Ironically, reverse order is simpler - we just have to recurse on the element type and then add a rank specifier.
                symbol.ElementType.Accept(Me)
                AddCustomModifiersIfRequired(symbol.CustomModifiers, trailingSpace:=True)
                AddArrayRank(symbol)
                Return
            End If

            Dim underlyingNonArrayType As ITypeSymbol = symbol.ElementType
            While underlyingNonArrayType.Kind = SymbolKind.ArrayType
                underlyingNonArrayType = DirectCast(underlyingNonArrayType, IArrayTypeSymbol).ElementType
            End While

            underlyingNonArrayType.Accept(Me.NotFirstVisitor())

            Dim arrayType As IArrayTypeSymbol = symbol
            While arrayType IsNot Nothing
                AddCustomModifiersIfRequired(arrayType.CustomModifiers, trailingSpace:=True)

                AddArrayRank(arrayType)
                arrayType = TryCast(arrayType.ElementType, IArrayTypeSymbol)
            End While
        End Sub

        Private Sub AddArrayRank(symbol As IArrayTypeSymbol)
            Dim insertStars As Boolean = format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays)
            AddPunctuation(SyntaxKind.OpenParenToken)
            If symbol.Rank > 1 Then
                If insertStars Then
                    AddPunctuation(SyntaxKind.AsteriskToken)
                End If
            Else
                Dim array = TryCast(symbol, ArrayTypeSymbol)

                If array IsNot Nothing AndAlso Not array.IsSZArray Then
                    ' Always add an asterisk in this case in order to distinguish between SZArray and MDArray.
                    AddPunctuation(SyntaxKind.AsteriskToken)
                End If
            End If

            Dim i As Integer = 0
            While i < symbol.Rank - 1
                AddPunctuation(SyntaxKind.CommaToken)
                If insertStars Then
                    AddPunctuation(SyntaxKind.AsteriskToken)
                End If
                i = i + 1
            End While
            AddPunctuation(SyntaxKind.CloseParenToken)
        End Sub

        Public Overrides Sub VisitDynamicType(symbol As IDynamicTypeSymbol)
            AddKeyword(SyntaxKind.ObjectKeyword)
        End Sub

        Public Overrides Sub VisitPointerType(symbol As IPointerTypeSymbol)
            symbol.PointedAtType.Accept(Me.NotFirstVisitor())
            AddPunctuation(SyntaxKind.AsteriskToken)
        End Sub

        Public Overrides Sub VisitTypeParameter(symbol As ITypeParameterSymbol)
            If isFirstSymbolVisited Then
                AddTypeParameterVarianceIfRequired(symbol)
            End If

            builder.Add(CreatePart(SymbolDisplayPartKind.TypeParameterName, symbol, symbol.Name, False))
        End Sub

        Public Overrides Sub VisitNamedType(symbol As INamedTypeSymbol)
            If Me.IsMinimizing AndAlso TryAddAlias(symbol, builder) Then
                Return
            End If

            If format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseSpecialTypes) Then
                If AddSpecialTypeKeyword(symbol) Then
                    Return
                End If

                If Not format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.ExpandNullable) Then
                    If IsNullableType(symbol) AndAlso symbol IsNot symbol.OriginalDefinition Then
                        symbol.TypeArguments(0).Accept(Me.NotFirstVisitor())
                        AddPunctuation(SyntaxKind.QuestionToken)
                        Return
                    End If
                End If
            End If

            If Me.IsMinimizing Then
                MinimallyQualify(symbol)
                Return
            End If

            AddTypeKind(symbol)

            If CanShowDelegateSignature(symbol) Then
                If symbol.IsAnonymousType OrElse
                   format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndSignature Then

                    Dim invokeMethod = symbol.DelegateInvokeMethod
                    If invokeMethod.ReturnsVoid Then
                        AddKeyword(SyntaxKind.SubKeyword)
                    Else
                        AddKeyword(SyntaxKind.FunctionKeyword)
                    End If

                    AddSpace()
                End If
            End If

            'TODO: aliases

            'only visit the namespace if the style requires it and there isn't an enclosing type
            Dim visitedParents = False
            Dim containingSymbol As ISymbol = symbol.ContainingSymbol
            If ShouldVisitNamespace(containingSymbol) Then
                visitedParents = True

                Dim ns = DirectCast(containingSymbol, INamespaceSymbol)

                ' For some VB symbols, we may want to fix up the namespace name (to normalize case)
                ' GetEmittedNamespaceName may return Nothing
                Dim vbNamedTypeSymbol = TryCast(symbol, NamedTypeSymbol)
                Dim emittedName As String = If(
                    vbNamedTypeSymbol IsNot Nothing,
                    If(vbNamedTypeSymbol.GetEmittedNamespaceName(), String.Empty),
                                     String.Empty)

                VisitNamespace(ns, emittedName)
                AddOperator(SyntaxKind.DotToken)
            End If

            If format.TypeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameAndContainingTypes OrElse
                format.TypeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces Then
                Dim containingType = symbol.ContainingType
                If containingType IsNot Nothing Then
                    visitedParents = True
                    containingType.Accept(Me.NotFirstVisitor())
                    AddOperator(SyntaxKind.DotToken)
                End If
            End If

            AddNameAndTypeArgumentsOrParameters(symbol, visitedParents)

            If CanShowDelegateSignature(symbol) Then
                If symbol.IsAnonymousType OrElse
                   format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndSignature OrElse
                   format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndParameters Then

                    Dim method = symbol.DelegateInvokeMethod
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    AddParametersIfRequired(isExtensionMethod:=False, parameters:=method.Parameters)
                    AddPunctuation(SyntaxKind.CloseParenToken)
                End If

                If symbol.IsAnonymousType OrElse format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndSignature Then
                    Dim invokeMethod = symbol.DelegateInvokeMethod
                    If Not invokeMethod.ReturnsVoid Then
                        AddSpace()
                        AddKeyword(SyntaxKind.AsKeyword)
                        AddSpace()
                        invokeMethod.ReturnType.Accept(Me.NotFirstVisitor())
                    End If
                End If
            End If
        End Sub

        Private Function CanShowDelegateSignature(symbol As INamedTypeSymbol) As Boolean
            Return isFirstSymbolVisited AndAlso
                symbol.TypeKind = TypeKind.Delegate AndAlso
                (symbol.IsAnonymousType OrElse format.DelegateStyle <> SymbolDisplayDelegateStyle.NameOnly) AndAlso
                symbol.DelegateInvokeMethod IsNot Nothing
        End Function

        Private Sub AddNameAndTypeArgumentsOrParameters(symbol As INamedTypeSymbol, noEscaping As Boolean)
            Dim partKind As SymbolDisplayPartKind
            Dim symbolName = symbol.Name
            Dim skipTypeArguments As Boolean = False

            If symbol.IsAnonymousType Then
                AddAnonymousTypeName(symbol)
                Return
            End If

            If format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName) AndAlso
                String.IsNullOrEmpty(symbolName) Then

                symbolName = StringConstants.NamedSymbolErrorName
            Else
                symbolName = RemoveAttributeSuffixIfNecessary(symbol, symbolName)
            End If

            Select Case symbol.TypeKind
                Case TypeKind.Class,
                     TypeKind.Submission
                    partKind = SymbolDisplayPartKind.ClassName
                Case TypeKind.Delegate
                    partKind = SymbolDisplayPartKind.DelegateName
                Case TypeKind.Enum
                    partKind = SymbolDisplayPartKind.EnumName
                Case TypeKind.Interface
                    partKind = SymbolDisplayPartKind.InterfaceName
                Case TypeKind.Module
                    partKind = SymbolDisplayPartKind.ModuleName
                Case TypeKind.Struct
                    partKind = SymbolDisplayPartKind.StructName
                Case TypeKind.Error
                    partKind = SymbolDisplayPartKind.ErrorTypeName
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.TypeKind)
            End Select

            builder.Add(CreatePart(partKind, symbol, symbolName, noEscaping))

            ' Unfortunately, this will only work for VB symbols.  The degraded experience for non-VB symbols seems acceptable for now.
            Dim isMissingMetadataType As Boolean = TypeOf symbol Is MissingMetadataTypeSymbol

            If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes) Then
                ' Only the compiler can set the internal option and the compiler doesn't use other implementations of INamedTypeSymbol.
                If DirectCast(symbol, NamedTypeSymbol).MangleName Then
                    Debug.Assert(symbol.Arity > 0)
                    builder.Add(CreatePart(InternalSymbolDisplayPartKind.Arity, Nothing,
                                           MetadataHelpers.GenericTypeNameManglingChar & symbol.Arity.ToString(), False))
                End If
            ElseIf symbol.Arity > 0 AndAlso format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeParameters) AndAlso Not skipTypeArguments Then
                If isMissingMetadataType OrElse symbol.IsUnboundGenericType Then
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    AddKeyword(SyntaxKind.OfKeyword)
                    AddSpace()
                    Dim i As Integer = 0
                    While i < symbol.Arity - 1
                        AddPunctuation(SyntaxKind.CommaToken)
                        i = i + 1
                    End While
                    AddPunctuation(SyntaxKind.CloseParenToken)
                Else
                    ' TODO: Rewrite access to custom modifiers in terms of an interface
                    AddTypeArguments(symbol.TypeArguments,
                                     If(Me.format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers),
                                        TryCast(symbol, NamedTypeSymbol)?.TypeArgumentsCustomModifiers, Nothing).GetValueOrDefault())
                End If
            End If

            ' Only the compiler can set the internal option and the compiler doesn't use other implementations of INamedTypeSymbol.
            If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.FlagMissingMetadataTypes) AndAlso
                (isMissingMetadataType OrElse
                 (Not symbol.IsDefinition AndAlso TypeOf symbol.OriginalDefinition Is MissingMetadataTypeSymbol)) Then

                builder.Add(CreatePart(SymbolDisplayPartKind.Punctuation, Nothing, "[", False))
                builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, symbol, "missing", False))
                builder.Add(CreatePart(SymbolDisplayPartKind.Punctuation, Nothing, "]", False))
            End If
        End Sub

        Private Sub AddAnonymousTypeName(symbol As INamedTypeSymbol)
            Select Case symbol.TypeKind
                Case TypeKind.Class
                    ' TODO: revise to generate user-friendly name 
                    Dim members = String.Join(", ", symbol.GetMembers().OfType(Of IPropertySymbol).Select(Function(p) CreateAnonymousTypeMember(p)))

                    If members.Length = 0 Then
                        builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ClassName, symbol, "<empty anonymous type>"))
                    Else
                        Dim name = String.Format("<anonymous type: {0}>", members)
                        builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ClassName, symbol, name))
                    End If

                Case TypeKind.Delegate
                    builder.Add(CreatePart(SymbolDisplayPartKind.DelegateName, symbol, "<generated method>", True))
            End Select
        End Sub

        Private Function CreateAnonymousTypeMember(prop As IPropertySymbol) As String
            Dim result = CreateAnonymousTypeMemberWorker(prop)
            Return If(prop.IsReadOnly, "Key " & result, result)
        End Function

        Private Function CreateAnonymousTypeMemberWorker(prop As IPropertySymbol) As String
            Return prop.Name & " As " & prop.Type.ToDisplayString(format)
        End Function

        Private Function AddSpecialTypeKeyword(symbol As INamedTypeSymbol) As Boolean
            Dim type = symbol.SpecialType
            Dim specialTypeName = type.TryGetKeywordText()
            If specialTypeName Is Nothing Then
                Return False
            End If

            builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, symbol, specialTypeName, False))
            Return True
        End Function

        Private Sub AddTypeKind(symbol As INamedTypeSymbol)
            If isFirstSymbolVisited AndAlso format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeTypeKeyword) Then
                If symbol.IsAnonymousType Then
                    ' NOTE: Not actually a keyword, but it's not worth introducing a new kind just for this.
                    builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "AnonymousType"))
                    AddSpace()
                Else
                    Dim keyword = GetTypeKindKeyword(symbol.TypeKind)
                    If keyword = SyntaxKind.None Then
                        Return
                    End If

                    AddKeyword(keyword)
                    AddSpace()
                End If
            End If
        End Sub

        Private Shared Function GetTypeKindKeyword(typeKind As TypeKind) As SyntaxKind
            Select Case typeKind
                Case TypeKind.Enum
                    Return SyntaxKind.EnumKeyword
                Case TypeKind.Class
                    Return SyntaxKind.ClassKeyword
                Case TypeKind.Delegate
                    Return SyntaxKind.DelegateKeyword
                Case TypeKind.Interface
                    Return SyntaxKind.InterfaceKeyword
                Case TypeKind.Module
                    Return SyntaxKind.ModuleKeyword
                Case TypeKind.Struct
                    Return SyntaxKind.StructureKeyword
                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        Private Sub AddTypeParameterVarianceIfRequired(symbol As ITypeParameterSymbol)
            If format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeVariance) Then
                Select Case symbol.Variance
                    Case VarianceKind.In
                        AddKeyword(SyntaxKind.InKeyword)
                        AddSpace()
                    Case VarianceKind.Out
                        AddKeyword(SyntaxKind.OutKeyword)
                        AddSpace()
                End Select
            End If
        End Sub

        Private Sub AddTypeArguments(typeArguments As ImmutableArray(Of ITypeSymbol),
                                     Optional modifiers As ImmutableArray(Of ImmutableArray(Of CustomModifier)) = Nothing)
            AddPunctuation(SyntaxKind.OpenParenToken)
            AddKeyword(SyntaxKind.OfKeyword)
            AddSpace()

            Dim first As Boolean = True
            For i As Integer = 0 To typeArguments.Length - 1
                Dim typeArg = typeArguments(i)

                If Not first Then
                    AddPunctuation(SyntaxKind.CommaToken)
                    AddSpace()
                End If

                first = False

                If typeArg.Kind = SymbolKind.TypeParameter Then
                    Dim typeParam = DirectCast(typeArg, ITypeParameterSymbol)
                    AddTypeParameterVarianceIfRequired(typeParam)
                    typeParam.Accept(Me.NotFirstVisitor())

                    If format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeConstraints) Then
                        AddTypeParameterConstraints(typeParam)
                    End If
                Else
                    typeArg.Accept(Me.NotFirstVisitor())
                End If

                If Not modifiers.IsDefaultOrEmpty Then
                    AddCustomModifiersIfRequired(modifiers(i))
                End If
            Next

            AddPunctuation(SyntaxKind.CloseParenToken)
        End Sub

        ''' <summary>
        ''' Return the number of special constraints ('Class', 'Structure',
        ''' and 'New') associated with the type parameter.
        ''' </summary>
        Private Shared Function TypeParameterSpecialConstraintCount(typeParam As ITypeParameterSymbol) As Integer
            Return If(typeParam.HasReferenceTypeConstraint, 1, 0) +
                If(typeParam.HasValueTypeConstraint, 1, 0) +
                If(typeParam.HasConstructorConstraint, 1, 0)
        End Function

        Private Sub AddTypeParameterConstraints(typeParam As ITypeParameterSymbol)
            If Not isFirstSymbolVisited Then
                Return
            End If

            Dim constraintTypes = typeParam.ConstraintTypes
            Dim constraintCount = TypeParameterSpecialConstraintCount(typeParam) + constraintTypes.Length

            If constraintCount = 0 Then
                Return
            End If

            AddSpace()
            AddKeyword(SyntaxKind.AsKeyword)
            AddSpace()

            If constraintCount > 1 Then
                AddPunctuation(SyntaxKind.OpenBraceToken)
            End If

            Dim needComma As Boolean = False
            If typeParam.HasReferenceTypeConstraint Then
                AddKeyword(SyntaxKind.ClassKeyword)
                needComma = True
            ElseIf typeParam.HasValueTypeConstraint Then
                AddKeyword(SyntaxKind.StructureKeyword)
                needComma = True
            End If

            For Each baseType In constraintTypes
                If needComma Then
                    AddPunctuation(SyntaxKind.CommaToken)
                    AddSpace()
                End If

                baseType.Accept(Me.NotFirstVisitor())
                needComma = True
            Next

            If typeParam.HasConstructorConstraint Then
                If needComma Then
                    AddPunctuation(SyntaxKind.CommaToken)
                    AddSpace()
                End If
                AddKeyword(SyntaxKind.NewKeyword)
            End If

            If constraintCount > 1 Then
                AddPunctuation(SyntaxKind.CloseBraceToken)
            End If
        End Sub

        Private Shared Function IsNullableType(symbol As INamedTypeSymbol) As Boolean
            Return symbol.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T
        End Function
    End Class
End Namespace
