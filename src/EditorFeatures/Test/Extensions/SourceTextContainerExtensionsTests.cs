// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class SourceTextContainerExtensionsTests
    {
        [Fact]
        public void GetBufferTextFromNonTextContainerThrows()
        {
            var containerMock = new Mock<SourceTextContainer>(MockBehavior.Strict);
            Assert.Throws<ArgumentException>(() => Microsoft.CodeAnalysis.Text.Extensions.GetTextBuffer(containerMock.Object));
        }

        [Fact]
        public void GetBufferTextFromTextContainerDoesNotThrow()
        {
            var textImageMock = new Mock<VisualStudio.Text.ITextImage>(MockBehavior.Strict);
            var textSnapshotMock = new Mock<VisualStudio.Text.ITextSnapshot2>(MockBehavior.Strict);
            var bufferMock = new Mock<VisualStudio.Text.ITextBuffer>(MockBehavior.Strict);

            textSnapshotMock.SetupGet(s => s.TextImage).Returns(textImageMock.Object);
            textSnapshotMock.SetupGet(s => s.TextBuffer).Returns(bufferMock.Object);
            bufferMock.SetupGet(x => x.CurrentSnapshot).Returns(textSnapshotMock.Object);
            bufferMock.SetupGet(x => x.Properties).Returns(new VisualStudio.Utilities.PropertyCollection());

            var textContainer = CodeAnalysis.Text.Extensions.TextBufferContainer.From(bufferMock.Object);

            CodeAnalysis.Text.Extensions.GetTextBuffer(textContainer);
        }
    }
}
