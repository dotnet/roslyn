﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class SolutionTestHelpers
    {
        public static void TestProperty<T, TValue>(T instance, Func<T, TValue, T> factory, Func<T, TValue> getter, TValue validNonDefaultValue, bool defaultThrows = false)
            where T : class
        {
            Assert.NotEqual<TValue>(default, validNonDefaultValue);


            var instanceWithValue = factory(instance, validNonDefaultValue);
            Assert.Equal(validNonDefaultValue, getter(instanceWithValue));

            var instanceWithValue2 = factory(instanceWithValue, validNonDefaultValue);
            Assert.Same(instanceWithValue2, instanceWithValue);

            if (defaultThrows)
            {
                Assert.Throws<ArgumentNullException>(() => factory(instance, default));
            }
            else
            {
                Assert.NotNull(factory(instance, default));
            }
        }

        public static void TestListProperty<T, TValue>(T instance, Func<T, IEnumerable<TValue>, T> factory, Func<T, IEnumerable<TValue>> getter, TValue item)
            where T : class
        {
            var boxedItems = (IEnumerable<TValue>)ImmutableArray.Create(item);
            TestProperty(instance, factory, getter, boxedItems, defaultThrows: false);

            var instanceWithNoItem = factory(instance, null);
            Assert.Empty(getter(instanceWithNoItem));

            var instanceWithItem = factory(instanceWithNoItem, boxedItems);

            // the factory preserves the identity of a boxed immutable array:
            Assert.Same(boxedItems, getter(instanceWithItem));

            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, null));
            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, Array.Empty<TValue>()));
            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, ImmutableArray<TValue>.Empty));

            // the factory makes an immutable copy if given a mutable list:
            var mutableItems = new[] { item };
            var instanceWithMutableItems = factory(instanceWithNoItem, mutableItems);
            var items = getter(instanceWithMutableItems);
            Assert.NotSame(mutableItems, items);

            Assert.Throws<ArgumentNullException>(() => factory(instanceWithNoItem, new TValue[] { item, default }));
        }
    }
}
