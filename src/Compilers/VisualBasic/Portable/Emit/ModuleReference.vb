' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class ModuleReference
        Implements Cci.IModuleReference
        Implements Cci.IFileReference

        Private ReadOnly _moduleBeingBuilt As PEModuleBuilder
        Private ReadOnly _underlyingModule As ModuleSymbol

        Friend Sub New(moduleBeingBuilt As PEModuleBuilder, underlyingModule As ModuleSymbol)
            Debug.Assert(moduleBeingBuilt IsNot Nothing)
            Debug.Assert(underlyingModule IsNot Nothing)
            Me._moduleBeingBuilt = moduleBeingBuilt
            Me._underlyingModule = underlyingModule
        End Sub

        Private Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) Implements Cci.IReference.Dispatch
            visitor.Visit(DirectCast(Me, Cci.IModuleReference))
        End Sub

        Private ReadOnly Property INamedEntityName As String Implements Cci.INamedEntity.Name
            Get
                Return _underlyingModule.Name
            End Get
        End Property

        Private ReadOnly Property IFileReferenceHasMetadata As Boolean Implements Cci.IFileReference.HasMetadata
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IFileReferenceFileName As String Implements Cci.IFileReference.FileName
            Get
                Return _underlyingModule.Name
            End Get
        End Property

        Private Function IFileReferenceGetHashValue(algorithmId As AssemblyHashAlgorithm) As ImmutableArray(Of Byte) Implements Cci.IFileReference.GetHashValue
            Return _underlyingModule.GetHash(algorithmId)
        End Function

        Private Function IModuleReferenceGetContainingAssembly(context As EmitContext) As Cci.IAssemblyReference Implements Cci.IModuleReference.GetContainingAssembly
            If _moduleBeingBuilt.OutputKind.IsNetModule() AndAlso
                _moduleBeingBuilt.SourceModule.ContainingAssembly Is _underlyingModule.ContainingAssembly Then
                Return Nothing
            End If

            Return _moduleBeingBuilt.Translate(_underlyingModule.ContainingAssembly, context.Diagnostics)
        End Function

        Public Overrides Function ToString() As String
            Return _underlyingModule.ToString()
        End Function

        Private Function IReferenceAttributes(context As EmitContext) As IEnumerable(Of Cci.ICustomAttribute) Implements Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Cci.ICustomAttribute)()
        End Function

        Private Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition Implements Cci.IReference.AsDefinition
            Return Nothing
        End Function

        Private Function IReferenceGetInternalSymbol() As CodeAnalysis.Symbols.ISymbolInternal Implements Cci.IReference.GetInternalSymbol
            Return Nothing
        End Function
    End Class
End Namespace
