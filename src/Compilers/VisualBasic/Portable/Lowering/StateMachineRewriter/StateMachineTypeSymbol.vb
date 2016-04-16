' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class StateMachineTypeSymbol
        Inherits SynthesizedContainer
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private _attributes As ImmutableArray(Of VisualBasicAttributeData)
        Public ReadOnly KickoffMethod As MethodSymbol

        Public Sub New(slotAllocatorOpt As VariableSlotAllocator,
                       compilationState As TypeCompilationState,
                       kickoffMethod As MethodSymbol,
                       kickoffMethodOrdinal As Integer,
                       baseType As NamedTypeSymbol,
                       originalInterfaces As ImmutableArray(Of NamedTypeSymbol))
            MyBase.New(kickoffMethod, MakeName(slotAllocatorOpt, compilationState, kickoffMethod, kickoffMethodOrdinal), baseType, originalInterfaces)

            Debug.Assert(kickoffMethod IsNot Nothing)

            ' If the async method is partial the kickoff method should be the method with body (implementation)
            Debug.Assert(kickoffMethod.IsDefinition)
            Debug.Assert(Not kickoffMethod.IsPartial OrElse kickoffMethod.PartialImplementationPart Is Nothing)

            Me.KickoffMethod = kickoffMethod
        End Sub

        Private Shared Function MakeName(slotAllocatorOpt As VariableSlotAllocator, compilationState As TypeCompilationState, kickoffMethod As MethodSymbol, kickoffMethodOrdinal As Integer) As String
            Return If(slotAllocatorOpt?.PreviousStateMachineTypeName,
                      GeneratedNames.MakeStateMachineTypeName(kickoffMethod.Name,
                                                              kickoffMethodOrdinal,
                                                              If(compilationState.ModuleBuilderOpt?.CurrentGenerationOrdinal, 0))) ' Note: module builder is not available only when testing emit diagnostics
        End Function

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                ' This method contains user code from the async method
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return KickoffMethod
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _attributes.IsDefault Then
                Debug.Assert(MyBase.GetAttributes().Length = 0)

                Dim builder As ArrayBuilder(Of VisualBasicAttributeData) = Nothing

                ' Inherit some attributes from the container of the kickoff method
                Dim kickoffType = KickoffMethod.ContainingType
                For Each attribute In kickoffType.GetAttributes()
                    If attribute.IsTargetAttribute(kickoffType, AttributeDescription.DebuggerNonUserCodeAttribute) OrElse
                       attribute.IsTargetAttribute(kickoffType, AttributeDescription.DebuggerStepThroughAttribute) Then
                        If builder Is Nothing Then
                            builder = ArrayBuilder(Of VisualBasicAttributeData).GetInstance(2) ' only 2 different attributes are inherited at the moment
                        End If

                        builder.Add(attribute)
                    End If
                Next

                ImmutableInterlocked.InterlockedCompareExchange(_attributes,
                                                                If(builder Is Nothing, ImmutableArray(Of VisualBasicAttributeData).Empty, builder.ToImmutableAndFree()),
                                                                Nothing)
            End If

            Return _attributes
        End Function
    End Class
End Namespace
