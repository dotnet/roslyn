' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.SimplifyBooleanExpression

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyBooleanExpression
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSimplifyConditionalCodeFixProvider
        Inherits AbstractSimplifyConditionalCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub
    End Class
End Namespace
