// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveStaticMembers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.MoveStaticMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveStaticMembers;
using Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveStaticMembers
{
    public class CSharpMoveStaticMembersTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
        {
            return new CSharpMoveStaticMembersCodeRefactoringProvider((IMoveStaticMembersOptionsService)parameters.fixProviderData);
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

        internal Task TestWithDialogAsync(
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

        #region Perform Actions From Dialog
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestWithDialogAsync(initialMarkup,
                expectedResult,
                selectedDestinationName,
                selectedMembers,
                newFileName).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)]
        public async Task TestSelectInTypeIdentifierOfDeclaration_NoAction()
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
            await TestWithDialogAsync(initialMarkup,
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
            await TestNoRefactoringProvidedAsync(initialMarkup);
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
            await TestNoRefactoringProvidedAsync(initialMarkup);
        }
        #endregion
    }
}
