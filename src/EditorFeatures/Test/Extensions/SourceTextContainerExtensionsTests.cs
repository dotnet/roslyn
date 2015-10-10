// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class SourceTextContainerExtensionsTests
    {
        [WpfFact]
        public void GetBufferTextFromNonTextContainerThrows()
        {
            var containerMock = new Mock<SourceTextContainer>();
            Assert.Throws<ArgumentException>(() => Microsoft.CodeAnalysis.Text.Extensions.GetTextBuffer(containerMock.Object));
        }

        [WpfFact]
        public void GetBufferTextFromTextContainerDoesNotThrow()
        {
            var textSnapshotMock = new Mock<VisualStudio.Text.ITextSnapshot>();
            var bufferMock = new Mock<VisualStudio.Text.ITextBuffer>();
            bufferMock.SetupGet(x => x.CurrentSnapshot).Returns(textSnapshotMock.Object);
            bufferMock.SetupGet(x => x.Properties).Returns(new VisualStudio.Utilities.PropertyCollection());

            var textContainer = Microsoft.CodeAnalysis.Text.Extensions.TextBufferContainer.From(bufferMock.Object);

            Microsoft.CodeAnalysis.Text.Extensions.GetTextBuffer(textContainer);
        }
    }
}
