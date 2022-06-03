' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A simple Binder that wraps another Binder and reports a specific
    ''' binding location, but otherwise delegates to the other Binder.
    ''' </summary>
    Friend NotInheritable Class LocationSpecificBinder
        Inherits Binder

        Private ReadOnly _location As BindingLocation
        Private ReadOnly _owner As Symbol

        Public Sub New(location As BindingLocation, containingBinder As Binder)
            MyClass.New(location, Nothing, containingBinder)
        End Sub

        Public Sub New(location As BindingLocation, owner As Symbol, containingBinder As Binder)
            MyBase.New(containingBinder)
            _location = location
            _owner = owner
        End Sub

        Public Overrides ReadOnly Property BindingLocation As BindingLocation
            Get
                Return _location
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return If(_owner, MyBase.ContainingMember)
            End Get
        End Property

        Public Overrides ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Return ImmutableArray(Of Symbol).Empty
            End Get
        End Property
    End Class

End Namespace
