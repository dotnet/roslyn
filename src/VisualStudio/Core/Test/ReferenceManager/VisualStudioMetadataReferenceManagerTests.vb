' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ReferenceManager
    <UseExportProvider>
    Public Class VisualStudioMetadataReferenceManagerTests
        <Fact>
        Public Sub TestReferenceAssemblyWithMultipleModules() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp("")
                Dim assemblyDir = Path.GetDirectoryName(GetType(Object).Assembly.Location)
                Dim enterprisePath = Path.Combine(assemblyDir, "System.EnterpriseServices.dll")

                Dim tempStorageService = DirectCast(workspace.Services.GetRequiredService(Of ITemporaryStorageServiceInternal), TemporaryStorageService)

                Dim tuple = VisualStudioMetadataReferenceManager.TestAccessor.CreateAssemblyMetadata(
                    enterprisePath, tempStorageService)
                Assert.NotNull(tuple.assemblyMetadata)
                Assert.NotNull(tuple.handles)

                ' We should have two handles as this assembly has two modules (itself, and one submodule for
                ' System.EnterpriseServices.Wrapper.dll)
                Assert.Equal(2, tuple.handles.Count)
            End Using
        End Sub
    End Class
End Namespace
