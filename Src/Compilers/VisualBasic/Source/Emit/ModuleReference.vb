' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class ModuleReference
        Implements Microsoft.Cci.IModuleReference
        Implements Microsoft.Cci.IFileReference

        Private ReadOnly m_ModuleBeingBuilt As PEModuleBuilder
        Private ReadOnly m_UnderlyingModule As ModuleSymbol

        Friend Sub New(moduleBeingBuilt As PEModuleBuilder, underlyingModule As ModuleSymbol)
            Debug.Assert(moduleBeingBuilt IsNot Nothing)
            Debug.Assert(underlyingModule IsNot Nothing)
            Me.m_ModuleBeingBuilt = moduleBeingBuilt
            Me.m_UnderlyingModule = underlyingModule
        End Sub

        Private Sub IReferenceDispatch(visitor As Microsoft.Cci.MetadataVisitor) Implements Microsoft.Cci.IReference.Dispatch
            visitor.Visit(DirectCast(Me, Microsoft.Cci.IModuleReference))
        End Sub

        Private ReadOnly Property INamedEntityName As String Implements Microsoft.Cci.INamedEntity.Name
            Get
                Return m_UnderlyingModule.Name
            End Get
        End Property

        Private ReadOnly Property IFileReferenceHasMetadata As Boolean Implements Microsoft.Cci.IFileReference.HasMetadata
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IFileReferenceFileName As String Implements Microsoft.Cci.IFileReference.FileName
            Get
                Return m_UnderlyingModule.Name
            End Get
        End Property

        Private Function IFileReferenceGetHashValue(algorithmId As AssemblyHashAlgorithm) As ImmutableArray(Of Byte) Implements Microsoft.Cci.IFileReference.GetHashValue
            Return m_UnderlyingModule.GetHash(algorithmId)
        End Function

        Private Function IModuleReferenceGetContainingAssembly(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IAssemblyReference Implements Microsoft.Cci.IModuleReference.GetContainingAssembly
            If m_ModuleBeingBuilt.OutputKind.IsNetModule() AndAlso
                m_ModuleBeingBuilt.SourceModule.ContainingAssembly Is m_UnderlyingModule.ContainingAssembly Then
                Return Nothing
            End If

            Return m_ModuleBeingBuilt.Translate(m_UnderlyingModule.ContainingAssembly, context.Diagnostics)
        End Function

        Public Overrides Function ToString() As String
            Return m_UnderlyingModule.ToString()
        End Function

        Private Function IReferenceAttributes(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Microsoft.Cci.ICustomAttribute) Implements Microsoft.Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Microsoft.Cci.ICustomAttribute)()
        End Function

        Private Function IReferenceAsDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IDefinition Implements Microsoft.Cci.IReference.AsDefinition
            Return Nothing
        End Function
    End Class
End Namespace
