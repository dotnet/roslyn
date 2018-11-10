// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Editor.Wrapping;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class ParameterWrappingTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpParameterWrappingCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_NoIndentFirst_AlignAll_TwoParams()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Foo([||]int i, int j) {
    }
}",
@"class C {
    void Foo(int i,
             int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_NoIndentFirst_AlignAll_ThreeParams()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Foo([||]int i, int j, int k) {
    }
}",
@"class C {
    void Foo(int i,
             int j,
             int k) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_IndentFirst_AlignAll_TwoParams()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Foo([||]int i, int j) {
    }
}",
@"class C {
    void Foo(
        int i,
        int j) {
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_IndentFirst_AlignAll_ThreeParams()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Foo([||]int i, int j, int k) {
    }
}",
@"class C {
    void Foo(
        int i,
        int j,
        int k) {
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_NoIndentFirst_NoAlignAll_TwoParams()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Foo([||]int i, int j) {
    }
}",
@"class C {
    void Foo(int i,
        int j) {
    }
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_NoIndentFirst_NoAlignAll_ThreeParams()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Foo([||]int i, int j, int k) {
    }
}",
@"class C {
    void Foo(int i,
        int j,
        int k) {
    }
}", index: 2);
        }
    }
}
