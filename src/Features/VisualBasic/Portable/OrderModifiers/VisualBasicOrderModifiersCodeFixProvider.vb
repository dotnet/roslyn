' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.OrderModifiers
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.CodeAnalysis.VisualBasic.OrderModifiers
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicOrderModifiersCodeFixProvider
        Inherits AbstractOrderModifiersCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFactsService.Instance,
                       VisualBasicCodeStyleOptions.PreferredModifierOrder,
                       VisualBasicOrderModifiersHelper.Instance)
        End Sub

        Protected Overrides ReadOnly Property FixableCompilerErrorIds As ImmutableArray(Of String) =
            ImmutableArray(Of String).Empty
    End Class
End Namespace
