// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class InitializerExpressionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(InitializerExpressionSignatureHelpProvider);

    [Fact]
    public Task WithSingleParamAddMethods()
        => TestAsync("""
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    new List<int> { { $$
                }
            }
            """, [new("void List<int>.Add(int item)", currentParameterIndex: 0)]);

    [Fact]
    public Task ForMultiParamAddMethods()
        => TestAsync("""
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    new Dictionary<int, string> { { $$
                }
            }
            """, [new("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0)]);

    [Fact]
    public Task ForSecondParam()
        => TestAsync("""
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    new Dictionary<int, string> { { 0, $$
                }
            }
            """, [new("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 1)]);

    [Fact]
    public Task ForNestedCollectionInitializer()
        => TestAsync("""
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
            """, [new("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0)]);

    [Fact]
    public Task WithoutClosingBraces()
        => TestAsync("""
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
            """, [new("void Dictionary<int, string>.Add(int key, string value)", currentParameterIndex: 0)]);

    [Fact]
    public Task WithMultipleAddMethods()
        => TestAsync("""
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
            """, [
            new("void Bar.Add(int i)", currentParameterIndex: 0),
            new("void Bar.Add(int i, string s)", currentParameterIndex: 0, isSelected: true),
            new("void Bar.Add(int i, string s, bool b)", currentParameterIndex: 0)]);

    [Fact]
    public Task DoesNotImplementIEnumerable()
        => TestAsync("""
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
            """, expectedOrderedItemsOrNull: []);

    [Fact]
    public Task WithExtensionAddMethods()
        => TestAsync("""
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
            """, [
            new($"({CSharpFeaturesResources.extension}) void Bar.Add(int i)", currentParameterIndex: 0),
            new($"({CSharpFeaturesResources.extension}) void Bar.Add(int i, string s)", currentParameterIndex: 0, isSelected: true),
            new($"({CSharpFeaturesResources.extension}) void Bar.Add(int i, string s, bool b)", currentParameterIndex: 0)], sourceCodeKind: SourceCodeKind.Regular);
}
