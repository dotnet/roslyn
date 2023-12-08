' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.OrderModifiers
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.OrderModifiers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.OrderModifiers), [Shared]>
    Friend NotInheritable Class VisualBasicOrderModifiersCodeFixProvider
        Inherits AbstractOrderModifiersCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance,
                       VisualBasicOrderModifiersHelper.Instance)
        End Sub

        Protected Overrides Function GetCodeStyleOption(options As AnalyzerOptionsProvider) As CodeStyleOption2(Of String)
            Return CType(options, VisualBasicAnalyzerOptionsProvider).PreferredModifierOrder
        End Function

        Protected Overrides ReadOnly Property FixableCompilerErrorIds As ImmutableArray(Of String) =
            ImmutableArray(Of String).Empty
    End Class
End Namespace
