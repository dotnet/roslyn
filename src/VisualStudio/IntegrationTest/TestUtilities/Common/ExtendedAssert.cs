// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    public static class ExtendedAssert
    {
        public static void Contains(string stringToMatch, string container)
        {
            Assert.IsTrue(container.Contains(stringToMatch));
        }

        public static void DoesNotContain(string stringToMatch, string container)
        {
            Assert.IsFalse(container.Contains(stringToMatch));
        }

        public static void EndsWith(string stringToMatch, string container)
        {
            Assert.IsTrue(container.EndsWith(stringToMatch));
        }

        public static void StartsWith(string stringToMatch, string container)
        {
            Assert.IsTrue(container.StartsWith(stringToMatch));
        }

        public static void Contains(string stringToMatch, string[] containers)
        {
            Assert.IsTrue(containers.Contains(stringToMatch));
        }

        public static void DoesNotContain(string stringToMatch, string[] containers)
        {
            Assert.IsFalse(containers.Contains(stringToMatch));
        }

        public static void Empty<T>(IEnumerable<T> collection)
        {
            Assert.IsFalse(collection.Any());
        }

        public static void Collection<T>(T[] actual, Action<T>[] actions)
        {
            Assert.AreEqual(actions.Length, actual.Length, $"Collections counts: expected - {actions.Length}, actual - {actual.Length}");
            for(int i = 0; i<actions.Length; i++)
            {
                actions[i](actual[i]);
            }
        }
    }
}
