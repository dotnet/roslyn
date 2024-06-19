// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class UserDefinedConversionTests : CompilingTestBase
    {
        #region "Source"
        private readonly string _userDefinedConversionTestTemplate = @"
class C1 { }
class C2 { }
class D 
{
  public static XXX operator C1(D d) { return null; }
  public static XXX operator C2(D d) { return null; }
  public static XXX operator D(C1 c1) { return null; }
  public static XXX operator D(C2 c2) { return null; }
}

struct E1 { }
struct E2 { }
struct F
{
  public static XXX operator E1(F f) { return default(E1); }
  public static XXX operator E2(F f) { return default(E2); }
  public static XXX operator F(E1 e1) { return default(F); }
  public static XXX operator F(E2 e2) { return default(F); }
}
struct G {}
struct H
{
  public static XXX operator G(H h) { return default(G); }
}
struct I
{
  public static XXX operator G(I? i) { return default(G); }
}
struct J
{
  public static XXX operator G?(J j) { return default(G?); }
}
struct K
{
  public static XXX operator G?(K? k) { return default(G?); }
}
struct L
{
  public static XXX operator G(L l) { return default(G); }
  public static XXX operator G(L? l) { return default(G); }
}

struct M
{
  public static XXX operator G(M m) { return default(G); }
  public static XXX operator G?(M? m) { return default(G?); }
}

struct N
{
  public static XXX operator G(N? n) { return default(G); }
  public static XXX operator G?(N n) { return default(G?); }
}

struct O
{
  public static XXX operator G(O? o) { return default(G); }
  public static XXX operator G?(O? o) { return default(G?); }
}

struct P
{
  public static XXX operator G(P p) { return default(G); }
  public static XXX operator G(P? p) { return default(G); }
  public static XXX operator G?(P p) { return default(G?); }
}

struct Q
{
  public static XXX operator G(Q q) { return default(G); }
  public static XXX operator G(Q? q) { return default(G); }
  public static XXX operator G?(Q? q) { return default(G?); }
}

struct R
{
  public static XXX operator G(R r) { return default(G); }
  public static XXX operator G?(R r) { return default(G?); }
  public static XXX operator G?(R? r) { return default(G?); }
}

struct S
{
  public static XXX operator G(S? s) { return default(G); }
  public static XXX operator G?(S s) { return default(G?); }
  public static XXX operator G?(S? s) { return default(G?); }
}

struct T
{
  public static XXX operator G(T t) { return default(G); }
  public static XXX operator G(T? t) { return default(G); }
  public static XXX operator G?(T t) { return default(G?); }
  public static XXX operator G?(T? t) { return default(G?); }
}
";
        #endregion

        [Fact]
        public void TestUserDefinedImplicitConversionOverloadResolution()
        {
            string source1 = _userDefinedConversionTestTemplate.Replace("XXX", "implicit");
            string source2 = @"
class Z
{
  static void MC1(C1 c1) { }
  static void MC2(C2 c2) { }
  static void MD(D d) { }
  static void ME1(E1 e1) { }
  static void MNE1(E1? e1) { }
  static void ME2(E2 e2) { }
  static void MNE2(E2? e2) { }
  static void MF(F f) { }
  static void MNF(F? f) { }
  static void MG(G g) { }
  static void MNG(G? g) { }

  static void Main()
  {
    MC1(default(D));
    MC2(default(D));
    MD(default(C1));
    MD(default(C2));

    ME1(default(F));
    MNE1(default(F));
    MNE1(default(F?));
    ME2(default(F));
    MNE2(default(F));
    MNE2(default(F?));
    MF(default(E1));
    MF(default(E2));
    MNF(default(E1));
    MNF(default(E1?));
    MNF(default(E2));
    MNF(default(E2?));

    MG(default(H));
    // MG(default(H?)); Not implicit
    MNG(default(H));
    MNG(default(H?));

    MG(default(I));
    MG(default(I?));
    MNG(default(I));
    MNG(default(I?));

    //MG(default(J));  Not implicit
    //MG(default(J?)); Not implicit
    MNG(default(J));
    MNG(default(J?)); // Invalid according to specification. Native compiler and Roslyn allow it
                      // by improperly 'half lifting' the conversion.

    //MG(default(K));  Not implicit
    //MG(default(K?)); Not implicit
    MNG(default(K));
    MNG(default(K?));

    MG(default(L));
    MG(default(L?));
    MNG(default(L)); // Ambiguous according to specification; Roslyn and native compiler allow it
    MNG(default(L?));

    MG(default(M));
    //MG(default(M?)); Not implicit
    MNG(default(M));  // Ambiguous according to specification; Roslyn and native compiler allow it.
    MNG(default(M?));

    MG(default(N));
    MG(default(N?));
    MNG(default(N));
    // MNG(default(N?)); // Valid according to specification. Native compiler and Roslyn claim this is ambiguous
                         // even though the conversion from N-->G? is not applicable because it cannot be lifted.
                         // Native compiler and Roslyn choose improperly 'half lift' the operator to N?-->G?.

    MG(default(O));
    MG(default(O?));
    MNG(default(O));
    MNG(default(O?));

    MG(default(P));
    MG(default(P?));
    MNG(default(P));
    //MNG(default(P?)); // Similarly valid according to specification, but ambiguous according to native compiler and Roslyn.

    MG(default(Q));
    MG(default(Q?));
    MNG(default(Q)); // Ambiguous according to specification; Roslyn and native compiler allow it.
    MNG(default(Q?));

    MG(default(R));
    //MG(default(R?)); Not implicit
    MNG(default(R));
    MNG(default(R?));

    MG(default(S));
    MG(default(S?));
    MNG(default(S));
    MNG(default(S?));

    MG(default(T));
    MG(default(T?));
    MNG(default(T));
    MNG(default(T?));
  }
}
";
            string source3 = @"
class Z
{
  static void MC1(C1 c1) { }
  static void MC2(C2 c2) { }
  static void MD(D d) { }
  static void ME1(E1 e1) { }
  static void MNE1(E1? e1) { }
  static void ME2(E2 e2) { }
  static void MNE2(E2? e2) { }
  static void MF(F f) { }
  static void MNF(F? f) { }
  static void MG(G g) { }
  static void MNG(G? g) { }

  static void Main()
  {
    // None of these are implicit conversions.
    MG(default(H?));
    MG(default(J));
    MG(default(J?));
    MG(default(K));  
    MG(default(K?)); 
    MG(default(M?)); 
    MG(default(R?)); 
  }
}
";

            var comp = CreateCompilation(source1 + source2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source1 + source3);
            comp.VerifyDiagnostics(
// (115,8): error CS1503: Argument 1: cannot convert from 'H?' to 'G'
//     MG(default(H?));
Diagnostic(ErrorCode.ERR_BadArgType, "default(H?)").WithArguments("1", "H?", "G"),

// (116,8): error CS1503: Argument 1: cannot convert from 'J' to 'G'
//     MG(default(J));
Diagnostic(ErrorCode.ERR_BadArgType, "default(J)").WithArguments("1", "J", "G"),

// (117,8): error CS1503: Argument 1: cannot convert from 'J?' to 'G'
//     MG(default(J?));
Diagnostic(ErrorCode.ERR_BadArgType, "default(J?)").WithArguments("1", "J?", "G"),

// (119,8): error CS1503: Argument 1: cannot convert from 'K' to 'G'
//     MG(default(K));  
Diagnostic(ErrorCode.ERR_BadArgType, "default(K)").WithArguments("1", "K", "G"),

// (120,8): error CS1503: Argument 1: cannot convert from 'K?' to 'G'
//     MG(default(K?)); 
Diagnostic(ErrorCode.ERR_BadArgType, "default(K?)").WithArguments("1", "K?", "G"),

// (121,8): error CS1503: Argument 1: cannot convert from 'M?' to 'G'
//     MG(default(M?)); 
Diagnostic(ErrorCode.ERR_BadArgType, "default(M?)").WithArguments("1", "M?", "G"),

// (122,8): error CS1503: Argument 1: cannot convert from 'R?' to 'G'
//     MG(default(R?)); 
Diagnostic(ErrorCode.ERR_BadArgType, "default(R?)").WithArguments("1", "R?", "G"));
        }

        [Fact, WorkItem(543716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543716")]
        public void TestUserDefinedConversionOverloadResolution_SpecViolations()
        {
            // These are all cases where the specification says the conversion should either not exist
            // or be ambiguous, but the native compiler allows the conversion. Roslyn emulates the
            // native compiler's behavior to avoid the breaking change.

            string implicitConversions = _userDefinedConversionTestTemplate.Replace("XXX", "implicit");
            string implicitConversionBadSuccess = @"
class Z
{
    static void MNG(G? g) { }
    static void MG(G g) { }
    static void Main()
    {
        // Implicit conversions
        MNG(default(J?)); 
        MNG(default(L));
        MNG(default(M));
        MNG(default(Q));

        // Explicit conversions
        MNG((G?)default(L));
        MG((G)default(M?));
        MNG((G?)default(M));
        MNG((G?)default(Q));
        MG((G)default(R?));
    }
}";
            var comp = CreateCompilation(implicitConversions + implicitConversionBadSuccess);
            comp.VerifyDiagnostics();

            // More cases where the specification says that the conversion should be bad, but
            // the native compiler and Roslyn allow it.

            string explicitConversions = _userDefinedConversionTestTemplate.Replace("XXX", "explicit");
            string explicitConversionsBadSuccess = @"
class Z
{
  static void MG(G g) { }
  static void MNG(G? g) { }
  static void Main()
  {
    MNG((G?)default(L));
    MG((G)default(M?));
    MNG((G?)default(M));
    MNG((G?)default(Q));
    MG((G)default(R?));
  }
}";
            comp = CreateCompilation(explicitConversions + explicitConversionsBadSuccess);
            comp.VerifyDiagnostics();

            // These are cases where the specification indicates that a conversion should be legal,
            // but the native compiler disallows it. Roslyn follows the native compiler in these cases.

            string implicitConversionsBadFailures = @"
class Z
{
  static void MNG(G? g) { }
  static void Main()
  {
    MNG(default(N?)); 
    MNG(default(P?)); 
  }  
};";

            comp = CreateCompilation(implicitConversions + implicitConversionsBadFailures);
            comp.VerifyDiagnostics(
// (103,9): error CS0457: Ambiguous user defined conversions 'N.implicit operator G(N?)' and 'N.implicit operator G?(N)' when converting from 'N?' to 'G?'
//     MNG(default(N?)); 
Diagnostic(ErrorCode.ERR_AmbigUDConv, "default(N?)").WithArguments("N.implicit operator G(N?)", "N.implicit operator G?(N)", "N?", "G?"),
// (104,9): error CS0457: Ambiguous user defined conversions 'P.implicit operator G(P)' and 'P.implicit operator G(P?)' when converting from 'P?' to 'G?'
//     MNG(default(P?)); 
Diagnostic(ErrorCode.ERR_AmbigUDConv, "default(P?)").WithArguments("P.implicit operator G(P)", "P.implicit operator G(P?)", "P?", "G?")
                );

            // More cases where the specification indicates that a conversion should be legal,
            // but the native compiler disallows it. Roslyn follows the native compiler in these cases.

            string explicitConversionsBadFailures = @"
class Z
{
  static void MNG(G? g) { }
  static void Main()
  {
    MNG((G?)default(N?)); 
    MNG((G?)default(P?)); 
  }  
};";

            comp = CreateCompilation(explicitConversions + explicitConversionsBadFailures);
            comp.VerifyDiagnostics(
// (103,9): error CS0457: Ambiguous user defined conversions 'N.explicit operator G(N?)' and 'N.explicit operator G?(N)' when converting from 'N?' to 'G?'
//     MNG((G?)default(N?)); 
Diagnostic(ErrorCode.ERR_AmbigUDConv, "(G?)default(N?)").WithArguments("N.explicit operator G(N?)", "N.explicit operator G?(N)", "N?", "G?"),
// (104,9): error CS0457: Ambiguous user defined conversions 'P.explicit operator G(P)' and 'P.explicit operator G(P?)' when converting from 'P?' to 'G?'
//     MNG((G?)default(P?)); 
Diagnostic(ErrorCode.ERR_AmbigUDConv, "(G?)default(P?)").WithArguments("P.explicit operator G(P)", "P.explicit operator G(P?)", "P?", "G?")
                );
        }

        [Fact]
        public void TestUserDefinedConversionOverloadResolution_BreakingChanges()
        {
            // Roslyn emulates most of the native compiler's spec violations for user-defined conversions.

            // It is possible for a user-defined implicit conversion to be ambiguous when processed
            // as an explicit conversion but unambiguous when processed as an implicit conversion.
            // In that circumstance, the native compiler reports the ambiguity, oddly enough. 
            // One might expect that if "B b = 1;" succeeds then "B b = (B)1;" ought to as well.
            // Roslyn matches the native compiler.

            // This is bug 11202.

            string source1 = @" 
class A
{
    public static implicit operator A(int d) { return null; }
}
class B : A
{
    public static implicit operator B(long d) { return null; }
}
class C
{
    static void Main()
    {
        B b = 1; 
        // Only the operator in B is applicable because we must
        // convert implicitly from the return type to B.

        b = (B)1; 
        // Both the operators are applicable because we must
        // convert from the return type to B via an explicit conversion,
        // and A to B is therefore legal. Therefore, were we to treat
        // this solely as an explicit conversion, we'd have an ambiguity
        // because the return type exactly matches on the operator in B,
        // and the parameter type exactly matches on the operator in A.
        // The native compiler produces an error here. Roslyn reasons
        // that if the implicit conversion succeeds then the explicit conversion
        // ought to also succeed.
    }
}";

            var comp = CreateCompilation(source1);
            comp.VerifyDiagnostics(
                // (18,13): error CS0457: Ambiguous user defined conversions 'B.implicit operator B(long)' and 'A.implicit operator A(int)' when converting from 'int' to 'B'
                //         b = (B)1; 
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(B)1").WithArguments("B.implicit operator B(long)", "A.implicit operator A(int)", "int", "B"));

            // The native compiler incorrectly treats the explicit user-defined
            // conversion operator as inapplicable here. Roslyn allows this.
            // Contrast this with the genuinely incorrect code in the method
            // TestUserDefinedConversionsTypeParameterEdgeCase below.

            string source2 = @"
class Animal {}
class Mammal : Animal {}
class X<T> where T : Mammal
{
  public static explicit operator X<T>(Animal a) { return null; }
  public static void M(T t)
  { 
    X<T> xt = (X<T>)t;
  }
}
";

            comp = CreateCompilation(source2);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestUserDefinedConversionsTypeParameterEdgeCase()
        {
            // In contrast with the code above, this code should genuinely produce
            // an error. The cast operator means that a standard explicit conversion can
            // be inserted on both sides of the implicit conversion. Though there is an
            // explicit conversion from T to Giraffe, this is not a *standard* implicit
            // conversion because there is no implicit conversion from Giraffe to T.

            string source = @"
class Animal {}
class Mammal : Animal {}
class Giraffe : Mammal {}
class X<T> where T : Mammal
{
  public static implicit operator X<T>(Giraffe g) { return null; }
  public static void M(T t)
  { 
    X<T> xt = (X<T>)t;
  }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,15): error CS0030: Cannot convert type 'T' to 'X<T>'
                //     X<T> xt = (X<T>)t;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(X<T>)t").WithArguments("T", "X<T>"));
        }

        [Fact, WorkItem(605100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605100")]
        public void TestUserDefinedConversions_DynamicIdentityBetweenBaseTypes()
        {
            string source = @"
public class A<U>
{
   public static explicit operator T(A<U> a) { return null; }
}

public class S : A<dynamic>
{
    
}

public class T : A<object>
{

}

public class X
{
    static T F(S s)
    {
        return (T)s;
    }
}
";
            // Dev11 doesn't use identity conversion and reports an error, which is wrong:
            // error CS0457: Ambiguous user defined conversions 'A<dynamic>.explicit operator T(A<dynamic>)' and 'A<object>.explicit operator T(A<object>)' when converting from 'S' to 'T'
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(605326, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/605326")]
        public void TestUserDefinedConversions_DynamicIdentityBetweenBaseTypeAndTargetType()
        {
            string source = @"
public class A<T>
{
}
 
public class B : A<dynamic>
{
    public static explicit operator A<object>(B x)
    {
        return null;
    }
}
";
            // TODO (tomat): This should report ERR_ConversionWithBase 
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestUserDefinedExplicitConversionOverloadResolution()
        {
            // Explicit conversions should use implicit conversions.
            string source1 = _userDefinedConversionTestTemplate.Replace("XXX", "implicit");

            string source2 = _userDefinedConversionTestTemplate.Replace("XXX", "explicit");

            string source3 = @"
class Z
{
  static void MC1(C1 c1) { }
  static void MC2(C2 c2) { }
  static void MD(D d) { }
  static void ME1(E1 e1) { }
  static void MNE1(E1? e1) { }
  static void ME2(E2 e2) { }
  static void MNE2(E2? e2) { }
  static void MF(F f) { }
  static void MNF(F? f) { }
  static void MG(G g) { }
  static void MNG(G? g) { }

  static void Main()
  {
    MC1((C1)default(D));
    MC2((C2)default(D));
    MD((D)default(C1));
    MD((D)default(C2));

    ME1((E1)default(F));
    MNE1((E1?)default(F));
    MNE1((E1?)default(F?));
    ME2((E2)default(F));
    MNE2((E2?)default(F));
    MNE2((E2?)default(F?));
    MF((F)default(E1));
    MF((F)default(E2));
    MNF((F?)default(E1));
    MNF((F?)default(E1?));
    MNF((F?)default(E2));
    MNF((F?)default(E2?));

    MG((G)default(H));
    MG((G)default(H?));
    MNG((G?)default(H));
    MNG((G?)default(H?));

    MG((G)default(I));
    MG((G)default(I?));
    MNG((G?)default(I));
    MNG((G?)default(I?));

    MG((G)default(J));
    MG((G)default(J?));
    MNG((G?)default(J));
    MNG((G?)default(J?));

    MG((G)default(K)); 
    MG((G)default(K?));
    MNG((G?)default(K));
    MNG((G?)default(K?));

    MG((G)default(L));
    MG((G)default(L?));
    MNG((G?)default(L)); // Spec says this should be ambiguous; native compiler and Roslyn allow it.
    MNG((G?)default(L?));

    MG((G)default(M));
    MG((G)default(M?)); // Spec says this should be ambiguous; native compiler and Roslyn allow it.
    MNG((G?)default(M)); // Spec says this should be ambiguous; native compiler and Roslyn allow it.
    MNG((G?)default(M?));

    // MG((G)default(N));  This is an interesting one. The conversion is ambiguous when declared as explicit, unambiguous when implicit. See below.
    MG((G)default(N?));
    MNG((G?)default(N));
    // MNG((G?)default(N?)); // Spec says this should be unambiguous; native compiler and Roslyn make it ambiguous.

    MG((G)default(O));
    MG((G)default(O?));
    MNG((G?)default(O));
    MNG((G?)default(O?));

    MG((G)default(P));
    MG((G)default(P?));
    MNG((G?)default(P));
    // MNG((G?)default(P?)); // Spec says this should be unambiguous; native compiler and Roslyn make it ambiguous.

    MG((G)default(Q));
    MG((G)default(Q?));
    MNG((G?)default(Q)); // Spec says this should be ambiguous; native compiler and Roslyn allow it.
    MNG((G?)default(Q?));

    MG((G)default(R));
    MG((G)default(R?));  // Spec says this should be ambiguous; native compiler and Roslyn allow it.
    MNG((G?)default(R));
    MNG((G?)default(R?));

    // MG((G)default(S)); Another one that is ambiguous when declared as explicit, unambiguous when implicit. See below.
    MG((G)default(S?));
    MNG((G?)default(S));
    MNG((G?)default(S?));

    MG((G)default(T));
    MG((G)default(T?));
    MNG((G?)default(T));
    MNG((G?)default(T?));
  }
}
";

            string source4 = @"
class Z
{
  static void MG(G g) { }
  static void MNG(G? g) { }

  static void Main()
  {
    MG((G)default(N));
    MG((G)default(S));
  }
}
";

            var comp = CreateCompilation(source1 + source3);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source2 + source3);
            comp.VerifyDiagnostics();

            // The native compiler produces errors for all of these because
            // it does not consider whether or not an *implicit* conversion
            // would have succeeded; it only considers whether an *explicit*
            // conversion is unambiguous.

            // Roslyn originally had a breaking change here, but now matches dev11.

            comp = CreateCompilation(source1 + source4);
            comp.VerifyDiagnostics(
                // (105,8): error CS0457: Ambiguous user defined conversions 'N.implicit operator G(N?)' and 'N.implicit operator G?(N)' when converting from 'N' to 'G'
                //     MG((G)default(N));
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(G)default(N)").WithArguments("N.implicit operator G(N?)", "N.implicit operator G?(N)", "N", "G"),
                // (106,8): error CS0457: Ambiguous user defined conversions 'S.implicit operator G(S?)' and 'S.implicit operator G?(S)' when converting from 'S' to 'G'
                //     MG((G)default(S));
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(G)default(S)").WithArguments("S.implicit operator G(S?)", "S.implicit operator G?(S)", "S", "G"));

            // When restricted to only use explicit conversions, the conversions
            // truly are ambiguous; the native and Roslyn compilers agree on these cases:

            comp = CreateCompilation(source2 + source4);
            comp.VerifyDiagnostics(
                // (105,8): error CS0457: Ambiguous user defined conversions 'N.explicit operator G(N?)' and 'N.explicit operator G?(N)' when converting from 'N' to 'G'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(G)default(N)").WithArguments("N.explicit operator G(N?)", "N.explicit operator G?(N)", "N", "G"),
                // (106,9): error CS0457: Ambiguous user defined conversions 'N.explicit operator G(N?)' and 'N.explicit operator G?(N)' when converting from 'N?' to 'G?'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(G)default(S)").WithArguments("S.explicit operator G(S?)", "S.explicit operator G?(S)", "S", "G"));
        }

        [Fact]
        public void TestUserDefinedConversions()
        {
            string source = @"
using System;

class C
{
  public static void Main()
  {
    S sx = new S('x');
    D dy = new D('y');
    E ez = new F('z');
    
    T tx = sx;
    F fg = (F)dy;
    B dz = (D)ez;
    
    Console.Write(tx);
    Console.Write(fg);
    Console.Write(dz);
  }
}

struct S
{
  public string str;
  public S(string str) { this.str = str; }
  public S(char chr) { this.str = chr.ToString(); }
  public override string ToString() { return 'S' + this.str; }
  public static implicit operator T(S s) { return new T(s.str); }
}

struct T
{
  public string str;
  public T(string str) { this.str = str; }
  public T(char chr) { this.str = chr.ToString(); }
  public override string ToString() { return 'T' + this.str; }
}

class B
{
  public string str;
  public B(string str) { this.str = str; }
  public B(char chr) { this.str = chr.ToString(); }
  public override string ToString() { return 'B' + this.str; }
  public static explicit operator E(B b) { return new F(b.str); }
}

class D : B 
{
  public D(char chr) : base(chr) {}
  public D(string str) : base(str) {}
  public static explicit operator D(F f) { return new D(f.str); }
  public override string ToString() { return 'D' + this.str; }
}

class E
{
  public string str;
  public E(string str) { this.str = str; }
  public E(char chr) { this.str = chr.ToString(); }
  public override string ToString() { return 'E' + this.str; }
}

class F : E
{
  public F(char chr) : base(chr) {}
  public F(string str) : base(str) {}
  public override string ToString() { return 'F' + this.str; }
}
";

            string output = "TxFyDz";

            CompileAndVerify(source: source, expectedOutput: output);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/39959")]
        [WorkItem(39959, "https://github.com/dotnet/roslyn/issues/39959")]
        public void TestIntPtrUserDefinedConversions()
        {
            // IntPtr and UIntPtr violate the rules of user-defined conversions for 
            // backwards-compatibility reasons. All of these should compile without error.

            string source1 = @"
using System;
unsafe class P
{
    public static void Main()
    {
        byte uint8 = 0;
        sbyte int8 = 0;
        short int16 = 0;
        ushort uint16 = 0;
        int int32 = 0;
        long int64 = 0;
        uint uint32 = 0;
        ulong uint64 = 0;
        double real64 = 0;
        float real32 = 0;
        char chr = '\0';
        DayOfWeek e = default(DayOfWeek);
        IntPtr intPtr = default(IntPtr);
        UIntPtr uintPtr = default(UIntPtr);

        byte? nuint8 = 0;
        sbyte? nint8 = 0;
        short? nint16 = 0;
        ushort? nuint16 = 0;
        int? nint32 = 0;
        long? nint64 = 0;
        uint? nuint32 = 0;
        ulong? nuint64 = 0;
        double? nreal64 = 0;
        float? nreal32 = 0;
        char? nchr = '\0';
        DayOfWeek? ne = default(DayOfWeek);
        IntPtr? nintPtr = default(IntPtr);
        UIntPtr? nuintPtr = default(UIntPtr);

";

            string source2 = @"

        decimal dec = 0;
        decimal? ndec = 0;
        void* pvoid = default(void*);
        int* pint = default(int*);
";
            string source3 = @"

// The spec requires that there be either an implicit conversion
// from the argument type to the parameter type, or from the 
// parameter type to the argument type. It does *not* require
// that there be an *explicit* conversion from the argument type
// to the parameter type.
//
// This means that if the argument type is short and the parameter
// type is uint, the conversion is not a candidate. There is an
// implicit conversion from short to uint, but there is no implicit
// conversion from short to uint or from uint to short.
//
// The spec also requires that the operator chosen be unambiguous.
// A conversion from byte --> IntPtr?, for example, is ambiguous because
// it could be:
// byte --> int --> IntPtr --> IntPtr?, or 
// byte --> int? --> IntPtr? (using the lifted conversion), or
// byte --> long --> IntPtr --> IntPtr? or 
// byte --> long? --> IntPtr? (using the lifted conversion)
//
// And that set is ambiguous; there is no best operator.
//
// The native compiler, and hence Roslyn, implements none of these rules.
// It allows conversions from IntPtr to any numeric type and vice versa.
// All the cases below should compile without error.
//


// numeric to IntPtr

        intPtr = (IntPtr) uint8;  // i32 imp
        intPtr = (IntPtr) int8;   // i32 imp
        intPtr = (IntPtr) uint16; // i32 imp 
        intPtr = (IntPtr) int16;  // i32 imp
        intPtr = (IntPtr) chr;    // i32 exp
        intPtr = (IntPtr) uint32; // i64 imp
        intPtr = (IntPtr) int32;  // i32 id
        intPtr = (IntPtr) uint64; // i64 exp *
        intPtr = (IntPtr) int64;  // i64 id
        intPtr = (IntPtr) real32; // i64 exp
        intPtr = (IntPtr) real64; // i64 exp
        intPtr = (IntPtr) e;

// numeric to UIntPtr

        uintPtr = (UIntPtr) uint8;  // u32 imp
        uintPtr = (UIntPtr) int8;   // u64 exp (ulong is bigger type than uint) *
        uintPtr = (UIntPtr) uint16; // u32 imp
        uintPtr = (UIntPtr) int16;  // u64 exp (ulong bigger) *
        uintPtr = (UIntPtr) chr;    // u32 exp
        uintPtr = (UIntPtr) uint32; // u32 id
        uintPtr = (UIntPtr) int32;  // u64 exp (ulong bigger) *
        uintPtr = (UIntPtr) uint64; // u64 id
        uintPtr = (UIntPtr) int64;  // u64 exp
        uintPtr = (UIntPtr) real32; // u64 exp
        uintPtr = (UIntPtr) real64; // u64 exp
        uintPtr = (UIntPtr) e;

// IntPtr to numeric

        uint8 = (byte) intPtr; // i32
        uint16 = (ushort) intPtr;  // i32
        chr = (char) intPtr; // i32
        uint32 = (uint) intPtr; // i32
        uint64 = (ulong) intPtr; // i64
        int8 = (sbyte) intPtr; // i32
        int16 = (short) intPtr; // i32
        int32 = (int) intPtr; // i32
        int64 = (long) intPtr; // i64
        real32 = (float) intPtr;  // i64
        real64 = (double) intPtr;  // i64
        e = (DayOfWeek) intPtr;

// UIntPtr to numeric

        uint8 = (byte) uintPtr; // u32
        uint16 = (ushort) uintPtr; // u32
        chr = (char) uintPtr; // u32
        uint32 = (uint) uintPtr; // u32
        uint64 = (ulong) uintPtr; // u64
        int8 = (sbyte) uintPtr; // u32
        int16 = (short) uintPtr; // u32
        int32 = (int) uintPtr; // u32
        int64 = (long) uintPtr; // u64
        real32 = (float) uintPtr; // u64
        real64 = (double) uintPtr; // u64
        e = (DayOfWeek) uintPtr;

// numeric to IntPtr?
        
        nintPtr = (IntPtr?) uint8;  // i32 imp 
        nintPtr = (IntPtr?) int8;   // i32 imp
        nintPtr = (IntPtr?) uint16; // i32 imp 
        nintPtr = (IntPtr?) int16;  // i32 imp
        nintPtr = (IntPtr?) chr;    // i32 exp
        nintPtr = (IntPtr?) uint32; // i64 imp
        nintPtr = (IntPtr?) int32;  // i32 id
        nintPtr = (IntPtr?) uint64; // i64 exp
        nintPtr = (IntPtr?) int64;  // i64 id
        nintPtr = (IntPtr?) real32; // i64 exp
        nintPtr = (IntPtr?) real64; // i64 exp
        nintPtr = (IntPtr?) e;

// numeric to UIntPtr?
        
        nuintPtr = (UIntPtr?) uint8;  // u32 imp
        nuintPtr = (UIntPtr?) int8;   // u64 exp (ulong is bigger type than uint)
        nuintPtr = (UIntPtr?) uint16; // u32 imp
        nuintPtr = (UIntPtr?) int16;  // u64 exp (ulong bigger)
        nuintPtr = (UIntPtr?) chr;    // u32 exp
        nuintPtr = (UIntPtr?) uint32; // u32 id
        nuintPtr = (UIntPtr?) int32;  // u64 exp (ulong bigger)
        nuintPtr = (UIntPtr?) uint64; // u64 id
        nuintPtr = (UIntPtr?) int64;  // u64 exp
        nuintPtr = (UIntPtr?) real32; // u64 exp
        nuintPtr = (UIntPtr?) real64; // u64 exp
        nuintPtr = (UIntPtr?) e;

// From nullable numeric to IntPtr

        intPtr = (IntPtr) nuint8;  // i32 imp
        intPtr = (IntPtr) nint8;   // i32 imp
        intPtr = (IntPtr) nuint16; // i32 imp 
        intPtr = (IntPtr) nint16;  // i32 imp
        intPtr = (IntPtr) nchr;    // i32 exp
        intPtr = (IntPtr) nuint32; // i64 imp
        intPtr = (IntPtr) nint32;  // i32 id
        intPtr = (IntPtr) nuint64; // i64 exp
        intPtr = (IntPtr) nint64;  // i64 id
        intPtr = (IntPtr) nreal32; // i64 exp
        intPtr = (IntPtr) nreal64; // i64 exp
        intPtr = (IntPtr) ne;

// From nullable numeric to UIntPtr

        uintPtr = (UIntPtr) nuint8;  // u32 imp
        uintPtr = (UIntPtr) nint8;   // u64 exp (ulong is bigger type than uint)
        uintPtr = (UIntPtr) nuint16; // u32 imp
        uintPtr = (UIntPtr) nint16;  // u64 exp (ulong bigger)
        uintPtr = (UIntPtr) nchr;    // u32 exp
        uintPtr = (UIntPtr) nuint32; // u32 id
        uintPtr = (UIntPtr) nint32;  // u64 exp (ulong bigger)
        uintPtr = (UIntPtr) nuint64; // u64 id
        uintPtr = (UIntPtr) nint64;  // u64 exp
        uintPtr = (UIntPtr) nreal32; // u64 exp
        uintPtr = (UIntPtr) nreal64; // u64 exp
        uintPtr = (UIntPtr) ne;

// From IntPtr? to numeric

        uint8 = (byte) nintPtr; // i32 
        uint16 = (ushort) nintPtr; // i32
        chr = (char) nintPtr; // i32
        uint32 = (uint) nintPtr; // i32
        uint64 = (ulong) nintPtr;  // i64
        int8 = (sbyte) nintPtr; // i32
        int16 = (short) nintPtr; // i32
        int32 = (int) nintPtr; // i32
        int64 = (long) nintPtr; // i64
        real32 = (float) nintPtr; // i64
        real64 = (double) nintPtr; // i64
        e = (DayOfWeek) nintPtr;

// From UIntPtr? to numeric

        uint8 = (byte) nuintPtr; // u32
        uint16 = (ushort) nuintPtr; // u32
        chr = (char) nuintPtr; // u32
        uint32 = (uint) nuintPtr; // u32
        uint64 = (ulong) nuintPtr; // u64
        int8 = (sbyte) nuintPtr; // u32
        int16 = (short) nuintPtr; // u32
        int32 = (int) nuintPtr; // u32
        int64 = (long) nuintPtr; // u64
        real32 = (float) nuintPtr; // u64
        real64 = (double) nuintPtr; // u64
        e = (DayOfWeek) nuintPtr; 

// From IntPtr to nullable numeric

        nuint8 = (byte?) intPtr;
        nuint16 = (ushort?) intPtr;
        nchr = (char?) intPtr;
        nuint32 = (uint?) intPtr;
        nuint64 = (ulong?) intPtr;
        nint8 = (sbyte?) intPtr;
        nint16 = (short?) intPtr;
        nint32 = (int?) intPtr;
        nint64 = (long?) intPtr;
        nreal32 = (float?) intPtr;
        nreal64 = (double?) intPtr;
        ne = (DayOfWeek?) intPtr;
        
// From UIntPtr to nullable numeric

        nuint8 = (byte?) uintPtr;
        nuint16 = (ushort?) uintPtr;
        nchr = (char?) uintPtr;
        nuint32 = (uint?) uintPtr;
        nuint64 = (ulong?) uintPtr;
        nint8 = (sbyte?) uintPtr;
        nint16 = (short?) uintPtr;
        nint32 = (int?) uintPtr;
        nint64 = (long?) uintPtr;
        nreal32 = (float?) uintPtr;
        nreal64 = (double?) uintPtr;
        ne = (DayOfWeek?) uintPtr;

";

            string source4 = @"

// Decimal conversion lowering is not yet implemented:

        uintPtr = (UIntPtr) dec;    // u64 exp
        intPtr = (IntPtr) dec;    // i64 exp
        dec = (decimal) intPtr;  // i64
        dec = (decimal) uintPtr; // u64
        nintPtr = (IntPtr?) dec;    // i64 exp
        nuintPtr = (UIntPtr?) dec;    // u64 exp
        intPtr = (IntPtr) ndec;    // i64 exp
        uintPtr = (UIntPtr) ndec;    // u64 exp
        dec = (decimal) nintPtr; // i64
        dec = (decimal) nuintPtr; // u64
        ndec = (decimal?) intPtr;
        ndec = (decimal?) uintPtr;
        ndec = (decimal?) nintPtr;
        ndec = (decimal?) nuintPtr;

// Lifted numeric To IntPtr
       
        nintPtr = (IntPtr?) nuint8;  // i32 imp
        nintPtr = (IntPtr?) nint8;   // i32 imp
        nintPtr = (IntPtr?) nuint16; // i32 imp 
        nintPtr = (IntPtr?) nint16;  // i32 imp
        nintPtr = (IntPtr?) nchr;    // i32 exp
        nintPtr = (IntPtr?) nuint32; // i64 imp
        nintPtr = (IntPtr?) nint32;  // i32 id
        nintPtr = (IntPtr?) nuint64; // i64 exp
        nintPtr = (IntPtr?) nint64;  // i64 id
        nintPtr = (IntPtr?) ndec;    // i64 exp
        nintPtr = (IntPtr?) nreal32; // i64 exp
        nintPtr = (IntPtr?) nreal64; // i64 exp
        nintPtr = (IntPtr?) ne;

// Lifted numeric to UIntPtr

        nuintPtr = (UIntPtr?) nuint8;  // u32 imp
        nuintPtr = (UIntPtr?) nint8;   // u64 exp (ulong is bigger type than uint)
        nuintPtr = (UIntPtr?) nuint16; // u32 imp
        nuintPtr = (UIntPtr?) nint16;  // u64 exp (ulong bigger)
        nuintPtr = (UIntPtr?) nchr;    // u32 exp
        nuintPtr = (UIntPtr?) nuint32; // u32 imp
        nuintPtr = (UIntPtr?) nint32;  // u64 exp (ulong bigger)
        nuintPtr = (UIntPtr?) nuint64; // u64 id
        nuintPtr = (UIntPtr?) nint64;  // u64 exp
        nuintPtr = (UIntPtr?) ndec;    // u64 exp
        nuintPtr = (UIntPtr?) nreal32; // u64 exp
        nuintPtr = (UIntPtr?) nreal64; // u64 exp
        nuintPtr = (UIntPtr?) ne;

// Lifted IntPtr to numeric:

        nuint8 = (byte?) nintPtr;
        nuint16 = (ushort?) nintPtr;
        nchr = (char?) nintPtr;
        nuint32 = (uint?) nintPtr;
        nuint64 = (ulong?) nintPtr;
        nuint32 = (uint?) nuintPtr;
        nint8 = (sbyte?) nintPtr;
        nint16 = (short?) nintPtr;
        nint32 = (int?) nintPtr;
        nint64 = (long?) nintPtr;
        nreal32 = (float?) nintPtr;
        nreal64 = (double?) nintPtr;
        ne = (DayOfWeek?) nintPtr;

// Lifted UIntPtr to numeric:

        nuint8 = (byte?) nuintPtr;
        nuint16 = (ushort?) nuintPtr;
        nchr = (char?) nuintPtr;
        nuint64 = (ulong?) nuintPtr;
        nint8 = (sbyte?) nuintPtr;
        nint16 = (short?) nuintPtr;
        nint32 = (int?) nuintPtr;
        nint64 = (long?) nuintPtr;
        nreal32 = (float?) nuintPtr;
        nreal64 = (double?) nuintPtr;
        ne = (DayOfWeek?) nuintPtr;

// From pointer

        intPtr = (IntPtr) pvoid;  // pv id
        uintPtr = (UIntPtr) pvoid;  // pv id
        nintPtr = (IntPtr?) pvoid;  // pv id
        nuintPtr = (UIntPtr?) pvoid;  // pv id
        intPtr = (IntPtr) pint;   // pv ptr
        uintPtr = (UIntPtr) pint;   // pv ptr
        nintPtr = (IntPtr?) pint;   // pv ptr
        nuintPtr = (UIntPtr?) pint;   // pv ptr

// To pointer

        pvoid = (void*) intPtr; // pv
        pvoid = (void*) uintPtr; // pv
        pvoid = (void*) nintPtr; // pv
        pvoid = (void*) nuintPtr; // pv
        pint = (int*) intPtr; // pv
        pint = (int*) uintPtr; // pv
        pint = (int*) nintPtr; // pv
        pint = (int*) nuintPtr; // pv

";
            string source5 = "}}";

            // All of the cases above should pass semantic analysis:
            var comp = CreateCompilation(source1 + source2 + source3 + source4 + source5, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();

            // However, we have not yet implemented lowering and code generation for decimal,
            // lifted operators, nullable conversions and unsafe code so only generate code for
            // the straightforward intptr/number conversions:

            var verifier = CompileAndVerify(source: source1 + source3 + source5, options: TestOptions.UnsafeReleaseExe, expectedOutput: "");
        }

        [Fact, WorkItem(543427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543427")]
        public void Bug11203()
        {
            string source = @"
class Program
{
    static void Main()
    {
        A x = 1;
    }
}

class A
{
    public static implicit operator A(ulong x)
    {
        return null;
    }

    public static implicit operator A(long x)
    {
        return null;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,15): error CS0457: Ambiguous user defined conversions 'A.implicit operator A(ulong)' and 'A.implicit operator A(long)' when converting from 'int' to 'A'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "1").WithArguments("A.implicit operator A(ulong)", "A.implicit operator A(long)", "int", "A"));
        }

        [Fact, WorkItem(543430, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543430")]
        public void Bug11205()
        {
            string source1 = @"
using System;
class A
{
    public A(short i) : this(i.ToString()) {}
    public A(string str) { this.str = str; }
    public readonly string str;

    public static implicit operator short(A x)
    {
        return (short)int.Parse(x.str);
    }

    public static implicit operator A(short x)
    {
        return new A(x.ToString());
    }

    public override string ToString() { return 'A' + str; }

    static void Main()
    {
        var a = new A(5);
        Console.Write((object)(a));
        Console.Write((object)(a++));
        Console.Write((object)(a));
        Console.Write((object)(++a));
        Console.Write((object)(a));
        Console.Write((object)(a--));
        Console.Write((object)(a));
        Console.Write((object)(--a));
        Console.Write((object)(a));
    }
}
";

            string source2 = @"
class A
{
    public static implicit operator int(A x)
    {
        return 1;
    }

    static void Main()
    {
        A a = new A();
        a++;
    }
}
";

            CompileAndVerify(source1, expectedOutput: "A5A5A6A7A7A7A6A5A5");

            var comp = CreateCompilation(source2);
            comp.VerifyDiagnostics(
// (13,9): error CS0029: Cannot implicitly convert type 'int' to 'A'
//         a++;
Diagnostic(ErrorCode.ERR_NoImplicitConv, "a++").WithArguments("int", "A")
                );
        }

        [Fact, WorkItem(543435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543435")]
        public void TestBug11210()
        {
            // If we have both a user-defined implicit conversion and a built-in explicit conversion
            // then the built-in explicit conversion "wins" when cast, and the user-defined implicit
            // conversion "wins" when there is no cast. 

            string source = @"
using System;
class B
{
    static void Main()
    {
        C<int> x = new D<int>();
        D<int> y = (D<int>)x;
        Console.Write(y == null ? 1 : 2);
        y = x;
        Console.Write(y == null ? 3 : 4);
    }
}
class C<T> { }
class D<T> : C<T>
{
    public static implicit operator D<T>(C<int> x)
    {
        return null;
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput: "23");
        }

        [Fact, WorkItem(543436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543436")]
        public void TestBug11211()
        {
            string source = @"
using System;
class C
{
    string str;
    public static explicit operator C(Func<string> x)
    {
        C c = new C();
        c.str = x();
        return c;
    }
 
    static void Main()
    {
        string x = 'A'.ToString();
        var c = (C)(() => x.ToLower());
        Console.WriteLine(c.str);
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput: "a");
        }

        [Fact, WorkItem(543439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543439")]
        public void TestBug11214()
        {
            // The specification describes analysis of user-defined conversions only in
            // terms of the types of the source and destination; it strongly implies that
            // analysis should only consider the type of the source expression and not the
            // source expression itself. The dev10 compiler certainly does not do this;
            // it allows user-defined conversions from null literals, lambdas, method groups,
            // and so on. 
            //
            // We should update the specification to say that the algorithm takes the 
            // expression being converted into account, not just its type.

            string source = @"
struct C
{
    string str;
    public static implicit operator C?(string x)
    {
        C c = new C();
        c.str = x.ToLower();
        return c;
    }

    static void Main()
    {
        C c = (C)'B'.ToString();
        System.Console.WriteLine(c.str);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "b");
        }

        [Fact, WorkItem(543440, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543440")]
        public void TestBug11215()
        {
            string source = @"
using System;
static class A
{
    static void Main()
    {
        C x = 1;
        Console.WriteLine(x.str);
    }
}
class C
{
    public string str;
    public static implicit operator C(byte s)
    {
        C c = new C();
        c.str = s.ToString();
        return c;
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
        }

        [Fact, WorkItem(543441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543441")]
        public void TestBug11216()
        {
            // An ambiguous user-defined conversion should be considered a valid conversion
            // for the purposes of overload resolution; that is, this program should say that
            // Goo(B) and Goo(C) are both applicable candidates, and that neither is better,
            // even though the conversion from A to B is ambiguous.

            string source = @"
        class Program
        {
            static void Main()
            {
                A x = null;
                Goo(x);
            }

            static void Goo(B x) { }
            static void Goo(C x) { }
        }

        class A
        {
            public static implicit operator B(A x) { return null; }
        }

        class B
        {
            public static implicit operator B(A x) { return null; }
        }

        class C
        {
            public static implicit operator C(A x) { return null; }
        }
 ";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(// (7,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Goo(B)' and 'Program.Goo(C)'
                                   //                 Goo(x);
                                   Diagnostic(ErrorCode.ERR_AmbigCall, "Goo").WithArguments("Program.Goo(B)", "Program.Goo(C)"));
        }

        [Fact, WorkItem(543446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543446")]
        public void TestBug11223()
        {
            string source = @"
using System;
class C
{
    string str;
    public static explicit operator C(Func<string, string> x)
    {
        C c = new C();
        c.str = x('A'.ToString());
        return c;
    }

    static string M(string s) { return s.ToLower(); }
 
    static void Main()
    {
        var c = (C)M;
        Console.WriteLine(c.str);
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput: "a");
        }

        [Fact, WorkItem(543595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543595")]
        public void CompoundAssignment()
        {
            string source1 = @"
class A
{
    public static implicit operator A(int a)
    {
        return new A();
    }
    public static int operator +(A a, int b)
    {
        return 1;
    }
}

class Program
{
    static void Main()
    {
        A a = new A();
        a += 5;
    }
}
";

            var comp = CreateCompilation(source1);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(source: source1, expectedOutput: "");
        }

        [Fact, WorkItem(543598, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543598")]
        public void ConvertByteLiteralToUserDefinedType()
        {
            var source = @"
public class Conv
{
    static public implicit operator byte(Conv test) { return 1; }
    static public implicit operator Conv(byte val) { return null; }
}
class Test
{
    public static void Main()
    {
        Conv cl = new Conv();
        cl =  1;
    }
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(543789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543789")]
        public void UseImplicitConversionInBase()
        {
            string source = @"
using System;

class Program
{
    static void Main()
    {
        if (-(new A()))
        {
            Console.Write(""Hello"");
        }
    }
}

class B
{
    public static implicit operator bool(B p)
    {
        return true;
    }
}

class A : B
{
    public static A operator -(A p)
    {
        return null;
    }
}";
            CompileAndVerify(source, expectedOutput: "Hello");
        }

        [Fact, WorkItem(682456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682456")]
        public void GenericUDConversionVersusPredefinedConversion()
        {
            string source = @"
class InArgument<T>
{
    public static implicit operator InArgument<T>(T t) { return default(InArgument<T>); }
}

public struct @start
{
    static public void Main()
    {
        object bar = null;
        InArgument<object> outArgument2 = bar as InArgument<object>;
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(1063555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063555")]
        public void UserDefinedImplicitConversionsOnBuiltinTypes()
        {
            string source = @"
namespace System
{
   	public class Object { }
    public class ValueType { }
    public struct Void { }

    public struct Byte { public static implicit operator MyClass(Byte v) { return null; } }
    public struct Int16 { public static implicit operator MyClass(Int16 v) { return null; } }
    public struct Int32 { public static implicit operator MyClass(Int32 v) { return null; } }
    public struct Int64 { public static implicit operator MyClass(Int64 v) { return null; } }
    public struct Single { public static implicit operator MyClass(Single v) { return null; } }
    public struct Double { public static implicit operator MyClass(Double v) { return null; } }
    public struct Char { public static implicit operator MyClass(Char v) { return null; } }
    public struct Boolean { public static implicit operator MyClass(Boolean v) { return null; } }
    public struct SByte { public static implicit operator MyClass(SByte v) { return null; } }
    public struct UInt16 { public static implicit operator MyClass(UInt16 v) { return null; } }
    public struct UInt32 { public static implicit operator MyClass(UInt32 v) { return null; } }
    public struct UInt64 { public static implicit operator MyClass(UInt64 v) { return null; } }
    public struct IntPtr { public static implicit operator MyClass(IntPtr v) { return null; } }
    public struct UIntPtr { public static implicit operator MyClass(UIntPtr v) { return null; } }
    public struct Decimal { public static implicit operator MyClass(Decimal v) { return null; } }
    public class String { public static implicit operator MyClass(String v) { return null; } }

    public class MyClass
    {
        static void Test(MyClass v) { }

        static void Main()
        {
            Test(new Byte());
            Test(new Int16());
            Test(new Int32());
            Test(new Int64());
            Test(new Single());
            Test(new Double());
            Test(new Char());
            Test(new Boolean());
            Test(new SByte());
            Test(new UInt16());
            Test(new UInt32());
            Test(new UInt64());
            Test(new IntPtr());
            Test(new String());
            
        }
    }
}
";
            CreateEmptyCompilation(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(34876, "https://github.com/dotnet/roslyn/pull/34876")]
        public void GenericOperatorVoidConversion()
        {
            var source = @"
class C<T>
{
    public static implicit operator C<T>(T t) => new C<T>();

    private static void M1() { }
    private static C<object> M2()
    {
        return M1();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,16): error CS0029: Cannot implicitly convert type 'void' to 'C<object>'
                //         return M1();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "M1()").WithArguments("void", "C<object>").WithLocation(9, 16));
        }

        [Fact, WorkItem(34876, "https://github.com/dotnet/roslyn/pull/34876")]
        public void GenericOperatorVoidConversion_Cast()
        {
            var source = @"
class C<T>
{
    public static explicit operator C<T>(T t) => new C<T>();

    private static void M1() { }
    private static C<object> M2()
    {
        return (C<object>) M1();
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,16): error CS0030: Cannot convert type 'void' to 'C<object>'
                //         return (C<object>) M1();
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(C<object>) M1()").WithArguments("void", "C<object>").WithLocation(9, 16));
        }

        [Fact, WorkItem(56646, "https://github.com/dotnet/roslyn/issues/56646")]
        public void LiftedConversion_InvalidTypeArgument01()
        {
            var code = @"
int? i = null;
C c = i;

class C
{
    public static implicit operator C(long l) => throw null;
}

namespace System
{
    public class Object {}
    public class String {}
    public class Exception {}
    public class ValueType : Object {}
    public struct Void {}
    public struct Int32 {}
    public struct Nullable<T> where T : struct {}
    public ref struct Int64 {}
}
";

            var comp = CreateEmptyCompilation(code);
            comp.VerifyDiagnostics(
                // (3,7): error CS0266: Cannot implicitly convert type 'int?' to 'C'. An explicit conversion exists (are you missing a cast?)
                // C c = i;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "i").WithArguments("int?", "C").WithLocation(3, 7)
            );
        }

        [Fact, WorkItem(56646, "https://github.com/dotnet/roslyn/issues/56646")]
        public void LiftedConversion_InvalidTypeArgument02()
        {
            var code = @"
C c = null;
M(c);

void M(long? i) => throw null;

class C
{
    public static implicit operator int(C c) => throw null;
}

namespace System
{
    public class Object {}
    public class String {}
    public class Exception {}
    public class ValueType : Object {}
    public struct Void {}
    public ref struct Int32 {}
    public struct Nullable<T> where T : struct { public Nullable(T value) {} }
    public struct Int64 {}
    public struct Boolean { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
";

            var comp = CreateEmptyCompilation(code);
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);

            // Note that no int? is being created.
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       ""int C.op_Implicit(C)""
  IL_0006:  conv.i8
  IL_0007:  newobj     ""long?..ctor(long)""
  IL_000c:  call       ""void Program.<<Main>$>g__M|0_0(long?)""
  IL_0011:  ret
}
");
        }

        [Fact, WorkItem(56646, "https://github.com/dotnet/roslyn/issues/56646")]
        public void LiftedConversion_InvalidTypeArgument03()
        {
            var code = @"
int? i = null;
C c = (C)i;

class C
{
    public static explicit operator C(long l) => throw null;
}

namespace System
{
    public class Object {}
    public class String {}
    public class Exception {}
    public class ValueType : Object {}
    public struct Void {}
    public struct Int32 {}
    public struct Nullable<T> where T : struct { public T Value { get => throw null; } }
    public ref struct Int64 {}
    public struct Boolean { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
";

            var comp = CreateEmptyCompilation(code);
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (int? V_0) //i
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""int?""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""int int?.Value.get""
  IL_000f:  conv.i8
  IL_0010:  call       ""C C.op_Explicit(long)""
  IL_0015:  pop
  IL_0016:  ret
}
");
        }

        [Fact, WorkItem(56646, "https://github.com/dotnet/roslyn/issues/56646")]
        public void LiftedConversion_InvalidTypeArgument04()
        {
            var code = @"
C c = null;
M((long?)c);

void M(long? i) => throw null;

class C
{
    public static explicit operator int(C c) => throw null;
}

namespace System
{
    public class Object {}
    public class String {}
    public class Exception {}
    public class ValueType : Object {}
    public struct Void {}
    public ref struct Int32 {}
    public struct Nullable<T> where T : struct { public Nullable(T value) {} }
    public struct Int64 {}
    public struct Boolean { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
}
";

            var comp = CreateEmptyCompilation(code);
            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);

            // Note that no int? is being created.
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  call       ""int C.op_Explicit(C)""
  IL_0006:  conv.i8
  IL_0007:  newobj     ""long?..ctor(long)""
  IL_000c:  call       ""void Program.<<Main>$>g__M|0_0(long?)""
  IL_0011:  ret
}
");
        }

        [Fact, WorkItem(56646, "https://github.com/dotnet/roslyn/issues/56646")]
        public void LiftedConversion_InvalidTypeArgument05()
        {
            var code = @"
unsafe
{
    S? s = null;
    void* f = s;
    System.Console.WriteLine((int)f);
}

public struct S
{
    public static unsafe implicit operator void*(S v) => throw null;
}
";

            var comp = CreateCompilation(code, options: TestOptions.UnsafeReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Skipped);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init (S? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S?""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool S?.HasValue.get""
  IL_0011:  brtrue.s   IL_0017
  IL_0013:  ldc.i4.0
  IL_0014:  conv.u
  IL_0015:  br.s       IL_0023
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""S S?.GetValueOrDefault()""
  IL_001e:  call       ""void* S.op_Implicit(S)""
  IL_0023:  conv.i4
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  ret
}
");
        }

        [Fact, WorkItem(56646, "https://github.com/dotnet/roslyn/issues/56646")]
        public void LiftedConversion_InvalidTypeArgument06()
        {
            var code = @"
unsafe
{
    S? s = null;
    void* f = (void*)s;
    System.Console.WriteLine((int)f);
}

public struct S
{
    public static unsafe explicit operator void*(S v) => throw null;
}
";

            var comp = CreateCompilation(code, options: TestOptions.UnsafeReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Skipped);

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       42 (0x2a)
  .maxstack  1
  .locals init (S? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S?""
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""bool S?.HasValue.get""
  IL_0011:  brtrue.s   IL_0017
  IL_0013:  ldc.i4.0
  IL_0014:  conv.u
  IL_0015:  br.s       IL_0023
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       ""S S?.GetValueOrDefault()""
  IL_001e:  call       ""void* S.op_Explicit(S)""
  IL_0023:  conv.i4
  IL_0024:  call       ""void System.Console.WriteLine(int)""
  IL_0029:  ret
}
");
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_ImplicitObjectCreation_Ambiguous()
        {
            var source = """
                C c = ("a", new());

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                    public static implicit operator C((string, string) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", new());
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", new())").WithArguments("2", "C").WithLocation(1, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_ImplicitObjectCreation_NotAmbiguous()
        {
            var source = """
                using System;

                C c = ("a", new());

                class C
                {
                    public static implicit operator C((string, int) pair)
                    {
                        Console.WriteLine("int");
                        return new C();
                    }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "int");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_DefaultLiteral_Ambiguous()
        {
            var source = """
                C c = ("a", default);

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                    public static implicit operator C((string, string) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", default);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", default)").WithArguments("2", "C").WithLocation(1, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_NullLiteral_NotAmbiguous()
        {
            var source = """
                using System;

                C c = ("a", null);

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                    public static implicit operator C((string, string) pair)
                    {
                        Console.WriteLine("string");
                        return new C();
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_NullLiteral_NoConversion()
        {
            var source = """
                C c = ("a", null);

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", null);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", null)").WithArguments("2", "C").WithLocation(1, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_SwitchExpression_Ambiguous()
        {
            var source = """
                C c = ("a", "b" switch { _ => default });

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                    public static implicit operator C((string, string) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", "b" switch { _ => default });
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", ""b"" switch { _ => default })").WithArguments("2", "C").WithLocation(1, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_SwitchExpression_NotAmbiguous()
        {
            var source = """
                using System;

                C c = ("a", "b" switch { _ => null });

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                    public static implicit operator C((string, string) pair)
                    {
                        Console.WriteLine("string");
                        return new C();
                    }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "string");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_SwitchExpression_NoConversion()
        {
            var source = """
                C c = ("a", "b" switch { _ => null });

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", "b" switch { _ => null });
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", ""b"" switch { _ => null })").WithArguments("2", "C").WithLocation(1, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_CollectionExpression_Ambiguous()
        {
            var source = """
                C c = ("a", [default]);

                class C
                {
                    public static implicit operator C((string, int[]) pair) => new C();
                    public static implicit operator C((string, string[]) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", [default]);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", [default])").WithArguments("2", "C").WithLocation(1, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_CollectionExpression_NotAmbiguous()
        {
            var source = """
                using System;

                C c = ("a", [null]);

                class C
                {
                    public static implicit operator C((string, int[]) pair) => new C();
                    public static implicit operator C((string, string[]) pair)
                    {
                        Console.WriteLine("string[]");
                        return new C();
                    }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "string[]");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_CollectionExpression_NoConversion()
        {
            var source = """
                C c = ("a", [null]);

                class C
                {
                    public static implicit operator C((string, int[]) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (1,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", [null]);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", [null])").WithArguments("2", "C").WithLocation(1, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_ConditionalExpression_Ambiguous()
        {
            var source = """
                bool b = true;
                C c = ("a", b ? default : throw null!);

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                    public static implicit operator C((string, string) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", b ? default : throw null!);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", b ? default : throw null!)").WithArguments("2", "C").WithLocation(2, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_ConditionalExpression_NotAmbiguous()
        {
            var source = """
                using System;

                bool b = true;
                C c = ("a", b ? null : throw null!);

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                    public static implicit operator C((string, string) pair)
                    {
                        Console.WriteLine("string");
                        return new C();
                    }
                }
                """;

            var verifier = CompileAndVerify(source, expectedOutput: "string");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Tuple_UserDefinedConversion_ConditionalExpression_NoConversion()
        {
            var source = """
                bool b = true;
                C c = ("a", b ? null : throw null!);

                class C
                {
                    public static implicit operator C((string, int) pair) => new C();
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,7): error CS8135: Tuple with 2 elements cannot be converted to type 'C'.
                // C c = ("a", b ? null : throw null!);
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""a"", b ? null : throw null!)").WithArguments("2", "C").WithLocation(2, 7));
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2088611")]
        public void Repro_VsFeedback_2088611()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                public struct SpecialStruct
                {
                    public readonly string Value;
                    public readonly bool Flag;
                    public readonly Dictionary<string, string> Map;

                    public SpecialStruct(string value)
                    {
                        this.Value = value;
                    }
                    public SpecialStruct(string value, bool flag)
                    {
                        this.Value = value;
                        this.Flag = flag;
                    }
                    public SpecialStruct(string value, Dictionary<string, string> map)
                    {
                        Value = value;
                        Map = map;
                    }

                    public static implicit operator SpecialStruct(string s) => new(s);
                    public static implicit operator SpecialStruct((string, Dictionary<string, string>) tuple) => new(tuple.Item1, tuple.Item2);
                    public static implicit operator SpecialStruct((string, bool) tuple) => new(tuple.Item1, tuple.Item2);
                }

                public class Class1
                {
                    Dictionary<string, SpecialStruct> specialMap = new()
                        {
                            { "key1", "value1" },
                            { "key2", ("value2", true) },
                            { "key3", ("value3", new /*specific type is omitted*/ (StringComparer.OrdinalIgnoreCase)
                                {
                                    { "subkey", "subvalue" },
                                })
                            },
                        };
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (35,23): error CS8135: Tuple with 2 elements cannot be converted to type 'SpecialStruct'.
                //             { "key3", ("value3", new /*specific type is omitted*/ (StringComparer.OrdinalIgnoreCase)
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, @"(""value3"", new /*specific type is omitted*/ (StringComparer.OrdinalIgnoreCase)
                {
                    { ""subkey"", ""subvalue"" },
                })").WithArguments("2", "SpecialStruct").WithLocation(35, 23),
                // (35,34): error CS8754: There is no target type for 'new(System.StringComparer)'
                //             { "key3", ("value3", new /*specific type is omitted*/ (StringComparer.OrdinalIgnoreCase)
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, @"new /*specific type is omitted*/ (StringComparer.OrdinalIgnoreCase)
                {
                    { ""subkey"", ""subvalue"" },
                }").WithArguments("new(System.StringComparer)").WithLocation(35, 34));
        }
    }
}
