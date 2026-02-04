// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateTypeTests;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
public partial class GenerateTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
{
    #region SameProject
    #region SameProject_SameFile 
    [Fact]
    public Task GenerateTypeDefaultValues()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }

            class Goo
            {
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeInsideNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.Goo$$|] f;
                }
            }

            namespace A
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.Goo f;
                }
            }

            namespace A
            {
                class Goo
                {
                }
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeInsideFileScopedNamespace1()
        => TestWithMockedGenerateTypeDialog(
initial: """

            namespace A;

            class Program
            {
                void Main()
                {
                    [|A.Goo$$|] f;
                }
            }

            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """

            namespace A;

            class Program
            {
                void Main()
                {
                    A.Goo f;
                }
            }

            class Goo
            {
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeInsideFileScopedNamespace2()
        => TestWithMockedGenerateTypeDialog(
initial: """

            namespace A;

            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }

            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """

            namespace A;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }

            class Goo
            {
            }
            """,
isNewFile: false);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/82038")]
    public Task GenerateTypeInsideFileScopedNamespace_WithDifferentDefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """

            namespace Foo;

            class C([|D$$|] _);

            """,
languageName: LanguageNames.CSharp,
typeName: "D",
expected: """

            namespace Foo;

            class C(D _);

            class D
            {
            }
            """,
defaultNamespace: "Roslyntest",
isNewFile: false);

    [Fact]
    public Task GenerateTypeInsideQualifiedNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.B.Goo f;
                }
            }
            namespace A.B
            {
                class Goo
                {
                }
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeWithinQualifiedNestedNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.B.C.Goo$$|] f;
                }
            }
            namespace A.B
            {
                namespace C
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.B.C.Goo f;
                }
            }
            namespace A.B
            {
                namespace C
                {
                    class Goo
                    {
                    }
                }
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeWithinNestedQualifiedNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.B.C.Goo$$|] f;
                }
            }
            namespace A
            {
                namespace B.C
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.B.C.Goo f;
                }
            }
            namespace A
            {
                namespace B.C
                {
                    class Goo
                    {
                    }
                }
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeWithConstructorMembers()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var f = new [|$$Goo|](bar: 1, baz: 2);
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var f = new Goo(bar: 1, baz: 2);
                }
            }

            class Goo
            {
                private int bar;
                private int baz;

                public Goo(int bar, int baz)
                {
                    this.bar = bar;
                    this.baz = baz;
                }
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeWithBaseTypes()
        => TestWithMockedGenerateTypeDialog(
initial: """
            using System.Collections.Generic;
            class Program
            {
                static void Main(string[] args)
                {
                    List<int> f = new [|$$Goo|]();
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            using System.Collections.Generic;
            class Program
            {
                static void Main(string[] args)
                {
                    List<int> f = new Goo();
                }
            }

            class Goo : List<int>
            {
            }
            """,
isNewFile: false);

    [Fact]
    public Task GenerateTypeWithPublicInterface()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.B.C.Goo$$|] f;
                }
            }
            namespace A
            {
                namespace B.C
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.B.C.Goo f;
                }
            }
            namespace A
            {
                namespace B.C
                {
                    public interface Goo
                    {
                    }
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false);

    [Fact]
    public Task GenerateTypeWithInternalStruct()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.B.C.Goo$$|] f;
                }
            }
            namespace A
            {
                namespace B.C
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.B.C.Goo f;
                }
            }
            namespace A
            {
                namespace B.C
                {
                    internal struct Goo
                    {
                    }
                }
            }
            """,
accessibility: Accessibility.Internal,
typeKind: TypeKind.Struct,
isNewFile: false);

    [Fact]
    public Task GenerateTypeWithDefaultEnum()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A
            {
                namespace B
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.B.Goo f;
                }
            }
            namespace A
            {
                namespace B
                {
                    enum Goo
                    {
                    }
                }
            }
            """,
accessibility: Accessibility.NotApplicable,
typeKind: TypeKind.Enum,
isNewFile: false);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeWithDefaultEnum_DefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            using ConsoleApplication;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }

            namespace ConsoleApplication
            {
                enum Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
accessibility: Accessibility.NotApplicable,
typeKind: TypeKind.Enum,
isNewFile: false);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeWithDefaultEnum_DefaultNamespace_NotSimpleName()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A
            {
                namespace B
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                void Main()
                {
                    A.B.Goo f;
                }
            }
            namespace A
            {
                namespace B
                {
                    enum Goo
                    {
                    }
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
accessibility: Accessibility.NotApplicable,
typeKind: TypeKind.Enum,
isNewFile: false);
    #endregion

    // Working is very similar to the adding to the same file
    #region SameProject_ExistingFile
    [Fact]
    public Task GenerateTypeInExistingEmptyFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                    <Document FilePath="Test2.cs">

                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeInExistingEmptyFile_Usings_Folders()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                    <Document Folders= "outer\inner" FilePath="Test2.cs">

                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeInExistingEmptyFile_Usings_DefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                    <Document FilePath="Test2.cs">

                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace ConsoleApplication
            {
                public interface Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using ConsoleApplication;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeInExistingEmptyFile_Usings_Folders_DefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                    <Document Folders= "outer\inner" FilePath="Test2.cs">

                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace ConsoleApplication.outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using ConsoleApplication.outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeInExistingEmptyFile_NoUsings_Folders_NotSimpleName()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                    <Document FilePath="Test2.cs" Folders= "outer\inner">

                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs");
    #endregion

    #region SameProject_NewFile
    [WpfFact]
    public Task GenerateTypeInNewFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: [],
newFileName: "Test2.cs");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateType_UsingsNotNeeded_InNewFile_InFolder()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            namespace outer
            {
                namespace inner
                {
                    class Program
                    {
                        void Main()
                        {
                            [|Goo$$|] f;
                        }
                    }
                }
            }
                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: ["outer", "inner"],
newFileName: "Test2.cs");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateType_UsingsNeeded_InNewFile_InFolder()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: ["outer", "inner"],
newFileName: "Test2.cs");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateType_UsingsNotNeeded_InNewFile_InFolder_NotSimpleName()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: ["outer", "inner"],
newFileName: "Test2.cs");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateType_UsingsNeeded_InNewFile_InFolder_DefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace ConsoleApplication.outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using ConsoleApplication.outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: ["outer", "inner"],
newFileName: "Test2.cs");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateType_UsingsNotNeeded_InNewFile_InFolder_DefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            namespace ConsoleApplication.outer
            {
                class Program
                {
                    void Main()
                    {
                        [|Goo$$|] f;
                    }
                }
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace ConsoleApplication.outer
            {
                public interface Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            namespace ConsoleApplication.outer
            {
                class Program
                {
                    void Main()
                    {
                        Goo f;
                    }
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: ["outer"],
newFileName: "Test2.cs");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateType_UsingsNotNeeded_InNewFile_InFolder_DefaultNamespace_NotSimpleName()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }

            namespace A.B
            {
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            class Program
            {
                void Main()
                {
                    A.B.Goo f;
                }
            }

            namespace A.B
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileFolderContainers: ["outer"],
newFileName: "Test2.cs");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898452")]
    public Task GenerateType_InValidFolderNameNotMadeNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            namespace outer
            {
                namespace inner
                {
                    class Program
                    {
                        void Main()
                        {
                            [|Goo$$|] f;
                        }
                    }
                }
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
defaultNamespace: "ConsoleApplication",
expected: """
            namespace ConsoleApplication
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using ConsoleApplication;

            namespace outer
            {
                namespace inner
                {
                    class Program
                    {
                        void Main()
                        {
                            Goo f;
                        }
                    }
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
areFoldersValidIdentifiers: false,
newFileFolderContainers: ["123", "456"],
newFileName: "Test2.cs");

    #endregion

    #endregion
    #region SameLanguageDifferentProject
    #region SameLanguageDifferentProject_ExistingFile
    [Fact]
    public Task GenerateTypeIntoSameLanguageDifferentProjectEmptyFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                    <Document FilePath="Test2.cs">
                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs",
projectName: "Assembly2");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoSameLanguageDifferentProjectExistingFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                    <Document Folders="outer\inner" FilePath="Test2.cs">
            namespace A
            {
                namespace B
                {
                }
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """

            namespace A
            {
                namespace B
                {
                    public interface Goo
                    {
                    }
                }
            }
            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs",
projectName: "Assembly2");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoSameLanguageDifferentProjectExistingFile_Usings_Folders()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                    <Document Folders="outer\inner" FilePath="Test2.cs">
            namespace A
            {
                namespace B
                {
                }
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """

            namespace A
            {
                namespace B
                {
                }
            }

            namespace outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
existingFilename: "Test2.cs",
projectName: "Assembly2");

    #endregion
    #region SameLanguageDifferentProject_NewFile
    [WpfFact]
    public Task GenerateTypeIntoSameLanguageDifferentProjectNewFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: [],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_Usings()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_Usings_DefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace ConsoleApplication.outer.inner
            {
                public interface Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using ConsoleApplication.outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName_DefaultNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            namespace A.B
            {
                public interface Goo
                {
                }
            }
            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: true,
newFileName: "Test2.cs",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");
    #endregion
    #endregion
    #region DifferentLanguage
    [WpfFact]
    public Task GenerateTypeIntoDifferentLanguageNewFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace Global.A.B
                Public Class Goo
                End Class
            End Namespace

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: [],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoDifferentLanguageNewFile_Folders_Usings()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace outer.inner
                Public Class Goo
                End Class
            End Namespace

            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoDifferentLanguageNewFile_Folders_NoUsings_NotSimpleName()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace Global.A.B
                Public Class Goo
                End Class
            End Namespace

            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoDifferentLanguageNewFile_Folders_Usings_RootNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                    <CompilationOptions RootNamespace="BarBaz"/>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace outer.inner
                Public Class Goo
                End Class
            End Namespace

            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using BarBaz.outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoDifferentLanguageNewFile_Folders_NoUsings_NotSimpleName_RootNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                    <CompilationOptions RootNamespace="BarBaz"/>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace Global.A.B
                Public Class Goo
                End Class
            End Namespace

            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoDifferentLanguageNewFile_Folders_NoUsings_NotSimpleName_RootNamespace_ProjectReference()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                    <CompilationOptions RootNamespace="BarBaz"/>
                                    <Document FilePath="Test2.vb">
                                    Namespace A.B
                                        Public Class Goo
                                        End Class
                                    End Namespace
                                    </Document>
                                </Project>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <ProjectReference>Assembly2</ProjectReference>
                                    <Document FilePath="Test1.cs">
            using BarBaz.A;

            class Program
            {
                void Main()
                {
                    [|BarBaz.A.B.Bar$$|] f;
                }
            }</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: """
            Namespace A.B
                Public Class Bar
                End Class
            End Namespace

            """,
defaultNamespace: "ConsoleApplication",
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test3.vb",
newFileFolderContainers: ["outer", "inner"],
projectName: "Assembly2");

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858826")]
    public Task GenerateTypeIntoDifferentLanguageNewFileAdjustFileExtension()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace Global.A.B
                Public Class Goo
                End Class
            End Namespace

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: [],
projectName: "Assembly2");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoDifferentLanguageExistingEmptyFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                    <Document Folders="outer\inner" FilePath="Test2.vb">
                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace Global.A.B
                Public Class Goo
                End Class
            End Namespace

            """,
checkIfUsingsNotIncluded: true,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")]
    public Task GenerateTypeIntoDifferentLanguageExistingEmptyFile_Usings_Folder()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|Goo$$|] f;
                }
            }</Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                    <Document Folders="outer\inner" FilePath="Test2.vb">
                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace outer.inner
                Public Class Goo
                End Class
            End Namespace

            """,
checkIfUsingsIncluded: true,
expectedTextWithUsings: """

            using outer.inner;

            class Program
            {
                void Main()
                {
                    Goo f;
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");

    [Fact]
    public Task GenerateTypeIntoDifferentLanguageExistingNonEmptyFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                    <Document FilePath="Test2.vb">
            Namespace A
            End Namespace
                                    </Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """

            Namespace A
            End Namespace

            Namespace Global.A.B
                Public Class Goo
                End Class
            End Namespace

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");

    [Fact]
    public Task GenerateTypeIntoDifferentLanguageExistingNonEmptyTargetFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.B.Goo$$|] f;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                    <Document FilePath="Test2.vb">
            Namespace Global
                Namespace A
                    Namespace C
                    End Namespace
                    Namespace B
                    End Namespace
                End Namespace
            End Namespace</Document>
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """

            Namespace Global
                Namespace A
                    Namespace C
                    End Namespace
                    Namespace B
                        Public Class Goo
                        End Class
                    End Namespace
                End Namespace
            End Namespace
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
existingFilename: "Test2.vb",
projectName: "Assembly2");

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")]
    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869593")]
    public Task GenerateModuleFromCSharpToVisualBasicInTypeContext()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    [|A.Goo$$|].Bar f;
                }
            }
            namespace A
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace Global.A
                Public Module Goo
                End Module
            End Namespace

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Module,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: [],
projectName: "Assembly2",
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Module));

    #endregion
    #region Bugfix 
    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/873066")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")]
    public Task GenerateTypeWithProperAccessibilityAndTypeKind_1()
        => TestWithMockedGenerateTypeDialog(
initial: """

            public class C : [|$$D|]
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "D",
expected: """

            public class C : D
            {
            }

            public class D
            {
            }
            """,
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList, false));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")]
    public Task GenerateTypeWithProperAccessibilityAndTypeKind_2()
        => TestWithMockedGenerateTypeDialog(
initial: """
            public interface CCC : [|$$DDD|]
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "DDD",
expected: """
            public interface CCC : DDD
            {
            }

            public interface DDD
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Interface, false));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")]
    public Task GenerateTypeWithProperAccessibilityAndTypeKind_3()
        => TestWithMockedGenerateTypeDialog(
initial: """
            public struct CCC : [|$$DDD|]
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "DDD",
expected: """
            public struct CCC : DDD
            {
            }

            public interface DDD
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Interface, false));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")]
    public Task GenerateTypeInMemberAccessExpression()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = [|$$A.B|];
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "A",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = A.B;
                }
            }

            public class A
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")]
    public Task GenerateTypeInMemberAccessExpressionInNamespace()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = [|$$A.B.C|];
                }
            }

            namespace A
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "B",
expected: """
            class Program
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
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")]
    public Task GenerateTypeWithoutEnumForGenericsInMemberAccess()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = [|$$Goo<Bar>|].D;
                }
            }

            class Bar
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = Goo<Bar>.D;
                }
            }

            class Bar
            {
            }

            public class Goo<T>
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")]
    public Task GenerateTypeWithoutEnumForGenericsInNameContext()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    [|$$Goo<Bar>|] baz;
                }
            }

            internal class Bar
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    Goo<Bar> baz;
                }
            }

            internal class Bar
            {
            }

            public class Goo<T>
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Interface | TypeKindOptions.Delegate));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")]
    public Task GenerateTypeInMemberAccessWithNSForModule()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = [|Goo.$$Bar|].Baz;
                }
            }

            namespace Goo
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = Goo.Bar.Baz;
                }
            }

            namespace Goo
            {
                public class Bar
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")]
    public Task GenerateTypeInMemberAccessWithGlobalNSForModule()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = [|$$Bar|].Baz;
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = Bar.Baz;
                }
            }

            public class Bar
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.MemberAccessWithNamespace));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")]
    public Task GenerateTypeInMemberAccessWithoutNS()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = [|$$Bar|].Baz;
                }
            }

            namespace Bar
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
isMissing: true);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883531")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876202")]
    public Task GenerateType_NoParameterLessConstructorForStruct()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = new [|$$Bar|]();
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = new Bar();
                }
            }

            public struct Bar
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Structure,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure, false));

    [Fact, WorkItem(63280, "https://github.com/dotnet/roslyn/issues/63280")]
    public Task GenerateType_GenericBaseList()
        => TestWithMockedGenerateTypeDialog(
initial: """

            using System.Collections.Generic;

            struct C : IEnumerable<[|$$NewType|]>
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "NewType",
expected: """

            using System.Collections.Generic;

            struct C : IEnumerable<NewType>
            {
            }

            public class NewType
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions, false));

    [Fact]
    public Task GenerateType_QualifiedBaseList()
        => TestWithMockedGenerateTypeDialog(
initial: """

            using System.Collections.Generic;

            struct C : A.B.[|$$INewType|]
            {
            }

            namespace A.B
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "INewType",
expected: """

            using System.Collections.Generic;

            struct C : A.B.INewType
            {
            }

            namespace A.B
            {
                public interface INewType
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Interface, false));

    [Fact]
    public Task GenerateType_AliasQualifiedBaseList()
        => TestWithMockedGenerateTypeDialog(
initial: """

            using System.Collections.Generic;

            struct C : global::A.B.[|$$INewType|]
            {
            }

            namespace A.B
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "INewType",
expected: """

            using System.Collections.Generic;

            struct C : global::A.B.INewType
            {
            }

            namespace A.B
            {
                public interface INewType
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Interface,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Interface, false));
    #endregion
    #region Delegates
    [Fact]
    public Task GenerateDelegateType_ObjectCreationExpression_MethodGroup()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s2 = new [|$$MyD|](goo);
                }
                static void goo()
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s2 = new MyD(goo);
                }
                static void goo()
                {
                }
            }

            public delegate void MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateDelegateType_ObjectCreationExpression_MethodGroup_Generics()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s2 = new [|$$MyD|](goo);
                }
                static void goo<T>()
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s2 = new MyD(goo);
                }
                static void goo<T>()
                {
                }
            }

            public delegate void MyD<T>();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateDelegateType_ObjectCreationExpression_Delegate()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD1 d = null;
                    var s1 = new [|$$MyD2|](d);
                }
                public delegate object MyD1();
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD1 d = null;
                    var s1 = new MyD2(d);
                }
                public delegate object MyD1();
            }

            public delegate object MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateDelegateType_ObjectCreationExpression_Action()
        => TestWithMockedGenerateTypeDialog(
initial: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int> action1 = null;
                    var s3 = new [|$$MyD|](action1);
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int> action1 = null;
                    var s3 = new MyD(action1);
                }
            }

            public delegate void MyD(int obj);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateDelegateType_ObjectCreationExpression_Func()
        => TestWithMockedGenerateTypeDialog(
initial: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> lambda = () => { return 0; };
                    var s4 = new [|$$MyD|](lambda);
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> lambda = () => { return 0; };
                    var s4 = new MyD(lambda);
                }
            }

            public delegate int MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateDelegateType_ObjectCreationExpression_ParenLambda()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s5 = new [|$$MyD|]((int n) => { return n; });
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s5 = new MyD((int n) => { return n; });
                }
            }

            public delegate int MyD(int n);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateDelegateType_ObjectCreationExpression_SimpleLambda()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s6 = new [|$$MyD|](n => { return n; });
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s6 = new MyD(n => { return n; });
                }
            }

            public delegate void MyD(object n);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Delegate));

    [Fact(Skip = "872935")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/872935")]
    public Task GenerateDelegateType_ObjectCreationExpression_SimpleLambdaEmpty()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s7 = new [|$$MyD3|](() => { });
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s7 = new MyD3(() => { });
                }
            }

            public delegate void MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_VarDecl_MethodGroup()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    [|$$MyD|] z1 = goo;
                }
                static void goo()
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD z1 = goo;
                }
                static void goo()
                {
                }
            }

            public delegate void MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_VarDecl_Delegate()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD1 temp = null;
                    [|$$MyD|] z2 = temp; // Still Error
                }
            }

            public delegate object MyD1();
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD1 temp = null;
                    MyD z2 = temp; // Still Error
                }
            }

            public delegate object MyD1();

            public delegate object MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_VarDecl_Action()
        => TestWithMockedGenerateTypeDialog(
initial: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int, int, int> action2 = null;
                    [|$$MyD|] z3 = action2; // Still Error
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int, int, int> action2 = null;
                    MyD z3 = action2; // Still Error
                }
            }

            public delegate void MyD(int arg1, int arg2, int arg3);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_VarDecl_Func()
        => TestWithMockedGenerateTypeDialog(
initial: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> lambda2 = () => { return 0; };
                    [|$$MyD|] z4 = lambda2; // Still Error
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> lambda2 = () => { return 0; };
                    MyD z4 = lambda2; // Still Error
                }
            }

            public delegate int MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_VarDecl_ParenLambda()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    [|$$MyD|] z5 = (int n) => { return n; };
                }
            }

            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD z5 = (int n) => { return n; };
                }
            }

            public delegate int MyD(int n);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_VarDecl_SimpleLambda()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    [|$$MyD|] z6 = n => { return n; };
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD z6 = n => { return n; };
                }
            }

            public delegate void MyD(object n);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_Cast_MethodGroup()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var zz1 = ([|$$MyD|])goo;
                }
                static void goo()
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var zz1 = (MyD)goo;
                }
                static void goo()
                {
                }
            }

            public delegate void MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_Cast_Delegate()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyDDD temp1 = null;
                    var zz2 = ([|$$MyD|])temp1; // Still Error
                }
            }

            public delegate object MyDDD();
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyDDD temp1 = null;
                    var zz2 = (MyD)temp1; // Still Error
                }
            }

            public delegate object MyDDD();

            public delegate object MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_Cast_Action()
        => TestWithMockedGenerateTypeDialog(
initial: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int, int, int> action3 = null;
                    var zz3 = ([|$$MyD|])action3; // Still Error
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Action<int, int, int> action3 = null;
                    var zz3 = (MyD)action3; // Still Error
                }
            }

            public delegate void MyD(int arg1, int arg2, int arg3);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_Cast_Func()
        => TestWithMockedGenerateTypeDialog(
initial: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> lambda3 = () => { return 0; };
                    var zz4 = ([|$$MyD|])lambda3; // Still Error
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> lambda3 = () => { return 0; };
                    var zz4 = (MyD)lambda3; // Still Error
                }
            }

            public delegate int MyD();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_Cast_ParenLambda()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var zz5 = ([|$$MyD|])((int n) => { return n; });
                }
            }

            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var zz5 = (MyD)((int n) => { return n; });
                }
            }

            public delegate int MyD(int n);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [Fact]
    public Task GenerateDelegateType_Cast_SimpleLambda()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var zz6 = ([|$$MyD|])(n => { return n; });
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var zz6 = (MyD)(n => { return n; });
                }
            }

            public delegate void MyD(object n);

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.AllOptions));

    [WpfFact]
    public Task GenerateDelegateTypeIntoDifferentLanguageNewFile()
        => TestWithMockedGenerateTypeDialog(
initial: """
            <Workspace>
                                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                                    <Document FilePath="Test1.cs">
            class Program
            {
                void Main()
                {
                    var f = ([|A.B.Goo$$|])Main;
                }
            }
            namespace A.B
            {
            }
                                    </Document>
                                </Project>
                                <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                </Project>
                            </Workspace>
            """,
languageName: LanguageNames.CSharp,
typeName: "Goo",
expected: """
            Namespace Global.A.B
                Public Delegate Sub Goo()
            End Namespace

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: true,
newFileName: "Test2.vb",
newFileFolderContainers: [],
projectName: "Assembly2");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860210")]
    public Task GenerateDelegateType_NoInfo()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    [|$$MyD<int>|] d;
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "MyD",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    MyD<int> d;
                }
            }

            public delegate void MyD<T>();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false);
    #endregion 
    #region Dev12Filtering
    [Fact]
    public Task GenerateDelegateType_NoEnum_InvocationExpression_0()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s2 = [|$$B|].C();
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "B",
expected: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s2 = B.C();
                }
            }

            public class B
            {
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertTypeKindAbsent: [TypeKindOptions.Enum]);

    [Fact]
    public Task GenerateDelegateType_NoEnum_InvocationExpression_1()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                    var s2 = [|A.$$B|].C();
                }
            }

            namespace A
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "B",
expected: """
            class Program
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
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertTypeKindAbsent: [TypeKindOptions.Enum]);

    [Fact]
    public Task GenerateType_TypeConstraint_1()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
            {
                static void Main(string[] args)
                {
                }
            }

            public class F<T> where T : [|$$Bar|] 
            {
            }

            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: """
            class Program
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
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList));

    [Fact]
    public Task GenerateType_TypeConstraint_2()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
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

            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: """
            class Program
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
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.BaseList));

    [Fact]
    public Task GenerateType_TypeConstraint_3()
        => TestWithMockedGenerateTypeDialog(
initial: """
            class Program
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

            """,
languageName: LanguageNames.CSharp,
typeName: "Bar",
expected: """
            class Program
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
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList));

    [Fact]
    public Task GenerateTypeWithProperAccessibilityWithNesting_1()
        => TestWithMockedGenerateTypeDialog(
initial: """

            public class B
            {
                public class C : [|$$D|]
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "D",
expected: """

            public class B
            {
                public class C : D
                {
                }
            }

            public class D
            {
            }
            """,
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.BaseList, false));

    [Fact]
    public Task GenerateTypeWithProperAccessibilityWithNesting_2()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class B
            {
                public class C : [|$$D|]
                {
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "D",
expected: """

            class B
            {
                public class C : D
                {
                }
            }

            public class D
            {
            }
            """,
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.BaseList, false));

    [Fact]
    public Task GenerateTypeWithProperAccessibilityWithNesting_3()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class A
            {
                public class B
                {
                    public class C : [|$$D|]
                    {
                    }
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "D",
expected: """

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
            }
            """,
accessibility: Accessibility.Public,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.BaseList, false));

    [Fact]
    public Task GenerateType_Event_1()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class A
            {
                event [|$$goo|] name1
                {
                    add { }
                    remove { }
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            class A
            {
                event goo name1
                {
                    add { }
                    remove { }
                }
            }

            public delegate void goo();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateType_Event_2()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class A
            {
                public event [|$$goo|] name2;
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            class A
            {
                public event goo name2;
            }

            public delegate void goo();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateType_Event_3()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class A
            {
                event [|NS.goo$$|] name1
                {
                    add { }
                    remove { }
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            class A
            {
                event NS.goo name1
                {
                    add { }
                    remove { }
                }
            }

            namespace NS
            {
                public delegate void goo();
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateType_Event_4()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class A
            {
                public event [|NS.goo$$|] name2;
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            class A
            {
                public event NS.goo name2;
            }

            namespace NS
            {
                public delegate void goo();
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateType_Event_5()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class A
            {
                event [|$$NS.goo.Mydel|] name1
                {
                    add { }
                    remove { }
                }
            }

            namespace NS
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            class A
            {
                event NS.goo.Mydel name1
                {
                    add { }
                    remove { }
                }
            }

            namespace NS
            {
                public class goo
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Module));

    [Fact]
    public Task GenerateType_Event_6()
        => TestWithMockedGenerateTypeDialog(
initial: """

            class A
            {
                public event [|$$NS.goo.Mydel|] name2;
            }

            namespace NS
            {
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            class A
            {
                public event NS.goo.Mydel name2;
            }

            namespace NS
            {
                public class goo
                {
                }
            }
            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Class,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(false, TypeKindOptions.Class | TypeKindOptions.Structure | TypeKindOptions.Module));

    [Fact]
    public Task GenerateType_Event_7()
        => TestWithMockedGenerateTypeDialog(
initial: """

            public class A
            {
                public event [|$$goo|] name1
                {
                    add { }
                    remove { }
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            public class A
            {
                public event goo name1
                {
                    add { }
                    remove { }
                }
            }

            public delegate void goo();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Delegate));

    [Fact]
    public Task GenerateType_Event_8()
        => TestWithMockedGenerateTypeDialog(
initial: """

            public class outer
            {
                public class A
                {
                    public event [|$$goo|] name1
                    {
                        add { }
                        remove { }
                    }
                }
            }
            """,
languageName: LanguageNames.CSharp,
typeName: "goo",
expected: """

            public class outer
            {
                public class A
                {
                    public event goo name1
                    {
                        add { }
                        remove { }
                    }
                }
            }

            public delegate void goo();

            """,
accessibility: Accessibility.Public,
typeKind: TypeKind.Delegate,
isNewFile: false,
assertGenerateTypeDialogOptions: new GenerateTypeDialogOptions(true, TypeKindOptions.Delegate));
    #endregion
}
