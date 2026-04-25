// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorOptimizationPhaseTest
{
    [Fact]
    public void OnInitialized_OrdersPassesInAscendingOrder()
    {
        // Arrange & Act
        var phase = new DefaultRazorOptimizationPhase();

        var first = Mock.Of<IRazorOptimizationPass>(p => p.Order == 15);
        var second = Mock.Of<IRazorOptimizationPass>(p => p.Order == 17);

        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);

            b.Features.Add(second);
            b.Features.Add(first);
        });

        // Assert
        Assert.Collection(
            phase.Passes,
            p => Assert.Same(first, p),
            p => Assert.Same(second, p));
    }

    [Fact]
    public void Execute_ThrowsForMissingDependency()
    {
        // Arrange
        var phase = new DefaultRazorOptimizationPhase();

        var engine = RazorProjectEngine.CreateEmpty(b => b.Phases.Add(phase));

        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => phase.Execute(codeDocument));
        Assert.Equal(
            $"The '{nameof(DefaultRazorOptimizationPhase)}' phase requires a '{nameof(DocumentIntermediateNode)}' " +
            $"provided by the '{nameof(RazorCodeDocument)}'.",
            exception.Message);
    }

    [Fact]
    public void Execute_ExecutesPhasesInOrder()
    {
        // Arrange
        var codeDocument = TestRazorCodeDocument.CreateEmpty();

        // We're going to set up mocks to simulate a sequence of passes. We don't care about
        // what's in the nodes, we're just going to look at the identity via strict mocks.
        var originalNode = new DocumentIntermediateNode();
        var firstPassNode = new DocumentIntermediateNode();
        var secondPassNode = new DocumentIntermediateNode();
        codeDocument = codeDocument.WithDocumentNode(originalNode);

        var firstPass = new Mock<IRazorOptimizationPass>(MockBehavior.Strict);
        firstPass.SetupGet(m => m.Order).Returns(0);

        RazorEngine? firstPassEngine = null;
        firstPass
            .SetupGet(m => m.Engine)
            .Returns(() => firstPassEngine!);
        firstPass
            .Setup(m => m.Initialize(It.IsAny<RazorEngine>()))
            .Callback((RazorEngine engine) => firstPassEngine = engine);

        firstPass.Setup(m => m.Execute(codeDocument, originalNode, CancellationToken.None)).Callback(() =>
        {
            originalNode.Children.Add(firstPassNode);
        });

        var secondPass = new Mock<IRazorOptimizationPass>(MockBehavior.Strict);
        secondPass.SetupGet(m => m.Order).Returns(1);

        RazorEngine? secondPassEngine = null;
        secondPass
            .SetupGet(m => m.Engine)
            .Returns(() => secondPassEngine!);
        secondPass
            .Setup(m => m.Initialize(It.IsAny<RazorEngine>()))
            .Callback((RazorEngine engine) => secondPassEngine = engine);

        secondPass.Setup(m => m.Execute(codeDocument, originalNode, CancellationToken.None)).Callback(() =>
        {
            // Works only when the first pass has run before this.
            originalNode.Children[0].Children.Add(secondPassNode);
        });

        var phase = new DefaultRazorOptimizationPhase();

        var engine = RazorProjectEngine.CreateEmpty(b =>
        {
            b.Phases.Add(phase);

            b.Features.Add(firstPass.Object);
            b.Features.Add(secondPass.Object);
        });

        // Act
        phase.Execute(codeDocument);

        // Assert
        Assert.Same(secondPassNode, codeDocument.GetRequiredDocumentNode().Children[0].Children[0]);
    }
}
