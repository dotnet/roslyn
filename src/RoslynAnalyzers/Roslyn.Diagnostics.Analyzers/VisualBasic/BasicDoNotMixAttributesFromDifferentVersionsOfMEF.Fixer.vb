' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    ''' <summary>
    ''' RS0006: Do not mix attributes from different versions of MEF
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class BasicDoNotMixAttributesFromDifferentVersionsOfMEFFixer
        Inherits DoNotMixAttributesFromDifferentVersionsOfMEFFixer

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
