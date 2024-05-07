' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class SymbolDisplayVisitor
        Public Overrides Sub VisitField(symbol As IFieldSymbol)
            ' field symbol must have a containing type
            Debug.Assert(TypeOf symbol.ContainingSymbol Is INamedTypeSymbol)

            AddAccessibilityIfRequired(symbol)
            AddMemberModifiersIfRequired(symbol)
            AddFieldModifiersIfRequired(symbol)

            Dim visitedParents As Boolean = False
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) Then
                Dim containingType = TryCast(symbol.ContainingSymbol, INamedTypeSymbol)
                If containingType IsNot Nothing Then
                    containingType.Accept(Me.NotFirstVisitor())
                    AddOperator(SyntaxKind.DotToken)
                    visitedParents = True
                End If
            End If

            If symbol.ContainingType.TypeKind = TypeKind.Enum Then
                Builder.Add(CreatePart(SymbolDisplayPartKind.EnumMemberName, symbol, symbol.Name, visitedParents))
            ElseIf symbol.IsConst Then
                Builder.Add(CreatePart(SymbolDisplayPartKind.ConstantName, symbol, symbol.Name, visitedParents))
            Else
                Builder.Add(CreatePart(SymbolDisplayPartKind.FieldName, symbol, symbol.Name, visitedParents))
            End If

            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) AndAlso
               Me.IsFirstSymbolVisited AndAlso
               Not IsEnumMember(symbol) Then

                AddSpace()
                AddKeyword(SyntaxKind.AsKeyword)
                AddSpace()

                symbol.Type.Accept(Me.NotFirstVisitor())

                AddCustomModifiersIfRequired(symbol.CustomModifiers)
            End If

            If Me.IsFirstSymbolVisited AndAlso
                Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeConstantValue) AndAlso
                symbol.IsConst AndAlso
                symbol.HasConstantValue Then

                AddSpace()
                AddPunctuation(SyntaxKind.EqualsToken)
                AddSpace()
                AddConstantValue(symbol.Type, symbol.ConstantValue, preferNumericValueOrExpandedFlagsForEnum:=IsEnumMember(symbol))
            End If
        End Sub

        Public Overrides Sub VisitProperty(symbol As IPropertySymbol)
            AddAccessibilityIfRequired(symbol)
            AddMemberModifiersIfRequired(symbol)

            If Format.PropertyStyle = SymbolDisplayPropertyStyle.ShowReadWriteDescriptor Then
                If (symbol.IsReadOnly) Then
                    AddKeyword(SyntaxKind.ReadOnlyKeyword)
                    AddSpace()
                ElseIf (symbol.IsWriteOnly) Then
                    AddKeyword(SyntaxKind.WriteOnlyKeyword)
                    AddSpace()
                End If
            End If

            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) AndAlso symbol.IsIndexer Then
                AddKeyword(SyntaxKind.DefaultKeyword)
                AddSpace()
            End If

            If symbol.ReturnsByRef AndAlso Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef) Then
                AddKeyword(SyntaxKind.ByRefKeyword)
                AddCustomModifiersIfRequired(symbol.RefCustomModifiers)
                AddSpace()
            End If

            If Format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
                If IsWithEventsProperty(symbol) Then
                    AddKeyword(SyntaxKind.WithEventsKeyword)
                Else
                    AddKeyword(SyntaxKind.PropertyKeyword)
                End If

                AddSpace()
            End If

            Dim includedContainingType = False
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) AndAlso IncludeNamedType(symbol.ContainingType) Then
                symbol.ContainingType.Accept(Me.NotFirstVisitor)
                AddOperator(SyntaxKind.DotToken)
                includedContainingType = True
            End If

            Builder.Add(CreatePart(SymbolDisplayPartKind.PropertyName, symbol, symbol.Name, includedContainingType))

            If symbol.Parameters.Length > 0 Then
                If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    AddParametersIfRequired(isExtensionMethod:=False, parameters:=symbol.Parameters)
                    AddPunctuation(SyntaxKind.CloseParenToken)
                End If
            End If

            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
                AddSpace()
                AddKeyword(SyntaxKind.AsKeyword)
                AddSpace()
                symbol.Type.Accept(Me.NotFirstVisitor)

                AddCustomModifiersIfRequired(symbol.TypeCustomModifiers)
            End If
        End Sub

        Public Overrides Sub VisitEvent(symbol As IEventSymbol)
            AddAccessibilityIfRequired(symbol)
            AddMemberModifiersIfRequired(symbol)

            If Format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
                AddKeyword(SyntaxKind.EventKeyword)
                AddSpace()
            End If

            Dim visitedParents As Boolean = False
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) AndAlso IncludeNamedType(symbol.ContainingType) Then
                symbol.ContainingType.Accept(Me.NotFirstVisitor)
                AddOperator(SyntaxKind.DotToken)
                visitedParents = True
            End If

            Builder.Add(CreatePart(SymbolDisplayPartKind.EventName, symbol, symbol.Name, visitedParents))

            Dim sourceSymbol = TryCast(symbol, SourceEventSymbol)
            If sourceSymbol IsNot Nothing AndAlso sourceSymbol.IsTypeInferred Then
                If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    AddParametersIfRequired(isExtensionMethod:=False, parameters:=StaticCast(Of IParameterSymbol).From(sourceSymbol.DelegateParameters))
                    AddPunctuation(SyntaxKind.CloseParenToken)
                End If
            End If

            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
                If sourceSymbol IsNot Nothing AndAlso sourceSymbol.IsTypeInferred Then
                    'events with parameters - no need for return type here
                Else
                    AddSpace()
                    AddKeyword(SyntaxKind.AsKeyword)
                    AddSpace()
                    symbol.Type.Accept(Me.NotFirstVisitor)
                End If
            End If
        End Sub

        Public Overrides Sub VisitMethod(symbol As IMethodSymbol)
            If IsDeclareMethod(symbol) Then
                VisitDeclareMethod(symbol)
                Return
            End If

            If symbol.IsExtensionMethod AndAlso Format.ExtensionMethodStyle <> SymbolDisplayExtensionMethodStyle.Default Then
                If symbol.MethodKind = MethodKind.ReducedExtension AndAlso Format.ExtensionMethodStyle = SymbolDisplayExtensionMethodStyle.StaticMethod Then
                    symbol = symbol.GetConstructedReducedFrom()
                ElseIf symbol.MethodKind <> MethodKind.ReducedExtension AndAlso Format.ExtensionMethodStyle = SymbolDisplayExtensionMethodStyle.InstanceMethod Then
                    ' If we cannot reduce this to an instance form then display in the static form
                    symbol = If(symbol.ReduceExtensionMethod(symbol.Parameters.First().Type), symbol)
                End If
            End If

            AddAccessibilityIfRequired(symbol)
            AddMemberModifiersIfRequired(symbol)

            If symbol.ReturnsByRef AndAlso Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef) Then
                AddKeyword(SyntaxKind.ByRefKeyword)
                AddCustomModifiersIfRequired(symbol.RefCustomModifiers)
                AddSpace()
            End If

            AddMethodKind(symbol)
            AddMethodName(symbol)
            AddMethodGenericParameters(symbol)
            AddMethodParameters(symbol)
            AddMethodReturnType(symbol)
        End Sub

        Private Sub AddMethodKind(symbol As IMethodSymbol)
            If Format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
                Select Case symbol.MethodKind
                    Case MethodKind.Constructor, MethodKind.StaticConstructor
                        AddKeyword(SyntaxKind.SubKeyword)
                        AddSpace()

                    Case MethodKind.PropertyGet
                        If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                            AddKeyword(SyntaxKind.FunctionKeyword)
                            AddSpace()
                        Else
                            AddKeyword(SyntaxKind.PropertyKeyword)
                            AddSpace()
                            AddKeyword(SyntaxKind.GetKeyword)
                            AddSpace()
                        End If

                    Case MethodKind.PropertySet
                        If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                            AddKeyword(SyntaxKind.SubKeyword)
                            AddSpace()
                        Else
                            AddKeyword(SyntaxKind.PropertyKeyword)
                            AddSpace()
                            AddKeyword(SyntaxKind.SetKeyword)
                            AddSpace()
                        End If

                    Case MethodKind.EventAdd,
                        MethodKind.EventRemove,
                        MethodKind.EventRaise

                        If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                            AddKeyword(SyntaxKind.SubKeyword)
                            AddSpace()
                        Else
                            AddKeyword(
                                If(
                                    symbol.MethodKind = MethodKind.EventAdd,
                                    SyntaxKind.AddHandlerKeyword,
                                    If(
                                        symbol.MethodKind = MethodKind.EventRemove,
                                        SyntaxKind.RemoveHandlerKeyword,
                                        SyntaxKind.RaiseEventKeyword)))
                            AddSpace()
                            AddKeyword(SyntaxKind.EventKeyword)
                            AddSpace()
                        End If

                    Case MethodKind.Conversion

                        Dim tokenKind As SyntaxKind = TryGetConversionTokenKind(symbol)

                        If tokenKind = SyntaxKind.None OrElse Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                            AddKeyword(SyntaxKind.FunctionKeyword)
                            AddSpace()
                        Else
                            AddKeyword(tokenKind)
                            AddSpace()

                            AddKeyword(SyntaxKind.OperatorKeyword)
                            AddSpace()
                        End If

                    Case MethodKind.UserDefinedOperator, MethodKind.BuiltinOperator
                        Dim tokenKind As SyntaxKind = TryGetOperatorTokenKind(symbol)

                        If tokenKind = SyntaxKind.None OrElse Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                            AddKeyword(SyntaxKind.FunctionKeyword)
                            AddSpace()
                        Else
                            AddKeyword(SyntaxKind.OperatorKeyword)
                            AddSpace()
                        End If

                    Case MethodKind.Ordinary,
                         MethodKind.DelegateInvoke,
                         MethodKind.ReducedExtension,
                         MethodKind.AnonymousFunction

                        If symbol.ReturnsVoid Then
                            AddKeyword(SyntaxKind.SubKeyword)
                            AddSpace()
                        Else
                            AddKeyword(SyntaxKind.FunctionKeyword)
                            AddSpace()
                        End If

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(symbol.MethodKind)
                End Select
            End If
        End Sub

        Private Sub AddMethodName(symbol As IMethodSymbol)
            AssertContainingSymbol(symbol)

            Dim visitedParents As Boolean = False
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) Then
                Dim containingType As ITypeSymbol

                If symbol.MethodKind = MethodKind.ReducedExtension Then
                    containingType = symbol.ReceiverType
                    Debug.Assert(containingType IsNot Nothing)
                Else
                    containingType = TryCast(symbol.ContainingSymbol, INamedTypeSymbol)
                End If

                If containingType IsNot Nothing Then
                    containingType.Accept(Me.NotFirstVisitor())
                    AddOperator(SyntaxKind.DotToken)
                    visitedParents = True
                End If
            End If

            Select Case symbol.MethodKind
                Case MethodKind.Ordinary, MethodKind.DelegateInvoke, MethodKind.DeclareMethod
                    Builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))

                Case MethodKind.ReducedExtension
                    ' Note: Extension methods invoked off of their static class will be tagged as methods.
                    '       This behavior matches the semantic classification done in NameSyntaxClassifier.
                    Builder.Add(CreatePart(SymbolDisplayPartKind.ExtensionMethodName, symbol, symbol.Name, visitedParents))

                Case MethodKind.PropertyGet,
                    MethodKind.PropertySet,
                    MethodKind.EventAdd,
                    MethodKind.EventRemove,
                    MethodKind.EventRaise

                    If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        Builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
                    Else
                        Dim associatedPropertyOrEvent = symbol.AssociatedSymbol
                        Debug.Assert(associatedPropertyOrEvent IsNot Nothing)

                        If associatedPropertyOrEvent.Kind = SymbolKind.Property Then
                            Builder.Add(CreatePart(SymbolDisplayPartKind.PropertyName, associatedPropertyOrEvent, associatedPropertyOrEvent.Name, visitedParents))
                        Else
                            Builder.Add(CreatePart(SymbolDisplayPartKind.EventName, associatedPropertyOrEvent, associatedPropertyOrEvent.Name, visitedParents))
                        End If
                    End If

                Case MethodKind.Constructor, MethodKind.StaticConstructor
                    If Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        Builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
                    Else
                        AddKeyword(SyntaxKind.NewKeyword)
                    End If

                Case MethodKind.UserDefinedOperator, MethodKind.BuiltinOperator

                    Dim tokenKind As SyntaxKind = TryGetOperatorTokenKind(symbol)

                    If tokenKind = SyntaxKind.None OrElse Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        Builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
                    ElseIf SyntaxFacts.IsKeywordKind(tokenKind) Then
                        ' Prefer to add the operator token as a keyword if it considered both a keyword and an operator.
                        ' For example 'And' is both a keyword and operator, but we prefer 'keyword' here to match VB
                        ' classification behavior since inception.
                        AddKeyword(tokenKind)
                    Else
                        ' Otherwise, if it's an operator and not a keyword (like '+'), then add it as an operator.
                        AddOperator(tokenKind)
                    End If

                Case MethodKind.Conversion
                    Dim tokenKind As SyntaxKind = TryGetConversionTokenKind(symbol)

                    If tokenKind = SyntaxKind.None OrElse Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        Builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
                    Else
                        AddKeyword(SyntaxKind.CTypeKeyword)
                    End If

                Case MethodKind.AnonymousFunction
                    ' there is no name to show, but it must be handled to not cause
                    ' the NYI below. 

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.MethodKind)

            End Select
        End Sub

        Private Shared Function TryGetOperatorTokenKind(symbol As IMethodSymbol) As SyntaxKind
            Dim nameToCheck As String = symbol.Name

            If symbol.MethodKind = MethodKind.BuiltinOperator Then
                Select Case nameToCheck
                    Case WellKnownMemberNames.CheckedAdditionOperatorName
                        nameToCheck = WellKnownMemberNames.AdditionOperatorName
                    Case WellKnownMemberNames.CheckedDivisionOperatorName
                        nameToCheck = WellKnownMemberNames.IntegerDivisionOperatorName
                    Case WellKnownMemberNames.CheckedMultiplyOperatorName
                        nameToCheck = WellKnownMemberNames.MultiplyOperatorName
                    Case WellKnownMemberNames.CheckedSubtractionOperatorName
                        nameToCheck = WellKnownMemberNames.SubtractionOperatorName
                    Case WellKnownMemberNames.CheckedUnaryNegationOperatorName
                        nameToCheck = WellKnownMemberNames.UnaryNegationOperatorName
                End Select
            End If

            Dim opInfo As OverloadResolution.OperatorInfo = OverloadResolution.GetOperatorInfo(nameToCheck)

            If (opInfo.IsUnary AndAlso opInfo.UnaryOperatorKind <> UnaryOperatorKind.Error) OrElse
               (opInfo.IsBinary AndAlso opInfo.BinaryOperatorKind <> BinaryOperatorKind.Error) Then
                Return OverloadResolution.GetOperatorTokenKind(opInfo)
            Else
                Return SyntaxKind.None
            End If
        End Function

        Private Shared Function TryGetConversionTokenKind(symbol As IMethodSymbol) As SyntaxKind
            If CaseInsensitiveComparison.Equals(symbol.Name, WellKnownMemberNames.ImplicitConversionName) Then
                Return SyntaxKind.WideningKeyword
            ElseIf CaseInsensitiveComparison.Equals(symbol.Name, WellKnownMemberNames.ExplicitConversionName) Then
                Return SyntaxKind.NarrowingKeyword
            Else
                Return SyntaxKind.None
            End If
        End Function

        Private Sub AddMethodGenericParameters(method As IMethodSymbol)
            If method.Arity > 0 AndAlso Format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeParameters) Then
                AddTypeArguments(method.TypeArguments)
            End If
        End Sub

        Private Sub AddMethodParameters(method As IMethodSymbol)
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
                AddPunctuation(SyntaxKind.OpenParenToken)
                AddParametersIfRequired(isExtensionMethod:=method.IsExtensionMethod AndAlso method.MethodKind <> MethodKind.ReducedExtension,
                                        parameters:=method.Parameters)
                AddPunctuation(SyntaxKind.CloseParenToken)
            End If
        End Sub

        Private Sub AddMethodReturnType(method As IMethodSymbol)
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
                Select Case method.MethodKind
                    Case MethodKind.Constructor,
                        MethodKind.StaticConstructor
                    Case Else
                        If Not method.ReturnsVoid Then
                            AddSpace()
                            AddKeyword(SyntaxKind.AsKeyword)
                            AddSpace()
                            method.ReturnType.Accept(Me.NotFirstVisitor())
                        End If
                End Select

                AddCustomModifiersIfRequired(method.ReturnTypeCustomModifiers)
            End If
        End Sub

        Private Sub VisitDeclareMethod(method As IMethodSymbol)
            Dim data As DllImportData = method.GetDllImportData()

            AddAccessibilityIfRequired(method)

            If Format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
                AddKeyword(SyntaxKind.DeclareKeyword)
                AddSpace()

                Select Case data.CharacterSet
                    Case Cci.Constants.CharSet_None, CharSet.Ansi
                        AddKeyword(SyntaxKind.AnsiKeyword)
                        AddSpace()

                    Case Cci.Constants.CharSet_Auto
                        AddKeyword(SyntaxKind.AutoKeyword)
                        AddSpace()

                    Case CharSet.Unicode
                        AddKeyword(SyntaxKind.UnicodeKeyword)
                        AddSpace()

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(data.CharacterSet)
                End Select

                AddKeyword(If(method.ReturnsVoid, SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword))
                AddSpace()
            End If

            AddMethodName(method)

            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
                Dim spaceNeeded As Boolean = False

                If data.ModuleName IsNot Nothing Then
                    AddSpace()
                    AddKeyword(SyntaxKind.LibKeyword)
                    AddSpace()

                    Builder.Add(CreatePart(SymbolDisplayPartKind.StringLiteral, Nothing, Quote(data.ModuleName), noEscaping:=True))
                    spaceNeeded = True
                End If

                If data.EntryPointName IsNot Nothing Then
                    AddSpace()
                    AddKeyword(SyntaxKind.AliasKeyword)
                    AddSpace()

                    Builder.Add(CreatePart(SymbolDisplayPartKind.StringLiteral, Nothing, Quote(data.EntryPointName), noEscaping:=True))
                    spaceNeeded = True
                End If

                ' add space in between alias/module name and parameters
                If spaceNeeded AndAlso Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
                    AddSpace()
                End If
            End If

            AddMethodParameters(method)
            AddMethodReturnType(method)
        End Sub

        Private Shared Function Quote(str As String) As String
            Return """"c & str.Replace("""", """""") & """"c
        End Function

        Public Overrides Sub VisitParameter(symbol As IParameterSymbol)

            If Format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeOptionalBrackets) Then
                If symbol.IsOptional Then
                    AddPseudoPunctuation("[") ' There isn't a SyntaxKind for brackets in VB
                End If
            End If

            If Format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeParamsRefOut) Then
                If symbol.RefKind <> RefKind.None AndAlso IsExplicitByRefParameter(symbol) Then
                    AddKeyword(SyntaxKind.ByRefKeyword)
                    AddSpace()

                    AddCustomModifiersIfRequired(symbol.RefCustomModifiers, leadingSpace:=False, trailingSpace:=True)
                End If

                If symbol.IsParams Then
                    AddKeyword(SyntaxKind.ParamArrayKeyword)
                    AddSpace()
                End If
            End If

            If Format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeName) Then
                Dim kind = If(symbol.IsThis, SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.ParameterName)
                Builder.Add(CreatePart(kind, symbol, symbol.Name, False))
            End If

            If Format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeType) Then
                If Format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeName) Then
                    AddSpace()
                    AddKeyword(SyntaxKind.AsKeyword)
                    AddSpace()
                End If

                symbol.Type.Accept(Me.NotFirstVisitor())
                AddCustomModifiersIfRequired(symbol.CustomModifiers)
            End If

            If Format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeDefaultValue) AndAlso symbol.HasExplicitDefaultValue Then
                AddSpace()
                AddPunctuation(SyntaxKind.EqualsToken)
                AddSpace()

                AddConstantValue(symbol.Type, symbol.ExplicitDefaultValue)
            End If

            If Format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeOptionalBrackets) Then
                If symbol.IsOptional Then
                    AddPseudoPunctuation("]") ' There isn't a SyntaxKind for brackets in VB
                End If
            End If
        End Sub

        Private Sub AddCustomModifiersIfRequired(customModifiers As ImmutableArray(Of CustomModifier), Optional leadingSpace As Boolean = True, Optional trailingSpace As Boolean = False)
            If Me.Format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers) AndAlso Not customModifiers.IsEmpty Then
                Const IL_KEYWORD_MODOPT = "modopt"
                Const IL_KEYWORD_MODREQ = "modreq"
                Dim first As Boolean = True

                For Each customModifier In customModifiers
                    If Not first OrElse leadingSpace Then
                        AddSpace()
                    End If

                    first = False

                    Me.Builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, Nothing, If(customModifier.IsOptional, IL_KEYWORD_MODOPT, IL_KEYWORD_MODREQ), True))
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    customModifier.Modifier.Accept(Me.NotFirstVisitor)
                    AddPunctuation(SyntaxKind.CloseParenToken)
                Next

                If trailingSpace Then
                    AddSpace()
                End If
            End If
        End Sub

        Private Sub AddFieldModifiersIfRequired(symbol As IFieldSymbol)
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) AndAlso Not IsEnumMember(symbol) Then
                If symbol.IsConst Then
                    AddKeyword(SyntaxKind.ConstKeyword)
                    AddSpace()
                End If

                If symbol.IsReadOnly Then
                    AddKeyword(SyntaxKind.ReadOnlyKeyword)
                    AddSpace()
                End If
            End If
        End Sub

        Private Sub AddMemberModifiersIfRequired(symbol As ISymbol)
            AssertContainingSymbol(symbol)

            Dim containingType = TryCast(symbol.ContainingSymbol, INamedTypeSymbol)
            If Format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) AndAlso
                (containingType Is Nothing OrElse
                 (containingType.TypeKind <> TypeKind.Interface AndAlso Not IsEnumMember(symbol))) Then

                Dim isConst = symbol.Kind = SymbolKind.Field AndAlso DirectCast(symbol, IFieldSymbol).IsConst
                If symbol.IsStatic AndAlso
                   (containingType Is Nothing OrElse containingType.TypeKind <> TypeKind.Module) AndAlso
                   Not isConst Then

                    AddKeyword(SyntaxKind.SharedKeyword)
                    AddSpace()
                End If

                ' WithEvents visualize as if they are fields.
                If Not IsWithEventsProperty(symbol) Then
                    If symbol.IsAbstract Then
                        AddKeyword(SyntaxKind.MustOverrideKeyword)
                        AddSpace()
                    End If

                    If symbol.IsSealed Then
                        AddKeyword(SyntaxKind.NotOverridableKeyword)
                        AddSpace()
                    End If

                    If symbol.IsVirtual Then
                        AddKeyword(SyntaxKind.OverridableKeyword)
                        AddSpace()
                    End If

                    ' Overloads is only available for VisualBasic symbols.
                    If IsOverloads(symbol) AndAlso Not symbol.IsOverride Then
                        AddKeyword(SyntaxKind.OverloadsKeyword)
                        AddSpace()
                    End If

                    If symbol.IsOverride Then
                        AddKeyword(SyntaxKind.OverridesKeyword)
                        AddSpace()
                    End If
                End If
            End If
        End Sub

        Private Sub AddParametersIfRequired(isExtensionMethod As Boolean, parameters As ImmutableArray(Of IParameterSymbol))
            If Format.ParameterOptions = SymbolDisplayParameterOptions.None Then
                Return
            End If

            Dim first As Boolean = True
            For Each param In parameters
                If Not first Then
                    AddPunctuation(SyntaxKind.CommaToken)
                    AddSpace()
                End If

                first = False
                param.Accept(Me.NotFirstVisitor())
            Next
        End Sub

        Private Function IsWithEventsProperty(symbol As ISymbol) As Boolean
            Dim vbProperty = TryCast(symbol, PropertySymbol)
            Return vbProperty IsNot Nothing AndAlso vbProperty.IsWithEvents
        End Function

        Private Function IsOverloads(symbol As ISymbol) As Boolean
            Dim vbSymbol = TryCast(symbol, Symbol)
            Return vbSymbol IsNot Nothing AndAlso vbSymbol.IsOverloads
        End Function

        Private Function IsDeclareMethod(method As IMethodSymbol) As Boolean
            Dim vbMethod = TryCast(method, MethodSymbol)
            Return vbMethod IsNot Nothing AndAlso vbMethod.MethodKind = MethodKind.DeclareMethod
        End Function

        Private Function IsExplicitByRefParameter(parameter As IParameterSymbol) As Boolean
            Dim vbParameter = TryCast(parameter, ParameterSymbol)
            Return vbParameter IsNot Nothing AndAlso vbParameter.IsExplicitByRef
        End Function

        <Conditional("DEBUG")>
        Private Sub AssertContainingSymbol(symbol As ISymbol)
            ' Symbols which may have null containing type: Lambda methods and Synthesized global methods belonging to PrivateImplementationDetails class.
            Debug.Assert(
                symbol.ContainingSymbol IsNot Nothing OrElse
                symbol.Kind <> SymbolKind.Method OrElse
                DirectCast(symbol, IMethodSymbol).MethodKind = MethodKind.AnonymousFunction OrElse
                TypeOf symbol Is SynthesizedGlobalMethodBase)
        End Sub
    End Class
End Namespace
