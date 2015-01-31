' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging
    Public Class LocationInfoGetterTests

        Private Sub Test(text As String, expectedName As String)
            Dim position As Integer
            MarkupTestFile.GetPosition(text, text, position)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(text)
                Dim languageDebugInfo = New VisualBasicLanguageDebugInfoService()

                Dim result = languageDebugInfo.GetLocationInfoAsync(workspace.CurrentSolution.Projects.Single().Documents.Single(), position, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(expectedName, result.Name)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestLocationNameOnSub()
            Test(<text>
class C
  sub Foo()$$
  end sub
end class</text>.NormalizedValue, "C.Foo()")
        End Sub

    End Class
End Namespace
