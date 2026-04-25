// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostWillRenameEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task Component()
        => VerifyRenamesAsync(
            oldName: FileUri("Component.razor"),
            newName: FileUri("DifferentName.razor"),
            files: [
                (FilePath("Component.razor"), ""),
                (FilePath("OtherComponent.razor"), """
                    <Component />
                    <Component></Component>
                    <Component>
                    </Component>
                    """)
            ],
            expectedFiles: [
                (FileUri("OtherComponent.razor"), """
                    <DifferentName />
                    <DifferentName></DifferentName>
                    <DifferentName>
                    </DifferentName>
                    """)
            ]);

    [Fact]
    public Task Component_Self()
      => VerifyRenamesAsync(
          oldName: FileUri("Component.razor"),
          newName: FileUri("DifferentName.razor"),
          files: [
              (FilePath("Component.razor"), """
                <Component />
                """),
                (FilePath("OtherComponent.razor"), """
                    <Component />
                    <Component></Component>
                    <Component>
                    </Component>
                    """)
          ],
          expectedFiles: [
              (FileUri("Component.razor"), """
                    <DifferentName />
                    """),
              (FileUri("OtherComponent.razor"), """
                    <DifferentName />
                    <DifferentName></DifferentName>
                    <DifferentName>
                    </DifferentName>
                    """)
          ]);

    [Fact]
    public Task Component_FullyQualified()
        => VerifyRenamesAsync(
            oldName: FileUri("Component.razor"),
            newName: FileUri("DifferentName.razor"),
            files: [
                 (FilePath("Component.razor"), """
                     @namespace My.Components
                     """),
                 (FilePath("OtherComponent.razor"), """
                     <My.Components.Component />
                     <My.Components.Component></My.Components.Component>
                     <My.Components.Component>
                     </My.Components.Component>
                     """)
            ],
            expectedFiles: [
                (FileUri("OtherComponent.razor"), """
                    <My.Components.DifferentName />
                    <My.Components.DifferentName></My.Components.DifferentName>
                    <My.Components.DifferentName>
                    </My.Components.DifferentName>
                    """)
             ]);

    [Fact]
    public Task Component_WithCss()
        => VerifyRenamesAsync(
            oldName: FileUri("Component.razor"),
            newName: FileUri("DifferentName.razor"),
            files: [
                (FilePath("Component.razor"), ""),
                (FilePath("Component.razor.css"), ""),
                (FilePath("OtherComponent.razor"), """
                    <Component />
                    <Component></Component>
                    <Component>
                    </Component>
                    """)
            ],
            expectedFiles: [
                (FileUri("DifferentName.razor.css"), ""),
                (FileUri("OtherComponent.razor"), """
                    <DifferentName />
                    <DifferentName></DifferentName>
                    <DifferentName>
                    </DifferentName>
                    """)
            ]);

    [Fact]
    public Task Component_WithCSharp()
        => VerifyRenamesAsync(
            oldName: FileUri("Component.razor"),
            newName: FileUri("DifferentName.razor"),
            files: [
                (FilePath("Component.razor"), ""),
                (FilePath("ExtraCode.cs"), """
                    using SomeProject;

                    public class C
                    {
                        public void M(Component c)
                        {
                        }
                    }
                    """),
                (FilePath("OtherComponent.razor"), """
                    <Component />
                    <Component></Component>
                    <Component>
                    </Component>
                    """)
            ],
            expectedFiles: [
                (FileUri("ExtraCode.cs"), """
                    using SomeProject;

                    public class C
                    {
                        public void M(DifferentName c)
                        {
                        }
                    }
                    """),
                (FileUri("OtherComponent.razor"), """
                    <DifferentName />
                    <DifferentName></DifferentName>
                    <DifferentName>
                    </DifferentName>
                    """)
            ]);

    private async Task VerifyRenamesAsync(
        Uri newName,
        Uri oldName,
        (string fileName, string contents)[] files,
        (Uri fileUri, string contents)[] expectedFiles)
    {
        var document = CreateProjectAndRazorDocument(contents: "", additionalFiles: files);

        var fileSystem = (RemoteFileSystem)OOPExportProvider.GetExportedValue<IFileSystem>();
        fileSystem.GetTestAccessor().SetFileSystem(new TestFileSystem(files));

        var endpoint = new WorkspaceWillRenameEndpoint(RemoteServiceInvoker, LoggerFactory);

        var renameParams = new RenameFilesParams
        {
            Files = [
                new FileRename
                {
                    OldUri = new(oldName),
                    NewUri = new(newName),
                }
            ]
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(renameParams, document.Project.Solution, DisposalToken);

        Assert.NotNull(result);

        await result.AssertWorkspaceEditAsync(document.Project.Solution, expectedFiles, DisposalToken);
    }
}
