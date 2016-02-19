' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class SourcePropertySymbol
        Inherits PropertySymbol
        Implements IAttributeTargetSymbol

        Private ReadOnly _containingType As SourceMemberContainerTypeSymbol
        Private ReadOnly _name As String
        Private _lazyMetadataName As String
        Private ReadOnly _syntaxRef As SyntaxReference
        Private ReadOnly _blockRef As SyntaxReference
        Private ReadOnly _location As Location
        Private ReadOnly _flags As SourceMemberFlags
        Private _lazyType As TypeSymbol
        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)
        Private _getMethod As MethodSymbol
        Private _setMethod As MethodSymbol
        Private _backingField As FieldSymbol
        Private _lazyDocComment As String
        Private _lazyMeParameter As ParameterSymbol

        ' Attributes on property. Set once after construction. IsNull means not set. 
        Private _lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ' Attributes on return type of the property. Set once after construction. IsNull means not set. 
        Private _lazyReturnTypeCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ' The explicitly implemented interface properties, or Empty if none.
        Private _lazyImplementedProperties As ImmutableArray(Of PropertySymbol)

        ' The overridden or hidden property.
        Private _lazyOverriddenProperties As OverriddenMembersResult(Of PropertySymbol)

        Private _lazyState As Integer

        <Flags>
        Private Enum StateFlags As Integer
            SymbolDeclaredEvent = &H1           ' Bit value for generating SymbolDeclaredEvent
        End Enum

        Private Sub New(container As SourceMemberContainerTypeSymbol,
                        name As String,
                        flags As SourceMemberFlags,
                        syntaxRef As SyntaxReference,
                        blockRef As SyntaxReference,
                        location As Location)

            Debug.Assert(container IsNot Nothing)
            Debug.Assert(syntaxRef IsNot Nothing)
            Debug.Assert(location IsNot Nothing)

            _containingType = container
            _name = name
            _syntaxRef = syntaxRef
            _blockRef = blockRef
            _location = location
            _flags = flags
            _lazyState = 0
        End Sub

        Friend Shared Function Create(containingType As SourceMemberContainerTypeSymbol,
                                      bodyBinder As Binder,
                                      syntax As PropertyStatementSyntax,
                                      blockSyntaxOpt As PropertyBlockSyntax,
                                      diagnostics As DiagnosticBag) As SourcePropertySymbol

            ' Decode the flags.
            Dim modifiers = DecodeModifiers(syntax.Modifiers,
                                            containingType,
                                            bodyBinder,
                                            diagnostics)
            Dim identifier = syntax.Identifier
            Dim name = identifier.ValueText

            Dim omitFurtherDiagnostics As Boolean = String.IsNullOrEmpty(name)
            Dim location = identifier.GetLocation()
            Dim syntaxRef = bodyBinder.GetSyntaxReference(syntax)
            Dim blockRef = If(blockSyntaxOpt Is Nothing, Nothing, bodyBinder.GetSyntaxReference(blockSyntaxOpt))

            Dim prop = New SourcePropertySymbol(containingType, name, modifiers.AllFlags, syntaxRef, blockRef, location)

            bodyBinder = New LocationSpecificBinder(BindingLocation.PropertySignature, prop, bodyBinder)

            If syntax.AttributeLists.Count = 0 Then
                prop.SetCustomAttributeData(CustomAttributesBag(Of VisualBasicAttributeData).Empty)
            End If

            Dim accessorFlags = modifiers.AllFlags And (Not SourceMemberFlags.AccessibilityMask)
            Dim getMethod As SourcePropertyAccessorSymbol = Nothing
            Dim setMethod As SourcePropertyAccessorSymbol = Nothing

            If blockSyntaxOpt IsNot Nothing Then
                For Each accessor In blockSyntaxOpt.Accessors
                    Dim accessorKind = accessor.BlockStatement.Kind
                    If accessorKind = SyntaxKind.GetAccessorStatement Then
                        Dim accessorMethod = CreateAccessor(prop, SourceMemberFlags.MethodKindPropertyGet, accessorFlags, bodyBinder, accessor, diagnostics)
                        If getMethod Is Nothing Then
                            getMethod = accessorMethod
                        Else
                            diagnostics.Add(ERRID.ERR_DuplicatePropertyGet, accessorMethod.Locations(0))
                        End If
                    ElseIf accessorKind = SyntaxKind.SetAccessorStatement Then
                        Dim accessorMethod = CreateAccessor(prop, SourceMemberFlags.MethodKindPropertySet, accessorFlags, bodyBinder, accessor, diagnostics)
                        If setMethod Is Nothing Then
                            setMethod = accessorMethod
                        Else
                            diagnostics.Add(ERRID.ERR_DuplicatePropertySet, accessorMethod.Locations(0))
                        End If
                    End If
                Next
            End If

            Dim isReadOnly = (modifiers.FoundFlags And SourceMemberFlags.ReadOnly) <> 0
            Dim isWriteOnly = (modifiers.FoundFlags And SourceMemberFlags.WriteOnly) <> 0

            If Not prop.IsMustOverride Then
                If isReadOnly Then
                    If getMethod IsNot Nothing Then
                        If getMethod.LocalAccessibility <> Accessibility.NotApplicable Then
                            diagnostics.Add(ERRID.ERR_ReadOnlyNoAccessorFlag, GetAccessorBlockBeginLocation(getMethod))
                        End If
                    ElseIf blockSyntaxOpt IsNot Nothing Then
                        diagnostics.Add(ERRID.ERR_ReadOnlyHasNoGet, location)
                    End If
                    If setMethod IsNot Nothing Then
                        diagnostics.Add(ERRID.ERR_ReadOnlyHasSet, setMethod.Locations(0))
                    End If
                End If

                If isWriteOnly Then
                    If setMethod IsNot Nothing Then
                        If setMethod.LocalAccessibility <> Accessibility.NotApplicable Then
                            diagnostics.Add(ERRID.ERR_WriteOnlyNoAccessorFlag, GetAccessorBlockBeginLocation(setMethod))
                        End If
                    ElseIf blockSyntaxOpt IsNot Nothing Then
                        diagnostics.Add(ERRID.ERR_WriteOnlyHasNoWrite, location)
                    End If
                    If getMethod IsNot Nothing Then
                        diagnostics.Add(ERRID.ERR_WriteOnlyHasGet, getMethod.Locations(0))
                    End If
                End If

                If (getMethod IsNot Nothing) AndAlso (setMethod IsNot Nothing) Then
                    If (getMethod.LocalAccessibility <> Accessibility.NotApplicable) AndAlso (setMethod.LocalAccessibility <> Accessibility.NotApplicable) Then
                        ' Both accessors have explicit accessibility. Report an error on the second.
                        Dim accessor = If(getMethod.Locations(0).SourceSpan.Start < setMethod.Locations(0).SourceSpan.Start, setMethod, getMethod)
                        diagnostics.Add(ERRID.ERR_OnlyOneAccessorForGetSet, GetAccessorBlockBeginLocation(accessor))
                    ElseIf prop.IsOverridable AndAlso
                        ((getMethod.LocalAccessibility = Accessibility.Private) OrElse (setMethod.LocalAccessibility = Accessibility.Private)) Then
                        ' If either accessor is Private, property cannot be Overridable.
                        bodyBinder.ReportModifierError(syntax.Modifiers, ERRID.ERR_BadPropertyAccessorFlags3, diagnostics, s_overridableModifierKinds)
                    End If
                End If

                If (Not isReadOnly) AndAlso
                    (Not isWriteOnly) AndAlso
                    ((getMethod Is Nothing) OrElse (setMethod Is Nothing)) AndAlso
                    (blockSyntaxOpt IsNot Nothing) AndAlso
                    (Not prop.IsMustOverride) Then
                    diagnostics.Add(ERRID.ERR_PropMustHaveGetSet, location)
                End If
            End If

            If blockSyntaxOpt Is Nothing Then
                ' Generate backing field for auto property.
                If Not prop.IsMustOverride Then
                    If isWriteOnly Then
                        diagnostics.Add(ERRID.ERR_AutoPropertyCantBeWriteOnly, location)
                    End If

                    Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

                    Dim fieldName = "_" + prop._name
                    prop._backingField = New SynthesizedPropertyBackingFieldSymbol(prop, fieldName, isShared:=prop.IsShared)
                End If

                Dim flags = prop._flags And Not SourceMemberFlags.MethodKindMask

                ' Generate accessors for auto property or abstract property.
                If Not isWriteOnly Then
                    prop._getMethod = New SourcePropertyAccessorSymbol(
                        prop,
                        Binder.GetAccessorName(prop.Name, MethodKind.PropertyGet, isWinMd:=False),
                        flags Or SourceMemberFlags.MethodKindPropertyGet,
                        prop._syntaxRef,
                        prop.Locations)
                End If

                If Not isReadOnly Then
                    prop._setMethod = New SourcePropertyAccessorSymbol(
                        prop,
                        Binder.GetAccessorName(prop.Name, MethodKind.PropertySet,
                                               isWinMd:=prop.IsCompilationOutputWinMdObj()),
                        flags Or SourceMemberFlags.MethodKindPropertySet Or SourceMemberFlags.MethodIsSub,
                        prop._syntaxRef,
                        prop.Locations)
                End If
            Else
                prop._getMethod = getMethod
                prop._setMethod = setMethod
            End If

            Debug.Assert((prop._getMethod Is Nothing) OrElse prop._getMethod.ImplicitlyDefinedBy Is prop)
            Debug.Assert((prop._setMethod Is Nothing) OrElse prop._setMethod.ImplicitlyDefinedBy Is prop)

            Return prop
        End Function

        Friend Shared Function CreateWithEvents(
                             containingType As SourceMemberContainerTypeSymbol,
                             bodyBinder As Binder,
                             identifier As SyntaxToken,
                             syntaxRef As SyntaxReference,
                             modifiers As MemberModifiers,
                             firstFieldDeclarationOfType As Boolean,
                             diagnostics As DiagnosticBag) As SourcePropertySymbol

            Dim name = identifier.ValueText

            ' we will require AccessedThroughPropertyAttribute
            bodyBinder.ReportUseSiteErrorForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_AccessedThroughPropertyAttribute__ctor,
                                                    DirectCast(identifier.Parent, VisualBasicSyntaxNode),
                                                    diagnostics)

            Dim omitFurtherDiagnostics As Boolean = String.IsNullOrEmpty(name)
            Dim location = identifier.GetLocation()

            ' WithEvents instance property is always overridable
            Dim memberFlags = modifiers.AllFlags
            If (memberFlags And SourceMemberFlags.Shared) = 0 Then
                memberFlags = modifiers.AllFlags Or SourceMemberFlags.Overridable
            End If

            If firstFieldDeclarationOfType Then
                memberFlags = memberFlags Or SourceMemberFlags.FirstFieldDeclarationOfType
            End If

            Dim prop = New SourcePropertySymbol(containingType,
                                                name,
                                                memberFlags,
                                                syntaxRef,
                                                Nothing,
                                                location)

            ' no implements.
            prop._lazyImplementedProperties = ImmutableArray(Of PropertySymbol).Empty
            prop.SetCustomAttributeData(CustomAttributesBag(Of VisualBasicAttributeData).Empty)

            Dim fieldName = "_" + prop._name
            prop._backingField = New SourceWithEventsBackingFieldSymbol(prop, syntaxRef, fieldName)

            ' Generate synthesized accessors for auto property or abstract property.
            prop._getMethod = New SynthesizedWithEventsGetAccessorSymbol(
                    containingType,
                    prop)

            prop._setMethod = New SynthesizedWithEventsSetAccessorSymbol(
                containingType,
                prop,
                bodyBinder.GetSpecialType(SpecialType.System_Void, identifier, diagnostics),
                valueParameterName:=StringConstants.WithEventsValueParameterName)

            Debug.Assert((prop._getMethod Is Nothing) OrElse prop._getMethod.ImplicitlyDefinedBy Is prop)
            Debug.Assert((prop._setMethod Is Nothing) OrElse prop._setMethod.ImplicitlyDefinedBy Is prop)

            Return prop
        End Function

        Friend Sub CloneParametersForAccessor(method As MethodSymbol, parameterBuilder As ArrayBuilder(Of ParameterSymbol))
            Dim overriddenMethod As MethodSymbol = method.OverriddenMethod

            For Each parameter In Me.Parameters
                Dim clone As ParameterSymbol = New SourceClonedParameterSymbol(DirectCast(parameter, SourceParameterSymbol), method, parameter.Ordinal)

                If overriddenMethod IsNot Nothing Then
                    CustomModifierUtils.CopyParameterCustomModifiers(overriddenMethod.Parameters(parameter.Ordinal), clone)
                End If

                parameterBuilder.Add(clone)
            Next
        End Sub

        ''' <summary> 
        ''' Property declaration syntax node. 
        ''' It is either PropertyStatement for normal properties or FieldDeclarationSyntax for WithEvents ones.
        ''' </summary>
        Friend ReadOnly Property DeclarationSyntax As DeclarationStatementSyntax
            Get
                Dim syntax = _syntaxRef.GetVisualBasicSyntax()
                If syntax.Kind = SyntaxKind.PropertyStatement Then
                    Return DirectCast(syntax, PropertyStatementSyntax)
                Else
                    Debug.Assert(syntax.Kind = SyntaxKind.ModifiedIdentifier)
                    Return DirectCast(syntax.Parent.Parent, FieldDeclarationSyntax)
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                EnsureSignature()
                Return _lazyType
            End Get
        End Property

        Private Function ComputeType(diagnostics As DiagnosticBag) As TypeSymbol
            Dim binder = CreateBinderForTypeDeclaration()

            If IsWithEvents Then
                Dim syntax = DirectCast(_syntaxRef.GetSyntax(), ModifiedIdentifierSyntax)
                Return SourceMemberFieldSymbol.ComputeWithEventsFieldType(
                    Me,
                    syntax,
                    binder,
                    ignoreTypeSyntaxDiagnostics:=(_flags And SourceMemberFlags.FirstFieldDeclarationOfType) = 0,
                    diagnostics:=diagnostics)

            Else
                Dim syntax = DirectCast(_syntaxRef.GetSyntax(), PropertyStatementSyntax)
                Dim asClause = syntax.AsClause

                If asClause IsNot Nothing AndAlso
                    asClause.Kind = SyntaxKind.AsNewClause AndAlso
                    (DirectCast(asClause, AsNewClauseSyntax).NewExpression.Kind = SyntaxKind.AnonymousObjectCreationExpression) Then
                    Return ErrorTypeSymbol.UnknownResultType
                Else
                    Dim getErrorInfo As Func(Of DiagnosticInfo) = Nothing

                    Dim omitFurtherDiagnostics As Boolean = String.IsNullOrEmpty(_name)
                    If Not omitFurtherDiagnostics Then
                        If binder.OptionStrict = OptionStrict.On Then
                            getErrorInfo = ErrorFactory.GetErrorInfo_ERR_StrictDisallowsImplicitProc
                        ElseIf binder.OptionStrict = OptionStrict.Custom Then
                            getErrorInfo = ErrorFactory.GetErrorInfo_WRN_ObjectAssumedProperty1_WRN_MissingAsClauseinProperty
                        End If
                    End If

                    Dim identifier = syntax.Identifier
                    Dim type = binder.DecodeIdentifierType(identifier, asClause, getErrorInfo, diagnostics)

                    Debug.Assert(type IsNot Nothing)
                    If Not type.IsErrorType() Then
                        Dim errorLocation = SourceSymbolHelpers.GetAsClauseLocation(identifier, asClause)
                        AccessCheck.VerifyAccessExposureForMemberType(Me, errorLocation, type, diagnostics)

                        Dim restrictedType As TypeSymbol = Nothing
                        If type.IsRestrictedTypeOrArrayType(restrictedType) Then
                            Binder.ReportDiagnostic(diagnostics, errorLocation, ERRID.ERR_RestrictedType1, restrictedType)
                        End If

                        Dim getMethod = Me.GetMethod

                        If getMethod IsNot Nothing AndAlso getMethod.IsIterator Then
                            Dim originalRetTypeDef = type.OriginalDefinition

                            If originalRetTypeDef.SpecialType <> SpecialType.System_Collections_Generic_IEnumerable_T AndAlso
                                        originalRetTypeDef.SpecialType <> SpecialType.System_Collections_Generic_IEnumerator_T AndAlso
                                        type.SpecialType <> SpecialType.System_Collections_IEnumerable AndAlso
                                        type.SpecialType <> SpecialType.System_Collections_IEnumerator Then
                                Binder.ReportDiagnostic(diagnostics, errorLocation, ERRID.ERR_BadIteratorReturn)
                            End If
                        End If

                    End If

                    Return type
                End If

            End If
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                If _lazyMetadataName Is Nothing Then
                    OverloadingHelper.SetMetadataNameForAllOverloads(_name, SymbolKind.Property, _containingType)
                    Debug.Assert(_lazyMetadataName IsNot Nothing)
                End If

                Return _lazyMetadataName
            End Get
        End Property

        Friend Overrides Sub SetMetadataName(metadataName As String)
            Dim old = Interlocked.CompareExchange(_lazyMetadataName, metadataName, Nothing)
            Debug.Assert(old Is Nothing OrElse old = metadataName)
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public ReadOnly Property ContainingSourceType As SourceMemberContainerTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            ' WARNING: this should not allocate memory!
            Return New LexicalSortKey(_location, Me.DeclaringCompilation)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(_location)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(_syntaxRef)
            End Get
        End Property

        Friend Overrides Function IsDefinedInSourceTree(tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            Dim propertyStatementSyntax = Me.Syntax
            Return propertyStatementSyntax IsNot Nothing AndAlso IsDefinedInSourceTree(propertyStatementSyntax.Parent, tree, definedWithinSpan, cancellationToken)
        End Function

        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Property
            End Get
        End Property

        Private Function GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            If Me.IsWithEvents Then
                Return Nothing
            End If

            Return OneOrMany.Create(DirectCast(_syntaxRef.GetSyntax, PropertyStatementSyntax).AttributeLists)
        End Function

        Private Function GetReturnTypeAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            If Me.IsWithEvents Then
                Return Nothing
            End If

            Dim asClauseOpt = DirectCast(Me.Syntax, PropertyStatementSyntax).AsClause
            If asClauseOpt Is Nothing Then
                Return Nothing
            End If

            Return OneOrMany.Create(asClauseOpt.Attributes)
        End Function

        Friend Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If _lazyCustomAttributesBag Is Nothing OrElse Not _lazyCustomAttributesBag.IsSealed Then
                LoadAndValidateAttributes(Me.GetAttributeDeclarations(), _lazyCustomAttributesBag)
            End If
            Return _lazyCustomAttributesBag
        End Function

        Friend Function GetReturnTypeAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If _lazyReturnTypeCustomAttributesBag Is Nothing OrElse Not _lazyReturnTypeCustomAttributesBag.IsSealed Then
                LoadAndValidateAttributes(GetReturnTypeAttributeDeclarations(), _lazyReturnTypeCustomAttributesBag, symbolPart:=AttributeLocation.Return)
            End If
            Return _lazyReturnTypeCustomAttributesBag
        End Function

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        ''' <remarks>
        ''' NOTE: This method should always be kept as a NotOverridable method.
        ''' If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        ''' </remarks>
        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Private Function GetDecodedWellKnownAttributeData() As CommonPropertyWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonPropertyWellKnownAttributeData)
        End Function

        Private Function GetDecodedReturnTypeWellKnownAttributeData() As CommonReturnTypeWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazyReturnTypeCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetReturnTypeAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonReturnTypeWellKnownAttributeData)
        End Function

        Friend Overrides Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Debug.Assert(arguments.AttributeType IsNot Nothing)
            Debug.Assert(Not arguments.AttributeType.IsErrorType())
            Dim boundAttribute As VisualBasicAttributeData = Nothing
            Dim obsoleteData As ObsoleteAttributeData = Nothing

            If EarlyDecodeDeprecatedOrObsoleteAttribute(arguments, boundAttribute, obsoleteData) Then
                If obsoleteData IsNot Nothing Then
                    arguments.GetOrCreateData(Of CommonPropertyEarlyWellKnownAttributeData)().ObsoleteAttributeData = obsoleteData
                End If

                Return boundAttribute
            End If

            Return MyBase.EarlyDecodeWellKnownAttribute(arguments)
        End Function

        Friend Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

            Dim attrData = arguments.Attribute
            If arguments.SymbolPart = AttributeLocation.Return Then
                Dim isMarshalAs = attrData.IsTargetAttribute(Me, AttributeDescription.MarshalAsAttribute)

                ' write-only property doesn't accept any return type attributes other than MarshalAs
                ' MarshalAs is applied on the "Value" parameter of the setter if the property has no parameters and the containing type is an interface .
                If _getMethod Is Nothing AndAlso _setMethod IsNot Nothing AndAlso
                    (Not isMarshalAs OrElse Not SynthesizedParameterSymbol.IsMarshalAsAttributeApplicable(_setMethod)) Then

                    arguments.Diagnostics.Add(ERRID.WRN_ReturnTypeAttributeOnWriteOnlyProperty, arguments.AttributeSyntaxOpt.GetLocation())
                    Return
                End If

                If isMarshalAs Then
                    MarshalAsAttributeDecoder(Of CommonReturnTypeWellKnownAttributeData, AttributeSyntax, VisualBasicAttributeData, AttributeLocation).
                        Decode(arguments, AttributeTargets.Field, MessageProvider.Instance)

                    Return
                End If
            Else
                If attrData.IsTargetAttribute(Me, AttributeDescription.SpecialNameAttribute) Then
                    arguments.GetOrCreateData(Of CommonPropertyWellKnownAttributeData).HasSpecialNameAttribute = True
                    Return
                ElseIf Not IsWithEvents AndAlso attrData.IsTargetAttribute(Me, AttributeDescription.DebuggerHiddenAttribute) Then
                    ' if neither getter or setter is marked by DebuggerHidden Dev11 reports a warning
                    If Not (_getMethod IsNot Nothing AndAlso DirectCast(_getMethod, SourcePropertyAccessorSymbol).HasDebuggerHiddenAttribute OrElse
                            _setMethod IsNot Nothing AndAlso DirectCast(_setMethod, SourcePropertyAccessorSymbol).HasDebuggerHiddenAttribute) Then
                        arguments.Diagnostics.Add(ERRID.WRN_DebuggerHiddenIgnoredOnProperties, arguments.AttributeSyntaxOpt.GetLocation())
                    End If
                    Return
                End If
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasSpecialNameAttribute
            End Get
        End Property

        Friend ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Dim data = GetDecodedReturnTypeWellKnownAttributeData()
                Return If(data IsNot Nothing, data.MarshallingInformation, Nothing)
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return (_flags And SourceMemberFlags.MustOverride) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return (_flags And SourceMemberFlags.NotOverridable) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return (_flags And SourceMemberFlags.Overridable) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return (_flags And SourceMemberFlags.Overrides) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (_flags And SourceMemberFlags.Shared) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Return (_flags And SourceMemberFlags.Default) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsWriteOnly As Boolean
            Get
                Return (_flags And SourceMemberFlags.WriteOnly) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return (_flags And SourceMemberFlags.ReadOnly) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return _getMethod
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return _setMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                If (_flags And SourceMemberFlags.Shadows) <> 0 Then
                    Return False
                ElseIf (_flags And SourceMemberFlags.Overloads) <> 0 Then
                    Return True
                Else
                    Return (_flags And SourceMemberFlags.Overrides) <> 0
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                Return (_flags And SourceMemberFlags.WithEvents) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return (_flags And SourceMemberFlags.Shadows) <> 0
            End Get
        End Property

        ''' <summary> True if 'Overloads' is explicitly specified in method's declaration </summary>
        Friend ReadOnly Property OverloadsExplicitly As Boolean
            Get
                Return (_flags And SourceMemberFlags.Overloads) <> 0
            End Get
        End Property

        ''' <summary> True if 'Overrides' is explicitly specified in method's declaration </summary>
        Friend ReadOnly Property OverridesExplicitly As Boolean
            Get
                Return (_flags And SourceMemberFlags.Overrides) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return (If(IsShared, Microsoft.Cci.CallingConvention.Default, Microsoft.Cci.CallingConvention.HasThis))
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                EnsureSignature()
                Return _lazyParameters
            End Get
        End Property

        Private Sub EnsureSignature()
            If _lazyParameters.IsDefault Then
                Dim diagnostics = DiagnosticBag.GetInstance()
                Dim sourceModule = DirectCast(ContainingModule, SourceModuleSymbol)

                Dim params As ImmutableArray(Of ParameterSymbol) = ComputeParameters(diagnostics)
                Dim retType As TypeSymbol = ComputeType(diagnostics)

                ' For an overriding property, we need to copy custom modifiers from the property we override.
                Dim overriddenMembers As OverriddenMembersResult(Of PropertySymbol)

                If Not Me.IsOverrides OrElse Not OverrideHidingHelper.CanOverrideOrHide(Me) Then
                    overriddenMembers = OverriddenMembersResult(Of PropertySymbol).Empty
                Else
                    ' Since we cannot expose parameters and return type to the outside world yet,
                    ' let's create a fake symbol to use for overriding resolution
                    Dim fakeParamsBuilder = ArrayBuilder(Of ParameterSymbol).GetInstance(params.Length)
                    For Each param As ParameterSymbol In params
                        fakeParamsBuilder.Add(New SignatureOnlyParameterSymbol(
                                                param.Type,
                                                ImmutableArray(Of CustomModifier).Empty,
                                                defaultConstantValue:=Nothing,
                                                isParamArray:=False,
                                                isByRef:=param.IsByRef,
                                                isOut:=False,
                                                isOptional:=param.IsOptional))
                    Next

                    overriddenMembers = OverrideHidingHelper(Of PropertySymbol).
                        MakeOverriddenMembers(New SignatureOnlyPropertySymbol(Me.Name, _containingType,
                                                                            Me.IsReadOnly, Me.IsWriteOnly,
                                                                            fakeParamsBuilder.ToImmutableAndFree(),
                                                                            returnsByRef:=False,
                                                                            [type]:=retType,
                                                                            typeCustomModifiers:=ImmutableArray(Of CustomModifier).Empty,
                                                                            isOverrides:=True, isWithEvents:=Me.IsWithEvents))
                End If

                Debug.Assert(IsDefinition)
                Dim overridden = overriddenMembers.OverriddenMember

                If overridden IsNot Nothing Then
                    ' Copy custom modifiers
                    Dim returnTypeWithCustomModifiers As TypeSymbol = overridden.Type

                    ' We do an extra check before copying the return type to handle the case where the overriding
                    ' property (incorrectly) has a different return type than the overridden property.  In such cases,
                    ' we want to retain the original (incorrect) return type to avoid hiding the return type
                    ' given in source.
                    If retType.IsSameTypeIgnoringCustomModifiers(returnTypeWithCustomModifiers) Then
                        retType = returnTypeWithCustomModifiers
                    End If

                    params = CustomModifierUtils.CopyParameterCustomModifiers(overridden.Parameters, params)
                End If

                ' Unlike PropertySymbol, in SourcePropertySymbol we cache the result of MakeOverriddenOfHiddenMembers, because we use
                ' it heavily while validating methods and emitting.
                Interlocked.CompareExchange(_lazyOverriddenProperties, overriddenMembers, Nothing)

                Interlocked.CompareExchange(_lazyType, retType, Nothing)

                sourceModule.AtomicStoreArrayAndDiagnostics(
                    _lazyParameters,
                    params,
                    diagnostics,
                    CompilationStage.Declare)

                diagnostics.Free()
            End If
        End Sub

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                If Not Me._lazyParameters.IsDefault Then
                    Return Me._lazyParameters.Length
                End If

                Dim decl = Me.DeclarationSyntax

                If decl.Kind = SyntaxKind.PropertyStatement Then
                    Dim paramList As ParameterListSyntax = DirectCast(decl, PropertyStatementSyntax).ParameterList
                    Return If(paramList Is Nothing, 0, paramList.Parameters.Count)
                End If

                Return MyBase.ParameterCount
            End Get
        End Property

        Private Function ComputeParameters(diagnostics As DiagnosticBag) As ImmutableArray(Of ParameterSymbol)

            If Me.IsWithEvents Then
                ' no parameters
                Return ImmutableArray(Of ParameterSymbol).Empty
            End If

            Dim binder = CreateBinderForTypeDeclaration()
            Dim syntax = DirectCast(_syntaxRef.GetSyntax(), PropertyStatementSyntax)
            Dim parameters = binder.DecodePropertyParameterList(Me, syntax.ParameterList, diagnostics)

            If IsDefault Then
                ' Default properties must have required parameters.
                If Not HasRequiredParameters(parameters) Then
                    diagnostics.Add(ERRID.ERR_DefaultPropertyWithNoParams, _location)
                End If

                ' 'touch' System_Reflection_DefaultMemberAttribute__ctor to make sure all diagnostics are reported
                binder.ReportUseSiteErrorForSynthesizedAttribute(WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor,
                                                                     syntax,
                                                                     diagnostics)
            End If

            Return parameters
        End Function

        Friend Overrides ReadOnly Property MeParameter As ParameterSymbol
            Get
                If IsShared Then
                    Return Nothing
                Else
                    If _lazyMeParameter Is Nothing Then
                        Interlocked.CompareExchange(Of ParameterSymbol)(_lazyMeParameter, New MeParameterSymbol(Me), Nothing)
                    End If

                    Return _lazyMeParameter
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                If _lazyImplementedProperties.IsDefault Then
                    Dim diagnostics = DiagnosticBag.GetInstance()
                    Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
                    sourceModule.AtomicStoreArrayAndDiagnostics(_lazyImplementedProperties,
                                                                ComputeExplicitInterfaceImplementations(diagnostics),
                                                                diagnostics,
                                                                CompilationStage.Declare)
                    diagnostics.Free()
                End If

                Return _lazyImplementedProperties
            End Get
        End Property

        Private Function ComputeExplicitInterfaceImplementations(diagnostics As DiagnosticBag) As ImmutableArray(Of PropertySymbol)
            Dim binder = CreateBinderForTypeDeclaration()
            Dim syntax = DirectCast(_syntaxRef.GetSyntax(), PropertyStatementSyntax)
            Return BindImplementsClause(_containingType, binder, Me, syntax, diagnostics)
        End Function

        ''' <summary>
        ''' Helper method for accessors to get the overridden accessor methods. Should only be called by the
        ''' accessor method symbols.
        ''' </summary>
        ''' <param name="getter">True to get implemented getters, False to get implemented setters</param>
        ''' <returns>All the accessors of the given kind implemented by this property.</returns>
        Friend Function GetAccessorImplementations(getter As Boolean) As ImmutableArray(Of MethodSymbol)
            Dim implementedProperties = ExplicitInterfaceImplementations
            Debug.Assert(Not implementedProperties.IsDefault)

            If implementedProperties.IsEmpty Then
                Return ImmutableArray(Of MethodSymbol).Empty
            Else
                Dim builder As ArrayBuilder(Of MethodSymbol) = ArrayBuilder(Of MethodSymbol).GetInstance()

                For Each implementedProp In implementedProperties
                    Dim accessor = If(getter, implementedProp.GetMethod, implementedProp.SetMethod)
                    If accessor IsNot Nothing Then
                        builder.Add(accessor)
                    End If
                Next

                Return builder.ToImmutableAndFree()
            End If
        End Function

        Friend Overrides ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of PropertySymbol)
            Get
                EnsureSignature()
                Return Me._lazyOverriddenProperties
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Dim overridden = Me.OverriddenProperty

                If overridden Is Nothing Then
                    Return ImmutableArray(Of CustomModifier).Empty
                Else
                    Return overridden.TypeCustomModifiers
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return CType((_flags And SourceMemberFlags.AccessibilityMask), Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _containingType.AreMembersImplicitlyDeclared
            End Get
        End Property

        Friend ReadOnly Property IsCustomProperty As Boolean
            Get
                ' Auto and WithEvents properties have backing fields
                Return _backingField Is Nothing AndAlso Not IsMustOverride
            End Get
        End Property

        Friend ReadOnly Property IsAutoProperty As Boolean
            Get
                Return Not IsWithEvents AndAlso _backingField IsNot Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return _backingField
            End Get
        End Property

        ''' <summary> 
        ''' Property declaration syntax node. 
        ''' It is either PropertyStatement for normal properties or ModifiedIdentifier for WithEvents ones.
        ''' </summary>
        Friend ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return If(_syntaxRef IsNot Nothing, _syntaxRef.GetVisualBasicSyntax(), Nothing)
            End Get
        End Property

        Friend ReadOnly Property SyntaxReference As SyntaxReference
            Get
                Return Me._syntaxRef
            End Get
        End Property

        Friend ReadOnly Property BlockSyntaxReference As SyntaxReference
            Get
                Return Me._blockRef
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ' If there are no attributes then this symbol is not Obsolete.
                If (Not Me._containingType.AnyMemberHasAttributes) Then
                    Return Nothing
                End If

                Dim lazyCustomAttributesBag = Me._lazyCustomAttributesBag
                If (lazyCustomAttributesBag IsNot Nothing AndAlso lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed) Then
                    Dim data = DirectCast(_lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData, CommonPropertyEarlyWellKnownAttributeData)
                    Return If(data IsNot Nothing, data.ObsoleteAttributeData, Nothing)
                End If

                Return ObsoleteAttributeData.Uninitialized
            End Get
        End Property

        ' Get the location of the implements name for an explicit implemented property, for later error reporting.
        Friend Function GetImplementingLocation(implementedProperty As PropertySymbol) As Location
            Debug.Assert(ExplicitInterfaceImplementations.Contains(implementedProperty))

            Dim propertySyntax = TryCast(_syntaxRef.GetSyntax(), PropertyStatementSyntax)
            If propertySyntax IsNot Nothing AndAlso propertySyntax.ImplementsClause IsNot Nothing Then
                Dim binder = CreateBinderForTypeDeclaration()
                Dim implementingSyntax = FindImplementingSyntax(Of PropertySymbol)(propertySyntax.ImplementsClause,
                                                                                 Me,
                                                                                 implementedProperty,
                                                                                 _containingType,
                                                                                 binder)
                Return implementingSyntax.GetLocation()
            End If

            Return If(Locations.FirstOrDefault(), NoLocation.Singleton)
        End Function

        Private Function CreateBinderForTypeDeclaration() As Binder
            Dim binder = BinderBuilder.CreateBinderForType(DirectCast(ContainingModule, SourceModuleSymbol), _syntaxRef.SyntaxTree, _containingType)
            Return New LocationSpecificBinder(BindingLocation.PropertySignature, Me, binder)
        End Function

        Private Shared Function CreateAccessor([property] As SourcePropertySymbol,
                                               kindFlags As SourceMemberFlags,
                                               propertyFlags As SourceMemberFlags,
                                               bodyBinder As Binder,
                                               syntax As AccessorBlockSyntax,
                                               diagnostics As DiagnosticBag) As SourcePropertyAccessorSymbol

            Dim accessor = SourcePropertyAccessorSymbol.CreatePropertyAccessor([property], kindFlags, propertyFlags, bodyBinder, syntax, diagnostics)
            Debug.Assert(accessor IsNot Nothing)
            Dim localAccessibility = accessor.LocalAccessibility

            If Not IsAccessibilityMoreRestrictive([property].DeclaredAccessibility, localAccessibility) Then
                ReportAccessorAccessibilityError(bodyBinder, syntax, ERRID.ERR_BadPropertyAccessorFlagsRestrict, diagnostics)
            ElseIf [property].IsNotOverridable AndAlso localAccessibility = Accessibility.Private Then
                ReportAccessorAccessibilityError(bodyBinder, syntax, ERRID.ERR_BadPropertyAccessorFlags1, diagnostics)
            ElseIf [property].IsDefault AndAlso localAccessibility = Accessibility.Private Then
                ReportAccessorAccessibilityError(bodyBinder, syntax, ERRID.ERR_BadPropertyAccessorFlags2, diagnostics)
            End If

            Return accessor
        End Function

        ''' <summary>
        ''' Return true if the accessor accessibility is more restrictive
        ''' than the property accessibility, otherwise false.
        ''' </summary>
        Private Shared Function IsAccessibilityMoreRestrictive([property] As Accessibility, accessor As Accessibility) As Boolean
            If accessor = Accessibility.NotApplicable Then
                Return True
            End If

            Return (accessor < [property]) AndAlso ((accessor <> Accessibility.Protected) OrElse ([property] <> Accessibility.Friend))
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            If _lazyDocComment Is Nothing Then
                ' NOTE: replace Nothing with empty comment
                Interlocked.CompareExchange(
                    _lazyDocComment, GetDocumentationCommentForSymbol(Me, preferredCulture, expandIncludes, cancellationToken), Nothing)
            End If

            Return _lazyDocComment
        End Function

        Private Sub CopyPropertyCustomModifiers(propertyWithCustomModifiers As PropertySymbol, ByRef type As TypeSymbol, ByRef typeCustomModifiers As ImmutableArray(Of CustomModifier))
            Debug.Assert(propertyWithCustomModifiers IsNot Nothing)
            typeCustomModifiers = propertyWithCustomModifiers.TypeCustomModifiers
            Dim overriddenPropertyType As TypeSymbol = propertyWithCustomModifiers.Type
            If type.IsSameTypeIgnoringCustomModifiers(overriddenPropertyType) Then
                type = overriddenPropertyType
            End If
        End Sub

        Private Shared Function DecodeModifiers(modifiers As SyntaxTokenList,
                                                container As SourceMemberContainerTypeSymbol,
                                                binder As Binder,
                                                diagBag As DiagnosticBag) As MemberModifiers
            ' Decode the flags.
            Dim propertyModifiers = binder.DecodeModifiers(modifiers,
                SourceMemberFlags.AllAccessibilityModifiers Or
                SourceMemberFlags.Default Or
                SourceMemberFlags.Overloads Or
                SourceMemberFlags.Shadows Or
                SourceMemberFlags.Shared Or
                SourceMemberFlags.Overridable Or
                SourceMemberFlags.NotOverridable Or
                SourceMemberFlags.Overrides Or
                SourceMemberFlags.MustOverride Or
                SourceMemberFlags.ReadOnly Or
                SourceMemberFlags.Iterator Or
                SourceMemberFlags.WriteOnly,
                ERRID.ERR_BadPropertyFlags1,
                Accessibility.Public,
                diagBag)

            ' Diagnose Shared with an overriding modifier.
            Dim flags = propertyModifiers.FoundFlags

            If (flags And SourceMemberFlags.Default) <> 0 AndAlso (flags And SourceMemberFlags.InvalidIfDefault) <> 0 Then
                binder.ReportModifierError(modifiers, ERRID.ERR_BadFlagsWithDefault1, diagBag, InvalidModifiersIfDefault)
                flags = flags And Not SourceMemberFlags.InvalidIfDefault
                propertyModifiers = New MemberModifiers(flags, propertyModifiers.ComputedFlags)
            End If

            propertyModifiers = binder.ValidateSharedPropertyAndMethodModifiers(modifiers, propertyModifiers, True, container, diagBag)

            Return propertyModifiers
        End Function

        Private Shared Function BindImplementsClause(containingType As SourceMemberContainerTypeSymbol,
                                                     bodyBinder As Binder,
                                                     prop As SourcePropertySymbol,
                                                     syntax As PropertyStatementSyntax,
                                                     diagnostics As DiagnosticBag) As ImmutableArray(Of PropertySymbol)
            If syntax.ImplementsClause IsNot Nothing Then
                If prop.IsShared And Not containingType.IsModuleType Then
                    ' Implementing with shared methods is illegal.
                    ' Module case is caught inside ProcessImplementsClause and has different message.
                    Binder.ReportDiagnostic(diagnostics,
                                            syntax.Modifiers.First(SyntaxKind.SharedKeyword),
                                            ERRID.ERR_SharedOnProcThatImpl,
                                            syntax.Identifier.ToString())
                Else
                    Return ProcessImplementsClause(Of PropertySymbol)(syntax.ImplementsClause,
                                                                      prop,
                                                                      containingType,
                                                                      bodyBinder,
                                                                      diagnostics)
                End If
            End If

            Return ImmutableArray(Of PropertySymbol).Empty
        End Function

        ''' <summary>
        ''' Returns the location (span) of the accessor begin block.
        ''' (Used for consistency with the native compiler that
        ''' highlights the entire begin block for certain diagnostics.)
        ''' </summary>
        Private Shared Function GetAccessorBlockBeginLocation(accessor As SourcePropertyAccessorSymbol) As Location
            Dim syntaxTree = accessor.SyntaxTree
            Dim block = DirectCast(accessor.BlockSyntax, AccessorBlockSyntax)
            Debug.Assert(syntaxTree IsNot Nothing)
            Debug.Assert(block IsNot Nothing)
            Debug.Assert(block.BlockStatement IsNot Nothing)
            Return syntaxTree.GetLocation(block.BlockStatement.Span)
        End Function

        Private Shared ReadOnly s_overridableModifierKinds() As SyntaxKind =
            {
                SyntaxKind.OverridableKeyword
            }

        Private Shared ReadOnly s_accessibilityModifierKinds() As SyntaxKind =
            {
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.FriendKeyword,
                SyntaxKind.PublicKeyword
            }

        ''' <summary>
        ''' Report an error associated with the accessor accessibility modifier.
        ''' </summary>
        Private Shared Sub ReportAccessorAccessibilityError(binder As Binder,
                                                            syntax As AccessorBlockSyntax,
                                                            errorId As ERRID,
                                                            diagnostics As DiagnosticBag)
            binder.ReportModifierError(syntax.BlockStatement.Modifiers, errorId, diagnostics, s_accessibilityModifierKinds)
        End Sub

        Private Shared Function HasRequiredParameters(parameters As ImmutableArray(Of ParameterSymbol)) As Boolean
            For Each parameter In parameters
                If Not parameter.IsOptional AndAlso Not parameter.IsParamArray Then
                    Return True
                End If
            Next
            Return False
        End Function

        ''' <summary>
        ''' Gets the syntax tree.
        ''' </summary>
        Friend ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _syntaxRef.SyntaxTree
            End Get
        End Property

        ' This should be called at most once after the attributes are bound.  Attributes must be bound after the class
        ' and members are fully declared to avoid infinite recursion.
        Private Sub SetCustomAttributeData(attributeData As CustomAttributesBag(Of VisualBasicAttributeData))
            Debug.Assert(attributeData IsNot Nothing)
            Debug.Assert(_lazyCustomAttributesBag Is Nothing)

            _lazyCustomAttributesBag = attributeData
        End Sub

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            MyBase.GenerateDeclarationErrors(cancellationToken)

            ' Ensure return type attributes are bound
            Dim unusedType = Me.Type
            Dim unusedParameters = Me.Parameters
            Me.GetReturnTypeAttributesBag()
            Dim unusedImplementations = Me.ExplicitInterfaceImplementations

            If DeclaringCompilation.EventQueue IsNot Nothing Then
                DirectCast(Me.ContainingModule, SourceModuleSymbol).AtomicSetFlagAndRaiseSymbolDeclaredEvent(_lazyState, StateFlags.SymbolDeclaredEvent, 0, Me)
            End If
        End Sub

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace

