// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenImplicitlyTypeArraysTests : CompilingTestBase
    {
        #region "Functionality tests"

        [Fact]
        public void Test_001_Simple()
        {
            var source = @"
using System.Linq;

namespace Test
{
    public class Program
    {
        public static void Main()
        {
            var a = new [] {1, 2, 3};
            
            System.Console.Write(a.SequenceEqual(new int[]{1, 2, 3}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_002_IntTypeBest()
        {
            // Best type: int

            var source = @"
using System.Linq;

namespace Test
{
    public class Program
    {
        public static void Main()
        {            
            var a = new [] {1, (byte)2, (short)3};
        
             System.Console.Write(a.SequenceEqual(new int[]{1, (byte)2, (short)3}));
        }
    }
}
";

            CompileAndVerify(
              source,
              expectedOutput: "True");
        }

        [Fact]
        public void Test_003_DoubleTypeBest()
        {
            // Best type: double

            var source = @"
using System.Linq;

namespace Test
{
    public class Program
    {
        public static void Main()
        {            
            var a = new [] {(sbyte)1, (byte)2, (short)3, (ushort)4, 5, 6u, 7l, 8ul, (char)9, 10.0f, 11.0d};
        
           System.Console.Write(a.SequenceEqual(new double[]{(sbyte)1, (byte)2, (short)3, (ushort)4, 5, 6u, 7l, 8ul, (char)9, 10.0f, 11.0d}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact, WorkItem(895655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/895655")]
        public void Test_004_Enum()
        {
            // Enums conversions

            var source = @"
using System.Linq;

namespace Test
{
    public class Program
    {
        enum E
        {
            START
        };
        
        public static void Main()
        {            
            var a = new [] {E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu};
        
             System.Console.Write(a.SequenceEqual(new E[]{E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu}));
        }
    }
}
";

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Mscorlib40Extended);
            comp.VerifyDiagnostics(
    // (15,54): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
    //             var a = new [] {E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu};
    Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(15, 54),
    // (15,88): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
    //             var a = new [] {E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu};
    Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(15, 88),
    // (15,93): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
    //             var a = new [] {E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu};
    Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(15, 93),
    // (17,84): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
    //              System.Console.Write(a.SequenceEqual(new E[]{E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu}));
    Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(17, 84),
    // (17,118): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
    //              System.Console.Write(a.SequenceEqual(new E[]{E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu}));
    Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(17, 118),
    // (17,123): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
    //              System.Console.Write(a.SequenceEqual(new E[]{E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu}));
    Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(17, 123),
    // (15,21): error CS0826: No best type found for implicitly-typed array
    //             var a = new [] {E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu};
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new [] {E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu}").WithLocation(15, 21),
    // (17,35): error CS1929: '?[]' does not contain a definition for 'SequenceEqual' and the best extension method overload 'System.Linq.Queryable.SequenceEqual<Test.Program.E>(System.Linq.IQueryable<Test.Program.E>, System.Collections.Generic.IEnumerable<Test.Program.E>)' requires a receiver of type 'System.Linq.IQueryable<Test.Program.E>'
    //              System.Console.Write(a.SequenceEqual(new E[]{E.START, 0, 0U, 0u, 0L, 0l, 0UL, 0Ul, 0uL, 0ul, 0LU, 0Lu, 0lU, 0lu}));
    Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("?[]", "SequenceEqual", "System.Linq.Queryable.SequenceEqual<Test.Program.E>(System.Linq.IQueryable<Test.Program.E>, System.Collections.Generic.IEnumerable<Test.Program.E>)", "System.Linq.IQueryable<Test.Program.E>").WithLocation(17, 35)
                );
        }

        [Fact]
        public void Test_005_ObjectTypeBest()
        {
            // Implicit reference conversions -- From any reference-type to object.

            var source = @"

using System.Linq;

namespace Test
{
    public class C { };
    public interface I { };    
    public class C2 : I { };
    
    public class Program
    {
        delegate void D();
        
        public static void M() { }
        
        public static void Main()
        {
            object o = new object();
            C c = new C();
            I i = new C2();
            D d = new D(M);
            int[] aa = new int[] {1};
            
           var a = new [] {o, """", c, i, d, aa};
            
           System.Console.Write(a.SequenceEqual(new object[]{o, """", c, i, d, aa}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_006_ArrayTypeBest()
        {
            // Implicit reference conversions -- From an array-type S with an element type SE to an array-type T with an element type TE,

            var source = @"

using System.Linq;

namespace Test
{
    public class Program
    {
        public static void Main()
        {
            object[] oa = new object[] {null};
            string[] sa = new string[] {null};
                        
            var a = new [] {oa, sa};
            
            System.Console.Write(a.SequenceEqual(new object[][]{oa, sa}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_007A()
        {
            // Implicit reference conversions -- From a one-dimensional array-type S[] to System.Collections.Generic.IList<S>.

            var testSrc = @"
using System.Linq;
using System.Collections.Generic;

namespace Test
{
    public class Program
    {
        public static void Main()
        {
            int[] ia = new int[] {1, 2, 3};
            IList<int> la = new List<int> {1, 2, 3};
                        
            var a = new [] {ia, la};
            
            System.Console.Write(a.SequenceEqual(new IList<int>[]{ia, la}));
        }
    }
}
";
            var compilation = CompileAndVerify(
                testSrc,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_007B()
        {
            // Implicit reference conversions -- From a one-dimensional array-type S[] to System.Collections.Generic.IReadOnlyList<S>.

            var testSrc = @"
using System.Collections.Generic;

namespace Test
{
    public class Program
    {
        public static void Main()
        {
            int[] array = new int[] {1, 2, 3};
            object obj = array;
            IEnumerable<int> ro1 = array;
            IReadOnlyList<int> ro2 = (IReadOnlyList<int>)obj;
            IReadOnlyList<int> ro3 = (IReadOnlyList<int>)array;
            IReadOnlyList<int> ro4 = obj as IReadOnlyList<int>;
            System.Console.WriteLine(ro4 != null ? 1 : 2);
        }
    }
}
";
            // The version of mscorlib checked in to the test resources in v4_0_30316 does not have
            // the IReadOnlyList<T> and IReadOnlyCollection<T> interfaces. Use the one in v4_0_30316_17626.

            var mscorlib17626 = MetadataReference.CreateFromImage(TestResources.NetFX.v4_0_30319_17626.mscorlib);
            CompileAndVerify(testSrc, new MetadataReference[] { mscorlib17626 }, expectedOutput: "1", targetFramework: TargetFramework.Empty);
        }

        [Fact]
        public void Test_008_DelegateType()
        {
            // Implicit reference conversions -- From any delegate-type to System.Delegate.

            var source = @"
using System;
using System.Linq;

namespace Test
{
    public class Program
    {
        delegate void D1();
        public static void M1() {}
        
        delegate int D2();
        public static int M2() { return 0;}
        
        delegate void D3(int i);
        public static void M3(int i) {}
        
        delegate void D4(params object[] o);
        public static void M4(params object[] o) { }
               
        public static void Main()
        {            
            D1 d1 = new D1(M1);
            D2 d2 = new D2(M2);
            D3 d3 = new D3(M3);
            D4 d4 = new D4(M4);
            Delegate d = d1;
        
            var a = new [] {d, d1, d2, d3, d4};
            
            System.Console.Write(a.SequenceEqual(new Delegate[]{d, d1, d2, d3, d4}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_009_Null()
        {
            // Implicit reference conversions -- From the null type to any reference-type.

            var source = @"
using System.Linq;

namespace Test
{
    public class Program
    {
        public static void Main()
        {
            var a = new [] {""aa"", ""bb"", null};
            
             System.Console.Write(a.SequenceEqual(new string[]{""aa"", ""bb"", null}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_010_TypeParameter()
        {
            // Implicit reference conversions -- 
            // For a type-parameter T that is known to be a reference type , the following 
            // implicit reference conversions exist:
            // From T to its effective base class C, from T to any base class of C, 
            // and from T to any interface implemented by C.

            var source = @"
using System.Linq;

namespace Test
{
    public interface I {}
    public class B {}
    public class C : B, I {}
    public class D : C {}
    
    public class Program
    {
        public static void M<T>() where T : C, new()
        {
            T t = new T();
            I i = t;
            
            var a = new [] {i, t};
            System.Console.Write(a.SequenceEqual(new I[]{i, t}));
        }
    
        public static void Main()
        {
            M<D>();
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_011_BoxingConversion()
        {
            // Implicit reference conversions -- Boxing conversions

            var testSrc = @"
using System;
using System.Linq;

namespace Test
{
    public struct S
    {
    }
    
    public class Program
    {
        enum E { START };
        
        public static void Main()
        {
            IComparable v = 1;
            int? i = 1;
            
            var a = new [] {v, i, E.START, true, (byte)2, (sbyte)3, (short)4, (ushort)5, 6, 7U, 8L, 9UL, 10.0F, 11.0D, 12M, (char)13};
            
            System.Console.Write(a.SequenceEqual(new IComparable[]{v, i, E.START, true, (byte)2, (sbyte)3, (short)4, (ushort)5, 6, 7U, 8L, 9UL, 10.0F, 11.0D, 12M, (char)13}));
        }
    }
}
";
            var compilation = CompileAndVerify(
                testSrc,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_012_UserDefinedImplicitConversion()
        {
            // User-defined implicit conversions.

            var testSrc = @"
using System.Linq;

namespace Test
{
    public class B {}
    
    public class C
    {
        public static B b = new B();
        
        public static implicit operator B(C c)
        {
            return b;
        }
    }
    
    public class Program
    {
        public static void Main()
        {
            B b = new B();
            C c = new C();
            
            var a = new [] {b, c};
        
            System.Console.Write(a.SequenceEqual(new B[]{b, c}));
        }
    }
}
";
            // NYI: When user-defined conversion lowering is implemented, replace the
            // NYI: error checking below with:
            // var compilation = CompileAndVerify(testSrc, emitOptions: EmitOptions.CCI, 
            //    additionalRefs: GetReferences(), expectedOutput: "");
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(testSrc);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Test_013_A_UserDefinedNullableConversions()
        {
            // Lifted user-defined conversions

            var testSrc = @"
using System.Linq;
namespace Test
{
    public struct B { }
    public struct C
    {
        public static B b = new B();
        public static implicit operator B(C c)
        {
            return b;
        }
    }
    public class Program
    {
        public static void Main()
        {
            C? c = new C();
            B? b = new B();            
            if (!(new [] {b, c}.SequenceEqual(new B?[] {b, c})))
            {
                System.Console.WriteLine(""Test fail at struct C? implicitly convert to struct B?"");
            }
        }
    }
}
";
            // NYI: When lifted user-defined conversion lowering is implemented, replace the
            // NYI: error checking below with:

            // var compilation = CompileAndVerify(testSrc, emitOptions: EmitOptions.CCI, 
            //     additionalRefs: GetReferences(), expectedOutput: "");

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(testSrc);
            compilation.VerifyDiagnostics();
        }

        // Bug 10700: We should be able to infer the array type from elements of types int? and short?
        [Fact]
        public void Test_013_B_NullableConversions()
        {
            // Lifted implicit numeric conversions

            var testSrc = @"
using System.Linq;
namespace Test
{
    public class Program
    {
        public static void Main()
        {
            int? i = 1;
            short? s = 2;
            if (!new [] {i, s}.SequenceEqual(new int?[] {i, s}))
            {
                System.Console.WriteLine(""Test fail at short? implicitly convert to int?"");
            }
        }
    }
}
";
            // NYI: When lifted conversions are implemented, remove the diagnostics check
            // NYI: and replace it with:
            // var compilation = CompileAndVerify(testSrc, emitOptions: EmitOptions.CCI,
            //     additionalRefs: GetReferences(), expectedOutput: "");

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(testSrc);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void Test_014_LambdaExpression()
        {
            // Implicitly conversion from lambda expression to compatible delegate type

            var source = @"
using System;

namespace Test
{
    public class Program
    {
        delegate int D(int i);
        
        public static int M(int i) {return i;}
        
        public static void Main()
        {            
           var a = new [] {new D(M), (int i)=>{return i;}, x => x+1, (int i)=>{short s = 2; return s;}};
        
           Console.Write(((a is D[]) && (a.Length==4)));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_015_ImplicitlyTypedLocalExpr()
        {
            // local variable declared as "var" type is used inside an implicitly typed array.

            var source = @"
using System.Linq;

namespace Test
{
    public class B { };
    public class C : B { };
    
    public class Program
    {        
        public static void Main()
        {
            var b = new B();
            var c = new C();
            
            var a = new [] {b, c};
            
            System.Console.Write(a.SequenceEqual(new B[]{b, c}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_016_ArrayCreationExpression()
        {
            // Array creation expression as element in implicitly typed arrays

            var source = @"
using System;

namespace Test
{   
    public class Program
    {        
        public static void Main()
        {            
            var a = new [] {new int[1] , new int[3] {11, 12, 13}, new int[] {21, 22, 23}};
            
            Console.Write(((a is int[][]) && (a.Length==3)));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_017_AnonymousObjectCreationExpression()
        {
            // Anonymous object creation expression as element in implicitly typed arrays

            var source = @"
using System;

namespace Test
{   
    public class Program
    {        
        public static void Main()
        {
            var a = new [] {new {i = 2, s = ""bb""}, new {i = 3, s = ""cc""}};
            
            Console.Write(((a is Array) && (a.Length==2)));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_018_MemberAccessExpression()
        {
            // Member access expression as element in implicitly typed arrays

            var source = @"
using System.Linq;

using NT = Test;

namespace Test
{
    public class Program
    {
        public delegate int D(string s);
        
        public static D d1 = new D(M2);
        
        public static int M<T>(string s) {return 1;}
                
        public static int M2(string s) {return 2;}        
            
        public D d2 = new D(M2);
        public static Program p = new Program();
        
        public static Program GetP() {return p;}
    
        public static void Main()
        {            
            System.Console.Write(new [] {Program.d1, Program.M<int>, GetP().d2, int.Parse, NT::Program.M2}.SequenceEqual( 
                                 new D[] {Program.d1, Program.M<int>, GetP().d2, int.Parse, NT::Program.M2}));
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_019_JaggedArray()
        {
            // JaggedArray in implicitly typed arrays

            var source = @"
using System;

namespace Test
{    
    public class Program
    {
        public static void Main()
        {        
            var a3 = new [] { new [] {new [] {1}}, 
                                         new [] {new int[] {2}}, 
                                         new int[][] {new int[] {3}}, 
                                         new int[][] {new [] {4}} };
            if ( !((a3 is int[][][]) && (a3.Length == 4)) )
            {
                Console.Write(""Test fail"");          
            }
            else
            {
                Console.Write(""True"");
            }            
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_020_MultiDimensionalArray()
        {
            // MultiDimensionalArray in implicitly typed arrays

            var testSrc = @"
using System;

namespace Test
{    
    public class Program
    {
        public static void Main()
        {            
            var a3 = new [] { new int[,,] {{{3, 4}}}, 
                              new int[,,] {{{3, 4}}} };
            if ( !((a3 is int[][,,]) && (a3.Rank == 1) && (a3.Length == 2)) )
            {
                Console.WriteLine(0);
            }
            else
            {
                Console.WriteLine(1);
            }
        }
    }
}";

            CompileAndVerify(testSrc, expectedOutput: "1");
        }

        [Fact]
        public void Test_021_MultiDimensionalArray_02()
        {
            // Implicitly typed arrays should can be used in creating MultiDimensionalArray

            var testSrc = @"
using System;

namespace Test
{    
    public class Program
    {
        public static void Main()
        {           
            var a3 = new [,,] { {{2, 3, 4}}, {{2, 3, 4}} };
            if ( !((a3 is int[,,]) && (a3.Rank == 3) && (a3.Length == 6)) )
            {
                Console.WriteLine(0);
            }
            else
            {
                Console.WriteLine(1);
            }
        }
    }
}
";
            CompileAndVerify(testSrc, expectedOutput: "1");
        }

        [Fact]
        public void Test_022_GenericMethod()
        {
            //  Implicitly typed arrays used in generic method

            var source = @"
using System.Linq;

namespace Test
{   
    public class Program
    {
        public static void Main()
        {
            System.Console.Write(GM(new [] {1, 2, 3}).SequenceEqual(new int[]{1, 2, 3}));
        }
    
        public static T GM<T>(T t)
        {
           return t;
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_023_Query()
        {
            //  Query expression as element in implicitly typed arrays 

            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Program
{        
     public static void Main()
     {            
        int[] ta = new int[] {1, 2, 3, 4, 5};
        IEnumerable i = ta;
                        
        IEnumerable[] a = new [] {i, from c in ta select c, from c in ta group c by c, 
                                  from c in ta select c into g select g, from c in ta where c==3 select c, 
                                  from c in ta orderby c select c, from c in ta orderby c ascending select c, 
                                  from c in ta orderby c descending select c, from c in ta where c==3 orderby c select c};        
 
        Console.Write((a is IEnumerable[]) && (a.Length==9));
        }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [Fact]
        public void Test_023_Literal()
        {
            //  Query expression as element in implicitly typed arrays 

            var source = @"
using System;
using System.Linq;

public class Program
{
    public static void Main()
    {          
            Console.Write(new [] {true, false}.SequenceEqual(new bool[] {true, false}));
            Console.Write(new [] {0123456789U, 1234567890U, 2345678901u, 3456789012UL, 4567890123Ul, 5678901234UL, 6789012345Ul, 7890123456uL, 8901234567ul, 9012345678LU, 9123456780Lu, 1234567809LU, 2345678091LU}.SequenceEqual(
                          new ulong[] {0123456789U, 1234567890U, 2345678901u, 3456789012UL, 4567890123Ul, 5678901234UL, 6789012345Ul, 7890123456uL, 8901234567ul, 9012345678LU, 9123456780Lu, 1234567809LU, 2345678091LU}));
            Console.Write(new [] {0123456789, 1234567890, 2345678901, 3456789012L, 4567890123L, 5678901234L, 6789012345L, 7890123456L, 8901234567L, 9012345678L, 9123456780L, 1234567809L, 2345678091L}.SequenceEqual( 
                          new long[] {0123456789, 1234567890, 2345678901, 3456789012L, 4567890123L, 5678901234L, 6789012345L, 7890123456L, 8901234567L, 9012345678L, 9123456780L, 1234567809L, 2345678091L}));
            Console.Write(new [] {0x012345U, 0x6789ABU, 0xCDEFabU, 0xcdef01U, 0X123456U, 0x12579BU, 0x13579Bu, 0x14579BLU, 0x15579BLU, 0x16579BUL, 0x17579BUl, 0x18579BuL, 0x19579Bul, 0x1A579BLU, 0x1B579BLu, 0x1C579BLU, 0x1C579BLu}.SequenceEqual( 
                          new ulong[] {0x012345U, 0x6789ABU, 0xCDEFabU, 0xcdef01U, 0X123456U, 0x12579BU, 0x13579Bu, 0x14579BLU, 0x15579BLU, 0x16579BUL, 0x17579BUl, 0x18579BuL, 0x19579Bul, 0x1A579BLU, 0x1B579BLu, 0x1C579BLU, 0x1C579BLu}));
            Console.Write(new [] {0x012345, 0x6789AB, 0xCDEFab, 0xcdef01, 0X123456, 0x12579B, 0x13579B, 0x14579BL, 0x15579BL, 0x16579BL, 0x17579BL, 0x18579BL, 0x19579BL, 0x1A579BL, 0x1B579BL, 0x1C579BL, 0x1C579BL}.SequenceEqual(
                          new long[] {0x012345, 0x6789AB, 0xCDEFab, 0xcdef01, 0X123456, 0x12579B, 0x13579B, 0x14579BL, 0x15579BL, 0x16579BL, 0x17579BL, 0x18579BL, 0x19579BL, 0x1A579BL, 0x1B579BL, 0x1C579BL, 0x1C579BL}));
            Console.Write(new [] {0123456789F, 1234567890f, 2345678901D, 3456789012d, 3.2, 3.3F, 3.4e5, 3.5E5, 3.6E+5, 3.7E-5, 3.8e+5, 3.9e-5, 3.22E5D, .234, .23456D, .245e3, .2334e7D, 3E5, 3E-5, 3E+6, 4E4D}.SequenceEqual(
                          new double[] {0123456789F, 1234567890f, 2345678901D, 3456789012d, 3.2, 3.3F, 3.4e5, 3.5E5, 3.6E+5, 3.7E-5, 3.8e+5, 3.9e-5, 3.22E5D, .234, .23456D, .245e3, .2334e7D, 3E5, 3E-5, 3E+6, 4E4D})) ;    
            Console.Write(new [] {0123456789M, 1234567890m, 3.3M, 3.22E5M, 3.2E+4M, 3.3E-4M, .234M, .245e3M, .2334e+7M, .24e-3M, 3E5M, 3E-5M, 3E+6M}.SequenceEqual(
                          new decimal[] {0123456789M, 1234567890m, 3.3M, 3.22E5M, 3.2E+4M, 3.3E-4M, .234M, .245e3M, .2334e+7M, .24e-3M, 3E5M, 3E-5M, 3E+6M}));
            Console.Write(new [] {0123456789, 5678901234UL, 2345678901D, 3.2, 3.4E5, 3.6E+5, 3.7E-5, 3.22E5D, .234, .23456D, .245E3, 3E5, 4E4D}.SequenceEqual(
                          new double[] {0123456789, 5678901234UL, 2345678901D, 3.2, 3.4E5, 3.6E+5, 3.7E-5, 3.22E5D, .234, .23456D, .245E3, 3E5, 4E4D}));
            Console.Write(new [] {0123456789, 5678901234UL, 2345678901M, 3.2M, 3.4E5M, 3.6E+5M, 3.7E-5M, .234M, .245E3M, 3E5M}.SequenceEqual(
                          new decimal[] {0123456789, 5678901234UL, 2345678901M, 3.2M, 3.4E5M, 3.6E+5M, 3.7E-5M, .234M, .245E3M, 3E5M}));
   }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "TrueTrueTrueTrueTrueTrueTrueTrueTrue");
        }

        #endregion

        #region "Error tests"

        [Fact]
        public void Error_NonArrayInitExpr()
        {
            var testSrc = @"
namespace Test
{
    public class Program
    {
        public void Goo()
        {
            var a3 = new[,,] { { { 3, 4 } }, 3, 4 };
        }
    }
}
";
            var comp = CreateCompilation(testSrc);
            comp.VerifyDiagnostics(
                // (8,46): error CS0846: A nested array initializer is expected
                //             var a3 = new[,,] { { { 3, 4 } }, 3, 4 };
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "3").WithLocation(8, 46),
                // (8,49): error CS0846: A nested array initializer is expected
                //             var a3 = new[,,] { { { 3, 4 } }, 3, 4 };
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "4").WithLocation(8, 49));
        }

        [Fact]
        public void Error_NonArrayInitExpr_02()
        {
            var testSrc = @"
namespace Test
{
    public class Program
    {
        public void Goo()
        {
            var a3 = new[,,] { { { 3, 4 } }, x, 4 };
        }
    }
}
";
            var comp = CreateCompilation(testSrc);
            comp.VerifyDiagnostics(
                // (8,46): error CS0103: The name 'x' does not exist in the current context
                //             var a3 = new[,,] { { { 3, 4 } }, x, 4 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(8, 46),
                // (8,49): error CS0846: A nested array initializer is expected
                //             var a3 = new[,,] { { { 3, 4 } }, x, 4 };
                Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "4").WithLocation(8, 49));
        }

        [WorkItem(543571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543571")]
        [Fact]
        public void CS0826ERR_ImplicitlyTypedArrayNoBestType()
        {
            var text = @"
using System;

namespace Test
{
    public class Program
    {
        enum E
        {
            Zero,
            FortyTwo = 42
        };

        public static void Main()
        {
            E[] a = new[] { E.FortyTwo, 0 }; // Dev10 error CS0826
            Console.WriteLine(String.Format(""Type={0}, a[0]={1}, a[1]={2}"", a.GetType(), a[0], a[1]));
        }
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (16,21): error CS0826: No best type found for implicitly-typed array
                //             E[] a = new[] { E.FortyTwo, 0 }; // Dev10 error CS0826
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { E.FortyTwo, 0 }").WithLocation(16, 21));
        }

        #endregion
    }
}
