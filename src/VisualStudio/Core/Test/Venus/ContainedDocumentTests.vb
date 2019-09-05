' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus
    Public Class ContainedDocumentTests
        <Fact>
        Public Sub ContainedDocument_AcceptsNullInput()
            Dim documentId = ContainedDocument.TryGetContainedDocument(Nothing)
            Assert.Null(documentId)
        End Sub
    End Class
End Namespace
