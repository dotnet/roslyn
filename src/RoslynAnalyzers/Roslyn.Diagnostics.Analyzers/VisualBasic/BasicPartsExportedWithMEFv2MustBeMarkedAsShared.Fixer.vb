' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    ''' <summary>
    ''' RS0023: Parts exported with MEFv2 must be marked as Shared
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class BasicPartsExportedWithMEFv2MustBeMarkedAsSharedFixer
        Inherits PartsExportedWithMEFv2MustBeMarkedAsSharedFixer(Of TypeBlockSyntax)

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
