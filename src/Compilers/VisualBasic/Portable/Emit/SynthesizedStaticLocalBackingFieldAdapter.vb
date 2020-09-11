' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend Class SynthesizedStaticLocalBackingField
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
