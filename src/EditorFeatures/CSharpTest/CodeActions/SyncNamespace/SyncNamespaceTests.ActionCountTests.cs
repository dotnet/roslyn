// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task SyncNamespace_MissingNotOnDeclaration()
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
        public async Task SyncNamespace_MissingMultipleNamespaceDeclarations()
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
        public async Task SyncNamespace_ActionCounts_MoveOnly()
        {
            var folders = new[] { "A", "3B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""NS1"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
namespace [||]NS1.NS2.NS3
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";

            // Fixes offered will be move file to matching folder.
            // No rename namespace action since the folder name is invalid identifier.
            await TestActionCountAsync(code, count: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task SyncNamespace_ActionCounts_ChangeNameOnly()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""NS1"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
namespace [||]NS2.NS3
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";

            // Fixes offered will be change namespace file to match folder hierarchy.
            // No move file action since default namespace is not containing declared namespace.
            await TestActionCountAsync(code, count: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSyncNamespace)]
        public async Task SyncNamespace_MissingMatchingNamespace1()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""NS""  CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
namespace [||]NS.A.B
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
        public async Task SyncNamespace_MissingMatchingNamespace2()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""NS"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
namespace [||]NS.A.B
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
        public async Task SyncNamespace_ActionCounts_MoveAndChangeName()
        {
            var folders = new[] { "A", "B" };
            var documentPath = CreateDocumentFilePath(folders);

            var code =
$@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" FilePath=""{ProjectFilePath}"" DefaultNamespace=""NS1"" CommonReferences=""true"">
        <Document Folders=""{documentPath.folder}"" FilePath=""{documentPath.filePath}""> 
namespace [||]NS1.NS2.NS3
{{    
    class Class1
    {{
    }}
}}  
        </Document>
    </Project>
</Workspace>";
                                                                                      
            await TestActionCountAsync(code, count: 2);
        }
    }
}
