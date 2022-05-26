' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UnsealClass

Namespace Microsoft.CodeAnalysis.VisualBasic.UnsealClass
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UnsealClass), [Shared]>
    Friend NotInheritable Class VisualBasicUnsealClassCodeFixProvider
        Inherits AbstractUnsealClassCodeFixProvider

        Private Const BC30299 As String = NameOf(BC30299) ' 'D' cannot inherit from class 'C' because 'C' is declared 'NotInheritable'.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC30299)

        Protected Overrides ReadOnly Property TitleFormat As String = VisualBasicCodeFixesResources.Make_0_inheritable
    End Class
End Namespace
