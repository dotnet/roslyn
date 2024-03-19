' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class GeneratedNameParser
        Friend Shared Function GetKind(name As String) As GeneratedNameKind
            If name.StartsWith(GeneratedNameConstants.HoistedMeName, StringComparison.Ordinal) Then
                Return GeneratedNameKind.HoistedMeField
            ElseIf name.StartsWith(GeneratedNameConstants.StateMachineStateFieldName, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StateMachineStateField
            ElseIf name.StartsWith(GeneratedNameConstants.StaticLocalFieldNamePrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StaticLocalField
            ElseIf name.StartsWith(GeneratedNameConstants.HoistedSynthesizedLocalPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.HoistedSynthesizedLocalField
            ElseIf name.StartsWith(GeneratedNameConstants.HoistedUserVariablePrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.HoistedUserVariableField
            ElseIf name.StartsWith(GeneratedNameConstants.IteratorCurrentFieldName, StringComparison.Ordinal) Then
                Return GeneratedNameKind.IteratorCurrentField
            ElseIf name.StartsWith(GeneratedNameConstants.IteratorInitialThreadIdName, StringComparison.Ordinal) Then
                Return GeneratedNameKind.IteratorInitialThreadIdField
            ElseIf name.StartsWith(GeneratedNameConstants.IteratorParameterProxyPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.IteratorParameterProxyField
            ElseIf name.StartsWith(GeneratedNameConstants.StateMachineAwaiterFieldPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StateMachineAwaiterField
            ElseIf name.StartsWith(GeneratedNameConstants.HoistedWithLocalPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.HoistedWithLocalPrefix
            ElseIf name.StartsWith(GeneratedNameConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.StateMachineHoistedUserVariableOrDisplayClassField
            ElseIf name.StartsWith(GeneratedNameConstants.AnonymousTypeTemplateNamePrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.AnonymousType
            ElseIf name.StartsWith(GeneratedNameConstants.DisplayClassPrefix, StringComparison.Ordinal) Then
                Return GeneratedNameKind.LambdaDisplayClass
            ElseIf name.Equals(GeneratedNameConstants.It, StringComparison.Ordinal) OrElse
                    name.Equals(GeneratedNameConstants.It1, StringComparison.Ordinal) OrElse
                    name.Equals(GeneratedNameConstants.It2, StringComparison.Ordinal) Then
                Return GeneratedNameKind.TransparentIdentifier
            ElseIf name.Equals(GeneratedNameConstants.ItAnonymous, StringComparison.Ordinal) Then
                ' We distinguish GeneratedNameConstants.ItAnonymous, because it won't be an instance
                ' of an anonymous type.
                Return GeneratedNameKind.AnonymousTransparentIdentifier
            End If

            Return GeneratedNameKind.None
        End Function

        Public Shared Function TryParseStateMachineTypeName(stateMachineTypeName As String, <Out> ByRef methodName As String) As Boolean
            If Not stateMachineTypeName.StartsWith(GeneratedNameConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal) Then
                Return False
            End If

            Dim prefixLength As Integer = GeneratedNameConstants.StateMachineTypeNamePrefix.Length
            Dim separatorPos = stateMachineTypeName.IndexOf(GeneratedNameConstants.MethodNameSeparator, prefixLength)
            If separatorPos < 0 OrElse separatorPos = stateMachineTypeName.Length - 1 Then
                Return False
            End If

            methodName = stateMachineTypeName.Substring(separatorPos + 1)
            Return True
        End Function

        ''' <summary>
        ''' Try to parse the local (or parameter) name and return <paramref name="variableName"/> if successful.
        ''' </summary>
        Public Shared Function TryParseHoistedUserVariableName(proxyName As String, <Out> ByRef variableName As String) As Boolean
            variableName = Nothing

            Dim prefixLen As Integer = GeneratedNameConstants.HoistedUserVariablePrefix.Length
            If proxyName.Length <= prefixLen Then
                Return False
            End If

            ' All names should start with "$VB$Local_"
            If Not proxyName.StartsWith(GeneratedNameConstants.HoistedUserVariablePrefix, StringComparison.Ordinal) Then
                Return False
            End If

            variableName = proxyName.Substring(prefixLen)
            Return True
        End Function

        ''' <summary>
        ''' Try to parse the local name and return <paramref name="variableName"/> and <paramref name="index"/> if successful.
        ''' </summary>
        Public Shared Function TryParseStateMachineHoistedUserVariableOrDisplayClassName(proxyName As String, <Out> ByRef variableName As String, <Out()> ByRef index As Integer) As Boolean
            variableName = Nothing
            index = 0

            ' All names should start with "$VB$ResumableLocal_"
            If Not proxyName.StartsWith(GeneratedNameConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix, StringComparison.Ordinal) Then
                Return False
            End If

            Dim prefixLen As Integer = GeneratedNameConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix.Length
            Dim separator As Integer = proxyName.LastIndexOf("$"c)
            If separator <= prefixLen Then
                Return False
            End If

            variableName = proxyName.Substring(prefixLen, separator - prefixLen)
            Return Integer.TryParse(proxyName.Substring(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, index)
        End Function

        Friend Shared Function TryParseStaticLocalFieldName(
            fieldName As String,
            <Out> ByRef methodName As String,
            <Out> ByRef methodSignature As String,
            <Out> ByRef localName As String) As Boolean

            If fieldName.StartsWith(GeneratedNameConstants.StaticLocalFieldNamePrefix, StringComparison.Ordinal) Then
                Dim parts = fieldName.Split("$"c)
                If parts.Length = 5 Then
                    methodName = parts(2)
                    methodSignature = parts(3)
                    localName = parts(4)
                    Return True
                End If
            End If

            methodName = Nothing
            methodSignature = Nothing
            localName = Nothing
            Return False
        End Function

        ' Extracts the slot index from a name of a field that stores hoisted variables or awaiters.
        ' Such a name ends with "$prefix{slot index}". 
        ' Returned slot index is >= 0.
        Friend Shared Function TryParseSlotIndex(prefix As String, fieldName As String, <Out> ByRef slotIndex As Integer) As Boolean
            If fieldName.StartsWith(prefix, StringComparison.Ordinal) AndAlso
                Integer.TryParse(fieldName.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, slotIndex) Then
                Return True
            End If
            slotIndex = -1
            Return False
        End Function

    End Class
End Namespace
