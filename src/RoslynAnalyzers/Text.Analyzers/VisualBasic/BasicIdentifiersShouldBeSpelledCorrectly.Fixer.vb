' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Text.Analyzers

Namespace Text.VisualBasic.Analyzers
    ''' <summary>
    ''' CA1704: Identifiers should be spelled correctly
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class BasicIdentifiersShouldBeSpelledCorrectlyFixer
        Inherits IdentifiersShouldBeSpelledCorrectlyFixer

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
