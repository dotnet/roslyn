' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend Class SynthesizedStaticLocalBackingFieldAdapter
        Inherits FieldSymbolAdapter
#Else
    Partial Friend Class SynthesizedStaticLocalBackingField
#End If
        Implements IContextualNamedEntity

        Private Sub IContextualNamedEntity_AssociateWithMetadataWriter(metadataWriter As MetadataWriter) Implements IContextualNamedEntity.AssociateWithMetadataWriter
            DirectCast(AdaptedFieldSymbol, SynthesizedStaticLocalBackingField).AssociateWithMetadataWriter(metadataWriter)
        End Sub
    End Class

    Partial Friend Class SynthesizedStaticLocalBackingField
        Private _metadataWriter As MetadataWriter
        Private _nameToEmit As String

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Debug.Assert(_nameToEmit IsNot Nothing)
                Return _nameToEmit
            End Get
        End Property

        Friend Overrides ReadOnly Property IsContextualNamedEntity As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Sub AssociateWithMetadataWriter(metadataWriter As MetadataWriter)

            Interlocked.CompareExchange(_metadataWriter, metadataWriter, Nothing)
            Debug.Assert(metadataWriter Is _metadataWriter)

            If _nameToEmit Is Nothing Then
                Dim declaringMethod = DirectCast(Me.ImplicitlyDefinedBy.ContainingSymbol, MethodSymbol)
                Dim signature = GeneratedNames.MakeSignatureString(metadataWriter.GetMethodSignature(declaringMethod.GetCciAdapter()))
                _nameToEmit = GeneratedNames.MakeStaticLocalFieldName(declaringMethod.Name, signature, Name)
            End If
        End Sub
    End Class

#If DEBUG Then
    Partial Friend Class SynthesizedStaticLocalBackingFieldAdapter

        Friend Sub New(underlyingFieldSymbol As SynthesizedStaticLocalBackingField)
            MyBase.New(underlyingFieldSymbol)
        End Sub
    End Class
#End If
End Namespace
