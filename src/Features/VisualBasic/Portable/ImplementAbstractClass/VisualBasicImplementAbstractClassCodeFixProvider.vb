' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.CodeFixes
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes.ImplementAbstractClass

Namespace Microsoft.CodeAnalysis.VisualBasic.ImplementAbstractClass
    <ExportCodeFixProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeFixProviderNames.ImplementAbstractClass), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateType)>
    Friend Class VisualBasicImplementAbstractClassCodeFixProvider
        Inherits AbstractImplementAbstractClassCodeFixProvider(Of ClassBlockSyntax)

        Friend Const BC30610 As String = "BC30610" ' Class 'goo' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(BC30610)
        End Sub
    End Class
End Namespace
