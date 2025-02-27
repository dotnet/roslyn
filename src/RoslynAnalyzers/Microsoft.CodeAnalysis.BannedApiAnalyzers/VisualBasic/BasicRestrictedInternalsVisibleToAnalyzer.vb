﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.BannedApiAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.BannedApiAnalyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicRestrictedInternalsVisibleToAnalyzer
        Inherits RestrictedInternalsVisibleToAnalyzer(Of NameSyntax, SyntaxKind)

        Protected Overrides ReadOnly Property NameSyntaxKinds As ImmutableArray(Of SyntaxKind)
            Get
                Return ImmutableArray.Create(
                    SyntaxKind.IdentifierName,
                    SyntaxKind.GenericName,
                    SyntaxKind.QualifiedName)
            End Get
        End Property

        Protected Overrides Function IsInTypeOnlyContext(node As NameSyntax) As Boolean
            Return SyntaxFacts.IsInTypeOnlyContext(node)
        End Function
    End Class
End Namespace
