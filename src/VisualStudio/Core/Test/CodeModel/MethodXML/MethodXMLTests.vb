' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    <[UseExportProvider]>
    Partial Public Class MethodXMLTests

        Private Shared Sub Test(definition As XElement, expected As XElement)
            Using state = CreateCodeModelTestState(definition)
                Dim func = state.GetCodeElementAtCursor(Of EnvDTE.CodeFunction)()
                Dim actual = func.GetMethodXML()

                Assert.Equal(expected.ToString(), actual.ToString())
            End Using
        End Sub
    End Class
End Namespace
