' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend MustInherit Class SourceParameterSymbol
        Inherits SourceParameterSymbolBase
        Implements IAttributeTargetSymbol

        Private ReadOnly _location As Location
        Private ReadOnly _name As String
        Private ReadOnly _type As TypeSymbol

        Friend Sub New(
            container As Symbol,
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            location As Location)
            MyBase.New(container, ordinal)

            _name = name
            _type = type
            _location = location
        End Sub

        Friend ReadOnly Property Location As Location
            Get
                Return _location
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                ' Ideally we should look for IDispatchConstantAttribute on this source parameter symbol,
                ' but the native VB compiler respects this attribute only on metadata parameter symbols, we do the same.
                ' See Devdiv bug #10789 (Handle special processing of object type without a default value per VB Language Spec 11.8.2 Applicable Methods) for details.
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                ' Ideally we should look for IUnknownConstantAttribute on this source parameter symbol,
                ' but the native VB compiler respects this attribute only on metadata parameter symbols, we do the same.
                ' See Devdiv bug #10789 (Handle special processing of object type without a default value per VB Language Spec 11.8.2 Applicable Methods) for details.
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                If _location IsNot Nothing Then
                    Return ImmutableArray.Create(Of Location)(_location)
                Else
                    Return ImmutableArray(Of Location).Empty
                End If
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        Public MustOverride Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)

        Public MustOverride Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                If IsImplicitlyDeclared Then
                    Return ImmutableArray(Of SyntaxReference).Empty
                Else
                    Return GetDeclaringSyntaxReferenceHelper(Of ParameterSyntax)(Me.Locations)
                End If
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                If Me.ContainingSymbol.IsImplicitlyDeclared Then

                    If If(TryCast(Me.ContainingSymbol, MethodSymbol)?.MethodKind = MethodKind.DelegateInvoke, False) AndAlso
                       Not Me.ContainingType.AssociatedSymbol?.IsImplicitlyDeclared Then
                        Return False
                    End If

                    Return True
                End If

                Return (GetMatchingPropertyParameter() IsNot Nothing)
            End Get
        End Property

        ''' <summary>
        ''' Is this an accessor parameter that came from the associated property? If so, 
        ''' return it, else return Nothing.
        ''' </summary>
        Private Function GetMatchingPropertyParameter() As ParameterSymbol
            Dim containingMethod = TryCast(ContainingSymbol, MethodSymbol)
            If containingMethod IsNot Nothing AndAlso containingMethod.IsAccessor() Then
                Dim containingProperty = TryCast(containingMethod.AssociatedSymbol, PropertySymbol)
                If containingProperty IsNot Nothing AndAlso Ordinal < containingProperty.ParameterCount Then
                    ' We match a parameter on our containing property.
                    Return containingProperty.Parameters(Ordinal)
                End If
            End If

            Return Nothing
        End Function

#Region "Attributes"

        ' Attributes on corresponding parameters of partial methods are merged. 
        ' We always create a complex parameter for a partial definition to store potential merged attributes there.
        ' At the creation time we don't know if the corresponding partial implementation has attributes so we need to always assume it might.
        ' 
        ' Unlike in C#, where both partial definition and partial implementation have partial syntax and 
        ' hence we create a complex parameter for both of them, in VB partial implementation Sub syntax 
        ' is no different from non-partial Sub. Therefore we can't determine at the creation time whether 
        ' a parameter of a Sub that is not a partial definition might need to store attributes or not.
        '
        ' We therefore need to virtualize the storage for attribute data. Simple parameter of a partial implementation
        ' uses attribute storage of the corresponding partial definition parameter.
        ' 
        ' When an implementation parameter is asked for attributes it gets them from the definition parameter:
        ' 1) If the implementation is a simple parameter it calls GetAttributeBag on the definition.
        ' 2) If it is a complex parameter it copies the data from the definition using BoundAttributesSource.

        Friend MustOverride Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
        Friend MustOverride Function GetEarlyDecodedWellKnownAttributeData() As ParameterEarlyWellKnownAttributeData
        Friend MustOverride Function GetDecodedWellKnownAttributeData() As CommonParameterWellKnownAttributeData
        Friend MustOverride ReadOnly Property AttributeDeclarationList As SyntaxList(Of AttributeListSyntax)

        Friend NotOverridable Overrides ReadOnly Property HasParamArrayAttribute As Boolean
            Get
                Dim data = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasParamArrayAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasDefaultValueAttribute As Boolean
            Get
                Dim data = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.DefaultParameterValue <> ConstantValue.Unset
            End Get
        End Property

        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Parameter
            End Get
        End Property

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        Public NotOverridable Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Friend Overrides Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            ' Declare methods need to know early what marshalling type are their parameters of to determine ByRef-ness.
            Dim containingSymbol = Me.ContainingSymbol
            If containingSymbol.Kind = SymbolKind.Method AndAlso DirectCast(containingSymbol, MethodSymbol).MethodKind = MethodKind.DeclareMethod Then
                If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.MarshalAsAttribute) Then
                    Dim hasAnyDiagnostics As Boolean = False

                    Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                    If Not attrdata.HasErrors Then
                        arguments.GetOrCreateData(Of ParameterEarlyWellKnownAttributeData).HasMarshalAsAttribute = True
                        Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                    Else
                        Return Nothing
                    End If
                End If
            End If

            Dim possibleValidParamArrayTarget As Boolean = False

            Select Case containingSymbol.Kind
                Case SymbolKind.Property
                    possibleValidParamArrayTarget = True

                Case SymbolKind.Method
                    Select Case DirectCast(containingSymbol, MethodSymbol).MethodKind
                        Case MethodKind.Conversion,
                             MethodKind.UserDefinedOperator,
                             MethodKind.EventAdd,
                             MethodKind.EventRemove
                            Debug.Assert(Not possibleValidParamArrayTarget)

                        Case Else
                            possibleValidParamArrayTarget = True

                    End Select
            End Select

            If possibleValidParamArrayTarget AndAlso
               VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ParamArrayAttribute) Then
                Dim hasAnyDiagnostics As Boolean = False

                Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                If Not attrdata.HasErrors Then
                    arguments.GetOrCreateData(Of ParameterEarlyWellKnownAttributeData).HasParamArrayAttribute = True
                    Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                Else
                    Return Nothing
                End If
            End If

            If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.DefaultParameterValueAttribute) Then
                Return EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription.DefaultParameterValueAttribute, arguments)
            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.DecimalConstantAttribute) Then
                Return EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription.DecimalConstantAttribute, arguments)
            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.DateTimeConstantAttribute) Then
                Return EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription.DateTimeConstantAttribute, arguments)
            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerLineNumberAttribute) Then
                arguments.GetOrCreateData(Of ParameterEarlyWellKnownAttributeData).HasCallerLineNumberAttribute = True
            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerFilePathAttribute) Then
                arguments.GetOrCreateData(Of ParameterEarlyWellKnownAttributeData).HasCallerFilePathAttribute = True
            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerMemberNameAttribute) Then
                arguments.GetOrCreateData(Of ParameterEarlyWellKnownAttributeData).HasCallerMemberNameAttribute = True
            ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerArgumentExpressionAttribute) Then
                Dim index = -1
                Dim attribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, False)
                If Not attribute.HasErrors Then
                    Dim parameterName As String = Nothing
                    If attribute.ConstructorArguments.Single().TryDecodeValue(SpecialType.System_String, parameterName) Then
                        Dim parameters = containingSymbol.GetParameters()
                        For i = 0 To parameters.Length - 1
                            If IdentifierComparison.Equals(parameters(i).Name, parameterName) Then
                                index = i
                                Exit For
                            End If
                        Next
                    End If
                End If

                arguments.GetOrCreateData(Of ParameterEarlyWellKnownAttributeData).CallerArgumentExpressionParameterIndex = index
            End If

            Return MyBase.EarlyDecodeWellKnownAttribute(arguments)
        End Function

        Friend Overrides Iterator Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Dim attributes = MyBase.GetCustomAttributesToEmit(moduleBuilder)

            For Each attribute In attributes
                If AttributeData.IsTargetEarlyAttribute(attributeType:=attribute.AttributeClass, attributeArgCount:=attribute.CommonConstructorArguments.Length, description:=AttributeDescription.CallerArgumentExpressionAttribute) Then
                    Dim callerArgumentExpressionParameterIndex = Me.CallerArgumentExpressionParameterIndex
                    If callerArgumentExpressionParameterIndex <> -1 AndAlso TypeOf attribute Is SourceAttributeData Then
                        Debug.Assert(callerArgumentExpressionParameterIndex >= 0)
                        Debug.Assert(attribute.CommonConstructorArguments.Length = 1)
                        ' We allow CallerArgumentExpression to have case-insensitive parameter name, but we
                        ' want to emit the parameter name with correct casing, so that it works with C#.
                        Dim correctedParameterName = ContainingSymbol.GetParameters()(callerArgumentExpressionParameterIndex).Name
                        Dim oldTypedConstant = attribute.CommonConstructorArguments.Single()
                        If correctedParameterName.Equals(oldTypedConstant.Value.ToString(), StringComparison.Ordinal) Then
                            Yield attribute
                            Continue For
                        End If
                        Dim newArgs = ImmutableArray.Create(New TypedConstant(oldTypedConstant.TypeInternal, oldTypedConstant.Kind, correctedParameterName))
                        Yield New SourceAttributeData(DeclaringCompilation, attribute.ApplicationSyntaxReference, attribute.AttributeClass, attribute.AttributeConstructor, newArgs, attribute.CommonNamedArguments, attribute.IsConditionallyOmitted, attribute.HasErrors)
                        Continue For
                    End If
                End If
                Yield attribute
            Next
        End Function

        ' It is not strictly necessary to decode default value attributes early in VB,
        ' but it is necessary in C#, so this keeps the implementations consistent.
        Private Function EarlyDecodeAttributeForDefaultParameterValue(description As AttributeDescription, ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Debug.Assert(description.Equals(AttributeDescription.DefaultParameterValueAttribute) OrElse
                         description.Equals(AttributeDescription.DecimalConstantAttribute) OrElse
                         description.Equals(AttributeDescription.DateTimeConstantAttribute))

            Dim hasAnyDiagnostics = False
            Dim attribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
            Dim value As ConstantValue

            If attribute.HasErrors Then
                value = ConstantValue.Bad
                hasAnyDiagnostics = True
            Else
                value = DecodeDefaultParameterValueAttribute(description, attribute)
            End If

            Dim paramData = arguments.GetOrCreateData(Of ParameterEarlyWellKnownAttributeData)()
            If paramData.DefaultParameterValue = ConstantValue.Unset Then
                paramData.DefaultParameterValue = value
            End If

            Return If(hasAnyDiagnostics, Nothing, attribute)
        End Function

        Friend Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim attrData = arguments.Attribute
            Debug.Assert(Not attrData.HasErrors)
            Debug.Assert(arguments.SymbolPart = AttributeLocation.None)
            Debug.Assert(TypeOf arguments.Diagnostics Is BindingDiagnosticBag)

            ' Differences from C#:
            '
            '   DefaultParameterValueAttribute
            '     - emitted as is, not treated as pseudo-custom attribute
            '     - checked (along with DecimalConstantAttribute and DateTimeConstantAttribute) for consistency with any explicit default value
            '
            '   OptionalAttribute 
            '     - Not used by the language, only syntactically optional parameters or metadata optional parameters are recognized by overload resolution.
            '       OptionalAttribute is checked for in emit phase.
            '
            '   ParamArrayAttribute
            '     - emitted as is, no error reported
            '     - Dev11 incorrectly emits the attribute twice
            '
            '  InAttribute, OutAttribute
            '     - metadata flag set, no diagnostics reported, don't influence language semantics

            If attrData.IsTargetAttribute(AttributeDescription.TupleElementNamesAttribute) Then
                DirectCast(arguments.Diagnostics, BindingDiagnosticBag).Add(ERRID.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location)
            End If

            If attrData.IsTargetAttribute(AttributeDescription.DefaultParameterValueAttribute) Then
                ' Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DefaultParameterValueAttribute, arguments)
            ElseIf attrData.IsTargetAttribute(AttributeDescription.DecimalConstantAttribute) Then
                ' Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DecimalConstantAttribute, arguments)
            ElseIf attrData.IsTargetAttribute(AttributeDescription.DateTimeConstantAttribute) Then
                ' Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DateTimeConstantAttribute, arguments)
            ElseIf attrData.IsTargetAttribute(AttributeDescription.InAttribute) Then
                arguments.GetOrCreateData(Of CommonParameterWellKnownAttributeData)().HasInAttribute = True
            ElseIf attrData.IsTargetAttribute(AttributeDescription.OutAttribute) Then
                arguments.GetOrCreateData(Of CommonParameterWellKnownAttributeData)().HasOutAttribute = True
            ElseIf attrData.IsTargetAttribute(AttributeDescription.MarshalAsAttribute) Then
                MarshalAsAttributeDecoder(Of CommonParameterWellKnownAttributeData, AttributeSyntax, VisualBasicAttributeData, AttributeLocation).Decode(arguments, AttributeTargets.Parameter, MessageProvider.Instance)
            ElseIf attrData.IsTargetAttribute(AttributeDescription.CallerArgumentExpressionAttribute) Then
                Dim index = GetEarlyDecodedWellKnownAttributeData()?.CallerArgumentExpressionParameterIndex
                If index = Ordinal Then
                    DirectCast(arguments.Diagnostics, BindingDiagnosticBag).Add(ERRID.WRN_CallerArgumentExpressionAttributeSelfReferential, arguments.AttributeSyntaxOpt.Location, Me.Name)
                ElseIf index = -1 Then
                    DirectCast(arguments.Diagnostics, BindingDiagnosticBag).Add(ERRID.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName, arguments.AttributeSyntaxOpt.Location, Me.Name)
                End If
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Private Sub DecodeDefaultParameterValueAttribute(description As AttributeDescription, ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim attribute = arguments.Attribute
            Dim diagnostics = DirectCast(arguments.Diagnostics, BindingDiagnosticBag)

            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)
            Debug.Assert(diagnostics IsNot Nothing)

            Dim value = DecodeDefaultParameterValueAttribute(description, attribute)
            If Not value.IsBad Then
                VerifyParamDefaultValueMatchesAttributeIfAny(value, arguments.AttributeSyntaxOpt, diagnostics)
            End If
        End Sub

        ''' <summary>
        ''' Verify the default value matches the default value from any earlier attribute
        ''' (DefaultParameterValueAttribute, DateTimeConstantAttribute or DecimalConstantAttribute).
        ''' If not, report ERR_ParamDefaultValueDiffersFromAttribute.
        ''' </summary>
        Protected Sub VerifyParamDefaultValueMatchesAttributeIfAny(value As ConstantValue, syntax As VisualBasicSyntaxNode, diagnostics As BindingDiagnosticBag)
            Dim data = GetEarlyDecodedWellKnownAttributeData()
            If data IsNot Nothing Then
                Dim attrValue = data.DefaultParameterValue
                If attrValue <> ConstantValue.Unset AndAlso
                    value <> attrValue Then
                    Binder.ReportDiagnostic(diagnostics, syntax, ERRID.ERR_ParamDefaultValueDiffersFromAttribute)
                End If
            End If
        End Sub

        Private Function DecodeDefaultParameterValueAttribute(description As AttributeDescription, attribute As VisualBasicAttributeData) As ConstantValue
            Debug.Assert(Not attribute.HasErrors)

            If description.Equals(AttributeDescription.DefaultParameterValueAttribute) Then
                Return DecodeDefaultParameterValueAttribute(attribute)
            ElseIf description.Equals(AttributeDescription.DecimalConstantAttribute) Then
                Return attribute.DecodeDecimalConstantValue()
            Else
                Debug.Assert(description.Equals(AttributeDescription.DateTimeConstantAttribute))
                Return attribute.DecodeDateTimeConstantValue()
            End If
        End Function

        Private Function DecodeDefaultParameterValueAttribute(attribute As VisualBasicAttributeData) As ConstantValue
            Debug.Assert(attribute.CommonConstructorArguments.Length = 1)

            ' the type of the value is the type of the expression in the attribute
            Dim arg = attribute.CommonConstructorArguments(0)

            Dim specialType = If(arg.Kind = TypedConstantKind.Enum,
                                 DirectCast(arg.TypeInternal, NamedTypeSymbol).EnumUnderlyingType.SpecialType,
                                 arg.TypeInternal.SpecialType)
            Dim constantValueDiscriminator = ConstantValue.GetDiscriminator(specialType)

            If constantValueDiscriminator = ConstantValueTypeDiscriminator.Bad Then
                If arg.Kind <> TypedConstantKind.Array AndAlso
                    arg.ValueInternal Is Nothing AndAlso
                    Type.IsReferenceType Then
                    Return ConstantValue.Null
                End If

                Return ConstantValue.Bad
            End If

            Return ConstantValue.Create(arg.ValueInternal, constantValueDiscriminator)
        End Function

        Friend NotOverridable Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                If data IsNot Nothing Then
                    Return data.MarshallingInformation
                End If

                ' Default marshalling for string parameters of Declare methods (ByRef or ByVal)
                If Type.IsStringType() Then
                    Dim container As Symbol = ContainingSymbol

                    If container.Kind = SymbolKind.Method Then
                        Dim methodSymbol = DirectCast(container, MethodSymbol)
                        If methodSymbol.MethodKind = MethodKind.DeclareMethod Then
                            Dim info As New MarshalPseudoCustomAttributeData()

                            Debug.Assert(IsByRef)

                            If IsExplicitByRef Then
                                Dim pinvoke As DllImportData = methodSymbol.GetDllImportData()

                                Select Case pinvoke.CharacterSet
                                    Case Cci.Constants.CharSet_None, CharSet.Ansi
                                        info.SetMarshalAsSimpleType(Cci.Constants.UnmanagedType_AnsiBStr)

                                    Case Cci.Constants.CharSet_Auto
                                        info.SetMarshalAsSimpleType(Cci.Constants.UnmanagedType_TBStr)

                                    Case CharSet.Unicode
                                        info.SetMarshalAsSimpleType(UnmanagedType.BStr)

                                    Case Else
                                        Throw ExceptionUtilities.UnexpectedValue(pinvoke.CharacterSet)
                                End Select
                            Else
                                info.SetMarshalAsSimpleType(Cci.Constants.UnmanagedType_VBByRefStr)
                            End If

                            Return info
                        End If
                    End If
                End If

                Return Nothing
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsByRef As Boolean
            Get
                If IsExplicitByRef Then
                    Return True
                End If

                ' String parameters of Declare methods without explicit MarshalAs attribute are always ByRef, even if they are declared ByVal.

                If Type.IsStringType() AndAlso
                   ContainingSymbol.Kind = SymbolKind.Method AndAlso
                   DirectCast(ContainingSymbol, MethodSymbol).MethodKind = MethodKind.DeclareMethod Then

                    Dim data = GetEarlyDecodedWellKnownAttributeData()
                    Return data Is Nothing OrElse Not data.HasMarshalAsAttribute
                End If

                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasOutAttribute
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasInAttribute
            End Get
        End Property
#End Region

    End Class

    Friend Enum SourceParameterFlags As Byte
        [ByVal] = 1 << 0
        [ByRef] = 1 << 1
        [Optional] = 1 << 2
        [ParamArray] = 1 << 3
    End Enum

End Namespace

