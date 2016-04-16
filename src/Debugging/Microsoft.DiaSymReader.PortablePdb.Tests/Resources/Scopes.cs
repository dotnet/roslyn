#line 1 "C:\Scopes.cs"
#pragma checksum "C:\Scopes.cs" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DBEB2A067B2F0E0D678A002C587A2806056C3DCE"
#pragma warning disable 219 // unused const

using System;
using System.Collections.Generic;

class X { }

public class C<S>
{
    enum EnumI1 : sbyte { A = 1 }
    enum EnumU1 : byte { A = 2 }
    enum EnumI2 : short { A = 3 }
    enum EnumU2 : ushort { A = 4 }
    enum EnumI4 : int { A = 5 }
    enum EnumU4 : uint { A = 6 }
    enum EnumI8 : long { A = 7 }
    enum EnumU8 : ulong { A = 8 }

    public static void F<T>()
    {
        const bool B = false;
        const char C = '\0';
        const sbyte I1 = 1;
        const byte U1 = 2;
        const short I2 = 3;
        const ushort U2 = 4;
        const int I4 = 5;
        const uint U4 = 6;
        const long I8 = 7;
        const ulong U8 = 8;
        const float R4 = (float)9.1;
        const double R8 = 10.2;

        const C<int>.EnumI1 EI1 = C<int>.EnumI1.A;
        const C<int>.EnumU1 EU1 = C<int>.EnumU1.A;
        const C<int>.EnumI2 EI2 = C<int>.EnumI2.A;
        const C<int>.EnumU2 EU2 = C<int>.EnumU2.A;
        const C<int>.EnumI4 EI4 = C<int>.EnumI4.A;
        const C<int>.EnumU4 EU4 = C<int>.EnumU4.A;
        const C<int>.EnumI8 EI8 = C<int>.EnumI8.A;
        const C<int>.EnumU8 EU8 = C<int>.EnumU8.A;

        const string StrWithNul = "\0";
        const string EmptyStr = "";
        const string NullStr = null;
        const object NullObject = null;
        const dynamic NullDynamic = null;
        const X NullTypeDef = null;
        const Action NullTypeRef = null;
        const Func<Dictionary<int, C<int>>, dynamic, T, List<S>> NullTypeSpec = null;

        const decimal D = 123456.78M;
    }

    public static void NestedScopes()
    {
        int x0 = 0;
        {
            const int c1 = 11;
            int x1 = 1;
        }

        int y0 = 0;
        {
            int y1 = 1;
            {
                const string c2 = nameof(c2);
                const string d2 = nameof(d2);
                int y2 = 2;
            }
        }
    }
}