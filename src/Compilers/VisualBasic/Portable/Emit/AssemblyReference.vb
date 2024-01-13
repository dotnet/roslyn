' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class AssemblyReference
        Implements Cci.IAssemblyReference

        Private ReadOnly _targetAssembly As AssemblySymbol

        Public Sub New(assemblySymbol As AssemblySymbol)
            Debug.Assert(assemblySymbol IsNot Nothing)
            _targetAssembly = assemblySymbol
        End Sub

        Public ReadOnly Property Identity As AssemblyIdentity Implements Cci.IAssemblyReference.Identity
            Get
                Return _targetAssembly.Identity
            End Get
        End Property

        Public ReadOnly Property AssemblyVersionPattern As Version Implements Cci.IAssemblyReference.AssemblyVersionPattern
            Get
                Return _targetAssembly.AssemblyVersionPattern
            End Get
        End Property

        Private Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) Implements Cci.IReference.Dispatch
            visitor.Visit(DirectCast(Me, Cci.IAssemblyReference))
        End Sub

        Private ReadOnly Property INamedEntityName As String Implements Cci.INamedEntity.Name
            Get
                Return Identity.Name
            End Get
        End Property

        Private Function IModuleReferenceGetContainingAssembly(context As EmitContext) As Cci.IAssemblyReference Implements Cci.IModuleReference.GetContainingAssembly
            Return Me
        End Function

        Public Overrides Function ToString() As String
            Return _targetAssembly.ToString()
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
