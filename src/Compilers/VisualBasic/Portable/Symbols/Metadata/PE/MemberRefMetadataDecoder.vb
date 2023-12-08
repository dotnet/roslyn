' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Reflection.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' This subclass of MetadataDecoder is specifically for finding
    ''' method symbols corresponding to method MemberRefs.  The parent 
    ''' implementation is unsuitable because it requires a PEMethodSymbol
    ''' for context when decoding method type parameters and no such
    ''' context is available because it is precisely what we are trying
    ''' to find.  Since we know in advance that there will be no context
    ''' and that signatures decoded with this class will only be used
    ''' for comparison (when searching through the methods of a known
    ''' TypeSymbol), we can return indexed type parameters instead.
    ''' </summary>
    Friend NotInheritable Class MemberRefMetadataDecoder
        Inherits MetadataDecoder

        Private ReadOnly _containingType As TypeSymbol

        Public Sub New(moduleSymbol As PEModuleSymbol, containingType As TypeSymbol)
            MyBase.New(moduleSymbol, TryCast(containingType, PENamedTypeSymbol))
            Debug.Assert(containingType IsNot Nothing)
            Me._containingType = containingType
        End Sub

        ''' <summary>
        ''' We know that we'll never have a method context because that's what we're
        ''' trying to find.  Instead, just return an indexed type parameter that will
        ''' make comparison easier.
        ''' </summary>
        ''' <param name="position"></param>
        ''' <returns></returns>
        Protected Overrides Function GetGenericMethodTypeParamSymbol(position As Integer) As TypeSymbol
            Return IndexedTypeParameterSymbol.GetTypeParameter(position)
        End Function

        ''' <summary>
        ''' This override can handle non-PE types.
        ''' </summary>
        Protected Overrides Function GetGenericTypeParamSymbol(position As Integer) As TypeSymbol
            Dim peType As PENamedTypeSymbol = TryCast(Me._containingType, PENamedTypeSymbol)
            If peType IsNot Nothing Then
                Return MyBase.GetGenericTypeParamSymbol(position)
            End If

            Dim namedType As NamedTypeSymbol = TryCast(Me._containingType, NamedTypeSymbol)
            If namedType IsNot Nothing Then
                Dim cumulativeArity As Integer
                Dim typeParameter As TypeParameterSymbol = Nothing

                GetGenericTypeParameterSymbol(position, namedType, cumulativeArity, typeParameter)
                If typeParameter IsNot Nothing Then
                    Return typeParameter
                Else
                    Debug.Assert(cumulativeArity <= position)
                    Return New UnsupportedMetadataTypeSymbol(VBResources.PositionOfTypeParameterTooLarge)
                End If
            End If

            Return New UnsupportedMetadataTypeSymbol(VBResources.AssociatedTypeDoesNotHaveTypeParameters)
        End Function

        Private Shared Sub GetGenericTypeParameterSymbol(position As Integer, namedType As NamedTypeSymbol, ByRef cumulativeArity As Integer, ByRef typeArgument As TypeParameterSymbol)
            cumulativeArity = namedType.Arity
            typeArgument = Nothing

            Dim arityOffset As Integer = 0

            Dim containingType = namedType.ContainingType
            If containingType IsNot Nothing Then
                Dim containingTypeCumulativeArity As Integer

                GetGenericTypeParameterSymbol(position, containingType, containingTypeCumulativeArity, typeArgument)
                cumulativeArity += containingTypeCumulativeArity
                arityOffset = containingTypeCumulativeArity
            End If

            If arityOffset <= position AndAlso position < cumulativeArity Then
                Debug.Assert(typeArgument Is Nothing)
                typeArgument = namedType.TypeParameters(position - arityOffset)
            End If
        End Sub

        ''' <summary> 
        ''' Search through the members of the <see cref="_containingType"/> type symbol to find the method that matches a particular signature. 
        ''' </summary> 
        ''' <param name="memberRefOrMethodDef">A MemberRef or a MethodDef handle that can be used to obtain the name and signature of the method</param> 
        ''' <param name="methodsOnly">True to only return a method.</param> 
        ''' <returns>The matching method symbol, or null if the inputs do not correspond to a valid method.</returns>
        Friend Function FindMember(memberRefOrMethodDef As EntityHandle, methodsOnly As Boolean) As Symbol
            Try
                Dim memberName As String
                Dim signatureHandle As BlobHandle

                Select Case memberRefOrMethodDef.Kind
                    Case HandleKind.MemberReference
                        Dim memberRef = CType(memberRefOrMethodDef, MemberReferenceHandle)
                        memberName = [Module].GetMemberRefNameOrThrow(memberRef)
                        signatureHandle = [Module].GetSignatureOrThrow(memberRef)

                    Case HandleKind.MethodDefinition
                        Dim methodDef = CType(memberRefOrMethodDef, MethodDefinitionHandle)
                        memberName = [Module].GetMethodDefNameOrThrow(methodDef)
                        signatureHandle = [Module].GetMethodSignatureOrThrow(methodDef)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(memberRefOrMethodDef.Kind)
                End Select

                Dim signatureHeader As SignatureHeader
                Dim signaturePointer As BlobReader = Me.DecodeSignatureHeaderOrThrow(signatureHandle, signatureHeader)

                Select Case signatureHeader.RawValue And SignatureHeader.CallingConventionOrKindMask
                    Case SignatureCallingConvention.Default, SignatureCallingConvention.VarArgs
                        Dim typeParamCount As Integer
                        Dim targetParamInfo As ParamInfo(Of TypeSymbol)() = Me.DecodeSignatureParametersOrThrow(signaturePointer, signatureHeader, typeParamCount)
                        Return FindMethodBySignature(_containingType, memberName, signatureHeader, typeParamCount, targetParamInfo)

                    Case SignatureKind.Field
                        If methodsOnly Then
                            ' skip
                            Return Nothing
                        End If

                        Dim fieldInfo As FieldInfo(Of TypeSymbol) = Me.DecodeFieldSignature(signaturePointer)
                        Return FindFieldBySignature(_containingType, memberName, fieldInfo)

                    Case Else
                        ' error
                        Return Nothing
                End Select
            Catch mrEx As BadImageFormatException
                Return Nothing
            End Try
        End Function

        Private Shared Function FindFieldBySignature(targetTypeSymbol As TypeSymbol, targetMemberName As String, fieldInfo As FieldInfo(Of TypeSymbol)) As FieldSymbol

            Dim type As TypeSymbol = Nothing
            Dim customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)) = Nothing
            PEFieldSymbol.GetSignatureParts(fieldInfo, type, customModifiers)

            For Each member In targetTypeSymbol.GetMembers(targetMemberName)
                Dim field = TryCast(member, FieldSymbol)
                If field IsNot Nothing AndAlso
                   TypeSymbol.Equals(field.Type, type, TypeCompareKind.AllIgnoreOptionsForVB) AndAlso
                   CustomModifiersMatch(field.CustomModifiers, customModifiers) Then

                    ' Behavior in the face of multiple matching signatures is
                    ' implementation defined - we'll just pick the first one.
                    Return field
                End If
            Next

            Return Nothing
        End Function

        Private Shared Function FindMethodBySignature(targetTypeSymbol As TypeSymbol, targetMemberName As String, targetMemberSignatureHeader As SignatureHeader, targetMemberTypeParamCount As Integer, targetParamInfo As ParamInfo(Of TypeSymbol)()) As MethodSymbol
            For Each member In targetTypeSymbol.GetMembers(targetMemberName)
                Dim method = TryCast(member, MethodSymbol)

                If method IsNot Nothing AndAlso
                   (CType(method.CallingConvention, Byte) = targetMemberSignatureHeader.RawValue) AndAlso
                   (targetMemberTypeParamCount = method.Arity) AndAlso
                   MethodSymbolMatchesParamInfo(method, targetParamInfo) Then

                    ' Behavior in the face of multiple matching signatures is
                    ' implementation defined - we'll just pick the first one.
                    Return method
                End If
            Next

            Return Nothing
        End Function

        Private Shared Function MethodSymbolMatchesParamInfo(candidateMethod As MethodSymbol, targetParamInfo As ParamInfo(Of TypeSymbol)()) As Boolean
            Dim numParams As Integer = targetParamInfo.Length - 1
            If candidateMethod.ParameterCount <> numParams Then
                Return False
            End If

            If candidateMethod.Arity > 0 Then
                ' Construct the method with a bunch of IndexedTypeParameterSymbols. This allows any usage a method type
                ' parameters in the return type or parameter types to compare property (they will match the method type
                ' parameters returned by GetGenericMethodTypeParamSymbol in this class).
                candidateMethod = candidateMethod.Construct(StaticCast(Of TypeSymbol).From(IndexedTypeParameterSymbol.Take(candidateMethod.Arity)))
            End If

            If Not ReturnTypesMatch(candidateMethod, targetParamInfo(0)) Then
                Return False
            End If

            For i As Integer = 0 To numParams - 1
                If Not ParametersMatch(candidateMethod.Parameters(i), targetParamInfo(i + 1)) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function ParametersMatch(candidateParam As ParameterSymbol, ByRef targetParam As ParamInfo(Of TypeSymbol)) As Boolean
            ' This could be combined into a single return statement with a more complicated expression, but that would
            ' be harder to debug.

            If candidateParam.IsByRef <> targetParam.IsByRef Then
                Return False
            End If

            'CONSIDER: Do we want to add special handling for error types?  Right now, we expect they'll just fail to match.
            If Not TypeSymbol.Equals(candidateParam.Type, targetParam.Type, TypeCompareKind.AllIgnoreOptionsForVB) Then
                Return False
            End If

            If Not CustomModifiersMatch(candidateParam.CustomModifiers, targetParam.CustomModifiers) OrElse
               Not CustomModifiersMatch(candidateParam.RefCustomModifiers, targetParam.RefCustomModifiers) Then
                Return False
            End If

            Return True
        End Function

        Private Shared Function ReturnTypesMatch(candidateMethod As MethodSymbol, ByRef targetReturnParam As ParamInfo(Of TypeSymbol)) As Boolean
            Dim candidateReturnType As TypeSymbol = candidateMethod.ReturnType
            Dim targetReturnType As TypeSymbol = targetReturnParam.Type

            ' No special handling for error types.  Right now, we expect they'll just fail to match.
            If Not TypeSymbol.Equals(candidateReturnType, targetReturnType, TypeCompareKind.AllIgnoreOptionsForVB) OrElse candidateMethod.ReturnsByRef <> targetReturnParam.IsByRef Then
                Return False
            End If

            If Not CustomModifiersMatch(candidateMethod.ReturnTypeCustomModifiers, targetReturnParam.CustomModifiers) OrElse
               Not CustomModifiersMatch(candidateMethod.RefCustomModifiers, targetReturnParam.RefCustomModifiers) Then
                Return False
            End If

            Return True
        End Function

        Private Shared Function CustomModifiersMatch(candidateReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier), targetReturnTypeCustomModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol))) As Boolean
            If targetReturnTypeCustomModifiers.IsDefault OrElse targetReturnTypeCustomModifiers.IsEmpty Then
                Return candidateReturnTypeCustomModifiers.IsDefault OrElse candidateReturnTypeCustomModifiers.IsEmpty
            ElseIf candidateReturnTypeCustomModifiers.IsDefault Then
                Return False
            End If

            Dim n = candidateReturnTypeCustomModifiers.Length
            If targetReturnTypeCustomModifiers.Length <> n Then
                Return False
            End If

            For i As Integer = 0 To n - 1
                Dim targetCustomModifier = targetReturnTypeCustomModifiers(i)
                Dim candidateCustomModifier As CustomModifier = candidateReturnTypeCustomModifiers(i)

                If targetCustomModifier.IsOptional <> candidateCustomModifier.IsOptional OrElse
                   Not Object.Equals(targetCustomModifier.Modifier, candidateCustomModifier.Modifier) Then
                    Return False
                End If
            Next

            Return True
        End Function
    End Class
End Namespace

