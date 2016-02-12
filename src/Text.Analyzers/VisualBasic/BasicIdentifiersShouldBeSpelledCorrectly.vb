' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Text.Analyzers
    ''' <summary>
    ''' CA1704: Identifiers should be spelled correctly
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicIdentifiersShouldBeSpelledCorrectlyAnalyzer
        Inherits IdentifiersShouldBeSpelledCorrectlyAnalyzer

    End Class
End Namespace