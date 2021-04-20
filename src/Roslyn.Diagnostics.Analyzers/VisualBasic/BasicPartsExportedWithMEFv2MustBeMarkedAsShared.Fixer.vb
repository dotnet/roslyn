' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    End Class
End Namespace
