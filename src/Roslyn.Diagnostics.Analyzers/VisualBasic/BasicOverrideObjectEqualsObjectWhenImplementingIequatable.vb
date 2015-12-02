' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers   
    ''' <summary>
    ''' RS0009: Override Object.Equals(object) when implementing Iequatable
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicOverrideObjectEqualsObjectWhenImplementingIequatableAnalyzer
        Inherits OverrideObjectEqualsObjectWhenImplementingIequatableAnalyzer

    End Class
End Namespace