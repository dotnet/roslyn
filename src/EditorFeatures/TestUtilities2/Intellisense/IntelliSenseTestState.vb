' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <Export(GetType(IIntelliSenseTestState))>
    <PartNotDiscoverable>
    Friend Class IntelliSenseTestState
        Implements IIntelliSenseTestState

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession Implements IIntelliSenseTestState.CurrentSignatureHelpPresenterSession
    End Class
End Namespace
