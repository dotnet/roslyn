﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicNamingStyleDiagnosticAnalyzer
        Inherits NamingStyleDiagnosticAnalyzerBase(Of SyntaxKind)

        Protected Overrides ReadOnly Property SupportedSyntaxKinds As ImmutableArray(Of SyntaxKind) =
            ImmutableArray.Create(
                SyntaxKind.ModifiedIdentifier,
                SyntaxKind.CatchStatement,
                SyntaxKind.Parameter,
                SyntaxKind.TypeParameter)
    End Class
End Namespace
