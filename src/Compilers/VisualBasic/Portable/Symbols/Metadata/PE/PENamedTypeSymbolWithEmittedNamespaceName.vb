' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent top level types imported from a PE/module.
    ''' </summary>
    Friend NotInheritable Class PENamedTypeSymbolWithEmittedNamespaceName
        Inherits PENamedTypeSymbol

        Private ReadOnly _emittedNamespaceName As String

        Private ReadOnly _corTypeId As SpecialType

        Friend Sub New(
            moduleSymbol As PEModuleSymbol,
            containingNamespace As PENamespaceSymbol,
            typeDef As TypeDefinitionHandle,
            emittedNamespaceName As String
        )
            MyBase.New(moduleSymbol, containingNamespace, typeDef)

            Debug.Assert(emittedNamespaceName IsNot Nothing)
            Debug.Assert(emittedNamespaceName.Length > 0)
            _emittedNamespaceName = emittedNamespaceName

            ' check if this is one of the COR library types
            If (Arity = 0 OrElse MangleName) AndAlso (moduleSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes) AndAlso Me.DeclaredAccessibility = Accessibility.Public Then
                Debug.Assert(emittedNamespaceName.Length > 0)
                _corTypeId = SpecialTypes.GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(emittedNamespaceName, MetadataName))
            Else
                _corTypeId = SpecialType.None
            End If
        End Sub

        Public Overrides ReadOnly Property SpecialType As SpecialType
            Get
                Return _corTypeId
            End Get
        End Property

        Friend Overrides Function GetEmittedNamespaceName() As String
            Return _emittedNamespaceName
        End Function

    End Class

End Namespace
