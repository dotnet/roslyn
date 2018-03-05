' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend Module SymbolExtensions
        <Extension>
        Friend Function GetCustomTypeInfoPayload(method As MethodSymbol) As ReadOnlyCollection(Of Byte)
            Return method.DeclaringCompilation.GetCustomTypeInfoPayload(method.ReturnType)
        End Function

        <Extension>
        Public Function IsContainingSymbolOfAllTypeParameters(containingSymbol As Symbol, type As TypeSymbol) As Boolean
            Return type.VisitType(s_hasInvalidTypeParameterFunc, containingSymbol) Is Nothing
        End Function

        Private ReadOnly s_hasInvalidTypeParameterFunc As Func(Of TypeSymbol, Symbol, Boolean) = AddressOf HasInvalidTypeParameter

        Private Function HasInvalidTypeParameter(type As TypeSymbol, containingSymbol As Symbol) As Boolean
            If type.TypeKind = TypeKind.TypeParameter Then
                Dim symbol = type.ContainingSymbol
                While containingSymbol IsNot Nothing AndAlso containingSymbol.Kind <> SymbolKind.Namespace
                    If containingSymbol = symbol Then
                        Return False
                    End If

                    containingSymbol = containingSymbol.ContainingSymbol
                End While
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' In VB, the type parameters of state machine classes (i.e. for implementing async
        ''' or iterator methods) are mangled.  We unmangle them here so that the unmangled
        ''' names will bind properly.  (Code gen only uses the ordinal, so the name shouldn't
        ''' affect behavior).
        ''' </summary>
        <Extension>
        Public Function GetUnmangledName(sourceTypeParameter As TypeParameterSymbol) As String
            Dim sourceName = sourceTypeParameter.Name

            If sourceName.StartsWith(StringConstants.StateMachineTypeParameterPrefix, StringComparison.Ordinal) Then
                Debug.Assert(sourceTypeParameter.ContainingSymbol.Name.
                             StartsWith(StringConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal))
                Debug.Assert(sourceName.Length > StringConstants.StateMachineTypeParameterPrefix.Length)
                Return sourceName.Substring(StringConstants.StateMachineTypeParameterPrefix.Length)
            End If

            Return sourceName
        End Function

        <Extension>
        Friend Function IsClosureOrStateMachineType(type As TypeSymbol) As Boolean
            Return type.IsClosureType() OrElse type.IsStateMachineType()
        End Function

        <Extension>
        Friend Function IsClosureType(type As TypeSymbol) As Boolean
            Return type.Name.StartsWith(StringConstants.DisplayClassPrefix, StringComparison.Ordinal)
        End Function

        <Extension>
        Friend Function IsStateMachineType(type As TypeSymbol) As Boolean
            Return type.Name.StartsWith(StringConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal)
        End Function

        <Extension>
        Friend Function GetAllTypeParameters(method As MethodSymbol) As ImmutableArray(Of TypeParameterSymbol)
            Dim builder = ArrayBuilder(Of TypeParameterSymbol).GetInstance()
            method.ContainingType.GetAllTypeParameters(builder)
            builder.AddRange(method.TypeParameters)
            Return builder.ToImmutableAndFree()
        End Function

        <Extension>
        Friend Function IsAnonymousTypeField(field As FieldSymbol, <Out> ByRef unmangledName As String) As Boolean
            If GeneratedNames.GetKind(field.ContainingType.Name) <> GeneratedNameKind.AnonymousType Then
                unmangledName = Nothing
                Return False
            End If

            unmangledName = field.Name
            If unmangledName(0) = "$"c Then
                unmangledName = unmangledName.Substring(1)
            End If

            Return True
        End Function
    End Module
End Namespace
