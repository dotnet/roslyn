' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddObsoleteAttribute
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Microsoft.CodeAnalysis.VisualBasic.AddObsoleteAttribute
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddObsoleteAttributeCodeFixProvider)), [Shared]>
    Friend Class VisualBasicAddObsoleteAttributeCodeFixProvider
        Inherits AbstractAddObsoleteAttributeCodeFixProvider

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(
                "BC40000", ' 'C' is obsolete. (msg)
                "BC40008"  ' 'C' is obsolete.
            )

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFactsService.Instance, VBFeaturesResources.Add_Obsolete)
        End Sub
    End Class
End Namespace
