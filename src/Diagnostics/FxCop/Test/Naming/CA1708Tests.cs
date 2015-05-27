// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AnalyzerPowerPack;
using Microsoft.AnalyzerPowerPack.Naming;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    public class CA1708Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA1708DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA1708DiagnosticAnalyzer();
        }

        #region Namespace Level

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestGlobalNamespaceNames()
        {
            VerifyCSharp(new string[]
                {
                @"
namespace N
{
    public class C { }
}
namespace n
{
    public class C { }
}
"
                },
                GetCA1708CSharpResult(Namespace, GetSymbolDisplayString("n", "N")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestNestedNamespaceNames()
        {
            VerifyCSharp(new string[]
                {
                @"
namespace N
{
    class C { }
    namespace n 
    {
        public class C { }
    }
    namespace n { }
}

namespace n
{
    public class C { }
    namespace n
    {
        public class C { }
    }
}
"
                },
                GetCA1708CSharpResult(Namespace, GetSymbolDisplayString("n", "N")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestGlobalTypeNames()
        {
            VerifyCSharp(new string[]
                {
                    @"
public class Ni
{
}
public struct ni
{
}
public interface nI
{
}
"
                },
                GetCA1708CSharpResult(Type, GetSymbolDisplayString("nI", "ni", "Ni")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestGenericClasses()
        {
            VerifyCSharp(new string[]
                {
                    @"
public class C<T>
{
}
public class c<S>
{
}
public class c
{
}
public class C<T,X>
{
}
"
                },
                GetCA1708CSharpResult(Type, GetSymbolDisplayString("c<S>", "C<T>")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestPartialTypes()
        {
            VerifyCSharp(new string[]
                {
                    @"
namespace N
{
    public partial class C
    {
        public int x;
    }
    public partial class C
    {
        public int X;
    }
    public partial class F
    {
        public int x;    
    }
}
",
                    @"
namespace N
{
    public class c
    {
    }
    public partial class F
    {
        public int X;
    }
}"
                },
                GetCA1708CSharpResult(Type, GetSymbolDisplayString("N.C", "N.c")),
                GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("N.C.x", "N.C.X"), "Test0.cs(4,26)", "Test0.cs(8,26)"),
                GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("N.F.x", "N.F.X"), "Test0.cs(12,26)", "Test1.cs(7,26)"));
        }

        #endregion

        #region Type Level

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestNestedTypeNames()
        {
            VerifyCSharp(@"
namespace NI
{
    public class Ni
    {    
        public struct Nd { }
        public delegate void nd();
        public class C
        {
            public struct nD { }
            public class nd
            {
                public class CI { }
                public struct ci
                {
                    public int x;
                    public void X() { }
                }
                public interface Ci 
                {
                    void foo();
                    void Foo();
                }
           }
        }
    }
   
    class NI
    {
        public class N 
        {
        }
        public class n { }
    }
}
",
            GetCA1708CSharpResult(Type, GetSymbolDisplayString("NI.Ni", "NI.NI")),
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("NI.Ni.Nd", "NI.Ni.nd"), 4, 18),
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("NI.Ni.C.nD", "NI.Ni.C.nd"), 8, 22),
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("NI.Ni.C.nd.CI", "NI.Ni.C.nd.ci", "NI.Ni.C.nd.Ci"), 11, 26),
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("NI.Ni.C.nd.ci.x", "NI.Ni.C.nd.ci.X()"), 14, 31),
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("NI.Ni.C.nd.Ci.foo()", "NI.Ni.C.nd.Ci.Foo()"), 19, 34));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestNestedTypeNamesWithScope()
        {
            VerifyCSharp(@"
using System;
namespace NI
{
    public class Ni
    {    
        public struct Nd { }
        public delegate void nd();
        public class C
        {
            public struct nD { }
            public class nd
            {
                public class CI { }
                public struct ci
                {
                    public int x;
                    public void X() { }
                }
                public interface Ci 
                {
                    public void foo();
                    public void Foo();
                }
           }
        }
    }
   
    [|class NI
    {
        public class N 
        {
        }
        public class n { }
    }|]
}
",
            GetCA1708CSharpResult(Type, GetSymbolDisplayString("NI.Ni", "NI.NI")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestMethodOverloads()
        {
            VerifyCSharp(@"
namespace NI
{
    public class C
    {
        public void foo() { }
        public void foo(int x) { }
        public void foo<T>(T x) { }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestGenericMethods()
        {
            VerifyCSharp(@"
namespace NI
{
    public class C
    {
        public void foo() { }
        public void foO(int x) { }
        public void fOo(int x) { }
        public void FOO<T>(T x) { }
        public void fOo<T, X>(T x, X y) { }
    }
}
",
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("NI.C.foo()", "NI.C.foO(int)", "NI.C.fOo(int)", "NI.C.FOO<T>(T)"), 4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestMembers()
        {
            VerifyCSharp(@"
namespace NI
{
    public class CASE1
    {
        public int Case1;
        void CAse1() { caSE1(); }
        public void CAse1(int x) { }
        public void CaSe1<T>(T x) { }
        public delegate void CASe1();
        public interface CasE1 { }
        public event CASe1 caSE1;
        public int CAsE1
        {
            get { return Case1; }
            set { Case1 = value; }
        }
        public CASE1() { }
        public int this[int x]
        {
            get { return x; }
            set { }    
        }
        ~CASE1() { }
        static CASE1() { }
        public static int operator +(CASE1 y, int x) { return 1; }
    }
}
",
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayStringNoSorting("NI.CASE1.CASe1", "NI.CASE1.CAsE1", "NI.CASE1.CAse1(int)", "NI.CASE1.CaSe1<T>(T)", "NI.CASE1.CasE1", "NI.CASE1.Case1", "NI.CASE1.caSE1"), 4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestCultureSpecificNames()
        {
            VerifyCSharp(@"
public class C
{
    public int γ;
    public int Γ;
}
",
            GetCA1708CSharpResultAt(Member, GetSymbolDisplayString("C.γ", "C.Γ"), 2, 14));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void TestParameters()
        {
            VerifyCSharp(@"
namespace N
{
    public class C
    {
        int x;
        public delegate void Delegate(int a, int A);
        public void Method(int b, int B) {}
        public C(int d, int D)
        {
        }
        public static int operator +(C e, int E) { return 1; }
        public int X
        {
            get { return x; }
            set { x = value; }
        }
        public int this[int f, int F]
        {
            get { return x; }
            set { }
        }
        public System.Action<int, int> action = (a, A) => { };
    }
    public partial class D
    {
    }
    public partial class D
    {
        public delegate void Foo(int x, int X);
    }
}
",
            GetCA1708CSharpResultAt(Parameter, "N.C.Delegate", 7, 30),
            GetCA1708CSharpResultAt(Parameter, "N.C.Method(int, int)", 8, 21),
            GetCA1708CSharpResultAt(Parameter, "N.C.C(int, int)", 9, 16),
            GetCA1708CSharpResultAt(Parameter, "N.C.operator +(N.C, int)", 12, 36),
            GetCA1708CSharpResultAt(Parameter, "N.C.this[int, int]", 18, 20),
            GetCA1708CSharpResultAt(Parameter, "N.D.Foo", 30, 30));
        }

        #endregion

        #region Helper Methods

        private const string RuleName = CA1708DiagnosticAnalyzer.RuleId;
        private static readonly string s_message = AnalyzerPowerPackRulesResources.IdentifierNamesShouldDifferMoreThanCase;

        private const string Namespace = CA1708DiagnosticAnalyzer.Namespace;
        private const string Type = CA1708DiagnosticAnalyzer.Type;
        private const string Member = CA1708DiagnosticAnalyzer.Member;
        private const string Parameter = CA1708DiagnosticAnalyzer.Parameter;

        private static string GetSymbolDisplayString(params string[] objectName)
        {
            return string.Join(", ", objectName.OrderByDescending(x => x, StringComparer.InvariantCulture));
        }

        private static string GetSymbolDisplayStringNoSorting(params string[] objectName)
        {
            return string.Join(", ", objectName);
        }

        private static DiagnosticResult GetCA1708CSharpResult(string typeName, string objectName)
        {
            return GetGlobalResult(RuleName, string.Format(s_message, typeName, objectName));
        }

        private static DiagnosticResult GetCA1708BasicResult(string typeName, string objectName)
        {
            return GetGlobalResult(RuleName, string.Format(s_message, typeName, objectName));
        }

        private static DiagnosticResult GetCA1708CSharpResultAt(string typeName, string objectName, int line, int column)
        {
            return GetCSharpResultAt(line, column, RuleName, string.Format(s_message, typeName, objectName));
        }

        private static DiagnosticResult GetCA1708BasicResultAt(string typeName, string objectName, int line, int column)
        {
            return GetBasicResultAt(line, column, RuleName, string.Format(s_message, typeName, objectName));
        }

        private static DiagnosticResult GetCA1708CSharpResultAt(string typeName, string objectName, params string[] locations)
        {
            return GetCSharpResultAt(RuleName, string.Format(s_message, typeName, objectName), locations);
        }

        #endregion
    }
}
