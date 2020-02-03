' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
