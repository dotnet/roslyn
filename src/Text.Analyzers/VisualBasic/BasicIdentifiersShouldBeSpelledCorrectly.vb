' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Text.Analyzers

Namespace Text.VisualBasic.Analyzers
    ''' <summary>
    ''' CA1704: Identifiers should be spelled correctly
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicIdentifiersShouldBeSpelledCorrectlyAnalyzer
        Inherits IdentifiersShouldBeSpelledCorrectlyAnalyzer

    End Class
End Namespace