' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <ExportCompletionProvider(NameOf(VisualBasicMockCompletionProvider), LanguageNames.VisualBasic)>
    <[Shared]>
    <PartNotDiscoverable>
    Friend Class VisualBasicMockCompletionProvider
        Inherits MockCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
