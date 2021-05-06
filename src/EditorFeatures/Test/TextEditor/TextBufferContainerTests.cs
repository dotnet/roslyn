// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TextEditor
{
    [UseExportProvider]
    public class TextBufferContainerTests
    {
        [Fact]
        public void TextBufferContainersHeldWeakly()
        {
            var exportProvider = EditorTestCompositions.Editor.ExportProviderFactory.CreateExportProvider();
            var bufferFactory = exportProvider.GetExportedValue<ITextBufferFactoryService>();

            var buffer = bufferFactory.CreateTextBuffer();
            var textContainer = ObjectReference.CreateFromFactory(() => buffer.AsTextContainer());

            // As long as the buffer is alive and CurrentSnapshot hasn't changed, we'll still have the container alive through the path that
            // a ITextSnapshot holds the SourceText, and the SourceText holds the TextContainer. That's not really a big deal
            // since we're not holding onto any snapshots we weren't before. So move the text buffer snapshot forward to something else
            // which means our old container should now be collectable, since the old SourceText is also collectable.
            buffer.Insert(0, "Hello, World!");

            textContainer.AssertReleased();

            GC.KeepAlive(buffer);
        }
    }
}
