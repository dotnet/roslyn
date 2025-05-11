' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Symbols
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

        Private ReadOnly _metadataDecoder As MetadataDecoder
        Private ReadOnly _previousSourceToMetadata As VisualBasicSymbolMatcher
        Private ReadOnly _sourceToMetadata As VisualBasicSymbolMatcher
        Private ReadOnly _sourceToPrevious As VisualBasicSymbolMatcher

        Public Sub New(edits As IEnumerable(Of SemanticEdit),
                       metadataDecoder As MetadataDecoder,
                       previousSourceToMetadata As VisualBasicSymbolMatcher,
                       sourceToMetadata As VisualBasicSymbolMatcher,
                       sourceToPreviousSource As VisualBasicSymbolMatcher,
                       baseline As EmitBaseline)

            MyBase.New(edits, baseline)

            Debug.Assert(metadataDecoder IsNot Nothing)
            Debug.Assert(sourceToMetadata IsNot Nothing)

            _metadataDecoder = metadataDecoder
            _previousSourceToMetadata = previousSourceToMetadata
            _sourceToMetadata = sourceToMetadata
            _sourceToPrevious = If(sourceToPreviousSource, sourceToMetadata)
        End Sub

        Public Overrides ReadOnly Property SourceToMetadataSymbolMatcher As SymbolMatcher
            Get
                Return _sourceToMetadata
            End Get
        End Property

        Public Overrides ReadOnly Property SourceToPreviousSymbolMatcher As SymbolMatcher
            Get
                Return _sourceToPrevious
            End Get
        End Property

        Public Overrides ReadOnly Property PreviousSourceToMetadataSymbolMatcher As SymbolMatcher
            Get
                Return _previousSourceToMetadata
            End Get
        End Property

        Protected Overrides Function GetISymbolInternalOrNull(symbol As ISymbol) As ISymbolInternal
            Return TryCast(symbol, Symbol)
        End Function

        Friend Overrides ReadOnly Property MessageProvider As CommonMessageProvider
            Get
                Return VisualBasic.MessageProvider.Instance
            End Get
        End Property

        Protected Overrides Function GetLambdaSyntaxFacts() As LambdaSyntaxFacts
            Return VisualBasicLambdaSyntaxFacts.Instance
        End Function

        Private Shared Function IsParentDisplayClassFieldName(name As String) As Boolean
            Return name.StartsWith(GeneratedNameConstants.HoistedSpecialVariablePrefix & GeneratedNameConstants.ClosureVariablePrefix, StringComparison.Ordinal)
        End Function

        Friend Function TryGetAnonymousTypeName(template As AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol, <Out> ByRef name As String, <Out> ByRef index As Integer) As Boolean
            Return _sourceToPrevious.TryGetAnonymousTypeName(template, name, index)
        End Function

        Protected Overrides Function TryGetStateMachineType(methodHandle As MethodDefinitionHandle) As ITypeSymbolInternal
            Dim typeName As String = Nothing
            If _metadataDecoder.Module.HasStateMachineAttribute(methodHandle, typeName) Then
                Return _metadataDecoder.GetTypeSymbolForSerializedType(typeName)
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetMethodSymbol(methodHandle As MethodDefinitionHandle) As IMethodSymbolInternal
            Return DirectCast(_metadataDecoder.GetSymbolForILToken(methodHandle), IMethodSymbolInternal)
        End Function

        Protected Overrides Sub GetStateMachineFieldMapFromMetadata(stateMachineType As ITypeSymbolInternal,
                                                                    localSlotDebugInfo As ImmutableArray(Of LocalSlotDebugInfo),
                                                                    <Out> ByRef hoistedLocalMap As IReadOnlyDictionary(Of EncHoistedLocalInfo, Integer),
                                                                    <Out> ByRef awaiterMap As IReadOnlyDictionary(Of Cci.ITypeReference, Integer),
                                                                    <Out> ByRef awaiterSlotCount As Integer)
            ' we are working with PE symbols
            Debug.Assert(TypeOf stateMachineType.ContainingAssembly Is PEAssemblySymbol)

            Dim hoistedLocals = New Dictionary(Of EncHoistedLocalInfo, Integer)()
            Dim awaiters = New Dictionary(Of Cci.ITypeReference, Integer)(DirectCast(Cci.SymbolEquivalentEqualityComparer.Instance, IEqualityComparer(Of Cci.IReference)))
            Dim maxAwaiterSlotIndex = -1

            For Each member In DirectCast(stateMachineType, TypeSymbol).GetMembers()
                If member.Kind = SymbolKind.Field Then
                    Dim name = member.Name
                    Dim slotIndex As Integer

                    Select Case GeneratedNameParser.GetKind(name)
                        Case GeneratedNameKind.StateMachineAwaiterField

                            If GeneratedNameParser.TryParseSlotIndex(GeneratedNameConstants.StateMachineAwaiterFieldPrefix, name, slotIndex) Then
                                Dim field = DirectCast(member, FieldSymbol)

                                ' Correct metadata won't contain duplicates, but malformed might, ignore the duplicate:
                                awaiters(DirectCast(field.Type.GetCciAdapter(), Cci.ITypeReference)) = slotIndex

                                If slotIndex > maxAwaiterSlotIndex Then
                                    maxAwaiterSlotIndex = slotIndex
                                End If
                            End If

                        Case GeneratedNameKind.HoistedSynthesizedLocalField,
                             GeneratedNameKind.HoistedWithLocalPrefix,
                             GeneratedNameKind.StateMachineHoistedUserVariableOrDisplayClassField

                            Dim variableName As String = Nothing
                            If GeneratedNameParser.TryParseSlotIndex(GeneratedNameConstants.HoistedSynthesizedLocalPrefix, name, slotIndex) OrElse
                               GeneratedNameParser.TryParseSlotIndex(GeneratedNameConstants.HoistedWithLocalPrefix, name, slotIndex) OrElse
                               GeneratedNameParser.TryParseStateMachineHoistedUserVariableOrDisplayClassName(name, variableName, slotIndex) Then
                                Dim field = DirectCast(member, FieldSymbol)
                                If slotIndex >= localSlotDebugInfo.Length Then
                                    ' Invalid metadata
                                    Continue For
                                End If

                                Dim key = New EncHoistedLocalInfo(localSlotDebugInfo(slotIndex), DirectCast(field.Type.GetCciAdapter(), Cci.ITypeReference))

                                ' Correct metadata won't contain duplicates, but malformed might, ignore the duplicate:
                                hoistedLocals(key) = slotIndex
                            End If
                    End Select
                End If
            Next

            hoistedLocalMap = hoistedLocals
            awaiterMap = awaiters
            awaiterSlotCount = maxAwaiterSlotIndex + 1
        End Sub

        Protected Overrides Function GetLocalSlotMapFromMetadata(handle As StandaloneSignatureHandle, debugInfo As EditAndContinueMethodDebugInformation) As ImmutableArray(Of EncLocalInfo)
            Debug.Assert(Not handle.IsNil)

            Dim localInfos = _metadataDecoder.GetLocalsOrThrow(handle)
            Dim result = CreateLocalSlotMap(debugInfo, localInfos)
            Debug.Assert(result.Length = localInfos.Length)
            Return result
        End Function

        ''' <summary>
        ''' Match local declarations to names to generate a map from
        ''' declaration to local slot. The names are indexed by slot And the
        ''' assumption Is that declarations are in the same order as slots.
        ''' </summary>
        Private Shared Function CreateLocalSlotMap(
            methodEncInfo As EditAndContinueMethodDebugInformation,
            slotMetadata As ImmutableArray(Of LocalInfo(Of TypeSymbol))) As ImmutableArray(Of EncLocalInfo)

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
                            Dim local = New EncLocalInfo(slot, DirectCast(metadata.Type.GetCciAdapter(), Cci.ITypeReference), metadata.Constraints, metadata.SignatureOpt)
                            map.Add(local, slotIndex)
                        End If
                    End If
                Next

                For Each pair In map
                    result(pair.Value) = pair.Key
                Next
            End If

            ' Populate any remaining locals that were Not matched to source.
            For i = 0 To result.Length - 1
                If result(i).IsDefault Then
                    result(i) = New EncLocalInfo(slotMetadata(i).SignatureOpt)
                End If
            Next

            Return ImmutableArray.Create(result)
        End Function

        Protected Overrides Function TryParseDisplayClassOrLambdaName(
            name As String,
            <Out> ByRef suffixIndex As Integer,
            <Out> ByRef idSeparator As Char,
            <Out> ByRef isDisplayClass As Boolean,
            <Out> ByRef isDisplayClassParentField As Boolean,
            <Out> ByRef hasDebugIds As Boolean) As Boolean

            idSeparator = GeneratedNameConstants.IdSeparator

            isDisplayClass = name.StartsWith(GeneratedNameConstants.DisplayClassPrefix, StringComparison.Ordinal)
            If isDisplayClass Then
                suffixIndex = GeneratedNameConstants.DisplayClassPrefix.Length
                isDisplayClassParentField = False
                hasDebugIds = name.Length > suffixIndex
                Return True
            End If

            If name.StartsWith(GeneratedNameConstants.LambdaMethodNamePrefix, StringComparison.Ordinal) Then
                suffixIndex = GeneratedNameConstants.LambdaMethodNamePrefix.Length
                isDisplayClassParentField = False
                hasDebugIds = name.Length > suffixIndex
                Return True
            End If

            If IsParentDisplayClassFieldName(name) Then
                suffixIndex = -1
                isDisplayClassParentField = True
                hasDebugIds = False
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
