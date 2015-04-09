' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Partial Class SynthesizedStaticLocalBackingField
        Implements IContextualNamedEntity

        Private _metadataWriter As MetadataWriter
        Private _nameToEmit As String

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Debug.Assert(_nameToEmit IsNot Nothing)
                Return _nameToEmit
            End Get
        End Property

        Friend Overrides ReadOnly Property IFieldReferenceIsContextualNamedEntity As Boolean
            Get
                Return True
            End Get
        End Property

        Private Sub AssociateWithMetadataWriter(metadataWriter As MetadataWriter) Implements IContextualNamedEntity.AssociateWithMetadataWriter

            Interlocked.CompareExchange(_metadataWriter, metadataWriter, Nothing)
            Debug.Assert(metadataWriter Is _metadataWriter)

            If _nameToEmit Is Nothing Then
                Dim declaringMethod = DirectCast(Me.ImplicitlyDefinedBy.ContainingSymbol, MethodSymbol)
                Dim signature = GeneratedNames.MakeSignatureString(metadataWriter.GetMethodSignature(declaringMethod))
                _nameToEmit = GeneratedNames.MakeStaticLocalFieldName(declaringMethod.Name, signature, Name)
            End If
        End Sub

    End Class
End Namespace
