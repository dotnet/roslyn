' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SourcePropertyAccessorSymbol
        Inherits SourceMethodSymbol

        Protected ReadOnly m_property As SourcePropertySymbol
        Private ReadOnly _name As String
        Private _lazyMetadataName As String

        Private _lazyExplicitImplementations As ImmutableArray(Of MethodSymbol) ' lazily populated with explicit implementations

        ' Parameters.
        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

        ' Return type. Void for a Sub.
        Private _lazyReturnType As TypeSymbol

        Friend Sub New(propertySymbol As SourcePropertySymbol,
                       name As String,
                       flags As SourceMemberFlags,
                       syntaxRef As SyntaxReference,
                       locations As ImmutableArray(Of Location))

            MyBase.New(
                propertySymbol.ContainingSourceType,
                If(flags.ToMethodKind() = MethodKind.PropertyGet, flags, flags And Not SourceMemberFlags.Iterator),
                syntaxRef,
                locations)

            m_property = propertySymbol
            _name = name
        End Sub

        Private Shared Function SynthesizeAutoGetterParameters(getter As SourcePropertyAccessorSymbol, propertySymbol As SourcePropertySymbol) As ImmutableArray(Of ParameterSymbol)
            If propertySymbol.ParameterCount = 0 Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            End If

            Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance(propertySymbol.ParameterCount)
            propertySymbol.CloneParametersForAccessor(getter, parameters)
            Return parameters.ToImmutableAndFree()
        End Function

        Private Shared Function SynthesizeAutoSetterParameters(setter As SourcePropertyAccessorSymbol, propertySymbol As SourcePropertySymbol) As ImmutableArray(Of ParameterSymbol)
            Dim valueParameter = SynthesizedParameterSymbol.CreateSetAccessorValueParameter(
                setter,
                propertySymbol,
                If(propertySymbol.IsAutoProperty, StringConstants.AutoPropertyValueParameterName, StringConstants.ValueParameterName))

            If propertySymbol.ParameterCount = 0 Then
                Return ImmutableArray.Create(valueParameter)
            End If

            Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance(propertySymbol.ParameterCount + 1)
            propertySymbol.CloneParametersForAccessor(setter, parameters)
            parameters.Add(valueParameter)
            Return parameters.ToImmutableAndFree()
        End Function

        Friend Shared Function CreatePropertyAccessor(propertySymbol As SourcePropertySymbol,
                                                      kindFlags As SourceMemberFlags,
                                                      propertyFlags As SourceMemberFlags,
                                                      binder As Binder,
                                                      blockSyntax As AccessorBlockSyntax,
                                                      diagnostics As DiagnosticBag) As SourcePropertyAccessorSymbol

            Dim syntax = blockSyntax.BlockStatement
            Dim modifiers = binder.DecodeModifiers(syntax.Modifiers,
                                                   SourceMemberFlags.AllAccessibilityModifiers,
                                                   ERRID.ERR_BadPropertyAccessorFlags,
                                                   Accessibility.NotApplicable,
                                                   diagnostics)

            If (modifiers.FoundFlags And SourceMemberFlags.Private) <> 0 Then
                ' Private accessors cannot be overridable.
                propertyFlags = propertyFlags And (Not SourceMemberFlags.Overridable)
            End If

            If (modifiers.FoundFlags And SourceMemberFlags.Protected) <> 0 Then
                Select Case propertySymbol.ContainingType.TypeKind
                    Case TypeKind.Structure
                        binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_StructCantUseVarSpecifier1, diagnostics, SyntaxKind.ProtectedKeyword)
                        modifiers = New MemberModifiers(modifiers.FoundFlags And Not SourceMemberFlags.Protected,
                                                        modifiers.ComputedFlags And Not SourceMemberFlags.AccessibilityMask)

                    Case TypeKind.Module
                        Debug.Assert((SourceMemberFlags.Protected And SourceMemberFlags.InvalidInModule) <> 0)
                        binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_BadFlagsOnStdModuleProperty1, diagnostics, SyntaxKind.ProtectedKeyword)
                End Select
            End If

            ' Include modifiers from the containing property.
            Dim flags = modifiers.AllFlags Or kindFlags Or propertyFlags
            Dim methodKind = kindFlags.ToMethodKind()
            If methodKind = MethodKind.PropertySet Then
                flags = flags Or SourceMemberFlags.MethodIsSub
            End If

            Dim method As New SourcePropertyAccessorSymbol(
                propertySymbol,
                Binder.GetAccessorName(propertySymbol.Name, methodKind, propertySymbol.IsCompilationOutputWinMdObj()),
                flags,
                binder.GetSyntaxReference(syntax),
                ImmutableArray.Create(syntax.DeclarationKeyword.GetLocation()))

            Return method
        End Function

        Public Overrides ReadOnly Property OverriddenMethod As MethodSymbol
            Get
                Return m_property.GetAccessorOverride(getter:=(MethodKind = MethodKind.PropertyGet))
            End Get
        End Property

        Friend Overrides ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of MethodSymbol)
            Get
                Return OverriddenMembersResult(Of MethodSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Not Me.m_property.IsCustomProperty OrElse MyBase.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return If(Me.m_property.IsCustomProperty, MyBase.DeclaringSyntaxReferences, ImmutableArray(Of SyntaxReference).Empty)
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                If _lazyMetadataName Is Nothing Then
                    ' VB compiler uses different rules for accessors that other members or the associated properties
                    ' (probably a bug, but we have to maintain binary compatibility now). An accessor name is set to match
                    ' its overridden method, regardless of what happens to its associated property.
                    Dim overriddenMethod = Me.OverriddenMethod
                    If overriddenMethod IsNot Nothing Then
                        Interlocked.CompareExchange(_lazyMetadataName, overriddenMethod.MetadataName, Nothing)
                    Else
                        Interlocked.CompareExchange(_lazyMetadataName, _name, Nothing)
                    End If
                End If

                Return _lazyMetadataName
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Dim accessibility = Me.LocalAccessibility
                If accessibility <> Accessibility.NotApplicable Then
                    Return accessibility
                End If

                Dim propertyAccessibility = m_property.DeclaredAccessibility
                Debug.Assert(propertyAccessibility <> Accessibility.NotApplicable)
                Return propertyAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Dim retType = _lazyReturnType
                If retType Is Nothing Then

                    Dim diagBag = BindingDiagnosticBag.GetInstance()
                    Dim sourceModule = ContainingSourceModule
                    Dim errorLocation As SyntaxNodeOrToken = Nothing
                    retType = GetReturnType(sourceModule, errorLocation, diagBag)

                    If Not errorLocation.IsKind(SyntaxKind.None) Then
                        Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                        Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing

                        retType.CheckAllConstraints(
                            DeclaringCompilation.LanguageVersion,
                            diagnosticsBuilder, useSiteDiagnosticsBuilder, template:=New CompoundUseSiteInfo(Of AssemblySymbol)(diagBag, sourceModule.ContainingAssembly))

                        If useSiteDiagnosticsBuilder IsNot Nothing Then
                            diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
                        End If

                        For Each diag In diagnosticsBuilder
                            diagBag.Add(diag.UseSiteInfo, errorLocation.GetLocation())
                        Next
                        diagnosticsBuilder.Free()
                    End If

                    sourceModule.AtomicStoreReferenceAndDiagnostics(
                        _lazyReturnType,
                        retType,
                        diagBag)

                    diagBag.Free()

                    retType = _lazyReturnType
                End If

                Return retType
            End Get
        End Property

        Private Function GetReturnType(sourceModule As SourceModuleSymbol,
                                       ByRef errorLocation As SyntaxNodeOrToken,
                                       diagBag As BindingDiagnosticBag) As TypeSymbol
            Select Case MethodKind
                Case MethodKind.PropertyGet
                    Dim accessorSym = DirectCast(Me, SourcePropertyAccessorSymbol)
                    Dim prop = DirectCast(accessorSym.AssociatedSymbol, PropertySymbol)

                    Dim result = prop.Type

                    Dim overriddenMethod = Me.OverriddenMethod
                    If overriddenMethod IsNot Nothing AndAlso overriddenMethod.ReturnType.IsSameTypeIgnoringAll(result) Then
                        result = overriddenMethod.ReturnType
                    End If

                    Return result

                Case MethodKind.PropertySet
                    Debug.Assert(Me.IsSub)
                    Dim binder As Binder = BinderBuilder.CreateBinderForType(sourceModule, Me.SyntaxTree, Me.m_property.ContainingSourceType)
                    Return binder.GetSpecialType(SpecialType.System_Void, Me.DeclarationSyntax, diagBag)

                Case Else
                    Throw ExceptionUtilities.Unreachable()
            End Select
        End Function

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Dim params = _lazyParameters
                If params.IsDefault Then

                    Dim diagBag = BindingDiagnosticBag.GetInstance()
                    Dim sourceModule = ContainingSourceModule

                    params = GetParameters(sourceModule, diagBag)

                    For Each param In params
                        ' TODO: The check for Locations is to rule out cases such as implicit parameters
                        ' from property accessors but it allows explicit accessor parameters. Is that correct?
                        If param.Locations.Length > 0 Then
                            ' Note: Errors are reported on the parameter name. Ideally, we should
                            ' match Dev10 and report errors on the parameter type syntax instead.
                            param.Type.CheckAllConstraints(
                                DeclaringCompilation.LanguageVersion,
                                param.GetFirstLocation(), diagBag, template:=New CompoundUseSiteInfo(Of AssemblySymbol)(diagBag, sourceModule.ContainingAssembly))
                        End If
                    Next

                    sourceModule.AtomicStoreArrayAndDiagnostics(
                        _lazyParameters,
                        params,
                        diagBag)

                    diagBag.Free()

                    params = _lazyParameters
                End If

                Return params
            End Get
        End Property

        Private Function GetParameters(sourceModule As SourceModuleSymbol, diagBag As BindingDiagnosticBag) As ImmutableArray(Of ParameterSymbol)
            If m_property.IsCustomProperty Then
                Dim binder As Binder = BinderBuilder.CreateBinderForType(sourceModule, Me.SyntaxTree, Me.m_property.ContainingSourceType)
                binder = New LocationSpecificBinder(BindingLocation.PropertyAccessorSignature, Me, binder)

                Return BindParameters(Me.m_property, Me, Me.Locations.FirstOrDefault, binder, BlockSyntax.BlockStatement.ParameterList, diagBag)
            Else
                ' synthesize parameters for auto-properties and abstract properties
                Return If(MethodKind = MethodKind.PropertyGet,
                                        SynthesizeAutoGetterParameters(Me, m_property),
                                        SynthesizeAutoSetterParameters(Me, m_property))
            End If
        End Function

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return m_property
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return m_property.ShadowsExplicitly
            End Get
        End Property '

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return If(m_property.IsCustomProperty, MyBase.GetLexicalSortKey(), m_property.GetLexicalSortKey())
        End Function

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If _lazyExplicitImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(
                        _lazyExplicitImplementations,
                        m_property.GetAccessorImplementations(getter:=(MethodKind = MethodKind.PropertyGet)),
                        Nothing)
                End If

                Return _lazyExplicitImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Dim overriddenMethod = Me.OverriddenMethod
                If overriddenMethod IsNot Nothing Then
                    Return overriddenMethod.ReturnTypeCustomModifiers
                End If

                Return If(Me.MethodKind = MethodKind.PropertySet, ImmutableArray(Of CustomModifier).Empty, m_property.TypeCustomModifiers)
            End Get
        End Property

        Protected Overrides Function GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            If m_property.IsCustomProperty Then
                Return OneOrMany.Create(AttributeDeclarationSyntaxList)
            Else
                Return Nothing
            End If
        End Function

        Protected Overrides Function GetReturnTypeAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            ' getter return type attributes should be copied from the property return type attributes
            Debug.Assert(Me.MethodKind = MethodKind.PropertySet)
            Return Nothing
        End Function

        Protected Overrides ReadOnly Property BoundReturnTypeAttributesSource As SourcePropertySymbol
            Get
                Return If(Me.MethodKind = MethodKind.PropertyGet, m_property, Nothing)
            End Get
        End Property

        Friend ReadOnly Property LocalAccessibility As Accessibility
            Get
                Return MyBase.DeclaredAccessibility
            End Get
        End Property

        ''' <summary>
        ''' Bind parameters declared on the accessor and combine with any
        ''' parameters declared on the property. If there are no explicit parameters
        ''' and this is a setter, create a synthesized value parameter.
        ''' </summary>
        Private Shared Function BindParameters(propertySymbol As SourcePropertySymbol,
                                               method As SourcePropertyAccessorSymbol,
                                               location As Location,
                                               binder As Binder,
                                               parameterListOpt As ParameterListSyntax,
                                               diagnostics As BindingDiagnosticBag) As ImmutableArray(Of ParameterSymbol)
            Dim propertyParameters = propertySymbol.Parameters
            Dim nPropertyParameters = propertyParameters.Length
            Dim isSetter As Boolean = (method.MethodKind = MethodKind.PropertySet)
            Dim parameterListSyntax = If(parameterListOpt Is Nothing OrElse Not isSetter, Nothing, parameterListOpt.Parameters)
            Dim synthesizeParameter = isSetter AndAlso (parameterListSyntax.Count = 0)
            Dim nParameters = nPropertyParameters + parameterListSyntax.Count + If(synthesizeParameter, 1, 0)
            Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance(nParameters)

            propertySymbol.CloneParametersForAccessor(method, parameters)

            If parameterListSyntax.Count > 0 Then
                ' Explicit accessor parameters. Bind all parameters (even though at most one
                ' parameter was expected), to ensure all diagnostics are generated and
                ' ensure parameter symbols are available for binding the method body.
                binder.DecodeParameterList(
                    method,
                    False,
                    SourceMemberFlags.None,
                    parameterListSyntax,
                    parameters,
                    s_checkParameterModifierCallback,
                    diagnostics)

                ' Check for duplicate parameter names across accessor (setter) and property.
                ' It is only necessary to check the one expected setter parameter since we'll report
                ' setter must have one parameter otherwise, and it's not necessary to check for
                ' duplicates if the setter parameter is named 'Value' since we'll report property
                ' cannot contain parameter named 'Value' if there is a duplicate in that case.
                Dim param = parameters(nPropertyParameters)
                If Not IdentifierComparison.Equals(param.Name, StringConstants.ValueParameterName) Then
                    Dim paramSyntax = parameterListSyntax(0)
                    Binder.CheckParameterNameNotDuplicate(parameters, nPropertyParameters, paramSyntax, param, diagnostics)
                End If

                If parameterListSyntax.Count = 1 Then
                    ' Verify parameter type matches property type.
                    Dim propertyType = propertySymbol.Type
                    Dim valueParameter = parameters(parameters.Count - 1)
                    Dim valueParameterType = valueParameter.Type

                    If Not propertyType.IsSameTypeIgnoringAll(valueParameterType) Then
                        If (Not propertyType.IsErrorType()) AndAlso (Not valueParameterType.IsErrorType()) Then
                            diagnostics.Add(ERRID.ERR_SetValueNotPropertyType, valueParameter.GetFirstLocation())
                        End If

                    Else
                        Dim overriddenMethod = method.OverriddenMethod
                        If overriddenMethod IsNot Nothing Then
                            Dim overriddenParameter = overriddenMethod.Parameters(parameters.Count - 1)

                            If overriddenParameter.Type.IsSameTypeIgnoringAll(valueParameterType) AndAlso
                               CustomModifierUtils.CopyParameterCustomModifiers(overriddenParameter, valueParameter) Then
                                parameters(parameters.Count - 1) = valueParameter
                            End If
                        End If
                    End If
                Else
                    diagnostics.Add(ERRID.ERR_SetHasOnlyOneParam, location)
                End If
            ElseIf synthesizeParameter Then
                ' No explicit set accessor parameter. Create a synthesized parameter.
                Dim valueParameter = SynthesizedParameterSymbol.CreateSetAccessorValueParameter(method, propertySymbol, parameterName:=StringConstants.ValueParameterName)
                parameters.Add(valueParameter)
            End If

            Return parameters.ToImmutableAndFree()
        End Function

        Private Shared ReadOnly s_checkParameterModifierCallback As Binder.CheckParameterModifierDelegate = AddressOf CheckParameterModifier

        Private Shared Function CheckParameterModifier(container As Symbol, token As SyntaxToken, flag As SourceParameterFlags, diagnostics As BindingDiagnosticBag) As SourceParameterFlags
            If flag <> SourceParameterFlags.ByVal Then
                Dim location = token.GetLocation()
                diagnostics.Add(ERRID.ERR_SetHasToBeByVal1, location, token.ToString())
                Return flag And SourceParameterFlags.ByVal
            End If
            Return SourceParameterFlags.ByVal
        End Function

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Debug.Assert(Not m_property.IsMustOverride)

            If m_property.IsAutoProperty Then
                Return SynthesizedPropertyAccessorHelper.GetBoundMethodBody(Me, m_property.AssociatedField, methodBodyBinder)
            Else
                Return MyBase.GetBoundMethodBody(compilationState, diagnostics, methodBodyBinder)
            End If
        End Function

        Friend Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            If arguments.SymbolPart = AttributeLocation.None Then
                If arguments.Attribute.IsTargetAttribute(AttributeDescription.DebuggerHiddenAttribute) Then
                    arguments.GetOrCreateData(Of MethodWellKnownAttributeData)().IsPropertyAccessorWithDebuggerHiddenAttribute = True
                End If
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Friend ReadOnly Property HasDebuggerHiddenAttribute As Boolean
            Get
                Dim attributeData = GetDecodedWellKnownAttributeData()
                Return attributeData IsNot Nothing AndAlso attributeData.IsPropertyAccessorWithDebuggerHiddenAttribute
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of VisualBasicAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            If m_property.IsAutoProperty Then
                Dim compilation = DeclaringCompilation

                AddSynthesizedAttribute(attributes,
                                        compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

                ' Dev11 adds DebuggerNonUserCode; there is no reason to do so since:
                ' - we emit no debug info for the body
                ' - the code doesn't call any user code that could inspect the stack and find the accessor's frame
                ' - the code doesn't throw exceptions whose stack frames we would need to hide
                ' 
                ' C# also doesn't add DebuggerHidden nor DebuggerNonUserCode attributes.
            End If
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return Not m_property.IsAutoProperty AndAlso MyBase.GenerateDebugInfoImpl
            End Get
        End Property
    End Class
End Namespace
