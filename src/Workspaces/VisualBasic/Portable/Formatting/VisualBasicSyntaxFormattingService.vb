' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Diagnostics
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <ExportLanguageService(GetType(ISyntaxFormattingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxFormattingService
        Inherits VisualBasicSyntaxFormatting
        Implements ISyntaxFormattingService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
