// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class WeakListTests : TestBase
    {
        private class C
        {
            private readonly string _value;

            public C(string value)
            {
                _value = value;
            }

            public override string ToString()
            {
                return _value;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private ObjectReference Create(string value)
        {
            return new ObjectReference(new C(value));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void Add(WeakList<object> list, ObjectReference value)
        {
            list.Add(value.Strong);
        }

        [Fact]
        public void EnumeratorCompacts()
        {
            var a = Create("a");
            var b = Create("B");
            var c = Create("C");
            var d = Create("D");
            var e = Create("E");

            var list = new WeakList<object>();
            Assert.Equal(0, list.TestOnly_UnderlyingArray.Length);

            Add(list, a);
            Assert.Equal(4, list.TestOnly_UnderlyingArray.Length);

            Add(list, b);
            Assert.Equal(4, list.TestOnly_UnderlyingArray.Length);

            Add(list, c);
            Assert.Equal(4, list.TestOnly_UnderlyingArray.Length);

            Add(list, d);
            Assert.Equal(4, list.TestOnly_UnderlyingArray.Length);

            Add(list, e);
            Assert.Equal(2 * 4 + 1, list.TestOnly_UnderlyingArray.Length);

            Assert.Equal(5, list.WeakCount);

            a.Strong = null;
            c.Strong = null;
            d.Strong = null;
            e.Strong = null;

            while (a.Weak.IsAlive || c.Weak.IsAlive || d.Weak.IsAlive || e.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            Assert.Equal(5, list.WeakCount);
            Assert.Same(null, list.GetWeakReference(0).GetTarget());
            Assert.Same(b.Strong, list.GetWeakReference(1).GetTarget());
            Assert.Same(null, list.GetWeakReference(2).GetTarget());
            Assert.Same(null, list.GetWeakReference(3).GetTarget());
            Assert.Same(null, list.GetWeakReference(4).GetTarget());

            var array = list.ToArray();

            Assert.Equal(1, array.Length);
            Assert.Same(b.Strong, array[0]);

            // list was compacted:
            Assert.Equal(1, list.WeakCount);
            Assert.Same(b.Strong, list.GetWeakReference(0).GetTarget());
            Assert.Equal(4, list.TestOnly_UnderlyingArray.Length);

            GC.KeepAlive(b.Strong);
        }

        [Fact]
        public void ResizeCompactsAllDead()
        {
            var a = Create("A");

            var list = new WeakList<object>();
            for (int i = 0; i < 9; i++)
            {
                Add(list, a);
            }

            Assert.Equal(list.WeakCount, list.TestOnly_UnderlyingArray.Length); // full

            a.Strong = null;
            while (a.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            Add(list, a); // shrinks, #alive < length/4
            Assert.Equal(4, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(1, list.WeakCount);

            list.ToArray(); // shrinks, #alive == 0
            Assert.Equal(0, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(0, list.WeakCount);
        }

        [Fact]
        public void ResizeCompactsFirstFourth()
        {
            var a = Create("A");
            var b = Create("B");

            var list = new WeakList<object>();
            for (int i = 0; i < 8; i++)
            {
                Add(list, a);
            }

            Add(list, b);
            Assert.Equal(list.WeakCount, list.TestOnly_UnderlyingArray.Length); // full

            a.Strong = null;
            while (a.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            Add(list, b); // shrinks, #alive < length/4
            Assert.Equal(4, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(2, list.WeakCount);

            b.Strong = null;
            while (b.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            list.ToArray(); // shrinks, #alive == 0
            Assert.Equal(0, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(0, list.WeakCount);
        }

        [Fact]
        public void ResizeCompactsSecondFourth()
        {
            var a = Create("A");
            var b = Create("B");

            var list = new WeakList<object>();
            for (int i = 0; i < 6; i++)
            {
                Add(list, a);
            }

            for (int i = 0; i < 3; i++)
            {
                Add(list, b);
            }

            Assert.Equal(list.WeakCount, list.TestOnly_UnderlyingArray.Length); // full

            a.Strong = null;
            while (a.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            Add(list, b); // just compacts, length/4 < #alive < 3/4 length
            Assert.Equal(9, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(4, list.WeakCount);

            for (int i = 0; i < list.TestOnly_UnderlyingArray.Length; i++)
            {
                if (i < 4)
                {
                    Assert.Same(b.Strong, list.TestOnly_UnderlyingArray[i].GetTarget());
                }
                else
                {
                    Assert.Null(list.TestOnly_UnderlyingArray[i]);
                }
            }

            GC.KeepAlive(b.Strong);
        }

        [Fact]
        public void ResizeCompactsThirdFourth()
        {
            var a = Create("A");
            var b = Create("B");

            var list = new WeakList<object>();
            for (int i = 0; i < 4; i++)
            {
                Add(list, a);
            }

            for (int i = 0; i < 5; i++)
            {
                Add(list, b);
            }

            Assert.Equal(list.WeakCount, list.TestOnly_UnderlyingArray.Length); // full

            a.Strong = null;
            while (a.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            Add(list, b); // compacts #alive < 3/4 length
            Assert.Equal(9, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(6, list.WeakCount);

            for (int i = 0; i < list.TestOnly_UnderlyingArray.Length; i++)
            {
                if (i < 6)
                {
                    Assert.Same(b.Strong, list.TestOnly_UnderlyingArray[i].GetTarget());
                }
                else
                {
                    Assert.Null(list.TestOnly_UnderlyingArray[i]);
                }
            }

            GC.KeepAlive(b.Strong);
        }

        [Fact]
        public void ResizeCompactsLastFourth()
        {
            var a = Create("A");
            var b = Create("B");

            var list = new WeakList<object>();
            for (int i = 0; i < 2; i++)
            {
                Add(list, a);
            }

            for (int i = 0; i < 7; i++)
            {
                Add(list, b);
            }

            Assert.Equal(list.WeakCount, list.TestOnly_UnderlyingArray.Length); // full

            a.Strong = null;
            while (a.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            Add(list, b); // expands #alive > 3/4 length
            Assert.Equal(9 * 2 + 1, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(8, list.WeakCount);

            for (int i = 0; i < list.TestOnly_UnderlyingArray.Length; i++)
            {
                if (i < 8)
                {
                    Assert.Same(b.Strong, list.TestOnly_UnderlyingArray[i].GetTarget());
                }
                else
                {
                    Assert.Null(list.TestOnly_UnderlyingArray[i]);
                }
            }

            GC.KeepAlive(b.Strong);
        }

        [Fact]
        public void ResizeCompactsAllAlive()
        {
            var b = Create("B");

            var list = new WeakList<object>();
            for (int i = 0; i < 9; i++)
            {
                Add(list, b);
            }

            Assert.Equal(list.WeakCount, list.TestOnly_UnderlyingArray.Length); // full

            Add(list, b); // expands #alive > 3/4 length
            Assert.Equal(9 * 2 + 1, list.TestOnly_UnderlyingArray.Length);
            Assert.Equal(10, list.WeakCount);

            for (int i = 0; i < list.TestOnly_UnderlyingArray.Length; i++)
            {
                if (i < 10)
                {
                    Assert.Same(b.Strong, list.TestOnly_UnderlyingArray[i].GetTarget());
                }
                else
                {
                    Assert.Null(list.TestOnly_UnderlyingArray[i]);
                }
            }

            GC.KeepAlive(b.Strong);
        }

        [Fact]
        public void Errors()
        {
            var list = new WeakList<object>();
            Assert.Throws<ArgumentOutOfRangeException>(() => list.GetWeakReference(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.GetWeakReference(0));
        }
    }
}
