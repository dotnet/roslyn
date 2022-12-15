' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.UnitTesting
    <UseExportProvider>
    Partial Public Class UnitTestingSearchHelpersTests
        Private Shared ReadOnly s_inProcessComposition As TestComposition = EditorTestCompositions.EditorFeatures
        Private Shared ReadOnly s_outOffProcessComposition As TestComposition = s_inProcessComposition.WithTestHostParts(TestHost.OutOfProcess)

        Private Shared Async Function Test(query As UnitTestingSearchQuery, workspace As TestWorkspace) As Task
            Dim project = workspace.CurrentSolution.Projects.Single()

            Dim actualLocations = Await UnitTestingSearchHelpers.GetSourceLocationsAsync(
                project, query, cancellationToken:=Nothing)

            Dim expectedLocations = workspace.Documents.Single().SelectedSpans

            Assert.Equal(expectedLocations.Count, actualLocations.Length)

            For i = 0 To expectedLocations.Count - 1
                Dim expected = expectedLocations(i)
                Dim actual = actualLocations(i)

                Assert.Equal(expected, actual.DocumentSpan.SourceSpan)
            Next

            Dim actualLocation = Await UnitTestingSearchHelpers.GetSourceLocationAsync(
                project, query, cancellationToken:=Nothing)

            If actualLocation Is Nothing Then
                Assert.Empty(expectedLocations)
            Else
                Dim expectedLocation = expectedLocations(0)
                Assert.Equal(expectedLocation, actualLocation.Value.DocumentSpan.SourceSpan)
            End If
        End Function
    End Class
End Namespace
