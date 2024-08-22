// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class SolutionTestHelpers
    {
        public static Workspace CreateWorkspace(Type[]? additionalParts = null, TestHost testHost = TestHost.InProcess)
            => new AdhocWorkspace(FeaturesTestCompositions.Features.AddParts(additionalParts).WithTestHostParts(testHost).GetHostServices());

        public static Workspace CreateWorkspaceWithPartialSemantics(TestHost testHost = TestHost.InProcess)
            => WorkspaceTestUtilities.CreateWorkspaceWithPartialSemantics(testHost: testHost);

        private static RemoteWorkspace CreateRemoteWorkspace()
            => new(FeaturesTestCompositions.RemoteHost.GetHostServices());

#nullable disable

        public static void TestProperty<T, TValue>(T instance, Func<T, TValue, T> factory, Func<T, TValue> getter, TValue validNonDefaultValue, bool defaultThrows = false)
            where T : class
        {
            Assert.NotEqual(getter(instance), validNonDefaultValue);

            var instanceWithValue = factory(instance, validNonDefaultValue);
            Assert.Equal(validNonDefaultValue, getter(instanceWithValue));

            // the factory returns the unchanged instance if the value is unchanged:
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

        public static void TestListProperty<T, TValue>(T instance, Func<T, IEnumerable<TValue>, T> factory, Func<T, IEnumerable<TValue>> getter, TValue item, bool allowDuplicates)
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
            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, []));
            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, []));

            // the factory makes an immutable copy if given a mutable list:
            var mutableItems = new[] { item };
            var instanceWithMutableItems = factory(instanceWithNoItem, mutableItems);
            var items = getter(instanceWithMutableItems);
            Assert.NotSame(mutableItems, items);

            // null item:
            Assert.Throws<ArgumentNullException>(() => factory(instanceWithNoItem, new TValue[] { item, default }));

            // duplicate item:
            if (allowDuplicates)
            {
                var boxedDupItems = (IEnumerable<TValue>)ImmutableArray.Create(item, item);
                Assert.Same(boxedDupItems, getter(factory(instanceWithNoItem, boxedDupItems)));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => factory(instanceWithNoItem, new TValue[] { item, item }));
            }
        }
    }
}
