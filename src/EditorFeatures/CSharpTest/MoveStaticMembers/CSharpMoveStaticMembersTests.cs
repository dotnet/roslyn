// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.MoveStaticMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveStaticMembers
{
    public class CSharpMoveStaticMembersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
        {
            return new CSharpMoveStaticMembersRefactoringProvider((IMoveStaticMembersOptionsService)parameters.fixProviderData);
        }

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions) => FlattenActions(actions);

        private async Task TestNoRefactoringProvidedAsync(
            string initialMarkup,
            TestParameters parameters = default)
        {
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters);
            var (actions, _) = await GetCodeActionsAsync(workspace, parameters).ConfigureAwait(false);
            // no actions should be provided
            Assert.Equal(0, actions.Length);
        }

        internal Task TestMovementAsync(
            string initialMarkUp,
            string expectedResult,
            string destinationType,
            ImmutableArray<string> selection,
            string destinationName = "a.cs",
            TestParameters parameters = default)
        {
            var service = new TestMoveStaticMembersService(destinationType, destinationName, selection.AsImmutable());

            return TestInRegularAndScript1Async(
                initialMarkUp,
                expectedResult,
                parameters.WithFixProviderData(service));
        }

        #region Perform Actions From Options
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveField()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Field = 1;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveProperty()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Property { get; set; }
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestProperty");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestProperty { get; set; }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveEvent()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
using System;

namespace TestNs1
{
    public class Class1
    {
        public static event EventHandler Test[||]Event;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestEvent");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
using System;

namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">using System;

namespace TestNs1
{
    static class Class1Helpers
    {
        public static event EventHandler TestEvent;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethod()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveExtensionMethod()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveConstField()
        {
            // const is static so we should work here
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public const int Test[||]Field = 1;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public const int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMultipleMethods()
        {
            // Bug where PullMembersUp doesn't apply formatting annotation
            // When the destination ends up with more than one member:
            // https://github.com/dotnet/roslyn/issues/54847
            // so we keep the weird formatting for now
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethodInt", "TestMethodBool");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
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
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveSingleMethodFromMultiple()
        {
            // move the method that this was not triggered on
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethodBool");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethodInt()
        {
            return 0;
        }
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {

        public static bool TestMethodBool()
        {
            return false;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveOneOfEach()
        {
            // Bug where PullMembersUp doesn't apply formatting annotation
            // When the destination ends up with more than one member:
            // https://github.com/dotnet/roslyn/issues/54847
            // so we keep the weird formatting for now
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
using System;

namespace TestNs1
{
    public class Class1
    {
        public static int Test[||]Method()
        {
            return 0;
        }

        public static bool TestProperty { get; set; }

        public static int TestField;

        public static event EventHandler TestEvent;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create(
                "TestMethod",
                "TestField",
                "TestProperty",
                "TestEvent");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
using System;

namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">using System;

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
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestInNestedClass()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public class NestedClass1
        {
            public static int Test[||]Field = 1;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public class NestedClass1
        {
        }
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestInNestedNamespace()
        {
            // collapse the namespaces
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    namespace InnerNs
    {
        public class Class1
        {
            public static int Test[||]Field = 1;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    namespace InnerNs
    {
        public class Class1
        {
        }
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1.InnerNs
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveFieldNoNamespace()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
public class Class1
{
    public static int Test[||]Field = 1;
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
public class Class1
{
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">static class Class1Helpers
{
    public static int TestField = 1;
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveFieldNewNamespace()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
public class Class1
{
    public static int Test[||]Field = 1;
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "NewNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
public class Class1
{
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace NewNs
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethodWithNamespacedSelectedDestination()
        {
            // in the case that we have an extra namespace in the destination name
            // we append it on to the old type's namespace
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "ExtraNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1.ExtraNs
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethodFileScopedNamespace()
        {
            // We still keep normal namespacing rules in the new file regardless
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1;

public class Class1
{
    public static int Test[||]Method()
    {
        return 0;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1;

public class Class1
{
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethodWithFolders()
        {
            // We should just put the new file in the same folder
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document Folders=""Folder1"" FilePath=""Class1.cs"">
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
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document Folders=""Folder1"" FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document Folders=""Folder1"" FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveGenericMethod()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static T Test[||]Method&lt;T&gt;(T item)
        {
            return item;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static T TestMethod&lt;T&gt;(T item)
        {
            return item;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethodWithGenericClass()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1&lt;T&gt;
    {
        public static T Test[||]Method(T item)
        {
            return item;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1&lt;T&gt;
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers&lt;T&gt;
    {
        public static T Test[||]Method(T item)
        {
            return item;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethodAndRefactorUsage()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethodAndRefactorUsageDifferentNamespace()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveMethodAndRefactorUsageNewNamespace()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "ExtraNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1.ExtraNs
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveExtensionMethodDontRefactor()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestMoveExtensionMethodRefactorImports()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "ExtraNs.Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
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
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">using TestNs2;

namespace TestNs1.ExtraNs
{
    static class Class1Helpers
    {
        public static int TestMethod(this Other other)
        {
            return other.OtherInt + 2;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }
        #endregion

        #region Selections and caret position

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInMethodParens()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethod([||])
        {
            return 0;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectWholeFieldDeclaration()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        [|public static int TestField = 1;|]
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectBeforeKeywordOfDeclaration()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        [||]public static int TestField = 1;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInKeyWordOfDeclaration1()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        pub[||]lic static int TestField = 1;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInKeyWordOfDeclaration2()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public st[||]atic int TestField = 1;
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInTypeIdentifierMethodDeclaration()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static i[||]nt TestMethod()
        {
            return 0;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestMethod");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestMethod()
        {
            return 0;
        }
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInTypeIdentifierOfFieldDeclaration_NoAction()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static i[||]nt TestField = 1;
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInFieldInitializerEquals_NoAction()
        {
            // The initializer isn't a member declaration
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int TestField =[||] 1;
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInFieldInitializerAfterSemicolon()
        {
            // However, a semicolon after the initializer is still considered a declaration
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int TestField = 1;[||]
    }
}
        </Document>
    </Project>
</Workspace>";
            var selectedDestinationName = "Class1Helpers";
            var newFileName = "Class1Helpers.cs";
            var selectedMembers = ImmutableArray.Create("TestField");
            var expectedResult = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
    }
}
        </Document>
        <Document FilePath=""Class1Helpers.cs"">namespace TestNs1
{
    static class Class1Helpers
    {
        public static int TestField = 1;
    }
}</Document>
    </Project>
</Workspace>";
            await TestMovementAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectMethodBody_NoAction()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethod()
        {
            retu[||]rn 0;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectMethodBracket_NoAction()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int TestMethod()
        [|{|]
            return 0;
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectPropertyBody_NoAction()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public static int TestProperty { get; [||]set; }
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectNonStaticProperty_NoAction()
        {
            var initialMarkup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly1"" CommonReferences=""true"">
        <Document FilePath=""Class1.cs"">
namespace TestNs1
{
    public class Class1
    {
        public int Test[||]Property { get; set; }
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestNoRefactoringProvidedAsync(initialMarkup).ConfigureAwait(false);
        }
        #endregion
    }
}
