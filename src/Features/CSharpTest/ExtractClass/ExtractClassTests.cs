// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ExtractClass;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractClass;

[UseExportProvider]
public class ExtractClassTests
{
    private class Test : CSharpCodeRefactoringVerifier<CSharpExtractClassCodeRefactoringProvider>.Test
    {
        public IEnumerable<(string name, bool makeAbstract)>? DialogSelection { get; set; }
        public bool SameFile { get; set; }
        public bool IsClassDeclarationSelection { get; set; }
        public string FileName { get; set; } = "/0/Test1.cs";
        public string WorkspaceKind { get; set; } = CodeAnalysis.WorkspaceKind.Host;

        protected override IEnumerable<CodeRefactoringProvider> GetCodeRefactoringProviders()
        {
            var service = new TestExtractClassOptionsService(DialogSelection, SameFile, IsClassDeclarationSelection)
            {
                FileName = FileName
            };

            return [new CSharpExtractClassCodeRefactoringProvider(service)];
        }

        protected override Task<Workspace> CreateWorkspaceImplAsync()
        {
            var unusedCompilationOptions = new CSharpCompilationOptions(OutputKind.NetModule);
            var unusedParseOptions = new CSharpParseOptions(LanguageVersion.CSharp1);
            return Task.FromResult<Workspace>(TestWorkspace.Create(WorkspaceKind, LanguageNames.CSharp, unusedCompilationOptions, unusedParseOptions));
        }
    }

    [Fact]
    public async Task TestSingleMethod()
    {
        var input = """
            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestErrorBaseMethod()
    {
        var input = """
            class ErrorBase
            {
            }

            class Test : ErrorBase
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;
        await new Test
        {
            TestCode = input,
            FixedCode = input,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMiscellaneousFiles()
    {
        var input = """
            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            WorkspaceKind = WorkspaceKind.MiscellaneousFiles
        }.RunAsync();
    }

    [Fact]
    public async Task TestPartialClass()
    {
        var input1 = """
            partial class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;
        var input2 = """
            partial class Test
            {
                int Method2()
                {
                    return 5;
                }
            }
            """;

        var expected1 = """
            partial class Test : MyBase
            {
            }
            """;
        var expected2 = """
            partial class Test
            {
            }
            """;
        var expected3 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
                int Method2()
                {
                    return 5;
                }
            }
            """;

        await new Test
        {
            TestState =
            {
                Sources =
                {
                    input1,
                    input2,
                }
            },
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                    expected3,
                }
            },
            FileName = "Test2.cs",
            DialogSelection = MakeSelection("Method", "Method2")
        }.RunAsync();
    }

    [Fact]
    public async Task TestRecord_Method()
    {
        var input = """
            record R(string S)
            {
                void $$M()
                {
                }
            }
            """;

        var expected1 = """
            record R(string S) : MyBase
            {
            }
            """;

        var expected2 = """
            internal record MyBase
            {
                void M()
                {
                }
            }

            """;

        await new Test
        {
            TestCode = input,
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClass_Method()
    {
        var input = """
            class R(string S)
            {
                void $$M()
                {
                }
            }
            """;

        var expected1 = """
            class R(string S) : MyBase
            {
            }
            """;

        var expected2 = """
            internal class MyBase
            {
                void M()
                {
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestRecord_Property()
    {
        var input = """
            record R
            {
                public string $$S { get; set; }
            }
            """;

        var expected1 = """
            record R : MyBase
            {
            }
            """;

        var expected2 = """
            internal record MyBase
            {
                public string S { get; set; }
            }

            """;

        await new Test
        {
            TestCode = input,
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/62415")]
    public async Task TestRecord_PropertyAndImplicitField()
    {
        var input = """
            record R(string S)
            {
                public string $$S { get; set; } = S;
            }
            """;

        var expected1 = """
            record R(string S) : MyBase(S)
            {
            }
            """;

        var expected2 = """
            record MyBase(string S)
            {
                public string S { get; set; } = S;
            }

            """;

        await new Test
        {
            TestCode = input,
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
        }.RunAsync();
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/62415")]
    public async Task TestClass_PropertyAndImplicitField()
    {
        var input = """
            class R(string S)
            {
                public string $$S { get; set; } = S;
            }
            """;

        var expected1 = """
            class R(string S) : MyBase(S)
            {
            }
            """;

        var expected2 = """
            class MyBase(string S)
            {
                public string S { get; set; } = S;
            }
            """;

        await new Test
        {
            TestCode = input,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestRecordParam()
    {
        // https://github.com/dotnet/roslyn/issues/62415 to make this scenario work
        var input = """
            record R(string $$S)
            {
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassParam1()
    {
        var input = """
            class R(string $$S)
            {
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassParam2()
    {
        var input = """
            class R(string $$S);
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task TestStructParam1()
    {
        var input = """
            struct R(string $$S)
            {
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task TestStructParam2()
    {
        var input = """
            struct R(string $$S);
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task TestRecordStruct()
    {
        var input = """
            record struct R(string S)
            {
                void $$M()
                {
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp10,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task TestStruct()
    {
        var input = """
            struct R(string S)
            {
                void $$M()
                {
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInNamespace()
    {
        var input = """
            namespace MyNamespace
            {
                class Test
                {
                    int [||]Method()
                    {
                        return 1 + 1;
                    }
                }
            }
            """;

        var expected1 = """
            namespace MyNamespace
            {
                class Test : MyBase
                {
                }
            }
            """;
        var expected2 = """
            namespace MyNamespace
            {
                internal class MyBase
                {
                    int Method()
                    {
                        return 1 + 1;
                    }
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestInNamespace_FileScopedNamespace1()
    {
        var input = """
            namespace MyNamespace
            {
                class Test
                {
                    int [||]Method()
                    {
                        return 1 + 1;
                    }
                }
            }
            """;

        var expected1 = """
            namespace MyNamespace
            {
                class Test : MyBase
                {
                }
            }
            """;
        var expected2 = """
            namespace MyNamespace;

            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Silent }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestInNamespace_FileScopedNamespace2()
    {
        var input = """
            namespace MyNamespace
            {
                class Test
                {
                    int [||]Method()
                    {
                        return 1 + 1;
                    }
                }
            }
            """;

        var expected1 = """
            namespace MyNamespace
            {
                class Test : MyBase
                {
                }
            }
            """;
        var expected2 = """
            namespace MyNamespace
            {
                internal class MyBase
                {
                    int Method()
                    {
                        return 1 + 1;
                    }
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            LanguageVersion = LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Silent }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestInNamespace_FileScopedNamespace3()
    {
        var input = """
            namespace MyNamespace
            {
                class Test
                {
                    int [||]Method()
                    {
                        return 1 + 1;
                    }
                }
            }
            """;

        var expected1 = """
            namespace MyNamespace
            {
                class Test : MyBase
                {
                }
            }
            """;
        var expected2 = """
            namespace MyNamespace
            {
                internal class MyBase
                {
                    int Method()
                    {
                        return 1 + 1;
                    }
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Silent }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestAccessibility()
    {
        var input = """
            public class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            public class Test : MyBase
            {
            }
            """;
        var expected2 = """
            public class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                },
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestEvent()
    {
        var input = """
            using System;

            class Test
            {
                private event EventHandler [||]Event1;
            }
            """;

        var expected1 = """
            using System;

            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            using System;

            internal class MyBase
            {
                private event EventHandler Event1;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestProperty()
    {
        var input = """
            class Test
            {
                int [||]MyProperty { get; set; }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int MyProperty { get; set; }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestField()
    {
        var input = """
            class Test
            {
                int [||]MyField;
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int MyField;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestFieldSelectInKeywords()
    {
        var input = """
            class Test
            {
                priva[||]te int MyField;
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                private int MyField;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestFieldSelectAfterSemicolon()
    {
        var input = """
            class Test
            {
                private int MyField;[||]
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                private int MyField;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestFieldSelectEntireDeclaration()
    {
        var input = """
            class Test
            {
                [|private int MyField;|]
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                private int MyField;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestFieldSelectMultipleVariables1()
    {
        var input = """
            class Test
            {
                [|private int MyField1, MyField2;|]
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                private int MyField1;
                private int MyField2;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestFieldSelectMultipleVariables2()
    {
        var input = """
            class Test
            {
                private int MyField1, [|MyField2;|]
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
                private int MyField1;
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                private int MyField2;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestFileHeader_FromExistingFile()
    {
        var input = """
            // this is my document header
            // that should be copied over

            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            // this is my document header
            // that should be copied over

            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            // this is my document header
            // that should be copied over

            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestFileHeader_FromOption()
    {
        var input = """
            // this is my document header
            // that should be ignored

            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            // this is my document header
            // that should be ignored

            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            // this is my real document header

            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            Options =
            {
                { CodeStyleOptions2.FileHeaderTemplate, "this is my real document header" }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public async Task TestUsingsInsideNamespace()
    {
        var input = """
            // this is my document header

            using System;
            using System.Collections.Generic;

            namespace ConsoleApp185
            {
                class Program
                {
                    static void [|Main|](string[] args)
                    {
                        Console.WriteLine(new List<int>());
                    }
                }
            }
            """;

        var expected1 = """
            // this is my document header

            using System;
            using System.Collections.Generic;

            namespace ConsoleApp185
            {
                class Program : MyBase
                {
                }
            }
            """;

        var expected2 = """
            // this is my real document header

            namespace ConsoleApp185;
            using System;
            using System.Collections.Generic;

            internal class MyBase
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(new List<int>());
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            LanguageVersion = LanguageVersion.CSharp10,
            Options = {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Error },
                { CodeStyleOptions2.FileHeaderTemplate, "this is my real document header" },
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public async Task TestUsingsInsideNamespace_FileScopedNamespace()
    {
        var input = """
            // this is my document header

            using System;
            using System.Collections.Generic;

            namespace ConsoleApp185
            {
                class Program
                {
                    static void [|Main|](string[] args)
                    {
                        Console.WriteLine(new List<int>());
                    }
                }
            }
            """;

        var expected1 = """
            // this is my document header

            using System;
            using System.Collections.Generic;

            namespace ConsoleApp185
            {
                class Program : MyBase
                {
                }
            }
            """;

        var expected2 = """
            // this is my real document header

            namespace ConsoleApp185
            {
                using System;
                using System.Collections.Generic;

                internal class MyBase
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine(new List<int>());
                    }
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            LanguageVersion = LanguageVersion.CSharp10,
            Options = {
                { CodeStyleOptions2.FileHeaderTemplate, "this is my real document header" },
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public async Task TestUsingsInsideNamespace_NoNamespace()
    {
        var input = """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void [|Main|](string[] args)
                {
                    Console.WriteLine(new List<int>());
                }
            }
            """;

        var expected1 = """
            using System;
            using System.Collections.Generic;

            class Program : MyBase
            {
            }
            """;

        var expected2 = """
            using System;
            using System.Collections.Generic;

            internal class MyBase
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(new List<int>());
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            LanguageVersion = LanguageVersion.CSharp10,
            Options = {
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55746")]
    public async Task TestUsingsInsideNamespace_MultipleNamespaces()
    {
        var input = """
            using System;
            using System.Collections.Generic;

            namespace N1
            {
                namespace N2
                {
                    class Program
                    {
                        static void [|Main|](string[] args)
                        {
                            Console.WriteLine(new List<int>());
                        }
                    }
                }
            }
            """;

        var expected1 = """
            using System;
            using System.Collections.Generic;

            namespace N1
            {
                namespace N2
                {
                    class Program : MyBase
                    {
                    }
                }
            }
            """;

        var expected2 = """
            namespace N1.N2
            {
                using System;
                using System.Collections.Generic;

                internal class MyBase
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine(new List<int>());
                    }
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2,
                }
            },
            LanguageVersion = LanguageVersion.CSharp10,
            Options = {
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, AddImportPlacement.InsideNamespace }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithInterface()
    {
        var input = """
            interface ITest
            {
                int Method();
            }

            class Test : ITest
            {
                public int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            interface ITest
            {
                int Method();
            }

            class Test : MyBase, ITest
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                public int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/roslyn/issues/45977")]
    public async Task TestRegion()
    {
        var input = """
            class Test
            {
                #region MyRegion
                int [||]Method()
                {
                    return 1 + 1;
                }

                void OtherMethiod() { }
                #endregion
            }
            """;

        var expected1 = """
            class Test : MyBase
            {

                #region MyRegion

                void OtherMethiod() { }
                #endregion
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                #region MyRegion
                int Method()
                {
                    return 1 + 1;
                }
                #endregion
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            DialogSelection = MakeSelection("Method")
        }.RunAsync();
    }

    [Fact]
    public async Task TestMakeAbstract_SingleMethod()
    {
        var input = """
            class Test
            {
                public int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
                public override int Method()
                {
                    return 1 + 1;
                }
            }
            """;
        var expected2 = """
            internal abstract class MyBase
            {
                public abstract int Method();
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            DialogSelection = MakeAbstractSelection("Method"),
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestMakeAbstract_MultipleMethods()
    {
        var input = """
            class Test
            {
                public int [||]Method()
                {
                    return 1 + 1;
                }

                public int Method2() => 2;
                public int Method3() => 3;
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
                public override int Method()
                {
                    return 1 + 1;
                }

                public override int Method2() => 2;
                public override int Method3() => 3;
            }
            """;
        var expected2 = """
            internal abstract class MyBase
            {
                public abstract int Method();
                public abstract int Method2();
                public abstract int Method3();
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            DialogSelection = MakeAbstractSelection("Method", "Method2", "Method3"),
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleMethods()
    {
        var input = """
            class Test
            {
                int [||]Method()
                {
                    return Method2() + 1;
                }

                int Method2() => 1;
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return Method2() + 1;
                }

                int Method2() => 1;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            DialogSelection = MakeSelection("Method", "Method2"),
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleMethods_SomeSelected()
    {
        var input = """
            class Test
            {
                int [||]Method()
                {
                    return Method2() + 1;
                }

                int Method2() => 1;
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
                int Method()
                {
                    return {|CS0122:Method2|}() + 1;
                }
            }
            """;
        var expected2 = """
            internal class MyBase
            {

                int Method2() => 1;
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            DialogSelection = MakeSelection("Method2"),
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestSelection_CompleteMethodAndComments()
    {
        var input = """
            class Test
            {
                [|/// <summary>
                /// this is a test method
                /// </summary>
                int Method()
                {
                    return 1 + 1;
                }|]
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestSelection_PartialMethodAndComments()
    {
        var input = """
            class Test
            {
                [|/// <summary>
                /// this is a test method
                /// </summary>
                int Method()
                {|]
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestSelection_PartialMethodAndComments2()
    {
        var input = """
            class Test
            {
                /// <summary>
                /// [|this is a test method
                /// </summary>
                int Method()
                {|]
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestSelection_PartialMethodAndComments3()
    {
        var input = """
            class Test
            {
                /// <summary>
                /// [|this is a test method
                /// </summary>
                int Method()|]
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestAttributes()
    {
        var input = """
            using System;

            class TestAttribute : Attribute { }

            class Test
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [||][TestAttribute]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            class TestAttribute : Attribute { }

            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [TestAttribute]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestAttributes2()
    {
        var input = """
            using System;

            class TestAttribute : Attribute { }
            class TestAttribute2 : Attribute { }

            class Test
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [||][TestAttribute]
                [TestAttribute2]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            class TestAttribute : Attribute { }
            class TestAttribute2 : Attribute { }

            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [TestAttribute]
                [TestAttribute2]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/roslyn/issues/45987")]
    public async Task TestAttributes3()
    {
        var input = """
            using System;

            class TestAttribute : Attribute { }
            class TestAttribute2 : Attribute { }

            class Test
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [TestAttribute]
                [||][TestAttribute2]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            class TestAttribute : Attribute { }

            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            using System;

            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [TestAttribute]
                [TestAttribute2]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            }
        }.RunAsync();
    }

    [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/roslyn/issues/45987")]
    public async Task TestAttributes4()
    {
        var input = """
            using System;

            class TestAttribute : Attribute { }
            class TestAttribute2 : Attribute { }

            class Test
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [TestAttribute]
                [TestAttribute2][||]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            class TestAttribute : Attribute { }

            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                /// <summary>
                /// this is a test method
                /// </summary>
                [TestAttribute]
                [TestAttribute2]
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestSameFile()
    {
        var input = """
            class Test
            {
                void Method[||]()
                {
                }
            }
            """;
        var expected = """
            internal class MyBase
            {
                void Method()
                {
                }
            }

            class Test : MyBase
            {
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedCode = expected,
            SameFile = true,
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration()
    {
        var input = """
            class Test[||]
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration2()
    {
        var input = """
            class [||]Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration3()
    {
        var input = """
            [||]class Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration4()
    {
        var input = """
            class[||] Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration_Comment()
    {
        var input = """
            using System;

            /// <summary>
            /// [|This is a test class
            /// </summary>
            class Test|]
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            /// <summary>
            /// This is a test class
            /// </summary>
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration_Comment2()
    {
        var input = """
            using System;

            /// <summary>
            /// This is a test class
            /// [|</summary>
            class Test|]
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            /// <summary>
            /// This is a test class
            /// </summary>
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration_Comment3()
    {
        var input = """
            using System;

            /// <summary>
            /// This is a [|test class
            /// </summary>
            class|] Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            /// <summary>
            /// This is a test class
            /// </summary>
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration_Attribute()
    {
        var input = """
            using System;

            public class MyAttribute : Attribute { }

            [||][MyAttribute]
            class Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        var expected1 = """
            using System;

            public class MyAttribute : Attribute { }

            [MyAttribute]
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            [My]
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration_SelectWithMembers()
    {
        var input = """
            [|class Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }|]
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestClassDeclaration_SelectWithMembers2()
    {
        var input = """
            [|class Test
            {
                int Method()
                {
                    return 1 + 1;
                }|]
            }
            """;

        var expected1 = """
            class Test : MyBase
            {
            }
            """;
        var expected2 = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            IsClassDeclarationSelection = true,
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55871")]
    public async Task TestGenericClass()
    {
        var input = """
            using System.Collections.Generic;

            [|class C<T1, T2, T3>
            {
                public List<T1> Field1;
                public T2 Field2;
                public T3 Method()
                {
                    return default;
                }|]
            }
            """;
        var expected1 = """
            using System.Collections.Generic;

            class C<T1, T2, T3> : MyBase<T1, T3>
            {
                public T2 Field2;
            }
            """;
        var expected2 = """
            using System.Collections.Generic;

            internal class MyBase<T1, T3>
            {
                public List<T1> Field1;
                public T3 Method()
                {
                    return default;
                }
            }
            """;
        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            DialogSelection = MakeSelection("Field1", "Method"),
            FileName = "Test1.cs",
        }.RunAsync();
    }

    [Fact]
    public async Task TestIncompleteFieldSelection_NoAction1()
    {
        var input = """
            class C
            {
                pub[||] {|CS1519:int|} Foo = 0;
            }
            """;
        await new Test
        {
            TestCode = input,
            FixedCode = input
        }.RunAsync();
    }

    [Fact]
    public async Task TestIncompleteMethodSelection_NoAction()
    {
        var input = """
            class C
            {
                pub[||] {|CS1519:int|} Foo()
                {
                    return 5;
                }
            }
            """;
        await new Test
        {
            TestCode = input,
            FixedCode = input
        }.RunAsync();
    }

    [Fact]
    public async Task TestTopLevelStatementSelection_NoAction()
    {
        var input = """
            [||]_ = 42;
            """;
        await new Test
        {
            TestCode = input,
            FixedCode = input,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestSealed()
    {
        var input = """
            internal sealed class MyClass
            {
                public void [||]M()
                {
                }
            }
            """;

        var expected1 = """
            internal sealed class MyClass : MyBase
            {
            }
            """;

        var expected2 = """
            internal class MyBase
            {
                public void M()
                {
                }
            }
            """;

        await new Test
        {
            TestCode = input,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs"
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63315")]
    public async Task TestMethodInsideNamespace_NoException()
    {
        var code = """
            namespace N
            {
                class C
                {
                }

                public void $$N
                {
                }
            }
            """;

        await new Test()
        {
            TestCode = code,
            FixedCode = code,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(7,17): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                DiagnosticResult.CompilerError("CS0116").WithSpan(7, 17, 7, 18),
                // /0/Test0.cs(7,17): error CS0547: '<invalid-global-code>.N': property or indexer cannot have void type
                DiagnosticResult.CompilerError("CS0547").WithSpan(7, 17, 7, 18).WithArguments("N.<invalid-global-code>.N"),
                // /0/Test0.cs(7,17): error CS0548: '<invalid-global-code>.N': property or indexer must have at least one accessor
                DiagnosticResult.CompilerError("CS0548").WithSpan(7, 17, 7, 18).WithArguments("N.<invalid-global-code>.N"),
            }
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55610")]
    public async Task TestMultipleMethodsSelected_WithTypeContainingBaseClass()
    {
        var code = """
            class Base
            {
            }

            class Derived : Base
            {
                [|public void M() { }
                public void N() { }|]
            }
            """;

        await new Test()
        {
            TestCode = code,
            FixedCode = code
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55610")]
    public async Task TestClassSelected_WithTypeContainingBaseClass()
    {
        var code = """
            class Base
            {
            }

            class $$Derived : Base
            {
                public void M() { }
                public void N() { }
            }
            """;

        await new Test()
        {
            TestCode = code,
            FixedCode = code
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleMethodsSelected_HighlightedMembersAreSelected()
    {
        var code = """
            class C
            {
                [|public void M() { }
                public void N() { }|]
                public void O() { }
            }
            """;

        var expected1 = """
            class C : MyBase
            {
                public void O() { }
            }
            """;

        var expected2 = """
            internal class MyBase
            {
                public void M() { }
                public void N() { }
            }
            """;

        await new Test()
        {
            TestCode = code,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs"
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55402")]
    public async Task TestMemberKeyword()
    {
        var code = """
            class C
            {
                $$public void M() { }
            }
            """;

        var expected1 = """
            class C : MyBase
            {
            }
            """;

        var expected2 = """
            internal class MyBase
            {
                public void M() { }
            }
            """;

        await new Test
        {
            TestCode = code,
            FixedState =
            {
                Sources =
                {
                    expected1,
                    expected2
                }
            },
            FileName = "Test1.cs",
        }.RunAsync();
    }

    private static IEnumerable<(string name, bool makeAbstract)> MakeAbstractSelection(params string[] memberNames)
        => memberNames.Select(m => (m, true));

    private static IEnumerable<(string name, bool makeAbstract)> MakeSelection(params string[] memberNames)
       => memberNames.Select(m => (m, false));

    private class TestExtractClassOptionsService : IExtractClassOptionsService
    {
        private readonly IEnumerable<(string name, bool makeAbstract)>? _dialogSelection;
        private readonly bool _sameFile;
        private readonly bool _isClassDeclarationSelection;

        public TestExtractClassOptionsService(IEnumerable<(string name, bool makeAbstract)>? dialogSelection = null, bool sameFile = false, bool isClassDeclarationSelection = false)
        {
            _dialogSelection = dialogSelection;
            _sameFile = sameFile;
            _isClassDeclarationSelection = isClassDeclarationSelection;
        }

        public string FileName { get; set; } = "MyBase.cs";
        public string BaseName { get; set; } = "MyBase";

        public Task<ExtractClassOptions?> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol originalSymbol, ImmutableArray<ISymbol> selectedMembers, CancellationToken cancellationToken)
        {
            var availableMembers = originalSymbol.GetMembers().Where(member => MemberAndDestinationValidator.IsMemberValid(member));

            IEnumerable<(ISymbol member, bool makeAbstract)> selections;

            if (_dialogSelection == null)
            {
                if (selectedMembers.IsEmpty)
                {
                    Assert.True(_isClassDeclarationSelection);
                    selections = availableMembers.Select(member => (member, makeAbstract: false));
                }
                else
                {
                    Assert.False(_isClassDeclarationSelection);
                    selections = selectedMembers.Select(m => (m, makeAbstract: false));
                }
            }
            else
            {
                selections = _dialogSelection.Select(selection => (member: availableMembers.Single(symbol => symbol.Name == selection.name), selection.makeAbstract));
            }

            var memberAnalysis = selections.Select(s =>
                new ExtractClassMemberAnalysisResult(
                    s.member,
                    s.makeAbstract))
                .ToImmutableArray();

            return Task.FromResult<ExtractClassOptions?>(new ExtractClassOptions(FileName, BaseName, _sameFile, memberAnalysis));
        }
    }
}
