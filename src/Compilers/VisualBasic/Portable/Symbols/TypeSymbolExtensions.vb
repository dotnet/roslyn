' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Module TypeSymbolExtensions

        <Extension()>
        Public Function IsNullableType(this As TypeSymbol) As Boolean
            Return this.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T
        End Function

        <Extension()>
        Public Function IsNullableOfBoolean(this As TypeSymbol) As Boolean
            Return this.IsNullableType() AndAlso this.GetNullableUnderlyingType().IsBooleanType()
        End Function

        <Extension()>
        Public Function GetNullableUnderlyingType(type As TypeSymbol) As TypeSymbol
            Debug.Assert(type.IsNullableType)
            Return (DirectCast(type, NamedTypeSymbol)).TypeArgumentsNoUseSiteDiagnostics(0)
        End Function

        <Extension()>
        Public Function GetNullableUnderlyingTypeOrSelf(type As TypeSymbol) As TypeSymbol
            Debug.Assert(type IsNot Nothing)

            If type.IsNullableType() Then
                Return type.GetNullableUnderlyingType()
            End If

            Return type
        End Function

        <Extension()>
        Public Function GetEnumUnderlyingType(type As TypeSymbol) As TypeSymbol
            Debug.Assert(type IsNot Nothing)

            Return TryCast(type, NamedTypeSymbol)?.EnumUnderlyingType
        End Function

        <Extension()>
        Public Function GetEnumUnderlyingTypeOrSelf(type As TypeSymbol) As TypeSymbol
            Return If(GetEnumUnderlyingType(type), type)
        End Function

        <Extension()>
        Public Function GetTupleUnderlyingType(type As TypeSymbol) As TypeSymbol
            Debug.Assert(type IsNot Nothing)

            Return TryCast(type, NamedTypeSymbol)?.TupleUnderlyingType
        End Function

        <Extension()>
        Public Function GetTupleUnderlyingTypeOrSelf(type As TypeSymbol) As TypeSymbol
            Return If(GetTupleUnderlyingType(type), type)
        End Function

        <Extension()>
        Public Function TryGetElementTypesIfTupleOrCompatible(type As TypeSymbol, <Out> ByRef elementTypes As ImmutableArray(Of TypeSymbol)) As Boolean
            If type.IsTupleType Then
                elementTypes = DirectCast(type, TupleTypeSymbol).TupleElementTypes
                Return True
            End If

            ' The following codepath should be very uncommon since it would be rare
            ' to see a tuple underlying type not represented as a tuple.
            ' It still might happen since tuple underlying types are creatable via public APIs 
            ' and it is also possible that they would be passed in.

            ' PERF: if allocations here become nuisance, consider caching the results
            '       in the type symbols that can actually be tuple compatible
            Dim cardinality As Integer
            If Not type.IsTupleCompatible(cardinality) Then
                ' source not a tuple or compatible
                elementTypes = Nothing
                Return False
            End If

            Dim elementTypesBuilder = ArrayBuilder(Of TypeSymbol).GetInstance(cardinality)
            TupleTypeSymbol.AddElementTypes(DirectCast(type, NamedTypeSymbol), elementTypesBuilder)

            Debug.Assert(elementTypesBuilder.Count = cardinality)

            elementTypes = elementTypesBuilder.ToImmutableAndFree()
            Return True
        End Function

        <Extension()>
        Public Function GetElementTypesOfTupleOrCompatible(Type As TypeSymbol) As ImmutableArray(Of TypeSymbol)
            If Type.IsTupleType Then
                Return DirectCast(Type, TupleTypeSymbol).TupleElementTypes
            End If

            ' The following codepath should be very uncommon since it would be rare
            ' to see a tuple underlying type not represented as a tuple.
            ' It still might happen since tuple underlying types are creatable via public APIs 
            ' and it is also possible that they would be passed in.

            Debug.Assert(Type.IsTupleCompatible())

            ' PERF: if allocations here become nuisance, consider caching the results
            '       in the type symbols that can actually be tuple compatible
            Dim elementTypesBuilder = ArrayBuilder(Of TypeSymbol).GetInstance()
            TupleTypeSymbol.AddElementTypes(DirectCast(Type, NamedTypeSymbol), elementTypesBuilder)

            Return elementTypesBuilder.ToImmutableAndFree()
        End Function

        <Extension()>
        Friend Function IsEnumType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.TypeKind = TypeKind.Enum
        End Function

        <Extension()>
        Friend Function IsValidEnumUnderlyingType(type As TypeSymbol) As Boolean
            Return type.SpecialType.IsValidEnumUnderlyingType
        End Function

        <Extension()>
        Friend Function IsClassOrInterfaceType(type As TypeSymbol) As Boolean
            Return type.IsClassType OrElse type.IsInterfaceType
        End Function

        <Extension()>
        Friend Function IsInterfaceType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.Kind = SymbolKind.NamedType AndAlso DirectCast(type, NamedTypeSymbol).IsInterface
        End Function

        <Extension()>
        Friend Function IsClassType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.TypeKind = TypeKind.Class
        End Function

        <Extension()>
        Friend Function IsStructureType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.TypeKind = TypeKind.Structure
        End Function

        <Extension()>
        Friend Function IsModuleType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.TypeKind = TypeKind.Module
        End Function

        <Extension()>
        Friend Function IsErrorType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.Kind = SymbolKind.ErrorType
        End Function

        <Extension()>
        Friend Function IsArrayType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.Kind = SymbolKind.ArrayType
        End Function

        <Extension()>
        Friend Function IsCharSZArray(type As TypeSymbol) As Boolean
            If type.IsArrayType() Then
                Dim array = DirectCast(type, ArrayTypeSymbol)

                If array.IsSZArray AndAlso array.ElementType.SpecialType = SpecialType.System_Char Then
                    Return True
                End If
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsDBNullType(type As TypeSymbol) As Boolean
            ' Based on Dev10 codebase, only BindBinaryOperator is going to use
            ' this method. The System.DBNull type isn't guaranteed to be defined in the core 
            ' library. It should be acceptable just to check the name of the type to avoid adding
            ' this type into WellKnownTypes and passing compilation into this method.
            ' Note, the comparison should be case-sensitive, similar to metadata resolution.
            Const [namespace] As String = MetadataHelpers.SystemString
            Const name As String = "DBNull"

            If type.SpecialType = SpecialType.None AndAlso
               type.Kind = SymbolKind.NamedType AndAlso
               String.Equals(type.Name, name, StringComparison.Ordinal) Then

                Dim namedType = DirectCast(type, NamedTypeSymbol)
                If namedType.HasNameQualifier([namespace], StringComparison.Ordinal) Then
                    Return True
                End If
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsMicrosoftVisualBasicCollection(type As TypeSymbol) As Boolean
            ' Based on Dev10 codebase, only ApplyConversion is going to use
            ' this method. 
            ' Note, the comparison should be case-sensitive, similar to metadata resolution.
            Const [namespace] As String = "Microsoft.VisualBasic"
            Const name As String = "Collection"

            If type.SpecialType = SpecialType.None AndAlso
               type.Kind = SymbolKind.NamedType AndAlso
               String.Equals(type.Name, name, StringComparison.Ordinal) Then

                Dim namedType = DirectCast(type, NamedTypeSymbol)
                If namedType.HasNameQualifier([namespace], StringComparison.Ordinal) Then
                    Return True
                End If
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsTypeParameter(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.Kind = SymbolKind.TypeParameter
        End Function

        <Extension()>
        Friend Function IsDelegateType(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Return type.TypeKind = TypeKind.Delegate
        End Function

        <Extension()>
        Friend Function IsSameTypeIgnoringAll(t1 As TypeSymbol, t2 As TypeSymbol) As Boolean
            Return IsSameType(t1, t2, TypeCompareKind.AllIgnoreOptionsForVB)
        End Function

        ''' <summary>
        ''' Compares types ignoring some differences.
        ''' </summary>
        <Extension()>
        Friend Function IsSameType(t1 As TypeSymbol, t2 As TypeSymbol, compareKind As TypeCompareKind) As Boolean
            Return TypeSymbol.Equals(t1, t2, compareKind)
        End Function

        Friend Function HasSameTypeArgumentCustomModifiers(type1 As NamedTypeSymbol, type2 As NamedTypeSymbol) As Boolean
            Dim hasMods1 = type1.HasTypeArgumentsCustomModifiers()
            Dim hasMods2 = type2.HasTypeArgumentsCustomModifiers()

            If Not hasMods1 AndAlso Not hasMods2 Then
                ' Neither has modifiers
                Return True
            End If

            If Not hasMods1 OrElse Not hasMods2 Then
                ' Only one has modifiers
                Return False
            End If

            ' Both have modifiers, let's compare them
            For i As Integer = 0 To type1.Arity - 1
                If Not type1.GetTypeArgumentCustomModifiers(i).AreSameCustomModifiers(type2.GetTypeArgumentCustomModifiers(i)) Then
                    Return False
                End If
            Next

            Return True
        End Function

        <Extension()>
        Friend Function AreSameCustomModifiers([mod] As ImmutableArray(Of CustomModifier), otherMod As ImmutableArray(Of CustomModifier)) As Boolean
            Dim count As Integer = [mod].Length

            If (count <> otherMod.Length) Then
                Return False
            End If

            Return [mod].SequenceEqual(otherMod)
        End Function

        <Extension()>
        Public Function GetSpecialTypeSafe(this As TypeSymbol) As SpecialType
            Return If(this IsNot Nothing, this.SpecialType, SpecialType.None)
        End Function

        <Extension()>
        Public Function IsNumericType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsNumericType()
        End Function

        <Extension()>
        Public Function IsIntegralType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsIntegralType()
        End Function

        <Extension()>
        Public Function IsUnsignedIntegralType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsUnsignedIntegralType()
        End Function

        <Extension()>
        Public Function IsSignedIntegralType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsSignedIntegralType()
        End Function

        <Extension()>
        Public Function IsFloatingType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsFloatingType()
        End Function

        <Extension()>
        Public Function IsSingleType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_Single
        End Function

        <Extension()>
        Public Function IsDoubleType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_Double
        End Function

        <Extension()>
        Public Function IsBooleanType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_Boolean
        End Function

        <Extension()>
        Public Function IsCharType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_Char
        End Function

        <Extension()>
        Public Function IsStringType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_String
        End Function

        <Extension()>
        Public Function IsObjectType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_Object
        End Function

        <Extension()>
        Public Function IsStrictSupertypeOfConcreteDelegate(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsStrictSupertypeOfConcreteDelegate()
        End Function

        <Extension()>
        Public Function IsVoidType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_Void
        End Function

        <Extension()>
        Public Function IsDecimalType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_Decimal
        End Function

        <Extension()>
        Public Function IsDateTimeType(this As TypeSymbol) As Boolean
            Return this.SpecialType = SpecialType.System_DateTime
        End Function

        <Extension()>
        Public Function IsRestrictedType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsRestrictedType()
        End Function

        <Extension()>
        Public Function IsRestrictedArrayType(this As TypeSymbol, <Out> ByRef restrictedType As TypeSymbol) As Boolean
            If this.Kind = SymbolKind.ArrayType Then
                Return this.IsRestrictedTypeOrArrayType(restrictedType)
            End If

            restrictedType = Nothing
            Return False
        End Function

        <Extension()>
        Public Function IsRestrictedTypeOrArrayType(this As TypeSymbol, <Out> ByRef restrictedType As TypeSymbol) As Boolean
            While this.Kind = SymbolKind.ArrayType
                this = DirectCast(this, ArrayTypeSymbol).ElementType
            End While

            If this.IsRestrictedType() Then
                restrictedType = this
                Return True
            End If

            restrictedType = Nothing
            Return False
        End Function

        <Extension()>
        Public Function IsIntrinsicType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsIntrinsicType()
        End Function

        <Extension()>
        Public Function IsIntrinsicValueType(this As TypeSymbol) As Boolean
            Return this.SpecialType.IsIntrinsicValueType()
        End Function

        ''' <summary>
        ''' Return true if nothing can inherit or implement this type.
        ''' </summary>
        <Extension()>
        Public Function IsNotInheritable(this As TypeSymbol) As Boolean
            Select Case this.TypeKind
                Case TypeKind.Array, TypeKind.Delegate, TypeKind.Enum, TypeKind.Structure, TypeKind.Module
                    Return True
                Case TypeKind.Interface, TypeKind.TypeParameter, TypeKind.Unknown
                    Return False
                Case TypeKind.Error, TypeKind.Class, TypeKind.Submission
                    Return DirectCast(this, NamedTypeSymbol).IsNotInheritable
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(this.TypeKind)
            End Select
        End Function

        <Extension()>
        Public Function GetConstantValueTypeDiscriminator(this As TypeSymbol) As ConstantValueTypeDiscriminator
            If this Is Nothing Then
                Return ConstantValueTypeDiscriminator.Nothing
            End If

            this = this.GetEnumUnderlyingTypeOrSelf()

            Select Case this.SpecialType
                Case SpecialType.System_Boolean
                    Return ConstantValueTypeDiscriminator.Boolean
                Case SpecialType.System_Byte
                    Return ConstantValueTypeDiscriminator.Byte
                Case SpecialType.System_SByte
                    Return ConstantValueTypeDiscriminator.SByte
                Case SpecialType.System_Int16
                    Return ConstantValueTypeDiscriminator.Int16
                Case SpecialType.System_UInt16
                    Return ConstantValueTypeDiscriminator.UInt16
                Case SpecialType.System_Int32
                    Return ConstantValueTypeDiscriminator.Int32
                Case SpecialType.System_UInt32
                    Return ConstantValueTypeDiscriminator.UInt32
                Case SpecialType.System_Int64
                    Return ConstantValueTypeDiscriminator.Int64
                Case SpecialType.System_UInt64
                    Return ConstantValueTypeDiscriminator.UInt64
                Case SpecialType.System_Single
                    Return ConstantValueTypeDiscriminator.Single
                Case SpecialType.System_Double
                    Return ConstantValueTypeDiscriminator.Double
                Case SpecialType.System_Decimal
                    Return ConstantValueTypeDiscriminator.Decimal
                Case SpecialType.System_DateTime
                    Return ConstantValueTypeDiscriminator.DateTime
                Case SpecialType.System_Char
                    Return ConstantValueTypeDiscriminator.Char
                Case SpecialType.System_String
                    Return ConstantValueTypeDiscriminator.String
                Case Else
                    If Not this.IsTypeParameter() AndAlso this.IsReferenceType() Then
                        Return ConstantValueTypeDiscriminator.Nothing
                    End If

                    Return ConstantValueTypeDiscriminator.Bad
            End Select
        End Function

        <Extension()>
        Public Function IsValidForConstantValue(this As TypeSymbol, value As ConstantValue) As Boolean
            Dim discriminator = this.GetConstantValueTypeDiscriminator()

            Return discriminator <> ConstantValueTypeDiscriminator.Bad AndAlso discriminator = value.Discriminator OrElse
                (value.Discriminator = ConstantValueTypeDiscriminator.Nothing AndAlso this.IsStringType())
        End Function

        <Extension()>
        Public Function AllowsCompileTimeConversions(this As TypeSymbol) As Boolean
            Return TypeAllowsCompileTimeConversions(this.GetConstantValueTypeDiscriminator())
        End Function

        <Extension()>
        Public Function AllowsCompileTimeOperations(this As TypeSymbol) As Boolean
            Return TypeAllowsCompileTimeOperations(this.GetConstantValueTypeDiscriminator())
        End Function

        <Extension()>
        Public Function CanContainUserDefinedOperators(this As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean

            If this.Kind = SymbolKind.TypeParameter Then
                For Each constraint In DirectCast(this, TypeParameterSymbol).ConstraintTypesWithDefinitionUseSiteDiagnostics(useSiteInfo)
                    If CanContainUserDefinedOperators(constraint, useSiteInfo) Then
                        Return True
                    End If
                Next
            Else
                If this.Kind = SymbolKind.NamedType AndAlso Not DirectCast(this, NamedTypeSymbol).IsInterface Then
                    ' Dev10 #564475 Dig into Nullable types to make sure we don't look for user defined
                    '                          operators between two intrinsic types. For example, Decimal has
                    '                          user defined operators, which should be shadowed by intrinsic conversions
                    '                          even if intrinsic conversion results in compilation error.
                    Dim underlyingType As TypeSymbol = this.GetNullableUnderlyingTypeOrSelf().GetEnumUnderlyingTypeOrSelf()
                    If Not (underlyingType.IsIntrinsicType() OrElse underlyingType.IsObjectType()) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        <Extension()>
        Public Function TypeToIndex(type As TypeSymbol) As Integer?
            Return type.SpecialType.TypeToIndex()
        End Function

        ''' <summary>
        ''' Dig through possibly jagged array type to the ultimate element type
        ''' </summary>
        <Extension()>
        Public Function DigThroughArrayType(possiblyArrayType As TypeSymbol) As TypeSymbol

            Do
                If possiblyArrayType.Kind = SymbolKind.ArrayType Then
                    possiblyArrayType = DirectCast(possiblyArrayType, ArrayTypeSymbol).ElementType
                Else
                    Return possiblyArrayType
                End If
            Loop
        End Function

        ' Determine if "inner" is the same type, or nested within, "outer"
        <Extension()>
        Public Function IsSameOrNestedWithin(inner As NamedTypeSymbol, outer As NamedTypeSymbol) As Boolean
            Do
                If TypeSymbol.Equals(inner, outer, TypeCompareKind.ConsiderEverything) Then
                    Return True
                End If

                inner = inner.ContainingType
            Loop While inner IsNot Nothing

            Return False
        End Function

        <Extension()>
        Public Function ImplementsInterface(subType As TypeSymbol, superInterface As TypeSymbol, comparer As EqualityComparer(Of TypeSymbol), <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            If comparer Is Nothing Then
                comparer = EqualityComparer(Of TypeSymbol).Default
            End If

            For Each [interface] In subType.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteInfo)

                If [interface].IsInterface AndAlso
                   comparer.Equals([interface], superInterface) Then

                    Return True
                End If
            Next

            Return False
        End Function

        <Extension()>
        Public Sub AddUseSiteInfo(
            type As TypeSymbol,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            If useSiteInfo.AccumulatesDiagnostics Then
                useSiteInfo.Add(type.GetUseSiteInfo())
            Else
                Debug.Assert(Not useSiteInfo.AccumulatesDependencies)
            End If
        End Sub

        <Extension()>
        Public Sub AddUseSiteDiagnosticsForBaseDefinitions(
            source As TypeSymbol,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            Dim current As TypeSymbol = source

            Do
                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteInfo)
            Loop While current IsNot Nothing
        End Sub

        <Extension()>
        Public Sub AddConstraintsUseSiteInfo(
            type As TypeParameterSymbol,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        )
            useSiteInfo.Add(type.GetConstraintsUseSiteInfo())
        End Sub

        <Extension()>
        Public Function IsBaseTypeOf(superType As TypeSymbol, subType As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Dim current As TypeSymbol = subType

            While current IsNot Nothing
                If current IsNot subType Then
                    current.OriginalDefinition.AddUseSiteInfo(useSiteInfo)
                End If

                If current.IsSameTypeIgnoringAll(superType) Then
                    Return True
                End If

                current = current.BaseTypeNoUseSiteDiagnostics
            End While

            Return False
        End Function

        <Extension()>
        Public Function IsOrDerivedFrom(derivedType As NamedTypeSymbol, baseType As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Dim current = derivedType
            While current IsNot Nothing
                If current.IsSameTypeIgnoringAll(baseType) Then
                    Return True
                End If

                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteInfo)
            End While

            Return False
        End Function

        <Extension()>
        Public Function IsOrDerivedFrom(derivedType As TypeSymbol, baseType As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Debug.Assert(Not baseType.IsInterfaceType()) ' Not checking interfaces below.

            While (derivedType IsNot Nothing)
                Select Case derivedType.TypeKind
                    Case TypeKind.Array
                        derivedType = derivedType.BaseTypeWithDefinitionUseSiteDiagnostics(useSiteInfo)
                    Case TypeKind.TypeParameter
                        ' Use GetNonInterfaceConstraint rather than GetClassConstraint
                        ' in case the well-known type is a specific structure or enum.
                        derivedType = DirectCast(derivedType, TypeParameterSymbol).GetNonInterfaceConstraint(useSiteInfo)
                    Case Else
                        Return DirectCast(derivedType, NamedTypeSymbol).IsOrDerivedFrom(baseType, useSiteInfo)
                End Select
            End While

            Return False
        End Function

        <Extension()>
        Public Function IsOrDerivedFromWellKnownClass(derivedType As TypeSymbol, wellKnownBaseType As WellKnownType, compilation As VisualBasicCompilation, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Return derivedType.IsOrDerivedFrom(compilation.GetWellKnownType(wellKnownBaseType), useSiteInfo)
        End Function

        ''' <summary>
        ''' Returns true if <paramref name="type" /> is/inherits from/implements IEnumerable(Of U), and U is/inherits from/implements <paramref name="typeArgument" />
        ''' </summary>
        ''' <param name="type">The type to check compatibility for.</param>
        ''' <param name="typeArgument">The type argument for IEnumerable(Of ...)</param>
        ''' <returns><c>True</c> if type can be assigned to a IEnumerable(Of <para>typeArgument</para>); otherwise <c>False</c>.</returns>
        ''' <remarks>This is not a general purpose helper.</remarks>
        <Extension()>
        Public Function IsCompatibleWithGenericIEnumerableOfType(type As TypeSymbol, typeArgument As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            If typeArgument.IsErrorType Then
                Return False
            End If

            ' get the containing assembly of the type argument to get special types later on. In case of an array type
            ' we need to dig into the element type.
            Dim typeWithContainingAssembly = typeArgument
            Do While typeWithContainingAssembly.IsArrayType
                typeWithContainingAssembly = DirectCast(typeWithContainingAssembly, ArrayTypeSymbol).ElementType
            Loop

            If typeWithContainingAssembly.IsErrorType Then
                Return False
            End If

            ' to figure out if a type is derived from IEnumerable(Of XContainer) it's not enough to check if the conversion from the type to
            ' IEnumerable(Of XContainer) because IEnumerable may come from framework 3.5 or below and does not support variance which would classify
            ' a conversion from IEnumerable(Of XElement) to IEnumerable(Of XContainer) as "NarrowingReference".
            ' Therefore we are doing the variance check manually (like Dev11, see TypeHelpers.cpp, IsCompatibleWithGenericEnumerableType)
            Dim genericIEnumerable = typeWithContainingAssembly.ContainingAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            Dim matchingInterfaces As New HashSet(Of NamedTypeSymbol)()

            ' first find all implementations of IEnumerable(Of T)
            If Binder.IsOrInheritsFromOrImplementsInterface(type, genericIEnumerable, useSiteInfo, matchingInterfaces) Then
                If matchingInterfaces.Count > 0 Then

                    ' now check if the type argument is compatible with the given type
                    For Each matchingInterface In matchingInterfaces
                        Call Global.System.Diagnostics.Debug.Assert(matchingInterface.Arity = 1)
                        Dim matchingTypeArgument = matchingInterface.TypeArgumentWithDefinitionUseSiteDiagnostics(0, useSiteInfo)

                        If matchingTypeArgument.IsErrorType Then
                            Return False
                        End If

                        Dim conversion = Global.Microsoft.CodeAnalysis.VisualBasic.Conversions.ClassifyDirectCastConversion(matchingTypeArgument, typeArgument, useSiteInfo)
                        If Global.Microsoft.CodeAnalysis.VisualBasic.Conversions.IsWideningConversion(conversion) Then
                            Return True
                        End If
                    Next
                End If
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsOrImplementsIEnumerableOfXElement(type As TypeSymbol, compilation As VisualBasicCompilation, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Dim xmlType = compilation.GetWellKnownType(WellKnownType.System_Xml_Linq_XElement)
            Return type.IsCompatibleWithGenericIEnumerableOfType(xmlType, useSiteInfo)
        End Function

        <Extension()>
        Public Function IsBaseTypeOrInterfaceOf(superType As TypeSymbol, subType As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            If superType.IsInterfaceType() Then
                Return subType.ImplementsInterface(superType, EqualsIgnoringComparer.InstanceCLRSignatureCompare, useSiteInfo)
            Else
                Return superType.IsBaseTypeOf(subType, useSiteInfo)
            End If
        End Function

        ''' <summary>
        ''' Determines whether the given type is valid for a const field.
        ''' VB Spec 9.5: The type of a constant may only be a primitive type or Object
        ''' </summary>
        ''' <param name="fieldType">The type of the field.</param><returns>
        '''   <c>true</c> if type is valid for a const field; otherwise, <c>false</c>.
        ''' </returns>
        <Extension()>
        Friend Function IsValidTypeForConstField(fieldType As TypeSymbol) As Boolean
            Return fieldType.IsIntrinsicType() OrElse
                    fieldType.SpecialType = SpecialType.System_Object OrElse
                    fieldType.TypeKind = TypeKind.Enum
        End Function

        <Extension()>
        Friend Sub CollectReferencedTypeParameters(this As TypeSymbol, typeParameters As HashSet(Of TypeParameterSymbol))
            VisitType(this, s_addIfTypeParameterFunc, typeParameters)
        End Sub

        Private ReadOnly s_addIfTypeParameterFunc As Func(Of TypeSymbol, HashSet(Of TypeParameterSymbol), Boolean) = AddressOf AddIfTypeParameter

        Private Function AddIfTypeParameter(type As TypeSymbol, typeParameters As HashSet(Of TypeParameterSymbol)) As Boolean
            If type.TypeKind = TypeKind.TypeParameter Then
                typeParameters.Add(DirectCast(type, TypeParameterSymbol))
            End If
            Return False
        End Function

        <Extension()>
        Friend Function ReferencesTypeParameterNotInTheSet(this As TypeSymbol, typeParameters As HashSet(Of TypeParameterSymbol)) As Boolean
            Dim typeParameter = VisitType(this, s_isTypeParameterNotInSetFunc, typeParameters)
            Return typeParameter IsNot Nothing
        End Function

        Private ReadOnly s_isTypeParameterNotInSetFunc As Func(Of TypeSymbol, HashSet(Of TypeParameterSymbol), Boolean) = AddressOf IsTypeParameterNotInSet

        Private Function IsTypeParameterNotInSet(type As TypeSymbol, typeParameters As HashSet(Of TypeParameterSymbol)) As Boolean
            Return (type.TypeKind = TypeKind.TypeParameter) AndAlso
                Not typeParameters.Contains(DirectCast(type, TypeParameterSymbol))
        End Function

        <Extension()>
        Friend Function ReferencesMethodsTypeParameter(this As TypeSymbol, method As MethodSymbol) As Boolean
            Dim typeParameter = VisitType(this, s_isMethodTypeParameterFunc, method)
            Return typeParameter IsNot Nothing
        End Function

        Private ReadOnly s_isMethodTypeParameterFunc As Func(Of TypeSymbol, MethodSymbol, Boolean) = AddressOf IsMethodTypeParameter

        Private Function IsMethodTypeParameter(type As TypeSymbol, method As MethodSymbol) As Boolean
            Return (type.TypeKind = TypeKind.TypeParameter) AndAlso
                type.ContainingSymbol.Equals(method)
        End Function

        <Extension()>
        Public Function IsUnboundGenericType(this As TypeSymbol) As Boolean
            Dim namedType = TryCast(this, NamedTypeSymbol)
            Return namedType IsNot Nothing AndAlso namedType.IsUnboundGenericType
        End Function

        <Extension()>
        Friend Function IsOrRefersToTypeParameter(this As TypeSymbol) As Boolean
            Dim typeParameter = VisitType(this, s_isTypeParameterFunc, Nothing)
            Return typeParameter IsNot Nothing
        End Function

        Private ReadOnly s_isTypeParameterFunc As Func(Of TypeSymbol, Object, Boolean) = Function(type, arg) (type.TypeKind = TypeKind.TypeParameter)

        ''' <summary>
        ''' Return true if the type contains any tuples.
        ''' </summary>
        <Extension()>
        Friend Function ContainsTuple(type As TypeSymbol) As Boolean
            Return type.VisitType(s_isTupleTypeFunc, Nothing) IsNot Nothing
        End Function

        Private ReadOnly s_isTupleTypeFunc As Func(Of TypeSymbol, Object, Boolean) = Function(type, arg) type.IsTupleType

        ''' <summary>
        ''' Return true if the type contains any tuples with element names.
        ''' </summary>
        <Extension()>
        Friend Function ContainsTupleNames(type As TypeSymbol) As Boolean
            Return type.VisitType(s_hasTupleNamesFunc, Nothing) IsNot Nothing
        End Function

        Private ReadOnly s_hasTupleNamesFunc As Func(Of TypeSymbol, Object, Boolean) = Function(type, arg) Not type.TupleElementNames.IsDefault

        ''' <summary>
        ''' Visit the given type and, in the case of compound types, visit all "sub type"
        ''' (such as A in A(), or { A(Of T), T, U } in A(Of T).B(Of U)) invoking 'predicate'
        ''' with the type and 'arg' at each sub type. If the predicate returns true for any type,
        ''' traversal stops and that type is returned from this method. Otherwise if traversal
        ''' completes without the predicate returning true for any type, this method returns null.
        ''' </summary>
        <Extension()>
        Friend Function VisitType(Of T)(type As TypeSymbol, predicate As Func(Of TypeSymbol, T, Boolean), arg As T) As TypeSymbol
            ' In order to handle extremely "deep" types like "Integer()()()()()()()()()...()"
            ' we implement manual tail recursion rather than doing the natural recursion.

            Dim current As TypeSymbol = type

            Do
                ' Visit containing types from outer-most to inner-most.
                Select Case current.TypeKind

                    Case TypeKind.Class,
                         TypeKind.Struct,
                         TypeKind.Interface,
                         TypeKind.Enum,
                         TypeKind.Delegate

                        Dim containingType = current.ContainingType
                        If containingType IsNot Nothing Then
                            Dim result = containingType.VisitType(predicate, arg)

                            If result IsNot Nothing Then
                                Return result
                            End If
                        End If

                    Case TypeKind.Submission
                        Debug.Assert(current.ContainingType Is Nothing)
                End Select

                If predicate(current, arg) Then
                    Return current
                End If

                Select Case current.TypeKind

                    Case TypeKind.Dynamic,
                         TypeKind.TypeParameter,
                         TypeKind.Submission,
                         TypeKind.Enum,
                         TypeKind.Module

                        Return Nothing

                    Case TypeKind.Class,
                         TypeKind.Struct,
                         TypeKind.Interface,
                         TypeKind.Delegate,
                         TypeKind.Error

                        If current.IsTupleType Then
                            ' turn tuple type elements into parameters
                            current = current.TupleUnderlyingType
                        End If

                        For Each nestedType In DirectCast(current, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics
                            Dim result = nestedType.VisitType(predicate, arg)
                            If result IsNot Nothing Then
                                Return result
                            End If
                        Next

                        Return Nothing

                    Case TypeKind.Array
                        current = DirectCast(current, ArrayTypeSymbol).ElementType
                        Continue Do

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(current.TypeKind)
                End Select
            Loop
        End Function

        ''' <summary>
        ''' Determines if the type is a valid type for a custom attribute argument
        ''' </summary>
        ''' <param name="type"></param>
        ''' <param name="compilation"></param>
        ''' <returns></returns>
        ''' <remarks>
        '''  The only valid types are 
        ''' 1. primitive types except date and decimal, 
        ''' 2. object, system.type, public enumerated types
        ''' 3. one dimensional arrays of (1) and (2) above
        ''' </remarks>
        <Extension()>
        Public Function IsValidTypeForAttributeArgument(type As TypeSymbol, compilation As VisualBasicCompilation) As Boolean
            If type Is Nothing Then
                Return False
            End If

            If type.IsArrayType Then
                Dim arrayType = DirectCast(type, ArrayTypeSymbol)
                If Not arrayType.IsSZArray Then
                    Return False
                End If
                type = arrayType.ElementType
            End If

            Return type.GetEnumUnderlyingTypeOrSelf.SpecialType.IsValidTypeForAttributeArgument() OrElse
                TypeSymbol.Equals(type, compilation.GetWellKnownType(WellKnownType.System_Type), TypeCompareKind.ConsiderEverything) ' don't call the version with diagnostics
        End Function

        <Extension()>
        Public Function IsValidTypeForSwitchTable(type As TypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)

            type = type.GetNullableUnderlyingTypeOrSelf()
            type = type.GetEnumUnderlyingTypeOrSelf()

            Return type.SpecialType.IsValidTypeForSwitchTable()
        End Function

        <Extension()>
        Public Function IsIntrinsicOrEnumType(type As TypeSymbol) As Boolean
            Return type IsNot Nothing AndAlso (type.GetEnumUnderlyingTypeOrSelf().IsIntrinsicType())
        End Function

        ''' <summary>
        ''' Add this instance to the set of checked types. Returns True
        ''' if this type was added, False if the type was already in the set.
        ''' </summary>
        <Extension()>
        Public Function MarkCheckedIfNecessary(type As TypeSymbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As Boolean
            If checkedTypes Is Nothing Then
                checkedTypes = New HashSet(Of TypeSymbol)()
            End If

            Return checkedTypes.Add(type)
        End Function

        ''' <summary> Checks for validity of type arguments passed to Construct(...) method </summary>
        <Extension()>
        Friend Sub CheckTypeArguments(typeArguments As ImmutableArray(Of TypeSymbol), expectedCount As Integer)
            If typeArguments.IsDefault Then
                Throw New Global.System.ArgumentNullException(NameOf(typeArguments))
            End If

            For Each typeArg In typeArguments
                If typeArg Is Nothing Then
                    Throw New ArgumentException(VBResources.TypeArgumentCannotBeNothing, NameOf(typeArguments))
                End If
            Next

            If typeArguments.Length = 0 OrElse typeArguments.Length <> expectedCount Then
                Throw New ArgumentException(VBResources.WrongNumberOfTypeArguments, NameOf(typeArguments))
            End If
        End Sub

        ''' <summary>
        ''' Returns Nothing for identity substitution.
        ''' </summary>
        <Extension()>
        Friend Function TransformToCanonicalFormFor(
            typeArguments As ImmutableArray(Of TypeSymbol),
            genericType As SubstitutedNamedType.SpecializedGenericType
        ) As ImmutableArray(Of TypeSymbol)
            Return TransformToCanonicalFormFor(typeArguments, genericType, genericType.TypeParameters)
        End Function

        ''' <summary>
        ''' Returns Nothing for identity substitution.
        ''' </summary>
        <Extension()>
        Friend Function TransformToCanonicalFormFor(
            typeArguments As ImmutableArray(Of TypeSymbol),
            genericMethod As SubstitutedMethodSymbol.SpecializedGenericMethod
        ) As ImmutableArray(Of TypeSymbol)
            Return TransformToCanonicalFormFor(typeArguments, genericMethod, genericMethod.TypeParameters)
        End Function

        Private Function TransformToCanonicalFormFor(
            typeArguments As ImmutableArray(Of TypeSymbol),
            specializedGenericTypeOrMethod As Symbol,
            specializedTypeParameters As ImmutableArray(Of TypeParameterSymbol)
        ) As ImmutableArray(Of TypeSymbol)

            ' Check for type arguments equal to type parameters of this type,
            ' but not contained by it ("cross-pollination"). Replace them with 
            ' this types' type parameters.
            Dim newTypeArguments As TypeSymbol() = Nothing
            Dim i As Integer = 0
            Dim typeArgument As TypeSymbol

            Do
                typeArgument = typeArguments(i)

                If typeArgument.IsTypeParameter() AndAlso Not typeArgument.IsDefinition Then
                    Dim container As Symbol = typeArgument.ContainingSymbol

                    If container IsNot specializedGenericTypeOrMethod AndAlso container.Equals(specializedGenericTypeOrMethod) Then
                        newTypeArguments = typeArguments.ToArray()
                        Exit Do
                    End If
                End If

                i += 1
            Loop While i < typeArguments.Length

            If newTypeArguments IsNot Nothing Then
                newTypeArguments(i) = specializedTypeParameters(DirectCast(typeArgument, TypeParameterSymbol).Ordinal)
                Debug.Assert(newTypeArguments(i).Equals(typeArgument))

                i += 1
                While i < typeArguments.Length
                    typeArgument = typeArguments(i)

                    If typeArgument.IsTypeParameter() AndAlso Not typeArgument.IsDefinition Then
                        Dim container As Symbol = typeArgument.ContainingSymbol

                        If container IsNot specializedGenericTypeOrMethod AndAlso container.Equals(specializedGenericTypeOrMethod) Then
                            newTypeArguments(i) = specializedTypeParameters(DirectCast(typeArgument, TypeParameterSymbol).Ordinal)
                            Debug.Assert(newTypeArguments(i).Equals(typeArgument))
                        End If
                    End If

                    i += 1
                End While

                typeArguments = newTypeArguments.AsImmutableOrNull()
            End If

            ' Check for identity substitution.
            For i = 0 To specializedTypeParameters.Length - 1
                If specializedTypeParameters(i) IsNot typeArguments(i) Then
                    Return typeArguments ' Not an identity substitution
                End If
            Next

            ' identity substitution
            Return Nothing
        End Function

        ''' <summary>
        ''' Is this type System.Linq.Expressions.Expression(Of T) for some delegate type T. If so, return the type
        ''' argument, else return nothing.
        ''' The passed-in compilation is used to find the well-known-type System.Linq.Expressions.Expression(Of T).
        ''' </summary>
        <Extension>
        Public Function ExpressionTargetDelegate(type As TypeSymbol, compilation As VisualBasicCompilation) As NamedTypeSymbol
            If type.TypeKind = TypeKind.Class Then
                Dim namedType = DirectCast(type, NamedTypeSymbol)

                ' Note that if the compilation doesn't have the Expression(Of T) well-known type, then the below test just fails correctly.
                If namedType.Arity = 1 AndAlso TypeSymbol.Equals(namedType.OriginalDefinition, compilation.GetWellKnownType(WellKnownType.System_Linq_Expressions_Expression_T), TypeCompareKind.ConsiderEverything) Then
                    Dim typeArgument = namedType.TypeArgumentsNoUseSiteDiagnostics(0)
                    If typeArgument.TypeKind = TypeKind.Delegate Then
                        Return DirectCast(typeArgument, NamedTypeSymbol)
                    End If
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' If the passed in type is a delegate type D, return D.
        ''' If the passed in type is a System.Linq.Expressions.Expression(Of D) for a delegate type D, return D.
        ''' Else return Nothing
        ''' </summary>
        <Extension>
        Public Function DelegateOrExpressionDelegate(type As TypeSymbol, binder As Binder) As NamedTypeSymbol
            If type.TypeKind = TypeKind.Delegate Then
                Return DirectCast(type, NamedTypeSymbol)
            Else
                Return type.ExpressionTargetDelegate(binder.Compilation)
            End If
        End Function

        ''' <summary>
        ''' If the passed in type is a delegate type D, return D and set wasExpression to False
        ''' If the passed in type is a System.Linq.Expressions.Expression(Of D) for a delegate type D, return D and set wasExpression to True
        ''' Else return Nothing and set wasExpression to False
        ''' </summary>
        <Extension>
        Public Function DelegateOrExpressionDelegate(type As TypeSymbol, binder As Binder, ByRef wasExpression As Boolean) As NamedTypeSymbol
            If type.TypeKind = TypeKind.Delegate Then
                wasExpression = False
                Return DirectCast(type, NamedTypeSymbol)
            Else
                Dim expressionArg = ExpressionTargetDelegate(type, binder.Compilation)
                wasExpression = (expressionArg IsNot Nothing)
                Return expressionArg
            End If
        End Function

        ''' <summary>
        ''' If the passed in type is a System.Linq.Expressions.Expression(Of D) for a delegate type D, return True
        ''' </summary>
        <Extension>
        Public Function IsExpressionTree(type As TypeSymbol, binder As Binder) As Boolean
            Return type.ExpressionTargetDelegate(binder.Compilation) IsNot Nothing
        End Function

        <Extension>
        Public Function IsExtensibleInterfaceNoUseSiteDiagnostics(type As TypeSymbol) As Boolean
            Return type.IsInterfaceType() AndAlso DirectCast(type, NamedTypeSymbol).IsExtensibleInterfaceNoUseSiteDiagnostics
        End Function

        <Extension>
        Public Function GetNativeCompilerVType(type As TypeSymbol) As String
            Return If(type.SpecialType.GetNativeCompilerVType(),
                      If(type.IsTypeParameter, "t_generic",
                          If(type.IsArrayType, "t_array",
                             If(type.IsValueType, "t_struct", "t_ref"))))

        End Function

        <Extension>
        Public Function IsVerifierReference(type As TypeSymbol) As Boolean
            'Type parameters are not considered references.
            If type.TypeKind = TypeKind.TypeParameter Then
                Return False
            End If
            Return type.IsReferenceType
        End Function

        <Extension>
        Public Function IsVerifierValue(type As TypeSymbol) As Boolean
            If type.TypeKind = TypeKind.TypeParameter Then
                Return False
            End If
            Return type.IsValueType
        End Function

        <Extension>
        Public Function IsPrimitiveType(t As TypeSymbol) As Boolean
            Return t.SpecialType.IsPrimitiveType
        End Function

        <Extension>
        Public Function IsTopLevelType(type As NamedTypeSymbol) As Boolean
            Return type.ContainingType Is Nothing
        End Function

        ''' <summary>
        ''' Return all of the type parameters in this type and enclosing types,
        ''' from outer-most to inner-most type.
        ''' </summary>
        <Extension>
        Public Function GetAllTypeParameters(type As NamedTypeSymbol) As ImmutableArray(Of TypeParameterSymbol)
            ' Avoid allocating a builder in the common case.
            If type.ContainingType Is Nothing Then
                Return type.TypeParameters
            End If

            Dim builder = ArrayBuilder(Of TypeParameterSymbol).GetInstance()
            type.GetAllTypeParameters(builder)
            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Return all of the type parameters in this type and enclosing types,
        ''' from outer-most to inner-most type.
        ''' </summary>
        <Extension>
        Public Sub GetAllTypeParameters(type As NamedTypeSymbol, builder As ArrayBuilder(Of TypeParameterSymbol))
            Dim containingType = type.ContainingType
            If containingType IsNot Nothing Then
                containingType.GetAllTypeParameters(builder)
            End If

            builder.AddRange(type.TypeParameters)
        End Sub

        ''' <summary>
        ''' Return all of the type arguments in this type and enclosing types,
        ''' from outer-most to inner-most type.
        ''' </summary>
        <Extension>
        Public Function GetAllTypeArguments(type As NamedTypeSymbol) As ImmutableArray(Of TypeSymbol)
            Dim typeArguments = type.TypeArgumentsNoUseSiteDiagnostics

            While True
                type = type.ContainingType
                If type Is Nothing Then
                    Exit While
                End If
                typeArguments = type.TypeArgumentsNoUseSiteDiagnostics.Concat(typeArguments)
            End While

            Return typeArguments
        End Function

        ''' <summary>
        ''' Return all of the type arguments and their modifiers in this type and enclosing types,
        ''' from outer-most to inner-most type.
        ''' </summary>
        <Extension>
        Public Function GetAllTypeArgumentsWithModifiers(type As NamedTypeSymbol) As ImmutableArray(Of TypeWithModifiers)
            Dim builder = ArrayBuilder(Of TypeWithModifiers).GetInstance()

            Do
                Dim typeArguments = type.TypeArgumentsNoUseSiteDiagnostics

                For i As Integer = typeArguments.Length - 1 To 0 Step -1
                    builder.Add(New TypeWithModifiers(typeArguments(i), type.GetTypeArgumentCustomModifiers(i)))
                Next

                type = type.ContainingType
            Loop While type IsNot Nothing

            builder.ReverseContents()
            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Return true if the fully qualified name of the type's containing symbol
        ''' matches the given name. This method avoids string concatenations
        ''' in the common case where the type is a top-level type.
        ''' </summary>
        <Extension>
        Friend Function HasNameQualifier(type As NamedTypeSymbol, qualifiedName As String, comparison As StringComparison) As Boolean
            Dim container = type.ContainingSymbol
            If container.Kind <> SymbolKind.Namespace Then
                ' Nested type. For simplicity, compare qualified name to SymbolDisplay result.
                Return String.Equals(container.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat), qualifiedName, comparison)
            End If

            Dim emittedName = type.GetEmittedNamespaceName()
            If emittedName IsNot Nothing Then
                Return String.Equals(qualifiedName, emittedName, comparison)
            End If

            Dim [namespace] = DirectCast(container, NamespaceSymbol)
            If [namespace].IsGlobalNamespace Then
                Return qualifiedName.Length = 0
            End If

            Return HasNamespaceName([namespace], qualifiedName, comparison, length:=qualifiedName.Length)
        End Function

        Private Function HasNamespaceName([namespace] As NamespaceSymbol, namespaceName As String, comparison As StringComparison, length As Integer) As Boolean
            If length = 0 Then
                Return False
            End If

            Dim container = [namespace].ContainingNamespace
            Dim separator = namespaceName.LastIndexOf("."c, length - 1, length)
            Dim offset = 0
            If separator >= 0 Then
                If container.IsGlobalNamespace Then
                    Return False
                End If

                If Not HasNamespaceName(container, namespaceName, comparison, length:=separator) Then
                    Return False
                End If

                Dim n = separator + 1
                offset = n
                length -= n

            ElseIf Not container.IsGlobalNamespace Then
                Return False
            End If

            Dim name = [namespace].Name
            Return (name.Length = length) AndAlso (String.Compare(name, 0, namespaceName, offset, length, comparison) = 0)
        End Function

        <Extension>
        Friend Function GetTypeRefWithAttributes(type As TypeSymbol, declaringCompilation As VisualBasicCompilation, typeRef As Cci.ITypeReference) As Cci.TypeReferenceWithAttributes
            If type.ContainsTupleNames() Then
                Dim attr = declaringCompilation.SynthesizeTupleNamesAttribute(type)
                If attr IsNot Nothing Then
                    Return New Cci.TypeReferenceWithAttributes(
                        typeRef,
                        ImmutableArray.Create(Of Cci.ICustomAttribute)(attr))
                End If
            End If

            Return New Cci.TypeReferenceWithAttributes(typeRef)
        End Function

        <Extension>
        Friend Function IsWellKnownTypeIsExternalInit(typeSymbol As TypeSymbol) As Boolean
            Return typeSymbol.IsWellKnownCompilerServicesTopLevelType("IsExternalInit")
        End Function

        ' Keep in sync with C# equivalent.
        <Extension>
        Friend Function IsWellKnownTypeLock(typeSymbol As TypeSymbol) As Boolean
            Dim namedTypeSymbol = TryCast(typeSymbol, NamedTypeSymbol)
            Return namedTypeSymbol IsNot Nothing AndAlso
                namedTypeSymbol.Name = WellKnownMemberNames.LockTypeName AndAlso
                namedTypeSymbol.Arity = 0 AndAlso
                namedTypeSymbol.ContainingType Is Nothing AndAlso
                namedTypeSymbol.IsContainedInNamespace(NameOf(System), NameOf(System.Threading))
        End Function

        <Extension>
        Private Function IsWellKnownCompilerServicesTopLevelType(typeSymbol As TypeSymbol, name As String) As Boolean
            If Not String.Equals(typeSymbol.Name, name) Then
                Return False
            End If

            Return IsCompilerServicesTopLevelType(typeSymbol)
        End Function

        <Extension>
        Friend Function IsCompilerServicesTopLevelType(typeSymbol As TypeSymbol) As Boolean
            Return typeSymbol.ContainingType Is Nothing AndAlso IsContainedInNamespace(typeSymbol, "System", "Runtime", "CompilerServices")
        End Function

        <Extension>
        Private Function IsContainedInNamespace(typeSymbol As TypeSymbol, outerNS As String, midNS As String, Optional innerNS As String = Nothing) As Boolean
            Dim midNamespace As NamespaceSymbol

            If innerNS IsNot Nothing Then
                Dim innerNamespace = typeSymbol.ContainingNamespace
                If Not String.Equals(innerNamespace?.Name, innerNS) Then
                    Return False
                End If
                midNamespace = innerNamespace.ContainingNamespace
            Else
                midNamespace = typeSymbol.ContainingNamespace
            End If

            If Not String.Equals(midNamespace?.Name, midNS) Then
                Return False
            End If

            Dim outerNamespace = midNamespace.ContainingNamespace
            If Not String.Equals(outerNamespace?.Name, outerNS) Then
                Return False
            End If

            Dim globalNamespace = outerNamespace.ContainingNamespace
            Return globalNamespace IsNot Nothing AndAlso globalNamespace.IsGlobalNamespace
        End Function
    End Module

End Namespace

