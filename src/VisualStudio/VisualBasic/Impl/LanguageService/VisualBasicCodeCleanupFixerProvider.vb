' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Language.CodeCleanUp
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeCleanup
    <Export(GetType(ICodeCleanUpFixerProvider))>
    <AppliesToProject("VB")>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    Friend Class VisualBasicCodeCleanUpFixerProvider
        Inherits AbstractCodeCleanUpFixerProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            <ImportMany> codeCleanUpFixers As IEnumerable(Of Lazy(Of AbstractCodeCleanUpFixer, ContentTypeMetadata)))
            MyBase.New(codeCleanUpFixers)
        End Sub
    End Class
End Namespace
