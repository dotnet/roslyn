' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic

' Required by Microsoft.CodeAnalysis.VisualBasic.Syntax.KeywordTable
Namespace Microsoft.CodeAnalysis.Text

End Namespace

Namespace Microsoft.CodeAnalysis

    Friend Module GreenNode
        ''' <summary>
        ''' Required by <see cref="SyntaxKind"/>.
        ''' </summary>
        Public Const ListKind As Integer = 1
    End Module

End Namespace

' This needs to be re-defined here to avoid ambiguity, because we allow this project to target .NET 4.0 on machines without 2.0 installed.
Namespace System.Runtime.CompilerServices

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=False)>
    Friend Class ExtensionAttribute : Inherits Attribute
    End Class

End Namespace
