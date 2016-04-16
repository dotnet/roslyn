' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class CustomModifierUtils
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
            Debug.Assert(thisParam.Type.IsSameTypeIgnoringCustomModifiers(overriddenParam.Type))

            If Not overriddenParam.CustomModifiers.SequenceEqual(thisParam.CustomModifiers) OrElse
               (overriddenParam.IsByRef AndAlso thisParam.IsByRef AndAlso overriddenParam.CountOfCustomModifiersPrecedingByRef <> thisParam.CountOfCustomModifiersPrecedingByRef) OrElse
               thisParam.Type <> overriddenParam.Type Then
                thisParam = DirectCast(thisParam, SourceParameterSymbolBase).WithTypeAndCustomModifiers(
                    overriddenParam.Type,
                    overriddenParam.CustomModifiers,
                    If(thisParam.IsByRef, overriddenParam.CountOfCustomModifiersPrecedingByRef, 0US))
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace


