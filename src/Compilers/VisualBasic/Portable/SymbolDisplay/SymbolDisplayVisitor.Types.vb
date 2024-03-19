' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

            If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.ReverseArrayRankSpecifiers) Then
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
            Dim insertStars As Boolean = Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays)
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
            If IsFirstSymbolVisited Then
                AddTypeParameterVarianceIfRequired(symbol)
            End If

            Builder.Add(CreatePart(SymbolDisplayPartKind.TypeParameterName, symbol, symbol.Name, False))
        End Sub

        Public Overrides Sub VisitNamedType(symbol As INamedTypeSymbol)
            If Me.IsMinimizing AndAlso TryAddAlias(symbol, Builder) Then
                Return
            End If

            If Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseSpecialTypes) Then
                If AddSpecialTypeKeyword(symbol) Then
                    Return
                End If

                If Not Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.ExpandNullable) Then
                    If ITypeSymbolHelpers.IsNullableType(symbol) AndAlso symbol IsNot symbol.OriginalDefinition Then
                        symbol.TypeArguments(0).Accept(Me.NotFirstVisitor())
                        AddPunctuation(SyntaxKind.QuestionToken)
                        Return
                    End If
                End If
            End If

            If Me.IsMinimizing OrElse (symbol.IsTupleType AndAlso Not ShouldDisplayAsValueTuple(symbol)) Then
                MinimallyQualify(symbol)
                Return
            End If

            AddTypeKind(symbol)

            If CanShowDelegateSignature(symbol) Then
                If symbol.IsAnonymousType OrElse
                   Format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndSignature Then

                    Dim invokeMethod = symbol.DelegateInvokeMethod
                    If invokeMethod.ReturnsVoid Then
                        AddKeyword(SyntaxKind.SubKeyword)
                    Else
                        If invokeMethod.ReturnsByRef AndAlso Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef) Then
                            AddKeyword(SyntaxKind.ByRefKeyword)
                            AddSpace()
                        End If

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

            If Format.TypeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameAndContainingTypes OrElse
                Format.TypeQualificationStyle = SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces Then
                Dim containingType = symbol.ContainingType
                If containingType IsNot Nothing Then
                    visitedParents = True
                    containingType.Accept(Me.NotFirstVisitor())

                    If Format.CompilerInternalOptions.HasFlag(SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes) Then
                        AddOperator(SyntaxKind.PlusToken)
                    Else
                        AddOperator(SyntaxKind.DotToken)
                    End If
                End If
            End If

            AddNameAndTypeArgumentsOrParameters(symbol, visitedParents)

            If CanShowDelegateSignature(symbol) Then
                If symbol.IsAnonymousType OrElse
                   Format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndSignature OrElse
                   Format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndParameters Then

                    Dim method = symbol.DelegateInvokeMethod
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    AddParametersIfRequired(isExtensionMethod:=False, parameters:=method.Parameters)
                    AddPunctuation(SyntaxKind.CloseParenToken)
                End If

                If symbol.IsAnonymousType OrElse Format.DelegateStyle = SymbolDisplayDelegateStyle.NameAndSignature Then
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
            Return IsFirstSymbolVisited AndAlso
                symbol.TypeKind = TypeKind.Delegate AndAlso
                (symbol.IsAnonymousType OrElse Format.DelegateStyle <> SymbolDisplayDelegateStyle.NameOnly) AndAlso
                symbol.DelegateInvokeMethod IsNot Nothing
        End Function

        Private Sub AddNameAndTypeArgumentsOrParameters(symbol As INamedTypeSymbol, noEscaping As Boolean)
            Dim partKind As SymbolDisplayPartKind
            Dim symbolName As String = Nothing
            Dim skipTypeArguments As Boolean = False

            If symbol.IsAnonymousType Then
                AddAnonymousTypeName(symbol)
                Return
            ElseIf symbol.IsTupleType Then
                ' If top level tuple uses non-default names, there is no way to preserve them
                ' unless we use tuple syntax for the type. So, we give them priority.
                If Not ShouldDisplayAsValueTuple(symbol) Then
                    AddTupleTypeName(symbol)
                    Return
                End If

                ' Fall back to displaying the underlying type.
                symbol = symbol.TupleUnderlyingType
            End If

            ' It would be nice to handle C# NoPia symbols too, but it's not worth the effort.

            Dim illegalGenericInstantiationSymbol = TryCast(symbol, NoPiaIllegalGenericInstantiationSymbol)
            If illegalGenericInstantiationSymbol IsNot Nothing Then
                symbol = illegalGenericInstantiationSymbol.UnderlyingSymbol
            Else
                Dim ambiguousCanonicalTypeSymbol = TryCast(symbol, NoPiaAmbiguousCanonicalTypeSymbol)
                If ambiguousCanonicalTypeSymbol IsNot Nothing Then
                    symbol = ambiguousCanonicalTypeSymbol.FirstCandidate
                Else
                    Dim missingCanonicalTypeSymbol = TryCast(symbol, NoPiaMissingCanonicalTypeSymbol)
                    If missingCanonicalTypeSymbol IsNot Nothing Then
                        symbolName = missingCanonicalTypeSymbol.FullTypeName
                    End If
                End If
            End If

            If symbolName Is Nothing Then
                symbolName = symbol.Name
            End If

            If Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName) AndAlso
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

            Builder.Add(CreatePart(partKind, symbol, symbolName, noEscaping))

            ' Unfortunately, this will only work for VB symbols.  The degraded experience for non-VB symbols seems acceptable for now.
            Dim isMissingMetadataType As Boolean = TypeOf symbol Is MissingMetadataTypeSymbol

            If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes) Then
                ' Only the compiler can set the internal option and the compiler doesn't use other implementations of INamedTypeSymbol.
                If DirectCast(symbol, NamedTypeSymbol).MangleName Then
                    Debug.Assert(symbol.Arity > 0)
                    Builder.Add(CreatePart(InternalSymbolDisplayPartKind.Arity, Nothing,
                                           MetadataHelpers.GenericTypeNameManglingChar & symbol.Arity.ToString(), False))
                End If
            ElseIf symbol.Arity > 0 AndAlso Format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeParameters) AndAlso Not skipTypeArguments Then
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
                    AddTypeArguments(symbol.TypeArguments, symbol)
                End If
            End If

            ' Only the compiler can set the internal option and the compiler doesn't use other implementations of INamedTypeSymbol.
            If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.FlagMissingMetadataTypes) AndAlso
                (isMissingMetadataType OrElse
                 (Not symbol.IsDefinition AndAlso TypeOf symbol.OriginalDefinition Is MissingMetadataTypeSymbol)) Then

                Builder.Add(CreatePart(SymbolDisplayPartKind.Punctuation, Nothing, "[", False))
                Builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, symbol, "missing", False))
                Builder.Add(CreatePart(SymbolDisplayPartKind.Punctuation, Nothing, "]", False))
            End If
        End Sub

        Private Function ShouldDisplayAsValueTuple(symbol As INamedTypeSymbol) As Boolean
            Debug.Assert(symbol.IsTupleType)

            If Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.ExpandValueTuple) Then
                Return True
            End If

            Return Not (HasNonDefaultTupleElements(symbol) OrElse CanUseTupleTypeName(symbol))
        End Function

        Private Sub AddAnonymousTypeName(symbol As INamedTypeSymbol)
            Select Case symbol.TypeKind
                Case TypeKind.Class
                    ' TODO: revise to generate user-friendly name 
                    Dim members = String.Join(", ", symbol.GetMembers().OfType(Of IPropertySymbol).Select(Function(p) CreateAnonymousTypeMember(p)))

                    If members.Length = 0 Then
                        Builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ClassName, symbol, "<empty anonymous type>"))
                    Else
                        Dim name = String.Format("<anonymous type: {0}>", members)
                        Builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ClassName, symbol, name))
                    End If

                Case TypeKind.Delegate
                    Builder.Add(CreatePart(SymbolDisplayPartKind.DelegateName, symbol, "<generated method>", True))
            End Select
        End Sub

        ''' <summary>
        ''' Returns true if tuple type syntax can be used to refer to the tuple type without loss of information.
        ''' For example, it cannot be used when extension tuple is using non-default friendly names. 
        ''' </summary>
        ''' <param name="tupleSymbol"></param>
        ''' <returns></returns>
        Private Shared Function CanUseTupleTypeName(tupleSymbol As INamedTypeSymbol) As Boolean
            Dim currentUnderlying As INamedTypeSymbol = GetTupleUnderlyingTypeOrSelf(tupleSymbol)

            If currentUnderlying.Arity = 1 Then
                Return False
            End If

            While currentUnderlying.Arity = TupleTypeSymbol.RestPosition
                tupleSymbol = DirectCast(currentUnderlying.TypeArguments(TupleTypeSymbol.RestPosition - 1), INamedTypeSymbol)
                Debug.Assert(tupleSymbol.IsTupleType)

                If HasNonDefaultTupleElements(tupleSymbol) Then
                    Return False
                End If

                currentUnderlying = GetTupleUnderlyingTypeOrSelf(tupleSymbol)
            End While

            Return True
        End Function

        Private Shared Function GetTupleUnderlyingTypeOrSelf(tupleSymbol As INamedTypeSymbol) As INamedTypeSymbol
            Return If(tupleSymbol.TupleUnderlyingType, tupleSymbol)
        End Function

        Private Shared Function HasNonDefaultTupleElements(tupleSymbol As INamedTypeSymbol) As Boolean
            Return tupleSymbol.TupleElements.Any(Function(e) e.IsExplicitlyNamedTupleElement)
        End Function

        Private Sub AddTupleTypeName(symbol As INamedTypeSymbol)
            Debug.Assert(symbol.IsTupleType)

            If Me.Format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.CollapseTupleTypes) Then
                Builder.Add(CreatePart(SymbolDisplayPartKind.StructName, symbol, "<tuple>", noEscaping:=True))
                Return
            End If

            Dim elements As ImmutableArray(Of IFieldSymbol) = symbol.TupleElements

            AddPunctuation(SyntaxKind.OpenParenToken)

            For i As Integer = 0 To elements.Length - 1
                Dim element = elements(i)

                If i <> 0 Then
                    AddPunctuation(SyntaxKind.CommaToken)
                    AddSpace()
                End If

                If element.IsExplicitlyNamedTupleElement Then
                    Builder.Add(CreatePart(SymbolDisplayPartKind.FieldName, element, element.Name, noEscaping:=False))
                    AddSpace()
                    AddKeyword(SyntaxKind.AsKeyword)
                    AddSpace()
                End If

                element.Type.Accept(Me.NotFirstVisitor)
            Next

            AddPunctuation(SyntaxKind.CloseParenToken)
        End Sub

        Private Function CreateAnonymousTypeMember(prop As IPropertySymbol) As String
            Dim result = CreateAnonymousTypeMemberWorker(prop)
            Return If(prop.IsReadOnly, "Key " & result, result)
        End Function

        Private Function CreateAnonymousTypeMemberWorker(prop As IPropertySymbol) As String
            Return prop.Name & " As " & prop.Type.ToDisplayString(Format)
        End Function

        Private Function AddSpecialTypeKeyword(symbol As INamedTypeSymbol) As Boolean
            Dim type = symbol.SpecialType
            Dim specialTypeName = type.TryGetKeywordText()
            If specialTypeName Is Nothing Then
                Return False
            End If

            Builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, symbol, specialTypeName, False))
            Return True
        End Function

        Private Sub AddTypeKind(symbol As INamedTypeSymbol)
            If IsFirstSymbolVisited AndAlso Format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeTypeKeyword) Then
                If symbol.IsAnonymousType Then
                    ' NOTE: Not actually a keyword, but it's not worth introducing a new kind just for this.
                    Builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.AnonymousTypeIndicator, Nothing, "AnonymousType"))
                    AddSpace()
                ElseIf symbol.IsTupleType Then
                    Builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.AnonymousTypeIndicator, Nothing, "Tuple"))
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
            If Format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeVariance) Then
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
                                     Optional modifiersSource As INamedTypeSymbol = Nothing)
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

                    If Format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeConstraints) Then
                        AddTypeParameterConstraints(typeParam)
                    End If
                Else
                    typeArg.Accept(Me.NotFirstVisitorNamespaceOrType())
                End If

                If modifiersSource IsNot Nothing Then
                    AddCustomModifiersIfRequired(modifiersSource.GetTypeArgumentCustomModifiers(i))
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
            If Not IsFirstSymbolVisited Then
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

    End Class
End Namespace
