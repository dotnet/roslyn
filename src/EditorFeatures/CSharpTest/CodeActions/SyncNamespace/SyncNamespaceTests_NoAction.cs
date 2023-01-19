// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
    public partial class SyncNamespaceTests : CSharpSyncNamespaceTestsBase
    {
        [WpfFact]
        public async Task NoAction_NotOnNamespaceDeclaration()
        {
            var folders = new[] { "A", "B" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace NS
{{    
    class [||]Class1
    {{
    }}
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }
        [WpfFact]
        public async Task NoAction_NotOnNamespaceDeclaration_FileScopedNamespace()
        {
            var folders = new[] { "A", "B" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace NS;

class [||]Class1
{{
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_NotOnFirstMemberInGlobal()
        {
            var folders = new[] { "A" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace="""" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">    
class Class1
{{
}}

class [||]Class2
{{
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_MultipleNamespaceDeclarations()
        {
            var folders = new[] { "A", "B" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]NS1
{{   
    class Class1
    {{
    }}
}}

namespace NS2
{{    
    class Class1
    {{
    }}
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_MembersInBothGlobalAndNamespaceDeclaration_CursorOnNamespace()
        {
            var folders = new[] { "A", "B" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]NS1
{{   
    class Class1
    {{
    }}
}} 

class Class2
{{
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_MembersInBothGlobalAndNamespaceDeclaration_CursorOnFirstGlobalMember()
        {
            var folders = new[] { "A", "B" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
class [||]Class1
{{
}}

namespace NS1
{{   
    class Class2
    {{
    }}
}} 
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_NestedNamespaceDeclarations()
        {
            var folders = new[] { "A", "B" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]NS1
{{   
    namespace NS2
    {{
        class Class1
        {{
        }}
    }}
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_InvalidNamespaceIdentifier()
        {
            var folders = new[] { "A", "B" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace="""" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]
{{
    class Class1
    {{
    }}
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_MatchingNamespace_InGlobalNamespace()
        {
            var folders = Array.Empty<string>();
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""""  CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">    
class [||]Class1
{{
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_MatchingNamespace_DefaultGlobalNamespace()
        {
            var folders = new[] { "A", "B", "C" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""""  CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}"">    
namespace [||]A.B.C
{{  
    class Class1
    {{
    }}
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_MatchingNamespace_InNamespaceDeclaration()
        {
            var folders = new[] { "B", "C" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""A"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
namespace [||]A.B.C
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_FileNotRooted()
        {
            var filePath = PathUtilities.CombineAbsoluteAndRelativePaths(PathUtilities.GetPathRoot(ProjectFilePath), "Foo.cs");

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document FilePath=""{filePath}""> 
namespace [||]NS
{{    
    class Class1
    {{
    }}
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task NoAction_NoDeclaration()
        {
            var folders = new[] { "A" };
            var (folder, filePath) = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{folder}"" FilePath=""{filePath}""> 
using System;   
[||]
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }
    }
}
