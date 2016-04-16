using System;

public class LargeMethodTest
{
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

        internal int foo(int x)
        {
            return 0;
        }
        public bool foo(object x)
        {
            return false;
        }

        // Overridden Abstract Methods
        public override int abst(ref string x, params int[] y)
        {
            Console.WriteLine("    c1.abst(ref string, params int[])");
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
    }

    public class c2<T> : c1 
    {
    }

    public class c4 : c2<string> 
    {
        public static bool b = true;
        public static byte b1 = 0;
        public static sbyte sb = 1;

        private static short s = 4;
        private static ushort us = 5;
        private static long l = 6;
        private static ulong ul = 7;
    }

    public class c5
    {
        internal static float f = 8.0f;
        internal static double d = 9.0;
        internal static string s1 = "Test";
        internal static object o = null;

        public object bar(object arg)
        {
            return null;
        }

        public object foo(object arg)
        {
            return null;
        }
    }

    public object bar(object arg)
    {
        return null;
    }

    public object foo(object arg)
    {
        return null;
    }

    public bool LargeMethod()
    {
        string str = "c4.Test()";
        {
            int i = 2;
            Console.WriteLine(str);
            {
                c1 a = new c1(i); a.foo(i);
            }
            double d = 1.1;
            {
                sbyte sb = 1;
                c1 a = new c1(i + (i + i));
                a.foo(sb);
                {
                    a.foo(d);
                }
            }

            // Nested Scopes
            {
                object o = i;
                bool b = false;
                if (!b)
                {
                    byte b1 = 1;
                    string s = "    This is a test";
                    while (!b)
                    {
                        if (true) b = true;
                        Console.WriteLine(s);
                        while (b)
                        {
                            if (true) b = false;
                            object oo = i;
                            bool bb = b;
                            if (!bb)
                            {
                                if (!false) bb = true;
                                byte b11 = b1;
                                string ss = s;
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
                                                c.abst(ref s, 1, i);
                                                c.abst(ref s, new int[] { 1, i, i });
                                                c.abst(ref s, c.abst(ref s, l, l), l, l, l);

                                                // Method Calls - Ref, Paramarrays
                                                // Overloaded Virtual Methods
                                                c1 a = new c4();
                                                c.virt(i, c, new c2<string>[] { c.virt(i, ref a), new c4() });
                                                c.virt(c.virt(i, ref a), c.virt(ref i, c, c.virt(i, ref a)));
                                                c.virt(c.abst(ref s, l, l), c.abst(ref s, new long[] { 1, i, l }));
                                                c.virt(i, ref a);
                                                c.virt(ref i, new c4(), new c4(), new c2<string>());
                                                c.virt(new int[] { 1, 2, 3 });
                                                c.virt(new Exception[] { });
                                                c.virt(new c1[] { new c4(), new c2<string>() });

                                                if (true) continue;
                                            }
                                        }
                                        else if (bbb != true)
                                        {
                                            Console.WriteLine("Error - Should not have reached here");
                                            o = i;
                                            return (bool)o;
                                        }
                                        else if (bbb == false)
                                        {
                                            Console.WriteLine("Error - Should not have reached here");
                                            o = i;
                                            return (bool)o;

                                        }
                                        else
                                        {
                                            Console.WriteLine("Error - Should not have reached here");
                                            o = b;
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

        int us = 0;
        short sh = 1; 
        c5 cc = new c5();
        Console.WriteLine("c5.test");
        {
            uint ui = 1; object o = ui;
            int i = sh; bool b = false; us = 1;
            // Nested Scopes
            if (true)
            {
                byte b1 = 1; long l = i; string s1 = ""; string s = "" ;
                float f = 1.2f; o = f; l = ui;
                c4 c = new c4();
                c.foo(sh); this.bar(sh);
                if (b == false)
                {
                    double d = f; s1 = s;
                }
                if (b1 >= l)
                {
                    uint ui1 = 1; o = ui1;
                    i = sh; b = false; us = 1;
                    while (i != 1000)
                    {
                        byte b11 = 1; long l1 = i; string s11 = s1;
                        float f1 = 1.2f; o = f1; l1 = ui1;
                        c.foo(b);
                        b11 = (byte)l1;

                        if (!false)
                        {
                            double d1 = f1; s1 = s;
                            c.foo(b1);
                        }
                        if (i != 1000)
                        {
                            uint ui2 = 1; o = ui2;
                            i = sh; b = false; us = 1;
                            {
                                long l2 = i; string s12 = s11;
                                o = f1; l2 = ui1;
                                if (i <= 1000) break;
                            }
                        }
                        if (i <= 1000) i = 1000;
                        return false;
                    }
                }
            }
        }
        return us == 0;


    }
}