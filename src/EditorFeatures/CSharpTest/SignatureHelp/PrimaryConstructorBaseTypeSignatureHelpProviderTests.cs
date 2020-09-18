// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class PrimaryConstructorBaseTypeSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public PrimaryConstructorBaseTypeSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override Type GetSignatureHelpProviderType()
            => typeof(PrimaryConstructorBaseTypeSignatureHelpProvider);

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task PrimaryConstructorBaseType_FirstParameter()
        {
            var markup = @"
record Base(int Identifier)
{
    private Base(string ignored) : this(1, 2) { }
}
record Derived(int Other) : [|Base($$1|]);
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(Base original)", string.Empty, null, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(int Identifier)", string.Empty, null, currentParameterIndex: 0, isSelected: true));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task PrimaryConstructorBaseType_SecondParameter()
        {
            var markup = @"
record Base(int Identifier1, int Identifier2)
{
    protected Base(string name) : this(1, 2) { }
}
record Derived(int Other) : [|Base(1, $$2|]);
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(Base original)", string.Empty, null, currentParameterIndex: 1));
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(string name)", string.Empty, null, currentParameterIndex: 1));
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 1, isSelected: true));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task CommentOnBaseConstructor()
        {
            var markup = @"
record Base(int Identifier1, int Identifier2)
{
    /// <summary>Summary for constructor</summary>
    protected Base(string name) : this(1, 2) { }
}
record Derived(int Other) : [|Base(1, $$2|]);
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(Base original)", string.Empty, null, currentParameterIndex: 1));
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(string name)", "Summary for constructor", null, currentParameterIndex: 1));
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 1, isSelected: true));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task CommentOnBaseConstructorAndParameters()
        {
            var markup = @"
record Base(int Identifier1, int Identifier2)
{
    /// <summary>Summary for constructor</summary>
    /// <param name=""name"">Param name</param>
    protected Base(string name) : this(1, 2) { }
}
record Derived(int Other) : [|Base($$1, 2|]);
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(Base original)", string.Empty, null, currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(string name)", "Summary for constructor", "Param name", currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 0, isSelected: true));

            await TestAsync(markup, expectedOrderedItems);
        }
    }
}
