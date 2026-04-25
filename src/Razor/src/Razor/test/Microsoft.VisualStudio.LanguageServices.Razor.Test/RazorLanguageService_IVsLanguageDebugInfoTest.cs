// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Razor.Debugging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor;

public class RazorLanguageService_IVsLanguageDebugInfoTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly TextSpan[] _textSpans = [new TextSpan()];

    [Fact]
    public void ValidateBreakpointLocation_CanNotGetBackingTextBuffer_ReturnsNotImpl()
    {
        // Arrange
        var editorAdaptersFactoryService = new Mock<IVsEditorAdaptersFactoryService>(MockBehavior.Strict);
        editorAdaptersFactoryService.Setup(s => s.GetDataBuffer(It.IsAny<IVsTextBuffer>())).Returns(value: null);
        var languageService = CreateLanguageServiceWith(editorAdaptersFactory: editorAdaptersFactoryService.Object);

        // Act
        var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_NOTIMPL, result);
    }

    [Fact]
    public void ValidateBreakpointLocation_InvalidLocation_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), int.MaxValue, 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void ValidateBreakpointLocation_NullBreakpointRange_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void ValidateBreakpointLocation_ValidBreakpointRange_ReturnsSOK()
    {
        // Arrange
        var breakpointRange = LspFactory.CreateRange(2, 4, 3, 5);
        var breakpointResolver = Mock.Of<IRazorBreakpointResolver>(resolver => resolver.TryResolveBreakpointRangeAsync(It.IsAny<ITextBuffer>(), 0, 0, It.IsAny<CancellationToken>()) == System.Threading.Tasks.Task.FromResult(breakpointRange), MockBehavior.Strict);
        var languageService = CreateLanguageServiceWith(breakpointResolver);

        // Act
        var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.S_OK, result);
        var span = Assert.Single(_textSpans);
        Assert.Equal(2, span.iStartLine);
        Assert.Equal(4, span.iStartIndex);
        Assert.Equal(3, span.iEndLine);
        Assert.Equal(5, span.iEndIndex);
    }

    [Fact]
    public void ValidateBreakpointLocation_CanNotCreateDialog_ReturnsEFail()
    {
        // Arrange
        var uiThreadExecutor = new Mock<IUIThreadOperationExecutor>(MockBehavior.Strict);
        uiThreadExecutor.Setup(f => f.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Action<IUIThreadOperationContext>>())).Returns(value: UIThreadOperationStatus.Canceled);
        var languageService = CreateLanguageServiceWith(uiThreadOperationExecutor: uiThreadExecutor.Object);

        // Act
        var result = languageService.ValidateBreakpointLocation(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, _textSpans);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void GetProximityExpressions_CanNotGetBackingTextBuffer_ReturnsNotImpl()
    {
        // Arrange
        var editorAdaptersFactoryService = new Mock<IVsEditorAdaptersFactoryService>(MockBehavior.Strict);
        editorAdaptersFactoryService.Setup(s => s.GetDataBuffer(It.IsAny<IVsTextBuffer>())).Returns(value: null);
        var languageService = CreateLanguageServiceWith(editorAdaptersFactory: editorAdaptersFactoryService.Object);

        // Act
        var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, 0, out _);

        // Assert
        Assert.Equal(VSConstants.E_NOTIMPL, result);
    }

    [Fact]
    public void GetProximityExpressions_InvalidLocation_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), int.MaxValue, 0, 0, out _);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void GetProximityExpressions_NullRange_ReturnsEFail()
    {
        // Arrange
        var languageService = CreateLanguageServiceWith();

        // Act
        var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, 0, out _);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    [Fact]
    public void GetProximityExpressions_ValidRange_ReturnsSOK()
    {
        // Arrange
        IReadOnlyList<string> expressions = new[] { "something" };
        var resolver = Mock.Of<IRazorProximityExpressionResolver>(resolver => resolver.TryResolveProximityExpressionsAsync(It.IsAny<ITextBuffer>(), 0, 0, It.IsAny<CancellationToken>()) == System.Threading.Tasks.Task.FromResult(expressions), MockBehavior.Strict);
        var languageService = CreateLanguageServiceWith(proximityExpressionResolver: resolver);

        // Act
        var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, 0, out var resolvedExpressions);

        // Assert
        Assert.Equal(VSConstants.S_OK, result);
        var concreteResolvedExpressions = Assert.IsType<VsEnumBSTR>(resolvedExpressions);
        Assert.Equal(expressions, concreteResolvedExpressions.Values);
    }

    [Fact]
    public void GetProximityExpressions_CanNotCreateDialog_ReturnsEFail()
    {
        // Arrange
        var uiThreadOperationExecutor = new Mock<IUIThreadOperationExecutor>(MockBehavior.Strict);
        uiThreadOperationExecutor.Setup(f => f.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Action<IUIThreadOperationContext>>())).Returns(UIThreadOperationStatus.Canceled);
        var languageService = CreateLanguageServiceWith(uiThreadOperationExecutor: uiThreadOperationExecutor.Object);

        // Act
        var result = languageService.GetProximityExpressions(Mock.Of<IVsTextBuffer>(MockBehavior.Strict), 0, 0, 0, out _);

        // Assert
        Assert.Equal(VSConstants.E_FAIL, result);
    }

    private RazorLanguageService CreateLanguageServiceWith(
        IRazorBreakpointResolver breakpointResolver = null,
        IRazorProximityExpressionResolver proximityExpressionResolver = null,
        IUIThreadOperationExecutor uiThreadOperationExecutor = null,
        IVsEditorAdaptersFactoryService editorAdaptersFactory = null)
    {
        if (breakpointResolver is null)
        {
            breakpointResolver = new Mock<IRazorBreakpointResolver>(MockBehavior.Strict).Object;
            Mock.Get(breakpointResolver)
                .Setup(r => r.TryResolveBreakpointRangeAsync(
                    It.IsAny<ITextBuffer>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(value: null);
        }

        if (proximityExpressionResolver is null)
        {
            proximityExpressionResolver = new Mock<IRazorProximityExpressionResolver>(MockBehavior.Strict).Object;
            Mock.Get(proximityExpressionResolver)
                .Setup(r => r.TryResolveProximityExpressionsAsync(
                    It.IsAny<ITextBuffer>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(value: null);
        }

        uiThreadOperationExecutor ??= new TestIUIThreadOperationExecutor();
        editorAdaptersFactory ??= Mock.Of<IVsEditorAdaptersFactoryService>(service => service.GetDataBuffer(It.IsAny<IVsTextBuffer>()) == new TestTextBuffer(new StringTextSnapshot(Environment.NewLine), /* contentType */ null), MockBehavior.Strict);

        var lspServerActivationTracker = new LspServerActivationTracker();
        lspServerActivationTracker.Activated();

        var languageService = new RazorLanguageService(breakpointResolver, proximityExpressionResolver, lspServerActivationTracker, uiThreadOperationExecutor, editorAdaptersFactory, JoinableTaskFactory);
        return languageService;
    }

    private class TestIUIThreadOperationExecutor : IUIThreadOperationExecutor
    {
        public IUIThreadOperationContext BeginExecute(string title, string defaultDescription, bool allowCancellation, bool showProgress)
        {
            throw new NotImplementedException();
        }

        public IUIThreadOperationContext BeginExecute(UIThreadOperationExecutionOptions executionOptions)
        {
            throw new NotImplementedException();
        }

        public UIThreadOperationStatus Execute(string title, string defaultDescription, bool allowCancellation, bool showProgress, Action<IUIThreadOperationContext> action)
        {
            using (var context = new TestUIThreadOperationContext())
            {
                action(context);
            }

            return UIThreadOperationStatus.Completed;
        }

        public UIThreadOperationStatus Execute(UIThreadOperationExecutionOptions executionOptions, Action<IUIThreadOperationContext> action)
        {
            throw new NotImplementedException();
        }

        private class TestUIThreadOperationContext : IUIThreadOperationContext
        {
            public TestUIThreadOperationContext()
            {
            }

            public CancellationToken UserCancellationToken => new();

            public bool AllowCancellation => throw new NotImplementedException();

            public string Description => throw new NotImplementedException();

            public IEnumerable<IUIThreadOperationScope> Scopes => throw new NotImplementedException();

            public PropertyCollection Properties => throw new NotImplementedException();

            public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }

            public void TakeOwnership()
            {
                throw new NotImplementedException();
            }
        }
    }
}
