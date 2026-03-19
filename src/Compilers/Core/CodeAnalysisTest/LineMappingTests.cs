// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class LineMappingTests
    {
        [Fact]
        public void Equality()
        {
            var lineMappings = new LineMapping[]
            {
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: true)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 2)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(2, 2)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 1), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(1, 0), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("file.cs", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), 0, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 2)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 0), new LinePosition(2, 2)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(0, 1), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: false)),
                new LineMapping(new LinePositionSpan(new LinePosition(1, 0), new LinePosition(1, 1)), null, new FileLinePositionSpan("", new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 1)), hasMappedPath: false)),
            };
            var equalityUnits = lineMappings.SelectMany((left, leftIndex) => lineMappings.Select((right, rightIndex) => CreateEqualityUnit(left, leftIndex, right, rightIndex))).ToArray();
            EqualityUtil.RunAll(
                (left, right) => left == right,
                (left, right) => left != right,
                equalityUnits);

            static EqualityUnit<LineMapping> CreateEqualityUnit(LineMapping left, int leftIndex, LineMapping right, int rightIndex)
            {
                var leftUnit = EqualityUnit.Create(left);
                return (leftIndex == rightIndex) ? leftUnit.WithEqualValues(right) : leftUnit.WithNotEqualValues(right);
            }
        }
    }
}
