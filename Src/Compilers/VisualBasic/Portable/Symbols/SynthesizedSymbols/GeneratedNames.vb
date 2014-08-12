' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
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

        Friend Shared Function GenerateTempName(kind As SynthesizedLocalKind) As String
            Select Case kind

                Case SynthesizedLocalKind.LoweringTemp,
                     SynthesizedLocalKind.None
                    Return Nothing

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
                Case SynthesizedLocalKind.With
                    Return StringConstants.SynthesizedLocalKindWith

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
                Case SynthesizedLocalKind.StateMachineException
                    Return StringConstants.StateMachineExceptionLocalName
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

        Private Const SynthesizedLocalNamePrefix As String = "VB$"

        Friend Shared Function GenerateTempName(kind As SynthesizedLocalKind, type As TypeSymbol, index As Integer) As String
            Select Case kind
                Case SynthesizedLocalKind.XmlInExpressionLambda
                    Return SynthesizedLocalNamePrefix & type.GetNativeCompilerVType() & "$L" & index

                Case SynthesizedLocalKind.LambdaDisplayClass
                    'TODO: VB10 adds line/column numbers in hex here. Not sure if that is important or always meaningful.
                    Return StringConstants.ClosureVariablePrefix & index
            End Select

            Throw ExceptionUtilities.UnexpectedValue(kind)
        End Function

        Friend Shared Function TryParseLocalName(name As String, ByRef kind As SynthesizedLocalKind, ByRef uniqueId As Integer) As Boolean

            'TODO: are we using this for anything?
            uniqueId = 0

            Select Case name

                Case StringConstants.SynthesizedLocalKindLock
                    kind = SynthesizedLocalKind.Lock
                Case StringConstants.SynthesizedLocalKindUsing
                    kind = SynthesizedLocalKind.Using
                Case StringConstants.SynthesizedLocalKindForEachEnumerator
                    kind = SynthesizedLocalKind.ForEachEnumerator
                Case StringConstants.SynthesizedLocalKindForEachArray
                    kind = SynthesizedLocalKind.ForEachArray
                Case StringConstants.SynthesizedLocalKindForEachArrayIndex
                    kind = SynthesizedLocalKind.ForEachArrayIndex
                Case StringConstants.SynthesizedLocalKindLockTaken
                    kind = SynthesizedLocalKind.LockTaken
                Case StringConstants.SynthesizedLocalKindWith
                    kind = SynthesizedLocalKind.With

                Case StringConstants.OnErrorActiveHandler
                    kind = SynthesizedLocalKind.OnErrorActiveHandler
                Case StringConstants.OnErrorResumeTarget
                    kind = SynthesizedLocalKind.OnErrorResumeTarget
                Case StringConstants.OnErrorCurrentStatement
                    kind = SynthesizedLocalKind.OnErrorCurrentStatement
                Case StringConstants.OnErrorCurrentLine
                    kind = SynthesizedLocalKind.OnErrorCurrentLine
                Case StringConstants.StateMachineCachedState
                    kind = SynthesizedLocalKind.StateMachineCachedState
                Case StringConstants.StateMachineExceptionLocalName
                    kind = SynthesizedLocalKind.StateMachineException
                Case StringConstants.StateMachineReturnValueLocalName
                    kind = SynthesizedLocalKind.StateMachineReturnValue

                Case StringConstants.ForLimit
                    kind = SynthesizedLocalKind.ForLimit
                Case StringConstants.ForStep
                    kind = SynthesizedLocalKind.ForStep
                Case StringConstants.ForLoopObject
                    kind = SynthesizedLocalKind.ForLoopObject
                Case StringConstants.ForDirection
                    kind = SynthesizedLocalKind.ForDirection

                Case Else

                    If name.StartsWith(StringConstants.ClosureVariablePrefix, StringComparison.Ordinal) Then
                        kind = SynthesizedLocalKind.LambdaDisplayClass
                        Return True
                    End If

                    kind = SynthesizedLocalKind.None
                    Return False
            End Select
            Return True
        End Function
    End Class

End Namespace