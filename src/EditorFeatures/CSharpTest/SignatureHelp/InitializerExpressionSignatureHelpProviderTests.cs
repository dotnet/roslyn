// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    [Trait(Traits.Feature, Traits.Features.SignatureHelp)]
    public class InitializerExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
    {
        internal override Type GetSignatureHelpProviderType()
            => typeof(InitializerExpressionSignatureHelpProvider);

        [Fact]
        public async Task WithSingleParamAddMethods()
        {
            var markup = """
                using System.Collections.Generic;

                class C
                {
                    void Goo()
                    {
                        new List<int> { { $$
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void List<int>.Add(int item)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task ForMultiParamAddMethods()
        {
            var markup = """
                using System.Collections.Generic;

                class C
                {
                    void Goo()
                    {
                        new Dictionary<int, string> { { $$
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task ForSecondParam()
        {
            var markup = """
                using System.Collections.Generic;

                class C
                {
                    void Goo()
                    {
                        new Dictionary<int, string> { { 0, $$
                    }
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 1)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task ForNestedCollectionInitializer()
        {
            var markup = """
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
                }
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task WithoutClosingBraces()
        {
            var markup = """
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
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task WithMultipleAddMethods()
        {
            var markup = """
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
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem("void Bar.Add(int i)", currentParameterIndex: 0),
                new SignatureHelpTestItem("void Bar.Add(int i, string s)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem("void Bar.Add(int i, string s, bool b)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task DoesNotImplementIEnumerable()
        {
            var markup = """
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
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>();

            await TestAsync(markup, expectedOrderedItems);
        }

        [Fact]
        public async Task WithExtensionAddMethods()
        {
            var markup = """
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
                """;

            var expectedOrderedItems = new List<SignatureHelpTestItem>
            {
                new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void Bar.Add(int i)", currentParameterIndex: 0),
                new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void Bar.Add(int i, string s)", currentParameterIndex: 0, isSelected: true),
                new SignatureHelpTestItem($"({CSharpFeaturesResources.extension}) void Bar.Add(int i, string s, bool b)", currentParameterIndex: 0)
            };

            await TestAsync(markup, expectedOrderedItems, sourceCodeKind: SourceCodeKind.Regular);
        }
    }
}
