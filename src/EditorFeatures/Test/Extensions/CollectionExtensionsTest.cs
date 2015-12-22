// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class CollectionExtensionsTest
    {
        [Fact]
        public void PushReverse1()
        {
            var stack = new Stack<int>();
            stack.PushReverse(new int[] { 1, 2, 3 });
            Assert.Equal(1, stack.Pop());
            Assert.Equal(2, stack.Pop());
            Assert.Equal(3, stack.Pop());
            Assert.Equal(0, stack.Count);
        }

        [Fact]
        public void PushReverse2()
        {
            var stack = new Stack<int>();
            stack.PushReverse(Array.Empty<int>());
            Assert.Equal(0, stack.Count);
        }

        [Fact]
        public void PushReverse3()
        {
            var stack = new Stack<int>();
            stack.Push(3);
            stack.PushReverse(new int[] { 1, 2 });
            Assert.Equal(1, stack.Pop());
            Assert.Equal(2, stack.Pop());
            Assert.Equal(3, stack.Pop());
            Assert.Equal(0, stack.Count);
        }
    }
}
