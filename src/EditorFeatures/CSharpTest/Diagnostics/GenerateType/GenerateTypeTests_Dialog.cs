// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GenerateType;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateTypeTests
{
    public partial class GenerateTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region SameProject
        #region SameProject_SameFile 
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeDefaultValues()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        Foo f;
    }
}

class Foo
{
}",
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInsideNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.Foo$$|] f;
    }
}

namespace A
{
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.Foo f;
    }
}

namespace A
{
    class Foo
    {
    }
}",
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInsideQualifiedNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.B.Foo f;
    }
}
namespace A.B
{
    class Foo
    {
    }
}",
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithinQualifiedNestedNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.B.C.Foo$$|] f;
    }
}
namespace A.B
{
    namespace C
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.B.C.Foo f;
    }
}
namespace A.B
{
    namespace C
    {
        class Foo
        {
        }
    }
}",
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithinNestedQualifiedNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.B.C.Foo$$|] f;
    }
}
namespace A
{
    namespace B.C
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.B.C.Foo f;
    }
}
namespace A
{
    namespace B.C
    {
        class Foo
        {
        }
    }
}",
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithConstructorMembers()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var f = new [|$$Foo|](bar: 1, baz: 2);
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var f = new Foo(bar: 1, baz: 2);
    }
}

class Foo
{
    private int bar;
    private int baz;

    public Foo(int bar, int baz)
    {
        this.bar = bar;
        this.baz = baz;
    }
}",
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithBaseTypes()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        List<int> f = new [|$$Foo|]();
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        List<int> f = new Foo();
    }
}

class Foo : List<int>
{
}",
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithPublicInterface()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.B.C.Foo$$|] f;
    }
}
namespace A
{
    namespace B.C
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.B.C.Foo f;
    }
}
namespace A
{
    namespace B.C
    {
        public interface Foo
        {
        }
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithInternalStruct()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.B.C.Foo$$|] f;
    }
}
namespace A
{
    namespace B.C
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.B.C.Foo f;
    }
}
namespace A
{
    namespace B.C
    {
        internal struct Foo
        {
        }
    }
}",
accessibility: Accessibility.Internal,
typeKind: TypeKind.Struct,
isNewFile: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithDefaultEnum()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A
{
    namespace B
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.B.Foo f;
    }
}
namespace A
{
    namespace B
    {
        enum Foo
        {
        }
    }
}",
accessibility: Accessibility.NotApplicable,
typeKind: TypeKind.Enum,
isNewFile: false);
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithDefaultEnum_DefaultNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"using ConsoleApplication;

class Program
{
    void Main()
    {
        Foo f;
    }
}

namespace ConsoleApplication
{
    enum Foo
    {
    }
}",
defaultNamespace: "ConsoleApplication",
accessibility: Accessibility.NotApplicable,
typeKind: TypeKind.Enum,
isNewFile: false);
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithDefaultEnum_DefaultNamespace_NotSimpleName()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A
{
    namespace B
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    void Main()
    {
        A.B.Foo f;
    }
}
namespace A
{
    namespace B
    {
        enum Foo
        {
        }
    }
}",
defaultNamespace: "ConsoleApplication",
accessibility: Accessibility.NotApplicable,
typeKind: TypeKind.Enum,
isNewFile: false);
        }
        #endregion

        // Working is very similar to the adding to the same file
        #region SameProject_ExistingFile
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInExistingEmptyFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                        <Document FilePath=""Test2.cs"">

                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInExistingEmptyFile_Usings_Folders()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                        <Document Folders= ""outer\inner"" FilePath=""Test2.cs"">

                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace outer.inner
{
    public interface Foo
    {
    }
}",
isLine: false,
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInExistingEmptyFile_Usings_DefaultNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                        <Document FilePath=""Test2.cs"">

                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace ConsoleApplication
{
    public interface Foo
    {
    }
}",
isLine: false,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using ConsoleApplication;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInExistingEmptyFile_Usings_Folders_DefaultNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                        <Document Folders= ""outer\inner"" FilePath=""Test2.cs"">

                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace ConsoleApplication.outer.inner
{
    public interface Foo
    {
    }
}",
isLine: false,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using ConsoleApplication.outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInExistingEmptyFile_NoUsings_Folders_NotSimpleName()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                        <Document FilePath=""Test2.cs"" Folders= ""outer\inner"">

                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");
        }
        #endregion

        #region SameProject_NewFile
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInNewFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: Array.Empty<string>(),
newFileName: "Test2.cs");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_UsingsNotNeeded_InNewFile_InFolder()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
namespace outer
{
    namespace inner
    {
        class Program
        {
            void Main()
            {
                [|Foo$$|] f;
            }
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace outer.inner
{
    public interface Foo
    {
    }
}",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: new string[] { "outer", "inner" },
newFileName: "Test2.cs");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_UsingsNeeded_InNewFile_InFolder()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace outer.inner
{
    public interface Foo
    {
    }
}",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: new string[] { "outer", "inner" },
newFileName: "Test2.cs");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_UsingsNotNeeded_InNewFile_InFolder_NotSimpleName()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: new string[] { "outer", "inner" },
newFileName: "Test2.cs");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_UsingsNeeded_InNewFile_InFolder_DefaultNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace ConsoleApplication.outer.inner
{
    public interface Foo
    {
    }
}",
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using ConsoleApplication.outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: new string[] { "outer", "inner" },
newFileName: "Test2.cs");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_UsingsNotNeeded_InNewFile_InFolder_DefaultNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
namespace ConsoleApplication.outer
{
    class Program
    {
        void Main()
        {
            [|Foo$$|] f;
        }
    }
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace ConsoleApplication.outer
{
    public interface Foo
    {
    }
}",
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
namespace ConsoleApplication.outer
{
    class Program
    {
        void Main()
        {
            Foo f;
        }
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: new string[] { "outer" },
newFileName: "Test2.cs");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_UsingsNotNeeded_InNewFile_InFolder_DefaultNamespace_NotSimpleName()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}

namespace A.B
{
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
class Program
{
    void Main()
    {
        A.B.Foo f;
    }
}

namespace A.B
{
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: new string[] { "outer" },
newFileName: "Test2.cs");
        }

        [WorkItem(898452)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_InValidFolderNameNotMadeNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
namespace outer
{
    namespace inner
    {
        class Program
        {
            void Main()
            {
                [|Foo$$|] f;
            }
        }
    }
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
defaultNamespace: "ConsoleApplication",
expected: @"namespace ConsoleApplication
{
    public interface Foo
    {
    }
}",
isLine: false,
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using ConsoleApplication;

namespace outer
{
    namespace inner
    {
        class Program
        {
            void Main()
            {
                Foo f;
            }
        }
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
areFoldersValidIdentifiers: false,
newFileFolderContainers: new string[] { "123", "456" },
newFileName: "Test2.cs");
        }

        #endregion

        #endregion
        #region SameLanguageDifferentProject
        #region SameLanguageDifferentProject_ExistingFile
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectEmptyFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document FilePath=""Test2.cs"">
                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs",
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectExistingFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document Folders=""outer\inner"" FilePath=""Test2.cs"">
namespace A
{
    namespace B
    {
    }
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"
namespace A
{
    namespace B
    {
        public interface Foo
        {
        }
    }
}",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs",
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectExistingFile_Usings_Folders()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document Folders=""outer\inner"" FilePath=""Test2.cs"">
namespace A
{
    namespace B
    {
    }
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"
namespace A
{
    namespace B
    {
    }
}

namespace outer.inner
{
    public interface Foo
    {
    }
}",
isLine: false,
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs",
projectName: "Assembly2");
        }

        #endregion
        #region SameLanguageDifferentProject_NewFile
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectNewFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: Array.Empty<string>(),
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_Usings()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace outer.inner
{
    public interface Foo
    {
    }
}",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_Usings_DefaultNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace ConsoleApplication.outer.inner
{
    public interface Foo
    {
    }
}",
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using ConsoleApplication.outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName_DefaultNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"namespace A.B
{
    public interface Foo
    {
    }
}",
isLine: false,
defaultNamespace: "ConsoleApplication",
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }
        #endregion
        #endregion
        #region DifferentLanguage
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageNewFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace Global.A.B
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: Array.Empty<string>(),
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageNewFile_Folders_Usings()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace outer.inner
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageNewFile_Folders_NoUsings_NotSimpleName()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace Global.A.B
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageNewFile_Folders_Usings_RootNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <CompilationOptions RootNamespace=""BarBaz""/>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace outer.inner
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using BarBaz.outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageNewFile_Folders_NoUsings_NotSimpleName_RootNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <CompilationOptions RootNamespace=""BarBaz""/>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace Global.A.B
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageNewFile_Folders_NoUsings_NotSimpleName_RootNamespace_ProjectReference()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <CompilationOptions RootNamespace=""BarBaz""/>
                        <Document FilePath=""Test2.vb"">
                        Namespace A.B
                            Public Class Foo
                            End Class
                        End Namespace
                        </Document>
                    </Project>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <ProjectReference>Assembly2</ProjectReference>
                        <Document FilePath=""Test1.cs"">
using BarBaz.A;

class Program
{
    void Main()
    {
        [|BarBaz.A.B.Bar$$|] f;
    }
}</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: @"Namespace A.B
    Public Class Bar
    End Class
End Namespace
",
isLine: false,
defaultNamespace: "ConsoleApplication",
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test3.vb",
newFileFolderContainers: new string[] { "outer", "inner" },
projectName: "Assembly2");
        }

        [WorkItem(858826)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageNewFileAdjustFileExtension()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace Global.A.B
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: Array.Empty<string>(),
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageExistingEmptyFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document Folders=""outer\inner"" FilePath=""Test2.vb"">
                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace Global.A.B
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");
        }

        [WorkItem(850101)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageExistingEmptyFile_Usings_Folder()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|Foo$$|] f;
    }
}</Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document Folders=""outer\inner"" FilePath=""Test2.vb"">
                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace outer.inner
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
checkIfUsingsIncluded: true,
expectedTextWithUsings: @"
using outer.inner;

class Program
{
    void Main()
    {
        Foo f;
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageExistingNonEmptyFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document FilePath=""Test2.vb"">
Namespace A
End Namespace
                        </Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"
Namespace A
End Namespace

Namespace Global.A.B
    Public Class Foo
    End Class
End Namespace
",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeIntoDifferentLanguageExistingNonEmptyTargetFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.B.Foo$$|] f;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                        <Document FilePath=""Test2.vb"">
Namespace Global
    Namespace A
        Namespace C
        End Namespace
        Namespace B
        End Namespace
    End Namespace
End Namespace</Document>
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"
Namespace Global
    Namespace A
        Namespace C
        End Namespace
        Namespace B
            Public Class Foo
            End Class
        End Namespace
    End Namespace
End Namespace",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");
        }

        [WorkItem(861362)]
        [WorkItem(869593)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateModuleFromCSharpToVisualBasicInTypeContext()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        [|A.Foo$$|].Bar f;
    }
}
namespace A
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace Global.A
    Public Module Foo
    End Module
End Namespace
",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Module,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: Array.Empty<string>(),
projectName: "Assembly2",
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Module));
        }

        #endregion
        #region Bugfix 
        [WorkItem(861462)]
        [WorkItem(873066)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithProperAccessibilityAndTypeKind_1()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
public class C : [|$$D|]
{
}",
languageName: LanguageNames.CSharp,
typeName: "D",
expected: @"
public class C : D
{
}

public class D
{
}",
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList, false));
        }

        [WorkItem(861462)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithProperAccessibilityAndTypeKind_2()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"public interface CCC : [|$$DDD|]
{
}",
languageName: LanguageNames.CSharp,
typeName: "DDD",
expected: @"public interface CCC : DDD
{
}

public interface DDD
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Interface, false));
        }

        [WorkItem(861462)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithProperAccessibilityAndTypeKind_3()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"public struct CCC : [|$$DDD|]
{
}",
languageName: LanguageNames.CSharp,
typeName: "DDD",
expected: @"public struct CCC : DDD
{
}

public interface DDD
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Interface, false));
        }

        [WorkItem(861362)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInMemberAccessExpression()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s = [|$$A.B|];
    }
}",
languageName: LanguageNames.CSharp,
typeName: "A",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s = A.B;
    }
}

public class A
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));
        }

        [WorkItem(861362)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInMemberAccessExpressionInNamespace()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s = [|$$A.B.C|];
    }
}

namespace A
{
}",
languageName: LanguageNames.CSharp,
typeName: "B",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s = A.B.C;
    }
}

namespace A
{
    public class B
    {
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));
        }

        [WorkItem(861600)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithoutEnumForGenericsInMemberAccess()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s = [|$$Foo<Bar>|].D;
    }
}

class Bar
{
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s = Foo<Bar>.D;
    }
}

class Bar
{
}

public class Foo<T>
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure));
        }

        [WorkItem(861600)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithoutEnumForGenericsInNameContext()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        [|$$Foo<Bar>|] baz;
    }
}

internal class Bar
{
}",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"class Program
{
    static void Main(string[] args)
    {
        Foo<Bar> baz;
    }
}

internal class Bar
{
}

public class Foo<T>
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Interface | TypeKindOptions.Delegate));
        }

        [WorkItem(861600)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInMemberAccessWithNSForModule()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s = [|Foo.$$Bar|].Baz;
    }
}

namespace Foo
{
}",
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s = Foo.Bar.Baz;
    }
}

namespace Foo
{
    public class Bar
    {
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));
        }

        [WorkItem(861600)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInMemberAccessWithGlobalNSForModule()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s = [|$$Bar|].Baz;
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s = Bar.Baz;
    }
}

public class Bar
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));
        }

        [WorkItem(861600)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeInMemberAccessWithoutNS()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s = [|$$Bar|].Baz;
    }
}

namespace Bar
{
}",
languageName: LanguageNames.CSharp,
typeName: "Bar",
isMissing: true);
        }

        [WorkItem(876202)]
        [WorkItem(883531)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_NoParameterLessConstructorForStruct()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s = new [|$$Bar|]();
    }
}",
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s = new Bar();
    }
}

public struct Bar
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Structure,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure, false));
        }
        #endregion
        #region Delegates
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_MethodGroup()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = new [|$$MyD|](foo);
    }
    static void foo()
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = new MyD(foo);
    }
    static void foo()
    {
    }
}

public delegate void MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_MethodGroup_Generics()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = new [|$$MyD|](foo);
    }
    static void foo<T>()
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = new MyD(foo);
    }
    static void foo<T>()
    {
    }
}

public delegate void MyD<T>();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_Delegate()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        MyD1 d = null;
        var s1 = new [|$$MyD2|](d);
    }
    public delegate object MyD1();
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        MyD1 d = null;
        var s1 = new MyD2(d);
    }
    public delegate object MyD1();
}

public delegate object MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_Action()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Action<int> action1 = null;
        var s3 = new [|$$MyD|](action1);
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Action<int> action1 = null;
        var s3 = new MyD(action1);
    }
}

public delegate void MyD(int obj);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_Func()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Func<int> lambda = () => { return 0; };
        var s4 = new [|$$MyD|](lambda);
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Func<int> lambda = () => { return 0; };
        var s4 = new MyD(lambda);
    }
}

public delegate int MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_ParenLambda()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s5 = new [|$$MyD|]((int n) => { return n; });
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s5 = new MyD((int n) => { return n; });
    }
}

public delegate int MyD(int n);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_SimpleLambda()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s6 = new [|$$MyD|](n => { return n; });
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s6 = new MyD(n => { return n; });
    }
}

public delegate void MyD(object n);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));
        }

        [WorkItem(872935)]
        [Fact(Skip = "872935"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_ObjectCreationExpression_SimpleLambdaEmpty()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s7 = new [|$$MyD3|](() => { });
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s7 = new MyD3(() => { });
    }
}

public delegate void MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_VarDecl_MethodGroup()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        [|$$MyD|] z1 = foo;
    }
    static void foo()
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        MyD z1 = foo;
    }
    static void foo()
    {
    }
}

public delegate void MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_VarDecl_Delegate()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        MyD1 temp = null;
        [|$$MyD|] z2 = temp; // Still Error
    }
}

public delegate object MyD1();",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        MyD1 temp = null;
        MyD z2 = temp; // Still Error
    }
}

public delegate object MyD1();

public delegate object MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_VarDecl_Action()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Action<int, int, int> action2 = null;
        [|$$MyD|] z3 = action2; // Still Error
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Action<int, int, int> action2 = null;
        MyD z3 = action2; // Still Error
    }
}

public delegate void MyD(int arg1, int arg2, int arg3);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_VarDecl_Func()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Func<int> lambda2 = () => { return 0; };
        [|$$MyD|] z4 = lambda2; // Still Error
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Func<int> lambda2 = () => { return 0; };
        MyD z4 = lambda2; // Still Error
    }
}

public delegate int MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_VarDecl_ParenLambda()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        [|$$MyD|] z5 = (int n) => { return n; };
    }
}
",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        MyD z5 = (int n) => { return n; };
    }
}

public delegate int MyD(int n);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_VarDecl_SimpleLambda()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        [|$$MyD|] z6 = n => { return n; };
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        MyD z6 = n => { return n; };
    }
}

public delegate void MyD(object n);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_Cast_MethodGroup()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var zz1 = ([|$$MyD|])foo;
    }
    static void foo()
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var zz1 = (MyD)foo;
    }
    static void foo()
    {
    }
}

public delegate void MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_Cast_Delegate()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        MyDDD temp1 = null;
        var zz2 = ([|$$MyD|])temp1; // Still Error
    }
}

public delegate object MyDDD();",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        MyDDD temp1 = null;
        var zz2 = (MyD)temp1; // Still Error
    }
}

public delegate object MyDDD();

public delegate object MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_Cast_Action()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Action<int, int, int> action3 = null;
        var zz3 = ([|$$MyD|])action3; // Still Error
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Action<int, int, int> action3 = null;
        var zz3 = (MyD)action3; // Still Error
    }
}

public delegate void MyD(int arg1, int arg2, int arg3);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_Cast_Func()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Func<int> lambda3 = () => { return 0; };
        var zz4 = ([|$$MyD|])lambda3; // Still Error
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"using System;
class Program
{
    static void Main(string[] args)
    {
        Func<int> lambda3 = () => { return 0; };
        var zz4 = (MyD)lambda3; // Still Error
    }
}

public delegate int MyD();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_Cast_ParenLambda()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var zz5 = ([|$$MyD|])((int n) => { return n; });
    }
}
",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var zz5 = (MyD)((int n) => { return n; });
    }
}

public delegate int MyD(int n);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_Cast_SimpleLambda()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var zz6 = ([|$$MyD|])(n => { return n; });
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var zz6 = (MyD)(n => { return n; });
    }
}

public delegate void MyD(object n);
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateTypeIntoDifferentLanguageNewFile()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"<Workspace>
                    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
                        <Document FilePath=""Test1.cs"">
class Program
{
    void Main()
    {
        var f = ([|A.B.Foo$$|])Main;
    }
}
namespace A.B
{
}
                        </Document>
                    </Project>
                    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"">
                    </Project>
                </Workspace>",
languageName: LanguageNames.CSharp,
typeName: "Foo",
expected: @"Namespace Global.A.B
    Public Delegate Sub Foo()
End Namespace
",
isLine: false,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: Array.Empty<string>(),
projectName: "Assembly2");
        }

        [WorkItem(860210)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_NoInfo()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        [|$$MyD<int>|] d;
    }
}",
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: @"class Program
{
    static void Main(string[] args)
    {
        MyD<int> d;
    }
}

public delegate void MyD<T>();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false);
        }
        #endregion 
        #region Dev12Filtering
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_NoEnum_InvocationExpression_0()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = [|$$B|].C();
    }
}",
languageName: LanguageNames.CSharp,
typeName: "B",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = B.C();
    }
}

public class B
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertTypeKindAbsent: new[] { TypeKindOptions.Enum });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateDelegateType_NoEnum_InvocationExpression_1()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = [|A.$$B|].C();
    }
}

namespace A
{
}",
languageName: LanguageNames.CSharp,
typeName: "B",
expected: @"class Program
{
    static void Main(string[] args)
    {
        var s2 = A.B.C();
    }
}

namespace A
{
    public class B
    {
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertTypeKindAbsent: new[] { TypeKindOptions.Enum });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_TypeConstraint_1()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
    }
}

public class F<T> where T : [|$$Bar|] 
{
}
",
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: @"class Program
{
    static void Main(string[] args)
    {
    }
}

public class F<T> where T : Bar 
{
}

public class Bar
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_TypeConstraint_2()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
    }
}

class outer
{
    public class F<T> where T : [|$$Bar|] 
    {
    }
}
",
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: @"class Program
{
    static void Main(string[] args)
    {
    }
}

class outer
{
    public class F<T> where T : Bar 
    {
    }
}

public class Bar
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.BaseList));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_TypeConstraint_3()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"class Program
{
    static void Main(string[] args)
    {
    }
}

public class outerOuter
{
    public class outer
    {
        public class F<T> where T : [|$$Bar|]
        {
        }
    }
}
",
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: @"class Program
{
    static void Main(string[] args)
    {
    }
}

public class outerOuter
{
    public class outer
    {
        public class F<T> where T : Bar
        {
        }
    }
}

public class Bar
{
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithProperAccessibilityWithNesting_1()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
public class B
{
    public class C : [|$$D|]
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "D",
expected: @"
public class B
{
    public class C : D
    {
    }
}

public class D
{
}",
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList, false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithProperAccessibilityWithNesting_2()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class B
{
    public class C : [|$$D|]
    {
    }
}",
languageName: LanguageNames.CSharp,
typeName: "D",
expected: @"
class B
{
    public class C : D
    {
    }
}

public class D
{
}",
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.BaseList, false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateTypeWithProperAccessibilityWithNesting_3()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class A
{
    public class B
    {
        public class C : [|$$D|]
        {
        }
    }
}",
languageName: LanguageNames.CSharp,
typeName: "D",
expected: @"
class A
{
    public class B
    {
        public class C : D
        {
        }
    }
}

public class D
{
}",
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.BaseList, false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_1()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class A
{
    event [|$$foo|] name1
    {
        add { }
        remove { }
    }
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
class A
{
    event foo name1
    {
        add { }
        remove { }
    }
}

public delegate void foo();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_2()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class A
{
    public event [|$$foo|] name2;
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
class A
{
    public event foo name2;
}

public delegate void foo();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_3()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class A
{
    event [|NS.foo$$|] name1
    {
        add { }
        remove { }
    }
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
class A
{
    event NS.foo name1
    {
        add { }
        remove { }
    }
}

namespace NS
{
    public delegate void foo();
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_4()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class A
{
    public event [|NS.foo$$|] name2;
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
class A
{
    public event NS.foo name2;
}

namespace NS
{
    public delegate void foo();
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_5()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class A
{
    event [|$$NS.foo.Mydel|] name1
    {
        add { }
        remove { }
    }
}

namespace NS
{
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
class A
{
    event NS.foo.Mydel name1
    {
        add { }
        remove { }
    }
}

namespace NS
{
    public class foo
    {
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Module));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_6()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
class A
{
    public event [|$$NS.foo.Mydel|] name2;
}

namespace NS
{
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
class A
{
    public event NS.foo.Mydel name2;
}

namespace NS
{
    public class foo
    {
    }
}",
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Module));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_7()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
public class A
{
    public event [|$$foo|] name1
    {
        add { }
        remove { }
    }
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
public class A
{
    public event foo name1
    {
        add { }
        remove { }
    }
}

public delegate void foo();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Delegate));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GenerateType_Event_8()
        {
            await TestWithMockedGenerateTypeDialog(
initial: @"
public class outer
{
    public class A
    {
        public event [|$$foo|] name1
        {
            add { }
            remove { }
        }
    }
}",
languageName: LanguageNames.CSharp,
typeName: "foo",
expected: @"
public class outer
{
    public class A
    {
        public event foo name1
        {
            add { }
            remove { }
        }
    }
}

public delegate void foo();
",
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Delegate));
        }
        #endregion
    }
}
