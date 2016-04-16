' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Roslyn.Utilities
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class EEAssemblyBuilder
        Inherits PEAssemblyBuilderBase

        Private ReadOnly _methods As ImmutableArray(Of MethodSymbol)

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

            _methods = methods

            If testData IsNot Nothing Then
                SetMethodTestData(testData.Methods)
                testData.Module = Me
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

        Public Overrides ReadOnly Property CurrentGenerationOrdinal As Integer
            Get
                Return 0
            End Get
        End Property

        Friend Overrides Function TryCreateVariableSlotAllocator(symbol As MethodSymbol, topLevelMethod As MethodSymbol) As VariableSlotAllocator
            Dim method = TryCast(symbol, EEMethodSymbol)
            If method IsNot Nothing AndAlso _methods.Contains(method) Then
                Dim defs = GetLocalDefinitions(method.Locals)
                Return New SlotAllocator(defs)
            End If

            Debug.Assert(Not _methods.Contains(symbol))
            Return Nothing
        End Function

        Private Shared Function GetLocalDefinitions(locals As ImmutableArray(Of LocalSymbol)) As ImmutableArray(Of LocalDefinition)
            Dim builder = ArrayBuilder(Of LocalDefinition).GetInstance()
            For Each local In locals
                Select Case local.DeclarationKind
                    Case LocalDeclarationKind.Constant, LocalDeclarationKind.Static
                        Continue For
                    Case Else
                        Dim def = ToLocalDefinition(local, builder.Count)
                        Debug.Assert(DirectCast(local, EELocalSymbol).Ordinal = def.SlotIndex)
                        builder.Add(def)
                End Select
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function ToLocalDefinition(local As LocalSymbol, index As Integer) As LocalDefinition
            Dim constraints = If(local.IsPinned, LocalSlotConstraints.Pinned, LocalSlotConstraints.None) Or
                If(local.IsByRef, LocalSlotConstraints.ByRef, LocalSlotConstraints.None)
            Return New LocalDefinition(
                local,
                local.Name,
                DirectCast(local.Type, ITypeReference),
                slot:=index,
                synthesizedKind:=CType(local.SynthesizedKind, SynthesizedLocalKind),
                id:=Nothing,
                pdbAttributes:=Cci.PdbWriter.DefaultLocalAttributesValue,
                constraints:=constraints,
                isDynamic:=False,
                dynamicTransformFlags:=ImmutableArray(Of TypedConstant).Empty)
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
                pdbAttributes As UInteger,
                constraints As LocalSlotConstraints,
                isDynamic As Boolean,
                dynamicTransformFlags As ImmutableArray(Of TypedConstant)) As LocalDefinition

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

            Public Overrides Function TryGetPreviousClosure(scopeSyntax As SyntaxNode, <Out> ByRef closureId As DebugId) As Boolean
                closureId = Nothing
                Return False
            End Function

            Public Overrides Function TryGetPreviousLambda(lambdaOrLambdaBodySyntax As SyntaxNode, isLambdaBody As Boolean, <Out> ByRef lambdaId As DebugId) As Boolean
                lambdaId = Nothing
                Return False
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
