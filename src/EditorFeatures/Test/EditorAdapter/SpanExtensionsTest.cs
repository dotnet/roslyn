// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorAdapter
{
    public class SpanExtensionsTest
    {
        [WpfFact]
        public void ConvertToTextSpan()
        {
            Action<int, int> del = (start, length) =>
                {
                    var span = new Span(start, length);
                    var textSpan = span.ToTextSpan();
                    Assert.Equal(start, textSpan.Start);
                    Assert.Equal(length, textSpan.Length);
                };
            del(0, 5);
            del(10, 15);
        }
    }
}
