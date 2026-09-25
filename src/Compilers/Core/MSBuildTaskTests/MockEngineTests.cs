// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class MockEngineTests
    {
        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("message", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("message", false)]
        public void LogCustomEvent(string? message, bool hasOutputHelper)
        {
            var outputHelper = new Mock<ITestOutputHelper>(MockBehavior.Strict);
            var expected = message ?? string.Empty;
            outputHelper.Setup(helper => helper.WriteLine(expected));
            var engine = new MockEngine(hasOutputHelper ? outputHelper.Object : null);

            engine.LogCustomEvent(new TestCustomBuildEventArgs(message));

            Assert.Equal(expected + Environment.NewLine, engine.Log);
            outputHelper.Verify(helper => helper.WriteLine(expected), hasOutputHelper ? Times.Once() : Times.Never());
            outputHelper.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("message", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("message", false)]
        public void LogMessageEvent(string? message, bool hasOutputHelper)
        {
            var outputHelper = new Mock<ITestOutputHelper>(MockBehavior.Strict);
            var expected = message ?? string.Empty;
            outputHelper.Setup(helper => helper.WriteLine(expected));
            var engine = new MockEngine(hasOutputHelper ? outputHelper.Object : null);
            var eventArgs = new BuildMessageEventArgs(message, null, nameof(MockEngineTests), MessageImportance.Normal);

            engine.LogMessageEvent(eventArgs);

            Assert.Equal(expected + Environment.NewLine, engine.Log);
            Assert.Same(eventArgs, Assert.Single(engine.BuildMessages));
            outputHelper.Verify(helper => helper.WriteLine(expected), hasOutputHelper ? Times.Once() : Times.Never());
            outputHelper.VerifyNoOtherCalls();
        }

        private sealed class TestCustomBuildEventArgs(string? message)
            : CustomBuildEventArgs(message, null, nameof(MockEngineTests));
    }
}
