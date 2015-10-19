' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeSymbolExtensions
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Summarizes whether a conversion is allowed, and if so, which kind of conversion (and in some cases, the
    ''' associated symbol).
    ''' </summary>
    Public Structure Conversion
        Implements IEquatable(Of Conversion)

        Private ReadOnly _convKind As ConversionKind
        Private ReadOnly _method As MethodSymbol

        Friend Sub New(conv As KeyValuePair(Of ConversionKind, MethodSymbol))
            _convKind = conv.Key
            _method = conv.Value
        End Sub

        Friend ReadOnly Property Kind As ConversionKind
            Get
                Return _convKind
            End Get
        End Property

        ''' <summary>
        ''' Returns True if the conversion exists, either as a widening or narrowing conversion.
        ''' </summary>
        ''' <remarks>
        ''' If this returns True, exactly one of <see cref="IsNarrowing"/> or <see cref="IsWidening"/> will return True. 
        ''' If this returns False, neither <see cref="IsNarrowing"/> or <see cref="IsWidening"/> will return True.
        ''' </remarks>
        Public ReadOnly Property Exists As Boolean
            Get
                Return Not Conversions.NoConversion(_convKind)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion a narrowing conversion, and not a widening conversion. 
        ''' </summary>
        Public ReadOnly Property IsNarrowing As Boolean
            Get
                Return Conversions.IsNarrowingConversion(_convKind)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is a widening conversion, and not a narrowing conversion.
        ''' </summary>
        Public ReadOnly Property IsWidening As Boolean
            Get
                Return Conversions.IsWideningConversion(_convKind)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is an identity conversion. 
        ''' </summary>
        ''' <remarks>
        ''' Note that identity conversion are also considered widening conversions.
        ''' </remarks>
        Public ReadOnly Property IsIdentity As Boolean
            Get
                Return Conversions.IsIdentityConversion(_convKind)
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is a default conversion (a conversion from the "Nothing" literal). 
        ''' </summary>
        ''' <remarks>Note that default conversions are considered widening conversions.</remarks>
        Public ReadOnly Property IsDefault As Boolean
            Get
                Return (_convKind And ConversionKind.WideningNothingLiteral) = ConversionKind.WideningNothingLiteral
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is a widening numeric conversion or a narrowing numeric conversion, as defined in
        ''' section 8.3.
        ''' </summary>
        Public ReadOnly Property IsNumeric As Boolean
            Get
                Return (_convKind And ConversionKind.Numeric) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is a narrowing boolean conversion, as defined in section 8.2.
        ''' </summary>
        Public ReadOnly Property IsBoolean As Boolean
            Get
                Return (_convKind And ConversionKind.Boolean) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is a widening reference conversion or narrowing reference conversion, as defined in
        ''' section 8.4.
        ''' </summary>
        Public ReadOnly Property IsReference As Boolean
            Get
                Return (_convKind And ConversionKind.Reference) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is a widening anonymous delegate conversion as defined in section 8.8, or a
        ''' narrowing anonymous delegate conversion as defined in section 8.9.
        ''' </summary>
        Public ReadOnly Property IsAnonymousDelegate As Boolean
            Get
                Return (_convKind And ConversionKind.AnonymousDelegate) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this is a lambda conversion.
        ''' </summary>
        Public ReadOnly Property IsLambda As Boolean
            Get
                Return (_convKind And ConversionKind.Lambda) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion is a widening array conversion or a narrowing array conversion, as defined in
        ''' section 8.5.
        ''' </summary>
        Public ReadOnly Property IsArray As Boolean
            Get
                Return (_convKind And ConversionKind.Array) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion a widening value type conversion or a narrowing value type conversion as defined in
        ''' section 8.6.
        ''' </summary>
        Public ReadOnly Property IsValueType As Boolean
            Get
                Return (_convKind And ConversionKind.Value) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion a widening nullable value type conversion or a narrowing nullable value type
        ''' conversion as defined in section 8.6.1.
        ''' </summary>
        Public ReadOnly Property IsNullableValueType As Boolean
            Get
                Return (_convKind And ConversionKind.Nullable) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion a widening string conversion or a narrowing string conversion as defined in section
        ''' 8.7.
        ''' </summary>
        Public ReadOnly Property IsString As Boolean
            Get
                Return (_convKind And ConversionKind.String) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion a widening type parameter or a narrowing type parameter conversion, as defined in
        ''' section 8.10.
        ''' </summary>
        Public ReadOnly Property IsTypeParameter As Boolean
            Get
                Return (_convKind And ConversionKind.TypeParameter) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Returns True if this conversion a widening user defined or a narrowing user defined conversion, as defined in
        ''' section 8.11.
        ''' </summary>
        ''' <remarks>
        ''' If this returns True, the involved conversion method can be obtained with the <see cref="Method"/>
        ''' property.
        ''' </remarks>
        Public ReadOnly Property IsUserDefined As Boolean
            Get
                Return (_convKind And ConversionKind.UserDefined) <> 0
            End Get
        End Property

        Friend ReadOnly Property Method As MethodSymbol
            Get
                Return _method
            End Get
        End Property

        ''' <summary>
        ''' Returns the method that defines the user defined conversion, if any. Otherwise returns Nothing.
        ''' </summary>
        Public ReadOnly Property MethodSymbol As IMethodSymbol
            Get
                Return _method
            End Get
        End Property

        ''' <summary>
        ''' Returns True if two <see cref="Conversion"/> values are equal.
        ''' </summary>
        ''' <param name="left">The left value.</param>
        ''' <param name="right">The right value.</param>
        Public Shared Operator =(left As Conversion, right As Conversion) As Boolean
            Return left.Equals(right)
        End Operator

        ''' <summary>
        ''' Returns True if two <see cref="Conversion"/> values are not equal.
        ''' </summary>
        ''' <param name="left">The left value.</param>
        ''' <param name="right">The right value.</param>
        Public Shared Operator <>(left As Conversion, right As Conversion) As Boolean
            Return Not (left = right)
        End Operator

        ''' <summary>
        ''' Determines whether the specified object is equal to the current object.
        ''' </summary>
        ''' <param name="obj">
        ''' The object to compare with the current object. 
        ''' </param>
        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is Conversion AndAlso
                Me = DirectCast(obj, Conversion)
        End Function

        ''' <summary>
        ''' Determines whether the specified object is equal to the current object.
        ''' </summary>
        ''' <param name="other">
        ''' The object to compare with the current object. 
        ''' </param>
        Public Overloads Function Equals(other As Conversion) As Boolean Implements IEquatable(Of Conversion).Equals
            Return Me._convKind = other._convKind AndAlso Me.Method = other.Method
        End Function

        ''' <summary>
        ''' Returns a hash code for the current object.
        ''' </summary>
        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Method, CInt(_convKind))
        End Function

        ''' <summary>
        ''' Returns a string that represents the current object.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return _convKind.ToString()
        End Function
    End Structure

    <Flags()>
    Friend Enum ConversionKind
        ' If there is a conversion, either [Widening] or [Narrowing] bit must be set, but not both.
        ' All VB conversions are either Widening or Narrowing.
        '
        ' To indicate the fact that no conversion exists:
        '    1) Neither [Widening] nor [Narrowing] are set.
        '    2) Additional flags may be set in order to provide specific reason.
        '
        ' Bits from the following values are never set at the same time :
        ' Identity, Numeric, Nullable, Reference, Array, TypeParameter, Value, [String], WideningNothingLiteral, InterpolatedString

        FailedDueToNumericOverflow = 1 << 31 ' Failure flag
        FailedDueToIntegerOverflow = FailedDueToNumericOverflow Or (1 << 30) ' Failure flag
        FailedDueToNumericOverflowMask = FailedDueToNumericOverflow Or FailedDueToIntegerOverflow
        FailedDueToQueryLambdaBodyMismatch = 1 << 29 ' Failure flag to indicate that conversion failed because body of a query lambda couldn't be converted to the target delegate return type.
        FailedDueToArrayLiteralElementConversion = 1 << 28 ' Failed because array literal element could not be converted to the target element type.

        ' If there is a conversion, one and only one of the following two bits must be set.
        ' All VB conversions are either Widening or Narrowing.
        [Widening] = 1 << 0
        [Narrowing] = 1 << 1

        ''' <summary>
        ''' Because flags can be combined, use the method IsIdentityConversion when testing for ConversionKind.Identity
        ''' </summary>
        ''' <remarks></remarks>
        Identity = [Widening] Or 1 << 2 ' According to VB spec, identity conversion is Widening

        Numeric = 1 << 3
        WideningNumeric = [Widening] Or Numeric
        NarrowingNumeric = [Narrowing] Or Numeric

        Nullable = 1 << 4
        WideningNullable = [Widening] Or Nullable
        NarrowingNullable = [Narrowing] Or Nullable

        Reference = 1 << 5
        WideningReference = [Widening] Or Reference
        NarrowingReference = [Narrowing] Or Reference

        Array = 1 << 6
        WideningArray = [Widening] Or Array
        NarrowingArray = [Narrowing] Or Array

        TypeParameter = 1 << 7
        WideningTypeParameter = [Widening] Or TypeParameter
        NarrowingTypeParameter = [Narrowing] Or TypeParameter

        Value = 1 << 8
        WideningValue = [Widening] Or Value
        NarrowingValue = [Narrowing] Or Value

        [String] = 1 << 9
        WideningString = [Widening] Or [String]
        NarrowingString = [Narrowing] Or [String]

        [Boolean] = 1 << 10
        ' Note: there are no widening boolean conversions.
        NarrowingBoolean = [Narrowing] Or [Boolean]

        WideningNothingLiteral = [Widening] Or (1 << 11)

        ' Compiler might be interested in knowing if constant numeric conversion involves narrowing
        ' for constant's original type. When user-defined conversions are involved, this flag can be
        ' combined with widening conversions other than WideningNumericConstant.
        '
        ' If this flag is combined with Narrowing, there should be no other reasons to treat
        ' conversion as narrowing. In some scenarios overload resolution is likely to dismiss
        ' narrowing in presence of this flag. Also, it appears that with Option Strict On, Dev10
        ' compiler does not report errors for narrowing conversions from an integral constant
        ' expression to an integral type (assuming integer overflow checks are disabled) or from a
        ' floating constant to a floating type.
        InvolvesNarrowingFromNumericConstant = 1 << 12

        ' This flag is set when conversion involves conversion enum <-> underlying type,
        ' or conversion between two enums, etc 
        InvolvesEnumTypeConversions = 1 << 13

        ' Lambda conversion
        Lambda = 1 << 14

        ' Delegate relaxation levels for Lambda and Delegate conversions
        DelegateRelaxationLevelNone = 0 ' Identity / Whidbey
        DelegateRelaxationLevelWidening = 1 << 15
        DelegateRelaxationLevelWideningDropReturnOrArgs = 2 << 15
        DelegateRelaxationLevelWideningToNonLambda = 3 << 15
        DelegateRelaxationLevelNarrowing = 4 << 15  ' OrcasStrictOff
        DelegateRelaxationLevelInvalid = 5 << 15 ' Keep invalid the biggest number

        DelegateRelaxationLevelMask = 7 << 15 ' Three bits used!

        'Can be combined with Narrowing
        VarianceConversionAmbiguity = 1 << 18

        ' This bit can be combined with NoConversion to indicate that, even though there is no conversion
        ' from the language point of view, there is a slight chance that conversion might succeed at run-time
        ' under the right circumstances. It is used to detect possibly ambiguous variance conversions to an
        ' interface, so it is set only for scenarios that are relevant to variance conversions to an
        ' interface. 
        MightSucceedAtRuntime = 1 << 19

        AnonymousDelegate = 1 << 20
        NeedAStub = 1 << 21
        ConvertedToExpressionTree = 1 << 22   ' Combined with Lambda, indicates a conversion of lambda to Expression(Of T).

        UserDefined = 1 << 23

        ' Some variance delegate conversions are treated as special narrowing (Dev10 #820752).
        ' This flag is combined with Narrowing to indicate the fact.
        NarrowingDueToContraVarianceInDelegate = 1 << 24

        ' Interpolated string conversions
        InterpolatedString = [Widening] Or (1 << 25)

        ' Bits 28 - 31 are reserved for failure flags.
    End Enum

    <Flags()>
    Friend Enum MethodConversionKind
        Identity = &H0
        OneArgumentIsVbOrBoxWidening = &H1
        OneArgumentIsClrWidening = &H2
        OneArgumentIsNarrowing = &H4

        ' TODO: It looks like Dev10 MethodConversionKinds for return are badly named because
        '       they appear to give classification in the direction opposite to the data
        '       flow. This is very confusing. However, I am not going to rename them just yet.
        '       Will do this when all parts are ported and working together, otherwise it will 
        '       be very hard to port the rest of the feature.
        ReturnIsWidening = &H8
        ReturnIsClrNarrowing = &H10
        ReturnIsIsVbOrBoxNarrowing = &H20

        ReturnValueIsDropped = &H40
        AllArgumentsIgnored = &H80
        ExcessOptionalArgumentsOnTarget = &H100

        Error_ByRefByValMismatch = &H200
        Error_Unspecified = &H400
        Error_IllegalToIgnoreAllArguments = &H800
        Error_RestrictedType = &H1000
        Error_SubToFunction = &H2000
        Error_ReturnTypeMismatch = &H4000
        Error_OverloadResolution = &H8000

        AllErrorReasons = Error_ByRefByValMismatch Or
                          Error_Unspecified Or
                          Error_IllegalToIgnoreAllArguments Or
                          Error_RestrictedType Or
                          Error_SubToFunction Or
                          Error_ReturnTypeMismatch Or
                          Error_OverloadResolution
    End Enum

    ''' <summary>
    ''' The purpose of this class is to answer questions about convertibility of one type to another.
    ''' It also answers questions about conversions from an expression to a type.
    '''
    ''' The code is organized such that each method attempts to implement exactly one section of the
    ''' specification.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class Conversions

        Public Shared ReadOnly Identity As New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.Identity, Nothing)

        Private Sub New()
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Class ConversionEasyOut
            ' There are situations in which we know that there is no unusual conversion going on (such as a conversion involving constants,
            ' enumerated types, and so on.) In those situations we can classify conversions via a simple table lookup:

            ' PERF: Use Integer instead of ConversionKind so the compiler can use array literal initialization.
            '       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
            Private Shared ReadOnly s_convkind As Integer(,)

            Shared Sub New()
                Const NOC As Integer = Nothing 'ConversionKind.NoConversion
                Const IDN As Integer = ConversionKind.Identity
                Const NUM As Integer = ConversionKind.WideningNumeric
                Const NNM As Integer = ConversionKind.NarrowingNumeric
                Const IRF As Integer = ConversionKind.WideningReference
                Const NRF As Integer = ConversionKind.NarrowingReference
                Const WIV As Integer = ConversionKind.WideningValue
                Const NAV As Integer = ConversionKind.NarrowingValue
                Const NST As Integer = ConversionKind.NarrowingString
                Const WST As Integer = ConversionKind.WideningString
                Const NBO As Integer = ConversionKind.NarrowingBoolean

                '    TO TYPE
                '    Obj  Str  Bool Char SByt Shrt Int  Long Byte UShr UInt ULng Sngl Dbl  Dec  Date
                s_convkind = New Integer(,) {                                                           ' FROM TYPE
                    {IDN, NRF, NAV, NAV, NAV, NAV, NAV, NAV, NAV, NAV, NAV, NAV, NAV, NAV, NAV, NAV}, ' Obj   
                    {IRF, IDN, NST, NST, NST, NST, NST, NST, NST, NST, NST, NST, NST, NST, NST, NST}, ' Str   
                    {WIV, NST, IDN, NOC, NBO, NBO, NBO, NBO, NBO, NBO, NBO, NBO, NBO, NBO, NBO, NOC}, ' Bool  
                    {WIV, WST, NOC, IDN, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC}, ' Char  
                    {WIV, NST, NBO, NOC, IDN, NUM, NUM, NUM, NNM, NNM, NNM, NNM, NUM, NUM, NUM, NOC}, ' SByt  
                    {WIV, NST, NBO, NOC, NNM, IDN, NUM, NUM, NNM, NNM, NNM, NNM, NUM, NUM, NUM, NOC}, ' Shrt  
                    {WIV, NST, NBO, NOC, NNM, NNM, IDN, NUM, NNM, NNM, NNM, NNM, NUM, NUM, NUM, NOC}, ' Int   
                    {WIV, NST, NBO, NOC, NNM, NNM, NNM, IDN, NNM, NNM, NNM, NNM, NUM, NUM, NUM, NOC}, ' Long  
                    {WIV, NST, NBO, NOC, NNM, NUM, NUM, NUM, IDN, NUM, NUM, NUM, NUM, NUM, NUM, NOC}, ' Byte  
                    {WIV, NST, NBO, NOC, NNM, NNM, NUM, NUM, NNM, IDN, NUM, NUM, NUM, NUM, NUM, NOC}, ' UShr  
                    {WIV, NST, NBO, NOC, NNM, NNM, NNM, NUM, NNM, NNM, IDN, NUM, NUM, NUM, NUM, NOC}, ' UInt  
                    {WIV, NST, NBO, NOC, NNM, NNM, NNM, NNM, NNM, NNM, NNM, IDN, NUM, NUM, NUM, NOC}, ' ULng  
                    {WIV, NST, NBO, NOC, NNM, NNM, NNM, NNM, NNM, NNM, NNM, NNM, IDN, NUM, NNM, NOC}, ' Sngl  
                    {WIV, NST, NBO, NOC, NNM, NNM, NNM, NNM, NNM, NNM, NNM, NNM, NNM, IDN, NNM, NOC}, ' Dbl   
                    {WIV, NST, NBO, NOC, NNM, NNM, NNM, NNM, NNM, NNM, NNM, NNM, NUM, NUM, IDN, NOC}, ' Dec   
                    {WIV, NST, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, NOC, IDN}  ' Date  
                }
                '    Obj  Str  Bool Char SByt Shrt Int  Long Byte UShr UInt ULng Sngl Dbl  Dec  Date

            End Sub

            Public Shared Function ClassifyPredefinedConversion(source As TypeSymbol, target As TypeSymbol) As ConversionKind?

                If source Is Nothing OrElse target Is Nothing Then
                    Return Nothing
                End If

                ' First, dig through Nullable
                Dim sourceNullableUnderlying = source.GetNullableUnderlyingTypeOrSelf()
                Dim sourceIsNullable As Boolean = (sourceNullableUnderlying IsNot source)

                Dim targetNullableUnderlying = target.GetNullableUnderlyingTypeOrSelf()
                Dim targetIsNullable As Boolean = (targetNullableUnderlying IsNot target)

                ' Now dig through Enum
                Dim sourceEnumUnderlying = sourceNullableUnderlying.GetEnumUnderlyingTypeOrSelf()
                Dim sourceIsEnum As Boolean = (sourceEnumUnderlying IsNot sourceNullableUnderlying)

                Dim targetEnumUnderlying = targetNullableUnderlying.GetEnumUnderlyingTypeOrSelf()
                Dim targetIsEnum As Boolean = (targetEnumUnderlying IsNot targetNullableUnderlying)

                ' Filter out unexpected underlying types for Nullable and enum types
                If (sourceIsEnum OrElse sourceIsNullable) AndAlso
                    (sourceEnumUnderlying.IsStringType() OrElse sourceEnumUnderlying.IsObjectType()) Then
                    Return Nothing
                End If

                If (targetIsEnum OrElse targetIsNullable) AndAlso
                    (targetEnumUnderlying.IsStringType() OrElse targetEnumUnderlying.IsObjectType()) Then
                    Return Nothing
                End If

                ' Classify conversion between underlying types.
                Dim sourceIndex As Integer? = TypeToIndex(sourceEnumUnderlying)
                If sourceIndex Is Nothing Then
                    Return Nothing
                End If

                Dim targetIndex As Integer? = TypeToIndex(targetEnumUnderlying)
                If targetIndex Is Nothing Then
                    Return Nothing
                End If

                ' Table lookup for underlying type doesn't give correct classification
                ' for Nullable <=> Object conversion. Need to check them explicitly.
                If sourceIsNullable Then
                    If target.IsObjectType() Then
                        Return ConversionKind.WideningValue
                    End If

                ElseIf targetIsNullable Then
                    If source.IsObjectType() Then
                        Return ConversionKind.NarrowingValue
                    End If
                End If

                Dim conv As ConversionKind = CType(s_convkind(sourceIndex.Value, targetIndex.Value), ConversionKind)

                If Conversions.NoConversion(conv) Then
                    Return conv
                End If

                ' Adjust classification for enum conversions first, but don't adjust conversions enum <=> Object.
                If ((sourceIsEnum AndAlso Not target.IsObjectType()) OrElse
                    (targetIsEnum AndAlso Not source.IsObjectType())) Then

                    '§8.8 Widening Conversions
                    '•	From an enumerated type to its underlying numeric type, or to a numeric type 
                    '   that its underlying numeric type has a widening conversion to.
                    '§8.9 Narrowing Conversions
                    '•	From a numeric type to an enumerated type.
                    '•	From an enumerated type to a numeric type its underlying numeric type has a narrowing conversion to.
                    '•	From an enumerated type to another enumerated type. 

                    '!!! Spec doesn't mention this, but VB also supports conversions between enums and non-numeric intrinsic types.  

                    Dim sourceEnum = sourceNullableUnderlying
                    Dim targetEnum = targetNullableUnderlying

                    If sourceIsEnum Then
                        If targetIsEnum Then
                            If Not (Conversions.IsIdentityConversion(conv) AndAlso sourceEnum.IsSameTypeIgnoringCustomModifiers(targetEnum)) Then
                                '•	From an enumerated type to another enumerated type. 
                                conv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions
                            End If

                        ElseIf Conversions.IsWideningConversion(conv) Then
                            '•	From an enumerated type to its underlying numeric type, or to a type 
                            '   that its underlying numeric type has a widening conversion to.
                            conv = conv Or ConversionKind.InvolvesEnumTypeConversions
                            If (conv And ConversionKind.Identity) <> 0 Then
                                conv = (conv And Not ConversionKind.Identity) Or ConversionKind.Widening Or ConversionKind.Numeric
                            End If
                        Else
                            Debug.Assert(Conversions.IsNarrowingConversion(conv))

                            '•	From an enumerated type to a numeric type its underlying numeric type has a narrowing conversion to.
                            conv = conv Or ConversionKind.InvolvesEnumTypeConversions
                        End If

                    Else
                        Debug.Assert(targetIsEnum)

                        '•	From a type convertible to underlying type to an enumerated type.
                        conv = (conv And Not ConversionKind.Widening) Or ConversionKind.Narrowing Or ConversionKind.InvolvesEnumTypeConversions
                        If (conv And ConversionKind.Identity) <> 0 Then
                            conv = (conv And Not ConversionKind.Identity) Or ConversionKind.Numeric
                        End If
                    End If

                    Debug.Assert(Conversions.IsIdentityConversion(conv) OrElse (conv And ConversionKind.InvolvesEnumTypeConversions) <> 0)
                End If

                ' Now adjust classification for Nullable conversions.
                If sourceIsNullable OrElse targetIsNullable Then
                    Debug.Assert(Not source.IsObjectType() AndAlso Not target.IsObjectType())

                    '§8.8 Widening Conversions
                    '•	From a type T to the type T?.
                    '•	From a type T? to a type S?, where there is a widening conversion from the type T to the type S.
                    '•	From a type T to a type S?, where there is a widening conversion from the type T to the type S.
                    '•	From a type T? to an interface type that the type T implements.
                    '§8.9 Narrowing Conversions
                    '•	From a type T? to a type T.
                    '•	From a type T? to a type S?, where there is a narrowing conversion from the type T to the type S.
                    '•	From a type T to a type S?, where there is a narrowing conversion from the type T to the type S.
                    '•	From a type S? to a type T, where there is a conversion from the type S to the type T.

                    Dim sourceNullable = source
                    Dim targetNullable = target

                    If sourceIsNullable Then

                        If targetIsNullable Then
                            If Conversions.IsNarrowingConversion(conv) Then
                                '•	From a type T? to a type S?, where there is a narrowing conversion from the type T to the type S.
                                conv = ConversionKind.NarrowingNullable
                            ElseIf Not Conversions.IsIdentityConversion(conv) Then
                                Debug.Assert(Conversions.IsWideningConversion(conv))
                                '•	From a type T? to a type S?, where there is a widening conversion from the type T to the type S.
                                conv = ConversionKind.WideningNullable
                            Else
                                Debug.Assert(Conversions.IsIdentityConversion(conv) AndAlso sourceNullable.IsSameTypeIgnoringCustomModifiers(targetNullable))
                            End If

                        Else
                            '•	From a type T? to a type T.
                            '•	From a type S? to a type T, where there is a conversion from the type S to the type T.
                            conv = ConversionKind.NarrowingNullable
                        End If

                    Else
                        Debug.Assert(targetIsNullable)

                        If Conversions.IsWideningConversion(conv) Then
                            '•	From a type T to the type T?.
                            '•	From a type T to a type S?, where there is a widening conversion from the type T to the type S.
                            conv = ConversionKind.WideningNullable
                        Else
                            Debug.Assert(Conversions.IsNarrowingConversion(conv))
                            '•	From a type T to a type S?, where there is a narrowing conversion from the type T to the type S.
                            conv = ConversionKind.NarrowingNullable
                        End If
                    End If
                End If

                Return conv
            End Function
        End Class

        Private Shared Function FastClassifyPredefinedConversion(source As TypeSymbol, target As TypeSymbol) As ConversionKind?
            Return ConversionEasyOut.ClassifyPredefinedConversion(source, target)
        End Function

        ''' <summary>
        ''' Attempts to fold conversion of a constant expression. 
        ''' 
        ''' Returns Nothing if conversion cannot be folded.
        ''' 
        ''' If conversion failed due to non-integer overflow, ConstantValue.Bad is returned. Consumer 
        ''' is responsible for reporting appropriate diagnostics.
        ''' 
        ''' If integer overflow occurs, integerOverflow is set to True and ConstantValue for overflowed result is returned. 
        ''' Consumer is responsible for reporting appropriate diagnostics and potentially discarding the result.
        ''' </summary>
        Public Shared Function TryFoldConstantConversion(
            source As BoundExpression,
            destination As TypeSymbol,
            ByRef integerOverflow As Boolean
        ) As ConstantValue

            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            integerOverflow = False

            Dim sourceValue As ConstantValue = source.ConstantValueOpt

            If sourceValue Is Nothing OrElse sourceValue.IsBad Then
                ' Not a constant
                Return Nothing
            End If

            If Not destination.AllowsCompileTimeConversions() Then
                Return Nothing
            End If

            If source.IsNothingLiteral() Then
                ' A Nothing literal turns into the default value of the target type.

                If destination.IsStringType() Then
                    Return source.ConstantValueOpt
                End If

                Dim dstDiscriminator As ConstantValueTypeDiscriminator = destination.GetConstantValueTypeDiscriminator()
                Debug.Assert((dstDiscriminator <> ConstantValueTypeDiscriminator.Bad) AndAlso (dstDiscriminator <> ConstantValueTypeDiscriminator.Nothing))

                Return ConstantValue.Default(dstDiscriminator)
            End If

            Dim sourceExpressionType = source.Type

            If Not sourceExpressionType.AllowsCompileTimeConversions() Then
                Return Nothing
            End If

            Debug.Assert(sourceExpressionType IsNot Nothing)
            Debug.Assert(sourceExpressionType.IsValidForConstantValue(sourceValue))

            Dim sourceType = sourceExpressionType.GetEnumUnderlyingTypeOrSelf()
            Dim targetType = destination.GetEnumUnderlyingTypeOrSelf()

            ' Shortcut for identity conversions
            If sourceType Is targetType Then
                Return sourceValue
            End If

            ' Convert the value of the constant to the result type.
            If IsStringType(sourceType) Then

                If IsCharType(targetType) Then
                    Dim str As String = If(sourceValue.IsNothing, Nothing, sourceValue.StringValue)
                    Dim [char] As Char

                    If str Is Nothing OrElse str.Length = 0 Then
                        [char] = ChrW(0)
                    Else
                        [char] = str(0)
                    End If

                    Return ConstantValue.Create([char])
                End If

            ElseIf IsCharType(sourceType) Then

                If IsStringType(targetType) Then
                    Return ConstantValue.Create(New String(sourceValue.CharValue, 1))
                End If

            Else
                Dim result = TryFoldConstantNumericOrBooleanConversion(
                                sourceValue,
                                sourceType,
                                targetType,
                                integerOverflow)

                Debug.Assert(result Is Nothing OrElse Not result.IsBad OrElse integerOverflow = False)
                Return result
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Attempts to fold conversion of a constant expression.
        ''' 
        ''' Returns Nothing if conversion cannot be folded, i.e. unexpected source and destination types. 
        ''' Returns Bad value (Discriminator = ConstantValueTypeDiscriminator.Bad) if conversion failed due to non-integer overflow. 
        ''' 
        ''' If integer overflow occurs, integerOverflow is set to True and the overflowed result is returned. 
        ''' </summary>
        Private Shared Function TryFoldConstantNumericOrBooleanConversion(
            ByRef sourceValue As ConstantValue,
            sourceType As TypeSymbol,
            targetType As TypeSymbol,
            ByRef integerOverflow As Boolean
        ) As ConstantValue

            integerOverflow = False

            If IsIntegralType(sourceType) OrElse IsBooleanType(sourceType) Then

                If IsNumericType(targetType) OrElse IsBooleanType(targetType) Then

                    Dim value As Int64 = GetConstantValueAsInt64(sourceValue)

                    ' // Converting True to an arithmetic value produces -1.
                    If IsBooleanType(sourceType) AndAlso value <> 0 Then

                        Const BASIC_TRUE As Integer = (-1)

                        If IsUnsignedIntegralType(targetType) Then
                            Dim ignoreOverflow As Boolean = False
                            value = NarrowIntegralResult(BASIC_TRUE, sourceType, targetType, ignoreOverflow)
                        Else
                            value = BASIC_TRUE
                        End If
                    End If

                    Return ConvertIntegralValue(value,
                                                sourceValue.Discriminator,
                                                targetType.GetConstantValueTypeDiscriminator(),
                                                integerOverflow)
                End If

            ElseIf IsFloatingType(sourceType) Then

                If IsNumericType(targetType) OrElse IsBooleanType(targetType) Then

                    Dim result = ConvertFloatingValue(
                            If(sourceValue.Discriminator = ConstantValueTypeDiscriminator.Double, sourceValue.DoubleValue, sourceValue.SingleValue),
                            targetType.GetConstantValueTypeDiscriminator(),
                            integerOverflow)

                    If result.IsBad Then
                        integerOverflow = False
                    End If

                    Return result
                End If

            ElseIf IsDecimalType(sourceType) Then

                If IsNumericType(targetType) OrElse IsBooleanType(targetType) Then

                    Dim result = ConvertDecimalValue(
                            sourceValue.DecimalValue,
                            targetType.GetConstantValueTypeDiscriminator(),
                            integerOverflow)

                    If result.IsBad Then
                        integerOverflow = False
                    End If

                    Return result
                End If

            End If

            Return Nothing
        End Function

        Public Shared Function TryFoldNothingReferenceConversion(
            source As BoundExpression,
            conversion As ConversionKind,
            targetType As TypeSymbol
        ) As ConstantValue

            Debug.Assert(source IsNot Nothing)
            Debug.Assert(targetType IsNot Nothing)
            Debug.Assert(targetType.Kind <> SymbolKind.ErrorType)

            Dim sourceValue As ConstantValue = source.ConstantValueOpt

            Return TryFoldNothingReferenceConversion(sourceValue, conversion, targetType)
        End Function

        Friend Shared Function TryFoldNothingReferenceConversion(
                   sourceValue As ConstantValue,
                   conversion As ConversionKind,
                   targetType As TypeSymbol
               ) As ConstantValue

            Debug.Assert(targetType IsNot Nothing)
            Debug.Assert(targetType.Kind <> SymbolKind.ErrorType)

            If sourceValue Is Nothing OrElse Not sourceValue.IsNothing OrElse
               targetType.IsTypeParameter() OrElse Not targetType.IsReferenceType() Then
                Return Nothing
            End If

            If conversion = ConversionKind.WideningNothingLiteral OrElse
               IsIdentityConversion(conversion) OrElse
               (conversion And ConversionKind.WideningReference) = ConversionKind.WideningReference OrElse
               (conversion And ConversionKind.WideningArray) = ConversionKind.WideningArray Then
                Return sourceValue
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' This function classifies all intrinsic language conversions and user-defined conversions.
        ''' </summary>
        Public Shared Function ClassifyConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As KeyValuePair(Of ConversionKind, MethodSymbol)
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(source.Kind <> SymbolKind.ErrorType)
            Debug.Assert(Not TypeOf source Is ArrayLiteralTypeSymbol)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            Dim predefinedConversion As ConversionKind = ClassifyPredefinedConversion(source, destination, useSiteDiagnostics)

            If ConversionExists(predefinedConversion) Then
                Return New KeyValuePair(Of ConversionKind, MethodSymbol)(predefinedConversion, Nothing)
            End If

            Return ClassifyUserDefinedConversion(source, destination, useSiteDiagnostics)
        End Function

        ''' <summary>
        ''' This function classifies all intrinsic language conversions, such as inheritance,
        ''' implementation, array covariance, and conversions between intrinsic types.
        ''' </summary>
        Public Shared Function ClassifyPredefinedConversion(source As BoundExpression, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            Return ClassifyPredefinedConversion(source, destination, binder, userDefinedConversionsMightStillBeApplicable:=Nothing, useSiteDiagnostics:=useSiteDiagnostics)
        End Function

        Private Shared Function ClassifyPredefinedConversion(
            source As BoundExpression,
            destination As TypeSymbol,
            binder As Binder,
            <Out()> ByRef userDefinedConversionsMightStillBeApplicable As Boolean,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            userDefinedConversionsMightStillBeApplicable = False
            Dim conv As ConversionKind

            ' Using <symbol>.IsConstant() for field accesses can result in an infinite loop.
            ' To detect such a loop pass the already visited constants from the binder.
            Dim sourceIsConstant As Boolean = False
            If source.Kind = BoundKind.FieldAccess Then
                sourceIsConstant = DirectCast(source, BoundFieldAccess).FieldSymbol.GetConstantValue(binder.ConstantFieldsInProgress) IsNot Nothing
            ElseIf source.Kind = BoundKind.Local Then
                sourceIsConstant = DirectCast(source, BoundLocal).LocalSymbol.GetConstantValue(binder) IsNot Nothing
            Else
                sourceIsConstant = source.IsConstant
            End If

            If sourceIsConstant Then

                '§8.8 Widening Conversions
                '•	From the literal Nothing to a type.
                conv = ClassifyNothingLiteralConversion(source, destination)

                If ConversionExists(conv) Then
                    Return conv
                End If

                '§8.8 Widening Conversions
                '•	From the literal 0 to an enumerated type.
                '•	From a constant expression of type ULong, Long, UInteger, Integer, UShort, Short, Byte, or SByte to 
                '   a narrower type, provided the value of the constant expression is within the range of the destination type.
                conv = ClassifyNumericConstantConversion(source, destination, binder)

                If ConversionExists(conv) OrElse FailedDueToNumericOverflow(conv) Then
                    Return conv
                End If
            End If

            If Not (source.IsValue) Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim sourceType As TypeSymbol = source.Type

            If sourceType Is Nothing Then

                userDefinedConversionsMightStillBeApplicable = source.GetMostEnclosedParenthesizedExpression().Kind = BoundKind.ArrayLiteral

                ' The node doesn't have a type yet and reclassification failed.
                Return Nothing ' No conversion
            End If

            If sourceType.Kind <> SymbolKind.ErrorType Then
                Dim predefinedConversion As ConversionKind = ClassifyPredefinedConversion(sourceType, destination, useSiteDiagnostics)

                If ConversionExists(predefinedConversion) Then
                    Return predefinedConversion
                End If

                userDefinedConversionsMightStillBeApplicable = True
            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        ''' <summary>
        ''' This function classifies all intrinsic language conversions and user-defined conversions.
        ''' </summary>
        Public Shared Function ClassifyConversion(source As BoundExpression, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As KeyValuePair(Of ConversionKind, MethodSymbol)
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            Dim conv As ConversionKind

            ' Reclassify lambdas, array literals, etc. 
            conv = ClassifyExpressionReclassification(source, destination, binder, useSiteDiagnostics)
            If ConversionExists(conv) OrElse FailedDueToQueryLambdaBodyMismatch(conv) OrElse
               (conv And (ConversionKind.Lambda Or ConversionKind.FailedDueToArrayLiteralElementConversion)) <> 0 Then
                Return New KeyValuePair(Of ConversionKind, MethodSymbol)(conv, Nothing)
            End If

            Dim userDefinedConversionsMightStillBeApplicable As Boolean = False
            conv = ClassifyPredefinedConversion(source, destination, binder, userDefinedConversionsMightStillBeApplicable, useSiteDiagnostics)

            If ConversionExists(conv) OrElse Not userDefinedConversionsMightStillBeApplicable Then
                Return New KeyValuePair(Of ConversionKind, MethodSymbol)(conv, Nothing)
            End If

            ' There could be some interesting conversions between the source expression 
            ' and the input type of the conversion operator. We might need to keep track 
            ' of them.
            Return ClassifyUserDefinedConversion(source, destination, binder, useSiteDiagnostics)
        End Function

        ''' <summary>
        ''' Reclassify lambdas, array literals, etc. 
        ''' </summary>
        Private Shared Function ClassifyExpressionReclassification(source As BoundExpression, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind

            Select Case source.Kind
                Case BoundKind.Parenthesized
                    If source.Type Is Nothing Then
                        ' Try to reclassify enclosed expression.
                        Return ClassifyExpressionReclassification(DirectCast(source, BoundParenthesized).Expression, destination, binder, useSiteDiagnostics)
                    End If

                Case BoundKind.UnboundLambda
                    Return ClassifyUnboundLambdaConversion(DirectCast(source, UnboundLambda), destination)

                Case BoundKind.QueryLambda
                    Return ClassifyQueryLambdaConversion(DirectCast(source, BoundQueryLambda), destination, binder, useSiteDiagnostics)

                Case BoundKind.GroupTypeInferenceLambda
                    Return ClassifyGroupTypeInferenceLambdaConversion(DirectCast(source, GroupTypeInferenceLambda), destination)

                Case BoundKind.AddressOfOperator
                    Return ClassifyAddressOfConversion(DirectCast(source, BoundAddressOfOperator), destination)

                Case BoundKind.ArrayLiteral
                    Return ClassifyArrayLiteralConversion(DirectCast(source, BoundArrayLiteral), destination, binder, useSiteDiagnostics)

                Case BoundKind.InterpolatedStringExpression
                    Return ClassifyInterpolatedStringConversion(DirectCast(source, BoundInterpolatedStringExpression), destination, binder)

            End Select

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyUnboundLambdaConversion(source As UnboundLambda, destination As TypeSymbol) As ConversionKind
            Dim leastRelaxationLevel As ConversionKind = ConversionKind.DelegateRelaxationLevelNone
            Dim conversionKindExpressionTree As ConversionKind = Nothing ' Set to ConversionKind.ConvertedToExpressionTree if expression tree involved.
            Dim delegateInvoke As MethodSymbol = Nothing

            ' Dev10#626389, Dev10#693976: If you convert a lambda to Object/Delegate/MulticastDelegate, then
            ' the best it can ever be is DelegateRelaxationWideningToNonLambda.
            ' And if you drop the return value from a lambda, the best it can be is DelegateRelaxationLevelWideningDropReturnOrArgs...
            If destination.IsStrictSupertypeOfConcreteDelegate() Then ' covers Object, System.Delegate, System.MulticastDelegate
                leastRelaxationLevel = ConversionKind.DelegateRelaxationLevelWideningToNonLambda

                ' Infer Anonymous Delegate as the target for the lambda.
                Dim anonymousDelegateInfo As KeyValuePair(Of NamedTypeSymbol, ImmutableArray(Of Diagnostic)) = source.InferredAnonymousDelegate

                ' If we have errors for the inference, we know that there is no conversion.
                If Not anonymousDelegateInfo.Value.IsDefault AndAlso anonymousDelegateInfo.Value.HasAnyErrors() Then
                    Return ConversionKind.Lambda ' No conversion
                End If

                delegateInvoke = anonymousDelegateInfo.Key.DelegateInvokeMethod
            Else
                Dim wasExpressionTree As Boolean
                Dim delegateType As NamedTypeSymbol = destination.DelegateOrExpressionDelegate(source.Binder, wasExpressionTree)
                delegateInvoke = If(delegateType IsNot Nothing,
                                    delegateType.DelegateInvokeMethod,
                                    Nothing)
                If wasExpressionTree Then
                    conversionKindExpressionTree = ConversionKind.ConvertedToExpressionTree
                End If

                If delegateInvoke Is Nothing OrElse delegateInvoke.GetUseSiteErrorInfo() IsNot Nothing Then
                    Return ConversionKind.Lambda Or conversionKindExpressionTree ' No conversion
                End If

                If source.IsInferredDelegateForThisLambda(delegateInvoke.ContainingType) Then
                    Dim inferenceDiagnostics As ImmutableArray(Of Diagnostic) = source.InferredAnonymousDelegate.Value

                    ' If we have errors for the inference, we know that there is no conversion.
                    If Not inferenceDiagnostics.IsDefault AndAlso inferenceDiagnostics.HasAnyErrors() Then
                        Return ConversionKind.Lambda Or conversionKindExpressionTree ' No conversion
                    End If
                End If
            End If

            Debug.Assert(delegateInvoke IsNot Nothing)

            Dim bound As BoundLambda = source.Bind(New UnboundLambda.TargetSignature(delegateInvoke))

            Debug.Assert(Not bound.Diagnostics.HasAnyErrors OrElse
                         bound.DelegateRelaxation = ConversionKind.DelegateRelaxationLevelInvalid)

            If bound.DelegateRelaxation = ConversionKind.DelegateRelaxationLevelInvalid Then
                Return ConversionKind.Lambda Or ConversionKind.DelegateRelaxationLevelInvalid Or conversionKindExpressionTree ' No conversion
            End If

            Return ConversionKind.Lambda Or conversionKindExpressionTree Or
                   If(IsNarrowingMethodConversion(bound.MethodConversionKind, isForAddressOf:=False), ConversionKind.Narrowing, ConversionKind.Widening) Or
                   CType(Math.Max(bound.DelegateRelaxation, leastRelaxationLevel), ConversionKind)
        End Function

        Public Shared Function ClassifyArrayLiteralConversion(source As BoundArrayLiteral, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind

            ' § 11.1.1
            ' 9. An array literal can be reclassified as a value. The type of the value is determined as follows:

            ' 9.1. If the reclassification occurs in the context of a conversion where the target type is known and the target type is an array type,
            ' then the array literal is reclassified as a value of type T(). If the target type is System.Collections.Generic.IList(Of T), 
            ' System.Collections.Generic.ICollection(Of T), or System.Collections.Generic.IEnumerable(Of T), and the array literal has one level of nesting, 
            ' then the array literal is reclassified as a value of type T().

            ' 9.2. If the reclassification occurs in the context of a conversion where the target type is known and there is a user-defined conversion 
            ' to the target type from an array type T() or from IList/ICollection/IEnumerable(Of T) as above, then the array literal is reclassified 
            ' as a value of type T().

            ' 9.3. Otherwise, the array literal is reclassified to a value whose type is an array of rank equal to the level of nesting is used, with 
            ' element type determined by the dominant type of the elements in the initializer; if no dominant type can be determined, Object is used.

            ' An array literal {e0,e1} can be converted to...
            '   * an array T() so long as each element can be converted to T
            '   * IEnumerable(Of T) / ICollection(Of T) / IList(Of T) so long as each element can be converted to T
            '   * IEnumerable / ICollection / IList
            '   * System.Array / System.Object
            '
            ' Incidentally, it might be that all elements can each convert to T even though
            ' the array literal doesn't have a dominant type -- e.g. {lambda} or {} have no dominant type.
            ' And if there are two elements {e0,e1} such that e0 narrows to e1 and e1 narrows to e0 then
            ' again there's no dominant type.
            '
            ' Observe that if every element has a reference conversion to T, then the conversions
            ' from {e0,e1} to T()/IEnumerable(Of T)/ICollection(Of T)/IList(Of T) are all predefined CLR conversions.
            ' Observe that the conversions to IEnumerable/ICollection/IList/System.Array/System.Object
            ' are always predefined CLR conversions.

            Dim sourceType = source.InferredType
            Dim targetType = TryCast(destination, NamedTypeSymbol)
            Dim originalTargetType = If(targetType IsNot Nothing, targetType.OriginalDefinition, Nothing)
            Dim targetElementType As TypeSymbol = Nothing
            Dim targetArrayType As ArrayTypeSymbol = TryCast(destination, ArrayTypeSymbol)

            If targetArrayType IsNot Nothing AndAlso (sourceType.Rank = targetArrayType.Rank OrElse source.IsEmptyArrayLiteral) Then

                targetElementType = targetArrayType.ElementType

            ElseIf (sourceType.Rank = 1 OrElse source.IsEmptyArrayLiteral) AndAlso
                originalTargetType IsNot Nothing AndAlso
                (originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IList_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_ICollection_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IReadOnlyList_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IReadOnlyCollection_T) Then

                targetElementType = targetType.TypeArgumentsWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)(0)

            Else
                Dim conv As ConversionKind = ClassifyStringConversion(sourceType, destination)

                If Conversions.NoConversion(conv) Then
                    ' No char() to string conversion
                    conv = ClassifyDirectCastConversion(sourceType, destination, useSiteDiagnostics)
                End If

                If Conversions.NoConversion(conv) Then
                    ' If no predefined conversion then we're done
                    Return Nothing
                End If

                Dim arrayLiteralElementConv = ClassifyArrayInitialization(source.Initializer, sourceType.ElementType, binder, useSiteDiagnostics)

                If Conversions.NoConversion(arrayLiteralElementConv) Then
                    ' No conversion for array elements. Preserve ConversionKind.FailedDueToArrayLiteralElementConversion
                    Return arrayLiteralElementConv
                End If

                If Conversions.IsWideningConversion(conv) Then
                    ' If the source to destination conversion is a widening then return the array literal element conversion.
                    ' That conversion will never be better than widening and it preserves ConversionKind.InvolvesNarrowingFromNumericConstant.
                    Return arrayLiteralElementConv
                End If

                ' Return the ConversionKind.Narrowing because we do not want to propagate any additional conversion kind information.
                Return ConversionKind.Narrowing
            End If

            Return ClassifyArrayInitialization(source.Initializer, targetElementType, binder, useSiteDiagnostics)
        End Function

        Public Shared Function ClassifyInterpolatedStringConversion(source As BoundInterpolatedStringExpression, destination As TypeSymbol, binder As Binder) As ConversionKind

            ' A special conversion exist from an interpolated string expression to System.IFormattable or System.FormattableString.
            If destination.Equals(binder.Compilation.GetWellKnownType(WellKnownType.System_FormattableString)) OrElse
               destination.Equals(binder.Compilation.GetWellKnownType(WellKnownType.System_IFormattable)) _
            Then
                Return ConversionKind.InterpolatedString
            End If

            Return Nothing

        End Function

        Private Shared Function ClassifyArrayInitialization(source As BoundArrayInitialization, targetElementType As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            ' Now we have to check that every element converts to TargetElementType.
            ' It's tempting to say "if the dominant type converts to TargetElementType, then it must be true that
            ' every element converts." But this isn't true for several reasons. First, the dominant type might
            ' be the unique NARROWING candidate among the elements in the case that no widest elements existed.
            If targetElementType.IsErrorType Then
                Return ConversionKind.FailedDueToArrayLiteralElementConversion
            End If

            Dim result = ConversionKind.Widening
            Dim mergedInvolvesNarrowingFromNumericConstant As ConversionKind = ConversionKind.InvolvesNarrowingFromNumericConstant
            Dim propagateInvolvesNarrowingFromNumericConstant As Boolean = False 'Has a value been set in mergedInvolvesNarrowingFromNumericConstant?

            ' We need to propagate InvolvesNarrowingFromNumericConstant bit. If there is at least one narrowing without the bit, 
            ' it shouldn't be propagated, otherwise if there was a widening or narrowing with this bit, it should be propagated.
            For Each sourceElement In source.Initializers
                Dim elementConv As ConversionKind

                If sourceElement.Kind = BoundKind.ArrayInitialization Then
                    elementConv = ClassifyArrayInitialization(DirectCast(sourceElement, BoundArrayInitialization), targetElementType, binder, useSiteDiagnostics)
                Else
                    elementConv = ClassifyConversion(sourceElement, targetElementType, binder, useSiteDiagnostics).Key
                End If

                If IsNarrowingConversion(elementConv) Then
                    result = ConversionKind.Narrowing

                    'If any narrowing conversion is missing InvolvesNarrowingFromNumericConstant then clear the bit.
                    mergedInvolvesNarrowingFromNumericConstant = mergedInvolvesNarrowingFromNumericConstant And elementConv
                    propagateInvolvesNarrowingFromNumericConstant = True

                ElseIf NoConversion(elementConv) Then
                    result = ConversionKind.FailedDueToArrayLiteralElementConversion
                    propagateInvolvesNarrowingFromNumericConstant = False
                    Exit For
                End If

                If IsWideningConversion(result) AndAlso (elementConv And ConversionKind.InvolvesNarrowingFromNumericConstant) <> 0 Then
                    ' If all conversions up to this point are widening and this element has InvolvesNarrowingFromNumericConstant then
                    ' result should include InvolvesNarrowingFromNumericConstant.

                    Debug.Assert((mergedInvolvesNarrowingFromNumericConstant And ConversionKind.InvolvesNarrowingFromNumericConstant) <> 0)
                    propagateInvolvesNarrowingFromNumericConstant = True
                End If
            Next

            If propagateInvolvesNarrowingFromNumericConstant Then
                result = result Or mergedInvolvesNarrowingFromNumericConstant
            End If

            Return result
        End Function

        Private Shared Function ClassifyAddressOfConversion(source As BoundAddressOfOperator, destination As TypeSymbol) As ConversionKind
            Return Binder.ClassifyAddressOfConversion(source, destination)
        End Function

        Private Shared Function ClassifyQueryLambdaConversion(source As BoundQueryLambda, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            ' The delegate type we're converting to (could be type argument of Expression(Of T)).
            Dim wasExpressionTree As Boolean
            Dim delegateDestination As NamedTypeSymbol = destination.DelegateOrExpressionDelegate(binder, wasExpressionTree)

            If delegateDestination Is Nothing Then
                Return Nothing ' No conversion
            End If

            Dim conversionKindExpressionTree As ConversionKind = If(wasExpressionTree, ConversionKind.ConvertedToExpressionTree, Nothing)
            Dim invoke As MethodSymbol = delegateDestination.DelegateInvokeMethod

            If invoke Is Nothing OrElse invoke.GetUseSiteErrorInfo() IsNot Nothing OrElse invoke.IsSub Then
                Return Nothing ' No conversion
            End If

            ' Parameter types should match.
            If invoke.ParameterCount <> source.LambdaSymbol.ParameterCount Then
                Return Nothing ' No conversion
            End If

            Dim lambdaParams As ImmutableArray(Of ParameterSymbol) = source.LambdaSymbol.Parameters
            Dim invokeParams As ImmutableArray(Of ParameterSymbol) = invoke.Parameters

            For i As Integer = 0 To lambdaParams.Length - 1
                Dim lambdaParam = lambdaParams(i)
                Dim invokeParam = invokeParams(i)

                If lambdaParam.IsByRef <> invokeParam.IsByRef OrElse
                   Not lambdaParam.Type.IsSameTypeIgnoringCustomModifiers(invokeParam.Type) Then
                    Return Nothing ' No conversion
                End If
            Next

            If source.LambdaSymbol.ReturnType Is LambdaSymbol.ReturnTypePendingDelegate Then
                If Not invoke.ReturnType.IsErrorType() Then
                    ' TODO: May need to do classification in a special way when ExprIsOperandOfConditionalBranch==True,
                    '       because we may need to check for IsTrue operator.
                    Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol)

                    If source.ExprIsOperandOfConditionalBranch AndAlso invoke.ReturnType.IsBooleanType() Then
                        conv = ClassifyConversionOfOperandOfConditionalBranch(source.Expression, invoke.ReturnType, binder, Nothing, Nothing, useSiteDiagnostics)
                    Else
                        conv = ClassifyConversion(source.Expression, invoke.ReturnType, binder, useSiteDiagnostics)
                    End If

                    If IsIdentityConversion(conv.Key) Then
                        Return conv.Key And (Not ConversionKind.Identity) Or (ConversionKind.Widening Or ConversionKind.Lambda) Or conversionKindExpressionTree
                    ElseIf NoConversion(conv.Key) Then
                        Return conv.Key Or (ConversionKind.Lambda Or ConversionKind.FailedDueToQueryLambdaBodyMismatch) Or conversionKindExpressionTree
                    ElseIf conv.Value IsNot Nothing Then
                        Debug.Assert((conv.Key And ConversionKind.UserDefined) <> 0)
                        Return (conv.Key And (Not (ConversionKind.UserDefined Or ConversionKind.Nullable))) Or ConversionKind.Lambda Or conversionKindExpressionTree
                    Else
                        Debug.Assert((conv.Key And ConversionKind.UserDefined) = 0)
                        Return conv.Key Or ConversionKind.Lambda Or conversionKindExpressionTree
                    End If
                End If
            ElseIf invoke.ReturnType.IsSameTypeIgnoringCustomModifiers(source.LambdaSymbol.ReturnType) Then
                Return ConversionKind.Widening Or ConversionKind.Lambda Or conversionKindExpressionTree
            End If

            Return Nothing ' No conversion
        End Function

        Public Shared Function ClassifyConversionOfOperandOfConditionalBranch(
            operand As BoundExpression,
            booleanType As TypeSymbol,
            binder As Binder,
            <Out> ByRef applyNullableIsTrueOperator As Boolean,
            <Out> ByRef isTrueOperator As OverloadResolution.OverloadResolutionResult,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As KeyValuePair(Of ConversionKind, MethodSymbol)
            Debug.Assert(operand IsNot Nothing)
            Debug.Assert(booleanType IsNot Nothing)
            Debug.Assert(booleanType.Kind <> SymbolKind.ErrorType)
            Debug.Assert(booleanType.IsBooleanType())

            ' 11.19 Boolean Expressions
            ' A Boolean expression is an expression that can be tested to see if it is true or if it is false.
            ' A type T can be used in a Boolean expression if, in order of preference:
            '  - T is Boolean or Boolean?
            '  - T has a widening conversion to Boolean
            '  - T has a widening conversion to Boolean?
            '  - T defines two pseudo operators, IsTrue and IsFalse.
            '  - T has a narrowing conversion to Boolean? that does not involve a conversion from Boolean to Boolean?.
            '  - T has a narrowing conversion to Boolean.
            '
            ' If a Boolean expression is typed as or converted to Boolean or Boolean?,
            ' then it is true if the value is True and false otherwise. 
            ' Otherwise, a Boolean expression calls the IsTrue operator and returns True if the operator returned True;
            ' otherwise it is false (but never calls the IsFalse operator).
            applyNullableIsTrueOperator = False
            isTrueOperator = Nothing
            Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(operand, booleanType, binder, useSiteDiagnostics)

            ' See if we need to use IsTrue operator.
            If Conversions.IsWideningConversion(conv.Key) Then
                '  - T is Boolean 
                '  - T has a widening conversion to Boolean
                Return conv
            End If

            Dim sourceType = operand.Type

            If sourceType IsNot Nothing AndAlso Not sourceType.IsErrorType() AndAlso Not sourceType.IsObjectType() Then

                Dim nullableOfBoolean As TypeSymbol = Nothing
                Dim convToNullableOfBoolean As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing

                If sourceType.IsNullableOfBoolean() Then
                    ' The source is Boolean?
                    convToNullableOfBoolean = Conversions.Identity
                    nullableOfBoolean = sourceType
                Else
                    Dim nullableOfT As NamedTypeSymbol = booleanType.ContainingAssembly.GetSpecialType(SpecialType.System_Nullable_T)

                    If Not nullableOfT.IsErrorType() AndAlso
                       (sourceType.IsNullableType() OrElse sourceType.CanContainUserDefinedOperators(useSiteDiagnostics)) Then
                        nullableOfBoolean = nullableOfT.Construct(ImmutableArray.Create(Of TypeSymbol)(booleanType))

                        convToNullableOfBoolean = Conversions.ClassifyConversion(operand, nullableOfBoolean, binder, useSiteDiagnostics)
                    End If
                End If

                If Conversions.IsWideningConversion(convToNullableOfBoolean.Key) Then
                    '  - T is Boolean?
                    '  - T has a widening conversion to Boolean?
                    applyNullableIsTrueOperator = True
                    Return convToNullableOfBoolean
                Else
                    ' Is there IsTrue operator that we can use.
                    Dim results As OverloadResolution.OverloadResolutionResult = Nothing

                    If sourceType.CanContainUserDefinedOperators(useSiteDiagnostics) Then
                        results = OverloadResolution.ResolveIsTrueOperator(operand, binder, useSiteDiagnostics)
                    End If

                    If results.BestResult.HasValue Then
                        '  - T defines two pseudo operators, IsTrue and IsFalse.
                        isTrueOperator = results

                        If results.BestResult.Value.Candidate.IsLifted Then
                            applyNullableIsTrueOperator = True
                        End If

                        Debug.Assert(Not results.BestResult.Value.RequiresNarrowingConversion)
                        Return New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.Widening, Nothing)
                    ElseIf Conversions.IsNarrowingConversion(convToNullableOfBoolean.Key) AndAlso
                           Not ((convToNullableOfBoolean.Key And (ConversionKind.UserDefined Or ConversionKind.Nullable)) =
                                        ConversionKind.UserDefined AndAlso
                                convToNullableOfBoolean.Value.ReturnType.IsBooleanType()) Then
                        '  - T has a narrowing conversion to Boolean? that does not involve a conversion from Boolean to Boolean?.
                        applyNullableIsTrueOperator = True
                        Return convToNullableOfBoolean
                    End If
                End If
            End If

            '  - T has a narrowing conversion to Boolean.
            '  - No conversion
            Return conv
        End Function


        Private Shared Function ClassifyGroupTypeInferenceLambdaConversion(source As GroupTypeInferenceLambda, destination As TypeSymbol) As ConversionKind

            Debug.Assert(source.Type Is Nothing) ' Shouldn't try to convert already converted query lambda.

            Dim delegateType As NamedTypeSymbol = destination.DelegateOrExpressionDelegate(source.Binder)
            If delegateType Is Nothing Then
                Return Nothing ' No conversion
            End If

            Dim invoke As MethodSymbol = delegateType.DelegateInvokeMethod

            ' Type of the first parameter of the delegate must be source.TypeOfGroupingKey.
            ' Return type of the delegate must be an Anonymous Type corresponding to the following initializer:
            '   New With {key .$VB$ItAnonymous = <delegates's second parameter> }

            If invoke Is Nothing OrElse invoke.GetUseSiteErrorInfo() IsNot Nothing OrElse invoke.IsSub Then
                Return Nothing ' No conversion
            End If

            ' Parameter types should match.
            Dim lambdaParams As ImmutableArray(Of ParameterSymbol) = source.Parameters

            If invoke.ParameterCount <> lambdaParams.Length Then
                Return Nothing ' No conversion
            End If

            Dim invokeParams As ImmutableArray(Of ParameterSymbol) = invoke.Parameters

            For i As Integer = 0 To lambdaParams.Length - 1
                Dim lambdaParam = lambdaParams(i)
                Dim invokeParam = invokeParams(i)

                If lambdaParam.IsByRef <> invokeParam.IsByRef OrElse
                   (lambdaParam.Type IsNot Nothing AndAlso Not lambdaParam.Type.IsSameTypeIgnoringCustomModifiers(invokeParam.Type)) Then
                    Return Nothing ' No conversion
                End If
            Next

            If Not invoke.ReturnType.IsAnonymousType Then
                Return Nothing ' No conversion
            End If

            Dim returnType = DirectCast(invoke.ReturnType, NamedTypeSymbol)
            Dim anonymousType = DirectCast(returnType, AnonymousTypeManager.AnonymousTypePublicSymbol)

            If anonymousType.Properties.Length <> 1 OrElse
               anonymousType.Properties(0).SetMethod IsNot Nothing OrElse
               Not anonymousType.Properties(0).Name.Equals(StringConstants.ItAnonymous) OrElse
               Not invokeParams(1).Type.IsSameTypeIgnoringCustomModifiers(anonymousType.Properties(0).Type) Then
                Return Nothing ' No conversion
            End If

            Return (ConversionKind.Widening Or ConversionKind.Lambda)
        End Function

        Private Shared Function ClassifyNumericConstantConversion(constantExpression As BoundExpression, destination As TypeSymbol, binder As Binder) As ConversionKind
            Debug.Assert(constantExpression.ConstantValueOpt IsNot Nothing)

            If constantExpression.ConstantValueOpt.IsBad Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim targetDestinationType As TypeSymbol = destination.GetNullableUnderlyingTypeOrSelf()

            '§8.8 Widening Conversions
            '•	From the literal 0 to an enumerated type.
            If constantExpression.IsIntegerZeroLiteral() AndAlso targetDestinationType.IsEnumType() AndAlso
               DirectCast(targetDestinationType, NamedTypeSymbol).EnumUnderlyingType.IsIntegralType() Then

                If targetDestinationType Is destination Then
                    Return ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions
                Else
                    ' Target is nullable, conversion is narrowing, but we need to preserve the fact that it was from literal.
                    Return ConversionKind.NarrowingNullable Or ConversionKind.InvolvesNarrowingFromNumericConstant
                End If
            End If

            Dim sourceType As TypeSymbol = constantExpression.Type

            Debug.Assert(sourceType IsNot Nothing) ' Shouldn't this be a [Nothing] literal?

            If sourceType Is Nothing Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            If sourceType Is destination Then
                Return ConversionKind.Identity
            End If

            Dim conv As ConversionKind = Nothing 'ConversionKind.NoConversions
            Dim integerOverflow As Boolean = False
            Dim result As ConstantValue

            If sourceType.IsIntegralType() Then

                '§8.8 Widening Conversions
                '•	From a constant expression of type ULong, Long, UInteger, Integer, UShort, Short, Byte, or SByte to 
                '   a narrower type, provided the value of the constant expression is within the range of the destination type.

                If targetDestinationType.IsIntegralType() Then

                    conv = FastClassifyPredefinedConversion(sourceType, destination).Value

                    If IsNarrowingConversion(conv) Then

                        conv = conv Or ConversionKind.InvolvesNarrowingFromNumericConstant

                        ' check if the value is within the target range
                        result = TryFoldConstantNumericOrBooleanConversion(constantExpression.ConstantValueOpt, sourceType, targetDestinationType,
                                                                               integerOverflow)

                        Debug.Assert(Not result.IsBad)

                        If Not integerOverflow Then
                            ' Reclassify as widening, but not for a Nullable target type
                            If targetDestinationType Is destination Then
                                conv = (conv And (Not ConversionKind.Narrowing)) Or ConversionKind.Widening
                            End If
                        ElseIf binder.CheckOverflow Then
                            '      Compiler generated code (for example, implementation of GetHashCode for Anonymous Types)
                            '      not always uses project level setting for the option.
                            Debug.Assert(sourceType.AllowsCompileTimeConversions() AndAlso targetDestinationType.AllowsCompileTimeConversions())
                            Return ConversionKind.FailedDueToIntegerOverflow
                        End If
                    Else
                        Debug.Assert(IsWideningConversion(conv))
                    End If

                    Return conv
                End If

            ElseIf sourceType.IsFloatingType() Then

                If targetDestinationType.IsFloatingType() Then
                    conv = FastClassifyPredefinedConversion(sourceType, destination).Value

                    If IsNarrowingConversion(conv) Then
                        conv = conv Or ConversionKind.InvolvesNarrowingFromNumericConstant
                    Else
                        Debug.Assert(IsWideningConversion(conv))
                    End If
                End If
            End If

            ' We need to detect overflow errors for constant conversions during classification to make sure overload resolution 
            ' takes the overflow errors into consideration.
            If Not IsWideningConversion(conv) Then
                Dim underlyingSourceType = sourceType.GetEnumUnderlyingTypeOrSelf()
                Dim underlyingDestination = destination.GetNullableUnderlyingTypeOrSelf().GetEnumUnderlyingTypeOrSelf()

                If underlyingSourceType.IsNumericType() AndAlso underlyingDestination.IsNumericType() Then
                    Debug.Assert(sourceType.AllowsCompileTimeConversions() AndAlso destination.GetNullableUnderlyingTypeOrSelf().AllowsCompileTimeConversions())

                    result = TryFoldConstantNumericOrBooleanConversion(constantExpression.ConstantValueOpt, underlyingSourceType, underlyingDestination,
                                                                       integerOverflow)

                    If result.IsBad Then
                        conv = ConversionKind.FailedDueToNumericOverflow
                    ElseIf integerOverflow AndAlso binder.CheckOverflow Then
                        ' Compiler generated code (for example, implementation of GetHashCode for Anonymous Types)
                        ' not always uses project level setting for the option.
                        Return ConversionKind.FailedDueToIntegerOverflow
                    End If
                End If
            End If

            Return conv
        End Function

        Private Shared Function ClassifyNothingLiteralConversion(constantExpression As BoundExpression, destination As TypeSymbol) As ConversionKind
            '§8.8 Widening Conversions
            '•	From the literal Nothing to a type.

            ' We fold NOTHING conversions, as Dev10 does. And for the purpose of conversion classification, 
            ' Dev10 ignores explicitly converted NOTHING.
            If constantExpression.IsStrictNothingLiteral() Then

                If destination.IsObjectType() AndAlso constantExpression.Type IsNot Nothing AndAlso
                   constantExpression.Type.IsObjectType() Then
                    Return ConversionKind.Identity
                End If

                Return ConversionKind.WideningNothingLiteral
            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Public Shared Function ClassifyDirectCastConversion(
            source As TypeSymbol,
            destination As TypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(source.Kind <> SymbolKind.ErrorType)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            '§8.12 Native Conversions
            'The conversions classified as native conversions are: 
            'identity conversions, default conversions, reference conversions, 
            'array conversions, value type conversions, and type parameter conversions.

            Dim result As ConversionKind

            'Identity/Default conversions
            result = ClassifyIdentityConversion(source, destination)
            If ConversionExists(result) Then
                Return result
            End If

            ' Numeric conversions
            ' CLI spec says "enums shall have a built-in integer type" (ECMA I $8.5.2) and gives
            ' a list of built-in types (ECMA I $8.2.2). The only built-in integer types are i8,ui8,i16,ui16,i32,ui32,i64,ui64
            If source.IsIntegralType() Then
                If destination.TypeKind = TypeKind.Enum AndAlso
                   DirectCast(destination, NamedTypeSymbol).EnumUnderlyingType.Equals(source) Then
                    Return ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions
                End If
            ElseIf destination.IsIntegralType() Then
                If source.TypeKind = TypeKind.Enum AndAlso
                   DirectCast(source, NamedTypeSymbol).EnumUnderlyingType.Equals(destination) Then
                    Return ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions
                End If
            ElseIf source.TypeKind = TypeKind.Enum AndAlso destination.TypeKind = TypeKind.Enum Then
                Dim srcUnderlying = DirectCast(source, NamedTypeSymbol).EnumUnderlyingType

                If srcUnderlying.IsIntegralType() AndAlso
                   srcUnderlying.Equals(DirectCast(destination, NamedTypeSymbol).EnumUnderlyingType) Then
                    Return ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions
                End If
            End If

            'Reference conversions
            result = ClassifyReferenceConversion(source, destination, varianceCompatibilityClassificationDepth:=0, useSiteDiagnostics:=useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            'Array conversions
            result = ClassifyArrayConversion(source, destination, varianceCompatibilityClassificationDepth:=0, useSiteDiagnostics:=useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            'Value Type conversions
            result = ClassifyValueTypeConversion(source, destination, useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            'Type Parameter conversions
            result = ClassifyTypeParameterConversion(source, destination, varianceCompatibilityClassificationDepth:=0, useSiteDiagnostics:=useSiteDiagnostics)

            Return result
        End Function

        Public Shared Function ClassifyDirectCastConversion(source As BoundExpression, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            Dim conv As ConversionKind

            If source.IsConstant Then
                conv = ClassifyNothingLiteralConversion(source, destination)

                If ConversionExists(conv) Then
                    Return conv
                End If
            End If

            ' Reclassify lambdas, array literals, etc. 
            conv = ClassifyExpressionReclassification(source, destination, binder, useSiteDiagnostics)
            If ConversionExists(conv) OrElse (conv And (ConversionKind.Lambda Or ConversionKind.FailedDueToArrayLiteralElementConversion)) <> 0 Then
                Return conv
            End If

            If Not (source.IsValue) Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim sourceType As TypeSymbol = source.Type

            If sourceType Is Nothing Then
                ' The node doesn't have a type yet and reclassification failed.
                Return Nothing ' No conversion
            End If

            If sourceType.Kind <> SymbolKind.ErrorType Then
                Return ClassifyDirectCastConversion(sourceType, destination, useSiteDiagnostics)
            End If

            Return Nothing
        End Function

        Public Shared Function ClassifyTryCastConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(source.Kind <> SymbolKind.ErrorType)
            Debug.Assert(Not TypeOf source Is ArrayLiteralTypeSymbol)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            Dim result As ConversionKind

            result = ClassifyDirectCastConversion(source, destination, useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            Return ClassifyTryCastConversionForTypeParameters(source, destination, useSiteDiagnostics)
        End Function

        Public Shared Function ClassifyTryCastConversion(source As BoundExpression, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            Dim conv As ConversionKind

            If source.IsConstant Then
                conv = ClassifyNothingLiteralConversion(source, destination)

                If ConversionExists(conv) Then
                    Return conv
                End If
            End If

            ' Reclassify lambdas, array literals, etc. 
            conv = ClassifyExpressionReclassification(source, destination, binder, useSiteDiagnostics)
            If ConversionExists(conv) OrElse (conv And (ConversionKind.Lambda Or ConversionKind.FailedDueToArrayLiteralElementConversion)) <> 0 Then
                Return conv
            End If

            If Not (source.IsValue) Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim sourceType As TypeSymbol = source.Type

            If sourceType Is Nothing Then
                ' The node doesn't have a type yet and reclassification failed.
                Return Nothing ' No conversion
            End If

            If sourceType.Kind <> SymbolKind.ErrorType Then
                Return ClassifyTryCastConversion(sourceType, destination, useSiteDiagnostics)
            End If

            Return Nothing
        End Function

        Private Shared Function ClassifyTryCastConversionForTypeParameters(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind

            Dim sourceKind = source.Kind
            Dim destinationKind = destination.Kind

            If sourceKind = SymbolKind.ArrayType AndAlso destinationKind = SymbolKind.ArrayType Then

                Dim sourceArray = DirectCast(source, ArrayTypeSymbol)
                Dim destinationArray = DirectCast(destination, ArrayTypeSymbol)

                Dim sourceElement = sourceArray.ElementType
                Dim destinationElement = destinationArray.ElementType

                If sourceElement.IsReferenceType Then
                    If destinationElement.IsValueType Then
                        Return Nothing 'ConversionKind.NoConversion
                    End If
                ElseIf sourceElement.IsValueType Then
                    If destinationElement.IsReferenceType Then
                        Return Nothing 'ConversionKind.NoConversion
                    End If
                End If

                ' Note that Dev10 compiler does not require arrays rank to match, which is probably
                ' an oversight. Will match the same behavior for now.

                Return ClassifyTryCastConversionForTypeParameters(sourceElement, destinationElement, useSiteDiagnostics)
            End If

            If sourceKind <> SymbolKind.TypeParameter AndAlso destinationKind <> SymbolKind.TypeParameter Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            ' Notation:
            ' T - non class constrained type parameter
            ' TC - class constrained type parameter
            ' CC - Class constraint - eg: CC1 is class constraint of TC1
            ' C - class
            ' NC - Notinheritable class
            '
            ' A - Conversions between Type parameters:
            '
            ' 1. T1 -> T2      Conversion possible because T1 and T2 could be any types and thus be potentially related
            '
            ' 2. TC1 -> T2     Conversion possible because T2 could be any type and thus be potentially related to TC1
            '
            ' 3. T1 -> TC2     Conversion possible because T1 could be any type and thus be potentially related to TC2
            '
            ' 4. TC1 -> TC2    Conversion possible only when CC1 and CC2 are related through inheritance, else
            '                  TC1 and TC2 would always be guaranteed to be classes along 2 completely unrelated
            '                  inheritance hierarchies.
            '
            '
            ' B - Conversions between Type parameter and a non-type parameter type:
            '
            ' 1. T1 -> C2      Conversion possible because T1 could be any type and thus potentially related to C2
            '
            ' 2. C1 -> T2      Conversion possible because T2 could be any type and thus potentially related to C1
            '
            ' 3. TC1 -> C2     Conversion possible only when CC1 and C2 are related through inheritance, else
            '                  TC1 and C2 would always be guaranteed to be classes along unrelated inheritance
            '                  hierarchies.
            '
            ' 4. C1 -> TC2     Conversion possible only when C1 and CC2 are related through inheritance, else
            '                  C1 and CC2 would always be guaranteed to be classes along unrelated inheritance
            '                  hierarchies.
            '
            ' 5. NC1 -> TC2    Conversion possible only when one of NC1 or its bases satisfies constraints of TC2
            '                  because those are the only types related to NC1 that could ever be passed to TC2.
            '
            ' 6. TC1 -> NC2    Conversion possible only when one of NC2 or its bases satisfies constraints of TC1
            '                  because those are the only types related to NC1 that could ever be passed to TC1.
            '
            '
            ' Both A and B above are unified conceptually by treating C1 and C2 to be type parameters constrained
            ' to class constraints C1 and C2 respectively.
            '
            ' Note that type params with the value type constraint i.e "Structure" is treated as a param with a class
            ' constraint of System.ValueType.
            '
            ' Note that the reference type constraint does not affect the try cast conversions because it does not prevent
            ' System.ValueType and System.Enum and so value types can still be related to type params with such constraints.
            '

            Dim src = GetNonInterfaceTypeConstraintOrSelf(source, useSiteDiagnostics)
            Dim dst = GetNonInterfaceTypeConstraintOrSelf(destination, useSiteDiagnostics)

            ' If the class constraint is Nothing, then conversion is possible because the non-class constrained
            ' type parameter can be any type related to the other type or type parameter.
            '
            ' Handles cases: A.1, A.2, A.3, B.1 and B.2
            '
            If src Is Nothing OrElse dst Is Nothing Then
                Return ConversionKind.Narrowing
            End If

            Debug.Assert(src.Kind <> SymbolKind.TypeParameter)
            Debug.Assert(dst.Kind <> SymbolKind.TypeParameter)

            ' partially handles cases: A.4, B.3, B.4, B.5 and B.6
            '
            Dim conv As ConversionKind = ClassifyDirectCastConversion(src, dst, useSiteDiagnostics)

            If IsWideningConversion(conv) Then
                Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)

                ' Since NotInheritable classes cannot be inherited, more conversion possibilities
                ' could be ruled out at compiler time if the NotInheritable class or any of its
                ' base can never be used as a type argument for this type parameter during type
                ' instantiation.
                '
                ' partially handles cases: B.5 and B.6
                '
                If destinationKind = SymbolKind.TypeParameter AndAlso
                   (src.TypeKind <> TypeKind.Class OrElse DirectCast(src, NamedTypeSymbol).IsNotInheritable) AndAlso
                   Not ClassOrBasesSatisfyConstraints(src, DirectCast(destination, TypeParameterSymbol), useSiteDiagnostics) Then
                    Return Nothing 'ConversionKind.NoConversion
                End If

                Return ConversionKind.Narrowing Or (conv And ConversionKind.InvolvesEnumTypeConversions)
            End If

            ' partially handles cases: A.4, B.3 and B.4
            conv = ClassifyDirectCastConversion(dst, src, useSiteDiagnostics)

            If IsWideningConversion(conv) Then
                Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)

                ' Since NotInheritable classes cannot be inherited, more conversion possibilities
                ' could be rules out at compiler time if the NotInheritable class or any of its
                ' base can never be used as a type argument for this type parameter during type
                ' instantiation.
                '
                ' partially handles cases: B.5 and B.6
                '
                If sourceKind = SymbolKind.TypeParameter AndAlso
                   (dst.TypeKind <> TypeKind.Class OrElse DirectCast(dst, NamedTypeSymbol).IsNotInheritable) AndAlso
                   Not ClassOrBasesSatisfyConstraints(dst, DirectCast(source, TypeParameterSymbol), useSiteDiagnostics) Then
                    Return Nothing 'ConversionKind.NoConversion
                End If

                Return ConversionKind.Narrowing Or (conv And ConversionKind.InvolvesEnumTypeConversions)
            End If

            ' No conversion ever possible
            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassOrBasesSatisfyConstraints([class] As TypeSymbol, typeParam As TypeParameterSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            Dim candidate As TypeSymbol = [class]

            While candidate IsNot Nothing
                If ConstraintsHelper.CheckConstraints(constructedSymbol:=Nothing,
                                                      substitution:=Nothing,
                                                      typeParameter:=typeParam,
                                                      typeArgument:=candidate,
                                                      diagnosticsBuilder:=Nothing,
                                                      useSiteDiagnostics:=useSiteDiagnostics) Then
                    Return True
                End If

                candidate = candidate.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
            End While

            Return False
        End Function

        Private Shared Function GetNonInterfaceTypeConstraintOrSelf(type As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As TypeSymbol
            If type.Kind = SymbolKind.TypeParameter Then
                Dim typeParameter = DirectCast(type, TypeParameterSymbol)

                If typeParameter.HasValueTypeConstraint Then
                    ' This method returns System.ValueType if the type parameter has a 'Structure'
                    ' constraint, even if there is a more specific constraint type. We could return
                    ' typeParameter.GetNonInterfaceConstraint(), if not Nothing, but that would be
                    ' a breaking change from Dev10. Specifically, in the following, "TypeOf _1 Is U2"
                    ' would be reported (correctly) as an error ("BC31430: Expression of type 'U1'
                    ' can never be of type 'U2'"). Dev10 does not report an error in this case.
                    '
                    ' Public Overrides Sub M(Of U1 As {Structure, S1}, U2 As {Structure, S2})(_1 As U1)
                    '     If TypeOf _1 Is U2 Then
                    '     End If
                    ' End Sub

                    Dim valueType = typeParameter.ContainingAssembly.GetSpecialType(SpecialType.System_ValueType)
                    Return If(valueType.Kind = SymbolKind.ErrorType, Nothing, valueType)
                End If

                Return typeParameter.GetNonInterfaceConstraint(useSiteDiagnostics)
            End If

            Return type
        End Function

        ''' <summary>
        ''' This function classifies user-defined conversions between two types.
        ''' </summary>
        ''' <param name="source"></param>
        ''' <param name="destination"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function ClassifyUserDefinedConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As KeyValuePair(Of ConversionKind, MethodSymbol)
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(source.Kind <> SymbolKind.ErrorType)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)
            ' ClassifyUserDefinedConversion is the only method that allows the source to be an ArrayLiteralTypeSymbol.

            If IsInterfaceType(source) OrElse
               IsInterfaceType(destination) OrElse
               Not (source.CanContainUserDefinedOperators(useSiteDiagnostics) OrElse destination.CanContainUserDefinedOperators(useSiteDiagnostics)) Then

                Return Nothing 'ConversionKind.NoConversion
            End If

            Return OverloadResolution.ResolveUserDefinedConversion(source, destination, useSiteDiagnostics)
        End Function

        ''' <summary>
        ''' This function classifies user-defined conversions.
        ''' </summary>
        ''' <param name="source"></param>
        ''' <param name="destination"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function ClassifyUserDefinedConversion(source As BoundExpression, destination As TypeSymbol, binder As Binder, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As KeyValuePair(Of ConversionKind, MethodSymbol)
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            Dim sourceType As TypeSymbol = source.Type

            If sourceType Is Nothing Then
                source = source.GetMostEnclosedParenthesizedExpression()
                sourceType = If(source.Kind <> BoundKind.ArrayLiteral, source.Type, New ArrayLiteralTypeSymbol(DirectCast(source, BoundArrayLiteral)))
            End If

            Debug.Assert(sourceType IsNot Nothing)
            Debug.Assert(sourceType.Kind <> SymbolKind.ErrorType)

            Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = ClassifyUserDefinedConversion(sourceType, destination, useSiteDiagnostics)

            If NoConversion(conv.Key) Then
                Return conv
            End If

            If IsNarrowingConversion(conv.Key) Then

                ' Conversion between types can exist, but it might still be unsupported due to numeric overflow detected, etc.
                Dim userDefinedInputType = conv.Value.Parameters(0).Type
                Dim inConversion As ConversionKind

                If (source.Kind <> BoundKind.ArrayLiteral) Then
                    inConversion = ClassifyPredefinedConversion(source, userDefinedInputType, binder, useSiteDiagnostics)
                Else
                    inConversion = ClassifyArrayLiteralConversion(DirectCast(source, BoundArrayLiteral), userDefinedInputType, binder, useSiteDiagnostics)
                End If

                If NoConversion(inConversion) Then
                    Debug.Assert(FailedDueToNumericOverflow(inConversion))
                    ' When we classify user-defined conversion from array literal, we are using the array literal 
                    ' rather than its inferred type and failure due to a numeric overflow should be detected then 
                    ' and ClassifyUserDefinedConversion should return NoConversion.
                    Debug.Assert(source.Kind <> BoundKind.ArrayLiteral)

                    If FailedDueToNumericOverflow(inConversion) Then
                        ' Preserve the fact of numeric overflow.
                        conv = New KeyValuePair(Of ConversionKind, MethodSymbol)((conv.Key And Not ConversionKind.Narrowing) Or
                                                                                 (inConversion And ConversionKind.FailedDueToNumericOverflowMask),
                                                                                 conv.Value)

                        Debug.Assert(NoConversion(conv.Key))
                        Return conv
                    End If

                    Return Nothing
                End If

                ' Need to keep track of the fact that narrowing is from numeric constant.
                If (inConversion And ConversionKind.InvolvesNarrowingFromNumericConstant) <> 0 AndAlso
                   OverloadResolution.IsWidening(conv.Value) AndAlso
                   IsWideningConversion(ClassifyPredefinedConversion(conv.Value.ReturnType, destination, useSiteDiagnostics)) Then

                    Dim newConv As ConversionKind = conv.Key Or ConversionKind.InvolvesNarrowingFromNumericConstant

                    If IsWideningConversion(inConversion) Then
                        ' Treat the whole conversion as widening.
                        newConv = (newConv And Not ConversionKind.Narrowing) Or ConversionKind.Widening
                    End If

                    conv = New KeyValuePair(Of ConversionKind, MethodSymbol)(newConv, conv.Value)
                End If
            End If

            Return conv
        End Function

        ''' <summary>
        ''' This function classifies all intrinsic language conversions, such as inheritance,
        ''' implementation, array covariance, and conversions between intrinsic types.
        ''' </summary>
        Public Shared Function ClassifyPredefinedConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            Debug.Assert(source IsNot Nothing)
            Debug.Assert(destination IsNot Nothing)
            Debug.Assert(source.Kind <> SymbolKind.ErrorType)
            Debug.Assert(Not TypeOf source Is ArrayLiteralTypeSymbol)
            Debug.Assert(destination.Kind <> SymbolKind.ErrorType)

            ' Try using the short-circuit "fast-conversion" path.
            Dim fastConversion = FastClassifyPredefinedConversion(source, destination)
            If fastConversion.HasValue Then
                Return fastConversion.Value
            End If

            Return ClassifyPredefinedConversionSlow(source, destination, useSiteDiagnostics)
        End Function

        Private Shared Function ClassifyPredefinedConversionSlow(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind

            Dim result As ConversionKind

            'Identity/Default conversions
            result = ClassifyIdentityConversion(source, destination)
            If ConversionExists(result) Then
                Return result
            End If

            'Reference conversions
            result = ClassifyReferenceConversion(source, destination, varianceCompatibilityClassificationDepth:=0, useSiteDiagnostics:=useSiteDiagnostics)
            If ConversionExists(result) Then
                Return AddDelegateRelaxationInformationForADelegate(source, destination, result)
            End If

            'Anonymous Delegate conversions
            result = ClassifyAnonymousDelegateConversion(source, destination, useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            'Array conversions
            result = ClassifyArrayConversion(source, destination, varianceCompatibilityClassificationDepth:=0, useSiteDiagnostics:=useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            'Value Type conversions
            result = ClassifyValueTypeConversion(source, destination, useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            'Nullable Value Type conversions
            result = ClassifyNullableConversion(source, destination, useSiteDiagnostics)
            If ConversionExists(result) Then
                Return result
            End If

            'String conversions
            result = ClassifyStringConversion(source, destination)
            If ConversionExists(result) Then
                Return result
            End If

            'Type Parameter conversions
            result = ClassifyTypeParameterConversion(source, destination, varianceCompatibilityClassificationDepth:=0, useSiteDiagnostics:=useSiteDiagnostics)

            Return AddDelegateRelaxationInformationForADelegate(source, destination, result)
        End Function

        Private Shared Function AddDelegateRelaxationInformationForADelegate(source As TypeSymbol, destination As TypeSymbol, convKind As ConversionKind) As ConversionKind
            Debug.Assert((convKind And ConversionKind.DelegateRelaxationLevelMask) = 0)

            ' Dev10#703313: for e.g. Func(Of String)->Func(Of Object) we have to record it as DelegateRelaxationLevelWidening.
            ' for e.g. Func(Of String)->Object we have to record it as DelegateRelaxationLevelWideningToNonLambda
            If source.IsDelegateType() Then
                convKind = convKind And (Not ConversionKind.DelegateRelaxationLevelMask)

                If Not ConversionExists(convKind) Then
                    Return convKind Or ConversionKind.DelegateRelaxationLevelInvalid
                ElseIf IsWideningConversion(convKind) Then
                    If IsIdentityConversion(convKind) Then
                        Return convKind
                    ElseIf Not destination.IsDelegateType() OrElse destination.IsStrictSupertypeOfConcreteDelegate() Then
                        Return convKind Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda
                    Else
                        Return convKind Or ConversionKind.DelegateRelaxationLevelWidening
                    End If
                Else
                    Debug.Assert(IsNarrowingConversion(convKind))
                    Return convKind Or ConversionKind.DelegateRelaxationLevelNarrowing
                End If
            End If

            Return convKind
        End Function

        Private Shared Function ClassifyIdentityConversion(source As TypeSymbol, destination As TypeSymbol) As ConversionKind
            '§8.8 Widening Conversions
            'Identity/Default conversions
            '•	From a type to itself.
            '•	From an anonymous delegate type generated for a lambda method reclassification to any delegate type with an identical signature.

            'From a type to itself
            If source.IsSameTypeIgnoringCustomModifiers(destination) Then
                Return ConversionKind.Identity
            End If

            'TODO: From an anonymous delegate type generated for a lambda method reclassification to any delegate type with an identical signature.
            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyReferenceConversion(
            source As TypeSymbol,
            destination As TypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            '§8.8 Widening Conversions
            '•	From a reference type to a base type.
            '•	From a reference type to an interface type, provided that the type 
            '   implements the interface or a variant compatible interface.
            '•	From an interface type to Object.
            '•	From an interface type to a variant compatible interface type.
            '•	From a delegate type to a variant compatible delegate type.
            '§8.9 Narrowing Conversions
            '•	From a reference type to a more derived type.
            '•	From a class type to an interface type, provided the class type does not implement 
            '   the interface type or an interface type variant compatible with it.
            '•	From an interface type to a class type. 
            '•	From an interface type to another interface type, provided there is no inheritance 
            '   relationship between the two types and provided they are not variant compatible.

            If source.SpecialType = SpecialType.System_Void OrElse destination.SpecialType = SpecialType.System_Void Then
                'CLR has nothing to say about conversions of these things.
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim srcIsClassType As Boolean
            Dim srcIsDelegateType As Boolean
            Dim srcIsInterfaceType As Boolean
            Dim srcIsArrayType As Boolean
            Dim dstIsClassType As Boolean
            Dim dstIsDelegateType As Boolean
            Dim dstIsInterfaceType As Boolean
            Dim dstIsArrayType As Boolean

            If Not Conversions.ClassifyAsReferenceType(source, srcIsClassType, srcIsDelegateType, srcIsInterfaceType, srcIsArrayType) OrElse
               Not Conversions.ClassifyAsReferenceType(destination, dstIsClassType, dstIsDelegateType, dstIsInterfaceType, dstIsArrayType) Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            If destination.SpecialType = SpecialType.System_Object Then
                'From an interface type to Object.
                'From a reference type to a base type (shortcut).
                Return ConversionKind.WideningReference
            End If

            If srcIsInterfaceType Then
                If dstIsClassType Then
                    'From an interface type to a class type.
                    Return ConversionKind.NarrowingReference
                ElseIf dstIsArrayType Then
                    ' !!! VB spec doesn't mention this explicitly, but
                    Dim conv As ConversionKind = ClassifyReferenceConversionFromArrayToAnInterface(destination, source, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                    If NoConversion(conv) Then
                        Return Nothing 'ConversionKind.NoConversion
                    End If

                    ' Possibly dropping ConversionKind.VarianceConversionAmbiguity because it is not
                    ' the only reason for the narrowing.
                    Return ConversionKind.NarrowingReference Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                End If
            End If

            If dstIsInterfaceType Then

                Debug.Assert(srcIsInterfaceType OrElse srcIsClassType OrElse srcIsArrayType)

                If (srcIsInterfaceType OrElse srcIsClassType) Then

                    Dim conv As ConversionKind = ToInterfaceConversionClassifier.ClassifyConversionToVariantCompatibleInterface(DirectCast(source, NamedTypeSymbol),
                                                                                                                                   DirectCast(destination, NamedTypeSymbol),
                                                                                                                                   varianceCompatibilityClassificationDepth,
                                                                                                                                   useSiteDiagnostics)

                    If ConversionExists(conv) Then
                        'From an interface type to a variant compatible interface type.
                        'From a reference type to an interface type, provided that the type implements the interface or a variant compatible interface.
                        Debug.Assert((conv And Not (ConversionKind.Widening Or ConversionKind.Narrowing Or
                                                    ConversionKind.InvolvesEnumTypeConversions Or
                                                    ConversionKind.VarianceConversionAmbiguity)) = 0)
                        Return conv Or ConversionKind.Reference
                    End If

                    'From a class type to an interface type, provided the class type does not implement 
                    'the interface type or an interface type variant compatible with it.
                    'From an interface type to another interface type, provided there is no inheritance 
                    'relationship between the two types and provided they are not variant compatible.
                    Return ConversionKind.NarrowingReference

                ElseIf srcIsArrayType Then
                    ' !!! Spec doesn't mention this conversion explicitly.
                    Return ClassifyReferenceConversionFromArrayToAnInterface(source, destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                End If

            Else
                Debug.Assert(dstIsClassType OrElse dstIsArrayType)

                If (srcIsClassType OrElse srcIsArrayType) Then

                    If dstIsClassType AndAlso IsDerivedFrom(source, destination, useSiteDiagnostics) Then
                        'From a reference type to a base type.
                        Return ConversionKind.WideningReference

                    ElseIf srcIsClassType AndAlso IsDerivedFrom(destination, source, useSiteDiagnostics) Then
                        'From a reference type to a more derived type.
                        Return ConversionKind.NarrowingReference

                    ElseIf srcIsDelegateType AndAlso dstIsDelegateType Then
                        'From a delegate type to a variant compatible delegate type.
                        Dim conv As ConversionKind = ClassifyConversionToVariantCompatibleDelegateType(DirectCast(source, NamedTypeSymbol),
                                                                                                       DirectCast(destination, NamedTypeSymbol),
                                                                                                       varianceCompatibilityClassificationDepth,
                                                                                                       useSiteDiagnostics)

                        If ConversionExists(conv) Then
                            Debug.Assert((conv And Not (ConversionKind.Widening Or ConversionKind.Narrowing Or
                                                        ConversionKind.InvolvesEnumTypeConversions Or
                                                        ConversionKind.NarrowingDueToContraVarianceInDelegate)) = 0)
                            Return conv Or ConversionKind.Reference
                        ElseIf (conv And ConversionKind.MightSucceedAtRuntime) <> 0 Then
                            Return ConversionKind.MightSucceedAtRuntime
                        End If
                    End If
                End If

            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyReferenceConversionFromArrayToAnInterface(
            source As TypeSymbol,
            destination As TypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            Debug.Assert(source IsNot Nothing AndAlso Conversions.IsArrayType(source))
            Debug.Assert(destination IsNot Nothing AndAlso Conversions.IsInterfaceType(destination))

            'Check interfaces implemented by System.Array first.
            Dim base = source.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)

            If base IsNot Nothing Then
                If Not base.IsErrorType() AndAlso base.TypeKind = TypeKind.Class AndAlso
                   IsWideningConversion(ClassifyDirectCastConversion(base, destination, useSiteDiagnostics)) Then
                    'From a reference type to an interface type, provided that the type implements the interface or a variant compatible interface.
                    Return ConversionKind.WideningReference
                End If
            End If

            Dim array = DirectCast(source, ArrayTypeSymbol)

            'For one-dimensional arrays, if the target interface is IList(Of U) or ICollection(Of U) or IEnumerable(Of U),
            'look for any conversions that start with array covariance T()->U()
            'and then have a single array-generic conversion step U()->IList/ICollection/IEnumerable(Of U)
            If Not array.IsSZArray Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim dstUnderlying = DirectCast(destination.OriginalDefinition, NamedTypeSymbol)

            If dstUnderlying Is destination OrElse dstUnderlying.Kind = SymbolKind.ErrorType Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim dstUnderlyingSpecial = dstUnderlying.SpecialType

            If dstUnderlyingSpecial <> SpecialType.System_Collections_Generic_IList_T AndAlso
               dstUnderlyingSpecial <> SpecialType.System_Collections_Generic_ICollection_T AndAlso
               dstUnderlyingSpecial <> SpecialType.System_Collections_Generic_IEnumerable_T AndAlso
               dstUnderlyingSpecial <> SpecialType.System_Collections_Generic_IReadOnlyList_T AndAlso
               dstUnderlyingSpecial <> SpecialType.System_Collections_Generic_IReadOnlyCollection_T Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim dstUnderlyingElement = DirectCast(destination, NamedTypeSymbol).TypeArgumentsWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)(0)

            If dstUnderlyingElement.Kind = SymbolKind.ErrorType Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim arrayElement = array.ElementType

            If arrayElement.Kind = SymbolKind.ErrorType Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            If arrayElement.IsSameTypeIgnoringCustomModifiers(dstUnderlyingElement) Then
                Return ConversionKind.WideningReference
            End If

            Dim conv As ConversionKind = ClassifyArrayConversionBasedOnElementTypes(arrayElement, dstUnderlyingElement, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
            If IsWideningConversion(conv) Then
                Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)
                Return ConversionKind.WideningReference Or (conv And ConversionKind.InvolvesEnumTypeConversions)
            ElseIf IsNarrowingConversion(conv) Then
                Return ConversionKind.NarrowingReference Or
                       (conv And (ConversionKind.InvolvesEnumTypeConversions Or ConversionKind.VarianceConversionAmbiguity))
            End If

            ' Dev10 #831390 Closely match Orcas behavior of this function to never return ConversionError.
            ' Note, because of this we don't need to do anything special about ConversionKind.MightSucceedAtRuntime bit,
            ' since we are returning narrowing anyway.
            Return ConversionKind.NarrowingReference
        End Function

        Public Shared Function HasWideningDirectCastConversionButNotEnumTypeConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            If source.IsErrorType() OrElse destination.IsErrorType Then
                Return source.IsSameTypeIgnoringCustomModifiers(destination)
            End If

            Dim conv As ConversionKind = ClassifyDirectCastConversion(source, destination, useSiteDiagnostics)

            If Conversions.IsWideningConversion(conv) AndAlso
                (conv And ConversionKind.InvolvesEnumTypeConversions) = 0 Then
                Return True
            End If

            Return False
        End Function

        Private Shared Function ClassifyConversionToVariantCompatibleDelegateType(
            source As NamedTypeSymbol,
            destination As NamedTypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            Debug.Assert(source IsNot Nothing AndAlso Conversions.IsDelegateType(source))
            Debug.Assert(destination IsNot Nothing AndAlso Conversions.IsDelegateType(destination))

            Const validBits As ConversionKind = (ConversionKind.Widening Or ConversionKind.Narrowing Or
                                                 ConversionKind.InvolvesEnumTypeConversions Or
                                                 ConversionKind.VarianceConversionAmbiguity Or
                                                 ConversionKind.MightSucceedAtRuntime Or
                                                 ConversionKind.NarrowingDueToContraVarianceInDelegate)

            Dim forwardConv As ConversionKind = ClassifyImmediateVarianceCompatibility(source, destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
            Debug.Assert((forwardConv And Not validBits) = 0)

            If ConversionExists(forwardConv) Then
                Return forwardConv
            End If

            Dim backwardConv As ConversionKind = ClassifyImmediateVarianceCompatibility(destination, source, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
            Debug.Assert((backwardConv And Not validBits) = 0)

            If ConversionExists(backwardConv) Then
                Return (backwardConv And Not (ConversionKind.Widening Or ConversionKind.NarrowingDueToContraVarianceInDelegate)) Or ConversionKind.Narrowing
            End If

            Return ((forwardConv Or backwardConv) And ConversionKind.MightSucceedAtRuntime)
        End Function


        ''' <summary>
        ''' Helper structure to classify conversions from named types to interfaces
        ''' in accumulating fashion.
        ''' </summary>
        Private Structure ToInterfaceConversionClassifier
            Private _conv As ConversionKind
            Private _match As NamedTypeSymbol

            Public ReadOnly Property Result As ConversionKind
                Get
                    If IsIdentityConversion(_conv) Then
                        Return ConversionKind.Widening
                    End If

                    Debug.Assert(_conv = Nothing OrElse
                                 (_match.HasVariance() AndAlso
                                  (_conv = ConversionKind.Widening OrElse
                                   _conv = ConversionKind.Narrowing OrElse
                                   _conv = (ConversionKind.Widening Or ConversionKind.InvolvesEnumTypeConversions) OrElse
                                   _conv = (ConversionKind.Narrowing Or ConversionKind.InvolvesEnumTypeConversions) OrElse
                                   _conv = (ConversionKind.Narrowing Or ConversionKind.VarianceConversionAmbiguity))))

                    Return _conv
                End Get
            End Property

            <Conditional("DEBUG")>
            Public Sub AssertFoundIdentity()
                Debug.Assert(IsIdentityConversion(_conv))
            End Sub

            Public Shared Function ClassifyConversionToVariantCompatibleInterface(
                source As NamedTypeSymbol,
                destination As NamedTypeSymbol,
                varianceCompatibilityClassificationDepth As Integer,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            ) As ConversionKind
                Dim helper As ToInterfaceConversionClassifier = Nothing
                helper.AccumulateConversionClassificationToVariantCompatibleInterface(source, destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                Return helper.Result
            End Function

            ''' <summary>
            ''' Accumulates classification information about conversion to interface.
            ''' Returns True when classification gets promoted to Identity, this method should not 
            ''' be called after that.
            ''' </summary>
            Public Function AccumulateConversionClassificationToVariantCompatibleInterface(
                source As NamedTypeSymbol,
                destination As NamedTypeSymbol,
                varianceCompatibilityClassificationDepth As Integer,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            ) As Boolean
                Debug.Assert(source IsNot Nothing AndAlso
                             (Conversions.IsInterfaceType(source) OrElse Conversions.IsClassType(source) OrElse Conversions.IsValueType(source)))
                Debug.Assert(destination IsNot Nothing AndAlso Conversions.IsInterfaceType(destination))
                Debug.Assert(Not IsIdentityConversion(_conv))

                If IsIdentityConversion(_conv) Then
                    Return True
                End If

                If Conversions.IsInterfaceType(source) Then
                    ClassifyInterfaceImmediateVarianceCompatibility(source, destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                    Debug.Assert(Not IsIdentityConversion(_conv))
                End If

                For Each [interface] In source.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                    If [interface].IsErrorType() Then
                        Continue For
                    End If

                    If ClassifyInterfaceImmediateVarianceCompatibility([interface], destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics) Then
                        Return True
                    End If
                Next

                Return False
            End Function

            ''' <summary>
            ''' Returns when classification gets promoted to Identity.
            ''' </summary>
            Private Function ClassifyInterfaceImmediateVarianceCompatibility(
                source As NamedTypeSymbol,
                destination As NamedTypeSymbol,
                varianceCompatibilityClassificationDepth As Integer,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            ) As Boolean
                Debug.Assert(Conversions.IsInterfaceType(source) AndAlso Conversions.IsInterfaceType(destination))
                Debug.Assert(Not IsIdentityConversion(_conv))

                Dim addConv As ConversionKind = ClassifyImmediateVarianceCompatibility(source, destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                Debug.Assert((addConv And ConversionKind.NarrowingDueToContraVarianceInDelegate) = 0)

                If (addConv And ConversionKind.MightSucceedAtRuntime) <> 0 Then
                    ' Treat conversions that possibly might succeed at runtime as at least narrowing.
                    addConv = ConversionKind.Narrowing Or (addConv And (ConversionKind.InvolvesEnumTypeConversions Or ConversionKind.VarianceConversionAmbiguity))
                End If

                Const validNonidentityBits As ConversionKind = (ConversionKind.Widening Or ConversionKind.Narrowing Or
                                                                ConversionKind.InvolvesEnumTypeConversions Or
                                                                ConversionKind.VarianceConversionAmbiguity)

                Debug.Assert(IsIdentityConversion(addConv) OrElse (addConv And Not validNonidentityBits) = 0)

                If ConversionExists(addConv) Then
                    If IsIdentityConversion(addConv) Then
                        _conv = ConversionKind.Identity
                        Return True
                    End If

                    If _match IsNot Nothing Then
                        Debug.Assert(ConversionExists(_conv))

                        If (_conv And ConversionKind.VarianceConversionAmbiguity) <> 0 Then
                            Debug.Assert(IsNarrowingConversion(_conv))

                        ElseIf Not _match.IsSameTypeIgnoringCustomModifiers(source) Then
                            ' ambiguity
                            _conv = ConversionKind.Narrowing Or ConversionKind.VarianceConversionAmbiguity
                        Else
                            Debug.Assert(_conv = (addConv And validNonidentityBits))
                        End If
                    Else
                        _match = source
                        _conv = (addConv And validNonidentityBits)
                    End If
                End If

                Return False
            End Function

        End Structure


        Private Shared Function ClassifyImmediateVarianceCompatibility(
            source As NamedTypeSymbol,
            destination As NamedTypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            Debug.Assert(Conversions.IsInterfaceType(source) OrElse Conversions.IsDelegateType(source))
            Debug.Assert(Conversions.IsInterfaceType(destination) OrElse Conversions.IsDelegateType(destination))
            Debug.Assert(Conversions.IsInterfaceType(source) = Conversions.IsInterfaceType(destination))

            If Not source.OriginalDefinition.IsSameTypeIgnoringCustomModifiers(destination.OriginalDefinition) Then
                Return Nothing ' Incompatible.
            End If

            ' *****************
            ' (*) STACK OVERFLOW
            ' There are some computations for variance that will result in an infinite recursion
            ' e.g. the conversion C -> N(Of C) given "Interface N(Of In X)" and "Class C : Implements N(Of N(Of C))"
            ' The CLR spec is recursive on this topic, hence ambiguous, so it's not known whether there
            ' is a conversion. Theoretically we should detect such cases by maintaining a set of all
            ' conversion-pairs (SourceType,TargetType) that we've visited so far in our quest to tell whether
            ' a given conversion is possible: if we revisit a given pair, then we've encountered an ambiguity.
            ' But maintaining such a set is too onerous. So instead we keep a simple "RecursionCount".
            ' Once that reaches above a certain arbitrary limit, we'll return "ConversionNarrowing". That's
            ' our way of saying that the conversion might be allowed by the CLR, or it might not, but we're not
            ' sure. Note that even though we use an arbitrary limit, we're still typesafe.
            Const depthLimit As Integer = 20
            If varianceCompatibilityClassificationDepth > depthLimit Then
                Return ConversionKind.Narrowing
            End If

            varianceCompatibilityClassificationDepth += 1

            ' CLI I $8.7
            ' Given an arbitrary generic type signature X(Of X1,...,Xn), a value of type
            ' X(Of U1,...,Un) can be stored in a location of type X(Of T1,...,Tn) when all
            ' the following hold:
            '  * For each "in Xi" where Ti is not a value-type or generic parameter, we
            '    require that a value of type Ti can be stored in a location Ui. [Note: since
            '    Ti is not a value-type or generic parameter, it must be a reference type; and
            '    references can only be stored in locations that hold references; so we know that
            '    Ui is also a reference type.]
            '  * For each "out Xi" where Ti is not a value-type or generic parameter, we require
            '    that a value of type Ui can be stored in a location of type Ti. [Note: reference
            '    locations can only ever hold values that are references, so we know that Ui is
            '    also a reference type.]
            '  * For each "Xi" neither in nor out where Ti is not a value-type or generic parameter,
            '    we require that Ti must be the exact same type as Ui. [Note: therefore Ui is
            '    also a reference type.]
            '  * For each "Xi" where Ti is a value-type or generic parameter, we require that
            '    Ti must be the exact same type as Ui.
            '
            ' e.g. a class that implements IReadOnly(Of Mammal) can be stored in a location
            ' that holds IReadOnly(Of Animal), by the second bullet point, given IReadOnly(Of Out T),
            ' since a Mammal can be stored in an Animal location.
            ' but IReadOnly(Of Car) cannot be stored, since a Car cannot be stored in an Animal location.

            Dim conversionExists As Boolean = True
            Dim identity As Boolean = True
            Dim atMostNarrowingDueToContraVarianceInDelegate As Boolean = False
            Dim involvesEnumTypeConversions As ConversionKind = Nothing
            Dim varianceConversionAmbiguity As ConversionKind = ConversionKind.VarianceConversionAmbiguity

            Dim classifyingInterfaceConversions As Boolean = Conversions.IsInterfaceType(source)

            Do
                Dim typeParameters As ImmutableArray(Of TypeParameterSymbol) = source.TypeParameters
                Dim sourceArguments As ImmutableArray(Of TypeSymbol) = source.TypeArgumentsWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                Dim destinationArguments As ImmutableArray(Of TypeSymbol) = destination.TypeArgumentsWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)

                For i As Integer = 0 To typeParameters.Length - 1

                    Dim sourceArg As TypeSymbol = sourceArguments(i)
                    Dim destinationArg As TypeSymbol = destinationArguments(i)

                    If sourceArg.IsSameTypeIgnoringCustomModifiers(destinationArg) Then
                        Continue For
                    End If

                    If sourceArg.IsErrorType() OrElse destinationArg.IsErrorType() Then
                        Return Nothing ' Incompatible.
                    End If

                    If sourceArg.IsValueType OrElse destinationArg.IsValueType Then
                        Return Nothing ' Incompatible.
                    End If

                    identity = False
                    Dim conv As ConversionKind = Nothing

                    Select Case typeParameters(i).Variance
                        Case VarianceKind.Out
                            conv = Classify_Reference_Array_TypeParameterConversion(sourceArg, destinationArg, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                            If Not classifyingInterfaceConversions AndAlso IsNarrowingConversion(conv) AndAlso
                               (conv And ConversionKind.NarrowingDueToContraVarianceInDelegate) <> 0 Then
                                ' Dev10 #820752 
                                atMostNarrowingDueToContraVarianceInDelegate = True
                                varianceConversionAmbiguity = Nothing
                                Continue For
                            End If

                        Case VarianceKind.In
                            conv = Classify_Reference_Array_TypeParameterConversion(destinationArg, sourceArg, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                            If Not classifyingInterfaceConversions AndAlso Not IsWideningConversion(conv) AndAlso
                               destinationArg.IsReferenceType AndAlso sourceArg.IsReferenceType Then
                                ' Dev10 #820752 For delegates, treat conversion as narrowing if both type arguments are reference types.
                                atMostNarrowingDueToContraVarianceInDelegate = True
                                varianceConversionAmbiguity = Nothing
                                Continue For
                            End If

                        Case Else
                            Return Nothing ' Incompatible.
                    End Select

                    If NoConversion(conv) Then
                        ' Do not give up yet if conversion might succeed at runtime and thus might cause an ambiguity.
                        If (conv And ConversionKind.MightSucceedAtRuntime) = 0 Then
                            Return Nothing ' Incompatible.
                        End If

                        conversionExists = False
                        varianceConversionAmbiguity = Nothing
                    Else
                        If (conv And ConversionKind.InvolvesEnumTypeConversions) <> 0 Then
                            involvesEnumTypeConversions = ConversionKind.InvolvesEnumTypeConversions
                        End If

                        If IsNarrowingConversion(conv) Then
                            conversionExists = False
                            varianceConversionAmbiguity = varianceConversionAmbiguity And conv
                        Else
                            Debug.Assert(Not IsIdentityConversion(conv))
                            Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)
                        End If
                    End If
                Next

                source = source.ContainingType
                destination = destination.ContainingType
            Loop While source IsNot Nothing

            Debug.Assert((Not atMostNarrowingDueToContraVarianceInDelegate AndAlso involvesEnumTypeConversions = 0 AndAlso conversionExists) OrElse Not identity)

            If identity Then
                Return ConversionKind.Identity
            ElseIf Not conversionExists Then
                ' Since we didn't return earlier, conversion might succeed at runtime
                Return ConversionKind.MightSucceedAtRuntime Or varianceConversionAmbiguity Or involvesEnumTypeConversions
            ElseIf atMostNarrowingDueToContraVarianceInDelegate Then
                Debug.Assert(varianceConversionAmbiguity = Nothing)
                Return ConversionKind.Narrowing Or ConversionKind.NarrowingDueToContraVarianceInDelegate Or involvesEnumTypeConversions
            Else
                Return ConversionKind.Widening Or involvesEnumTypeConversions
            End If
        End Function

        Private Shared Function ClassifyAsReferenceType(
            candidate As TypeSymbol,
            ByRef isClassType As Boolean,
            ByRef isDelegateType As Boolean,
            ByRef isInterfaceType As Boolean,
            ByRef isArrayType As Boolean
        ) As Boolean
            Select Case candidate.TypeKind
                Case TypeKind.Class,
                     TypeKind.Module
                    isClassType = True
                    isDelegateType = False
                    isInterfaceType = False
                    isArrayType = False

                Case TypeKind.Delegate
                    isClassType = True
                    isDelegateType = True
                    isInterfaceType = False
                    isArrayType = False

                Case TypeKind.Interface
                    isClassType = False
                    isDelegateType = False
                    isInterfaceType = True
                    isArrayType = False

                Case TypeKind.Array
                    isClassType = False
                    isDelegateType = False
                    isInterfaceType = False
                    isArrayType = True

                Case Else
                    isClassType = False
                    isDelegateType = False
                    isInterfaceType = False
                    isArrayType = False
                    Return False
            End Select

            Return True
        End Function

        Private Shared Function IsClassType(type As TypeSymbol) As Boolean
            Dim typeKind = type.TypeKind
            Return typeKind = TypeKind.Class OrElse typeKind = TypeKind.Module OrElse typeKind = TypeKind.Delegate
        End Function

        Private Shared Function IsValueType(type As TypeSymbol) As Boolean
            Dim typeKind = type.TypeKind
            Return typeKind = TypeKind.Enum OrElse typeKind = TypeKind.Structure
        End Function

        Private Shared Function IsDelegateType(type As TypeSymbol) As Boolean
            Return type.TypeKind = TypeKind.Delegate
        End Function

        Private Shared Function IsArrayType(type As TypeSymbol) As Boolean
            Return type.TypeKind = TypeKind.Array
        End Function

        Private Shared Function IsInterfaceType(type As TypeSymbol) As Boolean
            Return type.IsInterfaceType()
        End Function

        ''' <summary>
        ''' Returns true if and only if baseType is a base class of derivedType.
        ''' </summary>
        ''' <param name="derivedType">
        ''' Derived class type.
        ''' </param>
        ''' <param name="baseType">
        ''' Target base class type.
        ''' </param>
        ''' <returns></returns>
        ''' <remarks>
        ''' </remarks>
        Public Shared Function IsDerivedFrom(derivedType As TypeSymbol, baseType As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            Debug.Assert(derivedType IsNot Nothing AndAlso
                         (Conversions.IsClassType(derivedType) OrElse Conversions.IsArrayType(derivedType) OrElse Conversions.IsValueType(derivedType)))
            Debug.Assert(baseType IsNot Nothing AndAlso Conversions.IsClassType(baseType))

            Return baseType.IsBaseTypeOf(derivedType, useSiteDiagnostics)
        End Function

        Private Shared Function ClassifyAnonymousDelegateConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            '§8.8 Widening Conversions
            '•	From an anonymous delegate type generated for a lambda method reclassification to any wider delegate type.
            '§8.9 Narrowing Conversions
            '•	From an anonymous delegate type generated for a lambda method reclassification to any narrower delegate type.

            Debug.Assert(Not source.IsSameTypeIgnoringCustomModifiers(destination))

            If source.IsAnonymousType AndAlso source.IsDelegateType() AndAlso destination.IsDelegateType() Then
                Dim delegateInvoke As MethodSymbol = DirectCast(destination, NamedTypeSymbol).DelegateInvokeMethod

                If delegateInvoke Is Nothing OrElse delegateInvoke.GetUseSiteErrorInfo() IsNot Nothing Then
                    Return Nothing ' No conversion
                End If

                Dim methodConversion As MethodConversionKind = ClassifyMethodConversionForLambdaOrAnonymousDelegate(delegateInvoke,
                                                                                                                    DirectCast(source, NamedTypeSymbol).DelegateInvokeMethod,
                                                                                                                    useSiteDiagnostics)

                If Not IsDelegateRelaxationSupportedFor(methodConversion) Then
                    Return Nothing 'ConversionKind.NoConversion
                End If

                Dim additionalFlags As ConversionKind = DetermineDelegateRelaxationLevel(methodConversion)

                If IsStubRequiredForMethodConversion(methodConversion) Then
                    additionalFlags = additionalFlags Or ConversionKind.NeedAStub
                End If

                ' Note, intentional change in behavior from Dev10, using AddressOf semantics to detect narrowing.
                ' New behavior is more inline with the language spec:
                ' Section 8.4.2
                '   A compatible delegate type is any delegate type that can be created using a delegate creation 
                '   expression with the anonymous delegate type's Invoke method as a parameter. 
                ' As a result, "zero argument" relaxation is treated as narrowing conversion. Dev10 treats it as 
                ' widening, but, with Option Strict On, reports a conversion error later.
                If Conversions.IsNarrowingMethodConversion(methodConversion, isForAddressOf:=True) Then
                    Return ConversionKind.AnonymousDelegate Or ConversionKind.Narrowing Or additionalFlags
                Else
                    Return ConversionKind.AnonymousDelegate Or ConversionKind.Widening Or additionalFlags
                End If
            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyArrayConversion(
            source As TypeSymbol,
            destination As TypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind

            '§8.8 Widening Conversions
            '•	From an array type S with an element type SE to an array type T with an element type TE, provided all of the following are true:
            '   •	S and T differ only in element type.
            '   •	Both SE and TE are reference types or are type parameters known to be a reference type.
            '   •	A widening reference, array, or type parameter conversion exists from SE to TE.
            '•	From an array type S with an enumerated element type SE to an array type T with an element type TE, 
            '   provided all of the following are true:
            '   •	S and T differ only in element type.
            '   •	TE is the underlying type of SE.
            '§8.9 Narrowing Conversions
            '•	From an array type S with an element type SE, to an array type T with an element type TE, 
            '   provided that all of the following are true:
            '   •	S and T differ only in element type.
            '   •	Both SE and TE are reference types or are type parameters not known to be value types.
            '   •	A narrowing reference, array, or type parameter conversion exists from SE to TE.
            '•	From an array type S with an element type SE to an array type T with an enumerated element type TE, 
            '   provided all of the following are true:
            '   •	S and T differ only in element type.
            '   •	SE is the underlying type of TE.

            If Not Conversions.IsArrayType(source) OrElse Not Conversions.IsArrayType(destination) Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim srcArray = DirectCast(source, ArrayTypeSymbol)
            Dim dstArray = DirectCast(destination, ArrayTypeSymbol)

            If Not srcArray.HasSameShapeAs(dstArray) Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim srcElem = srcArray.ElementType
            Dim dstElem = dstArray.ElementType

            If srcElem.Kind = SymbolKind.ErrorType OrElse dstElem.Kind = SymbolKind.ErrorType Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Return ClassifyArrayConversionBasedOnElementTypes(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
        End Function

        Public Shared Function ClassifyArrayElementConversion(srcElem As TypeSymbol, dstElem As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind

            Dim result As ConversionKind

            'Identity/Default conversions
            result = ClassifyIdentityConversion(srcElem, dstElem)
            If ConversionExists(result) Then
                Return result
            End If

            Return ClassifyArrayConversionBasedOnElementTypes(srcElem, dstElem, varianceCompatibilityClassificationDepth:=0, useSiteDiagnostics:=useSiteDiagnostics)
        End Function

        Friend Shared Function Classify_Reference_Array_TypeParameterConversion(
            srcElem As TypeSymbol,
            dstElem As TypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            Dim conv = ClassifyReferenceConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

            If NoConversion(conv) AndAlso (conv And ConversionKind.MightSucceedAtRuntime) = 0 Then
                conv = ClassifyArrayConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                If NoConversion(conv) AndAlso (conv And ConversionKind.MightSucceedAtRuntime) = 0 Then
                    conv = ClassifyTypeParameterConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                Else
                    Debug.Assert(NoConversion(ClassifyTypeParameterConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, Nothing)))
                End If
            Else
                Debug.Assert(NoConversion(ClassifyArrayConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, Nothing)))
                Debug.Assert(NoConversion(ClassifyTypeParameterConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, Nothing)))
            End If

            Return conv
        End Function


        Private Shared Function ClassifyArrayConversionBasedOnElementTypes(
            srcElem As TypeSymbol,
            dstElem As TypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            '§8.8 Widening Conversions
            '•	From an array type S with an element type SE to an array type T with an element type TE, provided all of the following are true:
            '   •	S and T differ only in element type.
            '   •	Both SE and TE are reference types or are type parameters known to be a reference type.
            '   •	A widening reference, array, or type parameter conversion exists from SE to TE.
            '•	From an array type S with an enumerated element type SE to an array type T with an element type TE, 
            '   provided all of the following are true:
            '   •	S and T differ only in element type.
            '   •	TE is the underlying type of SE.
            '§8.9 Narrowing Conversions
            '•	From an array type S with an element type SE, to an array type T with an element type TE, 
            '   provided that all of the following are true:
            '   •	S and T differ only in element type.
            '   •	Both SE and TE are reference types or are type parameters not known to be value types.
            '   •	A narrowing reference, array, or type parameter conversion exists from SE to TE.
            '•	From an array type S with an element type SE to an array type T with an enumerated element type TE, 
            '   provided all of the following are true:
            '   •	S and T differ only in element type.
            '   •	SE is the underlying type of TE.

            'Shouldn't get here for identity conversion
            Debug.Assert(Not srcElem.IsSameTypeIgnoringCustomModifiers(dstElem))

            Dim srcElemIsValueType As Boolean = srcElem.IsValueType
            Dim dstElemIsValueType As Boolean = dstElem.IsValueType

            If Not srcElemIsValueType AndAlso Not dstElemIsValueType Then

                Dim conv = Classify_Reference_Array_TypeParameterConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                If IsWideningConversion(conv) Then
                    Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)

                    If srcElem.IsReferenceType AndAlso dstElem.IsReferenceType Then

                        'Both SE and TE are reference types or are type parameters known to be a reference type.
                        'A widening reference, array, or type parameter conversion exists from SE to TE.
                        Return ConversionKind.WideningArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)

                    ElseIf srcElem.Kind = SymbolKind.TypeParameter AndAlso
                           dstElem.Kind = SymbolKind.TypeParameter Then

                        ' Important: We have widening conversion between two type parameters. 
                        ' This can happen only if srcElem is directly or indirectly constrained by dstElem.

                        If srcElem.IsReferenceType Then
                            Debug.Assert(Not dstElem.IsReferenceType)

                            'srcElem is constrained by dstElem and srcElem is a reference type.
                            'Therefore, we can infer that dstElem is known to be a reference type, 
                            'assuming there are no conflicting constraints.
                            Debug.Assert(Not dstElemIsValueType) ' enforced by one of the outer "If"s

                            'Both SE and TE are reference types or are type parameters known to be a reference type.
                            'A widening reference, array, or type parameter conversion exists from SE to TE.
                            Return ConversionKind.WideningArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)

                        ElseIf dstElem.IsReferenceType Then
                            'srcElem is constrained by dstElem
                            'srcElem is not known to be a reference type
                            'srcElem is not known to be a value type. 

                            ' !!! Spec doesn't mention this explicitly, but Dev10 compiler treats this
                            ' !!! as narrowing conversion. BTW, it looks like C# compiler doesn't support this conversion.
                            Return ConversionKind.NarrowingArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                        Else
                            'srcElem is constrained by dstElem
                            'Both are not known to be a reference type, 
                            'both are not known to be a value type. 

                            ' !!! Spec doesn't mention this explicitly, but Dev10 compiler treats this
                            ' !!! as narrowing conversion. BTW, it looks like C# compiler doesn't support this conversion.
                            Return ConversionKind.NarrowingArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                        End If

                    ElseIf srcElem.Kind = SymbolKind.TypeParameter OrElse
                       dstElem.Kind = SymbolKind.TypeParameter Then

                        ' One and only one is a type parameter

                        ' !!! Spec doesn't mention this explicitly, but Dev10 compiler treats this
                        ' !!! as narrowing conversion. 
                        Return ConversionKind.NarrowingArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                    End If

                ElseIf IsNarrowingConversion(conv) Then
                    Debug.Assert(Not srcElemIsValueType AndAlso Not dstElemIsValueType)
                    'Both SE and TE are reference types or are type parameters not known to be value types.
                    'A narrowing reference, array, or type parameter conversion exists from SE to TE.
                    Return ConversionKind.NarrowingArray Or
                           (conv And (ConversionKind.InvolvesEnumTypeConversions Or ConversionKind.VarianceConversionAmbiguity))

                ElseIf (conv And ConversionKind.MightSucceedAtRuntime) <> 0 Then
                    ' Preserve the fact that conversion might succeed at runtime.
                    Return ConversionKind.MightSucceedAtRuntime
                End If

                Return Nothing 'ConversionKind.NoConversion
            Else

                ' At least one of the elements is known to be a value type
                Debug.Assert(srcElemIsValueType OrElse dstElemIsValueType)

                If srcElemIsValueType Then
                    If dstElemIsValueType Then

                        Dim mightSucceedAtRuntime As ConversionKind = Nothing

                        If srcElem.Kind = SymbolKind.TypeParameter OrElse
                           dstElem.Kind = SymbolKind.TypeParameter Then
                            ' Must be the same type if there is a conversion. 
                            Dim conv = ClassifyTypeParameterConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                            Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)

                            If IsWideningConversion(conv) Then
                                ' !!! Spec doesn't mention this explicitly, but Dev10 compiler treats this
                                ' !!! as widening conversion.
                                Return ConversionKind.WideningArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                            ElseIf IsNarrowingConversion(conv) Then
                                ' !!! Spec doesn't mention this explicitly, but Dev10 compiler treats this
                                ' !!! as narrowing conversion.
                                Return ConversionKind.NarrowingArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                            End If

                            If (conv And ConversionKind.MightSucceedAtRuntime) <> 0 Then
                                mightSucceedAtRuntime = ConversionKind.MightSucceedAtRuntime
                            End If
                        End If

                        Dim srcValueType As TypeSymbol = srcElem
                        Dim dstValueType As TypeSymbol = dstElem

                        If srcElem.Kind = SymbolKind.TypeParameter Then
                            Dim valueType = GetValueTypeConstraint(srcElem, useSiteDiagnostics)

                            If valueType IsNot Nothing Then
                                srcValueType = valueType
                            End If
                        End If

                        If dstElem.Kind = SymbolKind.TypeParameter Then
                            Dim valueType = GetValueTypeConstraint(dstElem, useSiteDiagnostics)

                            If valueType IsNot Nothing Then
                                dstValueType = valueType
                            End If
                        End If

                        Dim srcUnderlying As NamedTypeSymbol = GetNonErrorEnumUnderlyingType(srcValueType)
                        Dim dstUnderlying As NamedTypeSymbol = GetNonErrorEnumUnderlyingType(dstValueType)

                        'TODO:
                        ' !!! The following logic is strange, it matches enums to enums and numeric types,
                        ' !!! but it doesn't match numeric types to each other. Looks like an oversight
                        ' !!! in Dev10 compiler. Follow the same logic for now.

                        If srcUnderlying IsNot Nothing Then
                            If IsNumericType(srcUnderlying) Then
                                If dstUnderlying IsNot Nothing Then
                                    If srcUnderlying.Equals(dstUnderlying) Then
                                        ' !!! Spec doesn't mention this explicitly, but Dev10 supports narrowing conversion
                                        ' !!! between arrays of enums, as long as the underlying type is the same.
                                        Return ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions
                                    End If

                                ElseIf srcUnderlying.Equals(dstValueType) Then
                                    'TE is the underlying type of SE.

                                    If dstElem Is dstValueType Then
                                        Return ConversionKind.WideningArray Or ConversionKind.InvolvesEnumTypeConversions
                                    Else
                                        ' Dev10 degrades this to narrowing if dstElem is generic parameter
                                        Return ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions
                                    End If
                                End If
                            End If

                        ElseIf dstUnderlying IsNot Nothing Then
                            If IsNumericType(dstUnderlying) AndAlso dstUnderlying.Equals(srcValueType) Then
                                'SE is the underlying type of TE.
                                Return ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions
                            End If

                        End If

                        If mightSucceedAtRuntime = Nothing Then
                            ' CLR spec $8.7 says that integral()->integral() is possible so long as they have the same bit size.
                            ' It claims that bool is to be taken as the same size as int8/uint8, so allowing e.g. bool()->uint8().
                            ' That isn't allowed in practice by the current CLR runtime, but since it's in the spec,
                            ' we'll return "ConversionKind.MightSucceedAtRuntime" to mean that it might potentially possibly occur.
                            ' Remember that we're in the business here of "OverestimateNarrowingConversions" after all.

                            Dim srcSize As Integer = ArrayElementBitSize(srcValueType)

                            If srcSize > 0 AndAlso srcSize = ArrayElementBitSize(dstValueType) Then
                                mightSucceedAtRuntime = ConversionKind.MightSucceedAtRuntime
                            End If
                        End If

                        Return mightSucceedAtRuntime

                    ElseIf dstElem.Kind = SymbolKind.TypeParameter AndAlso
                           Not dstElem.IsReferenceType Then

                        If srcElem.Kind = SymbolKind.TypeParameter Then
                            Dim conv = ClassifyTypeParameterConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                            If IsWideningConversion(conv) Then
                                ' !!! Spec doesn't mention this explicitly, but Dev10 compiler treats this
                                ' !!! as narrowing conversion.
                                Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)
                                Return ConversionKind.NarrowingArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                            End If

                            Debug.Assert(NoConversion(conv))

                            If (conv And ConversionKind.MightSucceedAtRuntime) <> 0 Then
                                Return ConversionKind.MightSucceedAtRuntime
                            End If

                            Return Nothing 'ConversionKind.NoConversion

                        ElseIf ArrayElementBitSize(srcElem) > 0 Then
                            Return ConversionKind.MightSucceedAtRuntime
                        End If
                    End If

                ElseIf srcElem.Kind = SymbolKind.TypeParameter AndAlso Not srcElem.IsReferenceType Then
                    Debug.Assert(dstElemIsValueType)

                    If dstElem.Kind = SymbolKind.TypeParameter Then

                        Dim conv = ClassifyTypeParameterConversion(srcElem, dstElem, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                        If IsNarrowingConversion(conv) Then
                            ' !!! Spec doesn't mention this explicitly, but Dev10 compiler treats this
                            ' !!! as narrowing conversion.

                            ' Possibly dropping ConversionKind.VarianceConversionAmbiguity because it is not
                            ' the only reason for the narrowing.
                            Return ConversionKind.NarrowingArray Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                        End If

                        Debug.Assert(NoConversion(conv))

                        If (conv And ConversionKind.MightSucceedAtRuntime) <> 0 Then
                            Return ConversionKind.MightSucceedAtRuntime
                        End If

                    ElseIf ArrayElementBitSize(dstElem) > 0 Then
                        Return ConversionKind.MightSucceedAtRuntime
                    End If
                End If

                Return Nothing 'ConversionKind.NoConversion
            End If
        End Function

        Private Shared Function ArrayElementBitSize(type As TypeSymbol) As Integer
            Select Case type.GetEnumUnderlyingTypeOrSelf().SpecialType
                Case SpecialType.System_Byte, SpecialType.System_SByte, SpecialType.System_Boolean
                    Return 8
                Case SpecialType.System_Int16, SpecialType.System_UInt16
                    Return 16
                Case SpecialType.System_Int32, SpecialType.System_UInt32
                    Return 32
                Case SpecialType.System_Int64, SpecialType.System_UInt64
                    Return 64
                Case Else
                    Return 0 ' Unknown
            End Select
        End Function

        Private Shared Function GetValueTypeConstraint(typeParam As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As TypeSymbol

            Dim constraint = DirectCast(typeParam, TypeParameterSymbol).GetNonInterfaceConstraint(useSiteDiagnostics)

            If constraint IsNot Nothing AndAlso constraint.IsValueType Then
                Return constraint
            End If

            Return Nothing
        End Function

        Private Shared Function GetNonErrorEnumUnderlyingType(type As TypeSymbol) As NamedTypeSymbol
            If type.TypeKind = TypeKind.Enum Then
                Dim underlying = DirectCast(type, NamedTypeSymbol).EnumUnderlyingType

                If underlying.Kind <> SymbolKind.ErrorType Then
                    Return underlying
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function ClassifyValueTypeConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            '§8.8 Widening Conversions
            '•	From a value type to a base type.
            '•	From a value type to an interface type that the type implements.
            '§8.9 Narrowing Conversions
            '•	From a reference type to a more derived value type.
            '•	From an interface type to a value type, provided the value type implements the interface type. 

            'System.Void is actually a value type.
            If source.SpecialType = SpecialType.System_Void OrElse destination.SpecialType = SpecialType.System_Void Then
                'CLR has nothing to say about conversions of these things.
                Return Nothing 'ConversionKind.NoConversion
            End If

            If IsValueType(source) Then

                ' Disallow boxing of restricted types
                If Not source.IsRestrictedType() Then

                    If destination.SpecialType = SpecialType.System_Object Then
                        'From a value type to a base type.
                        Return ConversionKind.WideningValue ' Shortcut
                    End If

                    If IsClassType(destination) Then
                        If IsDerivedFrom(source, destination, useSiteDiagnostics) Then
                            'From a value type to a base type.
                            Return ConversionKind.WideningValue
                        End If

                    ElseIf IsInterfaceType(destination) Then

                        Dim conv As ConversionKind = ToInterfaceConversionClassifier.ClassifyConversionToVariantCompatibleInterface(
                                                            DirectCast(source, NamedTypeSymbol),
                                                            DirectCast(destination, NamedTypeSymbol),
                                                            varianceCompatibilityClassificationDepth:=0,
                                                            useSiteDiagnostics:=useSiteDiagnostics)

                        If ConversionExists(conv) Then
                            'From a value type to an interface type that the type implements.
                            ' !!! Note that the spec doesn't mention anything about variance, but 
                            ' !!! it appears to be taken into account by Dev10 compiler.
                            Debug.Assert((conv And Not (ConversionKind.Widening Or ConversionKind.Narrowing Or
                                                        ConversionKind.InvolvesEnumTypeConversions Or
                                                        ConversionKind.VarianceConversionAmbiguity)) = 0)
                            Return conv Or ConversionKind.Value
                        End If
                    End If
                End If

            ElseIf IsValueType(destination) Then

                If source.SpecialType = SpecialType.System_Object Then
                    'From a reference type to a more derived value type.
                    Return ConversionKind.NarrowingValue ' Shortcut
                End If

                If IsClassType(source) Then
                    If IsDerivedFrom(destination, source, useSiteDiagnostics) Then
                        'From a reference type to a more derived value type.
                        Return ConversionKind.NarrowingValue
                    End If

                ElseIf IsInterfaceType(source) Then

                    For Each [interface] In destination.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                        If [interface].IsErrorType() Then
                            Continue For
                        ElseIf [interface].IsSameTypeIgnoringCustomModifiers(source) Then
                            ' From an interface type to a value type, provided the value type implements the interface type.
                            ' Note, variance is not taken into consideration here.
                            Return ConversionKind.NarrowingValue
                        End If
                    Next

                End If
            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyNullableConversion(source As TypeSymbol, destination As TypeSymbol, <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As ConversionKind
            '§8.8 Widening Conversions
            '•	From a type T to the type T?.
            '•	From a type T? to a type S?, where there is a widening conversion from the type T to the type S.
            '•	From a type T to a type S?, where there is a widening conversion from the type T to the type S.
            '•	From a type T? to an interface type that the type T implements.
            '§8.9 Narrowing Conversions
            '•	From a type T? to a type T.
            '•	From a type T? to a type S?, where there is a narrowing conversion from the type T to the type S.
            '•	From a type T to a type S?, where there is a narrowing conversion from the type T to the type S.
            '•	From a type S? to a type T, where there is a conversion from the type S to the type T.

            Dim srcIsNullable As Boolean = source.IsNullableType()
            Dim dstIsNullable As Boolean = destination.IsNullableType()

            If Not srcIsNullable AndAlso Not dstIsNullable Then
                Return Nothing 'ConversionKind.NoConversion
            End If

            Dim srcUnderlying As TypeSymbol = Nothing
            Dim dstUnderlying As TypeSymbol = Nothing

            If srcIsNullable Then
                srcUnderlying = source.GetNullableUnderlyingType()

                If srcUnderlying.Kind = SymbolKind.ErrorType OrElse Not srcUnderlying.IsValueType OrElse srcUnderlying.IsNullableType() Then
                    Return Nothing 'ConversionKind.NoConversion
                End If
            End If

            If dstIsNullable Then
                dstUnderlying = destination.GetNullableUnderlyingType()

                If dstUnderlying.Kind = SymbolKind.ErrorType OrElse Not dstUnderlying.IsValueType OrElse dstUnderlying.IsNullableType() Then
                    Return Nothing 'ConversionKind.NoConversion
                End If
            End If

            If srcIsNullable Then

                Dim conv As ConversionKind

                If dstIsNullable Then
                    'From a type T? to a type S?
                    conv = ClassifyPredefinedConversion(srcUnderlying, dstUnderlying, useSiteDiagnostics)
                    Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)

                    If IsWideningConversion(conv) Then
                        'From a type T? to a type S?, where there is a widening conversion from the type T to the type S.
                        Return ConversionKind.WideningNullable
                    ElseIf IsNarrowingConversion(conv) Then
                        'From a type T? to a type S?, where there is a narrowing conversion from the type T to the type S.
                        Return ConversionKind.NarrowingNullable
                    End If

                ElseIf IsInterfaceType(destination) Then
                    ' !!! Note that the spec doesn't mention anything about variance, but 
                    ' !!! it appears to be taken into account by Dev10 compiler.
                    conv = ClassifyDirectCastConversion(srcUnderlying, destination, useSiteDiagnostics)

                    If IsWideningConversion(conv) Then
                        'From a type T? to an interface type that the type T implements.
                        Return ConversionKind.WideningNullable
                    ElseIf IsNarrowingConversion(conv) Then
                        ' !!! Note, spec doesn't mention this conversion.
                        Return ConversionKind.NarrowingNullable
                    End If

                ElseIf srcUnderlying.IsSameTypeIgnoringCustomModifiers(destination) Then
                    'From a type T? to a type T.
                    Return ConversionKind.NarrowingNullable
                ElseIf ConversionExists(ClassifyPredefinedConversion(srcUnderlying, destination, useSiteDiagnostics)) Then
                    'From a type S? to a type T, where there is a conversion from the type S to the type T.
                    Return ConversionKind.NarrowingNullable
                End If

            Else
                Debug.Assert(dstIsNullable)
                'From a type T to a type S?

                If source.IsSameTypeIgnoringCustomModifiers(dstUnderlying) Then
                    'From a type T to the type T?.
                    Return ConversionKind.WideningNullable
                End If

                Dim conv = ClassifyPredefinedConversion(source, dstUnderlying, useSiteDiagnostics)
                Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)

                If IsWideningConversion(conv) Then
                    'From a type T to a type S?, where there is a widening conversion from the type T to the type S.
                    Return ConversionKind.WideningNullable
                ElseIf IsNarrowingConversion(conv) Then
                    'From a type T to a type S?, where there is a narrowing conversion from the type T to the type S.
                    Return ConversionKind.NarrowingNullable
                End If

            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Public Shared Function ClassifyStringConversion(source As TypeSymbol, destination As TypeSymbol) As ConversionKind
            '§8.8 Widening Conversions
            '•	From Char() to String.
            '§8.9 Narrowing Conversions
            '•	From String to Char().

            Dim shouldBeArray As TypeSymbol

            If source.SpecialType = SpecialType.System_String Then
                shouldBeArray = destination
            ElseIf destination.SpecialType = SpecialType.System_String Then
                shouldBeArray = source
            Else
                Return Nothing 'ConversionKind.NoConversion
            End If

            If shouldBeArray.Kind = SymbolKind.ArrayType Then
                Dim array = DirectCast(shouldBeArray, ArrayTypeSymbol)

                If array.IsSZArray AndAlso array.ElementType.SpecialType = SpecialType.System_Char Then
                    If array Is source Then
                        Return ConversionKind.WideningString
                    Else
                        Return ConversionKind.NarrowingString
                    End If
                End If
            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyTypeParameterConversion(
            source As TypeSymbol,
            destination As TypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            '§8.8 Widening Conversions
            '•	From a type parameter to Object.
            '•	From a type parameter to an interface type constraint or any interface variant compatible with an interface type constraint.
            '•	From a type parameter to an interface implemented by a class constraint.
            '•	From a type parameter to an interface variant compatible with an interface implemented by a class constraint.
            '•	From a type parameter to a class constraint, or a base type of the class constraint.
            '•	From a type parameter T to a type parameter constraint TX, or anything TX has a widening conversion to.

            '§8.9 Narrowing Conversions
            '•	From Object to a type parameter.
            '•	From a type parameter to an interface type, provided the type parameter is not constrained 
            '   to that interface or constrained to a class that implements that interface.
            '•	From an interface type to a type parameter.

            ' !!! The following two narrowing conversions aren't actually honored by VB/C# Dev10 compiler,
            ' !!! I am not going to honor them either.
            '•-	From a type parameter to a derived type of a class constraint.
            '•-	From a type parameter T to anything a type parameter constraint TX has a narrowing conversion to.

            ' !!! The following two narrowing conversions are not mentioned in the spec, but are honored in Dev10:
            '•	From a class constraint, or a base type of the class constraint to a type parameter.
            '•	From a type parameter constraint TX to a type parameter T, or from anything that has narrowing conversion to TX.

            Dim conv As ConversionKind

            If source.Kind = SymbolKind.TypeParameter Then
                conv = ClassifyConversionFromTypeParameter(DirectCast(source, TypeParameterSymbol), destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                If ConversionExists(conv) Then
                    Return conv
                End If
            End If

            If destination.Kind = SymbolKind.TypeParameter Then
                conv = ClassifyConversionToTypeParameter(source, DirectCast(destination, TypeParameterSymbol), varianceCompatibilityClassificationDepth, useSiteDiagnostics)

                If ConversionExists(conv) Then
                    Debug.Assert(IsNarrowingConversion(conv)) ' We are relying on this while classifying conversions from type parameter to avoid need for recursion.
                    Return conv
                End If
            End If

            If source.Kind = SymbolKind.TypeParameter OrElse destination.Kind = SymbolKind.TypeParameter Then
                Return ConversionKind.MightSucceedAtRuntime
            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyConversionFromTypeParameter(
            typeParameter As TypeParameterSymbol,
            destination As TypeSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind

            If destination.SpecialType = SpecialType.System_Object Then
                'From a type parameter to Object.
                Return ConversionKind.WideningTypeParameter
            End If

            Dim queue As ArrayBuilder(Of TypeParameterSymbol) = Nothing
            Dim result As ConversionKind = ClassifyConversionFromTypeParameter(typeParameter, destination, queue, varianceCompatibilityClassificationDepth, useSiteDiagnostics)

            If queue IsNot Nothing Then
                queue.Free()
            End If

            Return result
        End Function


        Private Shared Function ClassifyConversionFromTypeParameter(
            typeParameter As TypeParameterSymbol,
            destination As TypeSymbol,
            <[In], Out> ByRef queue As ArrayBuilder(Of TypeParameterSymbol),
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind

            Dim queueIndex As Integer = 0
            Dim checkedValueTypeConstraint As Boolean = False

            Dim dstIsClassType As Boolean
            Dim dstIsDelegateType As Boolean
            Dim dstIsInterfaceType As Boolean
            Dim dstIsArrayType As Boolean

            Dim convToInterface As ToInterfaceConversionClassifier = Nothing
            Dim destinationInterface As NamedTypeSymbol = Nothing

            ClassifyAsReferenceType(destination, dstIsClassType, dstIsDelegateType, dstIsInterfaceType, dstIsArrayType)

            If dstIsInterfaceType Then
                destinationInterface = DirectCast(destination, NamedTypeSymbol)
            End If

            Do
                If Not checkedValueTypeConstraint AndAlso typeParameter.HasValueTypeConstraint Then
                    If destination.SpecialType = SpecialType.System_ValueType Then
                        ' !!! Not mentioned explicitly in the spec.
                        Return ConversionKind.WideningTypeParameter
                    End If

                    If dstIsInterfaceType Then
                        Dim valueType = typeParameter.ContainingAssembly.GetSpecialType(SpecialType.System_ValueType)

                        If valueType.Kind <> SymbolKind.ErrorType Then
                            ' !!! Not mentioned explicitly in the spec.
                            If convToInterface.AccumulateConversionClassificationToVariantCompatibleInterface(valueType, destinationInterface,
                                                                                                              varianceCompatibilityClassificationDepth,
                                                                                                              useSiteDiagnostics) Then
                                convToInterface.AssertFoundIdentity()
                                Return ConversionKind.WideningTypeParameter
                            End If
                        End If
                    End If

                    checkedValueTypeConstraint = True
                End If

                ' Iterate over the constraints
                For Each constraint As TypeSymbol In typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                    If constraint.Kind = SymbolKind.ErrorType Then
                        Continue For
                    End If

                    If constraint.IsSameTypeIgnoringCustomModifiers(destination) Then
                        'From a type parameter to an interface type constraint
                        'From a type parameter to a class constraint
                        'From a type parameter T to a type parameter constraint TX
                        Return ConversionKind.WideningTypeParameter
                    ElseIf constraint.TypeKind = TypeKind.Enum AndAlso
                       DirectCast(constraint, NamedTypeSymbol).EnumUnderlyingType.IsSameTypeIgnoringCustomModifiers(destination) Then
                        ' !!! Spec doesn't mention this, but Dev10 allows conversion 
                        ' !!! to the underlying type of the enum
                        Return ConversionKind.WideningTypeParameter Or ConversionKind.InvolvesEnumTypeConversions
                    End If

                    Dim constraintIsClassType As Boolean
                    Dim constraintIsDelegateType As Boolean
                    Dim constraintIsInterfaceType As Boolean
                    Dim constraintIsArrayType As Boolean
                    Dim constraintIsValueType As Boolean = False

                    If Not ClassifyAsReferenceType(constraint, constraintIsClassType, constraintIsDelegateType, constraintIsInterfaceType, constraintIsArrayType) Then
                        constraintIsValueType = IsValueType(constraint)
                    End If

                    If dstIsInterfaceType Then
                        ' Conversions to an interface

                        If (constraintIsClassType OrElse constraintIsInterfaceType OrElse constraintIsValueType) Then
                            If convToInterface.AccumulateConversionClassificationToVariantCompatibleInterface(DirectCast(constraint, NamedTypeSymbol),
                                                                                                              destinationInterface,
                                                                                                              varianceCompatibilityClassificationDepth,
                                                                                                              useSiteDiagnostics) Then
                                convToInterface.AssertFoundIdentity()
                                'From a type parameter to any interface variant compatible with an interface type constraint.
                                'From a type parameter to an interface implemented by a class constraint.
                                'From a type parameter to an interface variant compatible with an interface implemented by a class constraint.
                                ' !!! Spec doesn't explicitly mention interfaces implemented by interface constraints, but Dev10 compiler takes them
                                ' !!! into consideration.
                                Return ConversionKind.WideningTypeParameter
                            End If

                        ElseIf constraintIsArrayType Then

                            Dim conv As ConversionKind = ClassifyReferenceConversionFromArrayToAnInterface(constraint, destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                            If IsWideningConversion(conv) Then
                                'From a type parameter to an interface implemented by a class constraint.
                                'From a type parameter to an interface variant compatible with an interface implemented by a class constraint.
                                Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)
                                Return ConversionKind.WideningTypeParameter Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                            End If
                        End If

                    ElseIf dstIsClassType Then
                        If (constraintIsClassType OrElse constraintIsValueType OrElse constraintIsArrayType) AndAlso
                           IsDerivedFrom(constraint, destination, useSiteDiagnostics) Then
                            'From a type parameter to a base type of the class constraint.
                            Return ConversionKind.WideningTypeParameter
                        End If

                    ElseIf dstIsArrayType Then
                        If constraintIsArrayType Then
                            Dim conv As ConversionKind = ClassifyArrayConversion(constraint, destination, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                            If IsWideningConversion(conv) Then
                                ' !!! Spec doesn't explicitly mention array covariance, but Dev10 compiler takes them
                                ' !!! into consideration.
                                Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)
                                Return ConversionKind.WideningTypeParameter Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                            End If

                            ' We don't need to do anything special about ConversionKind.MightSucceedAtRuntime bit here
                            ' because the caller of this function, ClassifyTypeParameterConversion, returns 
                            ' ConversionKind.MightSucceedAtRuntime on its own in case of NoConversion.
                        End If

                        ' Unit test includes scenario for narrowing conversion between arrays. It produces expected result.
                    End If

                    If constraint.Kind = SymbolKind.TypeParameter Then
                        If queue Is Nothing Then
                            queue = ArrayBuilder(Of TypeParameterSymbol).GetInstance()
                        End If

                        'From a type parameter T to anything type parameter constraint TX has a widening conversion to.
                        queue.Add(DirectCast(constraint, TypeParameterSymbol))
                    End If
                Next

                If queue IsNot Nothing Then
                    If queueIndex < queue.Count Then
                        typeParameter = queue(queueIndex)
                        queueIndex += 1
                        Continue Do
                    End If
                End If

                Exit Do
            Loop

            If dstIsInterfaceType Then

                Dim conv As ConversionKind = convToInterface.Result

                If ConversionExists(conv) Then
                    'From a type parameter to any interface variant compatible with an interface type constraint.
                    'From a type parameter to an interface implemented by a class constraint.
                    'From a type parameter to an interface variant compatible with an interface implemented by a class constraint.
                    ' !!! Spec doesn't explicitly mention interfaces implemented by interface constraints, but Dev10 compiler takes them
                    ' !!! into consideration.
                    Debug.Assert((conv And Not (ConversionKind.Widening Or ConversionKind.Narrowing Or
                                                ConversionKind.InvolvesEnumTypeConversions Or
                                                ConversionKind.VarianceConversionAmbiguity)) = 0)
                    Return ConversionKind.TypeParameter Or conv
                End If

                'From a type parameter to an interface type, provided the type parameter is not constrained 
                'to that interface or constrained to a class that implements that interface.
                Return ConversionKind.NarrowingTypeParameter
            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        Private Shared Function ClassifyConversionToTypeParameter(
            source As TypeSymbol,
            typeParameter As TypeParameterSymbol,
            varianceCompatibilityClassificationDepth As Integer,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind
            If source.SpecialType = SpecialType.System_Object Then
                'From Object to a type parameter.
                Return ConversionKind.NarrowingTypeParameter
            End If

            If typeParameter.HasValueTypeConstraint Then
                If source.SpecialType = SpecialType.System_ValueType Then
                    ' !!! Not mentioned explicitly in the spec.
                    Return ConversionKind.NarrowingTypeParameter
                End If

                If IsClassType(source) Then
                    Dim valueType = typeParameter.ContainingAssembly.GetSpecialType(SpecialType.System_ValueType)

                    If valueType.Kind <> SymbolKind.ErrorType AndAlso IsDerivedFrom(valueType, source, useSiteDiagnostics) Then
                        ' !!! Not mentioned explicitly in the spec.
                        Return ConversionKind.NarrowingTypeParameter
                    End If
                End If
            End If

            Dim srcIsClassType As Boolean
            Dim srcIsDelegateType As Boolean
            Dim srcIsInterfaceType As Boolean
            Dim srcIsArrayType As Boolean

            ClassifyAsReferenceType(source, srcIsClassType, srcIsDelegateType, srcIsInterfaceType, srcIsArrayType)

            If srcIsInterfaceType Then
                'From an interface type to a type parameter.
                Return ConversionKind.NarrowingTypeParameter
            Else

                ' Iterate over constraints
                For Each constraint As TypeSymbol In typeParameter.ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                    If constraint.Kind = SymbolKind.ErrorType Then
                        Continue For
                    End If

                    If constraint.IsSameTypeIgnoringCustomModifiers(source) Then
                        'From a class constraint to a type parameter.
                        'From a type parameter constraint TX to a type parameter T
                        Return ConversionKind.NarrowingTypeParameter
                    ElseIf constraint.TypeKind = TypeKind.Enum AndAlso
                       DirectCast(constraint, NamedTypeSymbol).EnumUnderlyingType.IsSameTypeIgnoringCustomModifiers(source) Then
                        ' !!! Spec doesn't mention this, but Dev10 allows conversion 
                        ' !!! from the underlying type of the enum
                        Return ConversionKind.NarrowingTypeParameter Or ConversionKind.InvolvesEnumTypeConversions
                    End If

                    Dim constraintIsClassType As Boolean
                    Dim constraintIsDelegateType As Boolean
                    Dim constraintIsInterfaceType As Boolean
                    Dim constraintIsArrayType As Boolean
                    Dim constraintIsValueType As Boolean = False

                    If Not ClassifyAsReferenceType(constraint, constraintIsClassType, constraintIsDelegateType, constraintIsInterfaceType, constraintIsArrayType) Then
                        constraintIsValueType = IsValueType(constraint)
                    End If

                    If (constraintIsClassType OrElse constraintIsValueType OrElse constraintIsArrayType) Then
                        If srcIsClassType Then
                            If IsDerivedFrom(constraint, source, useSiteDiagnostics) Then
                                'From a base type of the class constraint to a type parameter.
                                Return ConversionKind.NarrowingTypeParameter
                            End If

                        ElseIf srcIsArrayType Then
                            If constraintIsArrayType Then
                                Dim conv = ClassifyArrayConversion(constraint, source, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                                If IsWideningConversion(conv) Then
                                    ' !!! Spec doesn't explicitly mention array covariance, but Dev10 compiler takes them
                                    ' !!! into consideration.
                                    Debug.Assert((conv And ConversionKind.VarianceConversionAmbiguity) = 0)
                                    Return ConversionKind.NarrowingTypeParameter Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                                End If

                                ' We don't need to do anything special about ConversionKind.MightSucceedAtRuntime bit here
                                ' because the caller of this function, ClassifyTypeParameterConversion, returns 
                                ' ConversionKind.MightSucceedAtRuntime on its own in case of NoConversion.
                            End If

                            ' Unit test includes scenario for narrowing conversion between arrays. It produces expected result.
                        End If

                    ElseIf constraint.Kind = SymbolKind.TypeParameter Then
                        Dim conv As ConversionKind = ClassifyTypeParameterConversion(source, constraint, varianceCompatibilityClassificationDepth, useSiteDiagnostics)
                        If IsNarrowingConversion(conv) Then
                            'From anything that has narrowing conversion to a type parameter constraint TX.

                            ' Possibly dropping ConversionKind.VarianceConversionAmbiguity because it is not
                            ' the only reason for the narrowing.
                            Return ConversionKind.NarrowingTypeParameter Or (conv And ConversionKind.InvolvesEnumTypeConversions)
                        End If

                        ' We don't need to do anything special about ConversionKind.MightSucceedAtRuntime bit here
                        ' because the caller of this function, ClassifyTypeParameterConversion, returns 
                        ' ConversionKind.MightSucceedAtRuntime on its own in case of NoConversion.
                    End If
                Next

            End If

            Return Nothing 'ConversionKind.NoConversion
        End Function

        ''' <summary>
        ''' Calculate MethodConversionKind based on required return type conversion.
        ''' 
        ''' TODO: It looks like Dev10 MethodConversionKinds for return are badly named because
        '''       they appear to give classification in the direction opposite to the data
        '''       flow. This is very confusing. However, I am not going to rename them just yet.
        '''       Will do this when all parts are ported and working together, otherwise it will 
        '''       be very hard to port the rest of the feature.
        ''' 
        ''' We are trying to classify conversion between methods
        ''' ConvertFrom(...) As returnTypeOfConvertFromMethod -> ConvertTo(...) As returnTypeOfConvertToMethod
        ''' 
        ''' The relaxation stub would look like:
        ''' Stub(...) As returnTypeOfConvertToMethod
        '''     Return ConvertFrom(...)
        ''' End ... 
        ''' </summary>
        Public Shared Function ClassifyMethodConversionBasedOnReturnType(
            returnTypeOfConvertFromMethod As TypeSymbol,
            returnTypeOfConvertToMethod As TypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As MethodConversionKind
            Debug.Assert(returnTypeOfConvertFromMethod IsNot Nothing)
            Debug.Assert(returnTypeOfConvertToMethod IsNot Nothing)

            If returnTypeOfConvertToMethod.IsVoidType() Then
                If returnTypeOfConvertFromMethod.IsVoidType() Then
                    Return MethodConversionKind.Identity
                Else
                    Return MethodConversionKind.ReturnValueIsDropped
                End If

            ElseIf returnTypeOfConvertFromMethod.IsVoidType() Then
                Return MethodConversionKind.Error_SubToFunction
            End If

            ' Note: this check is done after the void check to still support
            ' comparison of two subs without error messages.
            ' this can happen if e.g. references are missing
            If returnTypeOfConvertFromMethod.IsErrorType OrElse returnTypeOfConvertToMethod.IsErrorType Then
                If returnTypeOfConvertFromMethod Is returnTypeOfConvertToMethod AndAlso
                   returnTypeOfConvertFromMethod Is LambdaSymbol.ReturnTypeVoidReplacement Then
                    Return MethodConversionKind.Identity
                End If

                Return MethodConversionKind.Error_Unspecified
            End If

            Dim typeConversion As ConversionKind = ClassifyConversion(returnTypeOfConvertFromMethod, returnTypeOfConvertToMethod, useSiteDiagnostics).Key

            Dim result As MethodConversionKind

            If IsNarrowingConversion(typeConversion) Then
                result = MethodConversionKind.ReturnIsWidening

            ElseIf IsWideningConversion(typeConversion) Then

                If IsIdentityConversion(typeConversion) Then
                    result = MethodConversionKind.Identity
                Else
                    ' For return type, CLR will not relax on value types, only reference types
                    ' so treat these as relaxations that needs a stub.
                    If Not (returnTypeOfConvertFromMethod.IsReferenceType AndAlso returnTypeOfConvertToMethod.IsReferenceType) OrElse
                       (typeConversion And ConversionKind.UserDefined) <> 0 Then
                        result = MethodConversionKind.ReturnIsIsVbOrBoxNarrowing
                    Else
                        Dim clrTypeConversion = ClassifyDirectCastConversion(returnTypeOfConvertFromMethod, returnTypeOfConvertToMethod, useSiteDiagnostics)

                        If IsWideningConversion(clrTypeConversion) Then
                            result = MethodConversionKind.ReturnIsClrNarrowing
                        Else
                            result = MethodConversionKind.ReturnIsIsVbOrBoxNarrowing
                        End If
                    End If
                End If
            Else
                result = MethodConversionKind.Error_ReturnTypeMismatch
            End If

            Return result
        End Function

        ''' <summary>
        ''' Returns the methods conversions for the given conversion kind
        '''
        ''' We are trying to classify conversion between methods arguments
        ''' delegateInvoke(parameterConvertFrom) -> targetMethod(parameterConvertTo)
        ''' 
        ''' The relaxation stub would look like (stub has same signature as delegate invoke):
        ''' Stub(parameterConvertFrom)
        '''     return targetMethod(parameterConvertTo)
        ''' End Method
        ''' </summary>
        ''' <param name="conversion">The conversion.</param>
        ''' <param name="delegateParameterType">The delegate parameter type.</param>
        Public Shared Function ClassifyMethodConversionBasedOnArgumentConversion(
            conversion As ConversionKind,
            delegateParameterType As TypeSymbol
        ) As MethodConversionKind
            If Conversions.NoConversion(conversion) Then
                Return MethodConversionKind.Error_OverloadResolution
            ElseIf Conversions.IsNarrowingConversion(conversion) Then
                Return MethodConversionKind.OneArgumentIsNarrowing
            ElseIf Not Conversions.IsIdentityConversion(conversion) Then
                Debug.Assert(Conversions.IsWideningConversion(conversion))

                If Conversions.IsCLRPredefinedConversion(conversion) AndAlso
                    delegateParameterType.IsReferenceType Then
                    Return MethodConversionKind.OneArgumentIsClrWidening
                Else
                    Return MethodConversionKind.OneArgumentIsVbOrBoxWidening
                End If
            End If

            Return MethodConversionKind.Identity
        End Function

        Public Shared Function ClassifyMethodConversionForLambdaOrAnonymousDelegate(
            toMethod As MethodSymbol,
            lambdaOrDelegateInvokeSymbol As MethodSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As MethodConversionKind
            Return ClassifyMethodConversionForLambdaOrAnonymousDelegate(New UnboundLambda.TargetSignature(toMethod), lambdaOrDelegateInvokeSymbol, useSiteDiagnostics)
        End Function

        Public Shared Function ClassifyMethodConversionForLambdaOrAnonymousDelegate(
            toMethodSignature As UnboundLambda.TargetSignature,
            lambdaOrDelegateInvokeSymbol As MethodSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As MethodConversionKind

            ' determine conversions based on return type
            Dim methodConversions = Conversions.ClassifyMethodConversionBasedOnReturnType(lambdaOrDelegateInvokeSymbol.ReturnType, toMethodSignature.ReturnType, useSiteDiagnostics)

            ' determine conversions based on arguments
            methodConversions = methodConversions Or ClassifyMethodConversionForLambdaOrAnonymousDelegateBasedOnParameters(toMethodSignature, lambdaOrDelegateInvokeSymbol.Parameters, useSiteDiagnostics)

            Return methodConversions
        End Function

        Public Shared Function ClassifyMethodConversionForEventRaise(
            toDelegateInvokeMethod As MethodSymbol,
            parameters As ImmutableArray(Of ParameterSymbol),
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As MethodConversionKind

            Debug.Assert(toDelegateInvokeMethod.MethodKind = MethodKind.DelegateInvoke)

            Return ClassifyMethodConversionForLambdaOrAnonymousDelegateBasedOnParameters(New UnboundLambda.TargetSignature(toDelegateInvokeMethod), parameters, useSiteDiagnostics)
        End Function

        Private Shared Function ClassifyMethodConversionForLambdaOrAnonymousDelegateBasedOnParameters(
            toMethodSignature As UnboundLambda.TargetSignature,
            parameters As ImmutableArray(Of ParameterSymbol),
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As MethodConversionKind

            ' TODO: Take custom modifiers into account, if needed.
            Dim methodConversions As MethodConversionKind

            ' determine conversions based on arguments
            If parameters.Length = 0 AndAlso toMethodSignature.ParameterTypes.Length > 0 Then
                ' special flag for ignoring all arguments (zero argument relaxation)
                methodConversions = methodConversions Or MethodConversionKind.AllArgumentsIgnored
            ElseIf parameters.Length <> toMethodSignature.ParameterTypes.Length Then
                methodConversions = methodConversions Or MethodConversionKind.Error_OverloadResolution
            Else
                For parameterIndex As Integer = 0 To parameters.Length - 1
                    ' Check ByRef
                    If toMethodSignature.IsByRef(parameterIndex) <> parameters(parameterIndex).IsByRef Then
                        methodConversions = methodConversions Or MethodConversionKind.Error_ByRefByValMismatch
                    End If

                    ' Check conversion
                    Dim toParameterType As TypeSymbol = toMethodSignature.ParameterTypes(parameterIndex)
                    Dim lambdaParameterType As TypeSymbol = parameters(parameterIndex).Type

                    If Not toParameterType.IsErrorType() AndAlso Not lambdaParameterType.IsErrorType() Then
                        methodConversions = methodConversions Or
                                            Conversions.ClassifyMethodConversionBasedOnArgumentConversion(
                                                                     Conversions.ClassifyConversion(toParameterType, lambdaParameterType, useSiteDiagnostics).Key,
                                                                     toParameterType)

                        ' Check copy back conversion.
                        If toMethodSignature.IsByRef(parameterIndex) Then
                            methodConversions = methodConversions Or
                                                Conversions.ClassifyMethodConversionBasedOnArgumentConversion(
                                                                         Conversions.ClassifyConversion(lambdaParameterType, toParameterType, useSiteDiagnostics).Key,
                                                                         lambdaParameterType)
                        End If
                    End If
                Next
            End If

            Return methodConversions
        End Function

        ''' <summary>
        ''' Will set only bits used for delegate relaxation level.
        ''' </summary>
        Public Shared Function DetermineDelegateRelaxationLevelForLambdaReturn(
            expressionOpt As BoundExpression,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As ConversionKind

            If expressionOpt Is Nothing OrElse expressionOpt.Kind <> BoundKind.Conversion OrElse expressionOpt.HasErrors Then
                Return ConversionKind.DelegateRelaxationLevelNone
            End If

            Dim conversion = DirectCast(expressionOpt, BoundConversion)

            If conversion.ExplicitCastInCode Then
                Return ConversionKind.DelegateRelaxationLevelNone
            End If

            ' It is tempting to use ConversionKind from this node, but this would produce incorrect result in some cases
            ' because conversion from an expression to a type and from a type to a type can have different kind.
            ' The node captures the former, but relaxation classification should use latter.
            Dim methodConversion As MethodConversionKind

            Dim operandType As TypeSymbol = conversion.Operand.Type

            If operandType Is Nothing Then
                Debug.Assert(conversion.Operand.IsNothingLiteral() OrElse conversion.Operand.Kind = BoundKind.Lambda)
                methodConversion = MethodConversionKind.Identity
            Else
                methodConversion = ClassifyMethodConversionBasedOnReturnType(operandType, conversion.Type, useSiteDiagnostics)
            End If

            Return DetermineDelegateRelaxationLevel(methodConversion)
        End Function


        ''' <summary>
        ''' Determine the relaxation level of a given conversion. This will be used by
        ''' overload resolution in case of conflict. This is to prevent applications that compiled in VB8
        ''' to fail in VB9 because there are more matches. And the same for flipping strict On to Off.
        ''' 
        ''' Will set only bits used for delegate relaxation level.
        ''' </summary>
        Public Shared Function DetermineDelegateRelaxationLevel(
            methodConversion As MethodConversionKind
        ) As ConversionKind
            Dim result As ConversionKind

            If methodConversion = MethodConversionKind.Identity Then
                result = ConversionKind.DelegateRelaxationLevelNone

            ElseIf Not IsDelegateRelaxationSupportedFor(methodConversion) Then
                result = ConversionKind.DelegateRelaxationLevelInvalid

            ElseIf (methodConversion And (MethodConversionKind.OneArgumentIsNarrowing Or MethodConversionKind.ReturnIsWidening)) <> 0 Then
                result = ConversionKind.DelegateRelaxationLevelNarrowing

            ElseIf (methodConversion And (MethodConversionKind.ReturnValueIsDropped Or MethodConversionKind.AllArgumentsIgnored)) = 0 Then
                result = ConversionKind.DelegateRelaxationLevelWidening

            Else
                result = ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs
            End If

            Return result
        End Function


        Public Shared Function IsDelegateRelaxationSupportedFor(methodConversion As MethodConversionKind) As Boolean
            Return (methodConversion And MethodConversionKind.AllErrorReasons) = 0
        End Function

        ''' <summary>
        ''' Determines whether a stub needed for the delegate creations conversion based on the given method conversions.
        ''' </summary>
        ''' <param name="methodConversions">The method conversions.</param><returns>
        '''   <c>true</c> if a stub needed for conversion; otherwise, <c>false</c>.
        ''' </returns>
        Public Shared Function IsStubRequiredForMethodConversion(methodConversions As MethodConversionKind) As Boolean
            Const methodConversionsRequiringStubs = (MethodConversionKind.OneArgumentIsNarrowing Or
                                                     MethodConversionKind.OneArgumentIsVbOrBoxWidening Or
                                                     MethodConversionKind.ReturnIsWidening Or
                                                     MethodConversionKind.ReturnIsIsVbOrBoxNarrowing Or
                                                     MethodConversionKind.ReturnValueIsDropped Or
                                                     MethodConversionKind.AllArgumentsIgnored Or
                                                     MethodConversionKind.ExcessOptionalArgumentsOnTarget)

            Return (methodConversions And methodConversionsRequiringStubs) <> 0 AndAlso
                   (methodConversions And MethodConversionKind.AllErrorReasons) = 0
        End Function

        ''' <summary>
        ''' Tells whether the method conversion is considered to be narrowing or not.
        ''' </summary>
        Public Shared Function IsNarrowingMethodConversion(
            methodConversion As MethodConversionKind,
            isForAddressOf As Boolean
        ) As Boolean
            Dim checkForBits As MethodConversionKind

            If isForAddressOf Then
                checkForBits = MethodConversionKind.OneArgumentIsNarrowing Or MethodConversionKind.ReturnIsWidening Or MethodConversionKind.AllArgumentsIgnored
            Else
                checkForBits = MethodConversionKind.OneArgumentIsNarrowing Or MethodConversionKind.ReturnIsWidening
            End If

            Return (methodConversion And checkForBits) <> 0
        End Function


        Public Shared Function InvertConversionRequirement(restriction As RequiredConversion) As RequiredConversion

            Debug.Assert(RequiredConversion.Count = 8, "If you've updated the type argument inference restrictions, then please also update InvertConversionRequirement()")

            ' [reverse chain] [None] < AnyReverse < ReverseReference < Identity
            ' [middle  chain] None < [Any,AnyReverse] < AnyConversionAndReverse < Identity
            ' [forward chain] [None] < Any < ArrayElement < Reference < Identity

            ' from reverse chain to forward chain:
            If restriction = RequiredConversion.AnyReverse Then
                Return RequiredConversion.Any
            ElseIf restriction = RequiredConversion.ReverseReference Then
                Return RequiredConversion.Reference
            End If

            ' from forward chain to reverse chain:
            If restriction = RequiredConversion.Any Then
                Return RequiredConversion.AnyReverse
            ElseIf restriction = RequiredConversion.ArrayElement Then
                Debug.Assert(False, "unexpected: ArrayElementConversion restriction has no reverse")
                Return RequiredConversion.ReverseReference
            ElseIf restriction = RequiredConversion.Reference Then
                Return RequiredConversion.ReverseReference
            End If

            ' otherwise we're either in the middle chain, or identity
            Return restriction
        End Function


        ' Strengthens the restriction to at least ReferenceRestriction or ReverseReferenceRestriction
        ' Note: AnyConversionAndReverse strengthens to Identity
        Public Shared Function StrengthenConversionRequirementToReference(restriction As RequiredConversion) As RequiredConversion
            Debug.Assert(RequiredConversion.Count = 8, "If you've updated the type argument inference restrictions, then please also update StrengthenConversionRequirementToReference()")

            ' [reverse chain] [None] < AnyReverse < ReverseReference < Identity
            ' [middle  chain] None < [Any,AnyReverse] < AnyConversionAndReverse < Identity
            ' [forward chain] [None] < Any < ArrayElement < Reference < Identity

            If restriction = RequiredConversion.AnyReverse Then
                Return RequiredConversion.ReverseReference
            ElseIf restriction = RequiredConversion.Any OrElse restriction = RequiredConversion.ArrayElement Then
                Return RequiredConversion.Reference
            ElseIf restriction = RequiredConversion.AnyAndReverse Then
                Return RequiredConversion.Identity
            Else
                Return restriction
            End If
        End Function


        ' Combining inference restrictions: the least upper bound of the two restrictions
        Public Shared Function CombineConversionRequirements(
            restriction1 As RequiredConversion,
            restriction2 As RequiredConversion
        ) As RequiredConversion

            Debug.Assert(RequiredConversion.Count = 8, "If you've updated the type argument inference restrictions, then please also update CombineInferenceRestrictions()")

            ' [reverse chain] [None] < AnyReverse < ReverseReference < Identity
            ' [middle  chain] None < [Any,AnyReverse] < AnyConversionAndReverse < Identity
            ' [forward chain] [None] < Any < ArrayElement < Reference < Identity

            ' identical?
            If restriction1 = restriction2 Then
                Return restriction1
            End If

            ' none?
            If restriction1 = RequiredConversion.None Then
                Return restriction2
            ElseIf restriction2 = RequiredConversion.None Then
                Return restriction1
            End If

            ' forced to the top of the lattice?
            If restriction1 = RequiredConversion.Identity OrElse restriction2 = RequiredConversion.Identity Then
                Return RequiredConversion.Identity
            End If

            ' within the reverse chain?
            If (restriction1 = RequiredConversion.AnyReverse OrElse restriction1 = RequiredConversion.ReverseReference) AndAlso
               (restriction2 = RequiredConversion.AnyReverse OrElse restriction2 = RequiredConversion.ReverseReference) Then
                Return RequiredConversion.ReverseReference
            End If

            ' within the middle chain?
            If (restriction1 = RequiredConversion.Any OrElse restriction1 = RequiredConversion.AnyReverse OrElse restriction1 = RequiredConversion.AnyAndReverse) AndAlso
               (restriction2 = RequiredConversion.Any OrElse restriction2 = RequiredConversion.AnyReverse OrElse restriction2 = RequiredConversion.AnyAndReverse) Then
                Return RequiredConversion.AnyAndReverse
            End If

            ' within the forward chain?
            If (restriction1 = RequiredConversion.Any OrElse restriction1 = RequiredConversion.ArrayElement) AndAlso
               (restriction2 = RequiredConversion.Any OrElse restriction2 = RequiredConversion.ArrayElement) Then
                Return RequiredConversion.ArrayElement

            ElseIf (restriction1 = RequiredConversion.Any OrElse restriction1 = RequiredConversion.ArrayElement OrElse restriction1 = RequiredConversion.Reference) AndAlso
                   (restriction2 = RequiredConversion.Any OrElse restriction2 = RequiredConversion.ArrayElement OrElse restriction2 = RequiredConversion.Reference) Then
                Return RequiredConversion.Reference
            End If

            ' otherwise we've crossed chains
            Return RequiredConversion.Identity
        End Function


        Public Shared Function IsWideningConversion(conv As ConversionKind) As Boolean
            Debug.Assert(NoConversion(conv) OrElse
                        ((conv And ConversionKind.Widening) <> 0) <> ((conv And ConversionKind.Narrowing) <> 0))
            Return (conv And ConversionKind.Widening) <> 0
        End Function

        Public Shared Function IsNarrowingConversion(conv As ConversionKind) As Boolean
            Debug.Assert(NoConversion(conv) OrElse
                        ((conv And ConversionKind.Widening) <> 0) <> ((conv And ConversionKind.Narrowing) <> 0))
            Return (conv And ConversionKind.Narrowing) <> 0
        End Function

        Public Shared Function NoConversion(conv As ConversionKind) As Boolean
            Return (conv And (ConversionKind.Narrowing Or ConversionKind.Widening)) = 0
        End Function

        Public Shared Function ConversionExists(conv As ConversionKind) As Boolean
            Return (conv And (ConversionKind.Narrowing Or ConversionKind.Widening)) <> 0
        End Function

        Public Shared Function IsIdentityConversion(conv As ConversionKind) As Boolean
            Debug.Assert(NoConversion(conv) OrElse
                        ((conv And ConversionKind.Widening) <> 0) <> ((conv And ConversionKind.Narrowing) <> 0))
            Return (conv And ConversionKind.Identity) = ConversionKind.Identity
        End Function

        Public Shared Function FailedDueToNumericOverflow(conv As ConversionKind) As Boolean
            Return (conv And (ConversionKind.Narrowing Or ConversionKind.Widening Or ConversionKind.FailedDueToNumericOverflow)) = ConversionKind.FailedDueToNumericOverflow
        End Function

        Public Shared Function FailedDueToQueryLambdaBodyMismatch(conv As ConversionKind) As Boolean
            Return (conv And (ConversionKind.Narrowing Or ConversionKind.Widening Or ConversionKind.FailedDueToQueryLambdaBodyMismatch)) = ConversionKind.FailedDueToQueryLambdaBodyMismatch
        End Function

        ''' <summary>
        ''' Determines whether the given conversion is CLR supported conversion or not.
        ''' </summary>
        ''' <param name="conversion">The conversion.</param><returns>
        '''   <c>true</c> if the given conversion is a CLR supported conversion; otherwise, <c>false</c>.
        ''' </returns>
        Public Shared Function IsCLRPredefinedConversion(conversion As ConversionKind) As Boolean

            If IsIdentityConversion(conversion) Then
                Return True
            Else
                Const combinedClrConversions = ConversionKind.Reference Or ConversionKind.Array Or ConversionKind.TypeParameter
                If (conversion And combinedClrConversions) <> 0 Then
                    Return True
                End If
            End If

            Return False
        End Function

    End Class

    ''' <summary>
    ''' Used by ClassifyUserDefinedConversion to pass an ArrayTypeSymbol that has a link back to the BoundArrayLiteral node.
    ''' This allows the ClassifyConversionOperatorInOutConversions to properly classify a conversion from the inferred array 
    ''' type to the input type of a user defined conversion.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ArrayLiteralTypeSymbol
        Inherits ArrayTypeSymbol

        Private ReadOnly _arrayLiteral As BoundArrayLiteral

        ''' <summary>
        ''' Create a new ArrayTypeSymbol.
        ''' </summary>
        Friend Sub New(arrayLiteral As BoundArrayLiteral)
            Me._arrayLiteral = arrayLiteral
        End Sub

        Friend ReadOnly Property ArrayLiteral As BoundArrayLiteral
            Get
                Return _arrayLiteral
            End Get
        End Property

        Friend Overrides ReadOnly Property IsSZArray As Boolean
            Get
                Return _arrayLiteral.InferredType.IsSZArray
            End Get
        End Property

        Public Overrides ReadOnly Property Rank As Integer
            Get
                Return _arrayLiteral.InferredType.Rank
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDefaultSizesAndLowerBounds As Boolean
            Get
                Return _arrayLiteral.InferredType.HasDefaultSizesAndLowerBounds
            End Get
        End Property

        Friend Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return _arrayLiteral.InferredType.InterfacesNoUseSiteDiagnostics
            End Get
        End Property

        Friend Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol
            Get
                Return _arrayLiteral.InferredType.BaseTypeNoUseSiteDiagnostics
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _arrayLiteral.InferredType.CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property ElementType As TypeSymbol
            Get
                Return _arrayLiteral.InferredType.ElementType
            End Get
        End Property

        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
