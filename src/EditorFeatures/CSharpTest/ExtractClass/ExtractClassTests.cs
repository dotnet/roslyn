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
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractClass
{
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

                return SpecializedCollections.SingletonEnumerable(new CSharpExtractClassCodeRefactoringProvider(service));
            }

            protected override Workspace CreateWorkspaceImpl()
            {
                return TestWorkspace.Create(WorkspaceKind, LanguageNames.CSharp, this.CreateCompilationOptions(), this.CreateParseOptions());
            }
        }

        [Fact]
        public async Task TestSingleMethod()
        {
            var input = @"
class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestErrorBaseMethod()
        {
            var input = @"
class ErrorBase
{
}

class Test : ErrorBase
{
    int [||]Method()
    {
        return 1 + 1;
    }
}";
            await new Test
            {
                TestCode = input,
                FixedCode = input,
            }.RunAsync();
        }

        [Fact]
        public async Task TestMiscellaneousFiles()
        {
            var input = @"
class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}";

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
            var input1 = @"
partial class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}";
            var input2 = @"
partial class Test
{
    int Method2()
    {
        return 5;
    }
}";

            var expected1 = @"
partial class Test : MyBase
{
}";
            var expected2 = @"
partial class Test
{
}";
            var expected3 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
    int Method2()
    {
        return 5;
    }
}";

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
                FileName = "/0/Test2.cs",
                DialogSelection = MakeSelection("Method", "Method2")
            }.RunAsync();
        }

        [Fact]
        public async Task TestInNamespace()
        {
            var input = @"
namespace MyNamespace
{
    class Test
    {
        int [||]Method()
        {
            return 1 + 1;
        }
    }
}";

            var expected1 = @"
namespace MyNamespace
{
    class Test : MyBase
    {
    }
}";
            var expected2 = @"namespace MyNamespace
{
    internal class MyBase
    {
        int Method()
        {
            return 1 + 1;
        }
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestInNamespace_FileScopedNamespace1()
        {
            var input = @"
namespace MyNamespace
{
    class Test
    {
        int [||]Method()
        {
            return 1 + 1;
        }
    }
}";

            var expected1 = @"
namespace MyNamespace
{
    class Test : MyBase
    {
    }
}";
            var expected2 = @"namespace MyNamespace;

internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestInNamespace_FileScopedNamespace2()
        {
            var input = @"
namespace MyNamespace
{
    class Test
    {
        int [||]Method()
        {
            return 1 + 1;
        }
    }
}";

            var expected1 = @"
namespace MyNamespace
{
    class Test : MyBase
    {
    }
}";
            var expected2 = @"namespace MyNamespace
{
    internal class MyBase
    {
        int Method()
        {
            return 1 + 1;
        }
    }
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestInNamespace_FileScopedNamespace3()
        {
            var input = @"
namespace MyNamespace
{
    class Test
    {
        int [||]Method()
        {
            return 1 + 1;
        }
    }
}";

            var expected1 = @"
namespace MyNamespace
{
    class Test : MyBase
    {
    }
}";
            var expected2 = @"namespace MyNamespace
{
    internal class MyBase
    {
        int Method()
        {
            return 1 + 1;
        }
    }
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestAccessibility()
        {
            var input = @"
public class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
public class Test : MyBase
{
}";
            var expected2 = @"public class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
        public async Task TestEvent()
        {
            var input = @"
using System;

class Test
{
    private event EventHandler [||]Event1;
}";

            var expected1 = @"
using System;

class Test : MyBase
{
}";
            var expected2 = @"using System;

internal class MyBase
{
    private event EventHandler Event1;
}";

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
        public async Task TestProperty()
        {
            var input = @"
class Test
{
    int [||]MyProperty { get; set; }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int MyProperty { get; set; }
}";

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
        public async Task TestField()
        {
            var input = @"
class Test
{
    int [||]MyField;
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int MyField;
}";

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
        public async Task TestFileHeader_FromExistingFile()
        {
            var input = @"// this is my document header
// that should be copied over

class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"// this is my document header
// that should be copied over

class Test : MyBase
{
}";
            var expected2 = @"// this is my document header
// that should be copied over

internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
        public async Task TestFileHeader_FromOption()
        {
            var input = @"// this is my document header
// that should be ignored

class Test
{
    int [||]Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"// this is my document header
// that should be ignored

class Test : MyBase
{
}";
            var expected2 = @"// this is my real document header

internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(55746, "https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestUsingsInsideNamespace()
        {
            var input = @"// this is my document header

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
}";

            var expected1 = @"// this is my document header

using System;
using System.Collections.Generic;

namespace ConsoleApp185
{
    class Program : MyBase
    {
    }
}";

            var expected2 = @"// this is my real document header

namespace ConsoleApp185;
using System;
using System.Collections.Generic;

internal class MyBase
{
    static void Main(string[] args)
    {
        Console.WriteLine(new List<int>());
    }
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(55746, "https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestUsingsInsideNamespace_FileScopedNamespace()
        {
            var input = @"// this is my document header

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
}";

            var expected1 = @"// this is my document header

using System;
using System.Collections.Generic;

namespace ConsoleApp185
{
    class Program : MyBase
    {
    }
}";

            var expected2 = @"// this is my real document header

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
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(55746, "https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestUsingsInsideNamespace_NoNamespace()
        {
            var input = @"
using System;
using System.Collections.Generic;

class Program
{
    static void [|Main|](string[] args)
    {
        Console.WriteLine(new List<int>());
    }
}";

            var expected1 = @"
using System;
using System.Collections.Generic;

class Program : MyBase
{
}";

            var expected2 = @"using System;
using System.Collections.Generic;

internal class MyBase
{
    static void Main(string[] args)
    {
        Console.WriteLine(new List<int>());
    }
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(55746, "https://github.com/dotnet/roslyn/issues/55746")]
        public async Task TestUsingsInsideNamespace_MultipleNamespaces()
        {
            var input = @"
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
}";

            var expected1 = @"
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
}";

            var expected2 = @"namespace N1.N2
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
}";

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
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithInterface()
        {
            var input = @"
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
}";

            var expected1 = @"
interface ITest 
{
    int Method();
}

class Test : MyBase, ITest
{
}";
            var expected2 = @"internal class MyBase
{
    public int Method()
    {
        return 1 + 1;
    }
}";

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

        [ConditionalFact(AlwaysSkip = "https://github.com/dotnet/roslyn/issues/45977")]
        public async Task TestRegion()
        {
            var input = @"
class Test
{
    #region MyRegion
    int [||]Method()
    {
        return 1 + 1;
    }

    void OtherMethiod() { }
    #endregion
}";

            var expected1 = @"
class Test : MyBase
{

    #region MyRegion

    void OtherMethiod() { }
    #endregion
}";
            var expected2 = @"internal class MyBase
{
    #region MyRegion
    int Method()
    {
        return 1 + 1;
    }
    #endregion
}";

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
            var input = @"
class Test
{
    public int [||]Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
    public override int Method()
    {
        return 1 + 1;
    }
}";
            var expected2 = @"internal abstract class MyBase
{
    public abstract int Method();
}";

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
                DialogSelection = MakeAbstractSelection("Method")
            }.RunAsync();
        }

        [Fact]
        public async Task TestMakeAbstract_MultipleMethods()
        {
            var input = @"
class Test
{
    public int [||]Method()
    {
        return 1 + 1;
    }

    public int Method2() => 2;
    public int Method3() => 3;
}";

            var expected1 = @"
class Test : MyBase
{
    public override int Method()
    {
        return 1 + 1;
    }

    public override int Method2() => 2;
    public override int Method3() => 3;
}";
            var expected2 = @"internal abstract class MyBase
{
    public abstract int Method();
    public abstract int Method2();
    public abstract int Method3();
}";

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
                DialogSelection = MakeAbstractSelection("Method", "Method2", "Method3")
            }.RunAsync();
        }

        [Fact]
        public async Task TestMultipleMethods()
        {
            var input = @"
class Test
{
    int [||]Method()
    {
        return Method2() + 1;
    }

    int Method2() => 1;
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return Method2() + 1;
    }

    int Method2() => 1;
}";

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
                DialogSelection = MakeSelection("Method", "Method2")
            }.RunAsync();
        }

        [Fact]
        public async Task TestMultipleMethods_SomeSelected()
        {
            var input = @"
class Test
{
    int [||]Method()
    {
        return Method2() + 1;
    }

    int Method2() => 1;
}";

            var expected1 = @"
class Test : MyBase
{
    int Method()
    {
        return {|CS0122:Method2|}() + 1;
    }
}";
            var expected2 = @"internal class MyBase
{

    int Method2() => 1;
}";

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
                DialogSelection = MakeSelection("Method2")
            }.RunAsync();
        }

        [Fact]
        public async Task TestSelection_CompleteMethodAndComments()
        {
            var input = @"
class Test
{
    [|/// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }|]
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}";

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
        public async Task TestSelection_PartialMethodAndComments()
        {
            var input = @"
class Test
{
    [|/// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {|]
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}";

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
        public async Task TestSelection_PartialMethodAndComments2()
        {
            var input = @"
class Test
{
    /// <summary>
    /// [|this is a test method
    /// </summary>
    int Method()
    {|]
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}";

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
        public async Task TestSelection_PartialMethodAndComments3()
        {
            var input = @"
class Test
{
    /// <summary>
    /// [|this is a test method
    /// </summary>
    int Method()|]
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    int Method()
    {
        return 1 + 1;
    }
}";

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
        public async Task TestAttributes()
        {
            var input = @"
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
}";

            var expected1 = @"
using System;

class TestAttribute : Attribute { }

class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    /// <summary>
    /// this is a test method
    /// </summary>
    [TestAttribute]
    int Method()
    {
        return 1 + 1;
    }
}";

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
        public async Task TestAttributes2()
        {
            var input = @"
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
}";

            var expected1 = @"
using System;

class TestAttribute : Attribute { }
class TestAttribute2 : Attribute { }

class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
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
}";

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
        public async Task TestAttributes3()
        {
            var input = @"
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
}";

            var expected1 = @"
using System;

class TestAttribute : Attribute { }

class Test : MyBase
{
}";
            var expected2 = @"
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
}";

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
            var input = @"
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
}";

            var expected1 = @"
using System;

class TestAttribute : Attribute { }

class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
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
}";

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
            var input = @"
class Test
{
    void Method[||]()
    {
    }
}";
            var expected = @"
internal class MyBase
{
    void Method()
    {
    }
}

class Test : MyBase
{
}";

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
            var input = @"
class Test[||]
{
    int Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration2()
        {
            var input = @"
class [||]Test
{
    int Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration3()
        {
            var input = @"
[||]class Test
{
    int Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration4()
        {
            var input = @"
class[||] Test
{
    int Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration_Comment()
        {
            var input = @"
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
}";

            var expected1 = @"
using System;

/// <summary>
/// This is a test class
/// </summary>
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration_Comment2()
        {
            var input = @"
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
}";

            var expected1 = @"
using System;

/// <summary>
/// This is a test class
/// </summary>
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration_Comment3()
        {
            var input = @"
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
}";

            var expected1 = @"
using System;

/// <summary>
/// This is a test class
/// </summary>
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration_Attribute()
        {
            var input = @"
using System;

public class MyAttribute : Attribute { }

[||][MyAttribute]
class Test
{
    int Method()
    {
        return 1 + 1;
    }
}";

            var expected1 = @"
using System;

public class MyAttribute : Attribute { }

[MyAttribute]
class Test : MyBase
{
}";
            var expected2 = @"[My]
internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration_SelectWithMembers()
        {
            var input = @"
[|class Test
{
    int Method()
    {
        return 1 + 1;
    }
}|]";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact]
        public async Task TestClassDeclaration_SelectWithMembers2()
        {
            var input = @"
[|class Test
{
    int Method()
    {
        return 1 + 1;
    }|]
}";

            var expected1 = @"
class Test : MyBase
{
}";
            var expected2 = @"internal class MyBase
{
    int Method()
    {
        return 1 + 1;
    }
}";

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
            }.RunAsync();
        }

        [Fact, WorkItem(55871, "https://github.com/dotnet/roslyn/issues/55871")]
        public async Task TestGenericClass()
        {
            var input = @"using System.Collections.Generic;

[|class C<T1, T2, T3>
{
    public List<T1> Field1;
    public T2 Field2;
    public T3 Method()
    {
        return default;
    }|]
}";
            var expected1 = @"using System.Collections.Generic;

class C<T1, T2, T3> : MyBase<T1, T3>
{
    public T2 Field2;
}";
            var expected2 = @"using System.Collections.Generic;

internal class MyBase<T1, T3>
{
    public List<T1> Field1;
    public T3 Method()
    {
        return default;
    }
}";
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
                DialogSelection = MakeSelection("Field1", "Method")
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
            private readonly bool isClassDeclarationSelection;

            public TestExtractClassOptionsService(IEnumerable<(string name, bool makeAbstract)>? dialogSelection = null, bool sameFile = false, bool isClassDeclarationSelection = false)
            {
                _dialogSelection = dialogSelection;
                _sameFile = sameFile;
                this.isClassDeclarationSelection = isClassDeclarationSelection;
            }

            public string FileName { get; set; } = "MyBase.cs";
            public string BaseName { get; set; } = "MyBase";

            public Task<ExtractClassOptions?> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol originalSymbol, ISymbol? selectedMember, CancellationToken cancellationToken)
            {
                var availableMembers = originalSymbol.GetMembers().Where(member => MemberAndDestinationValidator.IsMemberValid(member));

                IEnumerable<(ISymbol member, bool makeAbstract)> selections;

                if (_dialogSelection == null)
                {
                    if (selectedMember is null)
                    {
                        Assert.True(isClassDeclarationSelection);
                        selections = availableMembers.Select(member => (member, makeAbstract: false));
                    }
                    else
                    {
                        Assert.False(isClassDeclarationSelection);
                        selections = new[] { (selectedMember, false) };
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
}
