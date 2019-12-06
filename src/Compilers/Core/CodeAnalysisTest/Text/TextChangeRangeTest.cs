// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class TextChangeRangeTest
    {
        [Fact]
        public void Ctor1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => { var notUsed = new TextChangeRange(new TextSpan(), -1); });
        }

        [Fact]
        public void Ctor2()
        {
            var span = new TextSpan(2, 50);
            var range = new TextChangeRange(span, 42);
            Assert.Equal(span, range.Span);
            Assert.Equal(42, range.NewLength);
        }

        [Fact]
        public void Equality()
        {
            EqualityUtil.RunAll(
                (left, right) => left == right,
                (left, right) => left != right,
                EqualityUnit.Create(new TextChangeRange()).WithEqualValues(new TextChangeRange()),
                EqualityUnit.Create(new TextChangeRange(new TextSpan(42, 2), 13)).WithEqualValues(new TextChangeRange(new TextSpan(42, 2), 13)),
                EqualityUnit.Create(new TextChangeRange(new TextSpan(42, 2), 13)).WithNotEqualValues(new TextChangeRange(new TextSpan(42, 2), 5)),
                EqualityUnit.Create(new TextChangeRange(new TextSpan(42, 2), 13)).WithNotEqualValues(new TextChangeRange(new TextSpan(42, 4), 13)));
        }
    }
}
