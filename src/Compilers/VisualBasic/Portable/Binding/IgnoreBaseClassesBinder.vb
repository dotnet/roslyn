' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
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
    ''' Causes lookups to ignore base classes. Used for binding
    ''' Imports statements.
    ''' </summary>
    Friend NotInheritable Class IgnoreBaseClassesBinder
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder, ignoreBaseClassesInLookup:=True)
        End Sub
    End Class

End Namespace
