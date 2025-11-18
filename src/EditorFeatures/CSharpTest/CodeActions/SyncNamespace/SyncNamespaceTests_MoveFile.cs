// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace;

[Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
public sealed partial class SyncNamespaceTests : CSharpSyncNamespaceTestsBase
{
    [Fact]
    public async Task MoveFile_DeclarationNotContainedInDefaultNamespace()
    {
        // No "move file" action because default namespace is not container of declared namespace
        var defaultNamespace = "A";
        var declaredNamespace = "Foo.Bar";

        var expectedFolders = new List<string[]>();

        var (folder, filePath) = CreateDocumentFilePath([], "File1.cs");
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}}
            {    
                class Class1
                {
                }
            }  
                    </Document>
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_DeclarationNotContainedInDefaultNamespace_FileScopedNamespace()
    {
        // No "move file" action because default namespace is not container of declared namespace
        var defaultNamespace = "A";
        var declaredNamespace = "Foo.Bar";

        var expectedFolders = new List<string[]>();

        var (folder, filePath) = CreateDocumentFilePath([], "File1.cs");
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}};

            class Class1
            {
            }  
                    </Document>
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_SingleAction1()
    {
        // current path is <root>\
        // expected new path is <root>\B\C\

        var defaultNamespace = "A";
        var declaredNamespace = "A.B.C";

        var expectedFolders = new List<string[]>
        {
            new[] { "B", "C" }
        };

        var (folder, filePath) = CreateDocumentFilePath([]);
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}}
            {    
                class Class1
                {
                }
            }  
                    </Document>
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_SingleAction2()
    {
        // current path is <root>\
        // expected new path is <root>\B\C\D\E\

        var defaultNamespace = "A";
        var declaredNamespace = "A.B.C.D.E";

        var expectedFolders = new List<string[]>
        {
            new[] { "B", "C", "D", "E" }
        };

        var (folder, filePath) = CreateDocumentFilePath([], "File1.cs");
        var documentPath2 = CreateDocumentFilePath(["B", "C"], "File2.cs");   // file2 is in <root>\B\C\
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}}
            {    
                class Class1
                {
                }
            }  
                    </Document>        
                    <Document Folders="{{documentPath2.folder}}" FilePath="{{documentPath2.filePath}}"> 
            namespace Foo
            {    
                class Class2
                {
                }
            }  
                    </Document>  
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_MoveToRoot()
    {
        // current path is <root>\A\B\C\
        // expected new path is <root>

        var defaultNamespace = "";

        var expectedFolders = new List<string[]>
        {
            Array.Empty<string>()
        };

        var (folder, filePath) = CreateDocumentFilePath(["A", "B", "C"]);
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}">   
            class [||]Class1
            {
            }

            class Class2
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_MultipleAction1()
    {
        // current path is <root>\
        // expected new paths are"
        // 1. <root>\B\C\D\E\
        // 2. <root>\B.C\D\E\

        var defaultNamespace = "A";
        var declaredNamespace = "A.B.C.D.E";

        var expectedFolders = new List<string[]>
        {
            (["B", "C", "D", "E"]),
            (["B.C", "D", "E"])
        };

        var (folder, filePath) = CreateDocumentFilePath([], "File1.cs");
        var documentPath2 = CreateDocumentFilePath(["B.C"], "File2.cs");   // file2 is in <root>\B.C\
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}}
            {    
                class Class1
                {
                }
            }  
                    </Document>        
                    <Document Folders="{{documentPath2.folder}}" FilePath="{{documentPath2.filePath}}"> 
            namespace Foo
            {    
                class Class2
                {
                }
            }  
                    </Document>  
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_MultipleAction2()
    {
        // current path is <root>\
        // expected new paths are:
        // 1. <root>\B\C\D\E\
        // 2. <root>\B.C\D\E\
        // 3. <root>\B\C.D\E\

        var defaultNamespace = "A";
        var declaredNamespace = "A.B.C.D.E";

        var expectedFolders = new List<string[]>
        {
            (["B", "C", "D", "E"]),
            (["B.C", "D", "E"]),
            (["B", "C.D", "E"]),
        };

        var (folder, filePath) = CreateDocumentFilePath([], "File1.cs");
        var documentPath2 = CreateDocumentFilePath(["B", "C.D"], "File2.cs");   // file2 is in <root>\B\C.D\
        var documentPath3 = CreateDocumentFilePath(["B.C"], "File3.cs");   // file3 is in <root>\B.C\
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}}
            {    
                class Class1
                {
                }
            }  
                    </Document>        
                    <Document Folders="{{documentPath2.folder}}" FilePath="{{documentPath2.filePath}}"> 
            namespace Foo
            {    
                class Class2
                {
                }
            }  
                    </Document>      
                    <Document Folders="{{documentPath3.folder}}" FilePath="{{documentPath3.filePath}}"> 
            namespace Foo
            {    
                class Class2
                {
                }
            }  
                    </Document>  
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_FromOneFolderToAnother1()
    {
        var defaultNamespace = "A";
        var declaredNamespace = "A.B.C.D.E";

        var expectedFolders = new List<string[]>
        {
            (["B", "C", "D", "E"]),
            (["B.C", "D", "E"]),
        };

        var (folder, filePath) = CreateDocumentFilePath(["B.C"], "File1.cs");                          // file1 is in <root>\B.C\
        var documentPath2 = CreateDocumentFilePath(["B", "Foo"], "File2.cs");   // file2 is in <root>\B\Foo\
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}}
            {    
                class Class1
                {
                }
            }  
                    </Document>        
                    <Document Folders="{{documentPath2.folder}}" FilePath="{{documentPath2.filePath}}"> 
            namespace Foo
            {    
                class Class2
                {
                }
            }  
                    </Document>  
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_FromOneFolderToAnother2()
    {
        var defaultNamespace = "A";
        var declaredNamespace = "A.B.C.D.E";

        var expectedFolders = new List<string[]>
        {
            (["B", "C", "D", "E"]),
        };

        var (folder, filePath) = CreateDocumentFilePath(["Foo.Bar", "Baz"], "File1.cs");  // file1 is in <root>\Foo.Bar\Baz\
        var documentPath2 = CreateDocumentFilePath(["B", "Foo"], "File2.cs");   // file2 is in <root>\B\Foo\
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}}
            {    
                class Class1
                {
                }
            }  
                    </Document>        
                    <Document Folders="{{documentPath2.folder}}" FilePath="{{documentPath2.filePath}}"> 
            namespace Foo
            {    
                class Class2
                {
                }
            }  
                    </Document>  
                </Project>
            </Workspace>
            """, expectedFolders);
    }

    [Fact]
    public async Task MoveFile_FromOneFolderToAnother2_FileScopedNamespace()
    {
        var defaultNamespace = "A";
        var declaredNamespace = "A.B.C.D.E";

        var expectedFolders = new List<string[]>
        {
            (["B", "C", "D", "E"]),
        };

        var (folder, filePath) = CreateDocumentFilePath(["Foo.Bar", "Baz"], "File1.cs");  // file1 is in <root>\Foo.Bar\Baz\
        var documentPath2 = CreateDocumentFilePath(["B", "Foo"], "File2.cs");   // file2 is in <root>\B\Foo\
        await TestMoveFileToMatchNamespace($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" FilePath="{{ProjectFilePath}}" RootNamespace="{{defaultNamespace}}" CommonReferences="true">
                    <Document Folders="{{folder}}" FilePath="{{filePath}}"> 
            namespace [||]{{declaredNamespace}};

            class Class1
            {
            }  
                    </Document>        
                    <Document Folders="{{documentPath2.folder}}" FilePath="{{documentPath2.filePath}}"> 
            namespace Foo;

            class Class2
            {
            }  
                    </Document>  
                </Project>
            </Workspace>
            """, expectedFolders);
    }
}
