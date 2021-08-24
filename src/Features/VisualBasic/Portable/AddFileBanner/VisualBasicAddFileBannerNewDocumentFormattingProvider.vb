' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddFileBanner
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.FileHeaders
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.FileHeaders

Namespace Microsoft.CodeAnalysis.VisualBasic.AddFileBanner
    <ExportNewDocumentFormattingProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddFileBannerNewDocumentFormattingProvider
        Inherits AbstractAddFileBannerNewDocumentFormattingProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxGenerator As SyntaxGenerator = VisualBasicSyntaxGenerator.Instance
        Protected Overrides ReadOnly Property SyntaxGeneratorInternal As SyntaxGeneratorInternal = VisualBasicSyntaxGeneratorInternal.Instance

        Protected Overrides ReadOnly Property FileHeaderHelper As AbstractFileHeaderHelper = VisualBasicFileHeaderHelper.Instance
    End Class
End Namespace
