' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class VisualBasicEESymbolProvider
        Inherits EESymbolProvider(Of TypeSymbol, LocalSymbol)

        Private ReadOnly _metadataDecoder As MetadataDecoder
        Private ReadOnly _method As PEMethodSymbol

        Public Sub New([module] As PEModuleSymbol, method As PEMethodSymbol)
            _metadataDecoder = New MetadataDecoder([module], method)
            _method = method
        End Sub

        Public Overrides Function GetLocalVariable(name As String, slotIndex As Integer, info As LocalInfo(Of TypeSymbol), dynamicFlagsOpt As ImmutableArray(Of Boolean)) As LocalSymbol
            ' ignore dynamic flags (in theory the CDI might be present in bad PDB)

            ' Custom modifiers can be dropped since binding ignores custom
            ' modifiers from locals and since we only need to preserve
            ' the type of the original local in the generated method.
            Dim kind = If(name = _method.Name, LocalDeclarationKind.FunctionValue, LocalDeclarationKind.Variable)
            Return New EELocalSymbol(_method, EELocalSymbol.NoLocations, name, slotIndex, kind, info.Type, info.IsByRef, info.IsPinned, canScheduleToStack:=False)
        End Function

        Public Overrides Function GetLocalConstant(name As String, type As TypeSymbol, value As ConstantValue, dynamicFlagsOpt As ImmutableArray(Of Boolean)) As LocalSymbol
            ' ignore dynamic flags (in theory the CDI might be present in bad PDB)
            Return New EELocalConstantSymbol(_method, name, type, value)
        End Function

        ''' <exception cref="BadImageFormatException"></exception>
        ''' <exception cref="UnsupportedSignatureContent"></exception>
        Public Overrides Function DecodeLocalVariableType(signature As ImmutableArray(Of Byte)) As TypeSymbol
            Return _metadataDecoder.DecodeLocalVariableTypeOrThrow(signature)
        End Function

        Public Overrides Function GetTypeSymbolForSerializedType(typeName As String) As TypeSymbol
            Return _metadataDecoder.GetTypeSymbolForSerializedType(typeName)
        End Function

        ''' <exception cref="BadImageFormatException"></exception>
        ''' <exception cref="UnsupportedSignatureContent"></exception>
        Public Overrides Sub DecodeLocalConstant(ByRef reader As BlobReader, ByRef type As TypeSymbol, ByRef value As ConstantValue)
            _metadataDecoder.DecodeLocalConstantBlobOrThrow(reader, type, value)
        End Sub

        ''' <exception cref="BadImageFormatException"></exception>
        Public Overrides Function GetReferencedAssembly(handle As AssemblyReferenceHandle) As IAssemblySymbol
            Dim index As Integer = _metadataDecoder.Module.GetAssemblyReferenceIndexOrThrow(handle)
            Return _metadataDecoder.ModuleSymbol.GetReferencedAssemblySymbols()(index)
        End Function

        ''' <exception cref="UnsupportedSignatureContent"></exception>
        Public Overrides Function [GetType](handle As EntityHandle) As TypeSymbol
            Dim isNoPiaLocalType As Boolean
            Return _metadataDecoder.GetSymbolForTypeHandleOrThrow(handle, isNoPiaLocalType, allowTypeSpec:=True, requireShortForm:=False)
        End Function

    End Class
End Namespace
