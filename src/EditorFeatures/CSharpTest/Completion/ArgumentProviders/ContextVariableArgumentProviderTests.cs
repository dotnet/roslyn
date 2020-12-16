﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.ArgumentProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class ContextVariableArgumentProviderTests : AbstractCSharpArgumentProviderTests
    {
        internal override Type GetArgumentProviderType()
            => typeof(ContextVariableArgumentProvider);

        [Theory]
        [InlineData("string")]
        [InlineData("bool")]
        [InlineData("int?")]
        public async Task TestLocalVariable(string type)
        {
            var markup = $@"
class C
{{
    void Method()
    {{
        {type} arg = default;
        this.Target($$);
    }}

    void Target({type} arg)
    {{
    }}
}}
";

            await VerifyDefaultValueAsync(markup, "arg");
            await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
        }

        [Theory]
        [InlineData("string")]
        [InlineData("bool")]
        [InlineData("int?")]
        public async Task TestParameter(string type)
        {
            var markup = $@"
class C
{{
    void Method({type} arg)
    {{
        this.Target($$);
    }}

    void Target({type} arg)
    {{
    }}
}}
";

            await VerifyDefaultValueAsync(markup, "arg");
            await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
        }

        [Theory]
        [InlineData("string")]
        [InlineData("bool")]
        [InlineData("int?")]
        public async Task TestInstanceVariable(string type)
        {
            var markup = $@"
class C
{{
    {type} arg;

    void Method()
    {{
        this.Target($$);
    }}

    void Target({type} arg)
    {{
    }}
}}
";

            await VerifyDefaultValueAsync(markup, "arg");
            await VerifyDefaultValueAsync(markup, expectedDefaultValue: null, previousDefaultValue: "prior");
        }

        // Note: The current implementation checks for exact type and name match. If this changes, some of these tests
        // may need to be updated to account for the new behavior.
        [Theory]
        [InlineData("object", "string")]
        [InlineData("string", "object")]
        [InlineData("bool", "bool?")]
        [InlineData("bool", "int")]
        [InlineData("int", "object")]
        public async Task TestMismatchType(string parameterType, string valueType)
        {
            var markup = $@"
class C
{{
    void Method({valueType} arg)
    {{
        this.Target($$);
    }}

    void Target({parameterType} arg)
    {{
    }}
}}
";

            await VerifyDefaultValueAsync(markup, null);
        }
    }
}
