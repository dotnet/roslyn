' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
