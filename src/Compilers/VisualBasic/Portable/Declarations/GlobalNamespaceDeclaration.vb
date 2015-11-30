﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents global namespace. Namespace's name is always empty
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class GlobalNamespaceDeclaration
        Inherits SingleNamespaceDeclaration

        Public Sub New(hasImports As Boolean,
                       syntaxReference As SyntaxReference,
                       nameLocation As Location,
                       children As ImmutableArray(Of SingleNamespaceOrTypeDeclaration))
            MyBase.New(String.Empty, hasImports, syntaxReference, nameLocation, children)
        End Sub

        Public Overrides ReadOnly Property IsGlobalNamespace As Boolean
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace
