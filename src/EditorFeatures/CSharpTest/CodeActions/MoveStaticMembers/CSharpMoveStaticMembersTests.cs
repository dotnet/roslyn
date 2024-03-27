// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MoveStaticMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers.CSharpMoveStaticMembersRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveStaticMembers
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
    public class CSharpMoveStaticMembersTests
    {
        private static readonly TestComposition s_testServices = FeaturesTestCompositions.Features.AddParts(typeof(TestMoveStaticMembersService));

        #region Perform New Type Action From Options
        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";

            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestProperty { get; set; }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static event EventHandler TestEvent;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public const int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveNothing()
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
            var selectedMembers = ImmutableArray<string>.Empty;
            var expectedResult1 = @"
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
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodWithTrivia()
        {
            var initialMarkup = @"
namespace TestNs1
{
    // some comment we don't want to move
    public class Class1
    {
        // some comment we want to move
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
    // some comment we don't want to move
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        // some comment we want to move
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
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

        [Fact]
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
    internal static class Class1Helpers
    {

        public static bool TestMethodBool()
        {
            return false;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
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

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
            var expectedResult2 = @"internal static class Class1Helpers
{
    public static int TestField = 1;
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
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

        [Fact]
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
    internal static class Class1Helpers
    {
        public static T TestMethod<T>(T item)
        {
            return item;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers<T>
    {
        public static T Test[||]Method(T item)
        {
            return item;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodAndRefactorUsageWithTrivia()
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
            // keep this comment, and the random spaces here
            return Class1. TestMethod( );
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
            // keep this comment, and the random spaces here
            return Class1Helpers. TestMethod( );
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodAndRefactorSourceUsage()
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

        public static int TestMethod2()
        {
            return TestMethod();
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
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveFieldAndRefactorSourceUsage()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Field = 0;

        public static int TestMethod2()
        {
            return TestField;
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
        public static int TestMethod2()
        {
            return Class1Helpers.TestField;
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int TestField = 0;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertyAndRefactorSourceUsage()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        private static int _testProperty;

        public static int Test[||]Property
        {
            get => _testProperty;
            set
            {
                _testProperty = value;
            }
        }

        public static int TestMethod2()
        {
            return TestProperty;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestProperty", "_testProperty");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestProperty;
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        private static int _testProperty;

        public static int Test[||]Property
        {
            get => _testProperty;
            set
            {
                _testProperty = value;
            }
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveGenericMethodAndRefactorImpliedUsage()
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

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1.TestMethod(5);
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
            return Class1Helpers.TestMethod(5);
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static T TestMethod<T>(T item)
        {
            return item;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveGenericMethodAndRefactorUsage()
        {
            var initialMarkup = @"
using System;

namespace TestNs1
{
    public class Class1
    {
        public static Type Test[||]Method<T>()
        {
            return typeof(T);
        }
    }

    public class Class2
    {
        public static Type TestMethod2()
        {
            return Class1.TestMethod<int>();
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
using System;

namespace TestNs1
{
    public class Class1
    {
    }

    public class Class2
    {
        public static Type TestMethod2()
        {
            return Class1Helpers.TestMethod<int>();
        }
    }
}";
            var expectedResult2 = @"using System;

namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static Type TestMethod<T>()
        {
            return typeof(T);
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodFromGenericClassAndRefactorUsage()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1<T>
    {
        public static T TestGeneric { get; set; }    

        public static T Test[||]Method()
        {
            return TestGeneric;
        }
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1<int>.TestMethod();
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod", "TestGeneric");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1<T>
    {
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers<int>.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers<T>
    {
        public static T TestGeneric { get; set; }

        public static T TestMethod()
        {
            return TestGeneric;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodFromGenericClassAndRefactorPartialTypeArgUsage()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1<T1, T2, T3>
        where T1 : new()
    {
        public static T1 Test[||]Method()
        {
            return new T1();
        }

        public static T2 TestGeneric2 { get; set; } 

        public T3 TestGeneric3 { get; set; }
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1<int, string, double>.TestMethod();
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1<T1, T2, T3>
        where T1 : new()
    {
        public static T2 TestGeneric2 { get; set; } 

        public T3 TestGeneric3 { get; set; }
    }

    public class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers<int>.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers<T1> where T1 : new()
    {
        public static T1 TestMethod()
        {
            return new T1();
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
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

        [Fact]
        public async Task TestMoveMethodAndRefactorClassAlias()
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
    using C1 = TestNs1.Class1;

    class Class2
    {
        public static int TestMethod2()
        {
            return C1.TestMethod();
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
    using C1 = TestNs1.Class1;

    class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodAndRefactorNamespaceAlias()
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
    using C1 = TestNs1;

    class Class2
    {
        public static int TestMethod2()
        {
            return C1.Class1.TestMethod();
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
    using C1 = TestNs1;

    class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodAndRefactorConflictingName()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int F[||]oo()
        {
            return 0;
        }
    }
}

namespace TestNs2
{
    using TestNs1;

    class Class2
    {
        class Class1Helpers
        {
            public static int Foo()
            {
                return 1;
            }
        }
        
        public static int TestMethod2()
        {
            return Class1.Foo() + Class1Helpers.Foo();
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("Foo");
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

    class Class2
    {
        class Class1Helpers
        {
            public static int Foo()
            {
                return 1;
            }
        }
        
        public static int TestMethod2()
        {
            return TestNs1.Class1Helpers.Foo() + Class1Helpers.Foo();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int Foo()
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
                // the test parser thinks "TestNs1.Class1Helpers" is a member access expression
                // but we made a qualified name. The text should still be the same
                CodeActionValidationMode = Testing.CodeActionValidationMode.None
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodAndRefactorQualifiedName()
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
    class Class2
    {
        public static int TestMethod2()
        {
            return TestNs1.Class1.TestMethod();
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult1 = @"
using TestNs1;

namespace TestNs1
{
    public class Class1
    {
    }
}

namespace TestNs2
{
    class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodAndRefactorStaticUsing()
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
    using static TestNs1.Class1;

    class Class2
    {
        public static int TestMethod2()
        {
            return TestMethod();
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
    using static TestNs1.Class1;

    class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodAndRefactorNamespaceAliasWithExtraNamespace()
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
    using C1 = TestNs1;

    class Class2
    {
        public static int TestMethod2()
        {
            return C1.Class1.TestMethod();
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
}

namespace TestNs2
{
    using TestNs1.ExtraNs;
    using C1 = TestNs1;

    class Class2
    {
        public static int TestMethod2()
        {
            return Class1Helpers.TestMethod();
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1.ExtraNs
{
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveExtensionMethodDoNotRefactor()
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
    internal static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveExtensionMethodRefactorMultipleImports()
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

        public int GetOtherInt2()
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

        public int GetOtherInt2()
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
    internal static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
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

        #region Perform Existing Type Action From Options
        [Fact]
        public async Task TestMoveFieldToExistingType()
        {
            var initialSourceMarkup = @"
public class Class1
{
    public static int Test[||]Field = 1;
}";
            var initialDestinationMarkup = @"
public class Class1Helpers
{
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestField");
            var fixedSourceMarkup = @"
public class Class1
{
}";
            var fixedDestinationMarkup = @"
public class Class1Helpers
{
    public static int TestField = 1;
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMovePropertyToExistingType()
        {
            var initialSourceMarkup = @"
public class Class1
{
    public static int Test[||]Property { get; set; }
}";
            var initialDestinationMarkup = @"
public class Class1Helpers
{
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestProperty");
            var fixedSourceMarkup = @"
public class Class1
{
}";
            var fixedDestinationMarkup = @"
public class Class1Helpers
{
    public static int TestProperty { get; set; }
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveEventToExistingType()
        {
            var initialSourceMarkup = @"
using System;

public class Class1
{
    public static event EventHandler Test[||]Event;
}";
            var initialDestinationMarkup = @"
public class Class1Helpers
{
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestEvent");
            var fixedSourceMarkup = @"
using System;

public class Class1
{
}";
            var fixedDestinationMarkup = @"
using System;

public class Class1Helpers
{
    public static event EventHandler TestEvent;
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodToExistingType()
        {
            var initialSourceMarkup = @"
public class Class1
{
    public static int Test[||]Method()
    {
        return 0;
    }
}";
            var initialDestinationMarkup = @"
public class Class1Helpers
{
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var fixedSourceMarkup = @"
public class Class1
{
}";
            var fixedDestinationMarkup = @"
public class Class1Helpers
{
    public static int TestMethod()
    {
        return 0;
    }
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveExtensionMethodToExistingType()
        {
            var initialSourceMarkup = @"
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
}";
            var initialDestinationMarkup = @"
public static class Class1Helpers
{
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var fixedSourceMarkup = @"
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
}";
            var fixedDestinationMarkup = @"
public static class Class1Helpers
{
    public static int TestMethod(this Other other)
    {
        return other.OtherInt + 2;
    }
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveConstFieldToExistingType()
        {
            var initialSourceMarkup = @"
public class Class1
{
    public const int Test[||]Field = 1;
}";
            var initialDestinationMarkup = @"
public class Class1Helpers
{
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestField");
            var fixedSourceMarkup = @"
public class Class1
{
}";
            var fixedDestinationMarkup = @"
public class Class1Helpers
{
    public const int TestField = 1;
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodToExistingTypeWithNamespace()
        {
            var initialSourceMarkup = @"
namespace TestNs
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }
    }
}";
            var initialDestinationMarkup = @"
namespace TestNs
{
    public class Class1Helpers
    {
    }
}";
            var selectedDestinationName = "TestNs.Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var fixedSourceMarkup = @"
namespace TestNs
{
    public class Class1
    {
    }
}";
            var fixedDestinationMarkup = @"
namespace TestNs
{
    public class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodToExistingTypeWithNewNamespace()
        {
            var initialSourceMarkup = @"
public class Class1
{
    public static int Test[||]Method()
    {
        return 0;
    }
}";
            var initialDestinationMarkup = @"
namespace TestNs
{
    public class Class1Helpers
    {
    }
}";
            var selectedDestinationName = "TestNs.Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var fixedSourceMarkup = @"
public class Class1
{
}";
            var fixedDestinationMarkup = @"
namespace TestNs
{
    public class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodToExistingTypeRefactorSourceUsage()
        {
            var initialSourceMarkup = @"
public class Class1
{
    public static int Test[||]Method()
    {
        return 0;
    }

    public static int TestMethod2()
    {
        return TestMethod();
    }
}";
            var initialDestinationMarkup = @"
public class Class1Helpers
{
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var fixedSourceMarkup = @"
public class Class1
{
    public static int TestMethod2()
    {
        return Class1Helpers.TestMethod();
    }
}";
            var fixedDestinationMarkup = @"
public class Class1Helpers
{
    public static int TestMethod()
    {
        return 0;
    }
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestMoveMethodToExistingTypeRefactorDestinationUsage()
        {
            var initialSourceMarkup = @"
public class Class1
{
    public static int Test[||]Method()
    {
        return 0;
    }
}";
            var initialDestinationMarkup = @"
public class Class1Helpers
{
    public static int TestMethod2()
    {
        return Class1.TestMethod();
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var fixedSourceMarkup = @"
public class Class1
{
}";
            var fixedDestinationMarkup = @"
public class Class1Helpers
{
    public static int TestMethod()
    {
        return 0;
    }
    public static int TestMethod2()
    {
        return Class1Helpers.TestMethod();
    }
}";

            await TestMovementExistingFileAsync(
                initialSourceMarkup,
                initialDestinationMarkup,
                fixedSourceMarkup,
                fixedDestinationMarkup,
                selectedMembers,
                selectedDestinationName).ConfigureAwait(false);
        }
        #endregion

        #region Selections and caret position

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}";
            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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
    internal static class Class1Helpers
    {
        public static int TestField = 1;
    }
}";

            await TestMovementNewFileAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectInMultipleFieldIdentifiers()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [|public static int Goo = 10, Foo = 9;|]
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("Goo", "Foo");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int Goo = 10;
        public static int Foo = 9;
    }
}";

            await TestMovementNewFileWithSelectionAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMultipleMembers1()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [|public static int Goo = 10, Foo = 9;

        public static int DoSomething()
        {
            return 5;
        }|]
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("Goo", "Foo", "DoSomething");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int Goo = 10;
        public static int Foo = 9;

        public static int DoSomething()
        {
            return 5;
        }
    }
}";

            await TestMovementNewFileWithSelectionAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMultipleMembers2()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {

        public static int DoSomething()
        {
            return [|5;
        }        
        public static int Goo = 10, Foo = 9;|]
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("Goo", "Foo");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {

        public static int DoSomething()
        {
            return 5;
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int Goo = 10;
        public static int Foo = 9;
    }
}";

            await TestMovementNewFileWithSelectionAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMultipleMembers3()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Go[|o = 10, Foo = 9;

        public static int DoSometh|]ing()
        {
            return 5;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("Goo", "Foo", "DoSomething");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int Goo = 10;
        public static int Foo = 9;

        public static int DoSomething()
        {
            return 5;
        }
    }
}";

            await TestMovementNewFileWithSelectionAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMultipleMembers4()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Goo = 10, F[|oo = 9;

        public static in|]t DoSomething()
        {
            return 5;
        }
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("Foo");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Goo = 10;

        public static int DoSomething()
        {
            return 5;
        }
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int Foo = 9;
    }
}";

            await TestMovementNewFileWithSelectionAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectOneOfMultipleFieldIdentifiers()
        {
            // However, a semicolon after the initializer is still considered a declaration
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public static int G[||]oo = 10, Foo = 9;
    }
}";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("Goo");
            var expectedResult1 = @"
namespace TestNs1
{
    public class Class1
    {
        public static int Foo = 9;
    }
}";
            var expectedResult2 = @"namespace TestNs1
{
    internal static class Class1Helpers
    {
        public static int Goo = 10;
    }
}";

            await TestMovementNewFileWithSelectionAsync(initialMarkup, expectedResult1, expectedResult2, newFileName, selectedMembers, selectedDestinationName).ConfigureAwait(false);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task TestSelectMalformedMethod_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public st[||] {|CS1519:int|} TestMethod()
        {
            return 0;
        }
    }
}";
            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMalformedField_NoAction1()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public st[||] {|CS1519:int|} TestField = 0;
    }
}";
            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMalformedField_NoAction2()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        public st [|{|CS1519:int|} Test|]Field = 0;
    }
}";
            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMalformedField_NoAction3()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [|public st {|CS1519:int|} TestField = 0;|]
    }
}";
            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectMalformedField_NoAction4()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [|publicc {|CS1585:static|} int TestField = 0;|]
    }
}";
            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task TestSelectStaticConstructor1_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [|static Class1()|]
        {
        }
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectStaticConstructor2_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        static Cl[||]ass1()
        {
        }
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectOperator_NoAction()
        {
            var initialMarkup = @"
namespace TestNs1
{
    public class Class1
    {
        [|public static Class1 operator +(Class1 a, Class1 b)|]
        {
            return new Class1();
        }
    }
}";
            await TestNoRefactoringAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectTopLevelStatement_NoAction1()
        {
            var initialMarkup = @"
using System;

[||]Console.WriteLine(5);
";

            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication
                },
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectTopLevelStatement_NoAction2()
        {
            var initialMarkup = @"
using System;

[|Console.WriteLine(5);|]
";

            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication
                },
            }.RunAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task TestSelectTopLevelLocalFunction_NoAction()
        {
            var initialMarkup = @"
DoSomething();

static int Do[||]Something()
{
    return 5;
}
";

            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp10,
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication
                },
            }.RunAsync().ConfigureAwait(false);
        }
        #endregion

        private class Test : VerifyCS.Test
        {
            public Test(
                string destinationType,
                ImmutableArray<string> selection,
                string? destinationName,
                bool testPreselection = false,
                bool createNew = true)
            {
                _destinationType = destinationType;
                _selection = selection;
                _destinationName = destinationName;
                _testPreselection = testPreselection;
                _createNew = createNew;
            }

            private readonly string _destinationType;

            private readonly ImmutableArray<string> _selection;

            private readonly string? _destinationName;

            private readonly bool _createNew;

            private readonly bool _testPreselection;

            protected override Task<Workspace> CreateWorkspaceImplAsync()
            {
                var hostServices = s_testServices.GetHostServices();

                var workspace = new AdhocWorkspace(hostServices);
                var testOptionsService = (TestMoveStaticMembersService)workspace.Services.GetRequiredService<IMoveStaticMembersOptionsService>();
                testOptionsService.DestinationName = _destinationType;
                testOptionsService.SelectedMembers = _selection;
                testOptionsService.Filename = _destinationName;
                testOptionsService.CreateNew = _createNew;
                testOptionsService.ExpectedPrecheckedMembers = _testPreselection ? _selection : ImmutableArray<string>.Empty;

                return Task.FromResult<Workspace>(workspace);
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

        private static async Task TestMovementNewFileWithSelectionAsync(
            string initialMarkup,
            string expectedSource,
            string expectedNewFile,
            string newFileName,
            ImmutableArray<string> selectedMembers,
            string newTypeName)
            => await new Test(newTypeName, selectedMembers, newFileName, testPreselection: true)
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

        private static async Task TestMovementExistingFileAsync(
            string intialSourceMarkup,
            string initialDestinationMarkup,
            string fixedSourceMarkup,
            string fixedDestinationMarkup,
            ImmutableArray<string> selectedMembers,
            string selectedDestinationType,
            string? selectedDestinationFile = null)
        {
            var test = new Test(selectedDestinationType, selectedMembers, selectedDestinationFile, createNew: false);
            test.TestState.Sources.Add(intialSourceMarkup);
            test.FixedState.Sources.Add(fixedSourceMarkup);
            if (selectedDestinationFile != null)
            {
                test.TestState.Sources.Add((selectedDestinationFile, initialDestinationMarkup));
                test.FixedState.Sources.Add((selectedDestinationFile, fixedDestinationMarkup));
            }
            else
            {
                test.TestState.Sources.Add(initialDestinationMarkup);
                test.FixedState.Sources.Add(fixedDestinationMarkup);
            }

            await test.RunAsync().ConfigureAwait(false);
        }

        private static async Task TestNoRefactoringAsync(string initialMarkup)
        {
            await new Test("", ImmutableArray<string>.Empty, "")
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
            }.RunAsync().ConfigureAwait(false);
        }
    }
}
