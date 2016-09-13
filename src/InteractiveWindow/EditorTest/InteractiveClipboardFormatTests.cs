// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class InteractiveClipboardFormatTests
    {
        [Fact]
        public void Deserialize_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => InteractiveClipboardFormat.Deserialize(null));
            Assert.Throws<InvalidDataException>(() => InteractiveClipboardFormat.Deserialize(1));
            Assert.Throws<InvalidDataException>(() => InteractiveClipboardFormat.Deserialize("foo"));

            Assert.Throws<InvalidDataException>(() => InteractiveClipboardFormat.Deserialize(@"
[   
    {""content"":""A"",""kind"":1},
    {""content"":""B"",""kind"":1000000000000000000000},
    {""content"":""C"",""kind"":1},
]"));

            Assert.Throws<InvalidDataException>(() => InteractiveClipboardFormat.Deserialize(@"
[   
    {""content"":""A"",""kind"":1},
    {""content"":""B"",""kind"":""x""},
    {""content"":""C"",""kind"":1},
]"));
        }

        [Fact]
        public void Deserialize()
        {
            var serialized = BufferBlock.Serialize(new[]
            {
                new BufferBlock(ReplSpanKind.Input, "I"),
                new BufferBlock(ReplSpanKind.Output, "O"),
                new BufferBlock(ReplSpanKind.LineBreak, "LB"),
                new BufferBlock(ReplSpanKind.Prompt, "P"),
                new BufferBlock(ReplSpanKind.StandardInput, "SI"),
            });
                        
            Assert.Equal("IOLBSI", InteractiveClipboardFormat.Deserialize(serialized));

            // missing kind interpreted as Prompt, which is ignored:
            Assert.Equal("AC", InteractiveClipboardFormat.Deserialize(@"
[   
    {""content"":""A"",""kind"":1},
    {""content"":""B"",""x"":1},
    {""content"":""C"",""kind"":1},
]"));

            // invalid kind ignored:
            Assert.Equal("AC", InteractiveClipboardFormat.Deserialize(@"
[   
    {""content"":""A"",""kind"":1},
    {""content"":""B"",""kind"":-1},
    {""content"":""C"",""kind"":1},
]"));

            // invalid kind ignored:
            Assert.Equal("AC", InteractiveClipboardFormat.Deserialize(@"
[   
    {""content"":""A"",""kind"":1},
    {""content"":""B"",""kind"":-1},
    {""content"":""C"",""kind"":1},
]"));
        }
    }
}
