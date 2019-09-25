// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public class InitializerExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        public InitializerExpressionSignatureHelpProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override ISignatureHelpProvider CreateSignatureHelpProvider()
            => new InitializerExpressionSignatureHelpProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task NotForSingleParamAddMethods()
        {
            var markup = @"
using System.Collections.Generic;

class C
{
    void Goo()
    {
        new List<int> { { $$
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ForMultiParamAddMethods()
        {
            var markup = @"
using System.Collections.Generic;

class C
{
    void Goo()
    {
        new Dictionary<int, string> { { $$
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ForSecondParam()
        {
            var markup = @"
using System.Collections.Generic;

class C
{
    void Goo()
    {
        new Dictionary<int, string> { { 0, $$
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 1));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task ForNestedCollectionInitializer()
        {
            var markup = @"
using System.Collections.Generic;

class Bar
{
    public Dictionary<int, string> D;
}

class C
{
    void Goo()
    {
        new Bar { D = { { $$
    }
}";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task WithoutClosingBraces()
        {
            var markup = @"
using System.Collections.Generic;

class Bar
{
    public Dictionary<int, string> D;
}

class C
{
    void Goo()
    {
        new Bar { D = { { $$
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task WithMultipleAddMethods()
        {
            var markup = @"
using System.Collections;

class Bar : IEnumerable
{
    public void Add(int i) { }
    public void Add(int i, string s) { }
    public void Add(int i, string s, bool b) { }
}

class C
{
    void Goo()
    {
        new Bar { { $$
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Bar.Add(int i, string s)", currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem("void Bar.Add(int i, string s, bool b)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task DoesNotImplementIEnumerable()
        {
            var markup = @"
using System.Collections;

class Bar
{
    public void Add(int i) { }
    public void Add(int i, string s) { }
    public void Add(int i, string s, bool b) { }
}

class C
{
    void Goo()
    {
        new Bar { { $$
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task WithExtensionAddMethods()
        {
            var markup = @"
using System.Collections;

class Bar : IEnumerable
{
}

static class Extensions
{
    public static void Add(this Bar b, int i) { }
    public static void Add(this Bar b, int i, string s) { }
    public static void Add(this Bar b, int i, string s, bool b) { }
}

class C
{
    void Goo()
    {
        new Bar { { $$
";

            var expectedOrderedItems = new List<SignatureHelpTestItem>();
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void Bar.Add(int i, string s)", currentParameterIndex: 0));
            expectedOrderedItems.Add(new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void Bar.Add(int i, string s, bool b)", currentParameterIndex: 0));

            await TestAsync(markup, expectedOrderedItems, sourceCodeKind: SourceCodeKind.Regular);
        }
    }
}
