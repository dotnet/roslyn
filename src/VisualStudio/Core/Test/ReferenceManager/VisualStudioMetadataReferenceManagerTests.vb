' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Serialization
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ReferenceManager
    <UseExportProvider>
    Public Class VisualStudioMetadataReferenceManagerTests
        <Fact>
        Public Sub TestReferenceAssemblyWithMultipleModules()
            Using workspace = EditorTestWorkspace.CreateCSharp("")
                Dim assemblyDir = Path.GetDirectoryName(GetType(Object).Assembly.Location)
                Dim enterprisePath = Path.Combine(assemblyDir, "System.EnterpriseServices.dll")

                Dim tempStorageService = DirectCast(workspace.Services.GetRequiredService(Of ITemporaryStorageServiceInternal), TemporaryStorageService)
                Dim serializerService = DirectCast(workspace.Services.GetRequiredService(Of ISerializerService), SerializerService)

                Dim tuple = VisualStudioMetadataReferenceManager.TestAccessor.CreateAssemblyMetadata(
                    enterprisePath, tempStorageService)
                Assert.NotNull(tuple.assemblyMetadata)
                Assert.NotNull(tuple.handles)

                ' We should have two handles as this assembly has two modules (itself, and one submodule for
                ' System.EnterpriseServices.Wrapper.dll)
                Assert.Equal(2, tuple.handles.Count)

                Dim testReference = New TestPEReference(
                    enterprisePath, tuple.assemblyMetadata, tuple.handles)

                Dim stream = New MemoryStream()
                Dim writer = New ObjectWriter(stream, leaveOpen:=True)
                serializerService.Serialize(testReference, writer, cancellationToken:=Nothing)

                stream.Position = 0
                Dim reader = ObjectReader.GetReader(stream, leaveOpen:=True)
                Dim deserialized = DirectCast(serializerService.Deserialize(
                    WellKnownSynchronizationKind.MetadataReference, reader, cancellationToken:=Nothing), MetadataReference)

                Dim checksum1 = SerializerService.CreateChecksum(testReference, cancellationToken:=Nothing)
                Dim checksum2 = SerializerService.CreateChecksum(deserialized, cancellationToken:=Nothing)

                ' Serializing the original reference and the deserialized reference should produce the same checksum
                Assert.Equal(checksum1, checksum2)
            End Using
        End Sub

        Private Class TestPEReference
            Inherits PortableExecutableReference
            Implements ISupportTemporaryStorage

            Private ReadOnly _metadata As Microsoft.CodeAnalysis.Metadata
            Private ReadOnly _storageHandles As IReadOnlyList(Of ITemporaryStorageStreamHandle)

            Public Sub New(fullPath As String, metadata As Microsoft.CodeAnalysis.Metadata, storageHandles As IReadOnlyList(Of ITemporaryStorageStreamHandle))
                MyBase.New(New MetadataReferenceProperties(), fullPath)
                _metadata = metadata
                _storageHandles = storageHandles
            End Sub

            Public ReadOnly Property StorageHandles As IReadOnlyList(Of ITemporaryStorageStreamHandle) Implements ISupportTemporaryStorage.StorageHandles
                Get
                    Return _storageHandles
                End Get
            End Property

            Protected Overrides Function CreateDocumentationProvider() As DocumentationProvider
                Throw New NotImplementedException()
            End Function

            Protected Overrides Function WithPropertiesImpl(properties As MetadataReferenceProperties) As PortableExecutableReference
                Throw New NotImplementedException()
            End Function

            Protected Overrides Function GetMetadataImpl() As Microsoft.CodeAnalysis.Metadata
                Return _metadata
            End Function
        End Class
    End Class
End Namespace
