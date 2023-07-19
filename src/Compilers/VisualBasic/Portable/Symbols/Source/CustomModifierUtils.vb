' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class CustomModifierUtils

        ''' <summary>
        ''' sourceMethod has the custom modifiers
        ''' </summary>
        Friend Shared Sub CopyMethodCustomModifiers(
                                sourceMethod As MethodSymbol,
                                destinationTypeParameters As ImmutableArray(Of TypeSymbol),
                                <[In], Out> ByRef destinationReturnType As TypeSymbol,
                                <[In], Out> ByRef parameters As ImmutableArray(Of ParameterSymbol))

            Debug.Assert(sourceMethod IsNot Nothing)

            ' For the most part, we will copy custom modifiers by copying types.
            ' The only time when this fails Is when the type refers to a type parameter
            ' owned by the overridden method.  We need to replace all such references
            ' with (equivalent) type parameters owned by this method.  We know that
            ' we can perform this mapping positionally, because the method signatures
            ' have already been compared.
            Dim constructedMethod As MethodSymbol = sourceMethod.ConstructIfGeneric(destinationTypeParameters)

            parameters = CustomModifierUtils.CopyParameterCustomModifiers(constructedMethod.Parameters, parameters)

            Dim returnTypeWithCustomModifiers = constructedMethod.ReturnType

            ' We do an extra check before copying the return type to handle the case where the overriding
            ' method (incorrectly) has a different return type than the overridden method.  In such cases,
            ' we want to retain the original (incorrect) return type to avoid hiding the return type
            ' given in source.
            If destinationReturnType.IsSameType(returnTypeWithCustomModifiers, TypeCompareKind.AllIgnoreOptionsForVB) Then
                destinationReturnType = CopyTypeCustomModifiers(returnTypeWithCustomModifiers, destinationReturnType)
            End If

        End Sub

        ''' <summary>
        ''' sourceType has the custom modifiers
        ''' </summary>
        Friend Shared Function CopyTypeCustomModifiers(sourceType As TypeSymbol, destinationType As TypeSymbol) As TypeSymbol
            Dim resultType As TypeSymbol

            Debug.Assert(sourceType.IsSameType(destinationType, TypeCompareKind.AllIgnoreOptionsForVB))

            If destinationType.ContainsTuple() AndAlso Not sourceType.IsSameType(destinationType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) Then
                Dim names As ImmutableArray(Of String) = VisualBasicCompilation.TupleNamesEncoder.Encode(destinationType)
                resultType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(sourceType, names)
            Else
                resultType = sourceType
            End If

            Debug.Assert(resultType.IsSameType(sourceType, TypeCompareKind.IgnoreTupleNames)) ' Same custom modifiers as source type
            Debug.Assert(resultType.IsSameType(destinationType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds)) ' Same tuple names as destination type
            Return resultType
        End Function

        Public Shared Function CopyParameterCustomModifiers(
            overriddenMemberParameters As ImmutableArray(Of ParameterSymbol),
            parameters As ImmutableArray(Of ParameterSymbol)
        ) As ImmutableArray(Of ParameterSymbol)
            Debug.Assert(Not parameters.IsDefault)
            Debug.Assert(overriddenMemberParameters.Length = parameters.Length)

            ' Nearly all of the time, there will be no custom modifiers to copy, so don't
            ' allocate the builder until we know that we need it.
            Dim builder As ArrayBuilder(Of ParameterSymbol) = Nothing

            For i As Integer = 0 To parameters.Length - 1
                Dim thisParam As ParameterSymbol = parameters(i)

                If CopyParameterCustomModifiers(overriddenMemberParameters(i), thisParam) Then
                    If builder Is Nothing Then
                        builder = ArrayBuilder(Of ParameterSymbol).GetInstance()
                        builder.AddRange(parameters, i) ' add up To, but Not including, the current parameter
                    End If

                    builder.Add(thisParam)
                ElseIf builder IsNot Nothing Then
                    builder.Add(thisParam)
                End If
            Next

            Return If(builder Is Nothing, parameters, builder.ToImmutableAndFree())
        End Function

        ''' <summary>
        ''' Returns True if <paramref name="thisParam"/> was modified.
        ''' </summary>
        ''' <returns></returns>
        Public Shared Function CopyParameterCustomModifiers(
            overriddenParam As ParameterSymbol,
            <[In], Out> ByRef thisParam As ParameterSymbol
        ) As Boolean
            Debug.Assert(TypeOf thisParam Is SourceParameterSymbolBase)
            Debug.Assert(thisParam.Type.IsSameType(overriddenParam.Type, TypeCompareKind.AllIgnoreOptionsForVB))

            If Not overriddenParam.CustomModifiers.SequenceEqual(thisParam.CustomModifiers) OrElse
               (overriddenParam.IsByRef AndAlso thisParam.IsByRef AndAlso Not overriddenParam.RefCustomModifiers.SequenceEqual(thisParam.RefCustomModifiers)) OrElse
               Not thisParam.Type.IsSameType(overriddenParam.Type, TypeCompareKind.AllIgnoreOptionsForVB And Not TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) Then

                Dim thisParamType As TypeSymbol = thisParam.Type
                Dim overriddenParamType As TypeSymbol = overriddenParam.Type

                If thisParamType.ContainsTuple() AndAlso Not overriddenParam.Type.IsSameType(thisParamType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) Then
                    Dim names As ImmutableArray(Of String) = VisualBasicCompilation.TupleNamesEncoder.Encode(thisParamType)
                    overriddenParamType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(overriddenParamType, names)
                End If

                thisParam = DirectCast(thisParam, SourceParameterSymbolBase).WithTypeAndCustomModifiers(
                    overriddenParamType,
                    overriddenParam.CustomModifiers,
                    If(thisParam.IsByRef, overriddenParam.RefCustomModifiers, ImmutableArray(Of CustomModifier).Empty))

                Return True
            End If

            Return False
        End Function

        Friend Shared Function HasIsExternalInitModifier(modifiers As ImmutableArray(Of CustomModifier)) As Boolean
            Return modifiers.Any(Function(modifier) Not modifier.IsOptional AndAlso
                   DirectCast(modifier, VisualBasicCustomModifier).ModifierSymbol.IsWellKnownTypeIsExternalInit())
        End Function
    End Class
End Namespace

