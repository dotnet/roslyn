' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#If False Then
Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Shared.TestHooks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense
    <Export(GetType(IAsynchronousOperationListener))>
    <Export(GetType(IAsynchronousOperationWaiter))>
    <Feature(FeatureAttribute.CompletionSet)>
    Friend Class CompletionWaiter
        Inherits AsynchronousOperationListener

    End Class
End Namespace
#End If
