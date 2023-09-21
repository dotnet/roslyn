' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class GeneratedNameParser
        Private Shared ReadOnly s_prefixMapping As ImmutableArray(Of (prefix As String, kind As GeneratedNameKind)) = ImmutableArray.Create(
            (GeneratedNameConstants.HoistedMeName, GeneratedNameKind.HoistedMeField),
            (GeneratedNameConstants.StateMachineStateFieldName, GeneratedNameKind.StateMachineStateField),
            (GeneratedNameConstants.StaticLocalFieldNamePrefix, GeneratedNameKind.StaticLocalField),
            (GeneratedNameConstants.HoistedSynthesizedLocalPrefix, GeneratedNameKind.HoistedSynthesizedLocalField),
            (GeneratedNameConstants.HoistedUserVariablePrefix, GeneratedNameKind.HoistedUserVariableField),
            (GeneratedNameConstants.IteratorCurrentFieldName, GeneratedNameKind.IteratorCurrentField),
            (GeneratedNameConstants.IteratorInitialThreadIdName, GeneratedNameKind.IteratorInitialThreadIdField),
            (GeneratedNameConstants.IteratorParameterProxyPrefix, GeneratedNameKind.IteratorParameterProxyField),
            (GeneratedNameConstants.StateMachineAwaiterFieldPrefix, GeneratedNameKind.StateMachineAwaiterField),
            (GeneratedNameConstants.HoistedWithLocalPrefix, GeneratedNameKind.HoistedWithLocalPrefix),
            (GeneratedNameConstants.StateMachineHoistedUserVariableOrDisplayClassPrefix, GeneratedNameKind.StateMachineHoistedUserVariableOrDisplayClassField),
            (GeneratedNameConstants.AnonymousTypeTemplateNamePrefix, GeneratedNameKind.AnonymousType),
            (GeneratedNameConstants.DisplayClassPrefix, GeneratedNameKind.LambdaDisplayClass),
            (GeneratedNameConstants.It, GeneratedNameKind.TransparentIdentifier),
            (GeneratedNameConstants.It1, GeneratedNameKind.TransparentIdentifier),
            (GeneratedNameConstants.It2, GeneratedNameKind.TransparentIdentifier),
            (GeneratedNameConstants.ItAnonymous, GeneratedNameKind.AnonymousTransparentIdentifier)) ' We distinguish GeneratedNameConstants.ItAnonymous, because it won't be an instance of an anonymous type.

        Friend Shared Function GetKind(name As String) As GeneratedNameKind
            For Each prefixAndKind In s_prefixMapping
                If name.StartsWith(prefixAndKind.prefix, StringComparison.Ordinal) Then
                    Return prefixAndKind.kind
                End If
            Next

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
