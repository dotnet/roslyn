' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Helper class to generate synthesized names.
    ''' </summary>
    Friend NotInheritable Class GeneratedNames
        Private Const GenerationSeparator As Char = CommonGeneratedNames.GenerationSeparator

        ''' <summary>
        ''' Generates the name of a state machine's type.
        ''' </summary>
        Public Shared Function MakeStateMachineTypeName(methodName As String, methodOrdinal As Integer, generation As Integer) As String
            Debug.Assert(methodOrdinal >= -1)
            Return MakeMethodScopedSynthesizedName(GeneratedNameConstants.StateMachineTypeNamePrefix, methodOrdinal, generation, methodName, isTypeName:=True)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine 'state' field 
        ''' </summary>
        Public Shared Function MakeStateMachineStateFieldName() As String
            Return GeneratedNameConstants.StateMachineStateFieldName
        End Function

        Public Shared Function MakeBaseMethodWrapperName(methodName As String, isMyBase As Boolean) As String
            Return GeneratedNameConstants.BaseMethodWrapperNamePrefix & methodName & If(isMyBase, "_MyBase", "_MyClass")
        End Function

        Public Shared Function ReusableHoistedLocalFieldName(number As Integer) As String
            Return GeneratedNameConstants.ReusableHoistedLocalFieldName & StringExtensions.GetNumeral(number)
        End Function

        Public Shared Function MakeStaticLambdaDisplayClassName(methodOrdinal As Integer, generation As Integer) As String
            Debug.Assert(methodOrdinal >= -1)
            Debug.Assert(generation >= 0)

            Return MakeMethodScopedSynthesizedName(GeneratedNameConstants.DisplayClassPrefix, methodOrdinal, generation)
        End Function

        Friend Shared Function MakeLambdaDisplayClassName(methodOrdinal As Integer, generation As Integer, closureOrdinal As Integer, closureGeneration As Integer, isDelegateRelaxation As Boolean) As String
            Debug.Assert(closureOrdinal >= 0)
            Debug.Assert(methodOrdinal >= 0)
            Debug.Assert(generation >= 0)

            Dim prefix = If(isDelegateRelaxation, GeneratedNameConstants.DelegateRelaxationDisplayClassPrefix, GeneratedNameConstants.DisplayClassPrefix)
            Return MakeMethodScopedSynthesizedName(prefix, methodOrdinal, generation, entityOrdinal:=closureOrdinal, entityGeneration:=closureGeneration, isTypeName:=True)
        End Function

        Friend Shared Function MakeDisplayClassGenericParameterName(parameterIndex As Integer) As String
            Return GeneratedNameConstants.DisplayClassGenericParameterNamePrefix & StringExtensions.GetNumeral(parameterIndex)
        End Function

        Friend Shared Function MakeLambdaMethodName(methodOrdinal As Integer, generation As Integer, lambdaOrdinal As Integer, lambdaGeneration As Integer, lambdaKind As SynthesizedLambdaKind) As String
            Debug.Assert(methodOrdinal >= -1)
            Debug.Assert(lambdaOrdinal >= 0)

            Dim prefix = If(lambdaKind = SynthesizedLambdaKind.DelegateRelaxationStub,
                            GeneratedNameConstants.DelegateRelaxationMethodNamePrefix,
                            GeneratedNameConstants.LambdaMethodNamePrefix)

            Return MakeMethodScopedSynthesizedName(prefix, methodOrdinal, generation, entityOrdinal:=lambdaOrdinal, entityGeneration:=lambdaGeneration)
        End Function

        ''' <summary>
        ''' Generates the name of a static lambda display class instance cache
        ''' </summary>
        Public Shared Function MakeCachedFrameInstanceName() As String
            Return GeneratedNameConstants.LambdaCacheFieldPrefix
        End Function

        Friend Shared Function MakeLambdaCacheFieldName(methodOrdinal As Integer, generation As Integer, lambdaOrdinal As Integer, lambdaGeneration As Integer, lambdaKind As SynthesizedLambdaKind) As String
            Debug.Assert(methodOrdinal >= -1)
            Debug.Assert(lambdaOrdinal >= 0)

            Dim prefix = If(lambdaKind = SynthesizedLambdaKind.DelegateRelaxationStub,
                            GeneratedNameConstants.DelegateRelaxationCacheFieldPrefix,
                            GeneratedNameConstants.LambdaCacheFieldPrefix)

            Return MakeMethodScopedSynthesizedName(prefix, methodOrdinal, generation, entityOrdinal:=lambdaOrdinal, entityGeneration:=lambdaGeneration)
        End Function

        Friend Shared Function MakeDelegateRelaxationParameterName(parameterIndex As Integer) As String
            Return GeneratedNameConstants.DelegateStubParameterPrefix & StringExtensions.GetNumeral(parameterIndex)
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
                    builder.Append(GenerationSeparator)
                    builder.Append(methodGeneration)
                End If
            End If

            If entityOrdinal >= 0 Then
                If methodOrdinal >= 0 Then
                    ' Can't use underscore since name parser uses it to find the method name.
                    builder.Append(GeneratedNameConstants.IdSeparator)
                End If

                builder.Append(entityOrdinal)

                If entityGeneration > 0 Then
                    builder.Append(GenerationSeparator)
                    builder.Append(entityGeneration)
                End If
            End If

            If methodNameOpt IsNot Nothing Then
                builder.Append(GeneratedNameConstants.MethodNameSeparator)
                builder.Append(methodNameOpt)

                ' CLR generally allows names with dots, however some APIs like IMetaDataImport
                ' can only return full type names combined with namespaces. 
                ' see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                ' When working with such APIs, names with dots become ambiguous since metadata 
                ' consumer cannot figure where namespace ends and actual type name starts.
                ' Therefore it is a good practice to avoid type names with dots.
                If isTypeName Then
                    builder.Replace("."c, GeneratedNameConstants.DotReplacementInTypeNames)
                End If
            End If

            Return result.ToStringAndFree()
        End Function

        ''' <summary>
        ''' Generates the name of a state machine 'builder' field 
        ''' </summary>
        Public Shared Function MakeStateMachineBuilderFieldName() As String
            Return GeneratedNameConstants.StateMachineBuilderFieldName
        End Function

        ''' <summary>
        ''' Generates the name of a field that backs Current property
        ''' </summary>
        Public Shared Function MakeIteratorCurrentFieldName() As String
            Return GeneratedNameConstants.IteratorCurrentFieldName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's awaiter field 
        ''' </summary>
        Public Shared Function MakeStateMachineAwaiterFieldName(index As Integer) As String
            Return GeneratedNameConstants.StateMachineAwaiterFieldPrefix & StringExtensions.GetNumeral(index)
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's parameter name
        ''' </summary>
        Public Shared Function MakeStateMachineParameterName(paramName As String) As String
            Return GeneratedNameConstants.HoistedUserVariablePrefix & paramName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine's parameter name
        ''' </summary>
        Public Shared Function MakeIteratorParameterProxyName(paramName As String) As String
            Return GeneratedNameConstants.IteratorParameterProxyPrefix & paramName
        End Function

        ''' <summary>
        ''' Generates the name of a field where initial thread ID is stored
        ''' </summary>
        Public Shared Function MakeIteratorInitialThreadIdName() As String
            Return GeneratedNameConstants.IteratorInitialThreadIdName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference
        ''' </summary>
        Public Shared Function MakeStateMachineCapturedMeName() As String
            Return GeneratedNameConstants.HoistedMeName
        End Function

        ''' <summary>
        ''' Generates the name of a state machine field name for captured me reference of lambda closure
        ''' </summary>
        Public Shared Function MakeStateMachineCapturedClosureMeName(closureName As String) As String
            Return GeneratedNameConstants.HoistedSpecialVariablePrefix & closureName
        End Function

        Friend Shared Function MakeAnonymousTypeTemplateName(prefix As String, index As Integer, submissionSlotIndex As Integer, moduleId As String) As String
            Return If(submissionSlotIndex >= 0,
                           String.Format("{0}{1}_{2}{3}", prefix, submissionSlotIndex, index, moduleId),
                           String.Format("{0}{1}{2}", prefix, index, moduleId))
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
                    name = GeneratedNameConstants.HoistedWithLocalPrefix & StringExtensions.GetNumeral(uniqueId)
                    uniqueId += 1

                Case Else
                    name = Nothing
            End Select

            Return name
        End Function

        Friend Shared Function MakeLambdaDisplayClassStorageName(uniqueId As Integer) As String
            Return GeneratedNameConstants.ClosureVariablePrefix & StringExtensions.GetNumeral(uniqueId)
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

            Return String.Format(GeneratedNameConstants.StaticLocalFieldNamePrefix & "{0}${1}${2}", methodName, methodSignature, localName)
        End Function
    End Class
End Namespace
