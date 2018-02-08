using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Attributes
{
    public class AttributeTests_Generics : WellKnownAttributesTestBase
    {
        [Fact]
        public void TestCompileGenericAttributes()
        {
            const string genericTestSource = @"
using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class XAttribute<T> : Attribute
{
	public int Value { get; set; }
		
	public XAttribute()
	{
		Value = -1;
	}
		
	public XAttribute(int value)
	{
		Value = value;
	}
}

interface YInterface
{
}

class YClass : YInterface
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class YAttribute<T> : Attribute
    where T : YInterface, new()
{
}

[XAttribute<int>()]
[XAttribute<int>(1)]
[XAttribute<int>(2)]
[YAttribute<YClass>()]
class Demo
{
}
";

            CompileAndVerify(genericTestSource, options: TestOptions.UnsafeReleaseDll, additionalRefs: new[] { SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef });
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void GenericAttributeType()
        {
            var source = @"
using System;

[A<>]
[A<int>]
[B]
[B<>]
[B<int>]
[C]
[C<>]
[C<int>]
[C<,>]
[C<int, int>]
class Test
{
}

public class A : Attribute
{
}

public class B<T> : Attribute
{
}

public class C<T, U> : Attribute
{
}
";

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(

                // NOTE: Dev11 reports ERR_AttributeCantBeGeneric for these, but this makes more sense.

                // (4,2): error CS0308: The non-generic type 'A' cannot be used with type arguments
                // [A<>]
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "A<>").WithArguments("A", "type"),
                // (5,2): error CS0308: The non-generic type 'A' cannot be used with type arguments
                // [A<int>]
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "A<int>").WithArguments("A", "type"),

                // (6,2): error CS0305: Using the generic type 'B<T>' requires 1 type arguments
                // [B]
                Diagnostic(ErrorCode.ERR_BadArity, "B").WithArguments("B<T>", "type", "1"),
                // (7,2): error CS7003: Unexpected use of an unbound generic name
                // [B<>]
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "B<>"),
                // (9,2): error CS0305: Using the generic type 'C<T, U>' requires 2 type arguments
                // [C<>]
                Diagnostic(ErrorCode.ERR_BadArity, "C").WithArguments("C<T, U>", "type", "2"),
                // (10,2): error CS0305: Using the generic type 'C<T, U>' requires 2 type arguments
                // [C<>]
                Diagnostic(ErrorCode.ERR_BadArity, "C<>").WithArguments("C<T, U>", "type", "2"),
                // (11,2): error CS0305: Using the generic type 'C<T, U>' requires 2 type arguments
                // [C<int>]
                Diagnostic(ErrorCode.ERR_BadArity, "C<int>").WithArguments("C<T, U>", "type", "2"),
                // (12,2): error CS7003: Unexpected use of an unbound generic name
                // [C<,>]
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "C<,>"));
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void AliasedGenericAttributeType_Source()
        {
            var source = @"
using System;
using Alias = C<int>;

[Alias]
[Alias<>]
[Alias<int>]
class Test
{
}

public class C<T> : Attribute
{
}
";

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
                // (6,2): error CS0307: The using alias 'Alias' cannot be used with type arguments
                // [Alias<>]
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "Alias<>").WithArguments("Alias", "using alias"),
                // (7,2): error CS0307: The using alias 'Alias' cannot be used with type arguments
                // [Alias<int>]
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "Alias<int>").WithArguments("Alias", "using alias"));
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void AliasedGenericAttributeType_Metadata()
        {
            var il = @"
.class public auto ansi beforefieldinit C`1<T>
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Attribute::.ctor()
    ret
  }
}
";

            var source = @"
using Alias = C<int>;

[Alias]
[Alias<>]
[Alias<int>]
class Test
{
}
";

            // NOTE: Dev11 does not give an error for "[Alias]" - it just silently drops the
            // attribute at emit-time.
            var comp = CreateCompilationWithCustomILSource(source, il);
            comp.VerifyDiagnostics(
                // (5,2): error CS0307: The using alias 'Alias' cannot be used with type arguments
                // [Alias<>]
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "Alias<>").WithArguments("Alias", "using alias"),
                // (6,2): error CS0307: The using alias 'Alias' cannot be used with type arguments
                // [Alias<int>]
                Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "Alias<int>").WithArguments("Alias", "using alias"));
        }

        [WorkItem(611177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611177")]
        [Fact]
        public void AliasedGenericAttributeType_Nested()
        {
            var source = @"
using InnerAlias = Outer<int>.Inner;
using OuterAlias = Outer<int>;

[InnerAlias]
class Test
{
    [OuterAlias.Inner]
    static void Main()
    {
    }
}

public class Outer<T>
{
    // Not a subtype of Attribute, since that wouldn't compile.
    public class Inner 
    {
    }
}
";

            var comp = CreateStandardCompilation(source);
            comp.VerifyDiagnostics(
            // (5,2): error CS0616: 'Outer<int>.Inner' is not an attribute class
            // [InnerAlias]
            Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "InnerAlias").WithArguments("Outer<int>.Inner"),
            // (8,17): error CS0616: 'Outer<int>.Inner' is not an attribute class
            //     [OuterAlias.Inner]
            Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "Inner").WithArguments("Outer<int>.Inner"));
        }

        [WorkItem(543914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543914")]
        [Fact]
        public void OpenGenericTypeInAttribute()
        {
            var source =
@"
class Gen<T> {}
class Gen2<T>: System.Attribute {}
	
[Gen]
[Gen2]
public class Test
{
	public static int Main()
	{
		return 1;
	}
}";
            CSharpCompilationOptions opt = TestOptions.ReleaseDll;

            var compilation = CreateStandardCompilation(source, null, options: opt);

            compilation.VerifyDiagnostics(
                // (5,2): error CS0616: 'Gen<T>' is not an attribute class
                // [Gen]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "Gen").WithArguments("Gen<T>"),
                // (6,2): error CS0305: Using the generic type 'Gen2<T>()' requires 1 type arguments
                // [Gen2]
                Diagnostic(ErrorCode.ERR_BadArity, "Gen2").WithArguments("Gen2<T>", "type", "1"));
        }

        [WorkItem(541072, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541072")]
        [Fact]
        public void AttributeContainsGeneric()
        {
            string source = @"
[Foo<int>]
class G
{
}
class Foo<T>
{
}
";

            var compilation = CreateStandardCompilation(source);
            compilation.VerifyDiagnostics(
                // (2,2): error CS0616: 'Foo<T>' is not an attribute class
                // [Foo<int>]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "Foo<int>").WithArguments("Foo<T>"));
        }
    }
}
