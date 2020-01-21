// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class ChainedExpressionWrappingTests : AbstractWrappingTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpWrappingCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSyntaxError()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        [||]the.quick().brown.fox(,);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithoutEnoughChunks()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        [||]the.quick();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestWithEnoughChunks()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown().fox.jumped();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
            .jumped();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestGenericNames()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown<int>().fox.jumped<string, bool>();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown<int>().fox
            .jumped<string, bool>();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown<int>().fox
                 .jumped<string, bool>();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestElementAccess()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown[1, 2, 3].fox.jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the.quick.brown[1, 2, 3].fox
            .jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the.quick.brown[1, 2, 3].fox
                 .jumped[1][2][3];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestUnwrap()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown[1, 2, 3].fox
                 .jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the.quick.brown[1, 2, 3].fox
            .jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the.quick.brown[1, 2, 3].fox.jumped[1][2][3];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestWrapAndUnwrap()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.
                brown[1, 2, 3]
           .fox.jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the.quick.brown[1, 2, 3].fox
            .jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the.quick.brown[1, 2, 3].fox
                 .jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the.quick.brown[1, 2, 3].fox.jumped[1][2][3];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestChunkMustHaveDottedSection()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the().quick.brown[1, 2, 3].fox.jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the().quick.brown[1, 2, 3].fox
            .jumped[1][2][3];
    }
}",
@"class C {
    void Bar() {
        the().quick.brown[1, 2, 3].fox
                   .jumped[1][2][3];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TrailingNonCallIsNotWrapped()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown().fox.jumped().over;
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over;
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TrailingLongWrapping1()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown().fox.jumped().over.the().lazy().dog();
    }
}",
GetIndentionColumn(35),
@"class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over
            .the()
            .lazy()
            .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the()
                 .lazy()
                 .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over.the()
            .lazy().dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the().lazy()
                 .dog();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TrailingLongWrapping2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown().fox.jumped().over.the().lazy().dog();
    }
}",
GetIndentionColumn(40),
@"class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over
            .the()
            .lazy()
            .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the()
                 .lazy()
                 .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over.the().lazy()
            .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over.the()
                 .lazy().dog();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TrailingLongWrapping3()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown().fox.jumped().over.the().lazy().dog();
    }
}",
GetIndentionColumn(60),
@"class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over
            .the()
            .lazy()
            .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the()
                 .lazy()
                 .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox.jumped().over.the().lazy()
            .dog();
    }
}",
@"class C {
    void Bar() {
        the.quick.brown().fox.jumped().over.the().lazy()
                 .dog();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInConditionalAccess()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        the?.[||]quick.brown().fox.jumped();
    }
}",
@"class C {
    void Bar() {
        the?.quick.brown().fox
            .jumped();
    }
}",
@"class C {
    void Bar() {
        the?.quick.brown().fox
                  .jumped();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInConditionalAccess2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        the?.[||]quick.brown()?.fox.jumped();
    }
}",
@"class C {
    void Bar() {
        the?.quick.brown()?.fox
            .jumped();
    }
}",
@"class C {
    void Bar() {
        the?.quick.brown()?.fox
                  .jumped();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInConditionalAccess3()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        the?.[||]quick.brown()?.fox().jumped();
    }
}",
@"class C {
    void Bar() {
        the?.quick.brown()?.fox()
            .jumped();
    }
}",
@"class C {
    void Bar() {
        the?.quick.brown()?.fox()
                  .jumped();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInConditionalAccess4()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the?.quick().brown()?.fox().jumped();
    }
}",
@"class C {
    void Bar() {
        the?.quick()
            .brown()?.fox()
            .jumped();
    }
}");
        }
    }
}
