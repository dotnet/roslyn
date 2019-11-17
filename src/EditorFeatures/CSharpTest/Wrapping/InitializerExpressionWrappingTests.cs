using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class IntializerExpressionWrappingTests : AbstractWrappingTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpWrappingCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestNoWrappingSuggestions()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        var test = new[] [||]{ 1 };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestWrappingShortInitializerExpression()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        var test = new[] [||]{ 1, 2 };
    }
}",
@"class C {
    void Bar() {
        var test = new[]
        {
            1,
            2
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestWrappingLongIntializerExpression()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        var test = new[] [||]{ ""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog"" };
     }
}",
@"class C {
    void Bar() {
        var test = new[]
        {
            ""the"",
            ""quick"",
            ""brown"",
            ""fox"",
            ""jumps"",
            ""over"",
            ""the"",
            ""lazy"",
            ""dog""
        };
     }
}",
@"class C {
    void Bar() {
        var test = new[]
        {
            ""the"", ""quick"", ""brown"", ""fox"", ""jumps"", ""over"", ""the"", ""lazy"", ""dog""
        };
     }
}"
);
        }
    }
}
