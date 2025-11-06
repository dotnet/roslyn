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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractClass;

[UseExportProvider]
public sealed class ExtractClassTests
{
    private sealed class Test : CSharpCodeRefactoringVerifier<CSharpExtractClassCodeRefactoringProvider>.Test
    {
        public IEnumerable<(string name, bool makeAbstract)>? DialogSelection { get; set; }
        public bool SameFile { get; set; }
        public bool IsClassDeclarationSelection { get; set; }
        public string FileName { get; set; } = "/0/Test1.cs";
        public string WorkspaceKind { get; set; } = CodeAnalysis.WorkspaceKind.Host;

        protected override IEnumerable<CodeRefactoringProvider> GetCodeRefactoringProviders()
        {
            var service = new TestExtractClassOptionsService(DialogSelection, WorkspaceKind != CodeAnalysis.WorkspaceKind.MiscellaneousFiles ? SameFile : true, IsClassDeclarationSelection)
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
            TestCode = """
            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
    public Task TestErrorBaseMethod()
        => new Test
        {
            TestCode = """
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
            """,
        }.RunAsync();

    [Fact]
    public Task TestMiscellaneousFiles()
        => new Test
        {
            TestCode = """
            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """,
            FixedCode = """
            internal class MyBase
            {
                int Method()
                {
                    return 1 + 1;
                }
            }

            class Test : MyBase
            {
            }
            """,
            WorkspaceKind = WorkspaceKind.MiscellaneousFiles
        }.RunAsync();

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
            TestCode = """
            record R(string S)
            {
                void $$M()
                {
                }
            }
            """,
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
            TestCode = """
            class R(string S)
            {
                void $$M()
                {
                }
            }
            """,
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
            TestCode = """
            record R
            {
                public string $$S { get; set; }
            }
            """,
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
            TestCode = """
            record R(string S)
            {
                public string $$S { get; set; } = S;
            }
            """,
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
            TestCode = """
            class R(string S)
            {
                public string $$S { get; set; } = S;
            }
            """,
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
    public Task TestRecordParam()
        => new Test
        {
            TestCode = """
            record R(string $$S)
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public Task TestClassParam1()
        => new Test
        {
            TestCode = """
            class R(string $$S)
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public Task TestClassParam2()
        => new Test
        {
            TestCode = """
            class R(string $$S);
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public Task TestStructParam1()
        => new Test
        {
            TestCode = """
            struct R(string $$S)
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public Task TestStructParam2()
        => new Test
        {
            TestCode = """
            struct R(string $$S);
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public Task TestRecordStruct()
        => new Test
        {
            TestCode = """
            record struct R(string S)
            {
                void $$M()
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public Task TestStruct()
        => new Test
        {
            TestCode = """
            struct R(string S)
            {
                void $$M()
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
        }.RunAsync();

    [Fact]
    public async Task TestInNamespace()
    {
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
            public class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
            using System;

            class Test
            {
                private event EventHandler [||]Event1;
            }
            """,
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
            TestCode = """
            class Test
            {
                int [||]MyProperty { get; set; }
            }
            """,
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
            TestCode = """
            class Test
            {
                int [||]MyField;
            }
            """,
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
            TestCode = """
            class Test
            {
                priva[||]te int MyField;
            }
            """,
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
            TestCode = """
            class Test
            {
                private int MyField;[||]
            }
            """,
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
            TestCode = """
            class Test
            {
                [|private int MyField;|]
            }
            """,
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
            TestCode = """
            class Test
            {
                [|private int MyField1, MyField2;|]
            }
            """,
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
            TestCode = """
            class Test
            {
                private int MyField1, [|MyField2;|]
            }
            """,
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
            TestCode = """
            // this is my document header
            // that should be copied over

            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
            // this is my document header
            // that should be ignored

            class Test
            {
                int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void [|Main|](string[] args)
                {
                    Console.WriteLine(new List<int>());
                }
            }
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
            class Test
            {
                public int [||]Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
            class Test
            {
                public int [||]Method()
                {
                    return 1 + 1;
                }

                public int Method2() => 2;
                public int Method3() => 3;
            }
            """,
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
            TestCode = """
            class Test
            {
                int [||]Method()
                {
                    return Method2() + 1;
                }

                int Method2() => 1;
            }
            """,
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
            TestCode = """
            class Test
            {
                int [||]Method()
                {
                    return Method2() + 1;
                }

                int Method2() => 1;
            }
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
    public Task TestSameFile()
        => new Test
        {
            TestCode = """
            class Test
            {
                void Method[||]()
                {
                }
            }
            """,
            FixedCode = """
            internal class MyBase
            {
                void Method()
                {
                }
            }

            class Test : MyBase
            {
            }
            """,
            SameFile = true,
        }.RunAsync();

    [Fact]
    public async Task TestClassDeclaration()
    {
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
            TestCode = """
            class Test[||]
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
            class [||]Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
            [||]class Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
            class[||] Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
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
            """,
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
            TestCode = """
            [|class Test
            {
                int Method()
                {
                    return 1 + 1;
                }
            }|]
            """,
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
            TestCode = """
            [|class Test
            {
                int Method()
                {
                    return 1 + 1;
                }|]
            }
            """,
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
            TestCode = """
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
            """,
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
    public Task TestIncompleteFieldSelection_NoAction1()
        => new Test
        {
            TestCode = """
            class C
            {
                pub[||] {|CS1519:int|} Foo = 0;
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestIncompleteMethodSelection_NoAction()
        => new Test
        {
            TestCode = """
            class C
            {
                pub[||] {|CS1519:int|} Foo()
                {
                    return 5;
                }
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestTopLevelStatementSelection_NoAction()
        => new Test
        {
            TestCode = """
            [||]_ = 42;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            }
        }.RunAsync();

    [Fact]
    public async Task TestSealed()
    {
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
            TestCode = """
            internal sealed class MyClass
            {
                public void [||]M()
                {
                }
            }
            """,
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
    public Task TestMethodInsideNamespace_NoException()
        => new Test()
        {
            TestCode = """
                namespace N
                {
                    class C
                    {
                    }

                    public void $$N
                    {
                    }
                }
                """,
            FixedState =
            {
                Sources =
                {
                    """
                    namespace N
                    {
                        class C : MyBase
                        {
                        }
                    """,
                    """
                    namespace N
                    {
                        internal class MyBase
                        {
                        }
                    
                        public void N
                            {
                            }
                        }
                    }
                    """,
                },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(5,6): error CS1513: } expected
                    DiagnosticResult.CompilerError("CS1513").WithSpan(5, 6, 5, 6),
                    // /0/Test1.cs(5,5): error CS1519: Invalid token '}' in class, record, struct, or interface member declaration
                    DiagnosticResult.CompilerError("CS1519").WithSpan("/0/Test1.cs", 5, 5, 5, 6).WithArguments("}"),
                    // /0/Test1.cs(7,17): error CS0547: 'MyBase.N': property or indexer cannot have void type
                    DiagnosticResult.CompilerError("CS0547").WithSpan("/0/Test1.cs", 7, 17, 7, 18).WithArguments("N.MyBase.N"),
                    // /0/Test1.cs(7,17): error CS0548: 'MyBase.N': property or indexer must have at least one accessor
                    DiagnosticResult.CompilerError("CS0548").WithSpan("/0/Test1.cs", 7, 17, 7, 18).WithArguments("N.MyBase.N"),
                }
            },
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(5,5): error CS1519: Invalid token '}' in class, record, struct, or interface member declaration
                DiagnosticResult.CompilerError("CS1519").WithSpan(5, 5, 5, 6).WithArguments("}"),
                // /0/Test0.cs(7,17): error CS0547: 'C.N': property or indexer cannot have void type
                DiagnosticResult.CompilerError("CS0547").WithSpan(7, 17, 7, 18).WithArguments("N.C.N"),
                // /0/Test0.cs(7,17): error CS0548: 'C.N': property or indexer must have at least one accessor
                DiagnosticResult.CompilerError("CS0548").WithSpan(7, 17, 7, 18).WithArguments("N.C.N"),
                // /0/Test0.cs(10,2): error CS1513: } expected
                DiagnosticResult.CompilerError("CS1513").WithSpan(10, 2, 10, 2),
            },
            FileName = "Test1.cs"
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55610")]
    public Task TestMultipleMethodsSelected_WithTypeContainingBaseClass()
        => new Test()
        {
            TestCode = """
            class Base
            {
            }

            class Derived : Base
            {
                [|public void M() { }
                public void N() { }|]
            }
            """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55610")]
    public Task TestClassSelected_WithTypeContainingBaseClass()
        => new Test()
        {
            TestCode = """
            class Base
            {
            }

            class $$Derived : Base
            {
                public void M() { }
                public void N() { }
            }
            """
        }.RunAsync();

    [Fact]
    public async Task TestMultipleMethodsSelected_HighlightedMembersAreSelected()
    {
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
            TestCode = """
            class C
            {
                [|public void M() { }
                public void N() { }|]
                public void O() { }
            }
            """,
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
            TestCode = """
            class C
            {
                $$public void M() { }
            }
            """,
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81066")]
    public async Task TestPartialEvent()
    {
        var input1 = """
            using System;

            partial class C
            {
                public partial event EventHandler [||]E;
            }
            """;

        var input2 = """
            using System;

            partial class C
            {
                public partial event EventHandler E { add { } remove { } }
            }
            """;

        var expected1 = """
            using System;

            partial class C : MyBase
            {
            }
            """;

        var expected2 = """
            using System;

            partial class C
            {
                public partial event EventHandler {|CS9276:E|} { add { } remove { } }
            }
            """;

        var expected3 = """
            using System;

            internal class MyBase
            {
                public event EventHandler E;
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81066")]
    public async Task TestPartialProperty()
    {
        var input1 = """
            partial class C
            {
                public partial int [||]P { get; }
            }
            """;

        var input2 = """
            partial class C
            {
                public partial int P => 42;
            }
            """;

        var expected1 = """
            partial class C : MyBase
            {
            }
            """;

        var expected2 = """
            partial class C
            {
                public partial int {|CS9249:P|} => 42;
            }
            """;

        var expected3 = """
            internal class MyBase
            {
                public partial int {|CS9248:{|CS0751:P|}|} { get; }
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81066")]
    public async Task TestPartialMethod()
    {
        var input1 = """
            partial class C
            {
                public partial void [||]M();
            }
            """;

        var input2 = """
            partial class C
            {
                public partial void M() { }
            }
            """;

        var expected1 = """
            partial class C : MyBase
            {
            }
            """;

        var expected2 = """
            partial class C
            {
                public partial void {|CS0759:M|}() { }
            }
            """;

        var expected3 = """
            internal class MyBase
            {
                public partial void {|CS8795:{|CS0751:M|}|}();
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
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();
    }

    private static IEnumerable<(string name, bool makeAbstract)> MakeAbstractSelection(params string[] memberNames)
        => memberNames.Select(m => (m, true));

    private static IEnumerable<(string name, bool makeAbstract)> MakeSelection(params string[] memberNames)
       => memberNames.Select(m => (m, false));

    private sealed class TestExtractClassOptionsService : IExtractClassOptionsService
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

        public ExtractClassOptions? GetExtractClassOptions(
            Document document,
            INamedTypeSymbol originalSymbol,
            ImmutableArray<ISymbol> selectedMembers,
            SyntaxFormattingOptions formattingOptions,
            CancellationToken cancellationToken)
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

            var memberAnalysis = selections.SelectAsArray(s =>
                new ExtractClassMemberAnalysisResult(
                    s.member,
                    s.makeAbstract));

            return new ExtractClassOptions(FileName, BaseName, _sameFile, memberAnalysis);
        }
    }
}
