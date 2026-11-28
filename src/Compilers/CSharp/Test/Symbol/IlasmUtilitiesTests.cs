// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Sdk;
using ILAssembler = Microsoft.DotNet.ILAssembler;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class IlasmUtilitiesTests
{
    private const string Source = """
        .assembly Test {}
        .assembly extern D {}
        .class extern forwarder C { .assembly extern D }
        .class extern forwarder C { .assembly extern D }
        """;

    private const string Markup = """
        .assembly Test {}
        .assembly extern D {}
        .class extern forwarder C { .assembly extern D }
        {|ILA0065:.class|} extern forwarder C { .assembly extern D }
        """;

    [Fact]
    public void ExpectedDiagnostic()
    {
        var source = new ILAssembler.SourceText(Source, "test.il");
        var start = Source.LastIndexOf(".class", StringComparison.Ordinal);
        var diagnostic = new ILAssembler.Diagnostic("ILA0065", ILAssembler.DiagnosticSeverity.Error, "Duplicate exported type 'C'",
            new ILAssembler.Location(new ILAssembler.SourceSpan(start, ".class".Length), source));

        var (image, _) = IlasmUtilities.Assemble(Source, ImmutableArray.Create(diagnostic), appendDefaultHeader: false, includePdb: false, autoInherit: true);
        Assert.False(image.IsDefaultOrEmpty);

        var incorrect = diagnostic with { Message = "unexpected" };
        Assert.ThrowsAny<XunitException>(() => IlasmUtilities.Assemble(Source, ImmutableArray.Create(incorrect), appendDefaultHeader: false, includePdb: false, autoInherit: true));

        incorrect = diagnostic with { Severity = ILAssembler.DiagnosticSeverity.Warning };
        Assert.ThrowsAny<XunitException>(() => IlasmUtilities.Assemble(Source, ImmutableArray.Create(incorrect), appendDefaultHeader: false, includePdb: false, autoInherit: true));
    }

    [Fact]
    public void RepeatedWarningMarkup()
    {
        var markup = """
            .assembly Test {}
            {|ILA0031:.class|} extern forwarder C {}
            {|ILA0031:.class|} extern forwarder C {}
            """;

        var (image, _) = IlasmUtilities.Assemble(markup, appendDefaultHeader: false, includePdb: false, autoInherit: true);
        Assert.False(image.IsDefaultOrEmpty);
    }

    [Fact]
    public void MarkupDiagnostics()
    {
        var (image, _) = IlasmUtilities.Assemble(Markup, appendDefaultHeader: false, includePdb: false, autoInherit: true);
        Assert.False(image.IsDefaultOrEmpty);

        Assert.ThrowsAny<XunitException>(() => IlasmUtilities.Assemble(Source, appendDefaultHeader: false, includePdb: false, autoInherit: true));
        Assert.ThrowsAny<XunitException>(() => IlasmUtilities.Assemble(Markup.Replace("ILA0065", "ILA0064"), appendDefaultHeader: false, includePdb: false, autoInherit: true));
        Assert.ThrowsAny<XunitException>(() => IlasmUtilities.Assemble(Markup.Replace("{|ILA0065:.class|}", ".{|ILA0065:class|}"), appendDefaultHeader: false, includePdb: false, autoInherit: true));
    }

    [Fact]
    public void MarkupDiagnosticsWithHeader()
    {
        var markup = """
            .assembly extern D {}
            .class extern forwarder C { .assembly extern D }
            {|ILA0065:.class|} extern forwarder C { .assembly extern D }
            """;

        var (image, _) = IlasmUtilities.Assemble(markup, appendDefaultHeader: true, includePdb: false, autoInherit: true);
        Assert.False(image.IsDefaultOrEmpty);
    }

    [Fact]
    public void ExpectedDiagnosticWithHeaderAndGeneratedName()
    {
        var declarations = Source.Substring(Source.IndexOf(".assembly extern", StringComparison.Ordinal));
        var diagnostic = new ILAssembler.Diagnostic("ILA0065", ILAssembler.DiagnosticSeverity.Error, "Duplicate exported type 'C'",
            new ILAssembler.Location(new ILAssembler.SourceSpan(declarations.LastIndexOf(".class", StringComparison.Ordinal), ".class".Length),
                new ILAssembler.SourceText(declarations, "test.il")));
        var (image, _) = IlasmUtilities.Assemble(declarations, ImmutableArray.Create(diagnostic), appendDefaultHeader: true, includePdb: false, autoInherit: true);
        Assert.False(image.IsDefaultOrEmpty);

        declarations = Source.Replace("Test", "'<<GeneratedFileName>>'");
        diagnostic = diagnostic with
        {
            Location = new ILAssembler.Location(new ILAssembler.SourceSpan(declarations.LastIndexOf(".class", StringComparison.Ordinal), ".class".Length),
                new ILAssembler.SourceText(declarations, "test.il"))
        };
        (image, _) = IlasmUtilities.Assemble(declarations, ImmutableArray.Create(diagnostic), appendDefaultHeader: false, includePdb: false, autoInherit: true);
        Assert.False(image.IsDefaultOrEmpty);
    }
}
