' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    <[UseExportProvider]>
    Partial Public Class MethodXMLTests

        Private Sub Test(definition As XElement, expected As XElement)
            Using state = CreateCodeModelTestState(definition)
                Dim func = state.GetCodeElementAtCursor(Of EnvDTE.CodeFunction)()
                Dim actual = func.GetMethodXML()

                Assert.Equal(expected.ToString(), actual.ToString())
            End Using
        End Sub
    End Class
End Namespace
