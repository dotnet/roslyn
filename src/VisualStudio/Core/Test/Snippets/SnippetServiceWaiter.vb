' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Shared.TestHooks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    <[Shared]>
    <Export(GetType(IAsynchronousOperationListener))>
    <Export(GetType(IAsynchronousOperationWaiter))>
    <Feature(FeatureAttribute.Snippets)>
    Friend Class SnippetServiceWaiter
        Inherits AsynchronousOperationListener

    End Class
End Namespace
