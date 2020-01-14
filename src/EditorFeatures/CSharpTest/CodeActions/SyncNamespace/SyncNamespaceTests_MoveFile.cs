// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    public partial class SyncNamespaceTests : CSharpSyncNamespaceTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task MoveFile_DeclarationNotContainedInDefaultNamespace()
        {
            // No "move file" action because default namespace is not container of declared namespace
            var defaultNamespace = "A";
            var declaredNamespace = "Foo.Bar";

            var expectedFolders = new List<string[]>();

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
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

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>());
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
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

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(new[] { "B", "C" }, "File2.cs");   // file2 is in <root>\B\C\
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>        
        <Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{    
    class Class2
    {{
    }}
}}  
        </Document>  
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task MoveFile_MoveToRoot()
        {
            // current path is <root>\A\B\C\
            // expected new path is <root>

            var defaultNamespace = "";

            var expectedFolders = new List<string[]>
            {
                Array.Empty<string>()
            };

            var (folder, filePath) = CreateDocumentFilePath(new[] { "A", "B", "C" });
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">   
class [||]Class1
{{
}}

class Class2
{{
}}
        </Document>
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task MoveFile_MultipleAction1()
        {
            // current path is <root>\
            // expected new paths are"
            // 1. <root>\B\C\D\E\
            // 2. <root>\B.C\D\E\

            var defaultNamespace = "A";
            var declaredNamespace = "A.B.C.D.E";

            var expectedFolders = new List<string[]>();
            expectedFolders.Add(new[] { "B", "C", "D", "E" });
            expectedFolders.Add(new[] { "B.C", "D", "E" });

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(new[] { "B.C" }, "File2.cs");   // file2 is in <root>\B.C\
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>        
        <Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{    
    class Class2
    {{
    }}
}}  
        </Document>  
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task MoveFile_MultipleAction2()
        {
            // current path is <root>\
            // expected new paths are:
            // 1. <root>\B\C\D\E\
            // 2. <root>\B.C\D\E\
            // 3. <root>\B\C.D\E\

            var defaultNamespace = "A";
            var declaredNamespace = "A.B.C.D.E";

            var expectedFolders = new List<string[]>();
            expectedFolders.Add(new[] { "B", "C", "D", "E" });
            expectedFolders.Add(new[] { "B.C", "D", "E" });
            expectedFolders.Add(new[] { "B", "C.D", "E" });

            var (folder, filePath) = CreateDocumentFilePath(Array.Empty<string>(), "File1.cs");
            var documentPath2 = CreateDocumentFilePath(new[] { "B", "C.D" }, "File2.cs");   // file2 is in <root>\B\C.D\
            var documentPath3 = CreateDocumentFilePath(new[] { "B.C" }, "File3.cs");   // file3 is in <root>\B.C\
            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>        
        <Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{    
    class Class2
    {{
    }}
}}  
        </Document>      
        <Document Folders=""{documentPath3.folder}"" FilePath=""{documentPath3.filePath}""> 
namespace Foo
{{    
    class Class2
    {{
    }}
}}  
        </Document>  
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task MoveFile_FromOneFolderToAnother1()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "A.B.C.D.E";

            var expectedFolders = new List<string[]>();
            expectedFolders.Add(new[] { "B", "C", "D", "E" });
            expectedFolders.Add(new[] { "B.C", "D", "E" });

            var (folder, filePath) = CreateDocumentFilePath(new[] { "B.C" }, "File1.cs");                          // file1 is in <root>\B.C\
            var documentPath2 = CreateDocumentFilePath(new[] { "B", "Foo" }, "File2.cs");   // file2 is in <root>\B\Foo\

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>        
        <Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{    
    class Class2
    {{
    }}
}}  
        </Document>  
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task MoveFile_FromOneFolderToAnother2()
        {
            var defaultNamespace = "A";
            var declaredNamespace = "A.B.C.D.E";

            var expectedFolders = new List<string[]>();
            expectedFolders.Add(new[] { "B", "C", "D", "E" });

            var (folder, filePath) = CreateDocumentFilePath(new[] { "Foo.Bar", "Baz" }, "File1.cs");  // file1 is in <root>\Foo.Bar\Baz\
            var documentPath2 = CreateDocumentFilePath(new[] { "B", "Foo" }, "File2.cs");   // file2 is in <root>\B\Foo\

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""{defaultNamespace}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]{declaredNamespace}
{{    
    class Class1
    {{
    }}
}}  
        </Document>        
        <Document Folders=""{documentPath2.folder}"" FilePath=""{documentPath2.filePath}""> 
namespace Foo
{{    
    class Class2
    {{
    }}
}}  
        </Document>  
    </Project>
</Workspace>";
            await TestMoveFileToMatchNamespace(code, expectedFolders);
        }
    }
}
