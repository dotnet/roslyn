' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class AssemblyReference
        Implements Microsoft.Cci.IAssemblyReference

        ' Assembly identity used in metadata to refer to the target assembly.
        ' NOTE: this could be different from assemblySymbol.AssemblyName due to mapping.
        ' For example, multiple assembly symbols might be emitted into a single dynamic assembly whose identity is stored here.
        Public ReadOnly MetadataIdentity As AssemblyIdentity

        Private ReadOnly m_targetAssembly As AssemblySymbol

        Public Sub New(assemblySymbol As AssemblySymbol, symbolMapper As Func(Of AssemblySymbol, AssemblyIdentity))
            Debug.Assert(assemblySymbol IsNot Nothing)
            MetadataIdentity = If(symbolMapper IsNot Nothing, symbolMapper(assemblySymbol), assemblySymbol.Identity)
            m_targetAssembly = assemblySymbol
        End Sub

        Private Sub IReferenceDispatch(visitor As Microsoft.Cci.MetadataVisitor) Implements Microsoft.Cci.IReference.Dispatch
            visitor.Visit(DirectCast(Me, Microsoft.Cci.IAssemblyReference))
        End Sub

        Private ReadOnly Property IAssemblyReferenceCulture As String Implements Microsoft.Cci.IAssemblyReference.Culture
            Get
                Return MetadataIdentity.CultureName
            End Get
        End Property

        Private ReadOnly Property IAssemblyReferenceIsRetargetable As Boolean Implements Microsoft.Cci.IAssemblyReference.IsRetargetable
            Get
                Return MetadataIdentity.IsRetargetable
            End Get
        End Property

        Private ReadOnly Property IAssemblyReferenceContentType As System.Reflection.AssemblyContentType Implements Microsoft.Cci.IAssemblyReference.ContentType
            Get
                Return MetadataIdentity.ContentType
            End Get
        End Property

        Private ReadOnly Property IAssemblyReferencePublicKeyToken As IEnumerable(Of Byte) Implements Microsoft.Cci.IAssemblyReference.PublicKeyToken
            Get
                Return MetadataIdentity.PublicKeyToken
            End Get
        End Property

        Private ReadOnly Property IAssemblyReferenceVersion As Version Implements Microsoft.Cci.IAssemblyReference.Version
            Get
                Return MetadataIdentity.Version
            End Get
        End Property

        Private ReadOnly Property INamedEntityName As String Implements Microsoft.Cci.INamedEntity.Name
            Get
                Return MetadataIdentity.Name
            End Get
        End Property

        Private Function IModuleReferenceGetContainingAssembly(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IAssemblyReference Implements Microsoft.Cci.IModuleReference.GetContainingAssembly
            Return Me
        End Function

        Public Overrides Function ToString() As String
            Return m_targetAssembly.ToString()
        End Function

        Private Function IReferenceAttributes(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Microsoft.Cci.ICustomAttribute) Implements Microsoft.Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Microsoft.Cci.ICustomAttribute)()
        End Function

        Private Function IReferenceAsDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IDefinition Implements Microsoft.Cci.IReference.AsDefinition
            Return Nothing
        End Function
    End Class
End Namespace
