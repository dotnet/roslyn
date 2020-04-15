﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousDelegateParameterSymbol
            Inherits SynthesizedParameterSymbol

            Public ReadOnly CorrespondingInvokeParameter As Integer

            Public Sub New(
                container As SynthesizedDelegateMethodSymbol,
                type As TypeSymbol, ordinal As Integer,
                isByRef As Boolean,
                name As String,
                Optional correspondingInvokeParameter As Integer = -1
            )
                MyBase.New(container, type, ordinal, isByRef, name)
                Me.CorrespondingInvokeParameter = correspondingInvokeParameter
            End Sub

            Public Overrides ReadOnly Property MetadataName As String
                Get
                    If CorrespondingInvokeParameter <> -1 Then
                        Return DirectCast(_container.ContainingSymbol, AnonymousDelegateTemplateSymbol).GetAdjustedName(CorrespondingInvokeParameter)
                    End If

                    Return MyBase.MetadataName
                End Get
            End Property
        End Class

    End Class
End Namespace
