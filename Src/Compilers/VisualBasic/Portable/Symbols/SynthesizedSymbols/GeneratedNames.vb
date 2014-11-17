' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Helper class to generate synthesized names.
    ''' </summary>
    Friend NotInheritable Class GeneratedNames

        ''' <summary>
        ''' Generates the name of an operator's function local based on the operator name.
        ''' </summary>
        Public Shared Function MakeOperatorLocalName(name As String) As String
            Debug.Assert(name.StartsWith("op_"))
            Return String.Format(StringConstants.OperatorLocalName, name)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's type
        ''' </summary>
        Public Shared Function MakeStateMachineTypeName(index As Integer, topMethodMetadataName As String) As String
            topMethodMetadataName = EnsureNoDotsInTypeName(topMethodMetadataName)
            Return String.Format(StringConstants.StateMachineTypeNameMask, index, topMethodMetadataName)
        End Function

        Public Shared Function TryParseStateMachineTypeName(stateMachineTypeName As String, <Out> ByRef index As Integer, <Out> ByRef methodName As String) As Boolean
            If Not stateMachineTypeName.StartsWith(StringConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal) Then
                Return False
            End If

            Dim prefixLength As Integer = StringConstants.StateMachineTypeNamePrefix.Length
            Dim separatorPos = stateMachineTypeName.IndexOf("_"c, prefixLength)
            If separatorPos < 0 OrElse separatorPos = stateMachineTypeName.Length - 1 Then
                Return False
            End If

            If Not Integer.TryParse(stateMachineTypeName.Substring(prefixLength, separatorPos - prefixLength), NumberStyles.None, CultureInfo.InvariantCulture, index) Then
                Return False
            End If

            methodName = stateMachineTypeName.Substring(separatorPos + 1)
            Return True
        End Function

        Public Shared Function EnsureNoDotsInTypeName(Name As String) As String
            ' CLR generally allows names with dots, however some APIs like IMetaDataImport
            ' can only return full type names combined with namespaces. 
            ' see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
            ' When working with such APIs, names with dots become ambiguous since metadata 
            ' consumer cannot figure where namespace ends and actual type name starts.
            ' Therefore it is a good practice to avoid type names with dots.
            If (Name.IndexOf("."c) >= 0) Then
                Name = Name.Replace("."c, "_"c)
            End If

            Return Name
        End Function

        ''' <summary>
        ''' Generates the name of a state machine 'builder' field 
        ''' </summary>
        Public Shared Function MakeStateMachineBuilderFieldName() As String
            Return StringConstants.StateMachineBuilderFieldName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine 'state' field 
        ''' </summary>
        Public Shared Function MakeStateMachineStateFieldName() As String
            Return StringConstants.StateMachineStateFieldName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's 'awaiter_xyz' field 
        ''' </summary>
        Public Shared Function MakeStateMachineAwaiterFieldName(index As Integer) As String
            Return String.Format(StringConstants.StateMachineAwaiterFieldName, index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's parameter name
        ''' </summary>
        Public Shared Function MakeStateMachineParameterName(paramName As String) As String
            Return StringConstants.LiftedLocalPrefix & paramName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's parameter name
        ''' </summary>
        Public Shared Function MakeIteratorParameterProxyName(paramName As String) As String
            Return StringConstants.IteratorParameterProxyName & paramName
        End Function

        ''' <summary>
        ''' Generates the name of a static lambda display class instance cache
        ''' </summary>
        ''' <returns></returns>
        Public Shared Function MakeCachedFrameInstanceName() As String
            Return StringConstants.CachedFrameInstanceName
        End Function

        ''' <summary>
        ''' Generates the name of a field used to store stack-spilled value 
        ''' </summary>
        Public Shared Function MakeStateMachineStackSpillFieldName(index As Integer) As String
            Return String.Format(StringConstants.StateMachineStackSpillNameMask, index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's field created to store captured r-value expression
        ''' </summary>
        Public Shared Function MakeStateMachineExpressionCaptureName(index As Integer) As String
            Return String.Format(StringConstants.StateMachineExpressionCaptureNameMask, index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's local name
        ''' </summary>
        Public Shared Function MakeStateMachineLocalName(index As Integer, localName As String) As String
            Return String.Format(StringConstants.StateMachineLocalNameMask, index, If(localName, "")) ' TODO: empty temp local name??
        End Function

        ''' <summary>
        ''' Generates the name of a field that backs Current property
        ''' </summary>
        Public Shared Function MakeIteratorCurrentFieldName() As String
            Return StringConstants.IteratorCurrentFieldName
        End Function

        ''' <summary>
        ''' Generates the name of a field where initial thread ID is stored
        ''' </summary>
        Public Shared Function MakeIteratorInitialThreadIdName() As String
            Return StringConstants.IteratorInitialThreadIdName
        End Function

        ''' <summary>
        ''' The function is reverse of 'MakeStateMachineLocalName'; it tries to parse the local name assuming 
        ''' it is produced by MakeStateMachineLocalName and returns 'index' and 'name' in case of successful.
        ''' </summary>
        Public Shared Function TryParseStateMachineLocalName(proxyName As String, <Out()> ByRef localName As String, <Out()> ByRef index As Integer) As Boolean
            localName = Nothing
            index = 0

            ' All names should start with "$VB$ResumableLocal_"
            If Not proxyName.StartsWith(StringConstants.StateMachineLocalNamePrefix) Then
                Return False
            End If

            Dim separator As Integer = proxyName.LastIndexOf("$"c)
            If separator < 0 Then
                Return False
            End If

            Dim prefixLen As Integer = StringConstants.StateMachineLocalNamePrefix.Length
            localName = proxyName.Substring(prefixLen, separator - prefixLen)
            Return Integer.TryParse(proxyName.Substring(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference
        ''' </summary>
        Public Shared Function MakeStateMachineCapturedMeName() As String
            Return StringConstants.LiftedMeName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference of lambda closure
        ''' </summary>
        Public Shared Function MakeStateMachineCapturedClosureMeName(closureName As String) As String
            Return StringConstants.LiftedNonLocalPrefix & closureName
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

        Friend Shared Function MakeHoistedLocalFieldName(kind As SynthesizedLocalKind, type As TypeSymbol, ByRef index As Integer) As String
            Debug.Assert(kind.IsLongLived())

            Const SynthesizedLocalNamePrefix As String = "VB$"

            Select Case kind
                Case SynthesizedLocalKind.XmlInExpressionLambda
                    index += 1
                    Return SynthesizedLocalNamePrefix & type.GetNativeCompilerVType() & "$L" & index

                Case SynthesizedLocalKind.LambdaDisplayClass
                    index += 1
                    Return StringConstants.ClosureVariablePrefix & index

                Case SynthesizedLocalKind.With
                    index += 1
                    Return StringConstants.SynthesizedLocalKindWith & index

                Case SynthesizedLocalKind.Lock
                    Return StringConstants.SynthesizedLocalKindLock
                Case SynthesizedLocalKind.Using
                    Return StringConstants.SynthesizedLocalKindUsing
                Case SynthesizedLocalKind.ForEachEnumerator
                    Return StringConstants.SynthesizedLocalKindForEachEnumerator
                Case SynthesizedLocalKind.ForEachArray
                    Return StringConstants.SynthesizedLocalKindForEachArray
                Case SynthesizedLocalKind.ForEachArrayIndex
                    Return StringConstants.SynthesizedLocalKindForEachArrayIndex
                Case SynthesizedLocalKind.LockTaken
                    Return StringConstants.SynthesizedLocalKindLockTaken

                Case SynthesizedLocalKind.ForLimit
                    Return StringConstants.ForLimit
                Case SynthesizedLocalKind.ForStep
                    Return StringConstants.ForStep
                Case SynthesizedLocalKind.ForLoopObject
                    Return StringConstants.ForLoopObject
                Case SynthesizedLocalKind.ForDirection
                    Return StringConstants.ForDirection

                Case SynthesizedLocalKind.StateMachineReturnValue
                    Return StringConstants.StateMachineReturnValueLocalName
                Case SynthesizedLocalKind.StateMachineCachedState
                    Return StringConstants.StateMachineCachedState

                Case SynthesizedLocalKind.OnErrorActiveHandler
                    Return StringConstants.OnErrorActiveHandler
                Case SynthesizedLocalKind.OnErrorResumeTarget
                    Return StringConstants.OnErrorResumeTarget
                Case SynthesizedLocalKind.OnErrorCurrentStatement
                    Return StringConstants.OnErrorCurrentStatement
                Case SynthesizedLocalKind.OnErrorCurrentLine
                    Return StringConstants.OnErrorCurrentLine
            End Select

            Throw ExceptionUtilities.UnexpectedValue(kind)
        End Function

        Friend Shared Function TryParseLocalName(name As String, ByRef kind As SynthesizedLocalKind, ByRef uniqueId As Integer) As Boolean
            ' TODO: revisit this method

            uniqueId = 0

            Select Case name
                Case StringConstants.SynthesizedLocalKindWith
                    kind = SynthesizedLocalKind.With

                Case StringConstants.StateMachineCachedState
                    kind = SynthesizedLocalKind.StateMachineCachedState
                Case StringConstants.StateMachineReturnValueLocalName
                    kind = SynthesizedLocalKind.StateMachineReturnValue

                Case Else

                    If name.StartsWith(StringConstants.ClosureVariablePrefix, StringComparison.Ordinal) Then
                        kind = SynthesizedLocalKind.LambdaDisplayClass
                        Return True
                    End If

                    kind = SynthesizedLocalKind.UserDefined
                    Return False
            End Select
            Return True
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
                    name = StringConstants.SynthesizedLocalKindWith & uniqueId
                    uniqueId += 1

                Case Else
                    name = Nothing
            End Select

            Return name
        End Function

        Friend Shared Function MakeLambdaMethodName(index As Integer) As String
            Return StringConstants.LAMBDA_PREFIX & index
        End Function

        Friend Shared Function MakeLambdaDisplayClassName(index As Integer) As String
            Return StringConstants.ClosureClassPrefix & index
        End Function

        Friend Shared Function MakeLambdaDisplayClassStorageName(uniqueId As Integer) As String
            Return StringConstants.ClosureVariablePrefix & uniqueId
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

            Return String.Format(StringConstants.StaticLocalFieldNameMask, methodName, methodSignature, localName)
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

    End Class

End Namespace