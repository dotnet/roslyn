' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) Then
                Dim containingType = TryCast(symbol.ContainingSymbol, INamedTypeSymbol)
                If containingType IsNot Nothing Then
                    containingType.Accept(Me.NotFirstVisitor())
                    AddOperator(SyntaxKind.DotToken)
                    visitedParents = True
                End If
            End If

            If symbol.ContainingType.TypeKind = TypeKind.Enum Then
                builder.Add(CreatePart(SymbolDisplayPartKind.EnumMemberName, symbol, symbol.Name, visitedParents))
            ElseIf symbol.IsConst Then
                builder.Add(CreatePart(SymbolDisplayPartKind.ConstantName, symbol, symbol.Name, visitedParents))
            Else
                builder.Add(CreatePart(SymbolDisplayPartKind.FieldName, symbol, symbol.Name, visitedParents))
            End If

            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) AndAlso
               Me.isFirstSymbolVisited AndAlso
               Not IsEnumMember(symbol) Then

                AddSpace()
                AddKeyword(SyntaxKind.AsKeyword)
                AddSpace()

                symbol.Type.Accept(Me.NotFirstVisitor())

                AddCustomModifiersIfRequired(symbol.CustomModifiers)
            End If

            If Me.isFirstSymbolVisited AndAlso
                format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeConstantValue) AndAlso
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

            If format.PropertyStyle = SymbolDisplayPropertyStyle.ShowReadWriteDescriptor Then
                If (symbol.IsReadOnly) Then
                    AddKeyword(SyntaxKind.ReadOnlyKeyword)
                    AddSpace()
                ElseIf (symbol.IsWriteOnly) Then
                    AddKeyword(SyntaxKind.WriteOnlyKeyword)
                    AddSpace()
                End If
            End If

            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) AndAlso symbol.IsIndexer Then
                AddKeyword(SyntaxKind.DefaultKeyword)
                AddSpace()
            End If

            If symbol.ReturnsByRef AndAlso format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef) Then
                AddKeyword(SyntaxKind.ByRefKeyword)
                AddCustomModifiersIfRequired(symbol.RefCustomModifiers)
                AddSpace()
            End If

            If format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
                If IsWithEventsProperty(symbol) Then
                    AddKeyword(SyntaxKind.WithEventsKeyword)
                Else
                    AddKeyword(SyntaxKind.PropertyKeyword)
                End If

                AddSpace()
            End If

            Dim includedContainingType = False
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) AndAlso IncludeNamedType(symbol.ContainingType) Then
                symbol.ContainingType.Accept(Me.NotFirstVisitor)
                AddOperator(SyntaxKind.DotToken)
                includedContainingType = True
            End If

            builder.Add(CreatePart(SymbolDisplayPartKind.PropertyName, symbol, symbol.Name, includedContainingType))

            If symbol.Parameters.Length > 0 Then
                If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    AddParametersIfRequired(isExtensionMethod:=False, parameters:=symbol.Parameters)
                    AddPunctuation(SyntaxKind.CloseParenToken)
                End If
            End If

            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
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

            If format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
                AddKeyword(SyntaxKind.EventKeyword)
                AddSpace()
            End If

            Dim visitedParents As Boolean = False
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) AndAlso IncludeNamedType(symbol.ContainingType) Then
                symbol.ContainingType.Accept(Me.NotFirstVisitor)
                AddOperator(SyntaxKind.DotToken)
                visitedParents = True
            End If

            builder.Add(CreatePart(SymbolDisplayPartKind.EventName, symbol, symbol.Name, visitedParents))

            Dim sourceSymbol = TryCast(symbol, SourceEventSymbol)
            If sourceSymbol IsNot Nothing AndAlso sourceSymbol.IsTypeInferred Then
                If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
                    AddPunctuation(SyntaxKind.OpenParenToken)
                    AddParametersIfRequired(isExtensionMethod:=False, parameters:=StaticCast(Of IParameterSymbol).From(sourceSymbol.DelegateParameters))
                    AddPunctuation(SyntaxKind.CloseParenToken)
                End If
            End If

            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
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

        Private Sub AddAccessor([property] As ISymbol, method As IMethodSymbol, keyword As SyntaxKind)
            If method IsNot Nothing Then
                AddSpace()
                If method.DeclaredAccessibility <> [property].DeclaredAccessibility Then
                    AddAccessibilityIfRequired(method)
                End If

                AddKeyword(keyword)
                AddPunctuation(SyntaxKind.SemicolonToken)
            End If
        End Sub

        Public Overrides Sub VisitMethod(symbol As IMethodSymbol)
            If IsDeclareMethod(symbol) Then
                VisitDeclareMethod(symbol)
                Return
            End If

            If symbol.IsExtensionMethod AndAlso format.ExtensionMethodStyle <> SymbolDisplayExtensionMethodStyle.Default Then
                If symbol.MethodKind = MethodKind.ReducedExtension AndAlso format.ExtensionMethodStyle = SymbolDisplayExtensionMethodStyle.StaticMethod Then
                    symbol = symbol.GetConstructedReducedFrom()
                ElseIf symbol.MethodKind <> MethodKind.ReducedExtension AndAlso format.ExtensionMethodStyle = SymbolDisplayExtensionMethodStyle.InstanceMethod Then
                    ' If we cannot reduce this to an instance form then display in the static form
                    symbol = If(symbol.ReduceExtensionMethod(symbol.Parameters.First().Type), symbol)
                End If
            End If

            AddAccessibilityIfRequired(symbol)
            AddMemberModifiersIfRequired(symbol)

            If symbol.ReturnsByRef AndAlso format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeRef) Then
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
            If format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
                Select Case symbol.MethodKind
                    Case MethodKind.Constructor, MethodKind.StaticConstructor
                        AddKeyword(SyntaxKind.SubKeyword)
                        AddSpace()

                    Case MethodKind.PropertyGet
                        If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                            AddKeyword(SyntaxKind.FunctionKeyword)
                            AddSpace()
                        Else
                            AddKeyword(SyntaxKind.PropertyKeyword)
                            AddSpace()
                            AddKeyword(SyntaxKind.GetKeyword)
                            AddSpace()
                        End If

                    Case MethodKind.PropertySet
                        If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
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

                        If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
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
                        If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                            AddKeyword(SyntaxKind.FunctionKeyword)
                            AddSpace()
                        Else
                            If CaseInsensitiveComparison.Equals(symbol.Name, WellKnownMemberNames.ImplicitConversionName) Then
                                AddKeyword(SyntaxKind.WideningKeyword)
                                AddSpace()
                            Else
                                AddKeyword(SyntaxKind.NarrowingKeyword)
                                AddSpace()
                            End If

                            AddKeyword(SyntaxKind.OperatorKeyword)
                            AddSpace()
                        End If

                    Case MethodKind.UserDefinedOperator, MethodKind.BuiltinOperator
                        If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
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
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeContainingType) Then
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
                    builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))

                Case MethodKind.ReducedExtension
                    ' Note: Extension methods invoked off of their static class will be tagged as methods.
                    '       This behavior matches the semantic classification done in NameSyntaxClassifier.
                    builder.Add(CreatePart(SymbolDisplayPartKind.ExtensionMethodName, symbol, symbol.Name, visitedParents))

                Case MethodKind.PropertyGet,
                    MethodKind.PropertySet,
                    MethodKind.EventAdd,
                    MethodKind.EventRemove,
                    MethodKind.EventRaise

                    If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
                    Else
                        Dim associatedPropertyOrEvent = symbol.AssociatedSymbol
                        Debug.Assert(associatedPropertyOrEvent IsNot Nothing)

                        If associatedPropertyOrEvent.Kind = SymbolKind.Property Then
                            builder.Add(CreatePart(SymbolDisplayPartKind.PropertyName, associatedPropertyOrEvent, associatedPropertyOrEvent.Name, visitedParents))
                        Else
                            builder.Add(CreatePart(SymbolDisplayPartKind.EventName, associatedPropertyOrEvent, associatedPropertyOrEvent.Name, visitedParents))
                        End If
                    End If

                Case MethodKind.Constructor, MethodKind.StaticConstructor
                    If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
                    Else
                        AddKeyword(SyntaxKind.NewKeyword)
                    End If

                Case MethodKind.UserDefinedOperator, MethodKind.BuiltinOperator
                    If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
                    Else
                        AddKeyword(OverloadResolution.GetOperatorTokenKind(symbol.Name))
                    End If

                Case MethodKind.Conversion
                    If format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames) Then
                        builder.Add(CreatePart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name, visitedParents))
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

        Private Sub AddMethodGenericParameters(method As IMethodSymbol)
            If method.Arity > 0 AndAlso format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeParameters) Then
                AddTypeArguments(method.TypeArguments)
            End If
        End Sub

        Private Sub AddMethodParameters(method As IMethodSymbol)
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
                AddPunctuation(SyntaxKind.OpenParenToken)
                AddParametersIfRequired(isExtensionMethod:=method.IsExtensionMethod AndAlso method.MethodKind <> MethodKind.ReducedExtension,
                                        parameters:=method.Parameters)
                AddPunctuation(SyntaxKind.CloseParenToken)
            End If
        End Sub

        Private Sub AddMethodReturnType(method As IMethodSymbol)
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
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

            If format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeMemberKeyword) Then
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

            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeType) Then
                Dim spaceNeeded As Boolean = False

                If data.ModuleName IsNot Nothing Then
                    AddSpace()
                    AddKeyword(SyntaxKind.LibKeyword)
                    AddSpace()

                    builder.Add(CreatePart(SymbolDisplayPartKind.StringLiteral, Nothing, Quote(data.ModuleName), noEscaping:=True))
                    spaceNeeded = True
                End If

                If data.EntryPointName IsNot Nothing Then
                    AddSpace()
                    AddKeyword(SyntaxKind.AliasKeyword)
                    AddSpace()

                    builder.Add(CreatePart(SymbolDisplayPartKind.StringLiteral, Nothing, Quote(data.EntryPointName), noEscaping:=True))
                    spaceNeeded = True
                End If

                ' add space in between alias/module name and parameters
                If spaceNeeded AndAlso format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeParameters) Then
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

            If format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeOptionalBrackets) Then
                If symbol.IsOptional Then
                    AddPseudoPunctuation("[") ' There isn't a SyntaxKind for brackets in VB
                End If
            End If

            If format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeParamsRefOut) Then
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

            If format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeName) Then
                Dim kind = If(symbol.IsThis, SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.ParameterName)
                builder.Add(CreatePart(kind, symbol, symbol.Name, False))
            End If

            If format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeType) Then
                If format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeName) Then
                    AddSpace()
                    AddKeyword(SyntaxKind.AsKeyword)
                    AddSpace()
                End If

                symbol.Type.Accept(Me.NotFirstVisitor())
                AddCustomModifiersIfRequired(symbol.CustomModifiers)
            End If

            If format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeDefaultValue) AndAlso symbol.HasExplicitDefaultValue Then
                AddSpace()
                AddPunctuation(SyntaxKind.EqualsToken)
                AddSpace()

                AddConstantValue(symbol.Type, symbol.ExplicitDefaultValue)
            End If

            If format.ParameterOptions.IncludesOption(SymbolDisplayParameterOptions.IncludeOptionalBrackets) Then
                If symbol.IsOptional Then
                    AddPseudoPunctuation("]") ' There isn't a SyntaxKind for brackets in VB
                End If
            End If
        End Sub

        Private Sub AddCustomModifiersIfRequired(customModifiers As ImmutableArray(Of CustomModifier), Optional leadingSpace As Boolean = True, Optional trailingSpace As Boolean = False)
            If Me.format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers) AndAlso Not customModifiers.IsEmpty Then
                Const IL_KEYWORD_MODOPT = "modopt"
                Const IL_KEYWORD_MODREQ = "modreq"
                Dim first As Boolean = True

                For Each customModifier In customModifiers
                    If Not first OrElse leadingSpace Then
                        AddSpace()
                    End If

                    first = False

                    Me.builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, Nothing, If(customModifier.IsOptional, IL_KEYWORD_MODOPT, IL_KEYWORD_MODREQ), True))
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
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) AndAlso Not IsEnumMember(symbol) Then
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
            If format.MemberOptions.IncludesOption(SymbolDisplayMemberOptions.IncludeModifiers) AndAlso
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
            If format.ParameterOptions = SymbolDisplayParameterOptions.None Then
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
