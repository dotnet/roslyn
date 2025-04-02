// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class WithElementSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(WithElementSignatureHelpProvider);

    [Theory]
    [InlineData("IList<int>")]
    [InlineData("ICollection<int>")]
    public async Task TestMutableInterfaces(string type)
    {
        var markup = $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" LanguageVersion="{{LanguageVersionExtensions.CSharpNext}}">
                    <Document><![CDATA[
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    {{type}} list = [with($$)];
                }
            }]]></Document>
                </Project>
            </Workspace>
            """;

        await TestAsync(markup, [new("List<int>(int capacity)", string.Empty, null, currentParameterIndex: 0)]);
    }

    [Theory]
    [InlineData("IReadOnlyList<int>")]
    [InlineData("IReadOnlyCollection<int>")]
    [InlineData("IEnumerable<int>")]
    [InlineData("IEnumerable")]
    public async Task TestReadOnlyInterfaces(string type)
    {
        var markup = $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" LanguageVersion="{{LanguageVersionExtensions.CSharpNext}}">
                    <Document><![CDATA[
            using System;
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    {{type}} list = [with($$)];
                }
            }]]></Document>
                </Project>
            </Workspace>
            """;

        await TestAsync(markup, []);
    }

    [Fact]
    public async Task TestConstructibleType()
    {
        var markup = $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" LanguageVersion="{{LanguageVersionExtensions.CSharpNext}}">
                    <Document><![CDATA[
            using System;
            using System.Collections.Generic;

            class C
            {
                void Goo()
                {
                    HashSet<int> set = [with($$)];
                }
            }]]></Document>
                </Project>
            </Workspace>
            """;

        await TestAsync(markup, [
            new("HashSet<int>()", string.Empty, null, currentParameterIndex: 0),
            new("HashSet<int>(IEnumerable<int> collection)", string.Empty, null, currentParameterIndex: 0),
            new("HashSet<int>(IEqualityComparer<int> comparer)", string.Empty, null, currentParameterIndex: 0),
            new("HashSet<int>(IEnumerable<int> collection, IEqualityComparer<int> comparer)", string.Empty, null, currentParameterIndex: 0)]);
    }

    [Fact]
    public async Task TestBuilder1()
    {
        var markup = $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" LanguageVersion="{{LanguageVersionExtensions.CSharpNext}}">
                    <Document><![CDATA[
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            namespace System
            {
                public readonly ref struct ReadOnlySpan<T> { }
            }

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
                internal sealed class CollectionBuilderAttribute : Attribute
                {
                    public CollectionBuilderAttribute(Type builderType, string methodName) { }
                }
            }

            [CollectionBuilder(typeof(MyCollectionBuilder), "Create")]
            class MyCollection<T> : IEnumerable<T>
            {
                public IEnumerator<T> GetEnumerator() => new System.NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => new System.NotImplementedException();
            }

            static class MyCollectionBuilder
            {
                public static MyCollection<T> Create<T>(ReadOnlySpan<T> values, int capacity, int extra) => new System.NotImplementedException();
                public static MyCollection<T> Create<T>(string capacity, string extra, ReadOnlySpan<T> values) => new System.NotImplementedException();
            }

            class C
            {
                public void Test()
                {
                    MyCollection<int> z = [with($$)];
                }
            }]]></Document>
                </Project>
            </Workspace>
            """;
        await TestAsync(markup, [
            new("MyCollection<int> MyCollectionBuilder.Create<T>(int capacity, int extra)", string.Empty, null, currentParameterIndex: 0),
            new("MyCollection<int> MyCollectionBuilder.Create<T>(string capacity, string extra)", string.Empty, null, currentParameterIndex: 0)]);
    }
}
