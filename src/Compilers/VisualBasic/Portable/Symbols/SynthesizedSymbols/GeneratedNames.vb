' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Helper class to generate synthesized names.
    ''' </summary>
    Friend NotInheritable Class GeneratedNames
        Friend Const DotReplacementInTypeNames As Char = "-"c
        Private Const s_methodNameSeparator As Char = "_"c
        Private Const s_idSeparator As Char = "-"c
        Private Const s_generationSeparator As Char = "#"c

        ''' <summary>
        ''' Generates the name of a state machine's type.
        ''' </summary>
        Public Shared Function MakeStateMachineTypeName(methodName As String, methodOrdinal As Integer, generation As Integer) As String
            Debug.Assert(methodOrdinal >= -1)
            Return MakeMethodScopedSynthesizedName(StringConstants.StateMachineTypeNamePrefix, methodOrdinal, generation, methodName, isTypeName:=True)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine 'state' field 
        ''' </summary>
        Public Shared Function MakeStateMachineStateFieldName() As String
            Return StringConstants.StateMachineStateFieldName
        End Function

        Public Shared Function MakeBaseMethodWrapperName(methodName As String, isMyBase As Boolean) As String
            Return StringConstants.BaseMethodWrapperNamePrefix & methodName & If(isMyBase, "_MyBase", "_MyClass")
        End Function

        Public Shared Function ReusableHoistedLocalFieldName(number As Integer) As String
            Return StringConstants.ReusableHoistedLocalFieldName & StringExtensions.GetNumeral(number)
        End Function

        Public Shared Function MakeStaticLambdaDisplayClassName(methodOrdinal As Integer, generation As Integer) As String
            Debug.Assert(methodOrdinal >= -1)
            Debug.Assert(generation >= 0)

            Return MakeMethodScopedSynthesizedName(StringConstants.DisplayClassPrefix, methodOrdinal, generation)
        End Function

        Friend Shared Function MakeLambdaDisplayClassName(methodOrdinal As Integer, generation As Integer, closureOrdinal As Integer, closureGeneration As Integer, isDelegateRelaxation As Boolean) As String
            Debug.Assert(closureOrdinal >= 0)
            Debug.Assert(methodOrdinal >= 0)
            Debug.Assert(generation >= 0)

            Dim prefix = If(isDelegateRelaxation, StringConstants.DelegateRelaxationDisplayClassPrefix, StringConstants.DisplayClassPrefix)
            Return MakeMethodScopedSynthesizedName(prefix, methodOrdinal, generation, entityOrdinal:=closureOrdinal, entityGeneration:=closureGeneration, isTypeName:=True)
        End Function

        Friend Shared Function MakeDisplayClassGenericParameterName(parameterIndex As Integer) As String
            Return StringConstants.DisplayClassGenericParameterNamePrefix & StringExtensions.GetNumeral(parameterIndex)
        End Function

        Friend Shared Function MakeLambdaMethodName(methodOrdinal As Integer, generation As Integer, lambdaOrdinal As Integer, lambdaGeneration As Integer, lambdaKind As SynthesizedLambdaKind) As String
            Debug.Assert(methodOrdinal >= -1)
            Debug.Assert(lambdaOrdinal >= 0)

            Dim prefix = If(lambdaKind = SynthesizedLambdaKind.DelegateRelaxationStub,
                            StringConstants.DelegateRelaxationMethodNamePrefix,
                            StringConstants.LambdaMethodNamePrefix)

            Return MakeMethodScopedSynthesizedName(prefix, methodOrdinal, generation, entityOrdinal:=lambdaOrdinal, entityGeneration:=lambdaGeneration)
        End Function

        ''' <summary>
        ''' Generates the name of a static lambda display class instance cache
        ''' </summary>
        Public Shared Function MakeCachedFrameInstanceName() As String
            Return StringConstants.LambdaCacheFieldPrefix
        End Function

        Friend Shared Function MakeLambdaCacheFieldName(methodOrdinal As Integer, generation As Integer, lambdaOrdinal As Integer, lambdaGeneration As Integer, lambdaKind As SynthesizedLambdaKind) As String
            Debug.Assert(methodOrdinal >= -1)
            Debug.Assert(lambdaOrdinal >= 0)

            Dim prefix = If(lambdaKind = SynthesizedLambdaKind.DelegateRelaxationStub,
                            StringConstants.DelegateRelaxationCacheFieldPrefix,
                            StringConstants.LambdaCacheFieldPrefix)

            Return MakeMethodScopedSynthesizedName(prefix, methodOrdinal, generation, entityOrdinal:=lambdaOrdinal, entityGeneration:=lambdaGeneration)
        End Function

        Friend Shared Function MakeDelegateRelaxationParameterName(parameterIndex As Integer) As String
            Return StringConstants.DelegateStubParameterPrefix & StringExtensions.GetNumeral(parameterIndex)
        End Function

        Private Shared Function MakeMethodScopedSynthesizedName(prefix As String,
                                                                methodOrdinal As Integer,
                                                                methodGeneration As Integer,
                                                                Optional methodNameOpt As String = Nothing,
                                                                Optional entityOrdinal As Integer = -1,
                                                                Optional entityGeneration As Integer = -1,
                                                                Optional isTypeName As Boolean = False) As String
            Debug.Assert(methodOrdinal >= -1)
            Debug.Assert(methodGeneration >= 0 OrElse methodGeneration = -1 AndAlso methodOrdinal = -1)
            Debug.Assert(entityOrdinal >= -1)
            Debug.Assert(entityGeneration >= 0 OrElse entityGeneration = -1 AndAlso entityOrdinal = -1)
            Debug.Assert(entityGeneration = -1 OrElse entityGeneration >= methodGeneration)

            Dim result = PooledStringBuilder.GetInstance()
            Dim builder = result.Builder

            builder.Append(prefix)

            If methodOrdinal >= 0 Then
                builder.Append(methodOrdinal)

                If methodGeneration > 0 Then
                    builder.Append(s_generationSeparator)
                    builder.Append(methodGeneration)
                End If
            End If

            If entityOrdinal >= 0 Then
                If methodOrdinal >= 0 Then
                    ' Can't use underscore since name parser uses it to find the method name.
                    builder.Append(s_idSeparator)
                End If

                builder.Append(entityOrdinal)

                If entityGeneration > 0 Then
                    builder.Append(s_generationSeparator)
                    builder.Append(entityGeneration)
                End If
            End If

            If methodNameOpt IsNot Nothing Then
                builder.Append(s_methodNameSeparator)
                builder.Append(methodNameOpt)

                ' CLR generally allows names with dots, however some APIs like IMetaDataImport
                ' can only return full type names combined with namespaces. 
                ' see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                ' When working with such APIs, names with dots become ambiguous since metadata 
                ' consumer cannot figure where namespace ends and actual type name starts.
                ' Therefore it is a good practice to avoid type names with dots.
                If isTypeName Then
                    builder.Replace("."c, DotReplacementInTypeNames)
                End If
            End If

            Return result.ToStringAndFree()
        End Function

        Public Shared Function TryParseStateMachineTypeName(stateMachineTypeName As String, <Out> ByRef methodName As String) As Boolean
            If Not stateMachineTypeName.StartsWith(StringConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal) Then
                Return False
            End If

            Dim prefixLength As Integer = StringConstants.StateMachineTypeNamePrefix.Length
            Dim separatorPos = stateMachineTypeName.IndexOf(s_methodNameSeparator, prefixLength)
            If separatorPos < 0 OrElse separatorPos = stateMachineTypeName.Length - 1 Then
                Return False
            End If

            methodName = stateMachineTypeName.Substring(separatorPos + 1)
            Return True
        End Function

        ''' <summary>
        ''' Generates the name of a state machine 'builder' field 
        ''' </summary>
        Public Shared Function MakeStateMachineBuilderFieldName() As String
            Return StringConstants.StateMachineBuilderFieldName
        End Function

        ''' <summary>
        ''' Generates the name of a field that backs Current property
        ''' </summary>
        Public Shared Function MakeIteratorCurrentFieldName() As String
            Return StringConstants.IteratorCurrentFieldName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's awaiter field 
        ''' </summary>
        Public Shared Function MakeStateMachineAwaiterFieldName(index As Integer) As String
            Return StringConstants.StateMachineAwaiterFieldPrefix & StringExtensions.GetNumeral(index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's parameter name
        ''' </summary>
        Public Shared Function MakeStateMachineParameterName(paramName As String) As String
            Return StringConstants.HoistedUserVariablePrefix & paramName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's parameter name
        ''' </summary>
        Public Shared Function MakeIteratorParameterProxyName(paramName As String) As String
            Return StringConstants.IteratorParameterProxyPrefix & paramName
        End Function

        ''' <summary>
        ''' Generates the name of a field where initial thread ID is stored
        ''' </summary>
        Public Shared Function MakeIteratorInitialThreadIdName() As String
            Return StringConstants.IteratorInitialThreadIdName
        End Function

        ''' <summary>
        ''' Try to parse the local (or parameter) name and return <paramref name="variableName"/> if successful.
        ''' </summary>
        Public Shared Function TryParseHoistedUserVariableName(proxyName As String, <Out> ByRef variableName As String) As Boolean
            variableName = Nothing

            Dim prefixLen As Integer = StringConstants.HoistedUserVariablePrefix.Length
            If proxyName.Length <= prefixLen Then
                Return False
            End If

            ' All names should start with "$VB$Local_"
            If Not proxyName.StartsWith(StringConstants.HoistedUserVariablePrefix, StringComparison.Ordinal) Then
                Return False
            End If

            variableName = proxyName.Substring(prefixLen)
            Return True
        End Function

        ''' <summary>
        ''' Try to parse the local name and return <paramref name="variableName"/> and <paramref name="index"/> if successful.
        ''' </summary>
        Public Shared Function TryParseStateMachineHoistedUserVariableName(proxyName As String, <Out> ByRef variableName As String, <Out()> ByRef index As Integer) As Boolean
            variableName = Nothing
            index = 0

            ' All names should start with "$VB$ResumableLocal_"
            If Not proxyName.StartsWith(StringConstants.StateMachineHoistedUserVariablePrefix, StringComparison.Ordinal) Then
                Return False
            End If

            Dim prefixLen As Integer = StringConstants.StateMachineHoistedUserVariablePrefix.Length
            Dim separator As Integer = proxyName.LastIndexOf("$"c)
            If separator <= prefixLen Then
                Return False
            End If

            variableName = proxyName.Substring(prefixLen, separator - prefixLen)
            Return Integer.TryParse(proxyName.Substring(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference
        ''' </summary>
        Public Shared Function MakeStateMachineCapturedMeName() As String
            Return StringConstants.HoistedMeName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference of lambda closure
        ''' </summary>
        Public Shared Function MakeStateMachineCapturedClosureMeName(closureName As String) As String
            Return StringConstants.HoistedSpecialVariablePrefix & closureName
        End Function

        Friend Const AnonymousTypeOrDelegateCommonPrefix = "VB$Anonymous"
        Friend Const AnonymousTypeTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix & "Type_"
        Friend Const AnonymousDelegateTemplateNamePrefix = AnonymousTypeOrDelegateCommonPrefix & "Delegate_"

        Friend Shared Function MakeAnonymousTypeTemplateName(prefix As String, index As Integer, submissionSlotIndex As Integer, moduleId As String) As String
            Return If(submissionSlotIndex >= 0,
                           String.Format("{0}{1}_{2}{3}", prefix, submissionSlotIndex, index, moduleId),
                           String.Format("{0}{1}{2}", prefix, index, moduleId))
        End Function

        Friend Shared Function TryParseAnonymousTypeTemplateName(prefix As String, name As String, <Out()> ByRef index As Integer) As Boolean
            ' No callers require anonymous types from net modules,
            ' so names with module id are ignored.
            If name.StartsWith(prefix, StringComparison.Ordinal) AndAlso
                Integer.TryParse(name.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, index) Then
                Return True
            End If
            index = -1
            Return False
        End Function

        Friend Shared Function MakeSynthesizedLocalName(kind As SynthesizedLocalKind, ByRef uniqueId As Integer) As String
            Debug.Assert(kind.IsLongLived())

            ' The following variables have to be named, EE depends on the name format.
            Dim name As String
            Select Case kind
                Case SynthesizedLocalKind.LambdaDisplayClass
                    name = MakeLambdaDisplayClassStorageName(uniqueId)
                    uniqueId += 1

                Case SynthesizedLocalKind.With
                    ' Dev12 didn't name the local. We do so that we can do better job in EE evaluating With statements.
                    name = StringConstants.HoistedWithLocalPrefix & StringExtensions.GetNumeral(uniqueId)
                    uniqueId += 1

                Case Else
                    name = Nothing
            End Select

            Return name
        End Function

        Friend Shared Function MakeLambdaDisplayClassStorageName(uniqueId As Integer) As String
            Return StringConstants.ClosureVariablePrefix & StringExtensions.GetNumeral(uniqueId)
        End Function

        Friend Shared Function MakeSignatureString(signature As Byte()) As String
            Dim builder = PooledStringBuilder.GetInstance()
            For Each b In signature
                ' Note the format of each byte is not fixed width, so the resulting string may be
                ' ambiguous. And since this method Is used to generate field names for static
                ' locals, the same field name may be generated for two locals with the same
                ' local name in overloaded methods. The native compiler has the same behavior.
                ' Using a fixed width format {0:X2} would solve this but since the EE relies on
                ' the format for recognizing static locals, that would be a breaking change.
                builder.Builder.AppendFormat("{0:X}", b)
            Next
            Return builder.ToStringAndFree()
        End Function

        Friend Shared Function MakeStaticLocalFieldName(
            methodName As String,
            methodSignature As String,
            localName As String) As String

            Return String.Format(StringConstants.StaticLocalFieldNamePrefix & "{0}${1}${2}", methodName, methodSignature, localName)
        End Function

        Friend Shared Function TryParseStaticLocalFieldName(
            fieldName As String,
            <Out> ByRef methodName As String,
            <Out> ByRef methodSignature As String,
            <Out> ByRef localName As String) As Boolean

            If fieldName.StartsWith(StringConstants.StaticLocalFieldNamePrefix, StringComparison.Ordinal) Then
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
