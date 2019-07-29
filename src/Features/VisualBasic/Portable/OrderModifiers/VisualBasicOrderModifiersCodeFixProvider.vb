' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
