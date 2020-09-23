// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Symbols;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue.UnitTests
{
    public class ModuleUtilitiesTests
    {
        [Theory]
        [InlineData(1, 2, 3, 4, 0, 1, 2, 3)]
        [InlineData(5, 0, 5, 0, 4, 0, 4, 0)]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(0, 2, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, 0, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, 0, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, 2, 0, 0, 0, 0, 0)]
        [InlineData(int.MinValue, 2, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, int.MinValue, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, int.MinValue, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, 2, int.MinValue, 0, 0, 0, 0)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue - 1, int.MaxValue - 1, int.MaxValue - 1, int.MaxValue - 1)]
        public void Span(int startLine, int startColumn, int endLine, int endColumn,
                         int expectedStartLine, int expectedStartColumn, int expectedEndLine, int expectedEndColumn)
        {
            var actual = ModuleUtilities.ToLinePositionSpan(new DkmTextSpan(StartLine: startLine, EndLine: endLine, StartColumn: startColumn, EndColumn: endColumn));
            var expected = new LinePositionSpan(new LinePosition(expectedStartLine, expectedStartColumn), new LinePosition(expectedEndLine, expectedEndColumn));

            Assert.Equal(expected, actual);
        }
    }
}
