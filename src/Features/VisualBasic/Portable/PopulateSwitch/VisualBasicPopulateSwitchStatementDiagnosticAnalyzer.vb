' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.PopulateSwitch

Namespace Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicPopulateSwitchStatementDiagnosticAnalyzer
        Inherits AbstractPopulateSwitchStatementDiagnosticAnalyzer(Of SelectBlockSyntax)
    End Class
End Namespace
