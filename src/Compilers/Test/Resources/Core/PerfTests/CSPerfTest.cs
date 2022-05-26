// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////////////////////////
//GNAMBOO: Changing this code has implications for perf tests
////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        ns1.Test.Run();
        ns1.LowFrequencyTest.Run();
    }
}

namespace ns1
{
    public class Test
    {
        public static void Run()
        {
            c1 a = new c1(); a.test();

            c2<string> b = new c2<string>(); b.TEST();

            c3<string, string>.test();

            c4.Test();

            c4.c5 d = new c4.c5(); d.Test();

            c6<int, long> e = new c6<int, long>(1, new List<Func<long>>()); e.Test();

            c7<int, int> f = new c7<int, int>(1, new List<Func<int>>()); f.Test();

            c9 h = new c9(); h.test();

            s0<int> i = new s0<int>(); i.Test();

            s1.Test();

            default(nested.s2).Test(1, 1);
        }
    }

    // Abstract Class
    public abstract class c0
    {
        // Abstract Methods
        public abstract int abst(ref string x, params int[] y);

        public abstract int abst(ref string x, params long[] y);

        public abstract int abst(ref string x, long y, long z);
    }

    public class c1 : c0 // Inheritance
    {
        private int i = 2;
        internal uint ui = 3;
        public c1 a = null;

        // Overloaded Constructors
        public c1()
        {
            i = 2; this.ui = 3;
        }

        public c1(int x)
        {
            i = x; this.ui = 3; this.a = new c1(this.i, this.ui, this.a);
        }

        public c1(int x, uint y, c1 c)
        {
            this.i = x; ui = y; a = new c1();
        }

        public void test()
        {
            int i = 2;
            bool b = true;

            // Nested Scopes
            if (b)
            {
                object o = i;
                b = false;
                {
                    byte b1 = 1;
                    string s = "c1.test()";
                    {
                        Console.WriteLine(s);
                        this.goo(o); goo(i); this.goo(b); this.goo(b1); // Overload Resolution, Implicit Conversions
                    }
                }
            }
            // Nested Scopes
            if (!b)
            {
                object o = i;
                b = false;
                if (true)
                {
                    byte b1 = 1;
                    string s = "c1.test()";
                    {
                        Console.WriteLine(s);
                        bar2(o); this.bar2(i); this.bar2(b1); bar1(s); // Non-Overloaded Methods, Implicit Conversions
                    }
                }
            }
            // Nested Scopes
            if (!false)
            {
                object o = i;
                b = false;
                {
                    byte b1 = 1;
                    string s = "c1.test()";
                    {
                        Console.WriteLine(s);
                        this.bar4(o); this.bar4(i); this.bar4(b1); this.bar3(b); // Non-Overloaded Methods, Implicit Conversions
                    }
                }
            }

            if (!false)
            {
                object o = i;
                b = false;
                {
                    string s = "c1.test()";
                    {
                        Console.WriteLine(s);

                        // Method Calls - Ref, Paramarrays
                        // Overloaded Abstract Methods
                        c1 c = new c1(); long l = 1;
                        this.abst(ref s, 1, i);
                        c.abst(ref s, new int[] { 1, i, i });
                        c.abst(ref s, this.abst(ref s, l, l), l, l, l);

                        // Method Calls - Ref, Paramarrays
                        // Overloaded Virtual Methods
                        c.virt(i, c, new c2<string>[] { virt(i, ref c), new c4() });
                        virt(this.virt(i, ref c), c.virt(ref i, c, virt(i, ref c)));
                        virt(c.abst(ref s, l, l), this.abst(ref s, new long[] { 1, i, l }));
                        c = new c4();
                        virt(i, ref c);
                        virt(ref i, new c4(), new c4(), new c2<string>());
                        virt(new int[] { 1, 2, 3 });
                        virt(new Exception[] { });
                        virt(new c1[] { new c4(), new c2<string>() });
                    }
                }
            }
        }

        // Overridden Abstract Methods
        public override int abst(ref string x, params int[] y)
        {
            Console.WriteLine("    c1.abst(ref string, params int[])");
            x = x.ToString(); y = new int[] { y[0] }; // Read, Write Ref + Paramarrays
            return 0;
        }

        public override int abst(ref string x, params long[] y)
        {
            Console.WriteLine("    c1.abst(ref string, params long[])");
            x = y[0].ToString(); y = null; // Read, Write Ref + Paramarrays
            return 1;
        }

        public override int abst(ref string x, long y, long z)
        {
            Console.WriteLine("    c1.abst(ref string, long, long)");
            x = z.ToString(); // Read, Write Ref
            return 2;
        }

        // Virtual Methods
        public virtual int virt(ref int x, c1 y, params c2<string>[] z)
        {
            Console.WriteLine("    c1.virt(ref int, c1, params c2<string>[])");
            x = x + x * 2; z = null; // Read, Write Ref + Paramarrays
            return 0;
        }

        public virtual c2<string> virt(int x, ref c1 y)
        {
            Console.WriteLine("    c1.virt(int, ref c1)");
            y = new c1(); // Read, Write Ref
            return new c4();
        }

        public virtual int virt(params object[] x)
        {
            Console.WriteLine("    c1.virt(params object[])");
            x = new object[] { 1, 2, null }; // Read, Write Paramarrays
            return new int();
        }

        public virtual int virt(params int[] x)
        {
            Console.WriteLine("    c1.virt(params int[])");
            x = new int[] { 0, 1 }; // Read, Write Paramarrays
            int i = x[0];
            return new int();
        }

        internal int goo(int x)
        {
            Console.WriteLine("    c1.goo(int)");

            // Read, Write Fields
            this.ui = 0u + this.ui;
            i = i - 1 * 2 + 3 / 1;
            this.i = 1;
            this.a = null; a = new c1(x);

            // Read, Write Locals
            bool b = true; string s = null;
            s = string.Empty;
            b = this.i != 1 + (2 - 3);
            s = "";
            c1 c = new c1(i, ui, new c1(this.i, this.ui, new c1(i)));
            c = this.a;
            b = b == true; s = s.ToString();

            // Read, Write Params
            x = (i - this.i) * i + (x / i);
            x = x.GetHashCode(); this.i = x;

            // Read, Write Array Element
            int[] a1 = new int[] { 1, 2, 3 };
            a1[1] = i; a1[2] = x;
            a1[1] = 1; a1[2] = 2;
            int i1 = a1[1] - a1[2];
            i1 = (a1[1] - (a1[2] + a1[1]));
            object o = i1;
            o = a1[2] + (a1[1] - a1[2]);

            return x;
        }

        public bool goo(object x)
        {
            Console.WriteLine("    c1.goo(object)");

            // Read, Write Fields
            ui = 0u;
            this.i = this.i - 1;
            a = null;
            uint ui1 = ui; int i1 = i;

            // Read, Write Locals
            bool b = true; string s = string.Empty;
            s = null; b = this.i != 1;
            ui = ui1; i = i1;
            bar4(b); this.goo(i1); bar4(b == (true != b));

            // Read, Write Params
            x = null; x = new c1(this.i, this.ui, a);
            this.bar4(x); this.bar4(x.GetHashCode() != (x.GetHashCode()));

            // Read, Write Array Element
            object[] a1 = new c1[3] { null, null, null };
            this.i = 1;
            a1[1] = null; a1[2] = new c1((i * i) / i, ui + (ui - ui), null);
            object o = null;
            o = a1[1]; this.bar4(a1[2]);

            if (b)
            {
                return b.GetHashCode() == this.i;
            }
            else
            {
                return b;
            }
        }

        private void bar1(string x)
        {
            Console.WriteLine("    c1.bar1(string)");

            // Read, Write Fields
            this.ui = 0u - 0u;
            i = this.i * 1;
            this.a = new c1();
            this.goo(i.GetHashCode()); this.a = this;

            // Read, Write Locals
            c1 c = new c1(1, 0u, (null));
            c = null; i = 1;
            c = new c1(i / i);
            c = this.a;
            this.ui = 1;
            c.ui = this.ui / ui;
            c.i = this.i + this.i;
            c.a = c;
            c.a = c = this.a = c.a = null;
            c = new c1(i.GetHashCode());
            this.goo(c.i); bar3(c != null);

            if (this.i == 10321)
            {
                return;
            }
            else
            {
            }

            // Read, Write Params
            x = null; this.bar4(x);

            // Read, Write Array Element
            string[] a1 = new string[] { "", null, null };
            a1[1] = null; a1[2] = "";
            string s = null;
            s = a1[1]; goo(a1[2]);
        }

        protected string bar2(object x)
        {
            Console.WriteLine("    c1.bar2(object)");

            // Read, Write Fields
            this.ui = ui - this.ui;
            i = i / 1;
            a = null;
            goo(i);

            // Read, Write Locals
            c1 c;
            object o;
            c = null; c = new c1(i / 2, ui * (2u), new c1(i / 2, ui * (2u), c));
            this.a = new c1(((1 + i) - 1));
            c = this.a;
            o = c;
            c.ui = this.ui;
            c.i = this.i * this.i;
            c.a = c; this.a = c.a;
            c.a = c = this.a = c.a = new c1(i, ui, new c1()); o = c.a = c;
            bar4(o.ToString()); this.bar4(c.a.a);

            // Read, Write Params
            x = c; x = o;
            o = x;

            // Read, Write Array Element
            object[] a1 = new c1[] { null, this.a, c };
            a1[1] = null; a1[2] = c;
            o = a1[1]; bar3(a1[2] != a1[1]);

            if (o == null)
            {
                return null;
            }
            else
            {
                return string.Empty;
            }
        }

        internal object bar3(bool x)
        {
            Console.WriteLine("    c1.bar3(bool)");

            // Read, Write Fields
            ui = ui - this.ui;
            i = i + 1;
            this.a = new c1(i, ui, a);

            // Read, Write Locals
            bool b = x;
            b = this.i > this.i + 1;

            // Read, Write Params
            x = (this.i == i + 1);
            goo(x.GetHashCode());

            // Read, Write Array Element
            bool[] a1 = new bool[] { true, false, x };
            a1[1] = x == (this.i != i - 1 + 1); a1[2] = x == (i >= this.i + 1 - 1);
            b = (a1[1]); b = a1[2];
            object o = b != a1[2];
            o = (a1[1].ToString()) == (a1[2].ToString());
            goo(a1[1].GetHashCode());

            if (b)
            {
                return this.i;
            }
            else
            {
                return a1[1];
            }
        }

        public c1 bar4(object x)
        {
            Console.WriteLine("    c1.bar4(object)");

            // Read, Write Fields
            ui = 0;
            this.ui = this.ui - (this.ui + this.ui) * this.ui;
            this.i = (i + 1) - (1 * (i / 1));
            this.a = (null);
            goo(this.i.GetHashCode());

            // Read, Write Locals
            object o = null;
            bool b = true;
            b = this.i <= this.i + 1 - 1;
            o = x;
            c1 c = new c1(i, this.ui, a);
            c.ui = (this.ui) + (this.ui) + c.ui;
            o = x = c;
            c.i = 1;
            c.i = this.i * (this.i / c.i + c.i);
            c.a = c = this.a = c.a = new c1(); c.a = c;
            goo(c.GetHashCode()); bar3(c.a.GetHashCode() != i);

            // Read, Write Params
            x = (o.ToString());
            x = x.ToString(); goo(x.GetHashCode()); goo(x.ToString().GetHashCode());

            // Read, Write Array Element
            object[] a1 = new object[] { (null), (this.a), c };
            a1[1] = ((this.a)); a1[2] = (c); a1[1] = (i);
            Array.Reverse(a1);
            o = a1[1]; goo(a1.GetHashCode()); bar3(a1[2] == null);

            if (b)
            {
                return this;
            }
            else if (!b)
            {
                return this.a;
            }
            else if (!b)
            {
                return new c1(i, ui, new c1(i + 1, ui - 1u, new c1(i + 2)));
            }
            else
            {
                return (c1)a1[2];
            }
        }
    }

    public class c2<T> : c1 // Inheritance, Generics
    {
        protected c1 c = new c1(0, 0, new c1(1, 1, new c1(2)));

        public void TEST()
        {
            // Nested Scopes
            byte b = 0;
            if (true)
            {
                sbyte sb = 0;
                if (!false)
                {
                    string s = "c2<T>.test()";
                    if (b == 0)
                    {
                        Console.WriteLine(s);
                        this.goo(x: b, y: sb); // Named Arguments
                    }
                }
                if (sb != 1)
                {
                    this.bar1(x: b, y: sb); // Named Arguments
                }
            }
            {
                sbyte sb2 = 0;
                if (b != 1)
                {
                    string s2 = "c2<T>.test()";
                    if (sb2 == 0)
                    {
                        Console.WriteLine(s2);
                        goo(x: b, y: sb2); // Named Arguments
                    }
                }
                if (b == sb2)
                {
                    bar2(x: b, y: sb2); // Named Arguments
                }
            }
            {
                c2<string> c = new c4();
                if (!(!true))
                {
                    // Method Calls - Ref, Paramarrays
                    // Overloaded Abstract Methods
                    int i = 1; long l = i; string s = "";
                    this.abst(ref s, 1, i);
                    c.abst(ref s, new int[] { 1, i, i });
                    c.abst(ref s, this.abst(ref s, l, l), l, l, l);

                    // Method Calls - Ref, Params
                    // Overloaded Virtual Methods
                    c1 a = c;
                    c.virt(i, c, new c2<string>[] { virt(i, ref a), new c4() });
                    virt(this.virt(i, ref a), c.virt(ref i, y: c, z: virt(i, ref a)));
                    virt(c.abst(ref s, l, l), this.abst(y: new long[] { 1, i, l }, x: ref s));
                    c = new c4();
                    virt(y: ref a, x: i);
                    virt(ref i, new c4(), new c4(), new c2<string>());
                    virt(new int[] { 1, 2, 3 });
                    virt(new Exception[] { });
                    virt(new c1[] { new c4(), new c2<string>() });
                }
            }
        }

        // Overridden Abstract Methods
        public override int abst(ref string x, params int[] y)
        {
            Console.WriteLine("    c2<T>.abst(ref string, params int[])");
            x = y[0].ToString(); y = null; // Read, Write Ref + Paramarrays
            return 0;
        }

        public override int abst(ref string x, params long[] y)
        {
            Console.WriteLine("    c2<T>.abst(ref string, params long[])");
            x = y[0].ToString(); y = null; // Read, Write Ref + Paramarrays
            return 1;
        }

        // Overridden Virtual Methods
        public override int virt(ref int x, c1 y, params c2<string>[] z)
        {
            Console.WriteLine("    c2<T>.virt(ref int, c1, params c2<string>[])");
            x = 0; x = y.GetHashCode(); z = null; // Read, Write Ref + Paramarrays
            return 0;
        }

        public override c2<string> virt(int x, ref c1 y)
        {
            Console.WriteLine("    c2<T>.virt(int, ref c1)");
            x.ToString(); y = new c1(x, (uint)x, y); // Read, Write Ref
            return new c2<string>();
        }

        public override int virt(params object[] x)
        {
            Console.WriteLine("    c2<T>.virt(params object[])");
            x.ToString(); x = null; // Read, Write Paramarrays
            return new int();
        }

        private void bar(T x)
        {
            Console.WriteLine("    c2<T>.bar(T)");

            // Read, Write Params
            T y = x;
            x = y;

            // Read Consts
            const string const1 = "";
            if (!false)
            {
                const int const2 = 1;
                const c1 const3 = null;
                if (true)
                {
                    this.bar4(const1); c.goo(const2 != 1); this.a = const3;
                }
            }
        }

        private T goo1(T x)
        {
            Console.WriteLine("    c2<T>.goo1(T)");

            int aa = 1;

            // Read, Write Params
            T y = x;
            x = y; bar(x);

            // Read Consts
            const long const1 = 1;
            const uint const2 = 1;
            while (const1 < const2 - aa)
            {
                continue;
            }

            while (const2 == const1 - aa + aa)
            {
                this.bar4(const1); c.goo(const2 != 1U);
                return x;
            }
            return x;
        }

        private bool goo(bool x)
        {
            Console.WriteLine("    c2<T>.goo(bool)");

            int aa = 1;

            // Read, Write Params
            x = x.ToString() == x.ToString(); a = c; a = this;

            // Read Consts
            const long const1 = 1;
            const uint const2 = 1;
            while (const1 < const1 - aa)
            {
                continue;
            }

            while (const2 == const2 - aa + aa)
            {
                return x;
            }
            return x;
        }

        protected c1 goo(byte x, object y)
        {
            Console.WriteLine("    c2<T>.goo(byte, object)");

            // Read, Write Params
            y = x; x = 1;
            c1 c = new c1();
            c.bar4(y); c.goo(x);

            // Read Consts
            const string const1 = "";
            bool b = false;
            if (!b)
            {
                const int const2 = 1;
                object o = y;
                while (y == o)
                {
                    const c1 const3 = null;
                    byte bb = 1;
                    if (bb == x)
                    {
                        this.bar4(const1); this.goo(const2 != 1); this.a = const3;
                        break;
                    }
                    else
                    {
                        return const3;
                    }
                }
                return c;
            }
            return null;
        }

        internal void bar1(byte x, object y)
        {
            Console.WriteLine("    c2<T>.bar1(byte, object)");

            // Read, Write Params
            y = x; x = 1;
            c1 c = new c1();
            c.bar4(y); c.goo(x);

            // Read Consts
            const long const1 = 1;
            const uint const2 = 1;
            while (const1 != x) continue;

            while (const2 == 1U)
            {
                this.bar4(const1); this.goo(const2 != 1);
                break;
            }
        }

        public int bar2(byte x, object y)
        {
            Console.WriteLine("    c2<T>.bar2(byte, object)");

            // Read, Write Params
            y = x; x = 1;
            c1 c = new c1();
            c.bar4(y); this.goo(x);

            // Read Consts
            const long const1 = 1;
            if (!false)
            {
                const sbyte const2 = 1;
                const c1 const3 = null;
                if (c != const3)
                {
                    c.bar4(const1); this.goo(const2 != 1); this.a = const3;
                }
            }
            return (int)const1;
        }

        internal float bar3(byte x, object y)
        {
            Console.WriteLine("    c2<T>.bar3(byte, object)");

            // Read, Write Params
            y = x; x = 1;
            double d = 1.1;
            c1 c = new c1();
            this.bar4(y); c.goo(x);

            // Read Consts
            const string const1 = "hi";
            bool b = !false;
            if (b)
            {
                const byte const2 = 1;
                const c1 const3 = null;
                if (const3 != c)
                {
                    this.bar4(const1); c.goo(const2 != 1); c.a = const3;
                    return (float)d;
                }
                return (float)(1.1f + (float)1.1);
            }
            return (float)d + (float)1.1;
        }
    }

    public class c3<T, U> // Generics
    {
        public static void test()
        {
            string s = "c3<T>.test()";
            {
                Console.WriteLine(s);
                goo(); goo(1); goo("1"); goo(1.1); // Overload Resolution, Implicit Conversions
            }
            // Nested Scopes
            {
                sbyte sb1 = 0;
                if (s != "")
                {
                    while (sb1 == 0)
                    {
                        byte b = 0;
                        c2<string> a = new c2<string>();
                        a.bar1(b, sb1);
                        sb1 = 1;
                        if (sb1 == 1) break;
                        else continue;
                    }
                }
                while (sb1 != 0)
                {
                    byte b = 1;
                    c2<string> a = new c2<string>();
                    a.bar1(b, sb1);
                    sb1 = 0;
                    if (sb1 == 1) break;
                    else continue;
                }
            }
            // Nested Scopes
            {
                sbyte sb2 = 0;
                while (sb2 < 2)
                {
                    sb2 = 3;
                    while (sb2 > 0)
                    {
                        sb2 = 0;
                        byte b = 1;
                        c2<int> a = new c2<int>();
                        a.bar2(b, sb2);
                    }
                    sb2 = 3;
                }
                if (sb2 >= 3)
                {
                    byte b = 0;
                    c2<int> a = new c2<int>();
                    a.bar2(b, sb2);
                }
            }
            // Nested Scopes
            {
                sbyte sb3 = 0;
                while (!string.IsNullOrEmpty(s))
                {
                    byte b = 1;
                    s = null;
                    if (sb3 != -20)
                    {
                        c2<bool> a = new c2<bool>();
                        a.bar3(b, sb3);
                    }
                    if (s != null) break;
                }
                while (s == null)
                {
                    byte b = 0;
                    if (s != null)
                    {
                        b = 1; continue;
                    }
                    c2<bool> a = new c2<bool>();
                    a.bar3(b, sb3);
                    s = "";
                    return;
                }
            }
        }

        // Static Methods
        protected static int goo(T x, U y)
        {
            Console.WriteLine("    c3<T, U>.goo(T, U)");
            int[] a = new int[3] { 1, 2, 3 }; a[1] = a[2];
            return (int)((long)x.GetHashCode() + (long)(int)(long)y.GetHashCode());
        }

        internal static c1 goo(object x)
        {
            Console.WriteLine("    c3<T, U>.goo(object)");
            c1[] a = new c1[3] { null, new c1(), new c1(1) }; a[1] = a[2];
            x = "hi";
            return new c1((int)1.1f, (uint)1, new c1(x.GetHashCode()));
        }

        private static float goo(string x)
        {
            Console.WriteLine("    c3<T, U>.goo(string)");
            string[] a = new string[] { x, x, "", null }; a[1] = a[2]; a[2] = a[1];
            return (float)goo(x.GetHashCode());
        }

        public static int goo(int x)
        {
            Console.WriteLine("    c3<T, U>.goo(int)");
            int[] a = new int[] { x, x, 1, 0 }; a[1] = a[2]; a[2] = a[1];
            return (int)x.GetHashCode() + x;
        }

        public static string goo()
        {
            Console.WriteLine("    c3<T, U>.goo()");
            string[] a = new string[] { "", null }; a[0] = a[1]; a[1] = a[0];
            return (string)null;
        }

        // Instance Methods
        protected int bar(T x, U y)
        {
            Console.WriteLine("    c3<T, U>.bar(T, U)");
            int[] a = new int[3] { 1, 2, 3 }; a[1] = a[2];
            return (int)((long)1 + (long)(int)(long)2);
        }

        public c1 bar(object x)
        {
            Console.WriteLine("    c3<T, U>.bar(object)");
            c1[] a = new c1[3] { null, new c1(), new c1(1) }; a[1] = a[2];
            return new c1((int)1.1f, (uint)1, new c1(x.GetHashCode()));
        }

        public float bar(string x)
        {
            Console.WriteLine("    c3<T, U>.bar(string)");
            string[] a = new string[] { x, x, "", null }; a[1] = a[2]; a[2] = a[1];
            x = a[2];
            return (float)goo(x.GetHashCode());
        }

        public int bar(int x)
        {
            Console.WriteLine("    c3<T, U>.bar(int)");
            int[] a = new int[] { x, x, 1, 0 }; a[1] = a[2]; a[2] = a[1];
            return (int)x.GetHashCode() + x;
        }

        public string bar()
        {
            Console.WriteLine("    c3<T, U>.bar()");
            string[] a = new string[] { "", null }; a[0] = a[1]; a[1] = a[0];
            return (string)null;
        }
    }

    public class c4 : c2<string> // Inheritance
    {
        public static bool b = true;
        public static byte b1 = 0;
        public static sbyte sb = 1;

        private static short s = 4;
        private static ushort us = 5;
        private static long l = 6;
        private static ulong ul = 7;

        public static bool Test()
        {
            string str = "c4.Test()";
            {
                int i = 2;
                Console.WriteLine(str);
                {
                    c1 a = new c1(i); a.goo(i);
                }
                double d = 1.1;
                {
                    sbyte sb = 1;
                    c1 a = new c1(i + (i + i));
                    a.goo(sb);
                    {
                        a.goo(d);
                    }
                }

                // Nested Scopes
                {
                    object o = i;
                    bool b = false;
                    if (!b)
                    {
                        byte b1 = 1;
                        string s_ = "    This is a test";
                        while (!b)
                        {
                            if (true) b = true;
                            Console.WriteLine(s_);
                            while (b)
                            {
                                if (true) b = false;
                                object oo = i;
                                bool bb = b;
                                if (!bb)
                                {
                                    if (!false) bb = true;
                                    byte b11 = b1;
                                    string ss = s_;
                                    if (bb)
                                    {
                                        Console.WriteLine(ss);
                                        if (bb != b)
                                        {
                                            object ooo = i;
                                            bool bbb = bb;
                                            if (bbb == true)
                                            {
                                                byte b111 = b11;
                                                string sss = ss;
                                                while (bbb)
                                                {
                                                    Console.WriteLine(sss);
                                                    bbb = false;

                                                    // Method Calls - Ref, Paramarrays
                                                    // Overloaded Abstract Methods
                                                    long l = i;
                                                    c4 c = new c4();
                                                    c.abst(ref s_, 1, i);
                                                    c.abst(ref s_, new int[] { 1, i, i });
                                                    c.abst(ref s_, c.abst(ref s_, l, l), l, l, l);

                                                    // Method Calls - Ref, Paramarrays
                                                    // Overloaded Virtual Methods
                                                    c1 a = new c4();
                                                    c.virt(i, c, new c2<string>[] { c.virt(i, ref a), new c4() });
                                                    c.virt(c.virt(i, ref a), c.virt(ref i, c, c.virt(i, ref a)));
                                                    c.
                                                      virt(
                                                           c.
                                                             abst(ref s_,
                                                                 l,
                                                                 l),
                                                           c.
                                                             abst(ref s_,
                                                                  new long[] {1, 
                                                                              i, 
                                                                              l})
                                                           );
                                                    c.virt(i, ref a);
                                                    c.virt(ref i,
                                                           new c4(),
                                                           new c4(),
                                                           new c2<string>()
                                                           );
                                                    c.virt(new int[] { 1, 2, 3 });
                                                    c.virt(new Exception[] { });
                                                    c.virt(new c1[] { new c4(), new c2<string>() });
                                                    s = (short)us;
                                                    if (true) continue;
                                                }
                                            }
                                            else if (bbb != true)
                                            {
                                                Console.WriteLine("Error - Should not have reached here");
                                                o = i; o = us;
                                                return (bool)o;
                                            }
                                            else if (bbb == false)
                                            {
                                                Console.WriteLine("Error - Should not have reached here");
                                                o = i; o = l;
                                                return (bool)o;

                                            }
                                            else
                                            {
                                                Console.WriteLine("Error - Should not have reached here");
                                                o = b; o = ul;
                                                return (bool)o;
                                            }
                                        }
                                    }
                                    else if (!b)
                                    {
                                        Console.WriteLine("Error - Should not have reached here");
                                        object o1 = b;
                                        return (bool)o1;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error - Should not have reached here");
                                        object o1 = b;
                                        return (bool)o1;
                                    }
                                }
                                else if (!bb)
                                {
                                    Console.WriteLine("Error - Should not have reached here");
                                    o = b;
                                    return (bool)o;
                                }
                                else
                                {
                                    Console.WriteLine("Error - Should not have reached here");
                                    object o1 = b;
                                    return (bool)o1;
                                }
                                while (b != false)
                                {
                                    b = false; break;
                                }
                                break;
                            }
                            while (b != true)
                            {
                                b = true; continue;
                            }
                        }
                    }
                    else if (b)
                    {
                        Console.WriteLine("Error - Should not have reached here");
                        return b;
                    }
                    else
                    {
                        Console.WriteLine("Error - Should not have reached here");
                        return (bool)b != true;
                    }
                }
            }
            return false;
        }

        // Non-Overloaded Method
        public static c4 goo(int i, string s, bool b, byte b1, long l, string s1)
        {
            Console.WriteLine("    c4.goo(int, string, bool, byte, long, string)");
            return new c4();
        }

        // Non-Overloaded Method
        internal static c5 bar(short s, ushort us, sbyte sb, float f, double d, double d1, float f1)
        {
            Console.WriteLine("    c4.bar(short, ushort, sbyte, float, double, double, float)");
            return new c5();
        }

        public class c5 : c3<string, c1> // Nested Class, Inheritance
        {
            internal static float f = 8.0f;
            internal static double d = 9.0;
            internal static string s1 = "Test";
            internal static object o = null;

            static c5()
            {
                o = s1; s1 = (string)o; o = f; o = null;
            }

            public int Test()
            {
                int i = 1; string s = "1"; bool b = true;
                short sh = 1; ushort us = 1; object o = i;
                c5 cc = new c5();
                Console.WriteLine("c5.test");
                {
                    uint ui = 1; o = ui;
                    i = sh; b = false; us = 1;
                    // Nested Scopes
                    if (true)
                    {
                        byte b1 = 1; long l = i; string s1 = s;
                        float f = 1.2f; o = f; l = ui;
                        c4.goo(sh, s, b, b1, i, s1); // Implicit Conversions
                        c4 c = new c4();
                        c.goo(sh); this.bar(sh);
                        cc.bar(c5.goo(cc.bar()));
                        c5.goo(cc.bar(c5.goo()));
                        if (b == false)
                        {
                            double d = f; ulong ul = 1; sbyte sb = 1; s1 = s;
                            c4.bar(sh, us, sb, f, d, ui, ul); // Implicit Conversions
                            c.bar4(us);
                            this.bar(cc.bar(), c);
                            c5.goo(this.bar(c5.goo(), c));
                        }
                        if (b1 >= l)
                        {
                            uint ui1 = 1; o = ui1;
                            i = sh; b = false; us = 1;
                            while (i != 1000)
                            {
                                byte b11 = 1; long l1 = i; string s11 = s1;
                                float f1 = 1.2f; o = f1; l1 = ui1;
                                c4.goo(sh, s1, b, b11, i, s11); // Implicit Conversions
                                c.goo(b);
                                this.bar(b); if (c5.goo() != null) c5.goo().ToString().GetHashCode();
                                cc.bar(this.bar(c5.goo()));

                                if (!false)
                                {
                                    double d1 = f1; ulong ul1 = 1; sbyte sb1 = 1; s1 = s;
                                    c4.bar(sh, us, sb1, f1, d1, ui1, ul1); // Implicit Conversions
                                    c.goo(b1, sb1);
                                    this.bar(o).bar4(c);
                                    cc.bar(c5.goo(o)).bar4(c).ToString();
                                    d1 = d;
                                    if (d != d1) return i;
                                }
                                if (i != 1000)
                                {
                                    uint ui2 = 1; o = ui2;
                                    i = sh; b = false; us = 1;
                                    {
                                        byte b12 = 1; long l2 = i; string s12 = s11;
                                        float f2 = 1.2f; o = f1; l2 = ui1;
                                        c4.goo(sh, s1, b, b12, i, s12); // Implicit Conversions
                                        c.bar4(b.ToString() == b.ToString());
                                        this.bar(c5.goo(cc.bar(i)));
                                        {
                                            double d2 = f2; ulong ul2 = 1; sbyte sb2 = 1; s1 = s;
                                            c4.bar(sh, us, sb2, f2, d2, ui2, ul2); // Implicit Conversions
                                            c.goo(false == true != false == b);
                                            c.bar4(sh > us == sh <= us);
                                            this.bar((object)c5.goo((object)cc.bar((object)i)));
                                            if (i != i +
                                                1 -
                                                1)
                                                return i -
                                                (int)b12;
                                        }
                                        if (i <=
                                              1000) break;
                                    }
                                }
                                if (i <=
                                      1000)
                                    i = 1000;
                                return sh;
                            }
                        }
                    }
                }
                return (int)sh;
            }
        }
    }

    public interface i0<T>
    {
        T prop1 { get; set; }
        List<T> prop2 { get; }
        void method1();
        void method1<TT>(T x, TT y);
    }

    public class c6<T, U> : IEnumerable<T>, IEnumerator<T> // Implement Framework Interfaces
    {
        // Constructor
        public c6()
        {
            Console.WriteLine("    c6<T, U>.ctor");
        }
        // Constructor
        public c6(int i)
            : this()
        {
            Console.WriteLine("    c6<T, U>.ctor(int i)");
        }
        // Constructor
        public c6(T i, List<Func<U>> j)
            : this(1)
        {
            Console.WriteLine("    c6<T, U>.ctor(T i, List<Func<U>> j)");
        }

        // Const Fields, Field Initializers
        protected const long L1 = 10101;
        protected const int I1 = 10101;

        // Enums
        protected enum E1 : long
        {
            A = L1, B = L2, C = L3
        }

        // Const Fields, Field Initializers
        public const long L2 = 2 * (long)E1.A;
        public const int I2 = 2 * I1;

        // Enums
        public enum E2 : long
        {
            Member1 = L1, Member2, Member3, Member4, Member5,
            Member6, Member7, Member8 = L2, Member9 = L1 + L1, Member10,
            Member11, Member12 = L3 * L2, Member13, Member14, Member15 = L2 + L3,
            Member16, Member17, Member18 = L3, Member19, Member20
        }
        public enum E3 : short
        {
            Member1 = 1, Member2 = 10, Member3 = 100, Member4 = 1000, Member5 = 10000,
            Member6 = 10, Member7 = 20, Member8 = 30, Member9 = 40, Member10 = 50,
            Member11 = 11, Member12 = 22, Member13 = 33, Member14 = 44, Member15 = 55,
            Member16, Member17, Member18, Member19, Member20
        }

        // Const Fields, Field Initializers
        protected const long L3 = L2 + L1;
        protected const int I3 = I2 + I1;

        // Read-Write Auto-Property
        public U prop1 { get; set; }

        // Read-Only Property
        public List<T> prop2
        {
            get
            {
                Console.WriteLine("    c6<T, U>.prop3.get()");
                return new List<T>();
            }
        }

        // Virtual Method
        protected virtual void virt<TT, UU, VV>(TT x, UU y, VV z)
        {
            Console.WriteLine("    c6<T, U>.virt<TT, UU, VV>(TT x, UU y, VV z)");
        }

        // Virtual Method
        protected virtual void virt<TT, UU, VV>(List<TT> x, List<UU> y, List<VV> z)
        {
            Console.WriteLine("    c6<T, U>.virt<TT, UU, VV>(List<TT >x, List<UU> y, List<VV> z)");
        }

        #region IEnumerable Implementation
        protected static IList<T> collection = new List<T>();
        // Implement Interface Implicitly
        public IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        // Implement Interface Explicitly
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region ICloneable Implementation
        // Implement Derived Class's Interface
        public object Clone()
        {
            return new c6<T, U>();
        }
        #endregion

        #region IDisposable Implementation
        // Implement Interface Explicitly
        private System.Threading.Tasks.Task proc = default(System.Threading.Tasks.Task);
        void IDisposable.Dispose()
        {
            proc.Dispose();
        }
        #endregion

        #region IEnumerator Implementation
        private IEnumerator<T> enumerator = collection.GetEnumerator();
        // Implement Interface Implicitly
        public T Current
        {
            get { return enumerator.Current; }
            // Additional Accessor
            set
            {
                if (enumerator.Current.Equals(value))
                {
                }
            }
        }

        // Implement Interface Explicitly
        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        // Implement Interface Explicitly
        bool System.Collections.IEnumerator.MoveNext()
        {
            return MoveNext();
        }
        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        // Implement Interface Implicitly
        public void Reset()
        {
            enumerator.Reset();
        }
        #endregion

        internal void Test()
        {
            bool b1 = true, b2 = true;
            int x = 0;
            if (b1 && b2)
            {
                Console.WriteLine("c6<T, U>.Test()");
                // Generic Virtual Methods, Enums
                E1 enum1 = E1.A; E2 enum2 = E2.Member17; E3 enum3 = E3.Member19;
                virt(enum1, E2.Member10, new List<E3>());
                enum1 = E1.A; enum3 = E3.Member3;
                c6<U, T> c = new c7<T, U>(1);
                c.virt<E1[], E2[], E3>(new E1[] { E1.A, E1.B }, new E2[] { enum2, E2.Member16 }, enum3);
                enum2 = E2.Member18;

                x = (int)enum1; x = x++; x = x--; ++x; --x;
            }
            else if (b1 || b2 || x++ >= --x)
            {
                b1 = (x++).Equals(--x) || (++x).CompareTo(x--) > 0 && x == (--x);
            }
        }
    }

    public interface i1<T, U> : i0<T>
    {
        new List<U> prop2 { set; }
        new void method1();
        void method1<TT, UU>(T x, TT y, U xx, UU yy, ref TT zz);
        void method2();
    }

    public class c7<T, U> : c6<U, T>, IEnumerable<U>, IDisposable, ICloneable, ICollection<U>
    {
        // Constructor
        public c7()
            : base()
        {
            Console.WriteLine("    c7<T, U>.ctor()");
        }
        // Constructor
        public c7(int i)
            : base(i)
        {
            ++i;
            Console.WriteLine("    c7<T, U>.ctor(int i)");
            --i;
        }
        // Constructor
        public c7(T i, List<Func<U>> j)
            : base(default(U), new List<Func<T>>())
        {
            Console.WriteLine("    c7<T, U>.ctor(T i, List<Func<U>> j)");
        }

        // Hide Enum
        public new enum E1
        {
            A = I1, B = I1 + I2, C = I2 / I3
        }

        // Const Fields
        const E1 enum1 = E1.A;
        const E2 enum2 = E2.Member19;

        // Hide Enum
        public new enum E2 : long
        {
            Member1 = L1, Member2 = enum2, Member3, Member4 = I2 + (I1 - I3), Member5 = ((I1 - I2)),
            Member6, Member7 = I1, Member8 = L2, Member9 = (L1 + L1) - (I3), Member10,
            Member11 = enum1, Member12 = L3 * (L2 + I1) / I3, Member13, Member14 = enum3, Member15 = L2 + L3 + I2,
            Member16, Member17, Member18 = L3, Member19, Member20 = enum2
        }

        // Read-Write Property
        public List<T> prop3
        {
            get
            {
                Console.WriteLine("    c7<T, U>.prop3.get()");
                return default(List<T>);
            }
            // Private Accessor
            private set { Console.WriteLine("    c7<T, U>.prop3.set()"); }
        }
        // Hide Read-Only Property
        public new U prop2
        {
            get
            {
                Console.WriteLine("    c7<T, U>.prop2.get()");
                return default(U);
            }
        }
        // Hide Read-Write Property
        public new IDictionary<T, U> prop1
        {
            get { Console.WriteLine("    c7<T, U>.prop1.get()"); return new Dictionary<T, U>(); }
            protected internal set { Console.WriteLine("    c7<T, U>.prop1.set()"); }
        }

        // Const Fields
        const E3 enum3 = E3.Member19;

        // Override Generic Virtual Method
        protected override void virt<TT, UU, VV>(TT x, UU y, VV z)
        {
            Console.WriteLine("    c7<T, U>.virt<TT, UU, VV>(TT x, UU y, VV z)");
            // Enums
            const E1 enum1 = E1.A; E2 enum2 = E2.Member17; const E3 enum3 = E3.Member19;
            bool b = (E1.B == enum1) || (enum2 < E2.Member19) && (enum3 >= E3.Member9);
            long e = (long)enum1; b = e++ == e-- || --e == ++e;
        }

        // Hide Generic Virtual Method
        protected new TT virt<TT, UU, VV>(List<TT> x, List<UU> y, List<VV> z)
        {
            Console.WriteLine("    c7<T, U>.virt<TT, UU, VV>(TT x, UU y, VV z)");
            return default(TT);
        }

        #region IEnumerable Re-Implementation
        // Re-Implement Interface Implicitly
        new IEnumerator<U> GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        // Re-Implement Interface Explicitly
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region ICollection Implementation
        // Implement Interface Implicitly
        private new List<U> collection = new List<U>();
        public void Add(U item)
        {
            collection.Add(item);
        }

        public void Clear()
        {
            collection.Clear();
        }

        public bool Contains(U item)
        {
            return collection.Contains(item);
        }

        public void CopyTo(U[] array, int arrayIndex)
        {
            collection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(U item)
        {
            return collection.Remove(item);
        }
        #endregion

        internal new void Test()
        {
            Console.WriteLine("c7<T, U>.Test()");
            c6<T, U> b = new c6<T, U>();
            // Read, Write Properties
            U uu = default(U); T tt = default(T);
            b.prop1 = uu; uu = b.prop1;
            b.prop2.Add(tt); b.prop2.Count.ToString();

            c7<T, U> d = this;
            IDictionary<T, U> dict = new Dictionary<T, U>();
            // Read, Write Properties
            d.prop1 = dict; dict = d.prop1; d.prop1.Add(tt, uu);
            uu = d.prop2; d.prop2.ToString();
            List<T> l = new List<T>();
            d.prop3 = l; l = d.prop3;
        }
    }

    public interface i2 : i1<int, int>
    {
        new int prop1 { get; }
        void method1(int x, int y);
        new void method1();
        new int method2();
    }

    abstract public class c8 : i0<int>, i1<long, long>
    {
        internal int _prop1 = 0;
        // Implement Read-Write Property
        // Virtual Property
        virtual public int prop1
        {
            get
            {
                Console.WriteLine("    c8.prop1.get()");
                return _prop1++;
            }
            set
            {
                _prop1 = --value;
                Console.WriteLine("    c8.prop1.set()");
            }
        }

        // Read-Write Property Implements Read-Only
        protected List<int> _prop2 = new List<int>();
        public List<int> prop2
        {
            get
            {
                Console.WriteLine("    c8.prop2.get()");
                return _prop2;
            }
            // Inaccessible Setter
            private set
            {
                _prop2 = value;
                Console.WriteLine("    c8.prop2.set()");
            }
        }

        List<long> __prop2 = new List<long>();
        // Explicitly Implement Write-Only Property
        List<long> i1<long, long>.prop2
        {
            set
            {
                __prop2 = value;
                Console.WriteLine("    c8.i1<long, long>.prop2.set()");
            }
        }

        // Explicitly Implement Read-Only Property
        List<long> i0<long>.prop2
        {
            get
            {
                Console.WriteLine("    c8.i0<long>.prop2.set()");
                return __prop2;
            }
        }

        // Explicitly Implement Read-Write Property
        long i0<long>.prop1
        {
            get
            {
                Console.WriteLine("    c8.i0<long>.prop1.get()"); return _prop1;
            }
            set
            {
                _prop1 = Convert.ToInt32(value--); --_prop1;
                Console.WriteLine("    c8.i0<long>.prop1.set()");
            }
        }

        // Abstract Properties, Protected Accessor
        public abstract IList<int> prop3 { get; protected set; }
        // Virtual Auto-Property, Internal Accessor
        public virtual IDictionary<string, IList<int>> prop4 { internal get; set; }
        // Virtual Auto-Property, Protected Internal Accessor
        public virtual i2 prop5 { get; protected internal set; }

        // Implicitly Implement Methods
        // Virtual Methods
        public virtual void method1()
        {
            Console.WriteLine("    c8.method1()");
        }
        public virtual void method1<TT>(long x, TT y)
        {
            Console.WriteLine("    c8.i0<int>.method1<TT>(long x, TT y)");
        }
        public virtual void method2()
        {
            Console.WriteLine("    c8.method2()");
        }

        // Implicitly Implement Method
        // Abstract Method
        public abstract void method1<TT, UU>(long x, TT y, long xx, UU yy, ref TT zz);

        // Explicitly Implement Methods
        void i0<int>.method1<TT>(int x, TT y)
        {
            Console.WriteLine("    c8.i0<int>.method1<TT>(int x, TT y)");
        }
        void i0<long>.method1()
        {
            Console.WriteLine("    c8.i0<long>.method1()");
        }
        void i1<long, long>.method1()
        {
            Console.WriteLine("    c8.i1<long, long>.method1()");
        }

        // Abstract Override Methods
        public abstract override string ToString();
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();

        // Implement Derived Type's Interface
        public void method1(int x, int y)
        {
            Console.WriteLine("    c8.method1(int x, int y)");
        }

        public void Test()
        {
            Console.WriteLine("c8.Test()");
            i0<int> a = this;
            // Invoke Interface Methods
            a.method1();
            a.method1(1, true);
            a.method1<Exception>(1, new ArgumentException());
            // Invoke Interface Properties
            int x = a.prop1--; a.prop1 = x;
            List<int> y = a.prop2;

            i1<long, long> b = this;
            // Invoke Interface Methods
            b.method1();
            b.method1(1, 0);
            AccessViolationException e = new AccessViolationException();
            b.method1(1, e, 1, new ArgumentException(), ref e);
            b.method2();
            // Invoke Interface Properties
            b.prop1 = x; x = Convert.ToInt32(++b.prop1);
            b.prop2 = new List<long>();
        }
    }

    public class c9 : c8, i2
    {
        // Write-Only Property Overrides Read-Write
        public override int prop1
        {
            set
            {
                Console.WriteLine("    c9.prop1.set()");
                _prop1 = value;
            }
        }

        // Hide Field
        protected new List<int> _prop2()
        {
            Console.WriteLine("    c9._prop2");
            return new List<int>();
        }

        // Read-Write Auto-Property Implicitly Implements Read-Only + Write-Only Property
        public new List<int> prop2 { get; set; }

        // Sealed Property
        public sealed override IList<int> prop3
        {
            get
            {
                Console.WriteLine("    c9.prop3.get()");
                return _prop2();
            }
            // Override Protected Accessor
            protected set
            {
                value.ToString();
                Console.WriteLine("    c9.prop3.set()");
            }
        }

        // Sealed Property
        // Read-Only Property Overrides Read-Write Property
        public sealed override IDictionary<string, IList<int>> prop4
        {
            internal get
            {
                Console.WriteLine("    c9.prop4.get()");
                Dictionary<string, IList<int>> x = new Dictionary<string, IList<int>>();
                x.Add("", new List<int>());
                return x;
            }
            // Synthesized Sealed Setter
        }

        // Sealed Property
        // Write-Only Property Overrides Read-Write Property
        public sealed override i2 prop5
        {
            // Synthesized Sealed Getter
            protected internal set
            {
                i0<int> x = value;
                x.ToString();
                Console.WriteLine("    c9.prop5.set()");
            }
        }

        // Hide Method
        public new void method1()
        {
            Console.WriteLine("    c9.method1()");
        }

        // Hide Method
        // Implicitly Implement Method
        public new int method2()
        {
            Console.WriteLine("    c9.method2()");
            return 1;
        }

        // Implicitly Implement Generic Method
        public void method1<TT, UU>(int x, TT y, int xx, UU yy, ref TT z)
        {
            Console.WriteLine("    c9.method1<TT, UU>(int x, TT y, int xx, UU yy, ref TT z)");
        }
        // Override Abstract Method
        public override void method1<TT, UU>(long x, TT y, long xx, UU yy, ref TT z)
        {
            Console.WriteLine("    c9.method1<TT, UU>(long x, TT y, long xx, UU yy,  ref TT z)");
        }

        // Sealed Methods
        public sealed override string ToString()
        {
            return _prop1.ToString();
        }
        public sealed override bool Equals(object obj)
        {
            return _prop1.Equals(obj);
        }
        public sealed override int GetHashCode()
        {
            return _prop1.GetHashCode();
        }

        public void test()
        {
            Test();
            Console.WriteLine("c9.Test()");
            i0<int> a = this;
            i1<int, int> b = this;
            a = b;
            i2 c = this;
            b = c;

            // Invoke Interface Methods
            short i = 1;
            c.method1();
            c.method1(i++, --i);
            c.method1(i--, 1L);
            c.method1(1, b, 1, 1L, ref a);
            c.method2();
            // Invoke Interface Properties
            c.prop2 = new List<int>(c.prop1 + 100);

            c9 dd = this; c8 bb = dd;
            // Invoke Virtual / Abstract Methods
            bb.method1();
            bb.method1(1, 1);
            bb.method1(--i, 1L);
            bb.method1(i++ - --i, b, --i * i--, 1L, ref a);
            bb.method2();

            int x = 0;
            // Invoke Virtual / Abstract Properties
            bb.prop1 = x; x = --bb.prop1 + ++bb.prop1;
            List<int> y = bb.prop2;
            bb.prop3.ToString(); dd.prop3 = bb.prop2;
            bb.prop4 = new Dictionary<string, IList<int>>(); bb.prop4.ToString();
            bb.prop5 = this; c = bb.prop5;
        }
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    struct s0<[nested.FirstAttribute]T> : i1<int, int>, i2
    {
        int i, j;
        private List<int> _prop2;

        // Static Constructor
        static s0()
        {
            // Extension Methods
            var collection = new int[] { 1, 2, (byte)3, (short)4, (int)5L };
            collection.AsParallel<int>();
            collection.Aggregate((a, b) => { return a; });
            var bl = collection.AsQueryable().Any() ||
                collection.AsQueryable<int>().Count() > collection.Sum();
            Console.WriteLine("    s0.cctor()");
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public List<int> prop2
        {
            set
            {
                Console.WriteLine("    s0.prop2.set()");
                _prop2 = value;
                throw new Exception();
            }
        }
        Exception k;
        public void method1()
        {
            k = (Exception)new ArgumentException();
            Console.WriteLine("    s0.method1()");
            throw k ?? new FormatException();
        }

        ArgumentException e;
        public void method1<TT, UU>(int x, TT y, [nested.FirstAttribute.Second(Value = 0, Value2 = 1)] int xx, UU yy, ref TT zz)
        {
            e = new ArgumentNullException();
            Console.WriteLine("    s0.method1<TT, UU>(int x, TT y, int xx, UU yy, ref TT zz)");
            throw (Exception)(ArgumentException)e ?? new Exception();
        }

        FieldAccessException f;
        public void method2()
        {
            f = new FieldAccessException();
            Console.WriteLine("    s0.method2()");
            throw f;
        }

        public int prop1
        {
            get
            {
                Console.WriteLine("    s0.prop1.get()");
                var v = new AccessViolationException();
                throw v ?? new Exception();
            }
            set
            {
                try
                {
                    if (value == j)
                        i = value;
                    else
                        j = value;
                    Console.WriteLine("    s0.prop1.set()");
                    Exception e = new IndexOutOfRangeException();
                    throw e ?? e;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    Exception e = new DivideByZeroException();
                    throw e ?? (Exception)new object();
                }
            }
        }

        List<int> i0<int>.prop2
        {
            get
            {
                Console.WriteLine("    s0.i0<int>.prop2.get()");
                throw new InvalidOperationException();
            }
        }

        public void method1<[nested.First(1)]TT>(int x, TT y)
        {
            Console.WriteLine("    s0.method1<TT>(int x, TT y)");
            throw new MemberAccessException();
        }

        int i2.prop1
        {
            get
            {
                Console.WriteLine("    s0.i2.prop1.get()");
                throw new UnauthorizedAccessException();
            }
        }

        void i2.method1([nested.First(Value = Value)]int x, [nested.FirstAttribute.SecondAttribute.Third(1, 1, Value3 = 0)] int y)
        {
            KeyNotFoundException ex = null;
            ex = ex ?? new KeyNotFoundException();
            Console.WriteLine("    s0.i2.method1(int x, int y)");
            throw ex;
        }

        [Obsolete()]
        void i2.method1()
        {
            Console.WriteLine("    s0.i2.method1()");
            throw new NotSupportedException();
        }

        [ContextStatic]
        public static string s = string.Empty;
        int i2.method2()
        {
            Console.WriteLine("    s0.i2.method2()");
            return s.Count();
        }

        List<int> i1<int, int>.prop2
        {
            set
            {
                Console.WriteLine("    s0.i1<int, int>.prop2.set()");
                var j = 0;
                value = new List<int>(new int[] { 1 / j });
            }
        }

        void i1<int, int>.method1()
        {
            Console.WriteLine("    s0.i1<int, int>.method1()");
            object o = null;
            o.ToString();
        }

        void i1<int, int>.method1<TT, UU>(int x, TT y, int xx, UU yy, ref TT zz)
        {
            Console.WriteLine("    s0.i1<int, int>.method1<TT, UU>(int x, TT y, int xx, UU yy, ref TT zz)");
            throw new NotImplementedException();
        }

        [ThreadStatic]
        const long l = 0;

        void i1<int, int>.method2()
        {
            l.ToString();
            Console.WriteLine("    s0.i1<int, int>.method2()");
            throw new OutOfMemoryException();
        }

        [Flags]
        enum Flags
        {
            A, B, C
        }

        int i0<int>.prop1
        {
            get
            {
                Console.WriteLine("    s0.i0<int>.prop1.get()");
                throw null;
            }
            set
            {
                Console.WriteLine("    s0.i0<int>.prop1.set()");
                BadImageFormatException o = null;
                throw o;
            }
        }

        [LoaderOptimization(LoaderOptimization.NotSpecified)]
        void i0<int>.method1()
        {
            Console.WriteLine("    s0.i0<int>.method1()");
            try
            {
                throw new NotImplementedException();
            }
            catch (NotImplementedException ex)
            {
                throw ex;
            }
            catch
            {
                throw;
            }
        }

        [Obsolete]
        void i0<int>.method1<TT>(int x, [nested.FirstAttribute.Second(Value, Value, Value = Value, Value2 = Value)] TT y)
        {
            Console.WriteLine("    s0.i0<int>.method1<TT>(int x, TT y)");
        }

        const int Value = 0;
        [nested.First(Value, Value = (short)Value)]
        [LoaderOptimization(LoaderOptimization.NotSpecified)]
        public void Test()
        {
            Console.WriteLine("s0.Test()");
            i0<int> a = this;
            i1<int, int> b = this;
            a = b;
            i2 c = this;
            b = c;

            var aa = a = b = c;
            var bb = b = c;
            var cc = c;

            {
                // Extension Methods
                int[] ii = { 1, 2, 3 };
                var q = ii.Where((jj) => jj > 0).Select((jj) => jj);
                Console.WriteLine("    Count = " + q.Count());
            }

            // Nested Exception Handling
            try
            {
                // Invoke Interface Methods
                aa.method1();
            }
            catch (NotImplementedException ex)
            {
                Console.WriteLine("    " + ex.Message);
                try
                {
                    // Invoke Interface Methods
                    aa.method1(1, true);
                    aa.method1<Exception>(1, new ArgumentException());
                    throw;
                }
                catch (NotImplementedException ex2)
                {
                    Console.WriteLine("    " + ex2.Message);
                    try
                    {
                        // Invoke Interface Properties
                        var x = aa.prop1--; aa.prop1 = x;
                        List<int> y = a.prop2;
                    }
                    catch (NotImplementedException ex3)
                    {
                        Console.WriteLine("    " + ex3.Message);
                        throw ex3;
                    }
                    catch (Exception ex3)
                    {
                        Console.WriteLine("    " + ex3.Message);
                    }
                    finally
                    {
                        // Extension Methods
                        var q = "string".Where((s) => s.ToString() != "string").
                            SelectMany((s) => new char[] { s });
                        foreach (var i in q)
                        {
                            Console.WriteLine("    Item: " + i);
                        }
                        Console.WriteLine("    First");
                    }
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("    " + ex2.Message);
                    throw;
                }
                finally
                {
                    // Extension Methods
                    int[] ii = { 1, 2, 3 };
                    foreach (var i in ii.Where((jj) => jj >= ii[0]).Select((jj) => jj))
                    {
                        if (ii.ToArray().Count() > 0)
                            Console.WriteLine("    Item: " + i);
                    }
                    Console.WriteLine("    Second");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("    " + ex.Message);
                throw;
            }
            finally
            {
                int i = 0;
                int[] ii = { 1, 2, 3 };
                // Extension Methods
                var q = ii.Where((jj) => jj > 0).Select((jj) => jj);
                for (i = 0; i < 3; ++i)
                {
                    if (q.Any())
                        Console.WriteLine("    Item: " + q.ElementAt(i));
                    else if (q.All((jj) => jj.GetType() is object))
                        Console.WriteLine("    Item: " + q.ElementAt(i));
                }
                Console.WriteLine("    Count = " + q.Count());
                Console.WriteLine("    Third");
            }

            try
            {
                // Invoke Interface Methods
                bb.method1();
                bb.method1(1, 0);
                AccessViolationException e = new AccessViolationException();
                bb.method1(1, e, 1, new ArgumentException(), ref e);
                bb.method2();
                // Invoke Interface Properties
                var x = 0;
                bb.prop1 = x; x = Convert.ToInt32(++bb.prop1);
                bb.prop2 = new List<int>();
            }
            catch (Exception ex)
            {
                int j = 2;
                // Extension Methods
                foreach (var i in aa.ToString().Where((e) => e.ToString() != j.ToString()).
                    OrderBy((e) => e).Distinct())
                    Console.WriteLine("    Item: " + i);
                Console.WriteLine("    " + ex.Message);
                Console.WriteLine("    Fourth");
            }

            try
            {
                // Invoke Interface Methods
                var i = 1L;
                cc.method1();
                cc.method1((int)i++, (short)--i);
                cc.method1((short)i--, (int)1L);
                cc.method1(1, bb, 1, 1L, ref aa);
                cc.method2();
                // Invoke Interface Properties
                cc.prop2 = new List<int>(cc.prop1 + 100);
                object o = null; o.ToString();
            }
            catch (Exception ex)
            {
                char j = (char)0;
                // Extension Methods
                foreach (char i in ex.Message.
                    Where((e) => j.ToString() != ex.Message + e.ToString()).
                    OrderBy((e) => e))
                    Console.WriteLine("    Item: " + i);
                Console.WriteLine("    " + ex.Message);
            }
            finally
            {
                Console.WriteLine("    Fifth");
            }
        }
    }

    [System.Runtime.InteropServices.StructLayout((short)0, Pack = 0, Size = 0)]
    [nested.First]
    [Serializable]
    struct s1
    {
        [NonSerialized]
        internal int _i;
        [NonSerialized()]
        [nested.First]
        internal int _j;

        // Overloaded Constructors
        private s1(int i, long l)
        {
            _i = i; _j = (int)l;
            Console.WriteLine("    s1.ctor(int i, long l)");
        }

        private s1(int i)
        {
            _i = i; _j = (short)i;
            Console.WriteLine("    s1.ctor(int i)");
        }

        [nested.First()]
        public override bool Equals([nested.FirstAttribute.SecondAttribute.Third(0, 1, Value2 = 1)]object obj)
        {
            if (this.ToString() == ((s1)obj).ToString())
            {
                var s = (s1)(s1)obj;
                return base.Equals(this);
            }
            Console.WriteLine("    s1.Equals(object obj)");
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            Console.WriteLine("    s1.GetHashCode()");
            return base.GetHashCode();
        }

        public override string ToString()
        {
            Console.WriteLine("    s1.ToString()");
            return "    s1.ToString()";
        }

        // Static Constructor
        [nested.FirstAttribute.SecondAttribute(Value = 0, Value2 = (byte)l)]
        static s1()
        {
            // Extension Methods
            var collection = new double[] { 1, (double)2, (float)3 };
            var bl = collection.AsEnumerable().Count() ==
                    collection.AsQueryable().DefaultIfEmpty().Distinct().
                    ElementAt((short)collection.FirstOrDefault());
            var s = new nested.FirstAttribute.SecondAttribute.ThirdAttribute(0, l, (short)l);
            Console.WriteLine("    s2.cctor()");
        }

        const long l = 2;
        [nested.First]
        public static void Test()
        {
            Console.WriteLine("s1.Test()");
            try
            {
                try
                {
                    var s = new s1();
                    s.ToString();

                    var l = new List<int>(new int[] { 1, 2, 3 });

                    // For Loop
                    foreach (var i in l)
                    {
                        if (i > 0)
                        {
                            Console.WriteLine("    " + i);
                        }
                        continue;
                    }
                    throw new Exception();
                }
                catch
                {
                    var s = new s1(1);

                    // For Loop
                    foreach (var i in "string")
                    {
                        // Boxing
                        object o = s;
                        // Ternary
                        var str = o != null ? new s1(o.GetHashCode()) : o == null ? default(s1) :
                            new s1(o.GetHashCode(), s.Equals(o).GetHashCode());
                        Console.WriteLine(str);

                        // Unboxing
                        s = (s1)o;
                        throw;
                    }
                }
                finally
                {
                    var s = new s1(1, 1);
                    s.Equals(s);
                    // Nested Loops
                    for (var i = 0; i <= 3; ++i)
                    {
                        if (i > 0)
                        {
                            // Boxing
                            object o = s;

                            // Ternary Operator
                            var str = o != null ? o.ToString() : o == null ? null : o.ToString();

                            // Unboxing
                            s = (s1)o;
                            break;
                        }
                        else
                        {
                            for (uint j = (uint)i; j + i < 3; ++j)
                            {
                                // Boxing
                                object o = s;

                                // Ternary Operator
                                var str = o != null ? o.ToString() == (i + j).ToString() :
                                    o == null ? false : o.ToString() != j.ToString();

                                // Unboxing
                                s = (s1)o;

                                if (o is s1 && j > 0)
                                    break;
                                else
                                    continue;
                            }
                        }
                    }

                    var iii = 1;
                    object ooo = "";
                    goto L1;
                L1: Console.WriteLine("    iii = " + iii);
                    ooo = "";
                    iii++;
                    if (iii >= 5 && ooo is string)
                    {
                        var sss = ooo as string;
                        ooo = sss ?? string.Empty;
                        ooo = iii;
                        goto L2;
                    }
                    else if (ooo is string)
                    {
                        ooo = new ArgumentException();
                        var eee = ooo as Exception;
                        ooo = eee ?? new Exception();
                        ooo = iii;
                        ooo = new s1();
                        if (ooo is s1)
                            goto L1;
                    }
                L2: Console.WriteLine("    iii = " + iii);
                    if (ooo.GetType() == typeof(string))
                        Console.WriteLine("    ooo is string");
                    else if (ooo.GetType() == typeof(Exception))
                        Console.WriteLine("    ooo is Exception");
                    else if (ooo.GetType() == typeof(s1))
                        Console.WriteLine("    ooo is s1");
                    else if (ooo.GetType() == typeof(int))
                        Console.WriteLine("    ooo is int");
                }
            }
            catch
            {
                Console.WriteLine("    First");
                var iii = 1;
                goto L1;
            L1: Console.WriteLine("    iii = " + iii);
                iii++;
                if (iii >= 5)
                    goto L2;
                else
                    goto L1;
            L2: Console.WriteLine("    iii = " + iii);
            }
        }
    }

    interface i3<T>
    {
        void method<U>(out T x, ref List<U> y, out Exception e, out nested.s2 s);
        void method<U>(ref List<T> x, out U y, out ArgumentException e, out nested.s2 s);
    }

    namespace nested
    {
        using nested;
        [Serializable]
        struct s2 : i3<string>
        {
            // Static Constructor
            [nested.FirstAttribute.SecondAttribute.Third(Value2 = 0)]
            static s2()
            {
                // Extension Methods
                var collection = new long[] { 1, 2, (byte)3, (short)4, (int)5L };
                var bl = collection.Any((a) => a != 0) || collection.All((a) => a > 1);
                bl = collection.AsEnumerable<long>().Average() >
                    collection.AsParallel().Average<long>((long a) => a + bl.GetHashCode());
                Console.WriteLine("    s2.cctor()");
            }

            // Nested Struct
            [nested.FirstAttribute.Second]
            internal struct s1
            {
                [nested.FirstAttribute.SecondAttribute.Third]
                public override string ToString()
                {
                    try
                    {
                        Console.WriteLine("    s2.s1.ToString()");
                        return base.ToString();
                    }
                    finally
                    {
                        Console.WriteLine("    First");
                    }
                }
            }

            [nested.FirstAttribute.SecondAttribute.ThirdAttribute.Second]
            public void Test<
                [nested.FirstAttribute.SecondAttribute.Third(0, 1, 2, Value3 = 2, Value = 0, Value2 = 1)]T,
                [nested.FirstAttribute]U>(T t, U u)
            {
                Console.WriteLine("s2.Test()");
                try
                {
                    s1.ReferenceEquals(t, u);

                    // Extension Methods
                    var s = default(s1).ToString();
                    default(s1).Stringize();

                    default(s1).ToString(out s);
                    var zero = default(int);
                    s1.Equals(t.GetHashCode() / zero, zero);
                }
                catch (Exception ex)
                {
                    {
                        i3<string> i = this;
                        string s = string.Empty; List<int> l = new List<int>();
                        i.method(out s, ref l, out ex, out this);
                    }
                    {
                        i3<U> i = new c1<U, T>.c2<U>();
                        var r = (c1<U, T>.c2<U>)i;
                        r.method();
                        // TODO - Uncomment below statements when bugs 8701, 8702 are fixed
                        // var x = new List<string>();
                        // i.method(out u, ref x, out ex, out this);
                    }
                }
            }

            // Implicit Implementation
            public void method<U>(out string x, ref List<U> y, out Exception e, out s2 s)
            {
                Console.WriteLine("    s2.method<U>(out string x, ref List<U> y, out Exception e, out s2 s)");
                e = new ArgumentException();
                var ee = (ArgumentException)e;
                var l = new List<string>();
                ((i3<string>)this).method(ref l, out x, out ee, out s);
            }
            // Explicit Implementation
            void i3<string>.method<U>(ref List<string> x, out U y, out ArgumentException e, out s2 s)
            {
                Console.WriteLine("    s2.i3<string>.method<U>(ref List<string> x, out U y, out ArgumentException e, out s2 s)");
                bool b = false;
                y = default(U); e = new ArgumentException();
                if (b)
                {
                    var l = new List<U>(); l.Add(y);
                    var a = x.FirstOrDefault();
                    var ee = (Exception)e;
                    method(out a, ref l, out ee, out s);
                }
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method |
            AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Struct |
            AttributeTargets.Parameter | AttributeTargets.Assembly | AttributeTargets.Module |
            AttributeTargets.GenericParameter)]
        [First]
        [FirstAttribute.SecondAttribute.Third(1, 2, 3, Value2 = 1)]
        public class FirstAttribute : Attribute
        {
            public int Value = (int)default(long);
            [First(Value = default(int))]
            [Second(value: default(short), Value = default(short))]
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method |
            AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Struct |
            AttributeTargets.Parameter | AttributeTargets.Assembly | AttributeTargets.Module |
            AttributeTargets.Constructor)]
            internal class SecondAttribute : FirstAttribute
            {
                new public long Value = default(int);
                public short Value2 = (short)default(int);
                // Static Constructor
                [Second]
                static SecondAttribute()
                {
                    // Extension Methods
                    var collection = new char[] { 'a', 'b', "c".Single() };
                    collection.ElementAtOrDefault(collection.First());
                    collection.Except(collection);
                    collection.Intersect(collection.AsEnumerable());
                    Console.WriteLine("    SecondAttribute.cctor()");
                }
                [Third]
                internal SecondAttribute()
                {
                    Console.WriteLine("    SecondAttribute.ctor()");
                }
                [ThirdAttribute.Second]
                internal SecondAttribute(int value)
                {
                    Value = value;
                    Console.WriteLine("    SecondAttribute.ctor(int value)");
                }
                [ThirdAttribute.Third]
                public SecondAttribute(int value, long value2)
                {
                    Value = value;
                    Value2 = (short)value2;
                    Console.WriteLine("    SecondAttribute.ctor(int value, long value2)");
                }
            }

            [Second(00, 11)]
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method |
            AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Struct |
            AttributeTargets.Parameter | AttributeTargets.Assembly | AttributeTargets.Module |
            AttributeTargets.Constructor | AttributeTargets.GenericParameter)]
            internal class ThirdAttribute : SecondAttribute
            {
                new public short Value = default(byte);
                new public long Value2 = (int)default(short);
                public long Value3 = default(int);

                // Static Constructor
                [Third]
                static ThirdAttribute()
                {
                    // Extension Methods
                    var collection = "string";
                    var i = collection.Skip(5).SingleOrDefault();
                    collection.Skip(2).SkipWhile((a) => a > 0);
                    collection.Take(2).TakeWhile((a) => a > 0).ToArray().ToList();
                    Console.WriteLine("    ThirdAttribute.cctor()");
                }
                [ThirdAttribute.Second]
                public ThirdAttribute()
                {
                    Console.WriteLine("    ThirdAttribute.ctor()");
                }
                [ThirdAttribute.Third]
                public ThirdAttribute(int value)
                {
                    Value = (short)value;
                    Console.WriteLine("    ThirdAttribute.ctor(int value)");
                }
                [Second]
                internal ThirdAttribute(int value, long value2)
                {
                    Value = (byte)value;
                    Value2 = value2;
                    Console.WriteLine("    ThirdAttribute.ctor(int value, long value2)");
                }
                public ThirdAttribute(int value, long value2, short value3)
                {
                    Value = (byte)value;
                    Value2 = value2;
                    Value3 = value3;
                    Console.WriteLine("    ThirdAttribute.ctor(int value, long value2, short value3)");
                }
            }

            // Static Constructor
            [Second]
            static FirstAttribute()
            {
                // Extension Methods
                var collection = new float[] { 1, 2, 3 };
                var bl = collection.AsParallel().AsOrdered().Cast<float>().
                    Concat(collection.AsParallel<float>().AsOrdered<float>().Cast<float>()).
                    Contains((long)collection[0]);
                collection.CopyTo(collection, 0);
                Console.WriteLine("    FirstAttribute.cctor()");
            }
            [SecondAttribute.Second]
            public FirstAttribute()
            {
                Console.WriteLine("    FirstAttribute.ctor()");
            }
            [FirstAttribute.SecondAttribute.ThirdAttribute.Second(default(byte))]
            internal FirstAttribute(int value)
            {
                Value = value;
                Console.WriteLine("    FirstAttribute.ctor(int value)");
            }
        }

        [First]
        [FirstAttribute.Second(default(int), Value = 0, Value2 = default(int))]
        [FirstAttribute.SecondAttribute.ThirdAttribute]
        static class ExtensionMethods
        {
            // Static Constructor
            static ExtensionMethods()
            {
                // Extension Methods
                var collection = new long[] { 1, 2, 3 };
                var i = collection.Max() == collection.Min() ?
                    collection.Max<long>((a) => (float)a) : collection.Min<long>((a) => (double)a);
                collection.OfType<short>().OrderBy((a) => a).OrderByDescending((a) => a);
                collection.SequenceEqual(collection);

                Console.WriteLine("    ExtensionMethods.cctor()");
            }
            [First]
            internal static string Stringize([FirstAttribute.SecondAttribute.Third]this s2.s1 s)
            {
                Console.WriteLine("    s2.ExtensionMethods.Stringize(this s2.s1 s)");
                var ss = s.ToString() ?? string.Empty;
                return (s.ToString() == s.ToString(out ss)).ToString();
            }
        }
        [FirstAttribute.Second]
        static class ExtensionMethods2
        {
            // Static Constructor
            static ExtensionMethods2()
            {
                // Extension Methods
                var collection = "string";
                collection.ToLookup((a) => a).LongCount();
                collection.Intersect(collection).Reverse().Skip(4).Single();
                Console.WriteLine("    ExtensionMethods2.cctor()");
            }
            [FirstAttribute.Second]
            public static string ToString([FirstAttribute.SecondAttribute]this s2.s1 s)
            {
                Console.WriteLine("    s2.ExtensionMethods.ToString(this s2.s1 s)");
                var ss = s.ToString() ?? string.Empty;
                return s.ToString(out ss);
            }
        }
        [FirstAttribute.SecondAttribute.Third]
        static class ExtensionMethods3
        {
            // Static Constructor
            static ExtensionMethods3()
            {
                Console.WriteLine("    ExtensionMethods3.cctor()");
            }
            [FirstAttribute.SecondAttribute.Third]
            public static string ToString([FirstAttribute.SecondAttribute]this s2.s1 s, [First(default(char), Value = (int)default(double))] out string ss)
            {
                Console.WriteLine("    s2.ExtensionMethods.ToString(this s2.s1 s, string s2)");
                ss = s.ToString() ?? string.Empty;
                return s.ToString();
            }
        }

        // Static Class
        static class c1<T, U>
        {
            static s1 field = new s1();
            // Static Constructor
            static c1()
            {
                Console.WriteLine("    c1<T, U>.cctor()");
                field._i = 0; field._i = field._i + 1; field = default(s1);
                i3<T> i = new c2<int>();
                var t = default(T); var l = new List<int>(); var s = default(s2);
                var ex = (Exception)new ArgumentException();
                i.method(out t, ref l, out ex, out s);
            }

            internal class c2<V> : i3<T>
            {
                // Explicit Implementation
                void i3<T>.method<UU>(out T x, ref List<UU> y, out Exception e, out s2 s)
                {
                    Console.WriteLine("    c1<V>.i4<T>.method<UU>(out T x, ref List<UU> y, out Exception e, out s2 s)");
                    field = default(s1); s = default(s2); e = (Exception)default(ArgumentException); x = default(T);
                }
                // Implicit Implementation
                public void method<UU>(ref List<T> x, out UU y, out ArgumentException e, out s2 s)
                {
                    Console.WriteLine("    c1<V>.method<UU>(ref List<T> x, out UU y, out ArgumentException e, out s2 s)");
                    field = default(s1); y = default(UU); e = default(ArgumentException); s = default(s2);
                }

                public void method()
                {
                    Console.WriteLine("    c1<V>.method()");
                    var i = ((i3<T>)this); var t = default(T); var s = default(s2);
                    var ex = (Exception)new ArgumentException();
                    var ee = (ArgumentException)ex; var l = new List<T>(); l.Add(t);
                    i.method(ref l, out l, out ee, out s);
                }
            }
        }
    }
}
namespace ns1
{
    public class LowFrequencyTest
    {
        public static void Run()
        {
            lowfrequency.c1<int, long> a = new lowfrequency.c1<int, long>(); a.Test();

            lowfrequency.c2<int, int> b = new lowfrequency.c2<int, int>(); b.Test();
        }
    }

    namespace lowfrequency
    {
        public class c1<T, U>
        {
            // Static Fields
            public static T t = default(T);
            public static U u = default(U);
            public static List<T> l = new List<T>();
            public static Dictionary<List<T>, U> d = new Dictionary<List<T>, U>();

            // Delegates
            public delegate UU Del<TT, UU>(TT x, List<TT> y, Dictionary<List<TT>, UU> z);
            public delegate int Del<TT>(TT x, ArgumentException y, Exception z);
            public delegate int Del(int x, long y, Exception z);

            // Lambda
            Del<T, U> del1 = (x, y, z) =>
            {
                if (y != l) return u;
                else
                {
                    Dictionary<List<T>, U> d1 = d;
                    if (x.Equals(t))
                    {
                        // Nested Lambdas
                        Func<Func<T, List<T>, U, Dictionary<List<T>, U>>,
                             Func<T, List<T>, U, Dictionary<List<T>, U>>> func = (a) => ((T xx, List<T> yy, U zz) => a(xx, yy, zz));
                        // Invoke Lambdas
                        func((T xx, List<T> yy, U zz) => func((aa, bb, cc) => null)(t, l, u))(t, l, u);
                        Console.WriteLine("    c1<T, U>.del1");
                    }
                    return default(U);
                }
            };

            // Lambda
            public Func<T, List<T>, U, Dictionary<List<T>, U>> func = (x, y, z) => d;
            // Anonymous Method
            public Del<U, T> del2 = delegate(U x, List<U> y, Dictionary<List<U>, T> z)
            {
                if (!u.Equals(x)) return t;
                else
                {
                    Dictionary<List<T>, U> d1 = d;
                    if (!l.Equals(y))
                    {
                        // Nested Lambda
                        Func<Func<T, List<T>, U, Dictionary<List<T>, U>>,
                             Func<T, List<T>, U, Dictionary<List<T>, U>>> func = (Func<T, List<T>, U, Dictionary<List<T>, U>> a) =>
                             {
                                 // Nested Anonymous Method
                                 return delegate(T xx, List<T> yy, U zz)
                                 {
                                     Console.WriteLine("    c1<T, U>.del2");
                                     return d1;
                                 };
                             };
                        // Invoke Lambdas
                        func((xx, yy, zz) => func((T aa, List<T> bb, U cc) => null)(t, l, u))(t, l, u);
                    }
                    return default(T);
                }
            };

            // Generic Method
            protected void goo<TT, UU, VV>(Func<TT, UU, VV> x, Func<UU, VV, TT> y, Func<VV, TT, UU> z)
            {
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(Func<TT, UU, VV> x, Func<UU, VV, TT> y, Func<VV, TT, UU> z)");
                TT t = default(TT); UU u = default(UU); VV v = default(VV);

                // Invoke Lambdas
                z(v, y(u, x(t, u)));
            }

            // Generic Method
            protected void goo<TT, UU, VV>(TT xx, UU yy, VV zz)
            {
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(TT xx, UU yy, VV zz)");
            }

            // Generic Method
            protected void goo<TT, UU, VV>(Func<TT, List<TT>, UU, Dictionary<List<TT>, UU>> x, Del<UU, VV> y, Action<VV, List<VV>, Dictionary<List<VV>, TT>> z)
            {
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(Func<TT, List<TT>, Dictionary<List<TT>, UU>> x, Del<UU, VV> y, Action<VV, List<VV>, Dictionary<List<VV>, TT>> z)");
                TT t = default(TT); UU u = default(UU); VV v = default(VV);

                // Invoke Lambdas
                x(t, new List<TT>(), u);
                y(u, new List<UU>(), new Dictionary<List<UU>, VV>());
                z(v, new List<VV>(), new Dictionary<List<VV>, TT>());
            }

            // Generic Method
            private void bar<TT, UU, VV>()
            {
                Console.WriteLine("    c1<T, U>.bar<TT, UU, VV>()");
                TT tt = default(TT); UU uu = default(UU); VV vv = default(VV);
                T t = default(T); U u = default(U); List<TT> ltt = new List<TT>();

                // 5 Levels Deep Nested Lambda, Closures
                Func<TT, UU, Func<UU, VV, Func<VV, TT, Func<T, U, Func<U, T>>>>> func =
                    (a, b) =>
                    {
                        Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level1()");
                        bool v1 = tt.Equals(a);
                        return (aa, bb) =>
                        {
                            Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level2()");
                            bool v2 = v1;
                            if (ltt.Count >= 0)
                            {
                                Dictionary<T, List<U>> dtu = new Dictionary<T, List<U>>();
                                v2 = aa.Equals(b); aa.Equals(uu);
                                return (aaa, bbb) =>
                                {
                                    Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level3()");
                                    bool v3 = v1;
                                    if (dtu.Count == 0)
                                    {
                                        v3 = v2;
                                        Dictionary<List<UU>, List<VV>> duuvv = new Dictionary<List<UU>, List<VV>>();
                                        if (ltt.Count >= 0)
                                        {
                                            v3 = aaa.Equals(bb);
                                            v2 = aa.Equals(b);
                                            aaa.Equals(vv);
                                            return (aaaa, bbbb) =>
                                            {
                                                Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level4()");
                                                List<U> lu = new List<U>();
                                                bool v4 = v3; v4 = v2; v4 = v1;
                                                if (duuvv.Count > 0)
                                                {
                                                    Console.WriteLine("Error - Should not have reached here");
                                                    return null;
                                                }
                                                else
                                                {
                                                    v4 = aaaa.Equals(t);
                                                    v3 = aaa.Equals(bb);
                                                    v2 = aa.Equals(b);
                                                    return (aaaaa) =>
                                                    {
                                                        Console.WriteLine("        c1<T, U>.bar<TT, UU, VV>.func.level5()");
                                                        if (lu.Count < 0)
                                                        {
                                                            Console.WriteLine("Error - Should not have reached here");
                                                            return t;
                                                        }
                                                        else
                                                        {
                                                            v4 = v3 = v2 = v1;
                                                            u.Equals(bbbb);
                                                            aa.Equals(b);
                                                            aaa.Equals(bb);
                                                            aaaa.Equals(t);
                                                            return aaaa;
                                                        }
                                                    };
                                                }
                                            };
                                        }
                                        else
                                        {
                                            Console.WriteLine("Error - Should not have reached here");
                                            return null;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error - Should not have reached here");
                                        return null;
                                    }
                                };
                            }
                            else
                            {
                                Console.WriteLine("Error - Should not have reached here");
                                return null;
                            }
                        };
                    };
                func(tt, uu)(uu, vv)(vv, tt)(t, u)(u);
            }

            public void goo<TT, UU, VV>(Func<TT, UU> x, Func<TT, VV> y, Func<UU, VV> z, Func<UU, TT> a, Func<VV, TT> b, Func<VV, UU> c)
            {
                Console.WriteLine("    c1<T, U>.goo<TT, UU, VV>(Func<TT, UU> x, Func<TT, VV> y, Func<UU, VV> z, Func<UU, TT> a, Func<VV, TT> b, Func<VV, UU> c)");
            }

            public void Test()
            {
                Console.WriteLine("c1<T, U>.Test()");
                func(t, l, u);
                del1(t, l, d);
                del2(u, null, null);

                int x = 0; int y = x;
                Del del = (a, b, c) => a;

                // Generic Methods, Simple Closures
                goo<int, string, Del<T, U>>(y, x.ToString(), (a, b, c) => u);
                goo<string, Del<T>, int>(x.ToString(), (a, b, c) => x, y);
                goo<Del, int, string>((c, a) => a.ToString(),
                    (int a, string b) => del,
                    (b, c) => y - c(x, y, null));

                // Generic Type Inference, Nested Lambdas
                goo(x, "", del1);
                goo(func, del2,
                    (T a, List<T> b, Dictionary<List<T>, T> c) =>
                    {
                        int z = x;
                        {
                            goo(new Action(() => x = 2),
                                new Func<ArgumentException, int>((aa) =>
                                {
                                    return y + x + +z + b.Count;
                                }),
                                new Func<Exception, long>(delegate(Exception aa)
                                {
                                    return y * z * x - c.Count;
                                }));
                        }
                        x = z;
                        {
                            goo((aa, bb) => a.ToString(),
                                (long bb, string cc) => y - (int)bb + b.Count - z,
                                (string cc, int aa) => x + y + aa + c.Values.Count);
                        }
                    });

                // Generic Type Inference, Dominant Type
                goo((Exception a, Exception b) => new ArgumentException(),
                    (Exception a, Exception b) => new ArgumentException(),
                    (Exception a, Exception b) => new ArgumentException());
                goo((a, b) => new ArgumentException(),
                    (a, b) => new ArgumentException(),
                    (ArgumentException a, Exception b) => new Exception());
                Func<Exception, ArgumentException> func2 = (Exception a) => new ArgumentException();
                Func<ArgumentException, Exception> func3 = (ArgumentException a) => new ArgumentException();
                goo(func2, func2, func2, func2, func3, func3);
                goo((a) => new ArgumentException(),
                    (Exception a) => new InvalidCastException(),
                    (a) => new InvalidCastException(),
                    (ArgumentException a) => new Exception(),
                    (a) => new ArgumentException(),
                    (InvalidCastException a) => new ArgumentException());
                goo((Exception a) => new Exception(),
                    (Exception a) => new ArgumentException(),
                    (Exception a) => new ArgumentException(),
                    (Exception a) => new Exception(),
                    (Exception a) => new ArgumentException(),
                    (Exception a) => new Exception());
                goo((a) => new Exception(),
                    (Exception a) => new ArgumentException(),
                    (a) => new ArgumentException(),
                    (a) => new Exception(),
                    (a) => new ArgumentException(),
                    (a) => new Exception());

                bar<int, long, double>();
            }
        }

        public class c2<T, U> : c1<U, T>
        {
            // Delegates
            public delegate Exception Del1(T x, U y, InvalidCastException z, ArgumentException w);
            public delegate void Del2<TT, UU, VV>(Func<TT, UU, VV> x, Func<UU, VV, TT> y, Func<VV, TT, UU> z);
            protected delegate void Del3<TT, UU, VV>(TT xx, UU yy, VV zz);
            protected delegate WW Del3<TT, UU, VV, WW>(TT xx, UU yy, VV zz);
            protected delegate void Del4<TT, UU, VV>(Func<TT, List<TT>, UU, Dictionary<List<TT>, UU>> x, Del<UU, VV> y, Action<VV, List<VV>, Dictionary<List<VV>, TT>> z);

            private void bar<TT, UU, VV>()
            {
                Console.WriteLine("    c2<T, U>.bar<TT, UU, VV>()");
                TT tt = default(TT); UU uu = default(UU); VV vv = default(VV);
                T t = default(T); U u = default(U);

                // Delegate Binding, Compound Assignment
                Del2<TT, UU, VV> d2 = goo; d2 += goo<TT, UU, VV>; d2 -= goo;
                Del3<TT, VV, UU> d3 = goo; d3 += goo<TT, VV, UU>; d3 -= goo;
                Del4<UU, TT, VV> d4 = goo; d4 += goo<UU, TT, VV>; d4 -= goo;
                // Invoke Delegates
                d2((a, b) => vv, (b, c) => tt, (c, a) => uu);
                d3(tt, vv, uu);
                d4((a, b, c) => null, (a, b, c) => vv, (a, b, c) => { uu.Equals(vv); });

                // Delegate Binding, Compound Assignment
                Del2<int, Del, VV> d22 = goo; d22 += (goo); d22 -= goo<int, Del, VV>;
                Del3<long, int, Exception> d32 = goo; d32 += goo<long, int, Exception>; d32 -= ((goo));
                Del4<T, U, Dictionary<List<TT>, Dictionary<List<UU>, VV>>> d42 = goo; d42 += goo; d42 -= goo;
                // Invoke Delegates
                d22((a, b) => vv, (b, c) => 1, (c, a) => null);
                d32(1, 0, null);
                d42((a, b, c) => null, (a, b, c) => null, (a, b, c) => { uu.Equals(vv); });

                // Delegate Relaxation, Compound Assignment
                Del1 d1 = goo; d1 += goo<T, U>;
                Del3<InvalidCastException, ArgumentNullException, NullReferenceException, Exception> d33 = goo<int, long>;
                d33 -= goo<int, long>; d33 += goo<int, double>;
                // Invoke Delegates
                d1(t, u, null, null);
                d33(new InvalidCastException(), new ArgumentNullException(), new NullReferenceException());

                // Delegate Relaxation, Generic Methods
                goo<ArgumentException, ArgumentException, Exception>(goo<int>, goo<long>, goo<double>);
                goo<ArgumentException, ArgumentException, Exception>(goo<Exception, ArgumentException>,
                                                                     goo<Exception, ArgumentException>,
                                                                     goo<Exception, ArgumentException>);
                goo<ArgumentException, ArgumentException, Exception>(bar, bar, bar);
            }

            private ArgumentException goo<TT, UU>(Exception x, Exception y, Exception z)
            {
                Console.WriteLine("    c2<T, U>.goo<TT, UU>(Exception x, Exception y, Exception z)");
                return null;
            }

            private ArgumentException goo<TT, UU>(TT x, UU y, Exception a, Exception b)
            {
                Console.WriteLine("    c2<T, U>.goo<TT, UU>(TT x, UU y, Exception a, Exception b)");
                return null;
            }

            private ArgumentException goo<TT>(Exception a, Exception b)
            {
                Console.WriteLine("    c2<T, U>.goo<TT>(Exception a, Exception b)");
                return null;
            }

            private ArgumentException bar(Exception a, Exception b)
            {
                Console.WriteLine("    c2<T, U>.bar(Exception a, Exception b)");
                return null;
            }

            private UU goo<TT, UU>(TT a, TT b)
            {
                Console.WriteLine("    c2<T, U>.goo<TT, UU>(TT a, TT b)");
                return default(UU);
            }

            public new void Test()
            {
                Console.WriteLine("c2<T, U>.Test()");
                bar<int, long, double>();
            }
        }
    }
}

