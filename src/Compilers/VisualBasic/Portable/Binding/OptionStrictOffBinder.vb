' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class Binder

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

