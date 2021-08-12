// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MoveStaticMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers.CSharpMoveStaticMembersRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveStaticMembers
{
    public class CSharpMoveStaticMembersTests
    {
        private static readonly TestComposition s_testServices = FeaturesTestCompositions.Features.AddParts(typeof(TestMoveStaticMembersService));

        #region Perform Actions From Options
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveField()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Field = 1;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";

            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveProperty()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Property { get; set; }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestProperty");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestProperty { get; set; }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveEvent()
        {
            var initialMarkup = @"
using System;

namespace TestNs1
{
    public class Class1
    {
        public static event EventHandler Test[||]Event;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestEvent");
            var expectedResult1 = @"
using System;

namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"using System;

namespace TestNs1
{
    static class Class1Helpers
    {
        public static event EventHandler TestEvent;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethod()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveExtensionMethod()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public static class Class1
    {
        public static int Test[||]Method(this Other other)
        {
            return other.OtherInt + 2;
        }
    }

    public class Other
    {
        public int OtherInt;
        public Other()
        {
            OtherInt = 5;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public static class Class1
    {
    }

    public class Other
    {
        public int OtherInt;
        public Other()
        {
            OtherInt = 5;
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveConstField()
        {
            // const is static so we should work here
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public const int Test[||]Field = 1;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public const int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMultipleMethods()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static bool TestMethodBool()
        {
            return false;
        }

        public static int Test[||]MethodInt()
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethodInt", "TestMethodBool");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static bool TestMethodBool()
        {
            return false;
        }

        public static int TestMethodInt()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveSingleMethodFromMultiple()
        {
            // move the method that this was not triggered on
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]MethodInt()
        {
            return 0;
        }

        public static bool TestMethodBool()
        {
            return false;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethodBool");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethodInt()
        {
            return 0;
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {

        public static bool TestMethodBool()
        {
            return false;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveOneOfEach()
        {
            var initialMarkup = @"
using System;

namespace TestNs1
{
    public class Class1
    {
        public static int TestField;

        public static bool TestProperty { get; set; }

        public static event EventHandler TestEvent;

        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create(
                "TestMethod",
                "TestField",
                "TestProperty",
                "TestEvent");
            var expectedResult1 = @"
using System;

namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"using System;

namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField;

        public static bool TestProperty { get; set; }

        public static event EventHandler TestEvent;

        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestInNestedClass()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public class NestedClass1
        {
            public static int Test[||]Field = 1;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
        public class NestedClass1
        {
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestInNestedNamespace()
        {
            // collapse the namespaces in the new file
            var initialMarkup = @"
namespace TestNs1
{
    namespace InnerNs
    {
        public class Class1
        {
            public static int Test[||]Field = 1;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    namespace InnerNs
    {
        public class Class1
        {
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1.InnerNs
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveFieldNoNamespace()
        {
            var initialMarkup = @"
public class Class1
{
    public static int Test[||]Field = 1;
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
public class Class1
{
}";
            var expectedResult2 = @"static class Class1Helpers
{
    public static int TestField = 1;
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveFieldNewNamespace()
        {
            var initialMarkup = @"
public class Class1
{
    public static int Test[||]Field = 1;
}";
            var selectedDestinationName = "NewNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
public class Class1
{
}";
            var expectedResult2 = @"namespace NewNs
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodWithNamespacedSelectedDestination()
        {
            // in the case that we have an extra namespace in the destination name
            // we append it on to the old type's namespace
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "ExtraNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1.ExtraNs
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodFileScopedNamespace()
        {
            // We still keep normal namespacing rules in the new file
            var initialMarkup = @"
namespace TestNs1;

public class Class1
{
    public static int Test[||]Method()
    {
        return 0;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1;

public class Class1
{
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await new Test(selectedDestinationName, selectedMembers, newFileName)
            {
                TestCode = initialMarkup,
                FixedState =
                {
                    Sources =
                    {
                        expectedResult1,
                        (newFileName, expectedResult2)
                    }
                },
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveGenericMethod()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static T Test[||]Method<T>(T item)
        {
            return item;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static T TestMethod<T>(T item)
        {
            return item;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodWithGenericClass()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1<T>
    {
        public static T Test[||]Method(T item)
        {
            return item;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1<T>
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers<T>
    {
        public static T Test[||]Method(T item)
        {
            return item;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodAndRefactorUsage()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1.TestMethod();
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodAndRefactorUsageDifferentNamespace()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }
}

namespace TestNs2
{
    using TestNs1;

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1.TestMethod();
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}

namespace TestNs2
{
    using TestNs1;

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodAndRefactorUsageNewNamespace()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1.TestMethod();
        }
    }
}";
            var selectedDestinationName = "ExtraNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
using TestNs1.ExtraNs;

namespace TestNs1
{
    public class Class1
    {
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1.ExtraNs
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodAndRefactorUsageSeparateFile()
        {
            var initialMarkup1 = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            var initialMarkup2 = @"
using TestNs1;

public class Class2
{
    public static int TestMethod2()
    {
        return Class1.TestMethod();
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult3 = @"
using TestNs1;

public class Class2
{
    public static int TestMethod2()
    {
        return Class1Helpers.TestMethod();
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await new Test(selectedDestinationName, selectedMembers, newFileName)
            {
                TestState =
                {
                    Sources =
                    {
                        initialMarkup1,
                        initialMarkup2
                    }
                },
                FixedState =
                {
                    Sources =
                    {
                        expectedResult1,
                        expectedResult3,
                        (newFileName, expectedResult2)
                    }
                }
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveExtensionMethodDontRefactor()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public static class Class1
    {
        public static int Test[||]Method(this Other other)
        {
            return other.OtherInt + 2;
        }
    }

    public class Class2
    {
        public int GetOtherInt()
        {
            var other = new Other();
            return other.TestMethod();
        }
    }

    public class Other
    {
        public int OtherInt;
        public Other()
        {
            OtherInt = 5;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public static class Class1
    {
    }

    public class Class2
    {
        public int GetOtherInt()
        {
            var other = new Other();
            return other.TestMethod();
        }
    }

    public class Other
    {
        public int OtherInt;
        public Other()
        {
            OtherInt = 5;
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveExtensionMethodRefactorImports()
        {
            var initialMarkup = @"
namespace TestNs1
{
    using TestNs2;

    public static class Class1
    {
        public static int Test[||]Method(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}

namespace TestNs2
{
    using TestNs1;

    public class Class2
    {
        public int GetOtherInt()
        {
            var other = new Other();
            return other.TestMethod();
        }
    }

    public class Other
    {
        public int OtherInt;
        public Other()
        {
            OtherInt = 5;
        }
    }
}";
            var selectedDestinationName = "ExtraNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    using TestNs2;

    public static class Class1
    {
    }
}

namespace TestNs2
{
    using TestNs1;
    using TestNs1.ExtraNs;

    public class Class2
    {
        public int GetOtherInt()
        {
            var other = new Other();
            return other.TestMethod();
        }
    }

    public class Other
    {
        public int OtherInt;
        public Other()
        {
            OtherInt = 5;
        }
    }
}";
            var expectedResult2 = @"using TestNs2;

namespace TestNs1.ExtraNs
{
    static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodFromStaticClass()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public static class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public static class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestMoveMethodRetainFileBanner()
        {
            var initialMarkup = @"// Here is an example of a license or something
// we would want to keep at the top of a file

namespace TestNs1
{
    public static class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"// Here is an example of a license or something
// we would want to keep at the top of a file

namespace TestNs1
{
    public static class Class1
    {
    }
}";
            var expectedResult2 = @"// Here is an example of a license or something
// we would want to keep at the top of a file

namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }
        #endregion

        #region Selections and caret position

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectInMethodParens()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethod([||])
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectWholeFieldDeclaration()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [|public static int TestField = 1;|]
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectBeforeKeywordOfDeclaration()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [||]public static int TestField = 1;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectInKeyWordOfDeclaration1()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        pub[||]lic static int TestField = 1;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectInKeyWordOfDeclaration2()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public st[||]atic int TestField = 1;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectInTypeIdentifierMethodDeclaration()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static i[||]nt TestMethod()
        {
            return 0;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectInFieldInitializerAfterSemicolon()
        {
            // However, a semicolon after the initializer is still considered a declaration
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestField = 1;[||]
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";

            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectInTypeIdentifierOfFieldDeclaration_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static i[||]nt TestField = 1;
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectInFieldInitializerEquals_NoAction()
        {
            // The initializer isn't a member declaration
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestField =[||] 1;
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectMethodBody_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethod()
        {
            retu[||]rn 0;
        }
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectMethodBracket_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethod()
        [|{|]
            return 0;
        }
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectPropertyBody_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestProperty { get; [||]set; }
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        [UseExportProvider]
        public async Task TestSelectNonStaticProperty_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public int Test[||]Property { get; set; }
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }
        #endregion

        private class Test : VerifyCS.Test
        {
            public Test(
                string destinationType,
                ImmutableArray<string> selection,
                string destinationName = "a.cs")
            {
                _destinationType = destinationType;
                _selection = selection;
                _destinationName = destinationName;
            }

            private readonly string _destinationType;

            private readonly ImmutableArray<string> _selection;

            private readonly string _destinationName;

            protected override Workspace CreateWorkspaceImpl()
            {
                var hostServices = s_testServices.GetHostServices();

                var workspace = new AdhocWorkspace(hostServices);
                var testOptionsService = (TestMoveStaticMembersService)workspace.Services.GetRequiredService<IMoveStaticMembersOptionsService>();
                testOptionsService.DestinationType = _destinationType;
                testOptionsService.SelectedMembers = _selection;
                testOptionsService.Filename = _destinationName;

                return workspace;
            }
        }

        private static async Task TestMovementNewFileAsync(
            string initialMarkup,
            string expectedSource,
            string expectedNewFile,
            string newFileName,
            ImmutableArray<string> selectedMembers,
            string newTypeName)
            => await new Test(newTypeName, selectedMembers, newFileName)
            {
                TestCode = initialMarkup,
                FixedState =
                {
                    Sources =
                    {
                        expectedSource,
                        (newFileName, expectedNewFile)
                    }
                },
            }.RunAsync().ConfigureAwait(false);

        private static async Task TestNoRefactoringAsync(string initialMarkup)
        {
            await new Test("", ImmutableArray<string>.Empty)
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup
            }.RunAsync().ConfigureAwait(false);
        }
    }
}
