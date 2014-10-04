' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit
    ''' <summary>
    ''' Matches symbols from an assembly in one compilation to
    ''' the corresponding assembly in another. Assumes that only
    ''' one assembly has changed between the two compilations.
    ''' </summary>
    Friend NotInheritable Class VisualBasicDefinitionMap
        Inherits DefinitionMap

        Private ReadOnly metadataDecoder As MetadataDecoder
        Private ReadOnly mapToMetadata As VisualBasicSymbolMatcher
        Private ReadOnly mapToPrevious As VisualBasicSymbolMatcher

        Public Sub New([module] As PEModule,
                       edits As IEnumerable(Of SemanticEdit),
                       metadataDecoder As MetadataDecoder,
                       mapToMetadata As VisualBasicSymbolMatcher,
                       mapToPrevious As VisualBasicSymbolMatcher)

            MyBase.New([module], edits)

            Debug.Assert(metadataDecoder IsNot Nothing)
            Debug.Assert(mapToMetadata IsNot Nothing)

            Me.metadataDecoder = metadataDecoder
            Me.mapToMetadata = mapToMetadata
            Me.mapToPrevious = If(mapToPrevious, mapToMetadata)
        End Sub

        Friend Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out> ByRef name As String, <Out> ByRef index As Integer) As Boolean
            Return Me.mapToPrevious.TryGetAnonymousTypeName(template, name, index)
        End Function

        Friend Overrides Function TryGetTypeHandle(def As Cci.ITypeDefinition, <Out> ByRef handle As TypeHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PENamedTypeSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetEventHandle(def As Cci.IEventDefinition, <Out> ByRef handle As EventHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEEventSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetFieldHandle(def As Cci.IFieldDefinition, <Out> ByRef handle As FieldHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEFieldSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetMethodHandle(def As Cci.IMethodDefinition, <Out> ByRef handle As MethodHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEMethodSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Private Overloads Function TryGetMethodHandle(baseline As EmitBaseline, def As Cci.IMethodDefinition, <Out> ByRef handle As MethodHandle) As Boolean
            If Me.TryGetMethodHandle(def, handle) Then
                Return True
            End If

            def = DirectCast(Me.mapToPrevious.MapDefinition(def), Cci.IMethodDefinition)
            If def IsNot Nothing Then
                Dim methodIndex As UInteger = 0
                If baseline.MethodsAdded.TryGetValue(def, methodIndex) Then
                    handle = MetadataTokens.MethodHandle(CInt(methodIndex))
                    Return True
                End If
            End If

            handle = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetPropertyHandle(def As Cci.IPropertyDefinition, <Out> ByRef handle As PropertyHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEPropertySymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function DefinitionExists(def As Cci.IDefinition) As Boolean
            Dim previous = Me.mapToPrevious.MapDefinition(def)
            Return previous IsNot Nothing
        End Function

        Friend Overrides Function TryCreateVariableSlotAllocator(baseline As EmitBaseline, method As IMethodSymbol) As VariableSlotAllocator
            Dim handle As MethodHandle = Nothing
            If Not Me.TryGetMethodHandle(baseline, CType(method, Cci.IMethodDefinition), handle) Then
                ' Unrecognized method. Must have been added in the current compilation.
                Return Nothing
            End If

            Dim methodEntry As MethodDefinitionEntry = Nothing
            If Not Me.methodMap.TryGetValue(method, methodEntry) Then
                ' Not part of changeset. No need to preserve locals.
                Return Nothing
            End If

            If Not methodEntry.PreserveLocalVariables Then
                ' Not necessary to preserve locals.
                Return Nothing
            End If

            Dim symbolMap As VisualBasicSymbolMatcher
            Dim previousLocals As ImmutableArray(Of EncLocalInfo) = Nothing

            Dim methodIndex As UInteger = CUInt(MetadataTokens.GetRowNumber(handle))

            ' Check if method has changed previously. If so, we already have a map.
            If baseline.LocalsForMethodsAddedOrChanged.TryGetValue(methodIndex, previousLocals) Then
                symbolMap = Me.mapToPrevious
            Else
                ' Method has not changed since initial generation. Generate a map
                ' using the local names provided with the initial metadata.
                Dim slotMetadata As ImmutableArray(Of MetadataDecoder.LocalInfo) = Nothing

                If Not metadataDecoder.TryGetLocals(handle, slotMetadata) Then
                    ' TODO: Report error that metadata Is Not supported.
                    Return Nothing
                End If

                Dim debugInfo = baseline.DebugInformationProvider(handle)

                previousLocals = CreateLocalSlotMap(debugInfo, slotMetadata)
                Debug.Assert(previousLocals.Length = slotMetadata.Length)

                symbolMap = Me.mapToMetadata
            End If

            Return New EncVariableSlotAllocator(symbolMap, methodEntry.SyntaxMap, methodEntry.PreviousMethod, previousLocals)
        End Function

        ''' <summary>
        ''' Match local declarations to names to generate a map from
        ''' declaration to local slot. The names are indexed by slot And the
        ''' assumption Is that declarations are in the same order as slots.
        ''' </summary>
        Public Shared Function CreateLocalSlotMap(
            methodEncInfo As EditAndContinueMethodDebugInformation,
            slotMetadata As ImmutableArray(Of MetadataDecoder.LocalInfo)) As ImmutableArray(Of EncLocalInfo)

            Dim result(slotMetadata.Length - 1) As EncLocalInfo

            Dim localSlots = methodEncInfo.LocalSlots
            If Not localSlots.IsDefault Then

                ' In case of corrupted PDB Or metadata, these lengths might Not match.
                ' Let's guard against such case.
                Dim slotCount = Math.Min(localSlots.Length, slotMetadata.Length)

                Dim map = New Dictionary(Of EncLocalInfo, Integer)()

                For slotIndex = 0 To slotCount - 1

                    Dim slot As ValueTuple(Of SynthesizedLocalKind, LocalDebugId) = localSlots(slotIndex)
                    If slot.Item1.IsLongLived() Then
                        Dim metadata = slotMetadata(slotIndex)

                        ' We do Not emit custom modifiers on locals so ignore the
                        ' previous version of the local if it had custom modifiers.
                        If metadata.CustomModifiers.IsDefaultOrEmpty Then
                            Dim local = New EncLocalInfo(slot.Item2, DirectCast(metadata.Type, Cci.ITypeReference), metadata.Constraints, slot.Item1, metadata.SignatureOpt)
                            map.Add(local, slotIndex)
                        End If
                    End If
                Next

                For Each pair In map
                    result(pair.Value) = pair.Key
                Next
            End If

            ' Populate any remaining locals that were Not matched to source.
            For i = 0 To result.Count - 1
                If result(i).IsDefault Then
                    result(i) = New EncLocalInfo(slotMetadata(i).SignatureOpt)
                End If
            Next

            Return ImmutableArray.Create(result)
        End Function

    End Class
End Namespace