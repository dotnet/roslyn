' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.AliasAmbiguousType
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.CodeAnalysis.VisualBasic.AliasAmbiguousType
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AliasAmbiguousType), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.FullyQualify)>
    Friend Class VisualBasicAliasAmbiguousTypeCodeFixProvider
        Inherits AbstractAliasAmbiguousTypeCodeFixProvider

        'BC30561: '<name1>' is ambiguous, imported from the namespaces or types '<name2>'
        Private Const BC30561 As String = NameOf(BC30561)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30561)

        Protected Overrides Function GetTextPreviewOfChange(aliasName As String, typeSymbol As ITypeSymbol) As String
            Return $"Imports { aliasName } = { typeSymbol.ToNameDisplayString() }"
        End Function
    End Class
End Namespace
