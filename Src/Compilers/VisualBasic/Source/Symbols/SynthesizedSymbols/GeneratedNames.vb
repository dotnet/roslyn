' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Helper class to generate synthesized names.
    ''' </summary>
    Friend Class GeneratedNames

        ''' <summary>
        ''' Generates the name of an operator's function local based on the operator name.
        ''' </summary>
        Public Shared Function MakeOperatorLocalName(name As String) As String
            Debug.Assert(name.StartsWith("op_"))
            Return String.Format(StringConstants.OperatorLocalName, name)
        End Function

        ''' <summary>
        ''' Generates the name of a lambda's function value local.
        ''' </summary>
        Public Shared Function MakeTempLambdaLocalName() As String
            Return String.Empty
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's type
        ''' </summary>
        Public Shared Function MakeStateMachineTypeName(index As Integer, topMethodMetadataName As String) As String
            topMethodMetadataName = EnsureNoDotsInTypeName(topMethodMetadataName)
            Return String.Format(StringConstants.StateMachineTypeNameMask, index, topMethodMetadataName)
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
            Return Integer.TryParse(proxyName.Substring(separator + 1), index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference
        ''' </summary>
        Public Shared Function MakeStateMachineCapturedMeName() As String
            Return StringConstants.LiftedMePrefix
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference of lambda closure
        ''' </summary>
        Public Shared Function MakeStateMachineCaptiredClosureMeName(closureName As String) As String
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
                Integer.TryParse(name.Substring(prefix.Length), index) Then
                Return True
            End If
            index = -1
            Return False
        End Function

        Friend Shared Function GenerateTempName(tempKind As TempKind) As String
            Select Case tempKind
                Case TempKind.Lock
                    Return StringConstants.TempKindLock
                Case TempKind.Using
                    Return StringConstants.TempKindUsing
                Case TempKind.ForEachEnumerator
                    Return StringConstants.TempKindForEachEnumerator
                Case TempKind.ForEachArray
                    Return StringConstants.TempKindForEachArray
                Case TempKind.ForEachArrayIndex
                    Return StringConstants.TempKindForEachArrayIndex
                Case TempKind.LockTaken
                    Return StringConstants.TempKindLockTaken
                Case TempKind.With
                    Return StringConstants.TempKindWith

                Case TempKind.ForLimit
                    Return StringConstants.ForLimit
                Case TempKind.ForStep
                    Return StringConstants.ForStep
                Case TempKind.ForLoopObject
                    Return StringConstants.ForLoopObject
                Case TempKind.ForDirection
                    Return StringConstants.ForDirection

                Case TempKind.OnErrorActiveHandler
                    Return StringConstants.OnErrorActiveHandler
                Case TempKind.OnErrorResumeTarget
                    Return StringConstants.OnErrorResumeTarget
                Case TempKind.OnErrorCurrentStatement
                    Return StringConstants.OnErrorCurrentStatement
                Case TempKind.OnErrorCurrentLine
                    Return StringConstants.OnErrorCurrentLine
                Case TempKind.StateMachineCachedState
                    Return StringConstants.StateMachineCachedState
                Case TempKind.StateMachineException
                    Return StringConstants.StateMachineExceptionLocalName
                Case TempKind.StateMachineReturnValue
                    Return StringConstants.StateMachineReturnValueLocalName
            End Select

            Throw ExceptionUtilities.UnexpectedValue(tempKind)
        End Function

        Private Const TemporaryNamePrefix As String = "VB$"

        Friend Shared Function GenerateTempName(tempKind As TempKind, type As TypeSymbol, index As Integer) As String
            Select Case tempKind
                Case TempKind.XmlInExpressionLambda
                    Return TemporaryNamePrefix & type.GetNativeCompilerVType() & "$L" & index
            End Select

            Throw ExceptionUtilities.UnexpectedValue(tempKind)
        End Function

        Friend Shared Function TryParseTemporaryName(name As String, ByRef kind As TempKind, ByRef uniqueId As Integer) As Boolean

            'TODO: are we using this for anything?
            uniqueId = 0

            Select Case name

                Case StringConstants.TempKindLock
                    kind = TempKind.Lock
                Case StringConstants.TempKindUsing
                    kind = TempKind.Using
                Case StringConstants.TempKindForEachEnumerator
                    kind = TempKind.ForEachEnumerator
                Case StringConstants.TempKindForEachArray
                    kind = TempKind.ForEachArray
                Case StringConstants.TempKindForEachArrayIndex
                    kind = TempKind.ForEachArrayIndex
                Case StringConstants.TempKindLockTaken
                    kind = TempKind.LockTaken
                Case StringConstants.TempKindWith
                    kind = TempKind.With

                Case StringConstants.OnErrorActiveHandler
                    kind = TempKind.OnErrorActiveHandler
                Case StringConstants.OnErrorResumeTarget
                    kind = TempKind.OnErrorResumeTarget
                Case StringConstants.OnErrorCurrentStatement
                    kind = TempKind.OnErrorCurrentStatement
                Case StringConstants.OnErrorCurrentLine
                    kind = TempKind.OnErrorCurrentLine
                Case StringConstants.StateMachineCachedState
                    kind = TempKind.StateMachineCachedState
                Case StringConstants.StateMachineExceptionLocalName
                    kind = TempKind.StateMachineException
                Case StringConstants.StateMachineReturnValueLocalName
                    kind = TempKind.StateMachineReturnValue

                Case StringConstants.ForLimit
                    kind = TempKind.ForLimit
                Case StringConstants.ForStep
                    kind = TempKind.ForStep
                Case StringConstants.ForLoopObject
                    kind = TempKind.ForLoopObject
                Case StringConstants.ForDirection
                    kind = TempKind.ForDirection

                Case Else
                    kind = TempKind.None
                    Return False
            End Select
            Return True
        End Function
    End Class

End Namespace