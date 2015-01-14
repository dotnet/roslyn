' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class ModuleReference
        Implements Cci.IModuleReference
        Implements Cci.IFileReference

        Private ReadOnly m_ModuleBeingBuilt As PEModuleBuilder
        Private ReadOnly m_UnderlyingModule As ModuleSymbol

        Friend Sub New(moduleBeingBuilt As PEModuleBuilder, underlyingModule As ModuleSymbol)
            Debug.Assert(moduleBeingBuilt IsNot Nothing)
            Debug.Assert(underlyingModule IsNot Nothing)
            Me.m_ModuleBeingBuilt = moduleBeingBuilt
            Me.m_UnderlyingModule = underlyingModule
        End Sub

        Private Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) Implements Cci.IReference.Dispatch
            visitor.Visit(DirectCast(Me, Cci.IModuleReference))
        End Sub

        Private ReadOnly Property INamedEntityName As String Implements Cci.INamedEntity.Name
            Get
                Return m_UnderlyingModule.Name
            End Get
        End Property

        Private ReadOnly Property IFileReferenceHasMetadata As Boolean Implements Cci.IFileReference.HasMetadata
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IFileReferenceFileName As String Implements Cci.IFileReference.FileName
            Get
                Return m_UnderlyingModule.Name
            End Get
        End Property

        Private Function IFileReferenceGetHashValue(algorithmId As AssemblyHashAlgorithm) As ImmutableArray(Of Byte) Implements Cci.IFileReference.GetHashValue
            Return m_UnderlyingModule.GetHash(algorithmId)
        End Function

        Private Function IModuleReferenceGetContainingAssembly(context As EmitContext) As Cci.IAssemblyReference Implements Cci.IModuleReference.GetContainingAssembly
            If m_ModuleBeingBuilt.OutputKind.IsNetModule() AndAlso
                m_ModuleBeingBuilt.SourceModule.ContainingAssembly Is m_UnderlyingModule.ContainingAssembly Then
                Return Nothing
            End If

            Return m_ModuleBeingBuilt.Translate(m_UnderlyingModule.ContainingAssembly, context.Diagnostics)
        End Function

        Public Overrides Function ToString() As String
            Return m_UnderlyingModule.ToString()
        End Function

        Private Function IReferenceAttributes(context As EmitContext) As IEnumerable(Of Cci.ICustomAttribute) Implements Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Cci.ICustomAttribute)()
        End Function

        Private Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition Implements Cci.IReference.AsDefinition
            Return Nothing
        End Function
    End Class
End Namespace
