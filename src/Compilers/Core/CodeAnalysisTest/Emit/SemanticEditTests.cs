// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Emit;

public class SemanticEditTests : TestBase
{
    [Fact]
    public void InvalidArgs()
    {
        var c = CSharpCompilation.Create("name", references: new[] { TestReferences.NetFx.Minimal.mincorlib });

        var type = c.GetTypeByMetadataName("System.Object")!.GetPublicSymbol();
        Assert.NotNull(type);

        var method = type!.Constructors.Single();
        Assert.NotNull(method);

        Assert.Throws<ArgumentOutOfRangeException>("kind", () => new SemanticEdit(SemanticEditKind.None, oldSymbol: null, newSymbol: null));

        Assert.Throws<ArgumentNullException>("oldSymbol", () => new SemanticEdit(SemanticEditKind.Update, oldSymbol: null, newSymbol: type));
        Assert.Throws<ArgumentNullException>("oldSymbol", () => new SemanticEdit(SemanticEditKind.Delete, oldSymbol: null, newSymbol: type));

        Assert.Throws<ArgumentNullException>("newSymbol", () => new SemanticEdit(SemanticEditKind.Update, oldSymbol: type, newSymbol: null));
        Assert.Throws<ArgumentNullException>("newSymbol", () => new SemanticEdit(SemanticEditKind.Insert, oldSymbol: type, newSymbol: null));
        Assert.Throws<ArgumentNullException>("newSymbol", () => new SemanticEdit(SemanticEditKind.Replace, oldSymbol: type, newSymbol: null));

        var instrumentation = new MethodInstrumentation() { Kinds = ImmutableArray.Create(InstrumentationKind.TestCoverage) };
        Assert.Throws<ArgumentOutOfRangeException>("kind", () => new SemanticEdit(SemanticEditKind.Replace, oldSymbol: method, newSymbol: method, instrumentation: instrumentation));
        Assert.Throws<ArgumentOutOfRangeException>("kind", () => new SemanticEdit(SemanticEditKind.Insert, oldSymbol: method, newSymbol: method, instrumentation: instrumentation));
        Assert.Throws<ArgumentOutOfRangeException>("kind", () => new SemanticEdit(SemanticEditKind.Delete, oldSymbol: method, newSymbol: method, instrumentation: instrumentation));

        Assert.Throws<ArgumentException>("oldSymbol", () => new SemanticEdit(SemanticEditKind.Update, oldSymbol: type, newSymbol: method, instrumentation: instrumentation));
        Assert.Throws<ArgumentException>("newSymbol", () => new SemanticEdit(SemanticEditKind.Update, oldSymbol: method, newSymbol: type, instrumentation: instrumentation));

        Assert.Throws<ArgumentOutOfRangeException>("Kinds", () => new SemanticEdit(SemanticEditKind.Update, oldSymbol: method, newSymbol: method,
            instrumentation: new MethodInstrumentation() { Kinds = ImmutableArray.Create(InstrumentationKindExtensions.LocalStateTracing) }));

        Assert.Throws<ArgumentOutOfRangeException>("Kinds", () => new SemanticEdit(SemanticEditKind.Update, oldSymbol: method, newSymbol: method,
            instrumentation: new MethodInstrumentation() { Kinds = ImmutableArray.Create((InstrumentationKind)123) }));
    }
}
