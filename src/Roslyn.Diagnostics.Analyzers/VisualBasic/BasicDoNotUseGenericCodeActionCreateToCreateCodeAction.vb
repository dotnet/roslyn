' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers   
    ''' <summary>
    ''' RS0005: Do not use generic CodeAction.Create to create CodeAction
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicDoNotUseGenericCodeActionCreateToCreateCodeActionAnalyzer
        Inherits DoNotUseGenericCodeActionCreateToCreateCodeActionAnalyzer

    End Class
End Namespace