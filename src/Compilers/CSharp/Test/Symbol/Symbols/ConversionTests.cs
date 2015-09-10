// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;
using System;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class ConversionTests : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var mscorlibRef = TestReferences.NetFx.v4_0_21006.mscorlib;
            var compilation = CSharpCompilation.Create("Test", references: new MetadataReference[] { mscorlibRef });
            var sys = compilation.GlobalNamespace.ChildNamespace("System");
            Conversions c = new BuckStopsHereBinder(compilation).Conversions;
            var types = new TypeSymbol[]
            {
            sys.ChildType("Object"),
            sys.ChildType("String"),
            sys.ChildType("Array"),
            sys.ChildType("Int64"),
            sys.ChildType("UInt64"),
            sys.ChildType("Int32"),
            sys.ChildType("UInt32"),
            sys.ChildType("Int16"),
            sys.ChildType("UInt16"),
            sys.ChildType("SByte"),
            sys.ChildType("Byte"),
            sys.ChildType("Double"),
            sys.ChildType("Single"),
            sys.ChildType("Decimal"),
            sys.ChildType("Char"),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Int64")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("UInt64")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Int32")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("UInt32")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Int16")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("UInt16")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("SByte")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Byte")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Double")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Single")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Decimal")),
            sys.ChildType("Nullable", 1).Construct(sys.ChildType("Char")),
            sys.ChildType("Exception"),
            sys.ChildNamespace("Collections").ChildType("IEnumerable"),
            sys.ChildNamespace("Collections").ChildNamespace("Generic").ChildType("IEnumerable", 1).Construct(sys.ChildType("Object")),
            sys.ChildNamespace("Collections").ChildNamespace("Generic").ChildType("IEnumerable", 1).Construct(sys.ChildType("String")),
            sys.ChildNamespace("Collections").ChildNamespace("Generic").ChildType("IEnumerable", 1).Construct(sys.ChildType("Char")),
            compilation.CreateArrayTypeSymbol(sys.ChildType("String")),
            compilation.CreateArrayTypeSymbol(sys.ChildType("Object")),
            sys.ChildNamespace("Collections").ChildNamespace("Generic").ChildType("IList", 1).Construct(sys.ChildType("String")),
            sys.ChildNamespace("Collections").ChildNamespace("Generic").ChildType("IList", 1).Construct(sys.ChildType("Object")),
            sys.ChildType("ArgumentException"),
            sys.ChildType("Delegate"),
            sys.ChildType("Func", 2).Construct(sys.ChildType("Exception"), sys.ChildType("Exception")),
            sys.ChildType("Func", 2).Construct(sys.ChildType("ArgumentException"), sys.ChildType("Object")),
            sys.ChildNamespace("Runtime").ChildNamespace("Serialization").ChildType("ISerializable"),
            sys.ChildType("IComparable", 0),
            };

            const ConversionKind Non = ConversionKind.NoConversion;
            const ConversionKind Idn = ConversionKind.Identity;
            const ConversionKind Inm = ConversionKind.ImplicitNumeric;
            const ConversionKind Inl = ConversionKind.ImplicitNullable;
            const ConversionKind Irf = ConversionKind.ImplicitReference;
            const ConversionKind Box = ConversionKind.Boxing;
            const ConversionKind Xrf = ConversionKind.ExplicitReference;
            const ConversionKind Ubx = ConversionKind.Unboxing;
            const ConversionKind Xnl = ConversionKind.ExplicitNullable;
            const ConversionKind Xnm = ConversionKind.ExplicitNumeric;

            ConversionKind[,] conversions =
            {
                // from   obj  str  arr  i64  u64  i32  u32  i16  u16  i08  u08  r64  r32  dec  chr ni64 nu64 ni32 nu32 ni16 nu16  ni8  nu8 nr64 nr32  ndc  nch  exc  ien  ieo  ies  iec  ars  aro  ils  ilo  aex  del  fee  fao  ser  cmp
                // to:    
                /*obj*/ { Idn, Irf, Irf, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Irf },
                /*str*/
                        { Xrf, Idn, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Non, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf },
                /*arr*/
                        { Xrf, Non, Idn, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Xrf, Irf, Irf, Xrf, Xrf, Non, Non, Non, Non, Xrf, Xrf },
                /*i64*/
                        { Ubx, Non, Non, Idn, Xnm, Inm, Inm, Inm, Inm, Inm, Inm, Xnm, Xnm, Xnm, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*u64*/
                        { Ubx, Non, Non, Xnm, Idn, Xnm, Inm, Xnm, Inm, Xnm, Inm, Xnm, Xnm, Xnm, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*i32*/
                        { Ubx, Non, Non, Xnm, Xnm, Idn, Xnm, Inm, Inm, Inm, Inm, Xnm, Xnm, Xnm, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*u32*/
                        { Ubx, Non, Non, Xnm, Xnm, Xnm, Idn, Xnm, Inm, Xnm, Inm, Xnm, Xnm, Xnm, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*i16*/
                        { Ubx, Non, Non, Xnm, Xnm, Xnm, Xnm, Idn, Xnm, Inm, Inm, Xnm, Xnm, Xnm, Xnm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*u16*/
                        { Ubx, Non, Non, Xnm, Xnm, Xnm, Xnm, Xnm, Idn, Xnm, Inm, Xnm, Xnm, Xnm, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*i08*/
                        { Ubx, Non, Non, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Idn, Xnm, Xnm, Xnm, Xnm, Xnm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*u08*/
                        { Ubx, Non, Non, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Idn, Xnm, Xnm, Xnm, Xnm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*r64*/
                        { Ubx, Non, Non, Inm, Inm, Inm, Inm, Inm, Inm, Inm, Inm, Idn, Inm, Xnm, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*r32*/
                        { Ubx, Non, Non, Inm, Inm, Inm, Inm, Inm, Inm, Inm, Inm, Xnm, Idn, Xnm, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*dec*/
                        { Ubx, Non, Non, Inm, Inm, Inm, Inm, Inm, Inm, Inm, Inm, Xnm, Xnm, Idn, Inm, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*chr*/
                        { Ubx, Non, Non, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Xnm, Idn, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*ni64*/
                        { Ubx, Non, Non, Inl, Xnl, Inl, Inl, Inl, Inl, Inl, Inl, Xnl, Xnl, Xnl, Inl, Idn, Xnl, Inl, Inl, Inl, Inl, Inl, Inl, Xnl, Xnl, Xnl, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*nu64*/
                        { Ubx, Non, Non, Xnl, Inl, Xnl, Inl, Xnl, Inl, Xnl, Inl, Xnl, Xnl, Xnl, Inl, Xnl, Idn, Xnl, Inl, Xnl, Inl, Xnl, Inl, Xnl, Xnl, Xnl, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*ni32*/
                        { Ubx, Non, Non, Xnl, Xnl, Inl, Xnl, Inl, Inl, Inl, Inl, Xnl, Xnl, Xnl, Inl, Xnl, Xnl, Idn, Xnl, Inl, Inl, Inl, Inl, Xnl, Xnl, Xnl, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*nu32*/
                        { Ubx, Non, Non, Xnl, Xnl, Xnl, Inl, Xnl, Inl, Xnl, Inl, Xnl, Xnl, Xnl, Inl, Xnl, Xnl, Xnl, Idn, Xnl, Inl, Xnl, Inl, Xnl, Xnl, Xnl, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*ni16*/
                        { Ubx, Non, Non, Xnl, Xnl, Xnl, Xnl, Inl, Xnl, Inl, Inl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Idn, Xnl, Inl, Inl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*nu16*/
                        { Ubx, Non, Non, Xnl, Xnl, Xnl, Xnl, Xnl, Inl, Xnl, Inl, Xnl, Xnl, Xnl, Inl, Xnl, Xnl, Xnl, Xnl, Xnl, Idn, Xnl, Inl, Xnl, Xnl, Xnl, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*ni8*/
                        { Ubx, Non, Non, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Inl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Idn, Xnl, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*nu8*/
                        { Ubx, Non, Non, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Inl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Idn, Xnl, Xnl, Xnl, Xnl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*nr64*/
                        { Ubx, Non, Non, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Xnl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Idn, Inl, Xnl, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*nr32*/
                        { Ubx, Non, Non, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Xnl, Inl, Xnl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Xnl, Idn, Xnl, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*ndc*/
                        { Ubx, Non, Non, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Xnl, Xnl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Inl, Xnl, Xnl, Idn, Inl, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*nch*/
                        { Ubx, Non, Non, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Inl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Xnl, Idn, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Ubx },
                /*exc*/
                        { Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Idn, Xrf, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf, Irf, Non, Non, Non, Xrf, Xrf },
                /*ien*/
                        { Xrf, Irf, Irf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Idn, Irf, Irf, Irf, Irf, Irf, Irf, Irf, Xrf, Xrf, Non, Non, Xrf, Xrf },
                /*ieo*/
                        { Xrf, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Idn, Irf, Xrf, Irf, Irf, Irf, Irf, Xrf, Xrf, Non, Non, Xrf, Xrf },
                /*ies*/
                        { Xrf, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Idn, Xrf, Irf, Xrf, Irf, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf },
                /*iec*/
                        { Xrf, Irf, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Xrf, Idn, Non, Non, Xrf, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf },
                /*ars*/
                        { Xrf, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Non, Idn, Xrf, Xrf, Xrf, Non, Non, Non, Non, Non, Non },
                /*aro*/
                        { Xrf, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Non, Irf, Idn, Xrf, Xrf, Non, Non, Non, Non, Non, Non },
                /*ils*/
                        { Xrf, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Xrf, Xrf, Irf, Xrf, Idn, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf },
                /*ilo*/
                        { Xrf, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Xrf, Xrf, Irf, Irf, Xrf, Idn, Xrf, Xrf, Non, Non, Xrf, Xrf },
                /*aex*/
                        { Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf, Idn, Non, Non, Non, Xrf, Xrf },
                /*del*/
                        { Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf, Non, Idn, Irf, Irf, Xrf, Xrf },
                /*fee*/
                        { Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Idn, Xrf, Xrf, Non },
                /*fao*/
                        { Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Xrf, Irf, Idn, Xrf, Non },
                /*ser*/
                        { Xrf, Non, Xrf, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Non, Irf, Xrf, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf, Irf, Irf, Irf, Irf, Idn, Xrf },
                /*cmp*/
                        { Xrf, Irf, Xrf, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Box, Xrf, Xrf, Xrf, Xrf, Xrf, Non, Non, Xrf, Xrf, Xrf, Xrf, Non, Non, Xrf, Idn },
            };

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            for (int i = 0; i < types.Length; ++i)
            {
                for (int j = 0; j < types.Length; ++j)
                {
                    var kind = conversions[i, j];
                    var result = c.ClassifyConversion(types[j], types[i], ref useSiteDiagnostics);
                    //Assert.Equal doesn't allow a string explanation, so provide one this way.
                    if (kind != result.Kind)
                    {
                        var result2 = c.ClassifyConversion(types[j], types[i], ref useSiteDiagnostics); // set breakpoint here if this test is failing...
                        Assert.True(false, string.Format("Expected {0} but got {1} when converting {2} -> {3}", kind, result, types[j], types[i]));
                    }
                }
            }

            // UNDONE: Not tested yet:
            // UNDONE: Type parameter reference, boxing and unboxing conversions
            // UNDONE: User-defined conversions
            // UNDONE: Dynamic conversions
            // UNDONE: Enum conversions
            // UNDONE: Conversions involving expressions: null, lambda, method group
        }


        [Fact]
        public void TestIsSameTypeIgnoringDynamic()
        {
            string code = @" 
class O<T>
{
    public class I<U,V>
    {
        static O<object>.I<U,V> g1;
        static O<dynamic>.I<U,V> g2;
    }
}

class X {
    object f1;
    dynamic f2;
    object[] f3;
    dynamic[] f4;
    object[,] f5;
    O<object>.I<int, object> f6;
    O<dynamic>.I<int, object> f7;
    O<object>.I<int, dynamic> f8;
    O<string> f9;
    O<dynamic> f10;
}
";
            var mscorlibRef = TestReferences.NetFx.v4_0_21006.mscorlib;
            var compilation = CSharpCompilation.Create("Test", new[] { Parse(code) }, new[] { mscorlibRef });
            var global = compilation.GlobalNamespace;

            var classX = global.ChildType("X");
            var classI = (NamedTypeSymbol)(global.ChildType("O").ChildSymbol("I"));
            var f1Type = ((FieldSymbol)(classX.ChildSymbol("f1"))).Type;
            var f2Type = ((FieldSymbol)(classX.ChildSymbol("f2"))).Type;
            var f3Type = ((FieldSymbol)(classX.ChildSymbol("f3"))).Type;
            var f4Type = ((FieldSymbol)(classX.ChildSymbol("f4"))).Type;
            var f5Type = ((FieldSymbol)(classX.ChildSymbol("f5"))).Type;
            var f6Type = ((FieldSymbol)(classX.ChildSymbol("f6"))).Type;
            var f7Type = ((FieldSymbol)(classX.ChildSymbol("f7"))).Type;
            var f8Type = ((FieldSymbol)(classX.ChildSymbol("f8"))).Type;
            var f9Type = ((FieldSymbol)(classX.ChildSymbol("f9"))).Type;
            var f10Type = ((FieldSymbol)(classX.ChildSymbol("f10"))).Type;
            var g1Type = ((FieldSymbol)(classI.ChildSymbol("g1"))).Type;
            var g2Type = ((FieldSymbol)(classI.ChildSymbol("g2"))).Type;
            string s = f7Type.ToTestDisplayString();

            Assert.False(f1Type.Equals(f2Type));
            Assert.True(f1Type.Equals(f2Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f2Type.Equals(f1Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f1Type.Equals(f1Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f2Type.Equals(f2Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));

            Assert.False(f3Type.Equals(f4Type));
            Assert.True(f3Type.Equals(f4Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f4Type.Equals(f3Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.False(f4Type.Equals(f5Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.False(f5Type.Equals(f4Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));

            Assert.False(f6Type.Equals(f7Type));
            Assert.False(f6Type.Equals(f8Type));
            Assert.False(f7Type.Equals(f8Type));
            Assert.True(f6Type.Equals(f7Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f7Type.Equals(f6Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f6Type.Equals(f6Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f7Type.Equals(f7Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f8Type.Equals(f7Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f7Type.Equals(f8Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f8Type.Equals(f8Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f7Type.Equals(f7Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f8Type.Equals(f6Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f6Type.Equals(f8Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f8Type.Equals(f8Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(f6Type.Equals(f6Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));

            Assert.False(f9Type.Equals(f10Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.False(f10Type.Equals(f9Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));

            Assert.False(g1Type.Equals(g2Type));
            Assert.True(g1Type.Equals(g2Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(g2Type.Equals(g1Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(g1Type.Equals(g1Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
            Assert.True(g2Type.Equals(g2Type, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));
        }

        /// <summary>
        /// ClassifyConversions should ignore custom modifiers: converting between a type and the same type
        /// with different custom modifiers should be an identity conversion.
        /// </summary>
        [Fact]
        public void TestConversionsWithCustomModifiers()
        {
            var text = @"
class C
{
    int[] a;
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            var compilation = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            compilation.VerifyDiagnostics(
                // (4,11): warning CS0169: The field 'C.a' is never used
                //     int[] a;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("C.a")
                );

            var classC = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var typeIntArray = classC.GetMember<FieldSymbol>("a").Type;

            var interfaceI3 = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I3");
            var typeIntArrayWithCustomModifiers = interfaceI3.GetMember<MethodSymbol>("M1").Parameters.Single().Type;

            Assert.True(typeIntArrayWithCustomModifiers.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds:false));

            var conv = new BuckStopsHereBinder(compilation).Conversions;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            // no custom modifiers to custom modifiers
            Assert.Equal(ConversionKind.Identity, conv.ClassifyConversion(typeIntArray, typeIntArrayWithCustomModifiers, ref useSiteDiagnostics).Kind);

            // custom modifiers to no custom modifiers
            Assert.Equal(ConversionKind.Identity, conv.ClassifyConversion(typeIntArrayWithCustomModifiers, typeIntArray, ref useSiteDiagnostics).Kind);

            // custom modifiers to custom modifiers
            Assert.Equal(ConversionKind.Identity, conv.ClassifyConversion(typeIntArrayWithCustomModifiers, typeIntArrayWithCustomModifiers, ref useSiteDiagnostics).Kind);
        }

        [WorkItem(529056, "DevDiv")]
        [WorkItem(529056, "DevDiv")]
        [Fact()]
        public void TestConversion_ParenthesizedExpression()
        {
            var source = @"
using System;

public class Program
{
    public static bool Eval(object obj1, object obj2)
    {
        if (/*<bind>*/(obj1 != null)/*</bind>*/ && (obj2 != null))
        {
            return true;
        }
        return false;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var tuple = GetBindingNodeAndModel<ExpressionSyntax>(comp);
            Assert.Equal(ConversionKind.Identity, tuple.Item2.ClassifyConversion(tuple.Item1, comp.GetSpecialType(SpecialType.System_Boolean)).Kind);
        }

        [WorkItem(544571, "DevDiv")]
        [Fact]
        public void TestClassifyConversion()
        {
            var source = @"
using System;
class Program
{
    static void M()
    {
    }
    static void M(long l)
    {
    }
    static void M(short s)
    {
    }
    static void M(int i)
    {
    }
    static void Main()
    {
        int ii = 0;
        Console.WriteLine(ii);
        short jj = 1;
        Console.WriteLine(jj);
        string ss = string.Empty;
        Console.WriteLine(ss);
 
       // Perform conversion classification here.
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(MscorlibRef)
                .AddSyntaxTrees(tree);

            var model = compilation.GetSemanticModel(tree);

            // Get VariableDeclaratorSyntax corresponding to variable 'ii' above.
            var variableDeclarator = (VariableDeclaratorSyntax)tree.GetCompilationUnitRoot()
                .FindToken(source.IndexOf("ii", StringComparison.Ordinal)).Parent;

            // Get TypeSymbol corresponding to above VariableDeclaratorSyntax.
            TypeSymbol targetType = ((LocalSymbol)model.GetDeclaredSymbol(variableDeclarator)).Type;

            // Perform ClassifyConversion for expressions from within the above SyntaxTree.
            var sourceExpression1 = (ExpressionSyntax)tree.GetCompilationUnitRoot()
                .FindToken(source.IndexOf("jj)", StringComparison.Ordinal)).Parent;
            Conversion conversion = model.ClassifyConversion(sourceExpression1, targetType);
            Assert.True(conversion.IsImplicit);
            Assert.True(conversion.IsNumeric);

            var sourceExpression2 = (ExpressionSyntax)tree.GetCompilationUnitRoot()
                .FindToken(source.IndexOf("ss)", StringComparison.Ordinal)).Parent;
            conversion = model.ClassifyConversion(sourceExpression2, targetType);
            Assert.False(conversion.Exists);

            // Perform ClassifyConversion for constructed expressions
            // at the position identified by the comment '// Perform ...' above.
            ExpressionSyntax sourceExpression3 = SyntaxFactory.IdentifierName("jj");
            var position = source.IndexOf("//", StringComparison.Ordinal);
            conversion = model.ClassifyConversion(position, sourceExpression3, targetType);
            Assert.True(conversion.IsImplicit);
            Assert.True(conversion.IsNumeric);

            ExpressionSyntax sourceExpression4 = SyntaxFactory.IdentifierName("ss");
            conversion = model.ClassifyConversion(position, sourceExpression4, targetType);
            Assert.False(conversion.Exists);

            ExpressionSyntax sourceExpression5 = SyntaxFactory.ParseExpression("100L");
            conversion = model.ClassifyConversion(position, sourceExpression5, targetType);
            Assert.True(conversion.IsExplicit);
            Assert.True(conversion.IsNumeric);
        }

        #region "Diagnostics"
        [Fact]
        public void VarianceRelationFail()
        {
            var source = @"
delegate void Covariant<out T>(int argument);

class B { }

class C { }

class Program
{
    public static void Main(string[] args)
    {
        Covariant<B> cb = null;
        Covariant<C> cc = (Covariant<C>)cb;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            var diagnostics = compilation.GetDiagnostics();
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void EnumFromZero()
        {
            var source = @"
enum Enum { e1, e2 };

class Program
{
    public static void Main(string[] args)
    {
        Enum e = (12L - 12);
        e = e + 1;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            var diagnostics = compilation.GetDiagnostics();
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void IdentityConversionInvolvingDynamic()
        {
            var source = @"
interface I1<T> { }
interface I2<T, U> { }

class Program
{
    public static void Main(string[] args)
    {
        I2<I1<dynamic>, I1<object>> i1 = null;
        I2<I1<object>, I1<dynamic>> i2 = null;
        i1 = i2;
        i2 = i1;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void WrongDirectionVarianceValueType()
        {
            var source = @"
interface I<out T> { }

struct S : I<object>
{
    public void M() { }
}

class Program
{
    public static void Main(string[] args)
    {
        I<string> i = null;
        S s = (S)i;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            var diagnostics = compilation.GetDiagnostics();
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void CastInterfaceToNonimplementingSealed()
        {
            var source = @"
interface I1 {}

sealed class C1 {}

public class Driver
{
    public static void Main()
    {
        I1 inter = null;
        C1 c1 = (C1)inter;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            var diagnostics = compilation.GetDiagnostics();
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void TestLiteralZeroToNullableEnumConversion()
        {
            // Oddly enough, the C# specification categorizes
            // the conversion from 0 to E? as an implicit enumeration conversion,
            // not as a nullable conversion.

            var source = @"
class Program
{
    enum E { None }
    public static void Main()
    {
        E? e = 0;
        System.Console.WriteLine(e);
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(542540, "DevDiv")]
        [Fact]
        public void TestMethodGroupConversionWithOptionalParameter()
        {
            var source = @"
class C
{
    static void foo(int x = 0) //overload resolution picks this method, but the parameter count doesn't match
    {
        System.Action a = foo;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,27): error CS0123: No overload for 'foo' matches delegate 'System.Action'
                //         System.Action a = foo;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "foo").WithArguments("foo", "System.Action"));
        }

        [WorkItem(543119, "DevDiv")]
        [Fact]
        public void TestConversion_IntToNullableShort()
        {
            var source =
@"namespace Test
{
    public class Program
    {
        short? Foo()
        {
            short? s = 2;
            return s;
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(543450, "DevDiv")]
        [Fact()]
        public void TestConversion_IntToByte()
        {
            var source =
@"
class Program
{
    public static void Main()
    {
        byte x = 1;
        int y = 1;
        x <<= y;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void AmbiguousImplicitConversion()
        {
            var source = @"
public class A
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

public class B
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

class Test
{
    static void Main()
    {
        B b = new B();
        A a = b;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (23,15): error CS0457: Ambiguous user defined conversions 'B.implicit operator A(B)' and 'A.implicit operator A(B)' when converting from 'B' to 'A'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "b").WithArguments("B.implicit operator A(B)", "A.implicit operator A(B)", "B", "A"));
        }

        [Fact]
        public void AmbiguousImplicitConversionAsExplicit()
        {
            var source = @"
public class A
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

public class B
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

class Test
{
    static void Main()
    {
        B b = new B();
        A a = (A)b;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (23,15): error CS0457: Ambiguous user defined conversions 'B.implicit operator A(B)' and 'A.implicit operator A(B)' when converting from 'B' to 'A'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(A)b").WithArguments("B.implicit operator A(B)", "A.implicit operator A(B)", "B", "A"));
        }

        [Fact]
        public void AmbiguousImplicitConversionGeneric()
        {
            var source = @"
public class A
{
    static public implicit operator A(B<A> b)
    {
        return default(A);
    }
}

public class B<T>
{
    static public implicit operator T(B<T> b)
    {
        return default(T);
    }
}

class C
{
    static void Main()
    {
        B<A> b = new B<A>();
        A a = b;
     }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (23,15): error CS0457: Ambiguous user defined conversions 'B<A>.implicit operator A(B<A>)' and 'A.implicit operator A(B<A>)' when converting from 'B<A>' to 'A'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "b").WithArguments("B<A>.implicit operator A(B<A>)", "A.implicit operator A(B<A>)", "B<A>", "A"));
        }

        [Fact]
        public void AmbiguousExplicitConversion()
        {
            var source = @"
public class A
{
    static public explicit operator A(B b)
    {
        return default(A);
    }
}

public class B
{
    static public explicit operator A(B b)
    {
        return default(A);
    }
}

class Test
{
    static void Main()
    {
        B b = new B();
        A a = (A)b;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (23,15): error CS0457: Ambiguous user defined conversions 'B.explicit operator A(B)' and 'A.explicit operator A(B)' when converting from 'B' to 'A'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(A)b").WithArguments("B.explicit operator A(B)", "A.explicit operator A(B)", "B", "A"));
        }

        [Fact]
        public void AmbiguousExplicitConversionAsImplicit()
        {
            var source = @"
public class A
{
    static public explicit operator A(B b)
    {
        return default(A);
    }
}

public class B
{
    static public explicit operator A(B b)
    {
        return default(A);
    }
}

class Test
{
    static void Main()
    {
        B b = new B();
        A a = b;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (23,15): error CS0266: Cannot implicitly convert type 'B' to 'A'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b").WithArguments("B", "A"));
        }

        [Fact]
        public void AmbiguousImplicitExplicitConversionAsImplicit()
        {
            var source = @"
public class A
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

public class B
{
    static public explicit operator A(B b)
    {
        return default(A);
    }
}

class Test
{
    static void Main()
    {
        B b = new B();
        A a = b;
    }
}";
            // As in Dev10, we prefer the implicit conversion.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void AmbiguousImplicitExplicitConversionAsExplicit()
        {
            var source = @"
public class A
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

public class B
{
    static public explicit operator A(B b)
    {
        return default(A);
    }
}

class Test
{
    static void Main()
    {
        B b = new B();
        A a = (A)b;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (23,15): error CS0457: Ambiguous user defined conversions 'B.explicit operator A(B)' and 'A.implicit operator A(B)' when converting from 'B' to 'A'
                //         A a = (A)b;
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(A)b").WithArguments("B.explicit operator A(B)", "A.implicit operator A(B)", "B", "A"));
        }

        [Fact]
        public void NoUserDefinedConversionsDefaultParameter1()
        {
            var source = @"
public class A
{
    static public implicit operator int(A a)
    {
        throw null;
    }
}

class C
{
    void Foo(int x = default(A))
    {

    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,18): error CS1750: A value of type 'A' cannot be used as a default parameter because there are no standard conversions to type 'int'
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("A", "int"));
        }

        [Fact]
        public void NoUserDefinedConversionsDefaultParameter2()
        {
            var source = @"
public class A
{
    static public implicit operator A(int i)
    {
        throw null;
    }
}

class C
{
    void Foo(A x = default(int))
    {

    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,16): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'A'
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("int", "A"));
        }

        [Fact]
        public void NoUserDefinedConversionsDefaultParameter3()
        {
            var source = @"
class Base { }
class Derived : Base { }

class A
{
    static public implicit operator Derived(A a)
    {
        throw null;
    }
}

class C
{
    void Foo(Base b = default(A))
    {

    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (15,19): error CS1750: A value of type 'A' cannot be used as a default parameter because there are no standard conversions to type 'Base'
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "b").WithArguments("A", "Base"));
        }

        [Fact]
        public void NoUserDefinedConversionsIs()
        {
            var source = @"
using System;

public sealed class A
{
    static public implicit operator A(B b)
    {
        throw null;
    }

    static public implicit operator B(A b)
    {
        throw null;
    }
}

public sealed class B
{
}

class C
{
    static void Main()
    {
        A a = new A();
        B b = new B();
        Console.WriteLine(a is B);
        Console.WriteLine(b is A);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (27,27): warning CS0184: The given expression is never of the provided ('B') type
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "a is B").WithArguments("B"),
                // (28,27): warning CS0184: The given expression is never of the provided ('A') type
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "b is A").WithArguments("A"));
        }

        [Fact]
        public void NoUserDefinedConversionsAs()
        {
            var source = @"
using System;

public sealed class A
{
    static public implicit operator A(B b)
    {
        throw null;
    }

    static public implicit operator B(A b)
    {
        throw null;
    }
}

public sealed class B
{
}

class C
{
    static void Main()
    {
        A a = new A();
        B b = new B();
        Console.WriteLine(a as B);
        Console.WriteLine(b as A);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (27,27): error CS0039: Cannot convert type 'A' to 'B' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, "a as B").WithArguments("A", "B"),
                // (28,27): error CS0039: Cannot convert type 'B' to 'A' via a reference conversion, boxing conversion, unboxing conversion, wrapping conversion, or null type conversion
                Diagnostic(ErrorCode.ERR_NoExplicitBuiltinConv, "b as A").WithArguments("B", "A"));
        }

        [Fact]
        public void NoUserDefinedConversionsThrow()
        {
            var source = @"
class C
{
    static void Main()
    {
        throw new Convertible();
    }
}

class Convertible
{
    public static implicit operator System.Exception(Convertible c)
    {
        throw null;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,15): error CS0155: The type caught or thrown must be derived from System.Exception
                Diagnostic(ErrorCode.ERR_BadExceptionType, "new Convertible()"));
        }

        [Fact]
        public void NoUserDefinedConversionsCatch1()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch(Convertible)
        {
        }
    }
}

class Convertible
{
    public static implicit operator System.Exception(Convertible c)
    {
        throw null;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,15): error CS0155: The type caught or thrown must be derived from System.Exception
                Diagnostic(ErrorCode.ERR_BadExceptionType, "Convertible"));
        }

        [Fact]
        public void NoUserDefinedConversionsCatch2()
        {
            var source = @"
class C
{
    static void Main()
    {
        try
        {
        }
        catch (Exception1)
        {
        }
        catch (Exception2)
        {
        }
    }
}

class Exception1 : System.Exception
{

}

class Exception2 : System.Exception
{
    public static implicit operator Exception1(Exception2 e)
    {
        throw null;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void NoUserDefinedConversionsCaseLabel1()
        {
            var source = @"
class C
{
    static void Main()
    {
        switch (0)
        {
            case default(Convertible): return;
        }
    }
}

class Convertible
{
    public static implicit operator int(Convertible e)
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,13): error CS0150: A constant value is expected
                Diagnostic(ErrorCode.ERR_ConstantExpected, "case default(Convertible):"),
                // (8,40): warning CS0162: Unreachable code detected
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"));
        }

        [Fact]
        public void NoUserDefinedConversionsCaseLabel2()
        {
            var source = @"
class C
{
    static void Main()
    {
        const Convertible c = null;
        switch (0)
        {
            case c: return;
        }
    }
}

class Convertible
{
    public static implicit operator int(Convertible e)
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,13): error CS0150: A constant value is expected
                Diagnostic(ErrorCode.ERR_ConstantExpected, "case c:"),
                // (9,21): warning CS0162: Unreachable code detected
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return"));
        }

        [Fact]
        public void NoUserDefinedConversionsUsing()
        {
            var il = @"
.class public sequential ansi sealed beforefieldinit ConvertibleToIDisposable
       extends [mscorlib]System.ValueType
{
  .method public hidebysig specialname static 
          class [mscorlib]System.IDisposable 
          op_Implicit(valuetype ConvertibleToIDisposable e) cil managed
  {
    ldnull
    ret
  }
}
";

            var csharp = @"
class C
{
    static void Main()
    {
        using (var d = new ConvertibleToIDisposable()) 
        {
        }
    }
}";
            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics(
                // (6,16): error CS1674: 'ConvertibleToIDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var d = new ConvertibleToIDisposable()").WithArguments("ConvertibleToIDisposable"));
        }

        [WorkItem(11221, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void OverflowInImplicitConversion()
        {
            var source =
@"class C
{
    public static explicit operator C(byte x)
    {
        return null;
    }

    static void Main()
    {
        var b = (C)1000M;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,17): error CS0031: Constant value '1000M' cannot be converted to a 'byte'
                //         var b = (C)1000M;
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "1000M").WithArguments("1000M", "byte")
                );
        }

        [WorkItem(529568, "DevDiv")]
        [Fact()]
        public void AmbiguousConversions()
        {
            var source = @"
// Tests conversions of generic constructed types - both open and closed.
using System;

public class A { }

public class B { }

public class C { }


public class G0 { public override string ToString() { return ""G0""; } }

public class G1<R> : G0 { public override string ToString() { return string.Format(""G1<{0}>"", typeof(R)); } }

public class GS2<R, S> : G1<S> { public override string ToString() { return string.Format(""GS2<{0},{1}>"", typeof(R), typeof(S)); } }

public class GS3<R, S, T> : GS2<S, T> { public override string ToString() { return string.Format(""GS3<{0},{1},{2}>"", typeof(R), typeof(S), typeof(T)); } }

// Parallel hierarchy.
public class H0 {
    public override string ToString() { return ""H0""; }
    public static implicit operator G0(H0 h) {
        Console.Write(""[H0->G0] "");
        return new G0();
    }
}

public class H1<R> : H0 {
    public override string ToString() { return string.Format(""H1<{0}>"", typeof(R)); }
    public static implicit operator G1<R>(H1<R> h) {
        Console.Write(""[H1->G1] "");
        return new G1<R>();
    }
}

public class HS2<R, S> : H1<S> {
    public override string ToString() { return string.Format(""HS2<{0},{1}>"", typeof(R), typeof(S)); }
    public static implicit operator GS2<R,S>(HS2<R,S> h) {
        Console.Write(""[HS2->GS2] "");
        return new GS2<R,S>();
    }
}

public class HS3<R, S, T> : HS2<S, T> {
    public override string ToString() { return string.Format(""HS3<{0},{1},{2}>"", typeof(R), typeof(S), typeof(T)); }
    public static implicit operator GS3<R,S,T>(HS3<R,S,T> h) {
        Console.Write(""[HS3->GS3] "");
        return new GS3<R,S,T>();
    }
}

// Complex constructed base types.
public class GC2<R, S> : G1<G1<S>> { public override string ToString() { return string.Format(""GC2<{0},{1}>"", typeof(R), typeof(S)); } }

public class GC3<R, S, T> : GC2<G1<T>, GC2<R, G1<S>>> { public override string ToString() { return string.Format(""GC3<{0},{1},{2}>"", typeof(R), typeof(S), typeof(T)); } }

// Parallel hierarchy.
public class HC2<R, S> : H1<G1<S>> {
    public override string ToString() { return string.Format(""HC2<{0},{1}>"", typeof(R), typeof(S)); }
    public static implicit operator GC2<R,S>(HC2<R,S> h) {
        Console.Write(""[HC2->GC2] "");
        return new GC2<R,S>();
    }
}

public class HC3<R, S, T> : HC2<G1<T>, GC2<R, G1<S>>> {
    public override string ToString() { return string.Format(""HC3<{0},{1},{2}>"", typeof(R), typeof(S), typeof(T)); }
    public static implicit operator GC3<R,S,T>(HC3<R,S,T> h) {
        Console.Write(""[HC3->GC3] "");
        return new GC3<R,S,T>();
    }
}

public class HH2<R, S> : H1<H1<S>> {
    public override string ToString() { return string.Format(""HH2<{0},{1}>"", typeof(R), typeof(S)); }
    public static implicit operator GC2<R,S>(HH2<R,S> h) {
        Console.Write(""[HH2->GC2] "");
        return new GC2<R,S>();
    }
}

public class HH3<R, S, T> : HH2<H1<T>, HH2<R, H1<S>>> {
    public override string ToString() { return string.Format(""HH3<{0},{1},{2}>"", typeof(R), typeof(S), typeof(T)); }
    public static implicit operator GC3<R,S,T>(HH3<R,S,T> h) {
        Console.Write(""[HH3->GC3] "");
        return new GC3<R,S,T>();
    }
}

public class Test {
    public static void F0(G0 g) { Console.WriteLine(""F0({0})"", g); }
    public static void F1<R>(G1<R> g) { Console.WriteLine(""F1<{0}>({1})"", typeof(R), g); }

    public static void FS2<R,S>(GS2<R, S> g) { Console.WriteLine(""FS2<{0},{1}>({2})"", typeof(R), typeof(S), g); }
    public static void FS3<R,S,T>(GS3<R, S, T> g) { Console.WriteLine(""FS3<{0},{1},{2}>({3})"", typeof(R), typeof(S), typeof(T), g); }

    public static void FC2<R,S>(GC2<R, S> g) { Console.WriteLine(""FC2<{0},{1}>({2})"", typeof(R), typeof(S), g); }
    public static void FC3<R,S,T>(GC3<R, S, T> g) { Console.WriteLine(""FC3<{0},{1},{2}>({3})"", typeof(R), typeof(S), typeof(T), g); }

    public static void Main() {
        Console.WriteLine(""***** Start generic constructed type conversion test"");

        G0 g0 = new G0();
        G1<A> g1a = new G1<A>();
        GS2<A, B> gs2ab = new GS2<A, B>();
        GS3<A, B, C> gs3abc = new GS3<A, B, C>();

        H0 h0 = new H0();
        H1<A> h1a = new H1<A>();
        HS2<A, B> hs2ab = new HS2<A, B>();
        HS3<A, B, C> hs3abc = new HS3<A, B, C>();

        GC2<A, B> gc2ab = new GC2<A, B>();
        GC3<A, B, C> gc3abc = new GC3<A, B, C>();

        HC2<A, B> hc2ab = new HC2<A, B>();
        HC3<A, B, C> hc3abc = new HC3<A, B, C>();

        HH2<A, B> hh2ab = new HH2<A, B>();
        HH3<A, B, C> hh3abc = new HH3<A, B, C>();

        // ***** Implicit user defined conversion.

        // H1<A> -> G0: ambiguous
        F0(h1a);

        // HS2<A,B> -> G0: ambiguous
        F0(hs2ab);

        // HS3<A,B,C> -> G0: ambiguous
        F0(hs3abc);

        // H1<A> -> G1<A>
        F1(h1a);

        // HS2<A,B> -> G1<B>: ambiguous
        F1<B>(hs2ab);

        // HS3<A,B,C> -> G1<C>: ambiguous
        F1<C>(hs3abc);

        // HS2<A,B> -> GS2<A,B>
        FS2(hs2ab);

        // HS3<A,B,C> -> GS2<B,C>: ambiguous
        FS2<B,C>(hs3abc);

        // HS3<A,B,C> -> GS3<A,B,C>
        FS3(hs3abc);

        // * Complex

        // HC2<A,B> -> G0: ambiguous
        F0(hc2ab);

        // HC3<A,B,C> -> G0: ambiguous
        F0(hc3abc);

        // HC2<A,B> -> G1<G1<B>>: ambiguous
        F1<G1<B>>(hc2ab);

        // HC3<A,B,C> -> G1<G1<GC2<A,G1<B>>>>: ambiguous
        F1<G1<GC2<A,G1<B>>>>(hc3abc);

        // HC2<A,B> -> GC2<A,B>
        FC2(hc2ab);

        // HC3<A,B,C> -> GC2<G1<C>,GC2<A,G1<B>>>: ambiguous
        FC2<G1<C>,GC2<A,G1<B>>>(hc3abc);

        // HC3<A,B,C> -> GC3<A,B,C>
        FC3(hc3abc);
        
        // HH2<A,B> -> G0: ambiguous
        F0(hh2ab);

        // HH3<A,B,C> -> G0: ambiguous
        F0(hh3abc);

        // HH2<A,B> -> G1<*>
        F1(hh2ab);

        // HH3<A,B,C> -> G1<*>
        F1(hh3abc);

        // HH3<A,B,C> -> G1<*>
        F1(hh3abc);

        // HH2<A,B> -> GC2<A,B>
        FC2(hh2ab);

        // HH3<A,B,C> -> GC2<*>
        FC2(hh3abc);

        // HH3<A,B,C> -> GC2<*>
        FC2(hh3abc);

        // HG3<A,B,C> -> GC3<A,B,C>
        FC3(hh3abc);

        Console.WriteLine(""***** End generic constructed type conversion test"");
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (126,12): error CS0457: Ambiguous user defined conversions 'H1<A>.implicit operator G1<A>(H1<A>)' and 'H0.implicit operator G0(H0)' when converting from 'H1<A>' to 'G0'
    //         F0(h1a);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "h1a").WithArguments("H1<A>.implicit operator G1<A>(H1<A>)", "H0.implicit operator G0(H0)", "H1<A>", "G0"),
    // (129,12): error CS0457: Ambiguous user defined conversions 'HS2<A, B>.implicit operator GS2<A, B>(HS2<A, B>)' and 'H1<B>.implicit operator G1<B>(H1<B>)' when converting from 'HS2<A, B>' to 'G0'
    //         F0(hs2ab);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hs2ab").WithArguments("HS2<A, B>.implicit operator GS2<A, B>(HS2<A, B>)", "H1<B>.implicit operator G1<B>(H1<B>)", "HS2<A, B>", "G0"),
    // (132,12): error CS0457: Ambiguous user defined conversions 'HS3<A, B, C>.implicit operator GS3<A, B, C>(HS3<A, B, C>)' and 'HS2<B, C>.implicit operator GS2<B, C>(HS2<B, C>)' when converting from 'HS3<A, B, C>' to 'G0'
    //         F0(hs3abc);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hs3abc").WithArguments("HS3<A, B, C>.implicit operator GS3<A, B, C>(HS3<A, B, C>)", "HS2<B, C>.implicit operator GS2<B, C>(HS2<B, C>)", "HS3<A, B, C>", "G0"),
    // (135,9): error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         F1(h1a);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Test.F1<R>(G1<R>)"),
    // (138,15): error CS0457: Ambiguous user defined conversions 'HS2<A, B>.implicit operator GS2<A, B>(HS2<A, B>)' and 'H1<B>.implicit operator G1<B>(H1<B>)' when converting from 'HS2<A, B>' to 'G1<B>'
    //         F1<B>(hs2ab);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hs2ab").WithArguments("HS2<A, B>.implicit operator GS2<A, B>(HS2<A, B>)", "H1<B>.implicit operator G1<B>(H1<B>)", "HS2<A, B>", "G1<B>"),
    // (141,15): error CS0457: Ambiguous user defined conversions 'HS3<A, B, C>.implicit operator GS3<A, B, C>(HS3<A, B, C>)' and 'HS2<B, C>.implicit operator GS2<B, C>(HS2<B, C>)' when converting from 'HS3<A, B, C>' to 'G1<C>'
    //         F1<C>(hs3abc);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hs3abc").WithArguments("HS3<A, B, C>.implicit operator GS3<A, B, C>(HS3<A, B, C>)", "HS2<B, C>.implicit operator GS2<B, C>(HS2<B, C>)", "HS3<A, B, C>", "G1<C>"),
    // (144,9): error CS0411: The type arguments for method 'Test.FS2<R, S>(GS2<R, S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FS2(hs2ab);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FS2").WithArguments("Test.FS2<R, S>(GS2<R, S>)"),
    // (147,18): error CS0457: Ambiguous user defined conversions 'HS3<A, B, C>.implicit operator GS3<A, B, C>(HS3<A, B, C>)' and 'HS2<B, C>.implicit operator GS2<B, C>(HS2<B, C>)' when converting from 'HS3<A, B, C>' to 'GS2<B, C>'
    //         FS2<B,C>(hs3abc);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hs3abc").WithArguments("HS3<A, B, C>.implicit operator GS3<A, B, C>(HS3<A, B, C>)", "HS2<B, C>.implicit operator GS2<B, C>(HS2<B, C>)", "HS3<A, B, C>", "GS2<B, C>"),
    // (150,9): error CS0411: The type arguments for method 'Test.FS3<R, S, T>(GS3<R, S, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FS3(hs3abc);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FS3").WithArguments("Test.FS3<R, S, T>(GS3<R, S, T>)"),
    // (155,12): error CS0457: Ambiguous user defined conversions 'HC2<A, B>.implicit operator GC2<A, B>(HC2<A, B>)' and 'H1<G1<B>>.implicit operator G1<G1<B>>(H1<G1<B>>)' when converting from 'HC2<A, B>' to 'G0'
    //         F0(hc2ab);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hc2ab").WithArguments("HC2<A, B>.implicit operator GC2<A, B>(HC2<A, B>)", "H1<G1<B>>.implicit operator G1<G1<B>>(H1<G1<B>>)", "HC2<A, B>", "G0"),
    // (158,12): error CS0457: Ambiguous user defined conversions 'HC3<A, B, C>.implicit operator GC3<A, B, C>(HC3<A, B, C>)' and 'HC2<G1<C>, GC2<A, G1<B>>>.implicit operator GC2<G1<C>, GC2<A, G1<B>>>(HC2<G1<C>, GC2<A, G1<B>>>)' when converting from 'HC3<A, B, C>' to 'G0'
    //         F0(hc3abc);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hc3abc").WithArguments("HC3<A, B, C>.implicit operator GC3<A, B, C>(HC3<A, B, C>)", "HC2<G1<C>, GC2<A, G1<B>>>.implicit operator GC2<G1<C>, GC2<A, G1<B>>>(HC2<G1<C>, GC2<A, G1<B>>>)", "HC3<A, B, C>", "G0"),
    // (161,19): error CS0457: Ambiguous user defined conversions 'HC2<A, B>.implicit operator GC2<A, B>(HC2<A, B>)' and 'H1<G1<B>>.implicit operator G1<G1<B>>(H1<G1<B>>)' when converting from 'HC2<A, B>' to 'G1<G1<B>>'
    //         F1<G1<B>>(hc2ab);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hc2ab").WithArguments("HC2<A, B>.implicit operator GC2<A, B>(HC2<A, B>)", "H1<G1<B>>.implicit operator G1<G1<B>>(H1<G1<B>>)", "HC2<A, B>", "G1<G1<B>>"),
    // (164,30): error CS0457: Ambiguous user defined conversions 'HC3<A, B, C>.implicit operator GC3<A, B, C>(HC3<A, B, C>)' and 'HC2<G1<C>, GC2<A, G1<B>>>.implicit operator GC2<G1<C>, GC2<A, G1<B>>>(HC2<G1<C>, GC2<A, G1<B>>>)' when converting from 'HC3<A, B, C>' to 'G1<G1<GC2<A, G1<B>>>>'
    //         F1<G1<GC2<A,G1<B>>>>(hc3abc);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hc3abc").WithArguments("HC3<A, B, C>.implicit operator GC3<A, B, C>(HC3<A, B, C>)", "HC2<G1<C>, GC2<A, G1<B>>>.implicit operator GC2<G1<C>, GC2<A, G1<B>>>(HC2<G1<C>, GC2<A, G1<B>>>)", "HC3<A, B, C>", "G1<G1<GC2<A, G1<B>>>>"),
    // (167,9): error CS0411: The type arguments for method 'Test.FC2<R, S>(GC2<R, S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FC2(hc2ab);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FC2").WithArguments("Test.FC2<R, S>(GC2<R, S>)"),
    // (170,33): error CS0457: Ambiguous user defined conversions 'HC3<A, B, C>.implicit operator GC3<A, B, C>(HC3<A, B, C>)' and 'HC2<G1<C>, GC2<A, G1<B>>>.implicit operator GC2<G1<C>, GC2<A, G1<B>>>(HC2<G1<C>, GC2<A, G1<B>>>)' when converting from 'HC3<A, B, C>' to 'GC2<G1<C>, GC2<A, G1<B>>>'
    //         FC2<G1<C>,GC2<A,G1<B>>>(hc3abc);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hc3abc").WithArguments("HC3<A, B, C>.implicit operator GC3<A, B, C>(HC3<A, B, C>)", "HC2<G1<C>, GC2<A, G1<B>>>.implicit operator GC2<G1<C>, GC2<A, G1<B>>>(HC2<G1<C>, GC2<A, G1<B>>>)", "HC3<A, B, C>", "GC2<G1<C>, GC2<A, G1<B>>>"),
    // (173,9): error CS0411: The type arguments for method 'Test.FC3<R, S, T>(GC3<R, S, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FC3(hc3abc);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FC3").WithArguments("Test.FC3<R, S, T>(GC3<R, S, T>)"),
    // (178,12): error CS0457: Ambiguous user defined conversions 'HH2<A, B>.implicit operator GC2<A, B>(HH2<A, B>)' and 'H1<H1<B>>.implicit operator G1<H1<B>>(H1<H1<B>>)' when converting from 'HH2<A, B>' to 'G0'
    //         F0(hh2ab);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hh2ab").WithArguments("HH2<A, B>.implicit operator GC2<A, B>(HH2<A, B>)", "H1<H1<B>>.implicit operator G1<H1<B>>(H1<H1<B>>)", "HH2<A, B>", "G0"),
    // (181,12): error CS0457: Ambiguous user defined conversions 'HH3<A, B, C>.implicit operator GC3<A, B, C>(HH3<A, B, C>)' and 'HH2<H1<C>, HH2<A, H1<B>>>.implicit operator GC2<H1<C>, HH2<A, H1<B>>>(HH2<H1<C>, HH2<A, H1<B>>>)' when converting from 'HH3<A, B, C>' to 'G0'
    //         F0(hh3abc);
    Diagnostic(ErrorCode.ERR_AmbigUDConv, "hh3abc").WithArguments("HH3<A, B, C>.implicit operator GC3<A, B, C>(HH3<A, B, C>)", "HH2<H1<C>, HH2<A, H1<B>>>.implicit operator GC2<H1<C>, HH2<A, H1<B>>>(HH2<H1<C>, HH2<A, H1<B>>>)", "HH3<A, B, C>", "G0"),
    // (184,9): error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         F1(hh2ab);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Test.F1<R>(G1<R>)"),
    // (187,9): error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         F1(hh3abc);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Test.F1<R>(G1<R>)"),
    // (190,9): error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         F1(hh3abc);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F1").WithArguments("Test.F1<R>(G1<R>)"),
    // (193,9): error CS0411: The type arguments for method 'Test.FC2<R, S>(GC2<R, S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FC2(hh2ab);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FC2").WithArguments("Test.FC2<R, S>(GC2<R, S>)"),
    // (196,9): error CS0411: The type arguments for method 'Test.FC2<R, S>(GC2<R, S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FC2(hh3abc);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FC2").WithArguments("Test.FC2<R, S>(GC2<R, S>)"),
    // (199,9): error CS0411: The type arguments for method 'Test.FC2<R, S>(GC2<R, S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FC2(hh3abc);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FC2").WithArguments("Test.FC2<R, S>(GC2<R, S>)"),
    // (202,9): error CS0411: The type arguments for method 'Test.FC3<R, S, T>(GC3<R, S, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         FC3(hh3abc);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "FC3").WithArguments("Test.FC3<R, S, T>(GC3<R, S, T>)")

                //Dev10
                //error CS0457: Ambiguous user defined conversions 'H1<A>.implicit operator G1<A>(H1<A>)' and 'H0.implicit operator G0(H0)' when converting from 'H1<A>' to 'G0'
                //error CS0457: Ambiguous user defined conversions 'HS2<A,B>.implicit operator GS2<A,B>(HS2<A,B>)' and 'H0.implicit operator G0(H0)' when converting from 'HS2<A,B>' to 'G0'
                //error CS0457: Ambiguous user defined conversions 'HS3<A,B,C>.implicit operator GS3<A,B,C>(HS3<A,B,C>)' and 'H0.implicit operator G0(H0)' when converting from 'HS3<A,B,C>' to 'G0'
                //error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0457: Ambiguous user defined conversions 'HS2<A,B>.implicit operator GS2<A,B>(HS2<A,B>)' and 'H1<B>.implicit operator G1<B>(H1<B>)' when converting from 'HS2<A,B>' to 'G1<B>'
                //error CS0457: Ambiguous user defined conversions 'HS3<A,B,C>.implicit operator GS3<A,B,C>(HS3<A,B,C>)' and 'H1<C>.implicit operator G1<C>(H1<C>)' when converting from 'HS3<A,B,C>' to 'G1<C>'
                //error CS0411: The type arguments for method 'Test.FS2<R,S>(GS2<R,S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0457: Ambiguous user defined conversions 'HS3<A,B,C>.implicit operator GS3<A,B,C>(HS3<A,B,C>)' and 'HS2<B,C>.implicit operator GS2<B,C>(HS2<B,C>)' when converting from 'HS3<A,B,C>' to 'GS2<B,C>'
                //error CS0411: The type arguments for method 'Test.FS3<R,S,T>(GS3<R,S,T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0457: Ambiguous user defined conversions 'HC2<A,B>.implicit operator GC2<A,B>(HC2<A,B>)' and 'H0.implicit operator G0(H0)' when converting from 'HC2<A,B>' to 'G0'
                //error CS0457: Ambiguous user defined conversions 'HC3<A,B,C>.implicit operator GC3<A,B,C>(HC3<A,B,C>)' and 'H0.implicit operator G0(H0)' when converting from 'HC3<A,B,C>' to 'G0'
                //error CS0457: Ambiguous user defined conversions 'HC2<A,B>.implicit operator GC2<A,B>(HC2<A,B>)' and 'H1<G1<B>>.implicit operator G1<G1<B>>(H1<G1<B>>)' when converting from 'HC2<A,B>' to 'G1<G1<B>>'
                //error CS0457: Ambiguous user defined conversions 'HC3<A,B,C>.implicit operator GC3<A,B,C>(HC3<A,B,C>)' and 'H1<G1<GC2<A,G1<B>>>>.implicit operator G1<G1<GC2<A,G1<B>>>>(H1<G1<GC2<A,G1<B>>>>)' when converting from 'HC3<A,B,C>' to 'G1<G1<GC2<A,G1<B>>>>'
                //error CS0411: The type arguments for method 'Test.FC2<R,S>(GC2<R,S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0457: Ambiguous user defined conversions 'HC3<A,B,C>.implicit operator GC3<A,B,C>(HC3<A,B,C>)' and 'HC2<G1<C>,GC2<A,G1<B>>>.implicit operator GC2<G1<C>,GC2<A,G1<B>>>(HC2<G1<C>,GC2<A,G1<B>>>)' when converting from 'HC3<A,B,C>' to 'GC2<G1<C>,GC2<A,G1<B>>>'
                //error CS0411: The type arguments for method 'Test.FC3<R,S,T>(GC3<R,S,T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0457: Ambiguous user defined conversions 'HH2<A,B>.implicit operator GC2<A,B>(HH2<A,B>)' and 'H0.implicit operator G0(H0)' when converting from 'HH2<A,B>' to 'G0'
                //error CS0457: Ambiguous user defined conversions 'HH3<A,B,C>.implicit operator GC3<A,B,C>(HH3<A,B,C>)' and 'H0.implicit operator G0(H0)' when converting from 'HH3<A,B,C>' to 'G0'
                //error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0411: The type arguments for method 'Test.F1<R>(G1<R>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0411: The type arguments for method 'Test.FC2<R,S>(GC2<R,S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0411: The type arguments for method 'Test.FC2<R,S>(GC2<R,S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0411: The type arguments for method 'Test.FC2<R,S>(GC2<R,S>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //error CS0411: The type arguments for method 'Test.FC3<R,S,T>(GC3<R,S,T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                );
        }

        [WorkItem(545361, "DevDiv")]
        [ClrOnlyFact]
        public void NullableIntToStructViaDecimal()
        {
            var source = @"
using System;

public struct S
{
    public static implicit operator S?(decimal d) { Console.Write(d); return new S(); }

    static void Test(bool b) { Console.Write(b ? 't' : 'f'); }

    static void Main()
    {
        int? i;
        S? s; 

        i = 1;

        // Native compiler allows this, even though this is illegal by the spec.
        // there must be a standard implicit conversion either from int? to decimal 
        // or from decimal to int? -- but neither of those conversions are implicit. 
        // The native compiler instead checks to see if there is a standard conversion 
        // between int and decimal, which there is.

        // Roslyn also allows this.

        // Note that there is a codegen difference between the native compiler and Roslyn
        // here, though the difference actually makes no difference other than the
        // Roslyn codegen being less efficient. The Roslyn compiler generates this as:
        //
        // start with int? 
        // lifted conversion to decimal? (cannot fail)
        // explicit conversion to decimal (can fail)
        // user-defined conversion to S?
        // explicit conversion to S.
        //
        // The native compiler generates this as
        //
        // start with int?
        // explicit conversion to int (can fail)
        // implicit conversion to decimal
        // user-defined conversion to S?
        // explicit conversion to S.
        //
        // The native code is better; there's no reason to do the lifted conversion
        // from int? to decimal? and then just check to see if the decimal? is null;
        // we could simply check if the int? is null in the first place.
        //
        // There are many places where Roslyn's nullable codegen is inefficient and 
        // this is one of them. We will likely fix this in a later milestone.

        s = (S)i;
        Test(s != null); // true

        // Let's try the same thing, but with a null. Whether we use the Roslyn or the
        // native codegen, we should get an exception.

        bool threw = false;
        try
        {
            i = null;
            s = (S)i; 
        }
        catch
        {
            threw = true;
        }
        
        Test(threw); // true

        // Now we come to an interesting case.
        //
        // First off, this should not even be legal, this time for two reasons. First,
        // because again, there is no explicit standard conversion from int? to decimal.
        // Second, the native compiler binds this as a lifted conversion, even though 
        // a lifted conversion requires that both the input and output types of the 
        // operator be non-nullable value types; obviously the output type is not a non-nullable
        // value type.
        //
        // Even worse, the native compiler allows this and then generates bad code. It generates
        // a null check on i, but then forgets to generate a conversion from i.Value to decimal
        // before doing the call.
        //
        // Roslyn matches the native compiler's analysis; it treats this as a lifted conversion
        // even though it ought not to. Roslyn however gets the codegen correct.
        //
        
        i = null;
        s = (S?)i;
        Test(s == null); // Roslyn: true. Native compiler: generates bad code that crashes the CLR.

    }
}
";
            CompileAndVerify(source, expectedOutput: @"1ttt");
        }

        [WorkItem(545471, "DevDiv")]
        [ClrOnlyFact]
        public void CheckedConversionsInExpressionTrees()
        {
            var source = @"
using System;
using System.Linq.Expressions;

namespace ExpressionTest
{
    public struct MyStruct
    {
        public static MyStruct operator +(MyStruct c1, MyStruct c2) { return c1; }
        public static explicit operator int(MyStruct m) { return 0; }

        public static void Main()
        {
            Expression<Func<MyStruct, MyStruct?, MyStruct?>> eb1 = (c1, c2) => c1 + c2;
            Expression<Func<MyStruct, MyStruct?, MyStruct?>> eb2 = (c1, c2) => checked(c1 + c2);
            Expression<Func<MyStruct, MyStruct?, MyStruct?>> eb3 = (c1, c2) => unchecked(c1 + c2);
            Expression<Func<int?, int, int?>> ee1 = (c1, c2) => c1 + c2;
            Expression<Func<int?, int, int?>> ee2 = (c1, c2) => checked(c1 + c2);
            Expression<Func<int?, int, int?>> ee3 = (c1, c2) => unchecked(c1 + c2);

            object[] tests = new object[] {
                eb1, eb2, eb3,
                ee1, ee2, ee3,
            };

            foreach (object test in tests)
            {
                Console.WriteLine(test);
            }
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"
(c1, c2) => (Convert(c1) + c2)
(c1, c2) => (Convert(c1) + c2)
(c1, c2) => (Convert(c1) + c2)
(c1, c2) => (c1 + Convert(c2))
(c1, c2) => (c1 + Convert(c2))
(c1, c2) => (c1 + Convert(c2))
");
        }

        [WorkItem(647055, "DevDiv")]
        [Fact]
        public void AmbiguousImplicitExplicitUserDefined()
        {
            var source = @"
class Program
{
    void Test(int[] a)
    {
        C<int> x1 = (C<int>)1; // Expression to type

        foreach (C<int> x2 in a) { } // Type to type
    }
}
 
class C<T>
{
    public static implicit operator C<T>(T x) { return null; }
    public static explicit operator C<T>(int x) { return null; }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,21): error CS0457: Ambiguous user defined conversions 'C<int>.explicit operator C<int>(int)' and 'C<int>.implicit operator C<int>(int)' when converting from 'int' to 'C<int>'
                //         C<int> x1 = (C<int>)1; // Expression to type
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(C<int>)1").WithArguments("C<int>.explicit operator C<int>(int)", "C<int>.implicit operator C<int>(int)", "int", "C<int>"),
                // (8,9): error CS0457: Ambiguous user defined conversions 'C<int>.explicit operator C<int>(int)' and 'C<int>.implicit operator C<int>(int)' when converting from 'int' to 'C<int>'
                //         foreach (C<int> x2 in a) { } // Type to type
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "foreach").WithArguments("C<int>.explicit operator C<int>(int)", "C<int>.implicit operator C<int>(int)", "int", "C<int>"));

            var destinationType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Construct(comp.GetSpecialType(SpecialType.System_Int32));
            var conversionSymbols = destinationType.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind == MethodKind.Conversion);
            Assert.Equal(2, conversionSymbols.Count());

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var castSyntax = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().Single();

            var castInfo = model.GetSymbolInfo(castSyntax);
            Assert.Null(castInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, castInfo.CandidateReason);
            AssertEx.SetEqual(castInfo.CandidateSymbols, conversionSymbols);

            var forEachSyntax = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var memberModel = ((CSharpSemanticModel)model).GetMemberModel(forEachSyntax);
            var boundForEach = memberModel.GetBoundNodes(forEachSyntax).OfType<BoundForEachStatement>().Single();
            var elementConversion = boundForEach.ElementConversion;
            Assert.Equal(LookupResultKind.OverloadResolutionFailure, elementConversion.ResultKind);
            AssertEx.SetEqual(elementConversion.OriginalUserDefinedConversions, conversionSymbols);
        }

        [WorkItem(715207, "DevDiv")]
        [ClrOnlyFact]
        public void LiftingReturnTypeOfExplicitUserDefinedConversion()
        {
            var source = @"
class C
{
    char? Test(object o)
    {
        return (char?)(BigInteger)o;
    }
}

struct BigInteger
{
    public static explicit operator ushort(BigInteger b) { return 0; }
}";

            CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  unbox.any  ""BigInteger""
  IL_0006:  call       ""ushort BigInteger.op_Explicit(BigInteger)""
  IL_000b:  newobj     ""char?..ctor(char)""
  IL_0010:  ret
}
");
        }

        [WorkItem(737732, "DevDiv")]
        [Fact]
        public void ConsiderSourceExpressionWhenDeterminingBestUserDefinedConversion()
        {
            var source = @"
public class C
{
    public static explicit operator C(double d) { return null; }
    public static explicit operator C(byte b) { return null; }
}

public class Test
{
    C M()
    {
        return (C)0;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().Single();

            var symbol = model.GetSymbolInfo(syntax).Symbol;
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            var method = (MethodSymbol)symbol;
            Assert.Equal(MethodKind.Conversion, method.MethodKind);
            Assert.Equal(comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C"), method.ContainingType);
            Assert.Equal(SpecialType.System_Byte, method.ParameterTypes.Single().SpecialType);
        }

        [WorkItem(737732, "DevDiv")]
        [Fact]
        public void Repro737732()
        {
            var source = @"
using System;

public struct C
{
    public static explicit operator C(decimal d) { return default(C); }
    public static explicit operator C(double d) { return default(C); }
    public static implicit operator C(byte d) { return default(C); }

    C Test()
    {
        return (C)0;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().Single();

            var symbol = model.GetSymbolInfo(syntax).Symbol;
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            var method = (MethodSymbol)symbol;
            Assert.Equal(MethodKind.Conversion, method.MethodKind);
            Assert.Equal(comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C"), method.ContainingType);
            Assert.Equal(SpecialType.System_Byte, method.ParameterTypes.Single().SpecialType);
        }

        [WorkItem(742345, "DevDiv")]
        [ClrOnlyFact]
        public void MethodGroupConversion_ContravarianceAndDynamic()
        {
            var source = @"
delegate void In<in T>(T t);

public class C
{
    static void Main()
    {
        M(F);
    }

    static void F(string s) { }

    static void M(In<dynamic> f) { System.Console.WriteLine('A'); } // Better, if both are applicable.
    static void M(In<string> f) { System.Console.WriteLine('B'); } // Actually chosen, since the other isn't applicable.
}
";
            CompileAndVerify(source, new[] { SystemCoreRef }, expectedOutput: "B");
        }

        [WorkItem(742345, "DevDiv")]
        [Fact]
        public void MethodGroupConversion_CovarianceAndDynamic()
        {
            var source = @"
delegate T Out<out T>();

public class C
{
    static void Main()
    {
        M(F);
    }

    static dynamic F() { throw null; }

    static void M(Out<string> f) { } // Better, if both are applicable.
    static void M(Out<dynamic> f) { }
}
";

            // The return type of F isn't considered until the delegate compatibility check,
            // which happens AFTER determining that the method group conversion exists.  As
            // a result, both methods are considered applicable and the "wrong" one is chosen.
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics(
                // (8,11): error CS0407: 'dynamic C.F()' has the wrong return type
                //         M(F);
                Diagnostic(ErrorCode.ERR_BadRetType, "F").WithArguments("C.F()", "dynamic"));
        }

        [WorkItem(737971, "DevDiv")]
        [Fact]
        public void ConversionsFromExpressions()
        {
            var source = @"
using System;

public enum E
{
    A
}

public class Q
{
    public static implicit operator Q(Func<int> f) { return null; }
}

public class R
{
    public static implicit operator R(E e) { return null; }
}

public class S
{
    public static implicit operator S(byte b) { return null; }
}

public struct T
{
    public static implicit operator T(string s) { return default(T); }
}

public struct U
{
    public unsafe static implicit operator U(void* p) { return default(U); }
}

public struct V
{
    public static implicit operator V(dynamic[] d) { return default(V); }
}

public class Test
{
    static void Main()
    {
        // Anonymous function
        {
            Q q;
            q = () => 1; //CS1660
            q = (Q)(() => 1);
        }

        // Method group
        {
            Q q;
            q = F; //CS0428
            q = (Q)F;
        }

        //Enum
        {
            R r;
            r = 0; //CS0029
            r = (E)0;
        }

        // Numeric constant
        {
            S s;
            s = 0;
            s = (S)0;
        }

        // Null literal
        {
            T t;
            t = null;
            t = (T)null;
        }

        // Pointer
        unsafe
        {
            U u;
            u = null;
            u = &u;
            u = (U)null;
            u = (U)(&u);
        }
    }

    static int F() { return 0; }
}
";
            // NOTE: It's pretty wacky that some of these implicit UDCs can only be applied via explicit (cast) conversions,
            // but that's the native behavior.  We need to replicate it for back-compat, but most of the strangeness will
            // not be spec'd.
            CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (46,17): error CS1660: Cannot convert lambda expression to type 'Q' because it is not a delegate type
                //             q = () => 1; //CS1660
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "() => 1").WithArguments("lambda expression", "Q"),
                // (53,17): error CS0428: Cannot convert method group 'F' to non-delegate type 'Q'. Did you intend to invoke the method?
                //             q = F; //CS0428
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "F").WithArguments("F", "Q"),
                // (60,17): error CS0029: Cannot implicitly convert type 'int' to 'R'
                //             r = 0; //CS0029
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "R"));
        }

        #endregion
    }
}
