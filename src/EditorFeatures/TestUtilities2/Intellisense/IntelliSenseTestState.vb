' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <Export(GetType(IIntelliSenseTestState))>
    <PartNotDiscoverable>
    Friend Class IntelliSenseTestState
        Implements IIntelliSenseTestState

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Property CurrentCompletionPresenterSession As TestCompletionPresenterSession Implements IIntelliSenseTestState.CurrentCompletionPresenterSession

        Public Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession Implements IIntelliSenseTestState.CurrentSignatureHelpPresenterSession
    End Class
End Namespace
