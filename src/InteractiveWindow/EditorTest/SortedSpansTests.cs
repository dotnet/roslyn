// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class SortedSpansTests
    {
        [Fact]
        public void CheckOverlap()
        {
            var spans = new SortedSpans();

            // no overlap with empty span list 
            Assert.Empty(spans.GetOverlap(new Span(0, 10)));

            // add span [10, 20)
            spans.Add(new Span(10, 10));
            // no overlap with [0, 5)
            Assert.Empty(spans.GetOverlap(new Span(0, 5))); 
            // no overlap with [25, 30)
            Assert.Empty(spans.GetOverlap(new Span(25, 5)));
            // no overlap with [0, 10)
            Assert.Empty(spans.GetOverlap(new Span(0, 10)));
            // no overlap with [20, 30)
            Assert.Empty(spans.GetOverlap(new Span(20, 10)));
            // overlap with [5, 15)
            Assert.Equal(new Span[] { new Span(10, 5) },
                         spans.GetOverlap(new Span(5, 10)));
            // overlap with [0, 11)
            Assert.Equal(new Span[] { new Span(10, 1) }, 
                         spans.GetOverlap(new Span(0, 11)));
            // overlap with [15, 25)
            Assert.Equal(new Span[] { new Span(15, 5) },
                         spans.GetOverlap(new Span(15, 10)));
            // overlap with [11, 15]
            Assert.Equal(new Span[] { new Span(11, 5) },
                         spans.GetOverlap(new Span(11, 5)));
            // overlap with [10, 20)
            Assert.Equal(new Span[] { new Span(10, 10) },
                         spans.GetOverlap(new Span(10, 10)));
            // overlap with [0, 30)
            Assert.Equal(new Span[] { new Span(10, 10) },
                         spans.GetOverlap(new Span(0, 30))); 

            // no overlap with [0, 0]
            Assert.Empty(spans.GetOverlap(new Span(0, 0)));
            // no overlap with [10, 10]
            Assert.Empty(spans.GetOverlap(new Span(10, 0)));
            // no overlap with [15, 15]
            Assert.Empty(spans.GetOverlap(new Span(15, 0)));

            // now has both [10, 20) and [30, 40)
            spans.Add(new Span(30, 10));

            // no overlap with [20, 30)
            Assert.Empty(spans.GetOverlap(new Span(20, 10)));
            // no overlap with [0, 10)
            Assert.Empty(spans.GetOverlap(new Span(0, 10)));
            // no overlap with [40, 50)
            Assert.Empty(spans.GetOverlap(new Span(40, 10)));

            // overlap with [0, 15)
            Assert.Equal(new Span[] { new Span(10, 5) },
                         spans.GetOverlap(new Span(0, 15)));
            // overlap with [20, 35)   
            Assert.Equal(new Span[] { new Span(30, 5) },
                         spans.GetOverlap(new Span(20, 15)));

            // overlap with [0, 35) 
            Assert.Equal(new Span[] { new Span(10, 10), new Span(30, 5) },
                         spans.GetOverlap(new Span(0, 35)));
            // overlap with [15, 35)  
            Assert.Equal(new Span[] { new Span(15, 5), new Span(30, 5) },
                         spans.GetOverlap(new Span(15, 20)));
            // overlap with [15, 50)
            Assert.Equal(new Span[] { new Span(15, 5), new Span(30, 10) },
                         spans.GetOverlap(new Span(15, 35)));
            // overlap with [0, 25) 
            Assert.Equal(new Span[] { new Span(10, 10) },
                         spans.GetOverlap(new Span(0, 25)));
            // overlap with [25, 45) 
            Assert.Equal(new Span[] { new Span(30, 10) },
                         spans.GetOverlap(new Span(25, 20)));
            // overlap with [0, 50)
            Assert.Equal(new Span[] { new Span(10, 10), new Span(30, 10) },
                         spans.GetOverlap(new Span(0, 50)));
        }
    }
}
