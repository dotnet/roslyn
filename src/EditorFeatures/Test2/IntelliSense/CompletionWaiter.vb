' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <Export(GetType(IAsynchronousOperationListener))>
    <Export(GetType(IAsynchronousOperationWaiter))>
    <Feature(FeatureAttribute.CompletionSet)>
    Friend Class CompletionWaiter
        Inherits AsynchronousOperationListener

    End Class
End Namespace
