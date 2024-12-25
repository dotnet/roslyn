' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class EEAssemblyBuilder
        Inherits PEAssemblyBuilderBase

        Friend ReadOnly Methods As ImmutableArray(Of MethodSymbol)

        Friend Sub New(
            sourceAssembly As SourceAssemblySymbol,
            emitOptions As EmitOptions,
            methods As ImmutableArray(Of MethodSymbol),
            serializationProperties As ModulePropertiesForSerialization,
            additionalTypes As ImmutableArray(Of NamedTypeSymbol),
            testData As CompilationTestData)

            MyBase.New(
                sourceAssembly,
                emitOptions,
                outputKind:=OutputKind.DynamicallyLinkedLibrary,
                serializationProperties:=serializationProperties,
                manifestResources:=SpecializedCollections.EmptyEnumerable(Of ResourceDescription)(),
                additionalTypes:=additionalTypes)

            Me.Methods = methods

            If testData IsNot Nothing Then
                SetTestData(testData)
            End If
        End Sub

        Protected Overrides Function TranslateModule(symbol As ModuleSymbol, diagnostics As DiagnosticBag) As IModuleReference
            Dim moduleSymbol = TryCast(symbol, PEModuleSymbol)
            If moduleSymbol IsNot Nothing Then
                Dim [module] = moduleSymbol.Module
                ' Expose the individual runtime Windows.*.winmd modules as assemblies.
                ' (The modules were wrapped in a placeholder Windows.winmd assembly
                ' in MetadataUtilities.MakeAssemblyReferences.)
                If MetadataUtilities.IsWindowsComponent([module].MetadataReader, [module].Name) AndAlso
                    MetadataUtilities.IsWindowsAssemblyName(moduleSymbol.ContainingAssembly.Name) Then
                    Dim identity = [module].ReadAssemblyIdentityOrThrow()
                    Return New Microsoft.CodeAnalysis.ExpressionEvaluator.AssemblyReference(identity)
                End If
            End If
            Return MyBase.TranslateModule(symbol, diagnostics)
        End Function

        Friend Overrides ReadOnly Property IgnoreAccessibility As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property EncSymbolChanges As SymbolChanges
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property PreviousGeneration As EmitBaseline
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function TryGetOrCreateSynthesizedHotReloadExceptionType() As INamedTypeSymbolInternal
            Return Nothing
        End Function

        Public Overrides Function GetOrCreateHotReloadExceptionConstructorDefinition() As IMethodSymbolInternal
            ' Should only be called when compiling EnC delta.
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function GetUsedSynthesizedHotReloadExceptionType() As INamedTypeSymbolInternal
            Return Nothing
        End Function

        Friend Overrides Function TryCreateVariableSlotAllocator(symbol As MethodSymbol, topLevelMethod As MethodSymbol, diagnostics As DiagnosticBag) As VariableSlotAllocator
            Dim method = TryCast(symbol, EEMethodSymbol)
            If method IsNot Nothing Then
                Dim defs = GetLocalDefinitions(method.Locals, diagnostics)
                Return New SlotAllocator(defs)
            End If
            Return Nothing
        End Function

        Private Function GetLocalDefinitions(locals As ImmutableArray(Of LocalSymbol), diagnostics As DiagnosticBag) As ImmutableArray(Of LocalDefinition)
            Dim builder = ArrayBuilder(Of LocalDefinition).GetInstance()
            For Each local In locals
                Select Case local.DeclarationKind
                    Case LocalDeclarationKind.Constant, LocalDeclarationKind.Static
                        Continue For
                    Case Else
                        Dim def = ToLocalDefinition(local, builder.Count, diagnostics)
                        Debug.Assert(DirectCast(local, EELocalSymbol).Ordinal = def.SlotIndex)
                        builder.Add(def)
                End Select
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Private Function ToLocalDefinition(local As LocalSymbol, index As Integer, diagnostics As DiagnosticBag) As LocalDefinition
            Dim constraints = If(local.IsPinned, LocalSlotConstraints.Pinned, LocalSlotConstraints.None) Or
                If(local.IsByRef, LocalSlotConstraints.ByRef, LocalSlotConstraints.None)
            Return New LocalDefinition(
                local,
                local.Name,
                Translate(local.Type, syntaxNodeOpt:=Nothing, diagnostics),
                slot:=index,
                synthesizedKind:=local.SynthesizedKind,
                id:=Nothing,
                pdbAttributes:=LocalVariableAttributes.None,
                constraints:=constraints,
                dynamicTransformFlags:=ImmutableArray(Of Boolean).Empty,
                tupleElementNames:=ImmutableArray(Of String).Empty)
        End Function

        Friend Overrides ReadOnly Property AllowOmissionOfConditionalCalls As Boolean
            Get
                Return False
            End Get
        End Property

        Private NotInheritable Class SlotAllocator
            Inherits VariableSlotAllocator

            Private ReadOnly _locals As ImmutableArray(Of LocalDefinition)

            Friend Sub New(locals As ImmutableArray(Of LocalDefinition))
                _locals = locals
            End Sub

            Public Overrides Sub AddPreviousLocals(builder As ArrayBuilder(Of ILocalDefinition))
                builder.AddRange(_locals)
            End Sub

            Public Overrides Function GetPreviousLocal(
                type As ITypeReference,
                symbol As ILocalSymbolInternal,
                nameOpt As String,
                synthesizedKind As SynthesizedLocalKind,
                id As LocalDebugId,
                pdbAttributes As LocalVariableAttributes,
                constraints As LocalSlotConstraints,
                dynamicTransformFlags As ImmutableArray(Of Boolean),
                tupleElementNames As ImmutableArray(Of String)) As LocalDefinition

                Dim local = TryCast(symbol, EELocalSymbol)
                If local Is Nothing Then
                    Return Nothing
                End If
                Return _locals(local.Ordinal)
            End Function

            Public Overrides ReadOnly Property PreviousHoistedLocalSlotCount As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Overrides ReadOnly Property PreviousAwaiterSlotCount As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Overrides Function TryGetPreviousHoistedLocalSlotIndex(currentDeclarator As SyntaxNode, currentType As ITypeReference, synthesizedKind As SynthesizedLocalKind, currentId As LocalDebugId, diagnostics As DiagnosticBag, <Out> ByRef slotIndex As Integer) As Boolean
                slotIndex = -1
                Return False
            End Function

            Public Overrides Function TryGetPreviousAwaiterSlotIndex(currentType As ITypeReference, diagnostics As DiagnosticBag, <Out> ByRef slotIndex As Integer) As Boolean
                slotIndex = -1
                Return False
            End Function

            Public Overrides Function TryGetPreviousClosure(closureSyntax As SyntaxNode, parentClosureId As DebugId?, structCaptures As ImmutableArray(Of String), ByRef closureId As DebugId, ByRef runtimeRudeEdit As RuntimeRudeEdit?) As Boolean
                closureId = Nothing
                runtimeRudeEdit = Nothing
                Return False
            End Function

            Public Overrides Function TryGetPreviousLambda(lambdaOrLambdaBodySyntax As SyntaxNode, isLambdaBody As Boolean, closureOrdinal As Integer, structClosureIds As ImmutableArray(Of DebugId), ByRef lambdaId As DebugId, ByRef runtimeRudeEdit As RuntimeRudeEdit?) As Boolean
                lambdaId = Nothing
                runtimeRudeEdit = Nothing
                Return False
            End Function

            Public Overrides Function TryGetPreviousStateMachineState(syntax As SyntaxNode, awaitId As AwaitDebugId, ByRef state As StateMachineState) As Boolean
                state = 0
                Return False
            End Function

            Public Overrides Function GetFirstUnusedStateMachineState(increasing As Boolean) As StateMachineState?
                Return Nothing
            End Function

            Public Overrides ReadOnly Property PreviousStateMachineTypeName As String
                Get
                    Return Nothing
                End Get
            End Property

            Public Overrides ReadOnly Property MethodId As DebugId?
                Get
                    Return Nothing
                End Get
            End Property
        End Class
    End Class
End Namespace
