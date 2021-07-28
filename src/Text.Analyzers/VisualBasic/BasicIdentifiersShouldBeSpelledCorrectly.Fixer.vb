' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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

    End Class
End Namespace
