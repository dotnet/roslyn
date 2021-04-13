' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class IteratorStateMachine
        Inherits StateMachineTypeSymbol

        Private ReadOnly _constructor As SynthesizedSimpleConstructorSymbol
        Private ReadOnly _iteratorMethod As MethodSymbol

        Protected Friend Sub New(slotAllocatorOpt As VariableSlotAllocator,
                                 compilationState As TypeCompilationState,
                                 iteratorMethod As MethodSymbol,
                                 iteratorMethodOrdinal As Integer,
                                 valueTypeSymbol As TypeSymbol,
                                 isEnumerable As Boolean)

            MyBase.New(slotAllocatorOpt,
                       compilationState,
                       iteratorMethod,
                       iteratorMethodOrdinal,
                       iteratorMethod.ContainingAssembly.GetSpecialType(SpecialType.System_Object),
                       GetIteratorInterfaces(valueTypeSymbol,
                                             isEnumerable,
                                             iteratorMethod.ContainingAssembly))

            Dim intType = DeclaringCompilation.GetSpecialType(SpecialType.System_Int32)

            Me._constructor = New SynthesizedSimpleConstructorSymbol(Me)
            Dim parameters = ImmutableArray.Create(Of ParameterSymbol)(
                New SynthesizedParameterSymbol(Me._constructor, intType, 0, False, GeneratedNames.MakeStateMachineStateFieldName()))

            Me._constructor.SetParameters(parameters)
            Me._iteratorMethod = iteratorMethod
        End Sub

        Private Shared Function GetIteratorInterfaces(elementType As TypeSymbol,
                                                      isEnumerable As Boolean,
                                                      containingAssembly As AssemblySymbol) As ImmutableArray(Of NamedTypeSymbol)

            Dim interfaces = ArrayBuilder(Of NamedTypeSymbol).GetInstance()

            If isEnumerable Then
                interfaces.Add(containingAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(elementType))
                interfaces.Add(containingAssembly.GetSpecialType(SpecialType.System_Collections_IEnumerable))
            End If

            interfaces.Add(containingAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T).Construct(elementType))
            interfaces.Add(containingAssembly.GetSpecialType(SpecialType.System_IDisposable))
            interfaces.Add(containingAssembly.GetSpecialType(SpecialType.System_Collections_IEnumerator))

            Return interfaces.ToImmutableAndFree()
        End Function

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Class
            End Get
        End Property

        Protected Friend Overrides ReadOnly Property Constructor As MethodSymbol
            Get
                Return Me._constructor
            End Get
        End Property
    End Class

End Namespace
