' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

                Dim builder = PooledStringBuilder.GetInstance()

                ' Munge the name of the field using name and metadata signature of the function,
                ' in which corresponding static local was defined, so the debugger so the debugger can find it.

                builder.Builder.Append("$STATIC$")
                builder.Builder.Append(declaringMethod.Name)
                builder.Builder.Append("$"c)

                For Each b As Byte In metadataWriter.GetMethodSignature(declaringMethod)
                    builder.Builder.Append(String.Format("{0:X}", b))
                Next

                builder.Builder.Append("$"c)
                builder.Builder.Append(Me.Name)

                m_NameToEmit = builder.ToStringAndFree()
            End If
        End Sub

    End Class
End Namespace
