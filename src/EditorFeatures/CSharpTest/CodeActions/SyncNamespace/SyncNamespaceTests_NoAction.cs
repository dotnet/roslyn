﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    public partial class SyncNamespaceTests : CSharpSyncNamespaceTestsBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_NotOnNamespaceDeclaration()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_NotOnFirstMemberInGlobal()
        {
            var folders = new[] { "A" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace="""" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}"">    
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_MultipleNamespaceDeclarations()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_MembersInBothGlobalAndNamespaceDeclaration_CursorOnNamespace()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_MembersInBothGlobalAndNamespaceDeclaration_CursorOnFirstGlobalMember()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_NestedNamespaceDeclarations()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_InvalidNamespaceIdentifier()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace="""" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_MatchingNamespace_InGlobalNamespace()
        {
            var folders = Array.Empty<string>();
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""""  CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}"">    
class [||]Class1
{{
}}
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_MatchingNamespace_DefaultGlobalNamespace()
        {
            var folders = new[] { "A", "B", "C" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""""  CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}"">    
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_MatchingNamespace_InNamespaceDeclaration()
        {
            var folders = new[] { "B", "C" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" RootNamespace=""A"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task NoAction_NoDeclaration()
        {
            var folders = new[] { "A" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
using System;   
[||]
        </Document>
    </Project>
</Workspace>";

            await TestMissingInRegularAndScriptAsync(code);
        }
    }
}
