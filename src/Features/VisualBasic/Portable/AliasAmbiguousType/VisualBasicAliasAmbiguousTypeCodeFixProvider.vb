' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AliasAmbiguousType
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.CodeAnalysis.VisualBasic.AliasAmbiguousType

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AliasAmbiguousType), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.FullyQualify)>
    Friend Class VisualBasicAliasAmbiguousTypeCodeFixProvider
        Inherits AbstractAliasAmbiguousTypeCodeFixProvider

        'BC30561: '<name1>' is ambiguous, imported from the namespaces or types '<name2>'
        Private Const BC30561 As String = NameOf(BC30561)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30561)

    End Class
End Namespace
