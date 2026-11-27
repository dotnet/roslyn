// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorEngineTest
{
    [Fact]
    public void Ctor_InitializesPhasesAndFeatures()
    {
        // Arrange
        ImmutableArray<IRazorEngineFeature> features = [
            new TestFeature(),
            new TestFeature()];

        ImmutableArray<IRazorEnginePhase> phases = [
            new TestPhase(),
            new TestPhase()];

        // Act
        var engine = new RazorEngine(features, phases);

        // Assert
        foreach (var feature in features)
        {
            Assert.Same(engine, feature.Engine);
        }

        foreach (var phase in phases)
        {
            Assert.Same(engine, phase.Engine);
        }
    }

    [Fact]
    public void Process_CallsAllPhases()
    {
        // Arrange
        ImmutableArray<IRazorEngineFeature> features = [
            new TestFeature(),
            new TestFeature()];

        ImmutableArray<IRazorEnginePhase> phases = [
            new TestPhase(),
            new TestPhase()];

        var engine = new RazorEngine(features, phases);
        var document = TestRazorCodeDocument.CreateEmpty();

        // Act
        engine.Process(document);

        // Assert
        foreach (var phase in phases)
        {
            var testPhase = Assert.IsType<TestPhase>(phase);
            Assert.Equal(1, testPhase.CallCount);
        }
    }

    private sealed class TestFeature : RazorEngineFeatureBase
    {
    }

    private sealed class TestPhase : RazorEnginePhaseBase
    {
        public int CallCount;

        protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CallCount);
            return codeDocument;
        }
    }
}
