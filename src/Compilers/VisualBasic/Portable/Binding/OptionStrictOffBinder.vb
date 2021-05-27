' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder

        ''' <summary>
        ''' Makes it look like Option Strict is Off, all other operations
        ''' are delegated up the chain.
        ''' </summary>
        Private Class OptionStrictOffBinder
            Inherits Binder

            Public Sub New(containingBinder As Binder)
                MyBase.New(containingBinder)
            End Sub

            Public Overrides ReadOnly Property OptionStrict As OptionStrict
                Get
                    Return OptionStrict.Off
                End Get
            End Property
        End Class

    End Class

End Namespace

