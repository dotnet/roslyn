' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Interface IIntelliSenseTestState
        Property CurrentCompletionPresenterSession As CompletionPresenterSession
        Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
    End Interface
End Namespace