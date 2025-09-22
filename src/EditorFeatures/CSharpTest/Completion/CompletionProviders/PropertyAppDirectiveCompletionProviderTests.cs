// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.FileBasedPrograms;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class PropertyAppDirectiveCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(PropertyAppDirectiveCompletionProvider);

    private static string GetMarkup(string code) => $$"""
        <Workspace>
            <Project Language="C#" CommonReferences="true" AssemblyName="Test1" Features="FileBasedProgram=true">
            <Document><![CDATA[
                {{code}}
            ]]></Document>
            </Project>
        </Workspace>
        """;

    [Fact(Skip = "TODO2 workspace markup doesn't seem to be respected from this helper")]
    public async Task VerifyCommitCharacters()
    {
        await VerifyCommonCommitCharactersAsync(GetMarkup("""
            // comment

            #:$$

            using NS;

            public class C { }
            """), "p");
    }

    [Fact]
    public async Task AfterHashColon()
    {
        await VerifyItemExistsAsync(GetMarkup("""
            #:$$
            """), expectedItem: "property");
    }

    [Fact]
    public async Task NotAfterHashOnly()
    {
        await VerifyItemIsAbsentAsync(GetMarkup("""
            #$$
            """), expectedItem: "property");
    }

    [Fact]
    public async Task NotAfterColonOnly()
    {
        await VerifyItemIsAbsentAsync(GetMarkup("""
            :$$
            """), expectedItem: "property");
    }

    [Fact]
    public async Task NotAfterStatement()
    {
        await VerifyItemIsAbsentAsync(GetMarkup("""
            Console.WriteLine();
            $$
            """), expectedItem: "property");
    }
}
