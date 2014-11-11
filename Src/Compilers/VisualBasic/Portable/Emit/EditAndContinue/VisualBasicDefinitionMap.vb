' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
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
        Inherits DefinitionMap(Of VisualBasicSymbolMatcher)

        Private ReadOnly metadataDecoder As MetadataDecoder

        Public Sub New([module] As PEModule,
                       edits As IEnumerable(Of SemanticEdit),
                       metadataDecoder As MetadataDecoder,
                       mapToMetadata As VisualBasicSymbolMatcher,
                       mapToPrevious As VisualBasicSymbolMatcher)

            MyBase.New([module], edits, mapToMetadata, mapToPrevious)

            Debug.Assert(metadataDecoder IsNot Nothing)
            Me.metadataDecoder = metadataDecoder
        End Sub

        Friend Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out> ByRef name As String, <Out> ByRef index As Integer) As Boolean
            Return Me.mapToPrevious.TryGetAnonymousTypeName(template, name, index)
        End Function

        Friend Overrides Function TryGetTypeHandle(def As Cci.ITypeDefinition, <Out> ByRef handle As TypeDefinitionHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PENamedTypeSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetEventHandle(def As Cci.IEventDefinition, <Out> ByRef handle As EventDefinitionHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEEventSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetFieldHandle(def As Cci.IFieldDefinition, <Out> ByRef handle As FieldDefinitionHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEFieldSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetMethodHandle(def As Cci.IMethodDefinition, <Out> ByRef handle As MethodDefinitionHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEMethodSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Private Overloads Function TryGetMethodHandle(baseline As EmitBaseline, def As Cci.IMethodDefinition, <Out> ByRef handle As MethodDefinitionHandle) As Boolean
            If Me.TryGetMethodHandle(def, handle) Then
                Return True
            End If

            def = DirectCast(Me.mapToPrevious.MapDefinition(def), Cci.IMethodDefinition)
            If def IsNot Nothing Then
                Dim methodIndex As UInteger = 0
                If baseline.MethodsAdded.TryGetValue(def, methodIndex) Then
                    handle = MetadataTokens.MethodDefinitionHandle(CInt(methodIndex))
                    Return True
                End If
            End If

            handle = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetPropertyHandle(def As Cci.IPropertyDefinition, <Out> ByRef handle As PropertyDefinitionHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEPropertySymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Protected Overrides Function TryGetStateMachineType(methodHandle As Handle) As ITypeSymbol
            Dim typeName As String = Nothing
            If metadataDecoder.Module.HasStringValuedAttribute(methodHandle, AttributeDescription.AsyncStateMachineAttribute, typeName) OrElse
               metadataDecoder.Module.HasStringValuedAttribute(methodHandle, AttributeDescription.IteratorStateMachineAttribute, typeName) Then

                Return metadataDecoder.GetTypeSymbolForSerializedType(typeName)
            End If

            Return Nothing
        End Function

        Protected Overrides Sub GetStateMachineFieldMapFromMetadata(stateMachineType As ITypeSymbol,
                                                                    localSlotDebugInfo As ImmutableArray(Of LocalSlotDebugInfo),
                                                                    <Out> ByRef hoistedLocalMap As IReadOnlyDictionary(Of EncHoistedLocalInfo, Integer),
                                                                    <Out> ByRef awaiterMap As IReadOnlyDictionary(Of ITypeReference, Integer),
                                                                    <Out> ByRef awaiterSlotCount As Integer)
            ' TODO:
            hoistedLocalMap = New Dictionary(Of EncHoistedLocalInfo, Integer)()
            awaiterMap = New Dictionary(Of ITypeReference, Integer)
            awaiterSlotCount = 0
        End Sub

        Protected Overrides Function TryGetLocalSlotMapFromMetadata(handle As MethodDefinitionHandle, debugInfo As EditAndContinueMethodDebugInformation) As ImmutableArray(Of EncLocalInfo)
            Dim slotMetadata As ImmutableArray(Of MetadataDecoder.LocalInfo) = Nothing
            If Not metadataDecoder.TryGetLocals(handle, slotMetadata) Then
                Return Nothing
            End If

            Dim result = CreateLocalSlotMap(debugInfo, slotMetadata)
            Debug.Assert(result.Length = slotMetadata.Length)
            Return result
        End Function

        ''' <summary>
        ''' Match local declarations to names to generate a map from
        ''' declaration to local slot. The names are indexed by slot And the
        ''' assumption Is that declarations are in the same order as slots.
        ''' </summary>
        Private Shared Function CreateLocalSlotMap(
            methodEncInfo As EditAndContinueMethodDebugInformation,
            slotMetadata As ImmutableArray(Of MetadataDecoder.LocalInfo)) As ImmutableArray(Of EncLocalInfo)

            Dim result(slotMetadata.Length - 1) As EncLocalInfo

            Dim localSlots = methodEncInfo.LocalSlots
            If Not localSlots.IsDefault Then

                ' In case of corrupted PDB or metadata, these lengths might Not match.
                ' Let's guard against such case.
                Dim slotCount = Math.Min(localSlots.Length, slotMetadata.Length)

                Dim map = New Dictionary(Of EncLocalInfo, Integer)()

                For slotIndex = 0 To slotCount - 1

                    Dim slot = localSlots(slotIndex)
                    If slot.SynthesizedKind.IsLongLived() Then
                        Dim metadata = slotMetadata(slotIndex)

                        ' We do Not emit custom modifiers on locals so ignore the
                        ' previous version of the local if it had custom modifiers.
                        If metadata.CustomModifiers.IsDefaultOrEmpty Then
                            Dim local = New EncLocalInfo(slot, DirectCast(metadata.Type, Cci.ITypeReference), metadata.Constraints, metadata.SignatureOpt)
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