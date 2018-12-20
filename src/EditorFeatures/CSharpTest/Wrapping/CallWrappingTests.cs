// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class CallWrappingTests : AbstractWrappingTests
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
        the.quick.brown()
           .fox.jumped();
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
        the.quick.brown()
           .fox.jumped();
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
        the.quick.brown[1, 2, 3]
           .fox.jumped[1][2][3];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestUnwrap()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        [||]the.quick.brown[1, 2, 3]
           .fox.jumped[1][2][3];
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
        the.quick.brown[1, 2, 3]
           .fox.jumped[1][2][3];
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
        the().quick.brown[1, 2, 3]
             .fox.jumped[1][2][3];
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
        the.quick.brown()
           .fox.jumped().over;
    }
}");
        }
    }
}
