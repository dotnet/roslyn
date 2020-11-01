' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.ChangeAccessibilityModifier

Friend Class VisualBasicChangeAccessibilityModifierCodeFixProvider
    Inherits AbstractChangeAccessibilityModifierCodeFixProvider

    'BC31408: 'Private' and 'MustOverride' cannot be combined.
    Private Const BC31408 As String = NameOf(BC31408)

    'BC30266: 'accessibility identifier' cannot override 'accessibility identifier' because they have different access levels.
    Private Const BC30266 As String = NameOf(BC30266)

    <ImportingConstructor>
    <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
    Public Sub New()
    End Sub

    Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
        ImmutableArray.Create(BC31408, BC30266)
End Class
