// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

// Note: the easiest way to create new unit tests that use GetSemanticInfo
// is to use the SemanticInfo unit test generate in Editor Test App.

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    using Utils = CompilationUtils;

    public class SemanticModelGetSemanticInfoTests : SemanticModelTestBase
    {
        [Fact]
        public void FailedOverloadResolution()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 8;
        int j = i + q;
        /*<bind>*/X.f/*</bind>*/(""hello"");
    }
}

class X
{
    public static void f() { }
    public static void f(int i) { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void X.f()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("void X.f(System.Int32 i)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void X.f()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("void X.f(System.Int32 i)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SimpleGenericType()
        {
            string sourceCode = @"
using System;

class Program
{
    /*<bind>*/K<int>/*</bind>*/ f;
}

class K<T>
{ }
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("K<System.Int32>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("K<System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("K<System.Int32>", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void WrongArity1()
        {
            string sourceCode = @"
using System;

class Program
{
    /*<bind>*/K<int, string>/*</bind>*/ f;
}

class K<T>
{ }
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("K<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("K<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.WrongArity, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("K<T>", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void WrongArity2()
        {
            string sourceCode = @"
using System;

class Program
{
    /*<bind>*/K/*</bind>*/ f;
}

class K<T>
{ }
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("K<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("K<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.WrongArity, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("K<T>", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void WrongArity3()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        /*<bind>*/K<int, int>/*</bind>*/.f();
    }

}

class K<T>
{
    void f() { }
}

";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("K<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("K<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.WrongArity, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("K<T>", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void WrongArity4()
        {
            string sourceCode = @"

using System;

class Program
{
    static K Main()
    {
        /*<bind>*/K/*</bind>*/.f();
    }

}

class K<T>
{
    void f() { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("K<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("K<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.WrongArity, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("K<T>", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NotInvocable()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        K./*<bind>*/f/*</bind>*/();
    }

}

class K
{
    public int f;
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotInvocable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 K.f", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InaccessibleField()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        K./*<bind>*/f/*</bind>*/ = 3;
    }
}

class K
{
    private int f;
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 K.f", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InaccessibleFieldAssignment()
        {
            string sourceCode =
@"class A
{
    string F;
}
class B
{
    static void M(A a)
    {
        /*<bind>*/a.F/*</bind>*/ = string.Empty;
    }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.Equal(SpecialType.System_String, semanticInfo.Type.SpecialType);
            var symbol = semanticInfo.Symbol;
            Assert.Null(symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            symbol = semanticInfo.CandidateSymbols[0];
            Assert.Equal("System.String A.F", symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, symbol.Kind);
        }

        [WorkItem(542481, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542481")]
        [Fact]
        public void InaccessibleBaseClassConstructor01()
        {
            string sourceCode = @"
namespace Test
{
    public class Base
    {
        protected Base() { }
    }

    public class Derived : Base
    {
        void M()
        {
            Base b = /*<bind>*/new Base()/*</bind>*/;
        }
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Test.Base..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
        }

        [WorkItem(542481, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542481")]
        [Fact]
        public void InaccessibleBaseClassConstructor02()
        {
            string sourceCode = @"
namespace Test
{
    public class Base
    {
        protected Base() { }
    }

    public class Derived : Base
    {
        void M()
        {
            Base b = new /*<bind>*/Base/*</bind>*/();
        }
    }
}";
            var semanticInfo = GetSemanticInfoForTest<NameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);

            Assert.Equal("Test.Base", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MemberGroup.Length);
        }

        [Fact]
        public void InaccessibleFieldMethodArg()
        {
            string sourceCode =
@"class A
{
    string F;
}
class B
{
    static void M(A a)
    {
        M(/*<bind>*/a.F/*</bind>*/);
    }
    static void M(string s) { }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.Equal(SpecialType.System_String, semanticInfo.Type.SpecialType);
            var symbol = semanticInfo.Symbol;
            Assert.Null(symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            symbol = semanticInfo.CandidateSymbols[0];
            Assert.Equal("System.String A.F", symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, symbol.Kind);
        }

        [Fact]
        public void TypeNotAVariable()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        /*<bind>*/K/*</bind>*/ = 12;
    }

}

class K
{
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("K", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("K", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAVariable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("K", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InaccessibleType1()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        /*<bind>*/K.J/*</bind>*/ = v;
    }

}

class K
{
    protected class J { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("?", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("K.J", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbiguousTypesBetweenUsings1()
        {
            string sourceCode = @"
using System;
using N1;
using N2;

class Program
{
    /*<bind>*/A/*</bind>*/ field;
}

namespace N1
{
    class A { }
}

namespace N2
{
    class A { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("N1.A", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.A", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("N1.A", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("N2.A", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbiguousTypesBetweenUsings2()
        {
            string sourceCode = @"
using System;
using N1;
using N2;

class Program
{
    void f()
    {
        /*<bind>*/A/*</bind>*/.g();
    }
}

namespace N1
{
    class A { }
}

namespace N2
{
    class A { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("N1.A", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.A", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("N1.A", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("N2.A", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbiguousTypesBetweenUsings3()
        {
            string sourceCode = @"
using System;
using N1;
using N2;

class Program
{
    void f()
    {
        /*<bind>*/A<int>/*</bind>*/.g();
    }
}

namespace N1
{
    class A<T> { }
}

namespace N2
{
    class A<U> { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("N1.A<System.Int32>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.A<System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("N1.A<T>", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("N2.A<U>", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbiguityBetweenInterfaceMembers()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

interface I1
{
    public int P { get; }
}

interface I2
{
    public string P { get; }
}

interface I3 : I1, I2
{ }

public class Class1
{
    void f()
    {
        I3 x = null;
        int o = x./*<bind>*/P/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("I1.P", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 I1.P { get; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);
            Assert.Equal("System.String I2.P { get; }", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void Alias1()
        {
            string sourceCode = @"
using O = System.Object;

partial class A : /*<bind>*/O/*</bind>*/ {}

";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.NotNull(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.NotNull(aliasInfo);
            Assert.Equal("O=System.Object", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void Alias2()
        {
            string sourceCode = @"
using O = System.Object;

partial class A {
    void f()
    {
        /*<bind>*/O/*</bind>*/.ReferenceEquals(null, null);
    }
}

";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            var aliasInfo = GetAliasInfoForTest(sourceCode);

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.NotNull(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal("O=System.Object", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(539002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539002")]
        [Fact]
        public void IncompleteGenericMethodCall()
        {
            string sourceCode = @"
class Array
{
  public static void Find<T>(T t) { }
}
class C
{
  static void Main()
  {
    /*<bind>*/Array.Find<int>/*</bind>*/(
  }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void Array.Find<System.Int32>(System.Int32 t)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void Array.Find<System.Int32>(System.Int32 t)", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void IncompleteExtensionMethodCall()
        {
            string sourceCode =
@"interface I<T> { }
class A { }
class B : A { }
class C
{
    static void M(A a)
    {
        /*<bind>*/a.M/*</bind>*/(
    }
}
static class S
{
    internal static void M(this object o, int x) { }
    internal static void M(this A a, int x, int y) { }
    internal static void M(this B b) { }
    internal static void M(this string s) { }
    internal static void M<T>(this T t, object o) { }
    internal static void M<T>(this T[] t) { }
    internal static void M<T, U>(this T x, I<T> y, U z) { }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.Null(semanticInfo.Symbol);
            Utils.CheckISymbols(semanticInfo.MethodGroup,
                "void object.M(int x)",
                "void A.M(int x, int y)",
                "void A.M<A>(object o)",
                "void A.M<A, U>(I<A> y, U z)");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols,
                "void object.M(int x)",
                "void A.M(int x, int y)",
                "void A.M<A>(object o)",
                "void A.M<A, U>(I<A> y, U z)");
            Utils.CheckReducedExtensionMethod(semanticInfo.MethodGroup[3].GetSymbol(),
                "void A.M<A, U>(I<A> y, U z)",
                "void S.M<T, U>(T x, I<T> y, U z)",
                "void T.M<T, U>(I<T> y, U z)",
                "void S.M<T, U>(T x, I<T> y, U z)");
        }

        [Fact]
        public void IncompleteExtensionMethodCallBadThisType()
        {
            string sourceCode =
@"interface I<T> { }
class B
{
    static void M(I<A> a)
    {
        /*<bind>*/a.M/*</bind>*/(
    }
}
static class S
{
    internal static void M(this object o) { }
    internal static void M<T>(this T t, object o) { }
    internal static void M<T>(this T[] t) { }
    internal static void M<T, U>(this I<T> x, I<T> y, U z) { }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Utils.CheckISymbols(semanticInfo.MethodGroup,
                "void object.M()",
                "void I<A>.M<I<A>>(object o)",
                "void I<A>.M<A, U>(I<A> y, U z)");
        }

        [WorkItem(541141, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541141")]
        [Fact]
        public void IncompleteGenericExtensionMethodCall()
        {
            string sourceCode =
@"using System.Linq;
class C
{
    static void M(double[] a)
    {
        /*<bind>*/a.Where/*</bind>*/(
    }
}";
            var compilation = CreateCompilation(source: sourceCode);
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Utils.CheckISymbols(semanticInfo.MethodGroup,
                "IEnumerable<double> IEnumerable<double>.Where<double>(Func<double, bool> predicate)",
                "IEnumerable<double> IEnumerable<double>.Where<double>(Func<double, int, bool> predicate)");
        }

        [WorkItem(541349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541349")]
        [Fact]
        public void GenericExtensionMethodCallExplicitTypeArgs()
        {
            string sourceCode =
@"interface I<T> { }
class C
{
    static void M(object o)
    {
        /*<bind>*/o.E<int>/*</bind>*/();
    }
}
static class S
{
    internal static void E(this object x, object y) { }
    internal static void E<T>(this object o) { }
    internal static void E<T>(this object o, T t) { }
    internal static void E<T>(this I<T> t) { }
    internal static void E<T, U>(this I<T> t) { }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Utils.CheckSymbol(semanticInfo.Symbol,
                "void object.E<int>()");
            Utils.CheckISymbols(semanticInfo.MethodGroup,
                "void object.E<int>()",
                "void object.E<int>(int t)");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);
        }

        [Fact]
        public void GenericExtensionMethodCallExplicitTypeArgsOfT()
        {
            string sourceCode =
@"interface I<T> { }
class C
{
    static void M<T, U>(T t, U u)
    {
        /*<bind>*/t.E<T, U>/*</bind>*/(u);
    }
}
static class S
{
    internal static void E(this object x, object y) { }
    internal static void E<T>(this object o) { }
    internal static void E<T, U>(this T t, U u) { }
    internal static void E<T, U>(this I<T> t, U u) { }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Utils.CheckISymbols(semanticInfo.MethodGroup,
                "void T.E<T, U>(U u)");
        }

        [WorkItem(541297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541297")]
        [Fact]
        public void GenericExtensionMethodCall()
        {
            // Single applicable overload with valid argument.
            var semanticInfo = GetSemanticInfoForTest(
@"class C
{
    static void M(string s)
    {
        /*<bind>*/s.E(s)/*</bind>*/;
    }
}
static class S
{
    internal static void E<T>(this T x, object y) { }
    internal static void E<T, U>(this T x, U y) { }
}");
            Utils.CheckSymbol(semanticInfo.Symbol,
                "void string.E<string, string>(string y)");
            Utils.CheckISymbols(semanticInfo.MethodGroup);
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);

            // Multiple applicable overloads with valid arguments.
            semanticInfo = GetSemanticInfoForTest(
@"class C
{
    static void M(string s, object o)
    {
        /*<bind>*/s.E(s, o)/*</bind>*/;
    }
}
static class S
{
    internal static void E<T>(this object x, T y, object z) { }
    internal static void E<T, U>(this T x, object y, U z) { }
}");
            Assert.Null(semanticInfo.Symbol);
            Utils.CheckISymbols(semanticInfo.MethodGroup);
            Utils.CheckISymbols(semanticInfo.CandidateSymbols,
                "void object.E<string>(string y, object z)",
                "void string.E<string, object>(object y, object z)");

            // Multiple applicable overloads with error argument.
            semanticInfo = GetSemanticInfoForTest(
@"class C
{
    static void M(string s)
    {
        /*<bind>*/s.E(t, s)/*</bind>*/;
    }
}
static class S
{
    internal static void E<T>(this T x, T y, object z) { }
    internal static void E<T, U>(this T x, string y, U z) { }
}");
            Assert.Null(semanticInfo.Symbol);
            Utils.CheckISymbols(semanticInfo.MethodGroup);
            Utils.CheckISymbols(semanticInfo.CandidateSymbols,
                "void string.E<string>(string y, object z)",
                "void string.E<string, string>(string y, string z)");

            // Multiple overloads but all inaccessible.
            semanticInfo = GetSemanticInfoForTest(
@"class C
{
    static void M(string s)
    {
        /*<bind>*/s.E()/*</bind>*/;
    }
}
static class S
{
    static void E(this string x) { }
    static void E<T>(this T x) { }
}");
            Assert.Null(semanticInfo.Symbol);
            Utils.CheckISymbols(semanticInfo.MethodGroup);
            Utils.CheckISymbols(semanticInfo.CandidateSymbols
                /* no candidates */
                );
        }

        [Fact]
        public void GenericExtensionDelegateMethod()
        {
            // Single applicable overload.
            var semanticInfo = GetSemanticInfoForTest(
@"class C
{
    static void M(string s)
    {
        System.Action<string> a = /*<bind>*/s.E/*</bind>*/;
    }
}
static class S
{
    internal static void E<T>(this T x, T y) { }
    internal static void E<T>(this object x, T y) { }
}");
            Utils.CheckSymbol(semanticInfo.Symbol,
                "void string.E<string>(string y)");
            Utils.CheckISymbols(semanticInfo.MethodGroup,
                "void string.E<string>(string y)",
                "void object.E<T>(T y)");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);

            // Multiple applicable overloads.
            semanticInfo = GetSemanticInfoForTest(
@"class C
{
    static void M(string s)
    {
        System.Action<string> a = /*<bind>*/s.E/*</bind>*/;
    }
}
static class S
{
    internal static void E<T>(this T x, T y) { }
    internal static void E<T, U>(this T x, U y) { }
}");
            Assert.Null(semanticInfo.Symbol);
            Utils.CheckISymbols(semanticInfo.MethodGroup,
                "void string.E<string>(string y)",
                "void string.E<string, U>(U y)");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols,
                "void string.E<string>(string y)",
                "void string.E<string, U>(U y)");
        }

        /// <summary>
        /// Overloads from different scopes should
        /// be included in method group.
        /// </summary>
        [WorkItem(541890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541890")]
        [Fact]
        public void IncompleteExtensionOverloadedDifferentScopes()
        {
            // Instance methods and extension method (implicit instance).
            var sourceCode =
@"class C
{
    void M()
    {
        /*<bind>*/F/*</bind>*/(
    }
    void F(int x) { }
    void F(object x, object y) { }
}
static class E
{
    internal static void F(this object x, object y) { }
}";
            var compilation = (Compilation)CreateCompilation(source: sourceCode);
            var type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            var symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void C.F(int x)",
                "void C.F(object x, object y)");
            symbols = model.LookupSymbols(expr.SpanStart, container: null, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void C.F(int x)",
                "void C.F(object x, object y)");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void C.F(int x)",
                "void C.F(object x, object y)",
                "void object.F(object y)");

            // Instance methods and extension method (explicit instance).
            sourceCode =
@"class C
{
    void M()
    {
        /*<bind>*/this.F/*</bind>*/(
    }
    void F(int x) { }
    void F(object x, object y) { }
}
static class E
{
    internal static void F(this object x, object y) { }
}";
            compilation = (Compilation)CreateCompilation(source: sourceCode);
            type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void C.F(int x)",
                "void C.F(object x, object y)",
                "void object.F(object y)");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void C.F(int x)",
                "void C.F(object x, object y)",
                "void object.F(object y)");

            // Applicable instance method and inapplicable extension method.
            sourceCode =
@"class C
{
    void M()
    {
        /*<bind>*/this.F<string>/*</bind>*/(
    }
    void F<T>(T t) { }
}
static class E
{
    internal static void F(this object x) { }
}";
            compilation = (Compilation)CreateCompilation(source: sourceCode);
            type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void C.F<string>(string t)");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void C.F<T>(T t)",
                "void object.F()");

            // Inaccessible instance method and accessible extension method.
            sourceCode =
@"class A
{
    void F() { }
}
class B : A
{
    static void M(A a)
    {
        /*<bind>*/a.F/*</bind>*/(
    }
}
static class E
{
    internal static void F(this object x) { }
}";
            compilation = (Compilation)CreateCompilation(source: sourceCode);
            type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("A");
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void object.F()");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void object.F()");

            // Inapplicable instance method and applicable extension method.
            sourceCode =
@"class C
{
    void M()
    {
        /*<bind>*/this.F<string>/*</bind>*/(
    }
    void F(object o) { }
}
static class E
{
    internal static void F<T>(this object x) { }
}";
            compilation = (Compilation)CreateCompilation(source: sourceCode);
            type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void object.F<string>()");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void C.F(object o)",
                "void object.F<T>()");

            // Viable instance and extension methods, binding to extension method.
            sourceCode =
@"class C
{
    void M()
    {
        /*<bind>*/this.F/*</bind>*/();
    }
    void F(object o) { }
}
static class E
{
    internal static void F(this object x) { }
}";
            compilation = (Compilation)CreateCompilation(source: sourceCode);
            type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void C.F(object o)",
                "void object.F()");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void C.F(object o)",
                "void object.F()");

            // Applicable and inaccessible extension methods.
            sourceCode =
@"class C
{
    void M(string s)
    {
        /*<bind>*/s.F<string>/*</bind>*/(
    }
}
static class E
{
    internal static void F(this object x, object y) { }
    internal static void F<T>(this T t) { }
}";
            compilation = (Compilation)CreateCompilation(source: sourceCode);
            type = compilation.GetSpecialType(SpecialType.System_String);
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void string.F<string>()");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void object.F(object y)",
                "void string.F<string>()");

            // Inapplicable and inaccessible extension methods.
            sourceCode =
@"class C
{
    void M(string s)
    {
        /*<bind>*/s.F<string>/*</bind>*/(
    }
}
static class E
{
    internal static void F(this object x, object y) { }
    private static void F<T>(this T t) { }
}";
            compilation = (Compilation)CreateCompilation(source: sourceCode);
            type = compilation.GetSpecialType(SpecialType.System_String);
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void string.F<string>()");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void object.F(object y)");

            // Multiple scopes.
            sourceCode =
@"namespace N1
{
    static class E
    {
        internal static void F(this object o) { }
    }
}
namespace N2
{
    using N1;
    class C
    {
        static void M(C c)
        {
            /*<bind>*/c.F/*</bind>*/(
        }
        void F(int x) { }
    }
    static class E
    {
        internal static void F(this object x, object y) { }
    }
}
static class E
{
    internal static void F(this object x, object y, object z) { }
}";
            compilation = CreateCompilation(source: sourceCode);
            type = compilation.GlobalNamespace.GetMember<INamespaceSymbol>("N2").GetMember<INamedTypeSymbol>("C");
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void C.F(int x)",
                "void object.F(object y)",
                "void object.F()",
                "void object.F(object y, object z)");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void C.F(int x)",
                "void object.F(object y)",
                "void object.F()",
                "void object.F(object y, object z)");

            // Multiple scopes, no instance methods.
            sourceCode =
@"namespace N
{
    class C
    {
        static void M(C c)
        {
            /*<bind>*/c.F/*</bind>*/(
        }
    }
    static class E
    {
        internal static void F(this object x, object y) { }
    }
}
static class E
{
    internal static void F(this object x, object y, object z) { }
}";
            compilation = CreateCompilation(source: sourceCode);
            type = compilation.GlobalNamespace.GetMember<INamespaceSymbol>("N").GetMember<INamedTypeSymbol>("C");
            tree = compilation.SyntaxTrees.First();
            model = compilation.GetSemanticModel(tree);
            expr = GetSyntaxNodeOfTypeForBinding<ExpressionSyntax>(GetSyntaxNodeList(tree));
            symbols = model.GetMemberGroup(expr);
            Utils.CheckISymbols(symbols,
                "void object.F(object y)",
                "void object.F(object y, object z)");
            symbols = model.LookupSymbols(expr.SpanStart, container: type, name: "F", includeReducedExtensionMethods: true);
            Utils.CheckISymbols(symbols,
                "void object.F(object y)",
                "void object.F(object y, object z)");
        }

        [ClrOnlyFact]
        public void PropertyGroup()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class A
    Property P(Optional x As Integer = 0) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Property P(x As Integer, y As Integer) As Integer
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Property P(x As Integer, y As String) As String
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Skipped);

            // Assignment (property group).
            var source2 =
@"class B
{
    static void M(A a)
    {
        /*<bind>*/a.P/*</bind>*/[1, null] = string.Empty;
    }
}";
            var compilation = CreateCompilation(source2, new[] { reference1 });
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Utils.CheckSymbol(semanticInfo.Symbol, "string A.P[int x, string y]");
            Utils.CheckISymbols(semanticInfo.MemberGroup,
                "object A.P[int x = 0]",
                "int A.P[int x, int y]",
                "string A.P[int x, string y]");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);

            // Assignment (property access).
            source2 =
@"class B
{
    static void M(A a)
    {
        /*<bind>*/a.P[1, null]/*</bind>*/ = string.Empty;
    }
}";
            compilation = CreateCompilation(source2, new[] { reference1 });
            semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Utils.CheckSymbol(semanticInfo.Symbol, "string A.P[int x, string y]");
            Utils.CheckISymbols(semanticInfo.MemberGroup);
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);

            // Object initializer.
            source2 =
@"class B
{
    static A F = new A() { /*<bind>*/P/*</bind>*/ = 1 };
}";
            compilation = CreateCompilation(source2, new[] { reference1 });
            semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Utils.CheckSymbol(semanticInfo.Symbol, "object A.P[int x = 0]");
            Utils.CheckISymbols(semanticInfo.MemberGroup);
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);

            // Incomplete reference, overload resolution failure (property group).
            source2 =
@"class B
{
    static void M(A a)
    {
        var o = /*<bind>*/a.P/*</bind>*/[1, a
    }
}";
            compilation = CreateCompilation(source2, new[] { reference1 });
            semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Assert.Null(semanticInfo.Symbol);
            Utils.CheckISymbols(semanticInfo.MemberGroup,
                "object A.P[int x = 0]",
                "int A.P[int x, int y]",
                "string A.P[int x, string y]");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols,
                "object A.P[int x = 0]",
                "int A.P[int x, int y]",
                "string A.P[int x, string y]");

            // Incomplete reference, overload resolution failure (property access).
            source2 =
@"class B
{
    static void M(A a)
    {
        var o = /*<bind>*/a.P[1, a/*</bind>*/
    }
}";
            compilation = CreateCompilation(source2, new[] { reference1 });
            semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Assert.Null(semanticInfo.Symbol);
            Utils.CheckISymbols(semanticInfo.MemberGroup);
            Utils.CheckISymbols(semanticInfo.CandidateSymbols,
                "object A.P[int x = 0]",
                "int A.P[int x, int y]",
                "string A.P[int x, string y]");
        }

        [ClrOnlyFact]
        public void PropertyGroupOverloadsOverridesHides()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Class A
    Overridable ReadOnly Property P1(index As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    ReadOnly Property P2(index As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    ReadOnly Property P2(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    ReadOnly Property P3(index As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    ReadOnly Property P3(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E212"")>
Public Class B
    Inherits A
    Overrides ReadOnly Property P1(index As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Shadows ReadOnly Property P2(index As String) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, verify: Verification.Skipped);

            // Overridden property.
            var source2 =
@"class C
{
    static object F(B b)
    {
        return /*<bind>*/b.P1/*</bind>*/[null];
    }
}";
            var compilation = CreateCompilation(source2, new[] { reference1 });
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Utils.CheckSymbol(semanticInfo.Symbol, "object B.P1[object index]");
            Utils.CheckISymbols(semanticInfo.MemberGroup, "object B.P1[object index]");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);

            // Hidden property.
            source2 =
@"class C
{
    static object F(B b)
    {
        return /*<bind>*/b.P2/*</bind>*/[null];
    }
}";
            compilation = CreateCompilation(source2, new[] { reference1 });
            semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Utils.CheckSymbol(semanticInfo.Symbol, "object B.P2[string index]");
            Utils.CheckISymbols(semanticInfo.MemberGroup, "object B.P2[string index]");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);

            // Overloaded property.
            source2 =
@"class C
{
    static object F(B b)
    {
        return /*<bind>*/b.P3/*</bind>*/[null];
    }
}";
            compilation = CreateCompilation(source2, new[] { reference1 });
            semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(compilation);
            Utils.CheckSymbol(semanticInfo.Symbol, "object A.P3[object index]");
            Utils.CheckISymbols(semanticInfo.MemberGroup, "object A.P3[object index]", "object A.P3[object x, object y]");
            Utils.CheckISymbols(semanticInfo.CandidateSymbols);
        }

        [WorkItem(538859, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538859")]
        [Fact]
        public void ThisExpression()
        {
            string sourceCode = @"
class C
{
    void M()
    {
        /*<bind>*/this/*</bind>*/.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C this", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538143")]
        [Fact]
        public void GetSemanticInfoOfNull()
        {
            var compilation = CreateCompilation("");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            Assert.Throws<ArgumentNullException>(() => model.GetSymbolInfo((ExpressionSyntax)null));
            Assert.Throws<ArgumentNullException>(() => model.GetTypeInfo((ExpressionSyntax)null));
            Assert.Throws<ArgumentNullException>(() => model.GetMemberGroup((ExpressionSyntax)null));
            Assert.Throws<ArgumentNullException>(() => model.GetConstantValue((ExpressionSyntax)null));

            Assert.Throws<ArgumentNullException>(() => model.GetSymbolInfo((ConstructorInitializerSyntax)null));
            Assert.Throws<ArgumentNullException>(() => model.GetTypeInfo((ConstructorInitializerSyntax)null));
            Assert.Throws<ArgumentNullException>(() => model.GetMemberGroup((ConstructorInitializerSyntax)null));
        }

        [WorkItem(537860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537860")]
        [Fact]
        public void UsingNamespaceName()
        {
            string sourceCode = @"
using /*<bind>*/System/*</bind>*/;

class Test
{
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3017, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void VariableUsedInForInit()
        {
            string sourceCode = @"
class Test
{
    void Fill()
    {
        for (int i = 0; /*<bind>*/i/*</bind>*/ < 10 ; i++ )
        {
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 i", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(527269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527269")]
        [Fact]
        public void NullLiteral()
        {
            string sourceCode = @"
class Test
{
    public static void Main()
    {
        string s = /*<bind>*/null/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Null(semanticInfo.ConstantValue.Value);
        }

        [WorkItem(3019, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void PostfixIncrement()
        {
            string sourceCode = @"
class Test
{
    public static void Main()
    {
        int i = 10;
        /*<bind>*/i++/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 System.Int32.op_Increment(System.Int32 value)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3199, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void ConditionalOrExpr()
        {
            string sourceCode = @"
class Program
{
  static void T1()
  {
      bool result = /*<bind>*/true || true/*</bind>*/;
  }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(true, semanticInfo.ConstantValue);
        }

        [WorkItem(3222, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void ConditionalOperExpr()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        int i = /*<bind>*/(true ? 0 : 1)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(0, semanticInfo.ConstantValue);
        }

        [WorkItem(3223, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void DefaultValueExpr()
        {
            string sourceCode = @"
class Test
{
    static void Main(string[] args)
    {
        int s = /*<bind>*/default(int)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(0, semanticInfo.ConstantValue);
        }

        [WorkItem(537979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537979")]
        [Fact]
        public void StringConcatWithInt()
        {
            string sourceCode = @"
public class Test
{
    public static void Main(string[] args)
    {
        string str =  /*<bind>*/""Count value is: "" + 5/*</bind>*/ ;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String System.String.op_Addition(System.String left, System.Object right)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3226, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void StringConcatWithIntAndNullableInt()
        {
            string sourceCode = @"
public class Test
{
    public static void Main(string[] args)
    {
        string str = /*<bind>*/""Count value is: "" + (int?) 10 + 5/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String System.String.op_Addition(System.String left, System.Object right)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3234, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void AsOper()
        {
            string sourceCode = @"
public class Test
{
    public static void Main(string[] args)
    {
        object o = null;
        string str = /*<bind>*/o as string/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537983")]
        [Fact]
        public void AddWithUIntAndInt()
        {
            string sourceCode = @"
public class Test
{
    public static void Main(string[] args)
    {
        uint ui = 0;
        ulong ui2 = /*<bind>*/ui + 5/*</bind>*/ ;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.UInt32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.UInt64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.UInt32 System.UInt32.op_Addition(System.UInt32 left, System.UInt32 right)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(527314, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527314")]
        [Fact()]
        public void AddExprWithNullableUInt64AndInt32()
        {
            string sourceCode = @"
public class Test
{
    public static void Main(string[] args)
    {
        ulong? ui = 0;
        ulong? ui2 = /*<bind>*/ui + 5/*</bind>*/ ;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.UInt64?", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.UInt64?", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("ulong.operator +(ulong, ulong)", semanticInfo.Symbol.ToString());
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3248, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void NegatedIsExpr()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static void Main()    
    {
      Exception e = new Exception(); 
      bool bl = /*<bind>*/!(e is DivideByZeroException)/*</bind>*/;     
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean System.Boolean.op_LogicalNot(System.Boolean value)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3249, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void IsExpr()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static void Main()    
    {
      Exception e = new Exception(); 
      bool bl = /*<bind>*/ (e is DivideByZeroException) /*</bind>*/ ;     
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(527324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527324")]
        [Fact]
        public void ExceptionCatchVariable()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static void Main()    
    {
      try
      {
      }
      catch (Exception e)
      {
        bool bl = (/*<bind>*/e/*</bind>*/ is DivideByZeroException) ;     
      }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Exception", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Exception", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Exception e", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3478, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GenericInvocation()
        {
            string sourceCode = @"
class Program { 
    public static void Ref<T>(T array) 
    {
    } 

    static void Main() 
    { 
      /*<bind>*/Ref<object>(null)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void Program.Ref<System.Object>(System.Object array)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538039")]
        [Fact]
        public void GlobalAliasQualifiedName()
        {
            string sourceCode = @"
namespace N1 
{
    interface I1
    {
        void Method();
    }
}

namespace N2
{
    class Test : N1.I1
    {
        void /*<bind>*/global::N1.I1/*</bind>*/.Method()
        {
        }        
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("N1.I1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.I1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("N1.I1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(527363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527363")]
        [Fact]
        public void ArrayInitializer()
        {
            string sourceCode = @"
class Test
{
    static void Main() 
    {
        int[] arr = new int[] /*<bind>*/{5}/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538041")]
        [Fact]
        public void AliasQualifiedName()
        {
            string sourceCode = @"
using NSA = A;

namespace A 
{
    class Goo {}
}

namespace B
{
    class Test
    {
        class NSA
        {
        }

        static void Main() 
        {
            /*<bind>*/NSA::Goo/*</bind>*/ goo = new NSA::Goo();

        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("A.Goo", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("A.Goo", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("A.Goo", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538021")]
        [Fact]
        public void EnumToStringInvocationExpr()
        {
            string sourceCode = @"
using System;

enum E { Red, Blue, Green}

public class MainClass
{
    public static int Main ()
    {
        E e = E.Red;
        string s = /*<bind>*/e.ToString()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String System.Enum.ToString()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538026")]
        [Fact]
        public void ExplIfaceMethInvocationExpr()
        {
            string sourceCode = @"
namespace N1 
{
    interface I1
    {
        int Method();
    }
}

namespace N2
{
    class Test : N1.I1
    {
        int N1.I1.Method()
        {
            return 5;
        }
        
        static int Main() 
        {
            Test t = new Test();
            if (/*<bind>*/((N1.I1)t).Method()/*</bind>*/ != 5)
                return 1;
            
            return 0;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 N1.I1.Method()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538027")]
        [Fact]
        public void InvocExprWithAliasIdentifierNameSameAsType()
        {
            string sourceCode = @"
using N1 = NGoo;

namespace NGoo
{
    class Goo 
    {
        public static void method() { }
    }
}

namespace N2
{
    class N1 { }

    class Test
    {
        static void Main() 
        {
            /*<bind>*/N1::Goo.method()/*</bind>*/;
            
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void NGoo.Goo.method()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3498, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void BaseAccessMethodInvocExpr()
        {
            string sourceCode = @"
using System;

public class BaseClass
{
    public virtual void MyMeth()
    {
    }
}

public class MyClass : BaseClass
{
    public override void MyMeth()
    {
        /*<bind>*/base.MyMeth()/*</bind>*/;
    }

    public static void Main()
    {
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void BaseClass.MyMeth()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538104")]
        [Fact]
        public void OverloadResolutionForVirtualMethods()
        {
            string sourceCode = @"
using System;
class Program
{
    static void Main()
    {
        D d = new D();
        string s = ""hello""; long l = 1;
        /*<bind>*/d.goo(ref s, l, l)/*</bind>*/;
    }
}
public class B
{
    // Should bind to this method.
    public virtual int goo(ref string x, long y, long z)
    {
        Console.WriteLine(""Base: goo(ref string x, long y, long z)"");
        return 0;
    }
    public virtual void goo(ref string x, params long[] y)
    {
        Console.WriteLine(""Base: goo(ref string x, params long[] y)"");
    }
}
public class D: B
{
    // Roslyn erroneously binds to this override.
    // Roslyn binds to the correct method above if you comment out this override.
    public override void goo(ref string x, params long[] y)
    {
        Console.WriteLine(""Derived: goo(ref string x, params long[] y)"");
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 B.goo(ref System.String x, System.Int64 y, System.Int64 z)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538104")]
        [Fact]
        public void OverloadResolutionForVirtualMethods2()
        {
            string sourceCode = @"
using System;
class Program
{
    static void Main()
    {
        D d = new D();
        int i = 1;
        /*<bind>*/d.goo(i, i)/*</bind>*/;
    } 
}
public class B
{
    public virtual int goo(params int[] x)
    {
        Console.WriteLine(""""Base: goo(params int[] x)"""");
        return 0;
    }
    public virtual void goo(params object[] x)
    {
        Console.WriteLine(""""Base: goo(params object[] x)"""");
    }
}
public class D: B
{
    public override void goo(params object[] x)
    {
        Console.WriteLine(""""Derived: goo(params object[] x)"""");
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 B.goo(params System.Int32[] x)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ThisInStaticMethod()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/this/*</bind>*/;
    }

}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("Program", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("Program this", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal(CandidateReason.StaticInstanceMismatch, semanticInfo.CandidateReason);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void Constructor1()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = new /*<bind>*/A/*</bind>*/(4);
    }
}

class A
{
    public A() { }
    public A(int x) { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("A", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void Constructor2()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/new A(4)/*</bind>*/;
    }
}

class A
{
    public A() { }
    public A(int x) { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("A", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("A..ctor(System.Int32 x)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("A..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("A..ctor(System.Int32 x)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void FailedOverloadResolution1()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = null;
        /*<bind>*/A.f(o)/*</bind>*/;
    }
}

class A
{
    public void f(int x, int y) { }
    public void f(string z) { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void A.f(System.Int32 x, System.Int32 y)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("void A.f(System.String z)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void FailedOverloadResolution2()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = null;
        A./*<bind>*/f/*</bind>*/(o);
    }
}

class A
{
    public void f(int x, int y) { }
    public void f(string z) { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void A.f(System.Int32 x, System.Int32 y)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("void A.f(System.String z)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void A.f(System.Int32 x, System.Int32 y)", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("void A.f(System.String z)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541745, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541745")]
        [Fact]
        public void FailedOverloadResolution3()
        {
            string sourceCode = @"
class C
{
    public int M { get; set; }
}
static class Extensions1
{
    public static int M(this C c) { return 0; }
}
static class Extensions2
{
    public static int M(this C c) { return 0; }
}

class Goo
{
    void M()
    {
        C c = new C();
        /*<bind>*/c.M/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 C.M()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("System.Int32 C.M()", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 C.M()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.Int32 C.M()", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542833, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542833")]
        [Fact]
        public void FailedOverloadResolution4()
        {
            string sourceCode = @"
class C
{
    public int M; 
}

static class Extensions
{
    public static int M(this C c, int i) { return 0; }
}
class Goo
{
    void M()
    {
        C c = new C();
        /*<bind>*/c.M/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 C.M(System.Int32 i)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 C.M(System.Int32 i)", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SucceededOverloadResolution1()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = null;
        /*<bind>*/A.f(""hi"")/*</bind>*/;
    }
}

class A
{
    public static void f(int x, int y) { }
    public static int f(string z) { return 3; }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 A.f(System.String z)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SucceededOverloadResolution2()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = null;
        A./*<bind>*/f/*</bind>*/(""hi"");
    }
}

class A
{
    public static void f(int x, int y) { }
    public static int f(string z) { return 3; }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 A.f(System.String z)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 A.f(System.String z)", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("void A.f(System.Int32 x, System.Int32 y)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541878")]
        [Fact]
        public void TestCandidateReasonForInaccessibleMethod()
        {
            string sourceCode = @"
class Test
{
    class NestedTest
    {
        static void Method1()
        {
        }
    }

    static void Main()
    {
        /*<bind>*/NestedTest.Method1()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void Test.NestedTest.Method1()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
        }

        [WorkItem(541879, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541879")]
        [Fact]
        public void InaccessibleTypeInObjectCreationExpression()
        {
            string sourceCode = @"
class Test
{
    class NestedTest
    {
        class NestedNestedTest
        {
        }
    }

    static void Main()
    {
        var nnt = /*<bind>*/new NestedTest.NestedNestedTest()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Test.NestedTest.NestedNestedTest", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Test.NestedTest.NestedNestedTest", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Test.NestedTest.NestedNestedTest..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
        }

        [WorkItem(541883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541883")]
        [Fact]
        public void InheritedMemberHiding()
        {
            string sourceCode = @"
public class A
{
    public static int m() { return 1; }
}

public class B : A
{
    public static int m() { return 5; }

    public static void Main1()
    {
        /*<bind>*/m/*</bind>*/(10);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 B.m()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 B.m()", sortedMethodGroup[0].ToTestDisplayString());
        }

        [WorkItem(538106, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538106")]
        [Fact]
        public void UsingAliasNameSystemInvocExpr()
        {
            string sourceCode = @"
using System = MySystem.IO.StreamReader;

namespace N1
{
    using NullStreamReader = System::NullStreamReader;
    class Test
    {
        static int Main()
        {
            NullStreamReader nr = new NullStreamReader();

            /*<bind>*/nr.ReadLine()/*</bind>*/;
            return 0;
        }
    }
}

namespace MySystem
{
    namespace IO
    {
        namespace StreamReader
        {
            public class NullStreamReader
            {
                public string ReadLine() { return null; }
            }
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String MySystem.IO.StreamReader.NullStreamReader.ReadLine()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538109")]
        [Fact]
        public void InterfaceMethodImplInvocExpr()
        {
            string sourceCode = @"
interface ISomething
{
    string ToString();
}

class A : ISomething
{
    string ISomething.ToString()
    {
        return null;
    }
}

class Test
{
    static void Main()
    {
        ISomething isome = new A();

        /*<bind>*/isome.ToString()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String ISomething.ToString()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538112")]
        [Fact]
        public void MemberAccessMethodWithNew()
        {
            string sourceCode = @"
public class MyBase
{
    public void MyMeth()
    {
    }
}

public class MyClass : MyBase
{
    new public void MyMeth()
    {
    }

    public static void Main()
    {
        MyClass test = new MyClass();
        /*<bind>*/test.MyMeth/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void MyClass.MyMeth()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void MyClass.MyMeth()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(527386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527386")]
        [Fact]
        public void MethodGroupWithStaticInstanceSameName()
        {
            string sourceCode = @"
class D
{
    public static void M2(int x, int y)
    {
    }

    public void M2(int x)
    {
    }
}

class C
{
    public static void Main()
    {
        D d = new D();
        /*<bind>*/d.M2/*</bind>*/(5);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void D.M2(System.Int32 x)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void D.M2(System.Int32 x)", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("void D.M2(System.Int32 x, System.Int32 y)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538123")]
        [Fact]
        public void VirtualOverriddenMember()
        {
            string sourceCode = @"
public class C1
{
    public virtual void M1()
    {
    }
}

public class C2:C1
{
    public override void M1()
    {
    }
}

public class Test
{
    static void Main()
    {
        C2 c2 = new C2();
        /*<bind>*/c2.M1/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void C2.M1()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void C2.M1()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538125")]
        [Fact]
        public void AbstractOverriddenMember()
        {
            string sourceCode = @"
public abstract class AbsClass
{
    public abstract void Test();
}

public class TestClass : AbsClass
{
    public  override void Test() { }

    public static void Main()
    {
        TestClass tc = new TestClass();
         /*<bind>*/tc.Test/*</bind>*/();

    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void TestClass.Test()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void TestClass.Test()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DiamondInheritanceMember()
        {
            string sourceCode = @"
public interface IB { void M(); }
public interface IM1 : IB {}
public interface IM2 : IB {}
public interface ID : IM1, IM2 {}
public class Program
{
    public static void Main()
    {
        ID id = null;
       /*<bind>*/id.M/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            // We must ensure that the method is only found once, even though there are two paths to it.

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void IB.M()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void IB.M()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InconsistentlyHiddenMember()
        {
            string sourceCode = @"
public interface IB { void M(); }
public interface IL : IB {}
public interface IR : IB { new void M(); }
public interface ID : IR, IL {}
public class Program
{
    public static void Main()
    {
        ID id = null;
       /*<bind>*/id.M/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            // Even though there is a "path" from ID to IB.M via IL on which IB.M is not hidden,
            // it is still hidden because *any possible hiding* hides the method.

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void IR.M()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void IR.M()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538138")]
        [Fact]
        public void ParenExprWithMethodInvocExpr()
        {
            string sourceCode = @"
class Test
{
    public static int Meth1()
    {
        return 9;
    }

    public static void Main()
    {
        int var1 = /*<bind>*/(Meth1())/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 Test.Meth1()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(527397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527397")]
        [Fact()]
        public void ExplicitIdentityCastExpr()
        {
            string sourceCode = @"
class Test
{
    public static void Main()
    {
        int i = 10;
        object j = /*<bind>*/(int)i/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Boxing, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(3652, "DevDiv_Projects/Roslyn")]
        [WorkItem(529056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529056")]
        [WorkItem(543619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543619")]
        [Fact()]
        public void OutOfBoundsConstCastToByte()
        {
            string sourceCode = @"
class Test
{
    public static void Main()
    {
        byte j = unchecked(/*<bind>*/(byte)-123/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Byte", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Byte", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal((byte)133, semanticInfo.ConstantValue);
        }

        [WorkItem(538160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538160")]
        [Fact]
        public void InsideCollectionsNamespace()
        {
            string sourceCode = @"
using System;

namespace Collections
{
    public class Test
    {
        public static /*<bind>*/void/*</bind>*/ Main()
        {
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Void", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538161")]
        [Fact]
        public void ErrorTypeNameSameAsVariable()
        {
            string sourceCode = @"
public class A
{
    public static void RunTest()
    {
        /*<bind>*/B/*</bind>*/ B = new B();

    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("B", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("B", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotATypeOrNamespace, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("B B", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537117")]
        [WorkItem(537127, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537127")]
        [Fact]
        public void SystemNamespace()
        {
            string sourceCode = @"
namespace System
{
    class A 
    {
        /*<bind>*/System/*</bind>*/.Exception c;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537118")]
        [Fact]
        public void SystemNamespace2()
        {
            string sourceCode = @"
namespace N1
{
    namespace N2
    {
        public class A1 { }
    }
    public class A2
    {
        /*<bind>*/N1.N2.A1/*</bind>*/ a;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("N1.N2.A1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.N2.A1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("N1.N2.A1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537119")]
        [Fact]
        public void SystemNamespace3()
        {
            string sourceCode = @"
class H<T>
{
}

class A
{
}

namespace N1
{
    public class A1
    {
        /*<bind>*/H<A>/*</bind>*/ a;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("H<A>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("H<A>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("H<A>", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537124, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537124")]
        [Fact]
        public void SystemNamespace4()
        {
            string sourceCode = @"
using System;

class H<T>
{
}

class H<T1, T2>
{
}

class A
{
}

namespace N1
{
    public class A1
    {
        /*<bind>*/H<H<A>, H<A>>/*</bind>*/ a;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("H<H<A>, H<A>>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("H<H<A>, H<A>>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("H<H<A>, H<A>>", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537160")]
        [Fact]
        public void SystemNamespace5()
        {
            string sourceCode = @"
namespace N1
{
    namespace N2
    {
        public class A2
        {
            public class A1 { }
            /*<bind>*/N1.N2.A2.A1/*</bind>*/ a;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("N1.N2.A2.A1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.N2.A2.A1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("N1.N2.A2.A1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537161")]
        [Fact]
        public void SystemNamespace6()
        {
            string sourceCode = @"
namespace N1
{
    class NC1
    {
        public class A1 { }
    }
    public class A2
    {
        /*<bind>*/N1.NC1.A1/*</bind>*/ a;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("N1.NC1.A1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.NC1.A1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("N1.NC1.A1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537340")]
        [Fact]
        public void LeftOfDottedTypeName()
        {
            string sourceCode = @"
class Main 
{  
   A./*<bind>*/B/*</bind>*/ x; // this refers to the B within A.
}

class A {    
   public class B {}
}

class B {}

";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("A.B", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("A.B", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("A.B", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537592")]
        [Fact]
        public void Parameters()
        {
            string sourceCode = @"
class C
{
    void M(DateTime dt)
    {
        /*<bind>*/dt/*</bind>*/.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("DateTime", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("DateTime", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("DateTime dt", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        // TODO: This should probably have a candidate symbol!
        [WorkItem(527212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527212")]
        [Fact]
        public void FieldMemberOfConstructedType()
        {
            string sourceCode = @"
class C<T> {
    public T Field;
}
class D {
    void M() {
        new C<int>./*<bind>*/Field/*</bind>*/.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("C<System.Int32>.Field", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("C<System.Int32>.Field", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);

            // Should bind to "field" with a candidateReason (not a typeornamespace>)
            Assert.NotEqual(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.NotEqual(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(537593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537593")]
        [Fact]
        public void Constructor()
        {
            string sourceCode = @"
class C
{
    public C() { /*<bind>*/new C()/*</bind>*/.ToString(); }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("C..ctor()", semanticInfo.MethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538046")]
        [Fact]
        public void TypeNameInTypeThatMatchesNamespace()
        {
            string sourceCode = @"
namespace T
{
    class T
    {
        void M()
        {
            /*<bind>*/T/*</bind>*/.T T = new T.T();
        }
    }
}

";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("T.T", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("T.T", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("T.T", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538267")]
        [Fact]
        public void RHSExpressionInTryParent()
        {
            string sourceCode = @"
using System;
public class Test
{
    static int Main()
    {
        try
        {
            object obj = /*<bind>*/null/*</bind>*/;
        }
        catch {}

        return 0;
    }
}

";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Null(semanticInfo.ConstantValue.Value);
        }

        [WorkItem(538215, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538215")]
        [Fact]
        public void GenericArgumentInBase1()
        {
            string sourceCode = @"
public class X 
{
    public interface Z { }
}
class A<T>
{
    public class X { }
}
class B : A<B.Y./*<bind>*/Z/*</bind>*/>
{
    public class Y : X { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("B.Y.Z", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("B.Y.Z", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538215, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538215")]
        [Fact]
        public void GenericArgumentInBase2()
        {
            string sourceCode = @"
public class X 
{
    public interface Z { }
}
class A<T>
{
    public class X { }
}
class B : /*<bind>*/A<B.Y.Z>/*</bind>*/
{
    public class Y : X { }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("A<B.Y.Z>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("A<B.Y.Z>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("A<B.Y.Z>", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538097")]
        [Fact]
        public void InvokedLocal1()
        {
            string sourceCode = @"
class C
{
  static void Goo()
  {
    int x = 10;
    /*<bind>*/x/*</bind>*/();
  }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538318")]
        [Fact]
        public void TooManyConstructorArgs()
        {
            string sourceCode = @"
class C
{
  C() {}
  void M()
  {
    /*<bind>*/new C(null
/*</bind>*/  }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("C..ctor()", semanticInfo.MethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(538185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538185")]
        [Fact]
        public void NamespaceAndFieldSameName1()
        {
            string sourceCode = @"
class C
{
    void M()
    {
        /*<bind>*/System/*</bind>*/.String x = F();
    }
    string F()
    {
        return null;
    }
    public int System;
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void PEProperty()
        {
            string sourceCode = @"
class C
{
  void M(string s)
  {
    /*<bind>*/s.Length/*</bind>*/;
  }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 System.String.Length { get; }", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NotPresentGenericType1()
        {
            string sourceCode = @"

class Class { void Test() { /*<bind>*/List<int>/*</bind>*/ l; } }
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("List<System.Int32>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("List<System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NotPresentGenericType2()
        {
            string sourceCode = @"

class Class {
    /*<bind>*/List<int>/*</bind>*/ Test() { return null;}
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("List<System.Int32>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("List<System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BadArityConstructorCall()
        {
            string sourceCode = @"
class C<T1>
{
    public void Test()
    {
        C c = new /*<bind>*/C/*</bind>*/();
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.WrongArity, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C<T1>", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BadArityConstructorCall2()
        {
            string sourceCode = @"
class C<T1>
{
    public void Test()
    {
        C c = /*<bind>*/new C()/*</bind>*/;
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("C<T1>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("C<T1>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C<T1>..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C<T1>..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void UnresolvedBaseConstructor()
        {
            string sourceCode = @"
class C : B {
    public C(int i) /*<bind>*/: base(i)/*</bind>*/ { }
    public C(string j, string k) : base() { }
}

class B {
    public B(string a, string b) { }
    public B() { }
    int i; 
}
";
            var semanticInfo = GetSemanticInfoForTest<ConstructorInitializerSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("B..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("B..ctor(System.String a, System.String b)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BoundBaseConstructor()
        {
            string sourceCode = @"
class C : B {
    public C(int i) /*<bind>*/: base(""hi"", ""hello"")/*</bind>*/ { }
    public C(string j, string k) : base() { }
}

class B
{
    public B(string a, string b) { }
    public B() { }
    int i;
}
";
            var semanticInfo = GetSemanticInfoForTest<ConstructorInitializerSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("B..ctor(System.String a, System.String b)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540998, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540998")]
        [Fact]
        public void DeclarationWithinSwitchStatement()
        {
            string sourceCode =
@"class C
{
    static void M(int i)
    {
        switch (i)
        {
            case 0:
                string name = /*<bind>*/null/*</bind>*/;
                if (name != null)
                {
                }
                break;
        }
    }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.NotNull(semanticInfo);
        }

        [WorkItem(537573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537573")]
        [Fact]
        public void UndeclaredTypeAndCheckContainingSymbol()
        {
            string sourceCode = @"
class C1
{
    void M()
    {
        /*<bind>*/F/*</bind>*/ f;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("F", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("F", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            Assert.Equal(SymbolKind.Namespace, semanticInfo.Type.ContainingSymbol.Kind);
            Assert.True(((INamespaceSymbol)semanticInfo.Type.ContainingSymbol).IsGlobalNamespace);
        }

        [WorkItem(538538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538538")]
        [Fact]
        public void AliasQualifier()
        {
            string sourceCode = @"
using X = A;
namespace A.B { }
namespace N
{
using /*<bind>*/X/*</bind>*/::B;
class X { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.NotNull(semanticInfo.Symbol);
            Assert.Equal("A", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.NotNull(aliasInfo);
            Assert.Equal("X=A", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AliasQualifier2()
        {
            string sourceCode = @"
using S = System.String;

{
    class X 
    { 
        void Goo()
        {
            string x;
            x = /*<bind>*/S/*</bind>*/.Empty;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.NotNull(semanticInfo.Symbol);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.Equal("S=System.String", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);
            Assert.Equal("String", aliasInfo.Target.Name);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void PropertyAccessor()
        {
            string sourceCode = @"
class C
{
    private object p = null;
    internal object P { set { p = /*<bind>*/value/*</bind>*/; } }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Object value", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void IndexerAccessorValue()
        {
            string sourceCode =
@"class C
{
    string[] values = new string[10];
    internal string this[int i]
    {
        get { return values[i]; }
        set { values[i] = /*<bind>*/value/*</bind>*/; }
    }
}";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal("System.String value", semanticInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void IndexerAccessorParameter()
        {
            string sourceCode =
@"class C
{
    string[] values = new string[10];
    internal string this[short i]
    {
        get { return values[/*<bind>*/i/*</bind>*/]; }
    }
}";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.Equal("System.Int16", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal("System.Int16 i", semanticInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void IndexerAccessNamedParameter()
        {
            string sourceCode =
@"class C
{
    string[] values = new string[10];
    internal string this[short i]
    {
        get { return values[i]; }
    }
    void Method()
    {
        string s = this[/*<bind>*/i/*</bind>*/: 0];
    }
}";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.NotNull(semanticInfo);
            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);

            var symbol = semanticInfo.Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.True(symbol.ContainingSymbol.Kind == SymbolKind.Property && ((IPropertySymbol)symbol.ContainingSymbol).IsIndexer);
            Assert.Equal("System.Int16 i", symbol.ToTestDisplayString());
        }

        [Fact]
        public void LocalConstant()
        {
            string sourceCode = @"
class C
{
    static void M()
    {
        const int i = 1;
        const int j = i + 1;
        const int k = /*<bind>*/j/*</bind>*/ - 2;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 j", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(2, semanticInfo.ConstantValue);

            var symbol = (ILocalSymbol)semanticInfo.Symbol;
            Assert.True(symbol.HasConstantValue);
            Assert.Equal(2, symbol.ConstantValue);
            Assert.False(symbol.IsForEach);
            Assert.False(symbol.IsUsing);
        }

        [Fact]
        public void FieldConstant()
        {
            string sourceCode = @"
class C
{
    const int i = 1;
    const int j = i + 1;
    static void M()
    {
        const int k = /*<bind>*/j/*</bind>*/ - 2;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 C.j", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(2, semanticInfo.ConstantValue);

            var symbol = (IFieldSymbol)semanticInfo.Symbol;
            Assert.Equal("j", symbol.Name);
            Assert.True(symbol.HasConstantValue);
            Assert.Equal(2, symbol.ConstantValue);
        }

        [Fact]
        public void FieldInitializer()
        {
            string sourceCode = @"
class C
{
    int F = /*<bind>*/G() + 1/*</bind>*/;
    static int G()
    {
        return 1;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<BinaryExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 System.Int32.op_Addition(System.Int32 left, System.Int32 right)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void EnumConstant()
        {
            string sourceCode = @"
enum E { A, B, C, D = B }
class C
{
    static void M(E e)
    {
        M(/*<bind>*/E.C/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("E", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.Type.TypeKind);
            Assert.Equal("E", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("E.C", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(2, semanticInfo.ConstantValue);

            var symbol = (IFieldSymbol)semanticInfo.Symbol;

            Assert.IsAssignableFrom<SourceEnumConstantSymbol>(symbol.GetSymbol());
            Assert.Equal("C", symbol.Name);
            Assert.True(symbol.HasConstantValue);
            Assert.Equal(2, symbol.ConstantValue);
        }

        [Fact]
        public void BadEnumConstant()
        {
            string sourceCode = @"
enum E { W = Z, X, Y }
class C
{
    static void M(E e)
    {
        M(/*<bind>*/E.Y/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("E", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.Type.TypeKind);
            Assert.Equal("E", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("E.Y", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var symbol = (IFieldSymbol)semanticInfo.Symbol;

            Assert.IsAssignableFrom<SourceEnumConstantSymbol>(symbol.GetSymbol());
            Assert.Equal("Y", symbol.Name);
            Assert.False(symbol.HasConstantValue);
        }

        [Fact]
        public void CircularEnumConstant01()
        {
            string sourceCode = @"
enum E { A = B, B }
class C
{
    static void M(E e)
    {
        M(/*<bind>*/E.B/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("E", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.Type.TypeKind);
            Assert.Equal("E", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("E.B", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var symbol = (IFieldSymbol)semanticInfo.Symbol;

            Assert.IsAssignableFrom<SourceEnumConstantSymbol>(symbol.GetSymbol());
            Assert.Equal("B", symbol.Name);
            Assert.False(symbol.HasConstantValue);
        }

        [Fact]
        public void CircularEnumConstant02()
        {
            string sourceCode = @"
enum E { A = 10, B = C, C, D }
class C
{
    static void M(E e)
    {
        M(/*<bind>*/E.D/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("E", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.Type.TypeKind);
            Assert.Equal("E", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("E.D", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var symbol = (IFieldSymbol)semanticInfo.Symbol;

            Assert.IsAssignableFrom<SourceEnumConstantSymbol>(symbol.GetSymbol());
            Assert.Equal("D", symbol.Name);
            Assert.False(symbol.HasConstantValue);
        }

        [Fact]
        public void EnumInitializer()
        {
            string sourceCode = @"
enum E { A, B = 3 }
enum F { C, D = 1 + /*<bind>*/E.B/*</bind>*/ }

";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("E", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Enum, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("E.B", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(3, semanticInfo.ConstantValue);

            var symbol = (IFieldSymbol)semanticInfo.Symbol;

            Assert.IsAssignableFrom<SourceEnumConstantSymbol>(symbol.GetSymbol());
            Assert.Equal("B", symbol.Name);
            Assert.True(symbol.HasConstantValue);
            Assert.Equal(3, symbol.ConstantValue);
        }

        [Fact]
        public void ParameterOfExplicitInterfaceImplementation()
        {
            string sourceCode = @"
class Class : System.IFormattable
{
    string System.IFormattable.ToString(string format, System.IFormatProvider formatProvider)
    {
        return /*<bind>*/format/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String format", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BaseConstructorInitializer()
        {
            string sourceCode = @"
class Class
{
    Class(int x) : this(/*<bind>*/x/*</bind>*/ , x) { }
    Class(int x, int y) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(MethodKind.Constructor, ((IMethodSymbol)semanticInfo.Symbol.ContainingSymbol).MethodKind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.ContainingSymbol.Kind);
            Assert.Equal(MethodKind.Constructor, ((IMethodSymbol)semanticInfo.Symbol.ContainingSymbol).MethodKind);
        }

        [WorkItem(541011, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541011")]
        [WorkItem(527831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527831")]
        [WorkItem(538794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538794")]
        [Fact]
        public void InaccessibleMethodGroup()
        {
            string sourceCode = @"
class C
{
    private static void M(long i) { }
    private static void M(int i) { }
}
class D
{
    void Goo()
    {
        C./*<bind>*/M/*</bind>*/(1);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void C.M(System.Int32 i)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal("void C.M(System.Int64 i)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void C.M(System.Int32 i)", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("void C.M(System.Int64 i)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleMethodGroup_Constructors_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    public static void Main(string[] args)
    {
        var x = /*<bind>*/new Class1(3, 7)/*</bind>*/;
    }
}

class Class1
{
    protected Class1() { }
    protected Class1(int x) { }
    private Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Class1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Class1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(3, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedCandidates[2].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[2].Kind);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            sortedCandidates = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedCandidates[2].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[2].Kind);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InaccessibleMethodGroup_Constructors_ImplicitObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    public static void Main(string[] args)
    {
        Class1 x = /*<bind>*/new(3, 7)/*</bind>*/;
    }
}

class Class1
{
    protected Class1() { }
    protected Class1(int x) { }
    private Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Class1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Class1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(3, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedCandidates[2].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[2].Kind);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            sortedCandidates = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedCandidates[2].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[2].Kind);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleMethodGroup_Constructors_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    public static void Main(string[] args)
    {
        var x = new /*<bind>*/Class1/*</bind>*/(3, 7);
    }
}

class Class1
{
    protected Class1() { }
    protected Class1(int x) { }
    private Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Class1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleMethodGroup_AttributeSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    [/*<bind>*/Class1(3, 7)/*</bind>*/]
    public static void Main(string[] args)
    {
    }
}

class Class1 : Attribute
{
    protected Class1() { }
    protected Class1(int x) { }
    private Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("Class1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Class1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);

            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleMethodGroup_Attribute_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    [/*<bind>*/Class1/*</bind>*/(3, 7)]
    public static void Main(string[] args)
    {
    }
}

class Class1 : Attribute
{
    protected Class1() { }
    protected Class1(int x) { }
    private Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Class1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Class1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleConstructorsFiltered_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    public static void Main(string[] args)
    {
        var x = /*<bind>*/new Class1(3, 7)/*</bind>*/;
    }
}

class Class1
{
    protected Class1() { }
    public Class1(int x) { }
    public Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Class1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Class1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleConstructorsFiltered_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    public static void Main(string[] args)
    {
        var x = new /*<bind>*/Class1/*</bind>*/(3, 7);
    }
}

class Class1
{
    protected Class1() { }
    public Class1(int x) { }
    public Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Class1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleConstructorsFiltered_AttributeSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    [/*<bind>*/Class1(3, 7)/*</bind>*/]
    public static void Main(string[] args)
    {
    }
}

class Class1 : Attribute
{
    protected Class1() { }
    public Class1(int x) { }
    public Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("Class1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Class1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542782")]
        [Fact]
        public void InaccessibleConstructorsFiltered_Attribute_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;

class Program
{
    [/*<bind>*/Class1/*</bind>*/(3, 7)]
    public static void Main(string[] args)
    {
    }
}

class Class1 : Attribute
{
    protected Class1() { }
    public Class1(int x) { }
    public Class1(int a, long b) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Class1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Class1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Class1..ctor(System.Int32 a, System.Int64 b)", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("Class1..ctor(System.Int32 x)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(528754, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528754")]
        [Fact]
        public void SyntaxErrorInReceiver()
        {
            string sourceCode = @"
public delegate int D(int x);
public class C
{
    public C(int i) { }
    public void M(D d) { }
}
class Main
{
    void Goo(int a)
    {
        new C(a.).M(x => /*<bind>*/x/*</bind>*/);
    }
}";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
        }

        [WorkItem(528754, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528754")]
        [Fact]
        public void SyntaxErrorInReceiverWithExtension()
        {
            string sourceCode = @"
public delegate int D(int x);
public static class CExtensions
{
    public static void M(this C c, D d) { }
}
public class C
{
    public C(int i) { }
}
class Main
{
    void Goo(int a)
    {
        new C(a.).M(x => /*<bind>*/x/*</bind>*/);
    }
}";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
        }

        [WorkItem(541011, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541011")]
        [Fact]
        public void NonStaticInstanceMismatchMethodGroup()
        {
            string sourceCode = @"
class C
{
    public static int P { get; set; }
}
class D
{
    void Goo()
    {
        C./*<bind>*/set_P/*</bind>*/(1);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotReferencable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void C.P.set", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal(MethodKind.PropertySet, ((IMethodSymbol)sortedCandidates[0]).MethodKind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void C.P.set", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540360")]
        [Fact]
        public void DuplicateTypeName()
        {
            string sourceCode = @"
struct C { }
class C
{
    public static void M() { }
}
enum C { A, B }
class D
{
    static void Main()
    {
        /*<bind>*/C/*</bind>*/.M();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(3, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("C", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);
            Assert.Equal("C", sortedCandidates[2].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[2].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void IfCondition()
        {
            string sourceCode = @"
class C 
{
  void M(int x)
  {
    if (/*<bind>*/x == 10/*</bind>*/) {}
  }
}
";
            var semanticInfo = GetSemanticInfoForTest<BinaryExpressionSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean System.Int32.op_Equality(System.Int32 left, System.Int32 right)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ForCondition()
        {
            string sourceCode = @"
class C 
{
  void M(int x)
  {
    for (int i = 0; /*<bind>*/i < 10/*</bind>*/; i = i + 1) { }
  }
}
";
            var semanticInfo = GetSemanticInfoForTest<BinaryExpressionSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean System.Int32.op_LessThan(System.Int32 left, System.Int32 right)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(539925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539925")]
        [Fact]
        public void LocalIsFromSource()
        {
            string sourceCode = @"

class C
{
    void M()
    {
        int x = 1;
        int y = /*<bind>*/x/*</bind>*/;
    }
}
";
            var compilation = CreateCompilation(sourceCode);
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(compilation);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
            Assert.True(semanticInfo.Symbol.GetSymbol().IsFromCompilation(compilation));
        }

        [WorkItem(540541, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540541")]
        [Fact]
        public void InEnumElementInitializer()
        {
            string sourceCode = @"
class C
{
    public const int x = 1;
}
enum E
{
    q = /*<bind>*/C.x/*</bind>*/,
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 C.x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(1, semanticInfo.ConstantValue);
        }

        [WorkItem(540541, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540541")]
        [Fact]
        public void InEnumOfByteElementInitializer()
        {
            string sourceCode = @"
class C
{
    public const int x = 1;
}
enum E : byte
{
    q = /*<bind>*/C.x/*</bind>*/,
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Byte", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitConstant, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 C.x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(1, semanticInfo.ConstantValue);
        }

        [WorkItem(540672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540672")]
        [Fact]
        public void LambdaExprWithErrorTypeInObjectCreationExpression()
        {
            var text = @"
class Program
{
    static int Main()
    {
       var d = /*<bind>*/() => { if (true) return new X(); else return new Y(); }/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(text, parseOptions: TestOptions.Regular9);
            Assert.NotNull(semanticInfo);
            Assert.Null(semanticInfo.Type);
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
        }

        [Fact]
        public void LambdaExpression()
        {
            string sourceCode = @"
using System;

public class TestClass
{
    public static void Main()
    {
        Func<string, int> f = /*<bind>*/str => 10/*</bind>*/ ;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Func<System.String, System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.AnonymousFunction, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("lambda expression", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            var lambdaSym = (IMethodSymbol)(semanticInfo.Symbol);
            Assert.Equal(1, lambdaSym.Parameters.Length);
            Assert.Equal("str", lambdaSym.Parameters[0].Name);
            Assert.Equal("System.String", lambdaSym.Parameters[0].Type.ToTestDisplayString());
            Assert.Equal("System.Int32", lambdaSym.ReturnType.ToTestDisplayString());

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void UnboundLambdaExpression()
        {
            string sourceCode = @"
using System;

public class TestClass
{
    public static void Main()
    {
        object f = /*<bind>*/str => 10/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<SimpleLambdaExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("lambda expression", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            var lambdaSym = (IMethodSymbol)(semanticInfo.Symbol);
            Assert.Equal(1, lambdaSym.Parameters.Length);
            Assert.Equal("str", lambdaSym.Parameters[0].Name);
            Assert.Equal(TypeKind.Error, lambdaSym.Parameters[0].Type.TypeKind);
            Assert.Equal("System.Int32", lambdaSym.ReturnType.ToTestDisplayString());

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540650, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540650")]
        [Fact]
        public void TypeOfExpression()
        {
            string sourceCode = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(/*<bind>*/typeof(C)/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<TypeOfExpressionSyntax>(sourceCode);

            Assert.Equal("System.Type", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void DeclarationEmbeddedStatement_If()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        if (c)
            int j = /*<bind>*/43/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(43, semanticInfo.ConstantValue);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void LabeledEmbeddedStatement_For()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        for (; c; c = !c)
            label: /*<bind>*/c/*</bind>*/ = false;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean c", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void DeclarationEmbeddedStatement_While()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        while (c)
            int j = /*<bind>*/43/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(43, semanticInfo.ConstantValue);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void LabeledEmbeddedStatement_ForEach()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        foreach (string s in args)
            label: /*<bind>*/c/*</bind>*/ = false;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean c", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void DeclarationEmbeddedStatement_Else()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        if (c);
        else
            long j = /*<bind>*/43/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(43, semanticInfo.ConstantValue);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void LabeledEmbeddedStatement_Do()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        do
            label: /*<bind>*/c/*</bind>*/ = false;
        while(c);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean c", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void DeclarationEmbeddedStatement_Using()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        using(null)
            long j = /*<bind>*/43/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(43, semanticInfo.ConstantValue);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void LabeledEmbeddedStatement_Lock()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        lock(this)
            label: /*<bind>*/c/*</bind>*/ = false;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean c", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540759")]
        [Fact]
        public void DeclarationEmbeddedStatement_Fixed()
        {
            string sourceCode = @"
unsafe class Program
{
    static void Main(string[] args)
    {
        bool c = true;

        fixed (bool* p = &c)
            int j = /*<bind>*/43/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(43, semanticInfo.ConstantValue);
        }

        [WorkItem(539255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539255")]
        [Fact]
        public void BindLiteralCastToDouble()
        {
            string sourceCode = @"
class MyClass 
{
    double dbl =  /*<bind>*/1/*</bind>*/ ;
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(1, semanticInfo.ConstantValue);
        }

        [WorkItem(540803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540803")]
        [Fact]
        public void BindDefaultOfVoidExpr()
        {
            string sourceCode = @"
class C
{
    void M()
    {
        return /*<bind>*/default(void)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<DefaultExpressionSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Void, semanticInfo.Type.SpecialType);
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void GetSemanticInfoForBaseConstructorInitializer()
        {
            string sourceCode = @"
class C
{
    C() /*<bind>*/: base()/*</bind>*/ { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ConstructorInitializerSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Object..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void GetSemanticInfoForThisConstructorInitializer()
        {
            string sourceCode = @"
class C
{
    C() /*<bind>*/: this(1)/*</bind>*/ { }
    C(int x) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ConstructorInitializerSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C..ctor(System.Int32 x)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540862")]
        [Fact]
        public void ThisStaticConstructorInitializer()
        {
            string sourceCode = @"
class MyClass
{
    static MyClass()
        /*<bind>*/: this()/*</bind>*/
    {
        intI = 2;
    }
    public MyClass() { }
    static int intI = 1;
}
";
            var semanticInfo = GetSemanticInfoForTest<ConstructorInitializerSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("MyClass..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541037")]
        [Fact]
        public void IncompleteForEachWithArrayCreationExpr()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        foreach (var f in new int[] { /*<bind>*/5/*</bind>*/
        {
                Console.WriteLine(f);
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(5, (int)semanticInfo.ConstantValue.Value);
        }

        [WorkItem(541037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541037")]
        [Fact]
        public void EmptyStatementInForEach()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        foreach (var a in /*<bind>*/args/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal(SpecialType.System_String, ((IArrayTypeSymbol)semanticInfo.Type).ElementType.SpecialType);
            // CONSIDER: we could conceivable use the foreach collection type (vs the type of the collection expr).
            Assert.Equal(SpecialType.System_Collections_IEnumerable, semanticInfo.ConvertedType.SpecialType);
            Assert.Equal("args", semanticInfo.Symbol.Name);
        }

        [WorkItem(540922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540922")]
        [WorkItem(541030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541030")]
        [Fact]
        public void ImplicitlyTypedForEachIterationVariable()
        {
            string sourceCode = @"
class Program
{
    static void Main(string[] args)
    {
        foreach (/*<bind>*/var/*</bind>*/ a in args);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal(SpecialType.System_String, semanticInfo.Type.SpecialType);

            var symbol = semanticInfo.Symbol;
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal(SpecialType.System_String, ((ITypeSymbol)symbol).SpecialType);
        }

        [Fact]
        public void ForEachCollectionConvertedType()
        {
            // Arrays don't actually use IEnumerable, but that's the spec'd behavior.
            CheckForEachCollectionConvertedType("int[]", "System.Int32[]", "System.Collections.IEnumerable");
            CheckForEachCollectionConvertedType("int[,]", "System.Int32[,]", "System.Collections.IEnumerable");

            // Strings don't actually use string.GetEnumerator, but that's the spec'd behavior.
            CheckForEachCollectionConvertedType("string", "System.String", "System.String");

            // Special case for dynamic
            CheckForEachCollectionConvertedType("dynamic", "dynamic", "System.Collections.IEnumerable");

            // Pattern-based, not interface-based
            CheckForEachCollectionConvertedType("System.Collections.Generic.List<int>", "System.Collections.Generic.List<System.Int32>", "System.Collections.Generic.List<System.Int32>");

            // Interface-based
            CheckForEachCollectionConvertedType("Enumerable", "Enumerable", "System.Collections.IEnumerable"); // helper method knows definition of this type

            // Interface
            CheckForEachCollectionConvertedType("System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.IEnumerable<System.Int32>", "System.Collections.Generic.IEnumerable<System.Int32>");

            // Interface
            CheckForEachCollectionConvertedType("NotAType", "NotAType", "NotAType"); // name not in scope
        }

        private void CheckForEachCollectionConvertedType(string sourceType, string typeDisplayString, string convertedTypeDisplayString)
        {
            string template = @"
public class Enumerable : System.Collections.IEnumerable
{{
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {{
        return null;
    }}
}}

class Program
{{
    void M({0} collection)
    {{
        foreach (var v in /*<bind>*/collection/*</bind>*/);
    }}
}}
";
            var semanticInfo = GetSemanticInfoForTest(string.Format(template, sourceType));
            Assert.Equal(typeDisplayString, semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(convertedTypeDisplayString, semanticInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void InaccessibleParameter()
        {
            string sourceCode = @"
using System;

class Outer
{
    class Inner
    {
    }
}

class Program
{
    public static void f(Outer.Inner a) { /*<bind>*/a/*</bind>*/ = 4; }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Outer.Inner", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Outer.Inner", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Outer.Inner a", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            // Parameter's type is an error type, because Outer.Inner is inaccessible.
            var param = (IParameterSymbol)semanticInfo.Symbol;
            Assert.Equal(TypeKind.Error, param.Type.TypeKind);

            // It's type is not equal to the SemanticInfo type, because that is
            // not an error type.
            Assert.NotEqual(semanticInfo.Type, param.Type);
        }

        [Fact]
        public void StructConstructor()
        {
            string sourceCode = @"
struct Struct{
    public static void Main()
    {
        Struct s = /*<bind>*/new Struct()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Struct", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("Struct", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            var symbol = semanticInfo.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal(MethodKind.Constructor, ((IMethodSymbol)symbol).MethodKind);
            Assert.True(symbol.IsImplicitlyDeclared);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Struct..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodGroupAsArgOfInvalidConstructorCall()
        {
            string sourceCode = @"
using System;

class Class { string M(int i) { new T(/*<bind>*/M/*</bind>*/); } }


";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.String Class.M(System.Int32 i)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.String Class.M(System.Int32 i)", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodGroupInReturnStatement()
        {
            string sourceCode = @"
class C
{
    public delegate int Func(int i);
 
    public Func Goo()
    {
        return /*<bind>*/Goo/*</bind>*/;
    }
    private int Goo(int i)
    {
        return i;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("C.Func", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.MethodGroup, semanticInfo.ImplicitConversion.Kind);
            Assert.Equal("C.Goo(int)", semanticInfo.ImplicitConversion.Method.ToString());

            Assert.Equal("System.Int32 C.Goo(System.Int32 i)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C.Func C.Goo()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.Int32 C.Goo(System.Int32 i)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DelegateConversionExtensionMethodNoReceiver()
        {
            string sourceCode =
@"class C
{
    static System.Action<object> F()
    {
        return /*<bind>*/S.E/*</bind>*/;
    }
}
static class S
{
    internal static void E(this object o) { }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.NotNull(semanticInfo);
            Assert.Equal("System.Action<System.Object>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal("void S.E(this System.Object o)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.MethodGroup, semanticInfo.ImplicitConversion.Kind);
            Assert.False(semanticInfo.ImplicitConversion.IsExtensionMethod);
        }

        [Fact]
        public void DelegateConversionExtensionMethod()
        {
            string sourceCode =
@"class C
{
    static System.Action F(object o)
    {
        return /*<bind>*/o.E/*</bind>*/;
    }
}
static class S
{
    internal static void E(this object o) { }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.NotNull(semanticInfo);
            Assert.Equal("System.Action", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal("void System.Object.E()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(ConversionKind.MethodGroup, semanticInfo.ImplicitConversion.Kind);
            Assert.True(semanticInfo.ImplicitConversion.IsExtensionMethod);
        }

        [Fact]
        public void InferredVarType()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var x = ""hello"";
        /*<bind>*/var/*</bind>*/ y = x;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InferredVarTypeWithNamespaceInScope()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace var { }
class Program
{
    static void Main(string[] args)
    {
        var x = ""hello"";
        /*<bind>*/var/*</bind>*/ y = x;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NonInferredVarType()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace N1
{
    class var { }
    class Program
    {
        static void Main(string[] args)
        {
            /*<bind>*/var/*</bind>*/ x = ""hello"";
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("N1.var", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.var", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("N1.var", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541207, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541207")]
        [Fact]
        public void UndeclaredVarInThrowExpr()
        {
            string sourceCode = @"
class Test
{
    static void Main()
    {
        throw /*<bind>*/d1.Get/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.NotNull(semanticInfo);
        }

        [Fact]
        public void FailedConstructorCall()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class C { }
class Program
{
    static void Main(string[] args)
    {
        C c = new /*<bind>*/C/*</bind>*/(17);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void FailedConstructorCall2()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class C { }
class Program
{
    static void Main(string[] args)
    {
        C c = /*<bind>*/new C(17)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MemberGroup.Length);
            Assert.Equal("C..ctor()", semanticInfo.MemberGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541332")]
        [Fact]
        public void ImplicitConversionCastExpression()
        {
            string sourceCode = @"
using System;

enum E { a, b }
class Program
{
    static int Main()
    {
        int ret = /*<bind>*/(int) E.b/*</bind>*/;
        return ret - 1;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<CastExpressionSyntax>(sourceCode);

            Assert.Equal("int", semanticInfo.Type.ToString());
            Assert.Equal("int", semanticInfo.ConvertedType.ToString());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);
            Assert.True(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541333, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541333")]
        [Fact]
        public void ImplicitConversionAnonymousMethod()
        {
            string sourceCode = @"
using System;

delegate int D();
class Program
{
    static int Main()
    {
        D d = /*<bind>*/delegate() { return int.MaxValue; }/*</bind>*/; 
        return 0;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<AnonymousMethodExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("D", semanticInfo.ConvertedType.ToString());
            Assert.Equal(ConversionKind.AnonymousFunction, semanticInfo.ImplicitConversion.Kind);
            Assert.False(semanticInfo.IsCompileTimeConstant);

            sourceCode = @"
using System;

delegate int D();
class Program
{
    static int Main()
    {
        D d = /*<bind>*/() => { return int.MaxValue; }/*</bind>*/; 
        return 0;
    }
}
";
            semanticInfo = GetSemanticInfoForTest<ParenthesizedLambdaExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("D", semanticInfo.ConvertedType.ToString());
            Assert.Equal(ConversionKind.AnonymousFunction, semanticInfo.ImplicitConversion.Kind);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(528476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528476")]
        [Fact]
        public void BindingInitializerToTargetType()
        {
            string sourceCode = @"
using System;

class Program
{
    static int Main()
    {
        int[] ret = new int[] /*<bind>*/ { 0, 1, 2 } /*</bind>*/;
        return ret[0];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);
            Assert.Null(semanticInfo.Type);
        }

        [Fact]
        public void BindShortMethodArgument()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void goo(short s)
    {
    }

    static void Main(string[] args)
    {
        goo(/*<bind>*/123/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int16", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitConstant, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(123, semanticInfo.ConstantValue);
        }

        [WorkItem(541400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541400")]
        [Fact]
        public void BindingAttributeParameter()
        {
            string sourceCode = @"
using System;

public class MeAttribute : Attribute 
{
    public MeAttribute(short p) 
    { 
    }
}

[Me(/*<bind>*/123/*</bind>*/)]
public class C
{
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);
            Assert.NotNull(semanticInfo.Type);
            Assert.Equal("int", semanticInfo.Type.ToString());
            Assert.Equal("short", semanticInfo.ConvertedType.ToString());
            Assert.Equal(ConversionKind.ImplicitConstant, semanticInfo.ImplicitConversion.Kind);
            Assert.True(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BindAttributeFieldNamedArgumentOnMethod()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class TestAttribute : Attribute
{
   public TestAttribute() { }

   public string F;
}

class C1
{
    [Test(/*<bind>*/F/*</bind>*/=""method"")]
    int f() { return 0; }
}


";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String TestAttribute.F", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BindAttributePropertyNamedArgumentOnMethod()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class TestAttribute : Attribute
{
  
    public TestAttribute() { }
    public TestAttribute(int i) { }

    public string F;
    public double P { get; set; }
}


class C1
{
    [Test(/*<bind>*/P/*</bind>*/=3.14)] 
    int f() { return 0; }
}


";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Double", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Double TestAttribute.P { get; set; }", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void TestAttributeNamedArgumentValueOnMethod()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class TestAttribute : Attribute
{
  
    public TestAttribute() { }
    public TestAttribute(int i) { }

    public string F;
    public double P { get; set; }
}


class C1
{
    [Test(P=/*<bind>*/1/*</bind>*/)] 
    int f() { return 0; }
}


";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(1, semanticInfo.ConstantValue);
        }

        [WorkItem(540775, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540775")]
        [Fact]
        public void LambdaExprPrecededByAnIncompleteUsingStmt()
        {
            var code = @"
using System;

class Program
{
    static void Main(string[] args)
    {
         using
         Func<int, int> Dele =  /*<bind>*/ x => {  return x; } /*</bind>*/;
    }
}
";

            var semanticInfo = GetSemanticInfoForTest<SimpleLambdaExpressionSyntax>(code);
            Assert.NotNull(semanticInfo);
            Assert.Null(semanticInfo.Type);
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
        }

        [WorkItem(540785, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540785")]
        [Fact]
        public void NestedLambdaExprPrecededByAnIncompleteNamespaceStmt()
        {
            var code = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        namespace
        Func<int, int> f1 = (x) =>
    {
            Func<int, int> f2 = /*<bind>*/ (y) => { return y; } /*</bind>*/;
            return  x;
        }
;
    }
}
";

            var semanticInfo = GetSemanticInfoForTest<ParenthesizedLambdaExpressionSyntax>(code);
            Assert.NotNull(semanticInfo);
            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Func<System.Int32, System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
        }

        [Fact]
        public void DefaultStructConstructor()
        {
            string sourceCode = @"
using System;

struct Struct{
    public static void Main()    
    {        
        Struct s = new /*<bind>*/Struct/*</bind>*/();
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Struct", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DefaultStructConstructor2()
        {
            string sourceCode = @"
using System;

struct Struct{
    public static void Main()    
    {        
        Struct s = /*<bind>*/new Struct()/*</bind>*/;
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Struct", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("Struct", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Struct..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Struct..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541451")]
        [Fact]
        public void BindAttributeInstanceWithoutAttributeSuffix()
        {
            string sourceCode = @"
[assembly: /*<bind>*/My/*</bind>*/]

class MyAttribute : System.Attribute { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("MyAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("MyAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("MyAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("MyAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541451")]
        [Fact]
        public void BindQualifiedAttributeInstanceWithoutAttributeSuffix()
        {
            string sourceCode = @"
[assembly: /*<bind>*/N1.My/*</bind>*/]

namespace N1
{
    class MyAttribute : System.Attribute { }
}
";
            var semanticInfo = GetSemanticInfoForTest<QualifiedNameSyntax>(sourceCode);

            Assert.Equal("N1.MyAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("N1.MyAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("N1.MyAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("N1.MyAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(540770, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540770")]
        [Fact]
        public void IncompleteDelegateCastExpression()
        {
            string sourceCode = @"
delegate void D();
class MyClass 
{
    public static int Main() 
    {
        D d;
        d =  /*<bind>*/(D) delegate /*</bind>*/
";
            var semanticInfo = GetSemanticInfoForTest<CastExpressionSyntax>(sourceCode);

            Assert.Equal("D", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind);
            Assert.Equal("D", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(7177, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void IncompleteGenericDelegateDecl()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Func<int, int> ()/*</bind>*/
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("?", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541120")]
        [Fact]
        public void DelegateCreationArguments()
        {
            string sourceCode = @"
class Program
{
     int goo(int i) { return i;}
 
    static void Main(string[] args)
    {
        var r = /*<bind>*/new System.Func<int, int>((arg)=> { return 1;}, goo)/*</bind>*/; 
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Func<System.Int32, System.Int32>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Func<System.Int32, System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DelegateCreationArguments2()
        {
            string sourceCode = @"
class Program
{
     int goo(int i) { return i;}
 
    static void Main(string[] args)
    {
        var r = new /*<bind>*/System.Func<int, int>/*</bind>*/((arg)=> { return 1;}, goo); 
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<TypeSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Func<System.Int32, System.Int32>", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BaseConstructorInitializer2()
        {
            string sourceCode = @"
class C
{
    C() /*<bind>*/: base()/*</bind>*/ { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ConstructorInitializerSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Object..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(MethodKind.Constructor, ((IMethodSymbol)semanticInfo.Symbol).MethodKind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ThisConstructorInitializer2()
        {
            string sourceCode = @"
class C
{
    C() /*<bind>*/: this(1)/*</bind>*/ { }
    C(int x) { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ConstructorInitializerSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C..ctor(System.Int32 x)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(539255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539255")]
        [Fact]
        public void TypeInParentOnFieldInitializer()
        {
            string sourceCode = @"
class MyClass 
{
    double dbl = /*<bind>*/1/*</bind>*/;
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(1, semanticInfo.ConstantValue);
        }

        [Fact]
        public void ExplicitIdentityConversion()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int y = 12;
        long x = /*<bind>*/(int)y/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<CastExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541588")]
        [Fact]
        public void ImplicitConversionElementsInArrayInit()
        {
            string sourceCode = @"
class MyClass 
{
    long[] l1 = {/*<bind>*/4L/*</bind>*/, 5L };
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int64", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(4L, semanticInfo.ConstantValue);
        }

        [WorkItem(116, "https://github.com/dotnet/roslyn/issues/116")]
        [Fact]
        public void ImplicitConversionArrayInitializer_01()
        {
            string sourceCode = @"
class MyClass 
{
    int[] arr = /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Int32[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(116, "https://github.com/dotnet/roslyn/issues/116")]
        [Fact]
        public void ImplicitConversionArrayInitializer_02()
        {
            string sourceCode = @"
class MyClass 
{
    void Test()
    {
        int[] arr = /*<bind>*/{ 1, 2, 3 }/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.Int32[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541595")]
        [Fact]
        public void ImplicitConversionExprReturnedByLambda()
        {
            string sourceCode = @"
using System;

class MyClass 
{
    Func<long> f1 = () => /*<bind>*/4/*</bind>*/;
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.False(semanticInfo.ImplicitConversion.IsIdentity);
            Assert.True(semanticInfo.ImplicitConversion.IsImplicit);
            Assert.True(semanticInfo.ImplicitConversion.IsNumeric);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(4, semanticInfo.ConstantValue);
        }

        [WorkItem(541040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541040")]
        [WorkItem(528551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528551")]
        [Fact]
        public void InaccessibleNestedType()
        {
            string sourceCode = @"
using System;

internal class EClass
{
    private enum EEK { a, b, c, d };
}

class Test
{
    public void M(EClass.EEK e)
    {
        b = /*<bind>*/ e /*</bind>*/;
    }
    EClass.EEK b = EClass.EEK.a;
}
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Type.Kind);
            Assert.Equal(TypeKind.Enum, semanticInfo.Type.TypeKind);
            Assert.NotNull(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(semanticInfo.Type, semanticInfo.ConvertedType);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NamedParameter1()
        {
            string sourceCode = @"
using System;

class Program
{
    public void f(int x, int y, int z) { }
    public void f(string y, string z) { }

    public void goo()
    {
        f(3, /*<bind>*/z/*</bind>*/: 4, y: 9);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 z", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NamedParameter2()
        {
            string sourceCode = @"
using System;

class Program
{
    public void f(int x, int y, int z) { }
    public void f(string y, string z, int q) { }
    public void f(string q, int w, int b) { }

    public void goo()
    {
        f(3, /*<bind>*/z/*</bind>*/: ""goo"", y: 9);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 z", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, sortedCandidates[0].Kind);
            Assert.Equal("System.String z", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NamedParameter3()
        {
            string sourceCode = @"
using System;

class Program
{
    public void f(int x, int y, int z) { }
    public void f(string y, string z, int q) { }
    public void f(string q, int w, int b) { }

    public void goo()
    {
        f(3, z: ""goo"", /*<bind>*/yagga/*</bind>*/: 9);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void NamedParameter4()
        {
            string sourceCode = @"
using System;


namespace ClassLibrary44
{
    [MyAttr(/*<bind>*/x/*</bind>*/:1)]
    public class Class1
    {
    }

    public class MyAttr: Attribute
    {
        public MyAttr(int x)
        {}
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541623")]
        [Fact]
        public void ImplicitReferenceConvExtensionMethodReceiver()
        {
            string sourceCode =
@"public static class Extend
{
    public static string TestExt(this object o1)
    {
        return o1.ToString();
    }
}
class Program
{
    static void Main(string[] args)
    {
        string str1 = ""Test"";
        var e1 = /*<bind>*/str1/*</bind>*/.TestExt();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);
            Assert.True(semanticInfo.ImplicitConversion.IsReference);

            Assert.Equal("System.String str1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
        }

        [WorkItem(541623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541623")]
        [Fact]
        public void ImplicitBoxingConvExtensionMethodReceiver()
        {
            string sourceCode =
@"struct S { }
static class C
{
    static void M(S s)
    {
        /*<bind>*/s/*</bind>*/.F();
    }
    static void F(this object o)
    {
    }
}";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("S", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Boxing, semanticInfo.ImplicitConversion.Kind);
            Assert.True(semanticInfo.ImplicitConversion.IsBoxing);

            Assert.Equal("S s", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind);
        }

        [Fact]
        public void AttributeSyntaxBinding()
        {
            string sourceCode = @"
using System;

[/*<bind>*/MyAttr(1)/*</bind>*/]
public class Class1
{
}

public class MyAttr: Attribute
{
    public MyAttr(int x)
    {}
}

";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            // Should bind to constructor.
            Assert.NotNull(semanticInfo.Symbol);
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
        }

        [WorkItem(541653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541653")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void MemberAccessOnErrorType()
        {
            string sourceCode = @"
public class Test2
{
    public static void Main()
    {
        string x1 = A./*<bind>*/M/*</bind>*/.C.D.E;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SymbolKind.ErrorType, semanticInfo.Type.Kind);
        }

        [WorkItem(541653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541653")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void MemberAccessOnErrorType2()
        {
            string sourceCode = @"
public class Test2
{
    public static void Main()
    {
        string x1 = A./*<bind>*/M/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SymbolKind.ErrorType, semanticInfo.Type.Kind);
        }

        [WorkItem(541764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541764")]
        [Fact]
        public void DelegateCreation1()
        {
            string sourceCode = @"
class C
{
    delegate void MyDelegate();

    public void F()
    {
        MyDelegate MD1 = new /*<bind>*/MyDelegate/*</bind>*/(this.F);
        MyDelegate MD2 = MD1 + MD1;
        MyDelegate MD3 = new MyDelegate(MD1);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C.MyDelegate", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DelegateCreation1_2()
        {
            string sourceCode = @"
class C
{
    delegate void MyDelegate();

    public void F()
    {
        MyDelegate MD1 = /*<bind>*/new MyDelegate(this.F)/*</bind>*/;
        MyDelegate MD2 = MD1 + MD1;
        MyDelegate MD3 = new MyDelegate(MD1);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("C.MyDelegate", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind);
            Assert.Equal("C.MyDelegate", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541764")]
        [Fact]
        public void DelegateCreation2()
        {
            string sourceCode = @"
class C
{
    delegate void MyDelegate();

    public void F()
    {
        MyDelegate MD1 = new MyDelegate(this.F);
        MyDelegate MD2 = MD1 + MD1;
        MyDelegate MD3 = new /*<bind>*/MyDelegate/*</bind>*/(MD1);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("C.MyDelegate", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541764")]
        [Fact]
        public void DelegateCreation2_2()
        {
            string sourceCode = @"
class C
{
    delegate void MyDelegate();

    public void F()
    {
        MyDelegate MD1 = new MyDelegate(this.F);
        MyDelegate MD2 = MD1 + MD1;
        MyDelegate MD3 = /*<bind>*/new MyDelegate(MD1)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("C.MyDelegate", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind);
            Assert.Equal("C.MyDelegate", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DelegateSignatureMismatch1()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static int f() { return 1; }
    static void Main(string[] args)
    {
        Action a = new /*<bind>*/Action/*</bind>*/(f);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Action", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DelegateSignatureMismatch2()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static int f() { return 1; }
    static void Main(string[] args)
    {
        Action a = /*<bind>*/new Action(f)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Action", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Action", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DelegateSignatureMismatch3()
        {
            // This test and the DelegateSignatureMismatch4 should have identical results, as they are semantically identical

            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static int f() { return 1; }
    static void Main(string[] args)
    {
        Action a = new Action(/*<bind>*/f/*</bind>*/);
    }
}
";
            {
                var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode,
                    parseOptions: TestOptions.WithoutImprovedOverloadCandidates);

                Assert.Null(semanticInfo.Type);
                Assert.Equal("System.Action", semanticInfo.ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.MethodGroup, semanticInfo.ImplicitConversion.Kind);

                Assert.Equal("System.Int32 Program.f()", semanticInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
                Assert.Empty(semanticInfo.CandidateSymbols);

                Assert.Equal(1, semanticInfo.MethodGroup.Length);
                Assert.Equal("System.Int32 Program.f()", semanticInfo.MethodGroup[0].ToTestDisplayString());

                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
            {
                var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

                Assert.Null(semanticInfo.Type);
                Assert.Equal("System.Action", semanticInfo.ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.MethodGroup, semanticInfo.ImplicitConversion.Kind);

                Assert.Null(semanticInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
                Assert.Equal("System.Int32 Program.f()", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
                Assert.Equal(1, semanticInfo.CandidateSymbols.Length);

                Assert.Equal(1, semanticInfo.MethodGroup.Length);
                Assert.Equal("System.Int32 Program.f()", semanticInfo.MethodGroup[0].ToTestDisplayString());

                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
        }

        [Fact]
        public void DelegateSignatureMismatch4()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static int f() { return 1; }
    static void Main(string[] args)
    {
        Action a = /*<bind>*/f/*</bind>*/;
    }
}
";
            {
                var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode,
                    parseOptions: TestOptions.WithoutImprovedOverloadCandidates);

                Assert.Null(semanticInfo.Type);
                Assert.Equal("System.Action", semanticInfo.ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.MethodGroup, semanticInfo.ImplicitConversion.Kind);

                Assert.Equal("System.Int32 Program.f()", semanticInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
                Assert.Empty(semanticInfo.CandidateSymbols);

                Assert.Equal(1, semanticInfo.MethodGroup.Length);
                Assert.Equal("System.Int32 Program.f()", semanticInfo.MethodGroup[0].ToTestDisplayString());

                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
            {
                var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

                Assert.Null(semanticInfo.Type);
                Assert.Equal("System.Action", semanticInfo.ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

                Assert.Null(semanticInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
                Assert.Equal("System.Int32 Program.f()", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
                Assert.Equal(1, semanticInfo.CandidateSymbols.Length);

                Assert.Equal(1, semanticInfo.MethodGroup.Length);
                Assert.Equal("System.Int32 Program.f()", semanticInfo.MethodGroup[0].ToTestDisplayString());

                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
        }

        [WorkItem(541802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541802")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void IncompleteLetClause()
        {
            string sourceCode = @"
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        /*<bind>*/var/*</bind>*/ q2 = from x in nums
                 let z = x.
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SymbolKind.ErrorType, semanticInfo.Type.Kind);
        }

        [WorkItem(541895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541895")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void QueryErrorBaseKeywordAsSelectExpression()
        {
            string sourceCode = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var expr1 = new int[] { 1 };

        /*<bind>*/var/*</bind>*/ query2 = from int b in expr1 select base;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SymbolKind.ErrorType, semanticInfo.Type.Kind);
        }

        [WorkItem(541805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541805")]
        [Fact]
        public void InToIdentifierQueryContinuation()
        {
            string sourceCode = @"
using System;
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x into w
                 select /*<bind>*/w/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
        }

        [WorkItem(541833, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541833")]
        [Fact]
        public void InOptimizedAwaySelectClause()
        {
            string sourceCode = @"
using System;
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 where x > 1
                 select /*<bind>*/x/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
        }

        [Fact]
        public void InFromClause()
        {
            string sourceCode = @"
using System;
using System.Linq;
class C
{          
    void M()
    {
        int rolf = 732;
        int roark = -9;
        var replicator = from r in new List<int> { 1, 2, 9, rolf, /*<bind>*/roark/*</bind>*/ } select r * 2;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
        }

        [WorkItem(541911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541911")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void QueryErrorGroupJoinFromClause()
        {
            string sourceCode = @"
class Test
{
    static void Main()
    {
        /*<bind>*/var/*</bind>*/ q = 
    from  Goo  i in i
    from  Goo<int>  j in j
    group i by  i
    join  Goo
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(SymbolKind.ErrorType, semanticInfo.Type.Kind);
        }

        [WorkItem(541920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541920")]
        [Fact]
        public void SymbolInfoForMissingSelectClauseNode()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string[] strings = { };

        var query = from s in strings
                    let word = s.Split(' ')
                    from w in w
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var selectClauseNode = tree.FindNodeOrTokenByKind(SyntaxKind.SelectClause).AsNode() as SelectClauseSyntax;

            var symbolInfo = semanticModel.GetSymbolInfo(selectClauseNode);

            // https://github.com/dotnet/roslyn/issues/38509
            // Assert.NotEqual(default, symbolInfo);
            Assert.Null(symbolInfo.Symbol);
        }

        [WorkItem(541940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541940")]
        [Fact]
        public void IdentifierInSelectNotInContext()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string[] strings = { };

        var query = from ch in strings
                    group ch by ch
                    into chGroup
                    where chGroup.Count() >= 2
                    select /*<bind>*/ x1 /*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
        }

        [Fact]
        public void WhereDefinedInType()
        {
            var csSource = @"
using System;

class Y
{
    public int Where(Func<int, bool> predicate)
    {
        return 45;
    }
}

class P
{
    static void Main()
    {
        var src = new Y();
        var query = from x in src
                where x > 0
                select /*<bind>*/ x /*</bind>*/;
    }
}
";

            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(csSource);
            Assert.Equal("x", semanticInfo.Symbol.Name);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
        }

        [WorkItem(541830, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541830")]
        [Fact]
        public void AttributeUsageError()
        {
            string sourceCode = @"
using System;

[/*<bind>*/AttributeUsage/*</bind>*/()]
class MyAtt : Attribute
{}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
            Assert.Equal("AttributeUsageAttribute", semanticInfo.Type.Name);
        }

        [WorkItem(541832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541832")]
        [Fact]
        public void OpenGenericTypeInAttribute()
        {
            string sourceCode = @"
class Gen<T> {}
    
[/*<bind>*/Gen<T>/*</bind>*/]
public class Test
{
}
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("Gen<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Gen<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<T>..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<T>..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541832")]
        [Fact]
        public void OpenGenericTypeInAttribute02()
        {
            string sourceCode = @"
class Goo {}
    
[/*<bind>*/Goo/*</bind>*/]
public class Test
{
}
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("Goo", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Goo", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Goo..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Goo..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541896")]
        [Fact]
        public void IncompleteEmptyAttributeSyntax01()
        {
            string sourceCode = @"
public class CSEvent {
    [
";
            var compilation = CreateCompilation(sourceCode);
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var attributeNode = tree.FindNodeOrTokenByKind(SyntaxKind.Attribute).AsNode() as AttributeSyntax;

            var semanticInfo = semanticModel.GetSemanticInfoSummary(attributeNode);

            Assert.NotNull(semanticInfo);
            Assert.Null(semanticInfo.Symbol);
            Assert.Null(semanticInfo.Type);
        }

        /// <summary>
        /// Same as above but with a token after the incomplete
        /// attribute so the attribute is not at the end of file.
        /// </summary>
        [WorkItem(541896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541896")]
        [Fact]
        public void IncompleteEmptyAttributeSyntax02()
        {
            string sourceCode = @"
public class CSEvent {
    [
}";
            var compilation = CreateCompilation(sourceCode);
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var attributeNode = tree.FindNodeOrTokenByKind(SyntaxKind.Attribute).AsNode() as AttributeSyntax;

            var semanticInfo = semanticModel.GetSemanticInfoSummary(attributeNode);

            Assert.NotNull(semanticInfo);
            Assert.Null(semanticInfo.Symbol);
            Assert.Null(semanticInfo.Type);
        }

        [WorkItem(541857, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541857")]
        [Fact]
        public void EventWithInitializerInInterface()
        {
            string sourceCode = @"
public delegate void MyDelegate();

interface test
{
    event MyDelegate e = /*<bind>*/new MyDelegate(Test.Main)/*</bind>*/;
}

class Test
{
    static void Main() { }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("MyDelegate", semanticInfo.Type.ToTestDisplayString());
        }

        [Fact]
        public void SwitchExpression_Constant01()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        switch (/*<bind>*/true/*</bind>*/)
        {
            default:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(true, semanticInfo.ConstantValue);
        }

        [Fact]
        [WorkItem(40352, "https://github.com/dotnet/roslyn/issues/40352")]
        public void SwitchExpression_Constant02()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        const string s = null;

        switch (/*<bind>*/s/*</bind>*/)
        {
            case null:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal(CodeAnalysis.NullableFlowState.None, semanticInfo.Nullability.FlowState);
            Assert.Equal(CodeAnalysis.NullableFlowState.None, semanticInfo.ConvertedNullability.FlowState);
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String s", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Null(semanticInfo.ConstantValue.Value);
        }

        [Fact]
        [WorkItem(40352, "https://github.com/dotnet/roslyn/issues/40352")]
        public void SwitchExpression_NotConstant()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        string s = null;
        switch (/*<bind>*/s/*</bind>*/)
        {
            case null:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal(CodeAnalysis.NullableFlowState.None, semanticInfo.Nullability.FlowState);
            Assert.Equal(CodeAnalysis.NullableFlowState.None, semanticInfo.ConvertedNullability.FlowState);
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.String s", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SwitchExpression_Invalid_Lambda()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        switch (/*<bind>*/()=>3/*</bind>*/)
        {
            default:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ParenthesizedLambdaExpressionSyntax>(sourceCode, parseOptions: TestOptions.Regular6);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("lambda expression", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SwitchExpression_Invalid_MethodGroup()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int M() {return 0; }
    public static int Main(string[] args)
    {
        int ret = 1;
        switch (/*<bind>*/M/*</bind>*/)
        {
            default:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode, parseOptions: TestOptions.Regular6);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal("System.Int32 Test.M()", semanticInfo.CandidateSymbols.Single().ToTestDisplayString());

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 Test.M()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SwitchExpression_Invalid_GoverningType()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        switch (/*<bind>*/2.2/*</bind>*/)
        {
            default:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode, parseOptions: TestOptions.Regular6);

            Assert.Equal("System.Double", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(2.2, semanticInfo.ConstantValue);
        }

        [Fact]
        public void SwitchCaseLabelExpression_Null()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        const string s = null;

        switch (s)
        {
            case /*<bind>*/null/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Null(semanticInfo.ConstantValue.Value);
        }

        [Fact]
        public void SwitchCaseLabelExpression_Constant01()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        switch (true)
        {
            case /*<bind>*/true/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(true, semanticInfo.ConstantValue);
        }

        [Fact]
        public void SwitchCaseLabelExpression_Constant02()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        const bool x = true;
        switch (true)
        {
            case /*<bind>*/x/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(true, semanticInfo.ConstantValue);
        }

        [Fact]
        public void SwitchCaseLabelExpression_NotConstant()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        bool x = true;
        switch (true)
        {
            case /*<bind>*/x/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SwitchCaseLabelExpression_CastExpression()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;        
        switch (ret)
        {
            case /*<bind>*/(int)'a'/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<CastExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(97, semanticInfo.ConstantValue);
        }

        [Fact]
        public void SwitchCaseLabelExpression_Invalid_Lambda()
        {
            string sourceCode = @"
using System;

public class Test
{
    public static int Main(string[] args)
    {
        int ret = 1;
        string s = null;
        switch (s)
        {
            case /*<bind>*/()=>3/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            CreateCompilation(sourceCode).VerifyDiagnostics(
                // (12,30): error CS1003: Syntax error, ':' expected
                //             case /*<bind>*/()=>3/*</bind>*/:
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(":").WithLocation(12, 30),
                // (12,30): error CS1513: } expected
                //             case /*<bind>*/()=>3/*</bind>*/:
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(12, 30),
                // (12,44): error CS1002: ; expected
                //             case /*<bind>*/()=>3/*</bind>*/:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(12, 44),
                // (12,44): error CS1513: } expected
                //             case /*<bind>*/()=>3/*</bind>*/:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(12, 44),
                // (12,28): error CS1501: No overload for method 'Deconstruct' takes 0 arguments
                //             case /*<bind>*/()=>3/*</bind>*/:
                Diagnostic(ErrorCode.ERR_BadArgCount, "()").WithArguments("Deconstruct", "0").WithLocation(12, 28),
                // (12,28): error CS8129: No suitable Deconstruct instance or extension method was found for type 'string', with 0 out parameters and a void return type.
                //             case /*<bind>*/()=>3/*</bind>*/:
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("string", "0").WithLocation(12, 28)
                );
        }

        [Fact]
        public void SwitchCaseLabelExpression_Invalid_LambdaWithSyntaxError()
        {
            string sourceCode = @"
using System;

public class Test
{
    static int M() { return 0;}
    public static int Main(string[] args)
    {
        int ret = 1;
        string s = null;
        switch (s)
        {
            case /*<bind>*/()=>/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            CreateCompilation(sourceCode).VerifyDiagnostics(
                // (13,30): error CS1003: Syntax error, ':' expected
                //             case /*<bind>*/()=>/*</bind>*/:
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(":").WithLocation(13, 30),
                // (13,30): error CS1513: } expected
                //             case /*<bind>*/()=>/*</bind>*/:
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(13, 30),
                // (13,28): error CS1501: No overload for method 'Deconstruct' takes 0 arguments
                //             case /*<bind>*/()=>/*</bind>*/:
                Diagnostic(ErrorCode.ERR_BadArgCount, "()").WithArguments("Deconstruct", "0").WithLocation(13, 28),
                // (13,28): error CS8129: No suitable Deconstruct instance or extension method was found for type 'string', with 0 out parameters and a void return type.
                //             case /*<bind>*/()=>/*</bind>*/:
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("string", "0").WithLocation(13, 28)
                );
        }

        [Fact]
        public void SwitchCaseLabelExpression_Invalid_MethodGroup()
        {
            string sourceCode = @"
using System;

public class Test
{
    static int M() { return 0;}
    public static int Main(string[] args)
    {
        int ret = 1;
        string s = null;
        switch (s)
        {
            case /*<bind>*/M/*</bind>*/:
                ret = 0;
                break;
        }

        Console.Write(ret);
        return (ret);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal("System.Int32 Test.M()", semanticInfo.CandidateSymbols.Single().ToTestDisplayString());

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 Test.M()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541932")]
        [Fact]
        public void IndexingExpression()
        {
            string sourceCode = @"
class Test
{
    static void Main()
    {
        string str = ""Test"";
        char ch = str[/*<bind>*/ 0 /*</bind>*/];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(0, semanticInfo.ConstantValue);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
        }

        [Fact]
        public void InaccessibleInTypeof()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class A
{
    class B { }
}

class Program
{
    static void Main(string[] args)
    {
        object o = typeof(/*<bind>*/A.B/*</bind>*/);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<QualifiedNameSyntax>(sourceCode);

            Assert.Equal("A.B", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("A.B", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("A.B", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AttributeWithUnboundGenericType01()
        {
            var sourceCode =
@"using System;

class A : Attribute
{
    public A(object o) { }
}

[A(typeof(/*<bind>*/B<>/*</bind>*/))] 
class B<T>
{
    public class C
    {
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            var type = semanticInfo.Type;
            Assert.True((type as INamedTypeSymbol).IsUnboundGenericType);
            Assert.False((type as INamedTypeSymbol).IsErrorType());
        }

        [Fact]
        public void AttributeWithUnboundGenericType02()
        {
            var sourceCode =
@"using System;

class A : Attribute
{
    public A(object o) { }
}

[A(typeof(/*<bind>*/B<>.C/*</bind>*/))] 
class B<T>
{
    public class C
    {
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            var type = semanticInfo.Type;
            Assert.True((type as INamedTypeSymbol).IsUnboundGenericType);
            Assert.False((type as INamedTypeSymbol).IsErrorType());
        }

        [Fact]
        public void AttributeWithUnboundGenericType03()
        {
            var sourceCode =
@"using System;

class A : Attribute
{
    public A(object o) { }
}

[A(typeof(/*<bind>*/D/*</bind>*/.C<>))] 
class B<T>
{
    public class C<U>
    {
    }
}

class D : B<int>
{
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            var type = semanticInfo.Type;
            Assert.False((type as INamedTypeSymbol).IsUnboundGenericType);
            Assert.False((type as INamedTypeSymbol).IsErrorType());
        }

        [Fact]
        public void AttributeWithUnboundGenericType04()
        {
            var sourceCode =
@"using System;

class A : Attribute
{
    public A(object o) { }
}

[A(typeof(/*<bind>*/B<>/*</bind>*/.C<>))] 
class B<T>
{
    public class C<U>
    {
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            var type = semanticInfo.Type;
            Assert.Equal("B", type.Name);
            Assert.True((type as INamedTypeSymbol).IsUnboundGenericType);
            Assert.False((type as INamedTypeSymbol).IsErrorType());
        }

        [WorkItem(542430, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542430")]
        [Fact]
        public void UnboundTypeInvariants()
        {
            var sourceCode =
@"using System;

public class A<T>
{
    int x;
    public class B<U>
    {
        int y;
    }
}

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(typeof(/*<bind>*/A<>.B<>/*</bind>*/));
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            var type = (INamedTypeSymbol)semanticInfo.Type;
            Assert.Equal("B", type.Name);
            Assert.True(type.IsUnboundGenericType);
            Assert.False(type.IsErrorType());
            Assert.True(type.TypeArguments[0].IsErrorType());

            var constructedFrom = type.ConstructedFrom;
            Assert.Equal(constructedFrom, constructedFrom.ConstructedFrom);
            Assert.Equal(constructedFrom, constructedFrom.TypeParameters[0].ContainingSymbol);
            Assert.Equal(constructedFrom.TypeArguments[0], constructedFrom.TypeParameters[0]);
            Assert.Equal(type.ContainingSymbol, constructedFrom.ContainingSymbol);
            Assert.Equal(type.TypeParameters[0], constructedFrom.TypeParameters[0]);
            Assert.False(constructedFrom.TypeArguments[0].IsErrorType());
            Assert.NotEqual(type, constructedFrom);
            Assert.False(constructedFrom.IsUnboundGenericType);
            var a = type.ContainingType;
            Assert.Equal(constructedFrom, a.GetTypeMembers("B").Single());
            Assert.NotEqual(type.TypeParameters[0], type.OriginalDefinition.TypeParameters[0]); // alpha renamed
            Assert.Null(type.BaseType);
            Assert.Empty(type.Interfaces);
            Assert.NotNull(constructedFrom.BaseType);
            Assert.Empty(type.GetMembers());
            Assert.NotEmpty(constructedFrom.GetMembers());
            Assert.True(a.IsUnboundGenericType);
            Assert.False(a.ConstructedFrom.IsUnboundGenericType);
            Assert.Equal(1, a.GetMembers().Length);
        }

        [WorkItem(528659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528659")]
        [Fact]
        public void AliasTypeName()
        {
            string sourceCode = @"
using A = System.String;

class Test
{
    static void Main()
    {
        /*<bind>*/A/*</bind>*/ a = null;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            var aliasInfo = GetAliasInfoForTest(sourceCode);

            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal("System.String", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);
            Assert.Equal("A", aliasInfo.Name);
            Assert.Equal("A=System.String", aliasInfo.ToTestDisplayString());
        }

        [WorkItem(542000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542000")]
        [Fact]
        public void AmbigAttributeBindWithoutAttributeSuffix()
        {
            string sourceCode = @"
namespace Blue
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace Red
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}


namespace Green
{
    using Blue;
    using Red;

    public class Test
    {
        [/*<bind>*/Description/*</bind>*/(null)]
        static void Main()
        {
        }

    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Blue.DescriptionAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("Blue.DescriptionAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Blue.DescriptionAttribute", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("Red.DescriptionAttribute", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(528669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528669")]
        [Fact]
        public void AmbigAttributeBind1()
        {
            string sourceCode = @"
namespace Blue
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace Red
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}


namespace Green
{
    using Blue;
    using Red;

    public class Test
    {
        [/*<bind>*/DescriptionAttribute/*</bind>*/(null)]
        static void Main()
        {
        }

    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Blue.DescriptionAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("Blue.DescriptionAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Blue.DescriptionAttribute", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("Red.DescriptionAttribute", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542205")]
        [Fact]
        public void IncompleteAttributeSymbolInfo()
        {
            string sourceCode = @"
using System;

class Program
{
    [/*<bind>*/ObsoleteAttribute(x/*</bind>*/
    static void Main(string[] args)
    {        
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Null(semanticInfo.Symbol);

            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(3, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedCandidates[2].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[2].Kind);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(541968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541968")]
        [Fact]
        public void ConstantFieldInitializerExpression()
        {
            var sourceCode = @"
using System;
public class Aa
{
    const int myLength = /*<bind>*/5/*</bind>*/;
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            Assert.Equal(5, semanticInfo.ConstantValue);
        }

        [WorkItem(541968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541968")]
        [Fact]
        public void CircularConstantFieldInitializerExpression()
        {
            var sourceCode = @"
public class C
{
    const int x = /*<bind>*/x/*</bind>*/;
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542017")]
        [Fact]
        public void AmbigAttributeBind2()
        {
            string sourceCode = @"
using System;

[AttributeUsage(AttributeTargets.All)]
public class X : Attribute
{
}

[AttributeUsage(AttributeTargets.All)]
public class XAttribute : Attribute
{
}

[/*<bind>*/X/*</bind>*/]
class Class1	
{
}
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("X", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("XAttribute", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
        }

        [WorkItem(542018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542018")]
        [Fact]
        public void AmbigAttributeBind3()
        {
            string sourceCode = @"
using System;

[AttributeUsage(AttributeTargets.All)]
public class X : Attribute
{
}

[AttributeUsage(AttributeTargets.All)]
public class XAttribute : Attribute
{
}

[/*<bind>*/X/*</bind>*/]
class Class1	
{
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);

            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("X", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("XAttribute", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
        }

        [Fact]
        public void AmbigAttributeBind4()
        {
            string sourceCode = @"
namespace ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace ValidWithoutSuffix
{
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_01
{
    using ValidWithSuffix;
    using ValidWithoutSuffix;

    [/*<bind>*/Description/*</bind>*/(null)]
    public class Test { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("ValidWithoutSuffix.Description", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("ValidWithoutSuffix.Description", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("ValidWithSuffix.DescriptionAttribute", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("ValidWithoutSuffix.Description", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbigAttributeBind5()
        {
            string sourceCode = @"
namespace ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace ValidWithSuffix_And_ValidWithoutSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_02
{
    using ValidWithSuffix;
    using ValidWithSuffix_And_ValidWithoutSuffix;

    [/*<bind>*/Description/*</bind>*/(null)]
    public class Test { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.Description", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.Description", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.Description.Description(string)", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.Description.Description(string)", semanticInfo.MethodGroup[0].ToDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbigAttributeBind6()
        {
            string sourceCode = @"
namespace ValidWithoutSuffix
{
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace ValidWithSuffix_And_ValidWithoutSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_03
{
    using ValidWithoutSuffix;
    using ValidWithSuffix_And_ValidWithoutSuffix;

    [/*<bind>*/Description/*</bind>*/(null)]
    public class Test { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute.DescriptionAttribute(string)", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.DescriptionAttribute.DescriptionAttribute(string)", semanticInfo.MethodGroup[0].ToDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbigAttributeBind7()
        {
            string sourceCode = @"
namespace ValidWithSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace ValidWithoutSuffix
{
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace ValidWithSuffix_And_ValidWithoutSuffix
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description : System.Attribute
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_04
{
    using ValidWithSuffix;
    using ValidWithoutSuffix;
    using ValidWithSuffix_And_ValidWithoutSuffix;

    [/*<bind>*/Description/*</bind>*/(null)]
    public class Test { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.Description", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.Description", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("ValidWithSuffix_And_ValidWithoutSuffix.Description", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("ValidWithoutSuffix.Description", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbigAttributeBind8()
        {
            string sourceCode = @"
namespace InvalidWithSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
}

namespace InvalidWithoutSuffix
{
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_05
{
    using InvalidWithSuffix;
    using InvalidWithoutSuffix;

    [/*<bind>*/Description/*</bind>*/(null)]
    public class Test { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("InvalidWithoutSuffix.Description", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("InvalidWithoutSuffix.Description", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("InvalidWithoutSuffix.Description..ctor(System.String name)", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("InvalidWithoutSuffix.Description..ctor(System.String name)", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AmbigAttributeBind9()
        {
            string sourceCode = @"
namespace InvalidWithoutSuffix
{
    public class Description
    {
        public Description(string name) { }
    }
}

namespace InvalidWithSuffix_And_InvalidWithoutSuffix
{
    public class DescriptionAttribute
    {
        public DescriptionAttribute(string name) { }
    }
    public class Description
    {
        public Description(string name) { }
    }
}

namespace TestNamespace_07
{
    using InvalidWithoutSuffix;
    using InvalidWithSuffix_And_InvalidWithoutSuffix;

    [/*<bind>*/Description/*</bind>*/(null)]
    public class Test { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("InvalidWithSuffix_And_InvalidWithoutSuffix.Description", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("InvalidWithSuffix_And_InvalidWithoutSuffix.Description", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("InvalidWithSuffix_And_InvalidWithoutSuffix.Description", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("InvalidWithoutSuffix.Description", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")]
        [Fact()]
        public void AliasAttributeName()
        {
            string sourceCode = @"
using A = A1;
class A1 : System.Attribute { }
[/*<bind>*/A/*</bind>*/] class C { }
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("A1", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("A1", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("A1..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("A1..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.NotNull(aliasInfo);
            Assert.Equal("A=A1", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);
        }

        [WorkItem(542979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")]
        [Fact()]
        public void AliasAttributeName_02_AttributeSyntax()
        {
            string sourceCode = @"
using GooAttribute = System.ObsoleteAttribute;

[/*<bind>*/Goo/*</bind>*/]
class C { }
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.NotNull(aliasInfo);
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);
        }

        [WorkItem(542979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")]
        [Fact]
        public void AliasAttributeName_02_IdentifierNameSyntax()
        {
            string sourceCode = @"
using GooAttribute = System.ObsoleteAttribute;

[/*<bind>*/Goo/*</bind>*/]
class C { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.NotNull(aliasInfo);
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);
        }

        [WorkItem(542979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")]
        [Fact]
        public void AliasAttributeName_03_AttributeSyntax()
        {
            string sourceCode = @"
using GooAttribute = System.ObsoleteAttribute;

[/*<bind>*/GooAttribute/*</bind>*/]
class C { }
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.NotNull(aliasInfo);
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);
        }

        [WorkItem(542979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")]
        [Fact]
        public void AliasAttributeName_03_IdentifierNameSyntax()
        {
            string sourceCode = @"
using GooAttribute = System.ObsoleteAttribute;

[/*<bind>*/GooAttribute/*</bind>*/]
class C { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.NotNull(aliasInfo);
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString());
            Assert.Equal(SymbolKind.Alias, aliasInfo.Kind);
        }

        [WorkItem(542979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542979")]
        [Fact()]
        public void AliasQualifiedAttributeName_01()
        {
            string sourceCode = @"
class AttributeClass : System.Attribute
{
    class NonAttributeClass { }
}

namespace N
{
    [global::/*<bind>*/AttributeClass/*</bind>*/.NonAttributeClass()]
    class C { }

    class AttributeClass : System.Attribute { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("AttributeClass", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("AttributeClass", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("AttributeClass", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            Assert.False(SyntaxFacts.IsAttributeName(((SourceNamedTypeSymbol)((CSharp.Symbols.PublicModel.NamedTypeSymbol)semanticInfo.Symbol).UnderlyingNamedTypeSymbol).SyntaxReferences.First().GetSyntax()),
                "IsAttributeName can be true only for alias name being qualified");
        }

        [Fact]
        public void AliasQualifiedAttributeName_02()
        {
            string sourceCode = @"
class AttributeClass : System.Attribute
{
    class NonAttributeClass { }
}

namespace N
{
    [/*<bind>*/global::AttributeClass/*</bind>*/.NonAttributeClass()]
    class C { }

    class AttributeClass : System.Attribute { }
}
";
            var semanticInfo = GetSemanticInfoForTest<AliasQualifiedNameSyntax>(sourceCode);

            Assert.Equal("AttributeClass", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("AttributeClass", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("AttributeClass", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            Assert.False(SyntaxFacts.IsAttributeName(((SourceNamedTypeSymbol)((CSharp.Symbols.PublicModel.NamedTypeSymbol)semanticInfo.Symbol).UnderlyingNamedTypeSymbol).SyntaxReferences.First().GetSyntax()),
                "IsAttributeName can be true only for alias name being qualified");
        }

        [Fact]
        public void AliasQualifiedAttributeName_03()
        {
            string sourceCode = @"
class AttributeClass : System.Attribute
{
    class NonAttributeClass { }
}

namespace N
{
    [global::AttributeClass./*<bind>*/NonAttributeClass/*</bind>*/()]
    class C { }

    class AttributeClass : System.Attribute { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("AttributeClass.NonAttributeClass", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("AttributeClass.NonAttributeClass", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("AttributeClass.NonAttributeClass..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("AttributeClass.NonAttributeClass..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AliasQualifiedAttributeName_04()
        {
            string sourceCode = @"
class AttributeClass : System.Attribute
{
    class NonAttributeClass { }
}

namespace N
{
    [/*<bind>*/global::AttributeClass.NonAttributeClass/*</bind>*/()]
    class C { }

    class AttributeClass : System.Attribute { }
}
";
            var semanticInfo = GetSemanticInfoForTest<QualifiedNameSyntax>(sourceCode);

            Assert.Equal("AttributeClass.NonAttributeClass", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("AttributeClass.NonAttributeClass", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("AttributeClass.NonAttributeClass..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("AttributeClass.NonAttributeClass..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AliasAttributeName_NonAttributeAlias()
        {
            string sourceCode = @"
using GooAttribute = C;

[/*<bind>*/GooAttribute/*</bind>*/]
class C { }
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("C..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.Null(aliasInfo);
        }

        [Fact]
        public void AliasAttributeName_NonAttributeAlias_GenericType()
        {
            string sourceCode = @"
using GooAttribute = Gen<int>;

[/*<bind>*/GooAttribute/*</bind>*/]
class C { }
class Gen<T> { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Gen<System.Int32>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Gen<System.Int32>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<System.Int32>..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<System.Int32>..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.Null(aliasInfo);
        }

        [Fact]
        public void AmbigAliasAttributeName()
        {
            string sourceCode = @"
using A = A1;
using AAttribute = A2;
class A1 : System.Attribute { }
class A2 : System.Attribute { }
[/*<bind>*/A/*</bind>*/] class C { }
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("A", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("A", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("A1", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("A2", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.Null(aliasInfo);
        }

        [Fact]
        public void AmbigAliasAttributeName_02()
        {
            string sourceCode = @"
using Goo = System.ObsoleteAttribute;
class GooAttribute : System.Attribute { }
[/*<bind>*/Goo/*</bind>*/]
class C { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Goo", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("Goo", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("GooAttribute", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("System.ObsoleteAttribute", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.Null(aliasInfo);
        }

        [Fact]
        public void AmbigAliasAttributeName_03()
        {
            string sourceCode = @"
using Goo = GooAttribute;
class GooAttribute : System.Attribute { }
[/*<bind>*/Goo/*</bind>*/]
class C { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Goo", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("Goo", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("GooAttribute", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("GooAttribute", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);

            var aliasInfo = GetAliasInfoForTest(sourceCode);
            Assert.Null(aliasInfo);
        }

        [WorkItem(542018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542018")]
        [Fact]
        public void AmbigObjectCreationBind()
        {
            string sourceCode = @"
using System;

public class X
{
}

public struct X
{
}

class Class1	
{
    public static void Main()
    {
        object x = new /*<bind>*/X/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason);

            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("X", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);
            Assert.Equal("X", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[1].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
        }

        [WorkItem(542027, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542027")]
        [Fact()]
        public void NonStaticMemberOfOuterTypeAccessedViaNestedType()
        {
            string sourceCode = @"
class MyClass
{
    public int intTest = 1;

    class TestClass
    {
        public void TestMeth()
        {
            int intI = /*<bind>*/ intTest /*</bind>*/;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.StaticInstanceMismatch, semanticInfo.CandidateReason);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 MyClass.intTest", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(530093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530093")]
        [Fact()]
        public void ThisInFieldInitializer()
        {
            string sourceCode = @"
class MyClass
{
    public MyClass self = /*<bind>*/ this /*</bind>*/;
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("MyClass", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("MyClass", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotReferencable, semanticInfo.CandidateReason);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal(1, sortedCandidates.Length);
            Assert.Equal("MyClass this", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(530093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530093")]
        [Fact()]
        public void BaseInFieldInitializer()
        {
            string sourceCode = @"
class MyClass
{
    public object self = /*<bind>*/ base /*</bind>*/ .Id();
    object Id() { return this; }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(SymbolKind.Parameter, semanticInfo.CandidateSymbols[0].Kind);
            Assert.Equal(CandidateReason.NotReferencable, semanticInfo.CandidateReason);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal(1, sortedCandidates.Length);
            Assert.Equal("MyClass this", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact()]
        public void MemberAccessToInaccessibleField()
        {
            string sourceCode = @"
class MyClass1
{
    private static int myInt1 = 12;
}

class MyClass2
{
    public int myInt2 = /*<bind>*/MyClass1.myInt1/*</bind>*/;
}

";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 MyClass1.myInt1", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(528682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528682")]
        [Fact]
        public void PropertyGetAccessWithPrivateGetter()
        {
            string sourceCode = @"
public class MyClass
{
    public int Property
    {
        private get { return 0; }
        set { }
    }
}

public class Test
{
    public static void Main(string[] args)
    {
        MyClass c = new MyClass();
        int a = c./*<bind>*/Property/*</bind>*/;  
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAValue, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 MyClass.Property { private get; set; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542053")]
        [Fact]
        public void GetAccessPrivateProperty()
        {
            string sourceCode = @"
public class Test 
{
    class Class1
    {
        private int a { get { return 1; } set { } }
    }
    class Class2 : Class1
    {
        public int b() { return /*<bind>*/a/*</bind>*/; } 
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 Test.Class1.a { get; set; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542053")]
        [Fact]
        public void GetAccessPrivateField()
        {
            string sourceCode = @"
public class Test 
{
    class Class1
    {
        private int a;
    }
    class Class2 : Class1
    {
        public int b() { return /*<bind>*/a/*</bind>*/; } 
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 Test.Class1.a", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542053")]
        [Fact]
        public void GetAccessPrivateEvent()
        {
            string sourceCode = @"
using System;

public class Test 
{
    class Class1
    {
        private event Action a;
    }
    class Class2 : Class1
    {
        public Action b() { return /*<bind>*/a/*</bind>*/(); } 
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Action", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Action", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("event System.Action Test.Class1.a", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Event, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(528684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528684")]
        [Fact]
        public void PropertySetAccessWithPrivateSetter()
        {
            string sourceCode = @"
public class MyClass
{
    public int Property
    {
        get { return 0; }
        private set { }
    }
}

public class Test
{
    static void Main()
    {
        MyClass c = new MyClass();
        c./*<bind>*/Property/*</bind>*/ = 10;     
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAVariable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 MyClass.Property { get; private set; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void PropertyIndexerAccessWithPrivateSetter()
        {
            string sourceCode = @"
public class MyClass
{
    public object this[int index]
    {
        get { return null; }
        private set { }
    }
}

public class Test
{
    static void Main()
    {
        MyClass c = new MyClass();
        /*<bind>*/c[0]/*</bind>*/ = null;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAVariable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Object MyClass.this[System.Int32 index] { get; private set; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542065")]
        [Fact]
        public void GenericTypeWithNoTypeArgsOnAttribute()
        {
            string sourceCode = @"
class Gen<T> { }

[/*<bind>*/Gen/*</bind>*/]
public class Test
{
    public static int Main()
    {
        return 1;
    }
}
";

            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("Gen<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Gen<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<T>..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<T>..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542125")]
        [Fact]
        public void MalformedSyntaxSemanticModel_Bug9223()
        {
            string sourceCode = @"
public delegate int D(int x);

public st C
{
    public event D EV;

    public C(D d)
    {

        EV = /*<bind>*/d/*</bind>*/;
    }

    public int OnEV(int x)
    {
        return x;
    }
}

";
            // Don't crash or assert.
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
        }

        [WorkItem(528746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528746")]
        [Fact]
        public void ImplicitConversionArrayCreationExprInQuery()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {

        var q2 = from x in /*<bind>*/new int[] { 4, 5 }/*</bind>*/
                 select x;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32[]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542256")]
        [Fact]
        public void MalformedConditionalExprInWhereClause()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q1 = from x in new int[] { 4, 5 }
                 where /*<bind>*/new Program()/*</bind>*/ ?
                 select x;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Symbol);
            Assert.Equal("Program..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal("Program", semanticInfo.Type.Name);
        }

        [WorkItem(542230, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542230")]
        [Fact]
        public void MalformedExpressionInSelectClause()
        {
            string sourceCode = @"
using System.Linq;
 
class P
{
    static void Main()
    {
        var src = new int[] { 4, 5 };
        var q = from x in src
                select /*<bind>*/x/*</bind>*/.";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Assert.NotNull(semanticInfo.Symbol);
        }

        [WorkItem(542344, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542344")]
        [Fact]
        public void LiteralExprInGotoCaseInsideSwitch()
        {
            string sourceCode = @"
public class Test
{
    public static void Main()
    {
        int ret = 6;

        switch (ret)
        {
            case 0:
                goto case /*<bind>*/2/*</bind>*/;
            case 2:
                break;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);
            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
            Assert.Equal(2, semanticInfo.ConstantValue);
        }

        [WorkItem(542405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542405")]
        [Fact]
        public void ImplicitConvCaseConstantExpr()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        long number = 45;

        switch (number)
        {
            case /*<bind>*/21/*</bind>*/:
                break;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(21, semanticInfo.ConstantValue);
        }

        [WorkItem(542405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542405")]
        [Fact]
        public void ErrorConvCaseConstantExpr()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        double number = 45;

        switch (number)
        {
            case /*<bind>*/21/*</bind>*/:
                break;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode, parseOptions: TestOptions.Regular6);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(21, semanticInfo.ConstantValue);
        }

        [WorkItem(542405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542405")]
        [Fact]
        public void ImplicitConvGotoCaseConstantExpr()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        long number = 45;

        switch (number)
        {
            case 1:
                goto case /*<bind>*/21/*</bind>*/;
            case 21:
                break;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(21, semanticInfo.ConstantValue);
        }

        [WorkItem(542405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542405")]
        [Fact]
        public void ErrorConvGotoCaseConstantExpr()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        double number = 45;

        switch (number)
        {
            case 1:
                goto case /*<bind>*/21/*</bind>*/;
            case 21:
                break;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode, parseOptions: TestOptions.Regular6);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(21, semanticInfo.ConstantValue);
        }

        [WorkItem(542351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542351")]
        [Fact]
        public void AttributeSemanticInfo_OverloadResolutionFailure_01()
        {
            string sourceCode = @"
[module: /*<bind>*/System.Obsolete(typeof(.<>))/*</bind>*/]

";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);
            Verify_AttributeSemanticInfo_OverloadResolutionFailure_Common(semanticInfo);
        }

        [WorkItem(542351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542351")]
        [Fact]
        public void AttributeSemanticInfo_OverloadResolutionFailure_02()
        {
            string sourceCode = @"
[module: System./*<bind>*/Obsolete/*</bind>*/(typeof(.<>))]

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Verify_AttributeSemanticInfo_OverloadResolutionFailure_Common(semanticInfo);
        }

        private void Verify_AttributeSemanticInfo_OverloadResolutionFailure_Common(CompilationUtils.SemanticInfoSummary semanticInfo)
        {
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(3, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedCandidates[2].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[2].Kind);

            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.ObsoleteAttribute..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", sortedMethodGroup[1].ToTestDisplayString());
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message, System.Boolean error)", sortedMethodGroup[2].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542351")]
        [Fact]
        public void ObjectCreationSemanticInfo_OverloadResolutionFailure()
        {
            string sourceCode = @"
using System;
class Goo
{
    public Goo() { }
    public Goo(int x) { }

    public static void Main()
    {
        var x = new /*<bind>*/Goo/*</bind>*/(typeof(.<>));
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Goo", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectCreationSemanticInfo_OverloadResolutionFailure_2()
        {
            string sourceCode = @"
using System;
class Goo
{
    public Goo() { }
    public Goo(int x) { }

    public static void Main()
    {
        var x = /*<bind>*/new Goo(typeof(Goo))/*</bind>*/;
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Goo", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Goo", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Goo..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("Goo..ctor(System.Int32 x)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Goo..ctor()", sortedMethodGroup[0].ToTestDisplayString());
            Assert.Equal("Goo..ctor(System.Int32 x)", sortedMethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ParameterDefaultValue1()
        {
            string sourceCode = @"
using System;

class Constants
{
    public const short k = 9;
}

public class Class1
{
    const int i = 12;
    const int j = 14;
    void f(long i = 32 + Constants./*<bind>*/k/*</bind>*/, long j = i)
    { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int16", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int16 Constants.k", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal((short)9, semanticInfo.ConstantValue);
        }

        [Fact]
        public void ParameterDefaultValue2()
        {
            string sourceCode = @"
using System;

class Constants
{
    public const short k = 9;
}

public class Class1
{
    const int i = 12;
    const int j = 14;
    void f(long i = 32 + Constants.k, long j = /*<bind>*/i/*</bind>*/)
    { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 Class1.i", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(12, semanticInfo.ConstantValue);
        }

        [Fact]
        public void ParameterDefaultValueInConstructor()
        {
            string sourceCode = @"
using System;

class Constants
{
    public const short k = 9;
}

public class Class1
{
    const int i = 12;
    const int j = 14;
    Class1(long i = 32 + Constants.k, long j = /*<bind>*/i/*</bind>*/)
    { }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 Class1.i", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(12, semanticInfo.ConstantValue);
        }

        [Fact]
        public void ParameterDefaultValueInIndexer()
        {
            string sourceCode = @"
using System;

class Constants
{
    public const short k = 9;
}

public class Class1
{
    const int i = 12;
    const int j = 14;
    public string this[long i = 32 + Constants.k, long j = /*<bind>*/i/*</bind>*/]
    {
        get { return """"; }
        set { }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 Class1.i", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(12, semanticInfo.ConstantValue);
        }

        [WorkItem(542589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542589")]
        [Fact]
        public void UnrecognizedGenericTypeReference()
        {
            string sourceCode = "/*<bind>*/C<object, string/*</bind>*/";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            var type = (INamedTypeSymbol)semanticInfo.Type;
            Assert.Equal("System.Boolean", type.ToTestDisplayString());
        }

        [WorkItem(542452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542452")]
        [Fact]
        public void LambdaInSelectExpressionWithObjectCreation()
        {
            string sourceCode = @"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{
    static void Main() { }

    static void Goo(List<int> Scores)
    {
        var z = from y in Scores select new Action(() => { /*<bind>*/var/*</bind>*/ x = y; });
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DefaultOptionalParamValue()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    const bool v = true;
    public void Goo(bool b = /*<bind>*/v == true/*</bind>*/)
    {
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<BinaryExpressionSyntax>(sourceCode);

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Boolean", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Boolean System.Boolean.op_Equality(System.Boolean left, System.Boolean right)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(true, semanticInfo.ConstantValue);
        }

        [Fact]
        public void DefaultOptionalParamValueWithGenericTypes()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public void Goo<T, U>(T t = /*<bind>*/default(U)/*</bind>*/) where U : class, T
    {
    }
    static void Main(string[] args)
    {
        
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<DefaultExpressionSyntax>(sourceCode);

            Assert.Equal("U", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.TypeParameter, semanticInfo.Type.TypeKind);
            Assert.Equal("T", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.TypeParameter, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Null(semanticInfo.ConstantValue.Value);
        }

        [WorkItem(542850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542850")]
        [Fact]
        public void InaccessibleExtensionMethod()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

public static class Extensions
{
    private static int Goo(this string z) { return 3; }
}

class Program
{
    static void Main(string[] args)
    {
        args[0]./*<bind>*/Goo/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 System.String.Goo()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 System.String.Goo()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542883")]
        [Fact]
        public void InaccessibleNamedAttrArg()
        {
            string sourceCode = @"
using System;

public class B : Attribute
{
    private int X;
}

[B(/*<bind>*/X/*</bind>*/ = 5)]
public class D { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 B.X", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(528914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528914")]
        [Fact]
        public void InvalidIdentifierAsAttrArg()
        {
            string sourceCode = @"
using System.Runtime.CompilerServices;

public interface Interface1
{
    [/*<bind>*/IndexerName(null)/*</bind>*/]
    string this[int arg]
    {
        get;
        set;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<AttributeSyntax>(sourceCode);

            Assert.Equal("System.Runtime.CompilerServices.IndexerNameAttribute", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Runtime.CompilerServices.IndexerNameAttribute", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Runtime.CompilerServices.IndexerNameAttribute..ctor(System.String indexerName)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Runtime.CompilerServices.IndexerNameAttribute..ctor(System.String indexerName)", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542890")]
        [Fact()]
        public void GlobalIdentifierName()
        {
            string sourceCode = @"
class Test
{
    static void Main()
    {
        var t1 = new /*<bind>*/global/*</bind>*/::Test();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            var aliasInfo = GetAliasInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("<global namespace>", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind);
            Assert.True(((INamespaceSymbol)semanticInfo.Symbol).IsGlobalNamespace);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.Equal("global", aliasInfo.Name);
            Assert.Equal("<global namespace>", aliasInfo.Target.ToTestDisplayString());
            Assert.True(((NamespaceSymbol)(aliasInfo.Target)).IsGlobalNamespace);
            Assert.False(aliasInfo.IsExtern);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact()]
        public void GlobalIdentifierName2()
        {
            string sourceCode = @"
class Test
{
    /*<bind>*/global/*</bind>*/::Test f;
    static void Main()
    {
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            var aliasInfo = GetAliasInfoForTest(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("<global namespace>", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind);
            Assert.True(((INamespaceSymbol)semanticInfo.Symbol).IsGlobalNamespace);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.Equal("global", aliasInfo.Name);
            Assert.Equal("<global namespace>", aliasInfo.Target.ToTestDisplayString());
            Assert.True(((NamespaceSymbol)(aliasInfo.Target)).IsGlobalNamespace);
            Assert.False(aliasInfo.IsExtern);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(542536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542536")]
        [Fact]
        public void UndeclaredSymbolInDefaultParameterValue()
        {
            string sourceCode = @"
class Program
{
    const int y = 1;
    public void Goo(bool x = (undeclared == /*<bind>*/y/*</bind>*/)) { }
    static void Main(string[] args)
    {
        
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 Program.y", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(1, semanticInfo.ConstantValue);
        }

        [WorkItem(543198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543198")]
        [Fact]
        public void NamespaceAliasInsideMethod()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

using A = NS1;



namespace NS1
{
    class B { }
}

class Program
{
    class A
    {
    }

    A::B y = null;

    void Main()
    {
        /*<bind>*/A/*</bind>*/::B.Equals(null, null);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            // Should bind to namespace alias A=NS1, not class Program.A.
            Assert.Equal("NS1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_ImplicitArrayCreationSyntax()
        {
            string sourceCode = @"
using System;

namespace Test
{
    public class Program
    {
        public static int Main()
        {
            var a = /*<bind>*/new[] { 1, 2, 3 }/*</bind>*/;

            return a[0];
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32[]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;

namespace Test
{
    public class Program
    {
        public static int Main()
        {
            var a = new[] { 1, 2, 3 };

            return /*<bind>*/a/*</bind>*/[0];
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32[]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32[] a", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_MultiDim_ImplicitArrayCreationSyntax()
        {
            string sourceCode = @"
using System;

namespace Test
{
    public class Program
    {
        public int[][, , ] Goo()
        {
            var a3 = new[] { /*<bind>*/new [,,] {{{1, 2}}}/*</bind>*/, new [,,] {{{3, 4}}} };
            return a3;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32[,,]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32[,,]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_MultiDim_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;

namespace Test
{
    public class Program
    {
        public int[][, , ] Goo()
        {
            var a3 = new[] { new [,,] {{{3, 4}}}, new [,,] {{{3, 4}}} };
            return /*<bind>*/a3/*</bind>*/;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32[][,,]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32[][,,]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32[][,,] a3", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_Error_ImplicitArrayCreationSyntax()
        {
            string sourceCode = @"
public class C
{
    public int[] Goo()
    {
        char c = 'c';
        short s1 = 0;
        short s2 = -0;
        short s3 = 1;
        short s4 = -1;

        var array1 = /*<bind>*/new[] { s1, s2, s3, s4, c, '1' }/*</bind>*/; // CS0826
        return array1;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("?[]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("?[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_Error_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class C
{
    public int[] Goo()
    {
        char c = 'c';
        short s1 = 0;
        short s2 = -0;
        short s3 = 1;
        short s4 = -1;

        var array1 = new[] { s1, s2, s3, s4, c, '1' }; // CS0826
        return /*<bind>*/array1/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("?[]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("?[] array1", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_Error_NonArrayInitExpr()
        {
            string sourceCode = @"
using System;

namespace Test
{
    public class Program
    {
        public int[][,,] Goo()
        {
            var a3 = new[] { /*<bind>*/new[,,] { { { 3, 4 } }, 3, 4 }/*</bind>*/, new[,,] { { { 3, 4 } } } };
            return a3;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("?[,,]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("?[,,]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_Error_NonArrayInitExpr_02()
        {
            string sourceCode = @"
using System;

namespace Test
{
    public class Program
    {
        public int[][,,] Goo()
        {
            var a3 = new[] { /*<bind>*/new[,,] { { { 3, 4 } }, x, y }/*</bind>*/, new[,,] { { { 3, 4 } } } };
            return a3;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("?[,,]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("?[,,]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ImplicitArrayCreationExpression_Inside_ErrorImplicitArrayCreation()
        {
            string sourceCode = @"
public class C
{
    public int[] Goo()
    {
        char c = 'c';
        short s1 = 0;
        short s2 = -0;
        short s3 = 1;
        short s4 = -1;

        var array1 = new[] { /*<bind>*/new[] { 1, 2 }/*</bind>*/, new[] { s1, s2, s3, s4, c, '1' } }; // CS0826
        return array1;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32[]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("?[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact, WorkItem(543201, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543201")]
        public void BindVariableIncompleteForLoop()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        for (int i = 0; /*<bind>*/i/*</bind>*/
    }
}";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
        }

        [Fact, WorkItem(542843, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542843")]
        public void Bug10245()
        {
            string sourceCode = @"
class C<T> {
    public T Field;
}
class D {
    void M() {
        new C<int>./*<bind>*/Field/*</bind>*/.ToString();
    }
}
";
            var tree = Parse(sourceCode);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var symbolInfo = model.GetSymbolInfo(expr);

            Assert.Equal(CandidateReason.NotATypeOrNamespace, symbolInfo.CandidateReason);
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length);
            Assert.Equal("System.Int32 C<System.Int32>.Field", symbolInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Null(symbolInfo.Symbol);
        }

        [Fact]
        public void StaticClassWithinNew()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = new /*<bind>*/Stat/*</bind>*/();
    }
}

static class Stat { }
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("Stat", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.CandidateSymbols[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void StaticClassWithinNew2()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/new Stat()/*</bind>*/;
    }
}

static class Stat { }
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Stat", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void StaticClassWithinNew_ImplicitCreation()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Stat s = /*<bind>*/new()/*</bind>*/;
    }
}

static class Stat { }
";
            var semanticInfo = GetSemanticInfoForTest<ImplicitObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Stat", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Stat", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543534, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543534")]
        [Fact]
        public void InterfaceWithNew()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = new /*<bind>*/X/*</bind>*/();
    }
}

interface X { }

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("X", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InterfaceWithNew2()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/new X()/*</bind>*/;
    }
}

interface X { }

";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("X", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InterfaceWithNew_ImplicitCreation()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        X x = /*<bind>*/new()/*</bind>*/;
    }
}

interface X { }

";
            var semanticInfo = GetSemanticInfoForTest<ImplicitObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("X", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("X", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void TypeParameterWithNew()
        {
            string sourceCode = @"
using System;

class Program<T>
{
    static void f()
    {
        object o = new /*<bind>*/T/*</bind>*/();
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("T", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.TypeParameter, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Theory]
        [InlineData("", (byte)ConversionKind.Boxing)]
        [InlineData("where T : new()", (byte)ConversionKind.Boxing)]
        [InlineData("where T : class, new()", (byte)ConversionKind.ImplicitReference)]
        [InlineData("where T : struct", (byte)ConversionKind.Boxing)]
        public void TypeParameterWithNew2(string constraintClause, byte conversionKind)
        {
            string sourceCode = $$"""
                using System;

                class Program<T> {{constraintClause}}
                {
                    static void f()
                    {
                        object o = /*<bind>*/new T()/*</bind>*/;
                    }
                }
                """;

            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("T", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.TypeParameter, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal((ConversionKind)conversionKind, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Theory]
        [InlineData("", (byte)ConversionKind.NoConversion)]
        [InlineData("where T : new()", (byte)ConversionKind.ObjectCreation)]
        [InlineData("where T : class, new()", (byte)ConversionKind.ObjectCreation)]
        [InlineData("where T : struct", (byte)ConversionKind.ObjectCreation)]
        public void TypeParameterWithNew_ImplicitCreation(string constraintClause, byte conversionKind)
        {
            string sourceCode = $$"""
                using System;

                class Program<T> {{constraintClause}}
                {
                    static void f()
                    {
                        T t = /*<bind>*/new()/*</bind>*/;
                    }
                }
                """;

            var semanticInfo = GetSemanticInfoForTest<ImplicitObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("T", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.TypeParameter, semanticInfo.Type.TypeKind);
            Assert.Equal("T", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.TypeParameter, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal((ConversionKind)conversionKind, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void AbstractClassWithNew_01()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = new /*<bind>*/X/*</bind>*/();
    }
}

abstract class X { }

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("X", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MemberGroup.Length);
        }

        [Fact]
        public void AbstractClassWithNew_02()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = new /*<bind>*/X/*</bind>*/();
    }
}

abstract class X
{
    public X() {}
}

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("X", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MemberGroup.Length);
        }

        [Fact]
        public void AbstractClassWithNew2()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/new X()/*</bind>*/;
    }
}

abstract class X { }

";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("X", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("X..ctor()", semanticInfo.CandidateSymbols.First().ToTestDisplayString());

            Assert.Equal(0, semanticInfo.MemberGroup.Length);
        }

        [Fact]
        public void AbstractClassWithNew_ImplicitCreation()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        X x = /*<bind>*/new()/*</bind>*/;
    }
}

abstract class X { }

";
            var semanticInfo = GetSemanticInfoForTest<ImplicitObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("X", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("X", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("X..ctor()", semanticInfo.CandidateSymbols.First().ToTestDisplayString());

            Assert.Equal(0, semanticInfo.MemberGroup.Length);
        }

        [Fact()]
        public void DynamicWithNew()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = new /*<bind>*/dynamic/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("dynamic", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact()]
        public void DynamicWithNew2()
        {
            string sourceCode = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        object o = /*<bind>*/new dynamic()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("dynamic", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Dynamic, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_SwitchGoverningImplicitUserDefined_01()
        {
            // There must be exactly one user-defined conversion to a non-nullable integral type,
            // and there is.

            string sourceCode = @"
struct Conv
{
    public static implicit operator int (Conv C)
    {
        return 1;
    }

    public static int Main()
    {
        Conv C = new Conv();
        switch (/*<bind>*/C/*</bind>*/)
        {
            default:
                return 1;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Conv", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitUserDefined, semanticInfo.ImplicitConversion.Kind);
            Assert.Equal("Conv.implicit operator int(Conv)", semanticInfo.ImplicitConversion.Method.ToString());

            Assert.Equal("Conv C", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_SwitchGoverningImplicitUserDefined_02()
        {
            // The specification requires that the user-defined conversion chosen be one
            // which converts to an integral or string type, but *not* a nullable integral type,
            // oddly enough. Since the only applicable user-defined conversion here would be the
            // lifted conversion from Conv? to int?, the resolution of the conversion fails
            // and this program produces an error.

            string sourceCode = @"
struct Conv
{
    public static implicit operator int (Conv C)
    {
        return 1;
    }

    public static int Main()
    {
        Conv? C = new Conv();
        switch (/*<bind>*/C/*</bind>*/)
        {
            default:
                return 1;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Conv?", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("Conv? C", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_SwitchGoverningImplicitUserDefined_Error_01()
        {
            string sourceCode = @"
struct Conv
{
    public static implicit operator int (Conv C)
    {
        return 1;
    }

    public static implicit operator int? (Conv? C)
    {
        return null;
    }

    public static int Main()
    {
        Conv C = new Conv();
        switch (/*<bind>*/C/*</bind>*/)
        {
            default:
                return 0;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode, parseOptions: TestOptions.Regular6);

            Assert.Equal("Conv", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("Conv", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Conv C", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_SwitchGoverningImplicitUserDefined_Error_02()
        {
            string sourceCode = @"
struct Conv
{
    public static int Main()
    {
        Conv C = new Conv();
        switch (/*<bind>*/C/*</bind>*/)
        {
            default:
                return 0;
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode, parseOptions: TestOptions.Regular6);

            Assert.Equal("Conv", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("Conv", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Conv C", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = /*<bind>*/new MemberInitializerTest() { x = 1, y = 2 }/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("MemberInitializerTest", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("MemberInitializerTest", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("MemberInitializerTest..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("MemberInitializerTest..ctor()", semanticInfo.MethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_InitializerExpressionSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() /*<bind>*/{ x = 1, y = 2 }/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_MemberInitializerAssignment_BinaryExpressionSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { /*<bind>*/x = 1/*</bind>*/, y = 2 };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<AssignmentExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_FieldAccess_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { /*<bind>*/x/*</bind>*/ = 1, y = 2 };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 MemberInitializerTest.x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_PropertyAccess_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, /*<bind>*/y/*</bind>*/ = 2 };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 MemberInitializerTest.y { get; set; }", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_TypeParameterBaseFieldAccess_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class Base
{
    public Base() { }
    public int x;
    public int y { get; set; }

    public static void Main()
    {
        MemberInitializerTest<Base>.Goo();
    }
}

public class MemberInitializerTest<T> where T : Base, new()
{
    public static void Goo()
    {
        var i = new T() { /*<bind>*/x/*</bind>*/ = 1, y = 2 };
        System.Console.WriteLine(i.x);
        System.Console.WriteLine(i.y);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 Base.x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_NestedInitializer_InitializerExpressionSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }
}

public class Test
{
    public readonly MemberInitializerTest m = new MemberInitializerTest();

    public static void Main()
    {
        var i = new Test() { m = /*<bind>*/{ x = 1, y = 2 }/*</bind>*/ };
        System.Console.WriteLine(i.m.x);
        System.Console.WriteLine(i.m.y);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_NestedInitializer_PropertyAccess_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }
}

public class Test
{
    public readonly MemberInitializerTest m = new MemberInitializerTest();

    public static void Main()
    {
        var i = new Test() { m = { x = 1, /*<bind>*/y/*</bind>*/ = 2 } };
        System.Console.WriteLine(i.m.x);
        System.Console.WriteLine(i.m.y);
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 MemberInitializerTest.y { get; set; }", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_InaccessibleMember_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    protected int x;
    private int y { get; set; }
    internal int z;
}

public class Test
{
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, /*<bind>*/y/*</bind>*/ = 2, z = 3 };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 MemberInitializerTest.y { get; set; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_ReadOnlyPropertyAssign_IdentifierNameSyntax()
        {
            string sourceCode = @"
public struct MemberInitializerTest
{
    public readonly int x;
    public int y { get { return 0; } }
}

public struct Test
{
    public static void Main()
    {
        var i = new MemberInitializerTest() { /*<bind>*/y/*</bind>*/ = 2 };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAVariable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.Int32 MemberInitializerTest.y { get; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_WriteOnlyPropertyAccess_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public int y { get; set; }
}

public class Test
{
    public MemberInitializerTest m;
    public MemberInitializerTest Prop { set { m = value; } }

    public static void Main()
    {
        var i = new Test() { /*<bind>*/Prop/*</bind>*/ = { x = 1, y = 2 } };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("MemberInitializerTest", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("MemberInitializerTest", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAValue, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("MemberInitializerTest Test.Prop { set; }", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Property, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_ErrorInitializerType_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public static void Main()
    {
        var i = new X() { /*<bind>*/x/*</bind>*/ = 0 };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_InvalidElementInitializer_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x, y;
    public static void Main()
    {
        var i = new MemberInitializerTest { x = 0, /*<bind>*/y/*</bind>*/++ };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 MemberInitializerTest.y", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_InvalidElementInitializer_InvocationExpressionSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public MemberInitializerTest Goo() { return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 0, /*<bind>*/Goo()/*</bind>*/};
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("MemberInitializerTest", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("MemberInitializerTest", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var symbol = semanticInfo.CandidateSymbols[0];
            Assert.Equal("MemberInitializerTest MemberInitializerTest.Goo()", symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, symbol.Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_BadNamedAssignmentLeft_InvocationExpressionSyntax_01()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public MemberInitializerTest Goo() { return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 0, /*<bind>*/Goo()/*</bind>*/ = new MemberInitializerTest() };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("MemberInitializerTest", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("MemberInitializerTest", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var symbol = semanticInfo.CandidateSymbols[0];
            Assert.Equal("MemberInitializerTest MemberInitializerTest.Goo()", symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, symbol.Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_BadNamedAssignmentLeft_InvocationExpressionSyntax_02()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public static MemberInitializerTest Goo() { return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 0, /*<bind>*/Goo()/*</bind>*/ = new MemberInitializerTest() };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("MemberInitializerTest", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("MemberInitializerTest", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAVariable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var symbol = semanticInfo.CandidateSymbols[0];
            Assert.Equal("MemberInitializerTest MemberInitializerTest.Goo()", symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, symbol.Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_MethodGroupNamedAssignmentLeft_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public MemberInitializerTest Goo() { return new MemberInitializerTest(); }
    public static void Main()
    {
        var i = new MemberInitializerTest() { /*<bind>*/Goo/*</bind>*/ };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("MemberInitializerTest MemberInitializerTest.Goo()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("MemberInitializerTest MemberInitializerTest.Goo()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectInitializer_DuplicateMemberInitializer_IdentifierNameSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int x;
    public static void Main()
    {
        var i = new MemberInitializerTest() { x = 1, /*<bind>*/x/*</bind>*/ = 2 };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 MemberInitializerTest.x", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var i = 2;
        B coll = /*<bind>*/new B { 1, i, { 4L }, { 9 }, 3L }/*</bind>*/;
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("B", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("B", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("B..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("B..ctor()", semanticInfo.MethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_InitializerExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var i = 2;
        B coll = new B /*<bind>*/{ 1, i, { 4L }, { 9 }, 3L }/*</bind>*/;
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_ElementInitializer_LiteralExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var i = 2;
        B coll = new B { /*<bind>*/1/*</bind>*/, i, { 4L }, { 9 }, 3L };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<LiteralExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.True(semanticInfo.IsCompileTimeConstant);
            Assert.Equal(1, semanticInfo.ConstantValue);
        }

        [Fact]
        public void CollectionInitializer_ElementInitializer_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var i = 2;
        B coll = new B { 1, /*<bind>*/i/*</bind>*/, { 4L }, { 9 }, 3L };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.ImplicitNumeric, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 i", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_ComplexElementInitializer_InitializerExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var i = 2;
        B coll = new B { 1, i, /*<bind>*/{ 4L }/*</bind>*/, { 9 }, 3L };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(long i)
    {
        list.Add(i);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_ComplexElementInitializer_Empty_InitializerExpressionSyntax()
        {
            string sourceCode = @"
using System.Collections.Generic;

public class MemberInitializerTest
{
    public List<int> y;
    public static void Main()
    {
        i = new MemberInitializerTest { y = { /*<bind>*/{ }/*</bind>*/ } };  // CS1920
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_ComplexElementInitializer_AddMethodOverloadResolutionFailure()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var i = 2;
        B coll = new B { /*<bind>*/{ 1, 2 }/*</bind>*/ };
        DisplayCollection(coll.GetEnumerator());
        return 0;
    }

    public static void DisplayCollection(IEnumerator collection)
    {
        while (collection.MoveNext())
        {
            Console.WriteLine(collection.Current);
        }
    }
}

public class B : IEnumerable
{
    List<object> list = new List<object>();

    public void Add(float i, int j)
    {
        list.Add(i);
        list.Add(j);
    }

    public void Add(int i, float j)
    {
        list.Add(i);
        list.Add(j);
    }

    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < list.Count; i++)
            yield return list[i];
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_Empty_InitializerExpressionSyntax()
        {
            string sourceCode = @"
using System.Collections.Generic;

public class MemberInitializerTest
{
    public static void Main()
    {
        var i = new List<int>() /*<bind>*/{ }/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_Nested_InitializerExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Collections;

class Test
{
    public static int Main()
    {
        var coll = new List<B> { new B(0) { list = new List<int>() { 1, 2, 3 } }, new B(1) { list = /*<bind>*/{ 2, 3 }/*</bind>*/ } };
        DisplayCollection(coll);
        return 0;
    }

    public static void DisplayCollection(IEnumerable<B> collection)
    {
        foreach (var i in collection)
        {
            i.Display();
        }
    }
}

public class B
{
    public List<int> list = new List<int>();
    public B() { }
    public B(int i) { list.Add(i); }

    public void Display()
    {
        foreach (var i in list)
        {
            Console.WriteLine(i);
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_InitializerTypeNotIEnumerable_InitializerExpressionSyntax()
        {
            string sourceCode = @"
class MemberInitializerTest
{
    public static int Main()
    {
        B coll = new B /*<bind>*/{ 1 }/*</bind>*/;
        return 0;
    }
}

class B
{
    public B() { }
}
";
            var semanticInfo = GetSemanticInfoForTest<InitializerExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_InvalidInitializer_PostfixUnaryExpressionSyntax()
        {
            string sourceCode = @"
public class MemberInitializerTest
{
    public int y;
    public static void Main()
    {
        var i = new MemberInitializerTest { /*<bind>*/y++/*</bind>*/ };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<PostfixUnaryExpressionSyntax>(sourceCode);

            Assert.Equal("?", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);

            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer_InvalidInitializer_BinaryExpressionSyntax()
        {
            string sourceCode = @"
using System.Collections.Generic;
public class MemberInitializerTest
{
    public int x;
    static MemberInitializerTest Goo() { return new MemberInitializerTest(); }

    public static void Main()
    {
        int y = 0;
        var i = new List<int> { 1, /*<bind>*/Goo().x = 1/*</bind>*/};
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<AssignmentExpressionSyntax>(sourceCode);

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_SimpleNameWithGenericTypeInAttribute()
        {
            string sourceCode = @"
class Gen<T> { }
class Gen2<T> : System.Attribute { }

[/*<bind>*/Gen/*</bind>*/]
[Gen2]
public class Test
{
    public static int Main()
    {
        return 1;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Gen<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Gen<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<T>..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen<T>..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_SimpleNameWithGenericTypeInAttribute_02()
        {
            string sourceCode = @"
class Gen<T> { }
class Gen2<T> : System.Attribute { }

[Gen]
[/*<bind>*/Gen2/*</bind>*/]
public class Test
{
    public static int Main()
    {
        return 1;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("Gen2<T>", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("Gen2<T>", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen2<T>..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Gen2<T>..ctor()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void SemanticInfo_VarKeyword_LocalDeclaration()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/var/*</bind>*/ rand = new Random();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("System.Random", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Random", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Random", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void SemanticInfo_VarKeyword_FieldDeclaration()
        {
            string sourceCode = @"
class Program
{
    /*<bind>*/var/*</bind>*/ x = 1;
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("var", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("var", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void SemanticInfo_VarKeyword_MethodReturnType()
        {
            string sourceCode = @"
class Program
{
    /*<bind>*/var/*</bind>*/ Goo() {}
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("var", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind);
            Assert.Equal("var", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543860")]
        [Fact]
        public void SemanticInfo_InterfaceCreation_With_CoClass_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

class CoClassType : InterfaceType { }

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(CoClassType))]
interface InterfaceType { }

public class Program
{
    public static void Main()
    {
        var a = new /*<bind>*/InterfaceType/*</bind>*/();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("InterfaceType", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(546242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546242")]
        [Fact]
        public void SemanticInfo_InterfaceArrayCreation_With_CoClass_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

class CoClassType : InterfaceType { }

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(CoClassType))]
interface InterfaceType { }

public class Program
{
    public static void Main()
    {
        var a = new /*<bind>*/InterfaceType/*</bind>*/[] { };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("InterfaceType", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("InterfaceType", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("InterfaceType", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_InterfaceCreation_With_CoClass_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

class CoClassType : InterfaceType { }

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(CoClassType))]
interface InterfaceType { }

public class Program
{
    public static void Main()
    {
        var a = /*<bind>*/new InterfaceType()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("InterfaceType", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("InterfaceType", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("CoClassType..ctor()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("CoClassType..ctor()", semanticInfo.MethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(546242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546242")]
        [Fact]
        public void SemanticInfo_InterfaceArrayCreation_With_CoClass_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

class CoClassType : InterfaceType { }

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(CoClassType))]
interface InterfaceType { }

public class Program
{
    public static void Main()
    {
        var a = /*<bind>*/new InterfaceType[] { }/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ArrayCreationExpressionSyntax>(sourceCode);

            Assert.Equal("InterfaceType[]", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind);
            Assert.Equal("InterfaceType[]", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_InterfaceCreation_With_Generic_CoClass_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

public class GenericCoClassType<T, U> : NonGenericInterfaceType
{
    public GenericCoClassType(U x) { Console.WriteLine(x); }
}

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(GenericCoClassType<int, string>))]
public interface NonGenericInterfaceType
{
}

public class MainClass
{
    public static int Main()
    {
        var a = new /*<bind>*/NonGenericInterfaceType/*</bind>*/(""string"");
        return 0;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("NonGenericInterfaceType", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_InterfaceCreation_With_Generic_CoClass_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

public class GenericCoClassType<T, U> : NonGenericInterfaceType
{
    public GenericCoClassType(U x) { Console.WriteLine(x); }
}

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(GenericCoClassType<int, string>))]
public interface NonGenericInterfaceType
{
}

public class MainClass
{
    public static int Main()
    {
        var a = /*<bind>*/new NonGenericInterfaceType(""string"")/*</bind>*/;
        return 0;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("NonGenericInterfaceType", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("NonGenericInterfaceType", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("GenericCoClassType<System.Int32, System.String>..ctor(System.String x)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("GenericCoClassType<System.Int32, System.String>..ctor(System.String x)", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_InterfaceCreation_With_Inaccessible_CoClass_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

public class Wrapper
{
    private class CoClassType : InterfaceType
    {
    }

    [ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
    [CoClass(typeof(CoClassType))]
    public interface InterfaceType
    {
    }
}

public class MainClass
{
    public static int Main()
    {
        var a = new Wrapper./*<bind>*/InterfaceType/*</bind>*/();
        return 0;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("Wrapper.InterfaceType", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_InterfaceCreation_With_Inaccessible_CoClass_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

public class Wrapper
{
    private class CoClassType : InterfaceType
    {
    }

    [ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
    [CoClass(typeof(CoClassType))]
    public interface InterfaceType
    {
    }
}

public class MainClass
{
    public static int Main()
    {
        var a = /*<bind>*/new Wrapper.InterfaceType()/*</bind>*/;
        return 0;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("Wrapper.InterfaceType", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("Wrapper.InterfaceType", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("Wrapper.CoClassType..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("Wrapper.CoClassType..ctor()", semanticInfo.MethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_InterfaceCreation_With_Invalid_CoClass_ObjectCreationExpressionSyntax()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(int))]
public interface InterfaceType
{
}

public class MainClass
{
    public static int Main()
    {
        var a = /*<bind>*/new InterfaceType()/*</bind>*/;
        return 0;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("InterfaceType", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("InterfaceType", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void SemanticInfo_InterfaceCreation_With_Invalid_CoClass_ObjectCreationExpressionSyntax_2()
        {
            string sourceCode = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810 - 0000 - 0000 - C000 - 000000000046"")]
[CoClass(typeof(int))]
public interface InterfaceType
{
}

public class MainClass
{
    public static int Main()
    {
        var a = new /*<bind>*/InterfaceType/*</bind>*/();
        return 0;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("InterfaceType", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(543593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543593")]
        [Fact]
        public void IncompletePropertyAccessStatement()
        {
            string sourceCode =
@"class C
{
    static void M()
    {
        var c = new { P = 0 };
        /*<bind>*/c.P.Q/*</bind>*/ x;
    }
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.Null(semanticInfo.Symbol);
        }

        [WorkItem(544449, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544449")]
        [Fact]
        public void IndexerAccessorWithSyntaxErrors()
        {
            string sourceCode =
@"public abstract int this[int i]
    (
{
    /*<bind>*/get/*</bind>*/;
    set;
}";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);
            Assert.Null(semanticInfo.Symbol);
        }

        [WorkItem(545040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545040")]
        [Fact]
        public void OmittedArraySizeExpressionSyntax()
        {
            string sourceCode =
@"
class A
{
    public static void Main()
    {
        var arr = new int[5][
        ];
    }
}
";
            var compilation = CreateCompilation(sourceCode);
            var tree = compilation.SyntaxTrees.First();
            var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<OmittedArraySizeExpressionSyntax>().Last();
            var model = compilation.GetSemanticModel(tree);
            var typeInfo = model.GetTypeInfo(node); // Ensure that this doesn't throw.

            Assert.NotEqual(default, typeInfo);
        }

        [WorkItem(11451, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void InvalidNewInterface()
        {
            string sourceCode = @"
using System;
public class Program
{
    static void Main(string[] args)
    {
        var c = new /*<bind>*/IFormattable/*</bind>*/
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.NotCreatable, semanticInfo.CandidateReason);
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("System.IFormattable", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, sortedCandidates[0].Kind);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void InvalidNewInterface2()
        {
            string sourceCode = @"
using System;
public class Program
{
    static void Main(string[] args)
    {
        var c = /*<bind>*/new IFormattable()/*</bind>*/
    }
}

";
            var semanticInfo = GetSemanticInfoForTest<ObjectCreationExpressionSyntax>(sourceCode);

            Assert.Equal("System.IFormattable", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind);
            Assert.Equal("System.IFormattable", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(545376, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545376")]
        [Fact]
        public void AssignExprInExternEvent()
        {
            string sourceCode = @"
struct Class1
{
	public event EventHandler e2;
	extern public event EventHandler e1 = /*<bind>*/ e2 = new EventHandler(this, new EventArgs()) = null /*</bind>*/;
}
";
            var semanticInfo = GetSemanticInfoForTest<AssignmentExpressionSyntax>(sourceCode);

            Assert.NotNull(semanticInfo.Type);
        }

        [Fact, WorkItem(531416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531416")]
        public void VarEvent()
        {
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(@"
event /*<bind>*/var/*</bind>*/ goo;
");
            Assert.True(((ITypeSymbol)semanticInfo.Type).IsErrorType());
        }

        [WorkItem(546083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546083")]
        [Fact]
        public void GenericMethodAssignedToDelegateWithDeclErrors()
        {
            string sourceCode = @"
delegate void D(void t);

class C {
  void M<T>(T t) {
  }
  D d = /*<bind>*/M/*</bind>*/;
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);
            Utils.CheckSymbol(semanticInfo.CandidateSymbols.Single(), "void C.M<T>(T t)");
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Null(semanticInfo.Type);
            Utils.CheckSymbol(semanticInfo.ConvertedType, "D");
        }

        [WorkItem(545992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545992")]
        [Fact]
        public void TestSemanticInfoForMembersOfCyclicBase()
        {
            string sourceCode = @"
using System;
using System.Collections;

class B : C
{
}
class C : B
{
    static void Main()
    {
    }
    void Goo(int x)
    {
        /*<bind>*/(this).Goo(1)/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void C.Goo(System.Int32 x)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(610975, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/610975")]
        [Fact]
        public void AttributeOnTypeParameterWithSameName()
        {
            string source = @"
class C<[T(a: 1)]T>
{
}
";

            var comp = CreateCompilation(source);
            comp.GetParseDiagnostics().Verify(); // Syntactically correct.

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var argumentSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Single();
            var argumentNameSyntax = argumentSyntax.NameColon.Name;
            var info = model.GetSymbolInfo(argumentNameSyntax);
        }

        private void CommonTestParenthesizedMethodGroup(string sourceCode)
        {
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal("void C.Goo()", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.OrderBy(s => s.ToTestDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("void C.Goo()", sortedMethodGroup[0].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        [WorkItem(576966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576966")]
        public void TestParenthesizedMethodGroup()
        {
            string sourceCode = @"
class C
{
    void Goo()
    {
        /*<bind>*/Goo/*</bind>*/();
    }
}";

            CommonTestParenthesizedMethodGroup(sourceCode);

            sourceCode = @"
class C
{
    void Goo()
    {
        ((/*<bind>*/Goo/*</bind>*/))();
    }
}";

            CommonTestParenthesizedMethodGroup(sourceCode);
        }

        [WorkItem(531549, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531549")]
        [Fact()]
        public void Bug531549()
        {
            var sourceCode1 = @"
class C1
{
    void Goo()
    {
        int x = 2;
        long? z = /*<bind>*/x/*</bind>*/;
    }
}";

            var sourceCode2 = @"
class C2
{
    void Goo()
    {
        long? y = /*<bind>*/x/*</bind>*/;
        int x = 2;
    }
}";

            var compilation = CreateCompilation(new[] { sourceCode1, sourceCode2 });

            for (int i = 0; i < 2; i++)
            {
                var tree = compilation.SyntaxTrees[i];
                var model = compilation.GetSemanticModel(tree);
                IdentifierNameSyntax syntaxToBind = GetSyntaxNodeOfTypeForBinding<IdentifierNameSyntax>(GetSyntaxNodeList(tree));

                var info1 = model.GetTypeInfo(syntaxToBind);

                Assert.NotEqual(default, info1);
                Assert.Equal("System.Int32", info1.Type.ToTestDisplayString());
            }
        }

        [Fact, WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")]
        public void ObjectCreation1()
        {
            var compilation = CreateCompilation(
@"
using System.Collections;

namespace Test
{
    class C : IEnumerable
    {
        public int P1 { get; set; }

        public void Add(int x)
        { }

        public static void Main()
        {
            var x1 = new C();
            var x2 = new C() {P1 = 1};
            var x3 = new C() {1, 2}; 
        }

        public static void Main2()
        {
            var x1 = new Test.C(); 
            var x2 = new Test.C() {P1 = 1}; 
            var x3 = new Test.C() {1, 2}; 
        }

        public IEnumerator GetEnumerator()
        {
            return null;
        }
    }
}");

            compilation.VerifyDiagnostics();

            SyntaxTree tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         select (node as ObjectCreationExpressionSyntax)).
                         Where(node => (object)node != null).ToArray();

            for (int i = 0; i < 6; i++)
            {
                ObjectCreationExpressionSyntax creation = nodes[i];

                SymbolInfo symbolInfo = model.GetSymbolInfo(creation.Type);
                Assert.Equal("Test.C", symbolInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

                var memberGroup = model.GetMemberGroup(creation.Type);
                Assert.Equal(0, memberGroup.Length);

                TypeInfo typeInfo = model.GetTypeInfo(creation.Type);
                Assert.Null(typeInfo.Type);
                Assert.Null(typeInfo.ConvertedType);

                var conv = model.GetConversion(creation.Type);
                Assert.True(conv.IsIdentity);

                symbolInfo = model.GetSymbolInfo(creation);
                Assert.Equal("Test.C..ctor()", symbolInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

                memberGroup = model.GetMemberGroup(creation);
                Assert.Equal(1, memberGroup.Length);
                Assert.Equal("Test.C..ctor()", memberGroup[0].ToTestDisplayString());

                typeInfo = model.GetTypeInfo(creation);
                Assert.Equal("Test.C", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("Test.C", typeInfo.ConvertedType.ToTestDisplayString());

                conv = model.GetConversion(creation);
                Assert.True(conv.IsIdentity);
            }
        }

        [Fact, WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")]
        public void ObjectCreation2()
        {
            var compilation = CreateCompilation(
@"
using System.Collections;

namespace Test
{
    public class CoClassI : I
    {
        public int P1 { get; set; }

        public void Add(int x)
        { }

        public IEnumerator GetEnumerator()
        {
            return null;
        }
    }

    [System.Runtime.InteropServices.ComImport, System.Runtime.InteropServices.CoClass(typeof(CoClassI))]
    [System.Runtime.InteropServices.Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
    public interface I : IEnumerable
    {
        int P1 { get; set; }

        void Add(int x);
    }

    class C
    {
        public static void Main()
        {
            var x1 = new I();
            var x2 = new I() {P1 = 1};
            var x3 = new I() {1, 2};
        }

        public static void Main2()
        {
            var x1 = new Test.I();
            var x2 = new Test.I() {P1 = 1};
            var x3 = new Test.I() {1, 2};
        }
    }
}
");

            compilation.VerifyDiagnostics();

            SyntaxTree tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         select (node as ObjectCreationExpressionSyntax)).
                         Where(node => (object)node != null).ToArray();

            for (int i = 0; i < 6; i++)
            {
                ObjectCreationExpressionSyntax creation = nodes[i];

                SymbolInfo symbolInfo = model.GetSymbolInfo(creation.Type);
                Assert.Equal("Test.I", symbolInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

                var memberGroup = model.GetMemberGroup(creation.Type);
                Assert.Equal(0, memberGroup.Length);

                TypeInfo typeInfo = model.GetTypeInfo(creation.Type);
                Assert.Null(typeInfo.Type);
                Assert.Null(typeInfo.ConvertedType);

                var conv = model.GetConversion(creation.Type);
                Assert.True(conv.IsIdentity);

                symbolInfo = model.GetSymbolInfo(creation);
                Assert.Equal("Test.CoClassI..ctor()", symbolInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

                memberGroup = model.GetMemberGroup(creation);
                Assert.Equal(1, memberGroup.Length);
                Assert.Equal("Test.CoClassI..ctor()", memberGroup[0].ToTestDisplayString());

                typeInfo = model.GetTypeInfo(creation);
                Assert.Equal("Test.I", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("Test.I", typeInfo.ConvertedType.ToTestDisplayString());

                conv = model.GetConversion(creation);
                Assert.True(conv.IsIdentity);
            }
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")]
        public void ObjectCreation3()
        {
            var pia = CreateCompilation(
@"
using System;
using System.Collections;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

namespace Test
{
    [Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b5827A"")]
    public class CoClassI : I
    {
        public int P1 { get; set; }

        public void Add(int x)
        { }

        public IEnumerator GetEnumerator()
        {
            return null;
        }
    }

    [ComImport()]
    [Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
    [System.Runtime.InteropServices.CoClass(typeof(CoClassI))]
    public interface I : IEnumerable
    {
        int P1 { get; set; }

        void Add(int x);
    }
}
", options: TestOptions.ReleaseDll);

            pia.VerifyDiagnostics();

            var compilation = CreateCompilation(
@"
namespace Test
{
    class C
    {
        public static void Main()
        {
            var x1 = new I();
            var x2 = new I() {P1 = 1};
            var x3 = new I() {1, 2};
        }

        public static void Main2()
        {
            var x1 = new Test.I();
            var x2 = new Test.I() {P1 = 1};
            var x3 = new Test.I() {1, 2};
        }
    }
}", references: new[] { new CSharpCompilationReference(pia, embedInteropTypes: true) });

            compilation.VerifyDiagnostics();

            SyntaxTree tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var nodes = (from node in tree.GetRoot().DescendantNodes()
                         select (node as ObjectCreationExpressionSyntax)).
                         Where(node => (object)node != null).ToArray();

            for (int i = 0; i < 6; i++)
            {
                ObjectCreationExpressionSyntax creation = nodes[i];

                SymbolInfo symbolInfo = model.GetSymbolInfo(creation.Type);
                Assert.Equal("Test.I", symbolInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);

                var memberGroup = model.GetMemberGroup(creation.Type);
                Assert.Equal(0, memberGroup.Length);

                TypeInfo typeInfo = model.GetTypeInfo(creation.Type);
                Assert.Null(typeInfo.Type);
                Assert.Null(typeInfo.ConvertedType);

                var conv = model.GetConversion(creation.Type);
                Assert.True(conv.IsIdentity);

                symbolInfo = model.GetSymbolInfo(creation);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

                memberGroup = model.GetMemberGroup(creation);
                Assert.Equal(0, memberGroup.Length);

                typeInfo = model.GetTypeInfo(creation);
                Assert.Equal("Test.I", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("Test.I", typeInfo.ConvertedType.ToTestDisplayString());

                conv = model.GetConversion(creation);
                Assert.True(conv.IsIdentity);
            }
        }

        /// <summary>
        /// SymbolInfo and TypeInfo should implement IEquatable&lt;T&gt;.
        /// </summary>
        [WorkItem(792647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792647")]
        [Fact]
        public void ImplementsIEquatable()
        {
            string sourceCode =
@"class C
{
    object F()
    {
        return this;
    }
}";
            var compilation = CreateCompilation(sourceCode);
            var tree = compilation.SyntaxTrees.First();
            var expr = (ExpressionSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.ThisKeyword).Parent;
            var model = compilation.GetSemanticModel(tree);

            var symbolInfo1 = model.GetSymbolInfo(expr);
            var symbolInfo2 = model.GetSymbolInfo(expr);
            var symbolComparer = (IEquatable<SymbolInfo>)symbolInfo1;
            Assert.True(symbolComparer.Equals(symbolInfo2));

            var typeInfo1 = model.GetTypeInfo(expr);
            var typeInfo2 = model.GetTypeInfo(expr);
            var typeComparer = (IEquatable<TypeInfo>)typeInfo1;
            Assert.True(typeComparer.Equals(typeInfo2));
        }

        [Fact]
        public void ConditionalAccessErr001()
        {
            string sourceCode = @"
public class C
{
    static void Main()
    {
        var dummy1 = ((string)null) ?.ToString().Length ?.ToString();
        var dummy2 = """"qqq"""" ?/*<bind>*/.ToString().Length/*</bind>*/.ToString();
        var dummy3 = 1.ToString() ?.ToString().Length.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberAccessExpressionSyntax>(sourceCode);

            Assert.Equal("int", semanticInfo.Type.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("int", semanticInfo.ConvertedType.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("string.Length", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalAccessErr002()
        {
            string sourceCode = @"
public class C
{
    static void Main()
    {
        var dummy1 = ((string)null) ?.ToString().Length ?.ToString();
        var dummy2 = ""qqq"" ?/*<bind>*/.ToString/*</bind>*/.Length.ToString();
        var dummy3 = 1.ToString() ?.ToString().Length.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<MemberBindingExpressionSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Null(semanticInfo.ConvertedType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            var sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(s => s.ToDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("string.ToString()", sortedCandidates[0].ToDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("string.ToString(System.IFormatProvider)", sortedCandidates[1].ToDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            var sortedMethodGroup = semanticInfo.MethodGroup.AsEnumerable().OrderBy(s => s.ToDisplayString(), StringComparer.Ordinal).ToArray();
            Assert.Equal("string.ToString()", sortedMethodGroup[0].ToDisplayString());
            Assert.Equal("string.ToString(System.IFormatProvider)", sortedMethodGroup[1].ToDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalAccess001()
        {
            string sourceCode = @"
public class C
{
    static void Main()
    {
        var dummy1 = ((string)null) ?.ToString().Length ?.ToString();
        var dummy2 = ""qqq"" ?/*<bind>*/.ToString()/*</bind>*/.Length.ToString();
        var dummy3 = 1.ToString() ?.ToString().Length.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<InvocationExpressionSyntax>(sourceCode);

            Assert.Equal("string", semanticInfo.Type.ToDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("string", semanticInfo.ConvertedType.ToDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("string.ToString()", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalAccess002()
        {
            string sourceCode = @"
public class C
{
    static void Main()
    {
        var dummy1 = ((string)null) ?.ToString().Length ?.ToString();
        var dummy2 = ""qqq"" ?.ToString()./*<bind>*/Length/*</bind>*/.ToString();
        var dummy3 = 1.ToString() ?.ToString().Length.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("int", semanticInfo.Type.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("int", semanticInfo.ConvertedType.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("string.Length", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalAccess003()
        {
            string sourceCode = @"
public class C
{
    static void Main()
    {
        var dummy1 = ((string)null)?.ToString()./*<bind>*/Length/*</bind>*/?.ToString();
        var dummy2 = ""qqq""?.ToString().Length.ToString();
        var dummy3 = 1.ToString()?.ToString().Length.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("int", semanticInfo.Type.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("?", semanticInfo.ConvertedType.ToDisplayString());
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("string.Length", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalAccess004()
        {
            string sourceCode = @"
public class C
{
    static void Main()
    {
        var dummy1 = ((string)null) ?.ToString()./*<bind>*/Length/*</bind>*/ .ToString();
        var dummy2 = ""qqq"" ?.ToString().Length.ToString();
        var dummy3 = 1.ToString() ?.ToString().Length.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("int", semanticInfo.Type.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("int", semanticInfo.ConvertedType.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("string.Length", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalAccess005()
        {
            string sourceCode = @"
public class C
{
    static void Main()
    {
        var dummy1 = ((string)null) ?.ToString() ?/*<bind>*/[1]/*</bind>*/ .ToString();
        var dummy2 = ""qqq"" ?.ToString().Length.ToString();
        var dummy3 = 1.ToString() ?.ToString().Length.ToString();
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ElementBindingExpressionSyntax>(sourceCode);

            Assert.Equal("char", semanticInfo.Type.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind);
            Assert.Equal("char", semanticInfo.ConvertedType.ToDisplayString());
            Assert.Equal(TypeKind.Struct, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("string.this[int]", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact, WorkItem(998050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/998050")]
        public void Bug998050()
        {
            var comp = CreateCompilation(@"
class BaselineLog
{}

public static BaselineLog Log
{
get
{
}
}= new /*<bind>*/BaselineLog/*</bind>*/();
", parseOptions: TestOptions.Regular);
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(comp);

            Assert.Null(semanticInfo.Type);

            Assert.Equal("BaselineLog", semanticInfo.Symbol.ToDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact, WorkItem(982479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/982479")]
        public void Bug982479()
        {
            const string sourceCode = @"
class C
{
    static void Main()
    {
        new C { Dynamic = { /*<bind>*/Name/*</bind>*/ = 1 } };
    }
 
    public dynamic Dynamic;
}
 
class Name
{
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Equal("dynamic", semanticInfo.Type.ToDisplayString());
            Assert.Equal(TypeKind.Dynamic, semanticInfo.Type.TypeKind);
            Assert.Equal("dynamic", semanticInfo.ConvertedType.ToDisplayString());
            Assert.Equal(TypeKind.Dynamic, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact, WorkItem(1084693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084693")]
        public void Bug1084693()
        {
            const string sourceCode =
@"
using System;
public class C {
    public Func<Func<C, C>, C> Select;
    public Func<Func<C, bool>, C> Where => null;

    public void M() {
        var e =
            from i in this
            where true
            select true?i:i;
    }
}";
            var compilation = CreateCompilation(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            string[] expectedNames = { null, "Where", "Select" };
            int i = 0;
            foreach (var qc in tree.GetRoot().DescendantNodes().OfType<QueryClauseSyntax>())
            {
                var infoSymbol = semanticModel.GetQueryClauseInfo(qc).OperationInfo.Symbol;
                Assert.Equal(expectedNames[i++], infoSymbol?.Name);
            }
            var qe = tree.GetRoot().DescendantNodes().OfType<QueryExpressionSyntax>().Single();
            var infoSymbol2 = semanticModel.GetSymbolInfo(qe.Body.SelectOrGroup).Symbol;
            Assert.Equal(expectedNames[i++], infoSymbol2.Name);
        }

        [Fact]
        public void TestIncompleteMember()
        {
            // Note: binding information in an incomplete member is not available.
            // When https://github.com/dotnet/roslyn/issues/7536 is fixed this test
            // will have to be updated.
            string sourceCode = @"
using System;

class Program
{
    public /*<bind>*/K/*</bind>*/
}

class K
{ }
";
            var semanticInfo = GetSemanticInfoForTest(sourceCode);

            Assert.Equal("K", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind);
            Assert.Equal("K", semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("K", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [WorkItem(18763, "https://github.com/dotnet/roslyn/issues/18763")]
        [Fact]
        public void AttributeArgumentLambdaThis()
        {
            string source =
@"class C
{
    [X(() => this._Y)]
    public void Z()
    {
    }
}";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.ThisExpression);
            var info = model.GetSemanticInfoSummary(syntax);
            Assert.Equal("C", info.Type.Name);
        }
    }
}
