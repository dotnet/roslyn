' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Class SynthesizedStaticLocalBackingField
        Implements IContextualNamedEntity

        Private m_MetadataWriter As MetadataWriter
        Private m_NameToEmit As String

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Debug.Assert(m_NameToEmit IsNot Nothing)
                Return m_NameToEmit
            End Get
        End Property

        Friend Overrides ReadOnly Property IFieldReferenceIsContextualNamedEntity As Boolean
            Get
                Return True
            End Get
        End Property

        Private Sub AssociateWithMetadataWriter(metadataWriter As MetadataWriter) Implements IContextualNamedEntity.AssociateWithMetadataWriter

            Interlocked.CompareExchange(m_MetadataWriter, metadataWriter, Nothing)
            Debug.Assert(metadataWriter Is m_MetadataWriter)

            If m_NameToEmit Is Nothing Then
                Dim declaringMethod = DirectCast(Me.ImplicitlyDefinedBy.ContainingSymbol, MethodSymbol)
                Dim signature = GeneratedNames.MakeSignatureString(metadataWriter.GetMethodSignature(declaringMethod))
                m_NameToEmit = GeneratedNames.MakeStaticLocalFieldName(declaringMethod.Name, signature, Name)
            End If
        End Sub

    End Class
End Namespace
