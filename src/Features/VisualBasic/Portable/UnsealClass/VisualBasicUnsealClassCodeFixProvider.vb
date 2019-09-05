' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UnsealClass

Namespace Microsoft.CodeAnalysis.VisualBasic.UnsealClass
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UnsealClass), [Shared]>
    Friend NotInheritable Class VisualBasicUnsealClassCodeFixProvider
        Inherits AbstractUnsealClassCodeFixProvider

        Private Const BC30299 As String = NameOf(BC30299) ' 'D' cannot inherit from class 'C' because 'C' is declared 'NotInheritable'.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC30299)

        Protected Overrides ReadOnly Property TitleFormat As String = VBFeaturesResources.Make_0_inheritable
    End Class
End Namespace
