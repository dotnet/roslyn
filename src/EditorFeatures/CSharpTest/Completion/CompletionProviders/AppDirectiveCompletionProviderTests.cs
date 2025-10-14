﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class SdkAppDirectiveCompletionProviderTests : AbstractAppDirectiveCompletionProviderTests
{
    protected override string DirectiveKind => "sdk";

    internal override Type GetCompletionProviderType()
        => typeof(SdkAppDirectiveCompletionProvider);
}

public sealed class PropertyAppDirectiveCompletionProviderTests : AbstractAppDirectiveCompletionProviderTests
{
    protected override string DirectiveKind => "property";

    internal override Type GetCompletionProviderType()
        => typeof(PropertyAppDirectiveCompletionProvider);
}

public sealed class PackageAppDirectiveCompletionProviderTests : AbstractAppDirectiveCompletionProviderTests
{
    protected override string DirectiveKind => "package";

    internal override Type GetCompletionProviderType()
        => typeof(PackageAppDirectiveCompletionProvider);
}

public sealed class ProjectAppDirectiveCompletionProviderTests : AbstractAppDirectiveCompletionProviderTests
{
    protected override string DirectiveKind => "project";

    internal override Type GetCompletionProviderType()
        => typeof(ProjectAppDirectiveCompletionProvider);
}

public abstract class AbstractAppDirectiveCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    /// <summary>The directive kind. For example, `package` in `#:package MyNugetPackage@Version`.</summary>
    /// <remarks>Term defined in feature doc: https://github.com/dotnet/sdk/blob/main/documentation/general/dotnet-run-file.md#directives-for-project-metadata</remarks>
    protected abstract string DirectiveKind { get; }

    protected static string GetMarkup(string code, string features = "FileBasedProgram=true") => $$"""
        <Workspace>
            <Project Language="C#" CommonReferences="true" AssemblyName="Test1" Features="{{features}}">
            <Document><![CDATA[{{code}}]]></Document>
            </Project>
        </Workspace>
        """;

    [Fact]
    public Task AfterHashColon()
        => VerifyItemExistsAsync(GetMarkup("""
            #:$$
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task AfterHashColonQuote()
        => VerifyItemIsAbsentAsync(GetMarkup("""
            #:"$$
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task NotWhenFileBasedProgramIsDisabled()
        => VerifyItemIsAbsentAsync(GetMarkup("""
            #:$$
            """, features: ""), expectedItem: DirectiveKind);

    [Fact]
    public Task AfterHashColonSpace()
        => VerifyItemExistsAsync(GetMarkup("""
            #: $$
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task NotAfterHashColonSpaceColon()
        => VerifyItemIsAbsentAsync(GetMarkup("""
            #: :$$
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task NotAfterHashColonWord()
        => VerifyItemIsAbsentAsync(GetMarkup("""
            #:word$$
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task AfterHashColonBeforeWord()
        => VerifyItemExistsAsync(GetMarkup("""
            #:$$word
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task AfterHashColonBeforeNameEqualsValue()
        => VerifyItemExistsAsync(GetMarkup("""
            #:$$ Name=Value
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task NotAfterHashOnly()
        => VerifyItemIsAbsentAsync(GetMarkup("""
            #$$
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task NotAfterColonOnly()
        => VerifyItemIsAbsentAsync(GetMarkup("""
            :$$
            """), expectedItem: DirectiveKind);

    [Fact]
    public Task NotAfterStatement()
        => VerifyItemIsAbsentAsync(GetMarkup("""
            Console.WriteLine();
            $$
            """), expectedItem: DirectiveKind);
}
