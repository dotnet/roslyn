// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class HoistedThisTests : ExpressionCompilerTestBase
    {
        [WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")]
        [Fact]
        public void InstanceIterator_NoCapturing()
        {
            var source = @"
class C
{
    System.Collections.IEnumerable F()
    {
        yield break;
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<F>d__0.MoveNext", "C", expectedIL);
        }

        [WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")]
        [Fact]
        public void InstanceAsync_NoCapturing()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        await Console.Out.WriteLineAsync('a');
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__0 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<F>d__0.MoveNext", "C", expectedIL);
        }

        [WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")]
        [Fact]
        public void InstanceLambda_NoCapturing()
        {
            var source = @"
class C
{
    void M()
    {
        System.Action a = () => 1.Equals(2);
        a();
    }
}
";
            // This test documents the fact that, as in dev12, "this"
            // is unavailable while stepping through the lambda.  It
            // would be preferable if it were.
            VerifyNoThis(source, "C.<>c.<M>b__0_0");
        }

        [Fact]
        public void InstanceLambda_NoCapturingExceptThis()
        {
            var source = @"
class C
{
    void M()
    {
        System.Action a = () => this.ToString();
        a();
    }
}
";
            var expectedIL = @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}";

            VerifyHasThis(source, "C.<M>b__0_0", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceIterator_CapturedThis()
        {
            var source = @"
class C
{
    System.Collections.IEnumerable F()
    {
        yield return this;
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<F>d__0.MoveNext", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceAsync_CapturedThis()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async Task F()
    {
        await Console.Out.WriteLineAsync(this.ToString());
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__0 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<F>d__0.MoveNext", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceLambda_CapturedThis_DisplayClass()
        {
            var source = @"
class C
{
    int x;

    void M(int y)
    {
        System.Action a = () => x.Equals(y);
        a();
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<>c__DisplayClass1_0.<M>b__0", "C", expectedIL, thisCanBeElided: false);
        }

        [WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")]
        [Fact]
        public void InstanceLambda_CapturedThis_NoDisplayClass()
        {
            var source = @"
class C
{
    int x;

    void M(int y)
    {
        System.Action a = () => x.Equals(1);
        a();
    }
}
";
            var expectedIL = @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}";

            VerifyHasThis(source, "C.<M>b__1_0", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceIterator_Generic()
        {
            var source = @"
class C<T>
{
    System.Collections.IEnumerable F<U>()
    {
        yield return this;
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C<T> C<T>.<F>d__0<U>.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<F>d__0.MoveNext", "C<T>", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceAsync_Generic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C<T>
{
    async Task F<U>()
    {
        await Console.Out.WriteLineAsync(this.ToString());
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C<T>.<F>d__0<U> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C<T> C<T>.<F>d__0<U>.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<F>d__0.MoveNext", "C<T>", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceLambda_Generic()
        {
            var source = @"
class C<T>
{
    int x;

    void M<U>(int y)
    {
        System.Action a = () => x.Equals(y);
        a();
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C<T> C<T>.<>c__DisplayClass1_0<U>.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<>c__DisplayClass1_0.<M>b__0", "C<T>", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceIterator_ExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    System.Collections.IEnumerable F();
}

class C : I
{
    System.Collections.IEnumerable I.F()
    {
        yield return this;
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<I-F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<I-F>d__0.MoveNext", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceAsync_ExplicitInterfaceImplementation()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface I
{
    Task F();
}

class C : I
{
    async Task I.F()
    {
        await Console.Out.WriteLineAsync(this.ToString());
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<I-F>d__0 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<I-F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<I-F>d__0.MoveNext", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceLambda_ExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    void M(int y);
}

class C : I
{
    int x;

    void I.M(int y)
    {
        System.Action a = () => x.Equals(y);
        a();
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<>c__DisplayClass1_0.<I.M>b__0", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceIterator_ExplicitGenericInterfaceImplementation()
        {
            var source = @"
interface I<T>
{
    System.Collections.IEnumerable F();
}

class C : I<int>
{
    System.Collections.IEnumerable I<int>.F()
    {
        yield return this;
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<I<System-Int32>-F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<I<System-Int32>-F>d__0.MoveNext", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceAsync_ExplicitGenericInterfaceImplementation()
        {
            var source = @"
using System;
using System.Threading.Tasks;

interface I<T>
{
    Task F();
}

class C : I<int>
{
    async Task I<int>.F()
    {
        await Console.Out.WriteLineAsync(this.ToString());
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<I<System-Int32>-F>d__0 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<I<System-Int32>-F>d__0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<I<System-Int32>-F>d__0.MoveNext", "C", expectedIL, thisCanBeElided: false);
        }

        [Fact]
        public void InstanceLambda_ExplicitGenericInterfaceImplementation()
        {
            var source = @"
interface I<T>
{
    void M(int y);
}

class C : I<int>
{
    int x;

    void I<int>.M(int y)
    {
        System.Action a = () => x.Equals(y);
        a();
    }
}
";
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ret
}";

            VerifyHasThis(source, "C.<>c__DisplayClass1_0.<I<System.Int32>.M>b__0", "C", expectedIL, thisCanBeElided: false);
        }

        [WorkItem(1066489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066489")]
        [Fact]
        public void InstanceIterator_ExplicitInterfaceImplementation_OldName()
        {
            var ilSource = @"
.class interface public abstract auto ansi I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerable 
          F() cil managed
  {
  } // end of method I`1::F

} // end of class I`1

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
       implements class I`1<int32>
{
  .class auto ansi sealed nested private beforefieldinit '<I<System.Int32>'.'F>d__0'
         extends [mscorlib]System.Object
         implements class [mscorlib]System.Collections.Generic.IEnumerable`1<object>,
                    [mscorlib]System.Collections.IEnumerable,
                    class [mscorlib]System.Collections.Generic.IEnumerator`1<object>,
                    [mscorlib]System.Collections.IEnumerator,
                    [mscorlib]System.IDisposable
  {
    .field private object '<>2__current'
    .field private int32 '<>1__state'
    .field private int32 '<>l__initialThreadId'
    .field public class C '<>4__this'

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.Generic.IEnumerator`1<object> 
            'System.Collections.Generic.IEnumerable<System.Object>.GetEnumerator'() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance class [mscorlib]System.Collections.IEnumerator 
            System.Collections.IEnumerable.GetEnumerator() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance bool  MoveNext() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot specialname virtual final 
            instance object  'System.Collections.Generic.IEnumerator<System.Object>.get_Current'() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance void  System.Collections.IEnumerator.Reset() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot virtual final 
            instance void  System.IDisposable.Dispose() cil managed
    {
      ldnull
      throw
    }

    .method private hidebysig newslot specialname virtual final 
            instance object  System.Collections.IEnumerator.get_Current() cil managed
    {
      ldnull
      throw
    }

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 '<>1__state') cil managed
    {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

    .property instance object 'System.Collections.Generic.IEnumerator<System.Object>.Current'()
    {
      .get instance object C/'<I<System.Int32>'.'F>d__0'::'System.Collections.Generic.IEnumerator<System.Object>.get_Current'()
    }
    .property instance object System.Collections.IEnumerator.Current()
    {
      .get instance object C/'<I<System.Int32>'.'F>d__0'::System.Collections.IEnumerator.get_Current()
    }
  } // end of class '<I<System.Int32>'.'F>d__0'

  .method private hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.IEnumerable 
          'I<System.Int32>.F'() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class C
";

            ImmutableArray<byte> ilBytes;
            ImmutableArray<byte> ilPdbBytes;
            EmitILToArray(ilSource, appendDefaultHeader: true, includePdb: true, assemblyBytes: out ilBytes, pdbBytes: out ilPdbBytes);

            var runtime = CreateRuntimeInstance(
                references: new[] { MscorlibRef },
                peImage: ilBytes,
                symReader: SymReaderFactory.CreateReader(ilPdbBytes));

            var context = CreateMethodContext(runtime, "C.<I<System.Int32>.F>d__0.MoveNext");
            VerifyHasThis(context, "C", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<I<System.Int32>.F>d__0.<>4__this""
  IL_0006:  ret
}");
        }

        [Fact]
        public void StaticIterator()
        {
            var source = @"
class C
{
    static System.Collections.IEnumerable F()
    {
        yield break;
    }
}
";
            VerifyNoThis(source, "C.<F>d__0.MoveNext");
        }

        [Fact]
        public void StaticAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C<T>
{
    static async Task F<U>()
    {
        await Console.Out.WriteLineAsync('a');
    }
}
";
            VerifyNoThis(source, "C.<F>d__0.MoveNext");
        }

        [Fact]
        public void StaticLambda()
        {
            var source = @"
using System;

class C<T>
{
    static void F<U>(int x)
    {
        Action a = () => x.ToString();
        a();
    }
}
";
            VerifyNoThis(source, "C.<>c__DisplayClass0_0.<F>b__0");
        }

        [Fact]
        public void ExtensionIterator()
        {
            var source = @"
static class C
{
    static System.Collections.IEnumerable F(this int x)
    {
        yield return x;
    }
}
";
            VerifyNoThis(source, "C.<F>d__0.MoveNext");
        }

        [Fact]
        public void ExtensionAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

static class C
{
    static async Task F(this int x)
    {
        await Console.Out.WriteLineAsync(x.ToString());
    }
}
";
            VerifyNoThis(source, "C.<F>d__0.MoveNext");
        }

        [Fact]
        public void ExtensionLambda()
        {
            var source = @"
using System;

static class C
{
    static void F(this int x)
    {
        Action a = () => x.ToString();
        a();
    }
}
";
            VerifyNoThis(source, "C.<>c__DisplayClass0_0.<F>b__0");
        }

        [WorkItem(1072296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072296")]
        [Fact]
        public void OldStyleNonCapturingLambda()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          M() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method private hidebysig static int32 
          '<M>b__0'() cil managed
  {
    ldnull
    throw
  }
} // end of class C
";

            ImmutableArray<byte> ilBytes;
            ImmutableArray<byte> ilPdbBytes;
            EmitILToArray(ilSource, appendDefaultHeader: true, includePdb: true, assemblyBytes: out ilBytes, pdbBytes: out ilPdbBytes);

            var runtime = CreateRuntimeInstance(
                references: new[] { MscorlibRef },
                peImage: ilBytes,
                symReader: SymReaderFactory.CreateReader(ilPdbBytes));

            var context = CreateMethodContext(runtime, "C.<M>b__0");
            VerifyNoThis(context);
        }

        [WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")]
        [Fact]
        public void LambdaLocations_Instance()
        {
            var source = @"
using System;

class C
{
    int _toBeCaptured;

    C()
    {
        int l = ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 4))() + x))(1);
    }

    ~C()
    {
        int l = ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 6))() + x))(1);
    }

    int P
    {
        get
        {
            return ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 7))() + x))(1);
        }
        set
        {
            value = ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 8))() + x))(1);
        }
    }

    int this[int p]
    {
        get
        {
            return ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 9))() + x))(1);
        }
        set
        {
            value = ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 10))() + x))(1);
        }
    }

    event Action E
    {
        add
        {
            int l = ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 11))() + x))(1);
        }
        remove
        {
            int l = ((Func<int, int>)(x => ((Func<int>)(() => _toBeCaptured + x + 12))() + x))(1);
        }
    }
}
";

            var expectedILTemplate = @"
{{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.{0}.<>4__this""
  IL_0006:  ret
}}";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(comp);

            var dummyComp = CreateCompilationWithMscorlib("", new[] { comp.EmitToImageReference() }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var typeC = dummyComp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var displayClassTypes = typeC.GetMembers().OfType<NamedTypeSymbol>();
            Assert.True(displayClassTypes.Any());
            foreach (var displayClassType in displayClassTypes)
            {
                var displayClassName = displayClassType.Name;
                Assert.Equal(GeneratedNameKind.LambdaDisplayClass, GeneratedNames.GetKind(displayClassName));
                foreach (var displayClassMethod in displayClassType.GetMembers().OfType<MethodSymbol>().Where(m => GeneratedNames.GetKind(m.Name) == GeneratedNameKind.LambdaMethod))
                {
                    var lambdaMethodName = string.Format("C.{0}.{1}", displayClassName, displayClassMethod.Name);
                    var context = CreateMethodContext(runtime, lambdaMethodName);
                    var expectedIL = string.Format(expectedILTemplate, displayClassName);
                    VerifyHasThis(context, "C", expectedIL);
                }
            }
        }

        [Fact]
        public void LambdaLocations_Static()
        {
            var source = @"
using System;

class C
{
    static int f = ((Func<int, int>)(x => ((Func<int>)(() => x + 2))() + x))(1);

    static C()
    {
        int l = ((Func<int, int>)(x => ((Func<int>)(() => x + 4))() + x))(1);
    }

    static int P
    {
        get
        {
            return ((Func<int, int>)(x => ((Func<int>)(() => x + 7))() + x))(1);
        }
        set
        {
            value = ((Func<int, int>)(x => ((Func<int>)(() => x + 8))() + x))(1);
        }
    }

    static event Action E
    {
        add
        {
            int l = ((Func<int, int>)(x => ((Func<int>)(() => x + 11))() + x))(1);
        }
        remove
        {
            int l = ((Func<int, int>)(x => ((Func<int>)(() => x + 12))() + x))(1);
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(comp);

            var dummyComp = CreateCompilationWithMscorlib("", new[] { comp.EmitToImageReference() }, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            var typeC = dummyComp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var displayClassTypes = typeC.GetMembers().OfType<NamedTypeSymbol>();
            Assert.True(displayClassTypes.Any());
            foreach (var displayClassType in displayClassTypes)
            {
                var displayClassName = displayClassType.Name;
                Assert.Equal(GeneratedNameKind.LambdaDisplayClass, GeneratedNames.GetKind(displayClassName));
                foreach (var displayClassMethod in displayClassType.GetMembers().OfType<MethodSymbol>().Where(m => GeneratedNames.GetKind(m.Name) == GeneratedNameKind.LambdaMethod))
                {
                    var lambdaMethodName = string.Format("C.{0}.{1}", displayClassName, displayClassMethod.Name);
                    var context = CreateMethodContext(runtime, lambdaMethodName);
                    VerifyNoThis(context);
                }
            }
        }

        private void VerifyHasThis(string source, string methodName, string expectedType, string expectedIL, bool thisCanBeElided = true)
        {
            var sourceCompilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(sourceCompilation);
            var context = CreateMethodContext(runtime, methodName);

            VerifyHasThis(context, expectedType, expectedIL);

            // Now recompile and test CompileExpression with optimized code.
            sourceCompilation = sourceCompilation.WithOptions(sourceCompilation.Options.WithOptimizationLevel(OptimizationLevel.Release));
            runtime = CreateRuntimeInstance(sourceCompilation);
            context = CreateMethodContext(runtime, methodName);
            // In C#, "this" may be optimized away.
            if (thisCanBeElided)
            {
                VerifyNoThis(context);
            }
            else
            {
                VerifyHasThis(context, expectedType, expectedIL: null);
            }
            // Verify that binding a trivial expression succeeds.
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("42", out error, testData);
            Assert.Null(error);
            Assert.Equal(1, testData.Methods.Count);
        }

        private static void VerifyHasThis(EvaluationContext context, string expectedType, string expectedIL)
        {
            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            var testData = new CompilationTestData();
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.NotNull(assembly);
            Assert.NotEqual(assembly.Count, 0);
            var localAndMethod = locals.Single(l => l.LocalName == "this");
            if (expectedIL != null)
            {
                VerifyMethodData(testData.Methods.Single(m => m.Key.Contains(localAndMethod.MethodName)).Value, expectedType, expectedIL);
            }
            locals.Free();

            string error;
            testData = new CompilationTestData();
            context.CompileExpression("this", out error, testData);
            Assert.Null(error);
            if (expectedIL != null)
            {
                VerifyMethodData(testData.Methods.Single(m => m.Key.Contains("<>m0")).Value, expectedType, expectedIL);
            }
        }

        private static void VerifyMethodData(CompilationTestData.MethodData methodData, string expectedType, string expectedIL)
        {
            methodData.VerifyIL(expectedIL);
            var method = (MethodSymbol)methodData.Method;
            VerifyTypeParameters(method);
            Assert.Equal(expectedType, method.ReturnType.ToTestDisplayString());
        }

        private void VerifyNoThis(string source, string methodName)
        {
            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugDll, assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, methodName);
            VerifyNoThis(context);
        }

        private static void VerifyNoThis(EvaluationContext context)
        {
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("this", out error, testData);
            Assert.Contains(error, new[]
            {
                "error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer",
                "error CS0027: Keyword 'this' is not available in the current context",
            });

            testData = new CompilationTestData();
            context.CompileExpression("base.ToString()", out error, testData);
            Assert.Contains(error, new[]
            {
                "error CS1511: Keyword 'base' is not available in a static method",
                "error CS1512: Keyword 'base' is not available in the current context",
            });

            var locals = ArrayBuilder<LocalAndMethod>.GetInstance();
            string typeName;
            testData = new CompilationTestData();
            var assembly = context.CompileGetLocals(locals, argumentsOnly: false, typeName: out typeName, testData: testData);
            Assert.NotNull(assembly);
            AssertEx.None(locals, l => l.LocalName.Contains("this"));
            locals.Free();
        }

        [WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")]
        [Fact]
        public void InstanceMembersInIterator()
        {
            var source =
@"class C
{
    object x;
    System.Collections.IEnumerable F()
    {
        yield return this.x;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(runtime, "C.<F>d__1.MoveNext");
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("this.x", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__1.<>4__this""
  IL_0006:  ldfld      ""object C.x""
  IL_000b:  ret
}");
        }

        [WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")]
        [Fact]
        public void InstanceMembersInAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    object x;
    async Task F()
    {
        await Console.Out.WriteLineAsync(this.ToString());
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(runtime, "C.<F>d__1.MoveNext");
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("this.x", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.<F>d__1 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C C.<F>d__1.<>4__this""
  IL_0006:  ldfld      ""object C.x""
  IL_000b:  ret
}");
        }

        [WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")]
        [Fact]
        public void InstanceMembersInLambda()
        {
            var source =
@"class C
{
    object x;
    void F()
    {
        System.Action a = () => this.x.ToString();
        a();
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(runtime, "C.<F>b__1_0");
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("this.x", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object C.x""
  IL_0006:  ret
}");
        }

        [Fact]
        public void BaseMembersInIterator()
        {
            var source = @"
class Base
{
    protected int x;
}

class Derived : Base
{
    new protected object x;

    System.Collections.IEnumerable M()
    {
        yield return base.x;
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(runtime, "Derived.<M>d__1.MoveNext");
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("base.x", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Derived Derived.<M>d__1.<>4__this""
  IL_0006:  ldfld      ""int Base.x""
  IL_000b:  ret
}");
        }

        [Fact]
        public void BaseMembersInAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Base
{
    protected int x;
}

class Derived : Base
{
    new protected object x;

    async Task M()
    {
        await Console.Out.WriteLineAsync(this.ToString());
    }
}";
            var compilation0 = CreateCompilationWithMscorlib45(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(runtime, "Derived.<M>d__1.MoveNext");
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("base.x", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (int V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                Derived.<M>d__1 V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Derived Derived.<M>d__1.<>4__this""
  IL_0006:  ldfld      ""int Base.x""
  IL_000b:  ret
}");
        }

        [Fact]
        public void BaseMembersInLambda()
        {
            var source = @"
class Base
{
    protected int x;
}

class Derived : Base
{
    new protected object x;

    void F()
    {
        System.Action a = () => this.x.ToString();
        a();
    }
}";
            var compilation0 = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            var runtime = CreateRuntimeInstance(compilation0);
            var context = CreateMethodContext(runtime, "Derived.<F>b__1_0");
            string error;
            var testData = new CompilationTestData();
            context.CompileExpression("this.x", out error, testData);
            testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object Derived.x""
  IL_0006:  ret
}");
        }

        [Fact]
        public void IteratorOverloading_Parameters1()
        {
            var source = @"
public class C
{
    public System.Collections.IEnumerable M()
    {
        yield return this;
    }

    public System.Collections.IEnumerable M(int x)
    {
        return null;
    }
}";
            CheckIteratorOverloading(source, m => m.ParameterCount == 0);
        }

        [Fact]
        public void IteratorOverloading_Parameters2() // Same as above, but declarations reversed.
        {
            var source = @"
public class C
{
    public System.Collections.IEnumerable M(int x)
    {
        return null;
    }

    public System.Collections.IEnumerable M()
    {
        yield return this;
    }
}";
            // NB: We pick the wrong overload, but it doesn't matter because 
            // the methods have the same characteristics.
            // Also, we don't require this behavior, we're just documenting it.
            CheckIteratorOverloading(source, m => m.ParameterCount == 1);
        }

        [Fact]
        public void IteratorOverloading_Staticness()
        {
            var source = @"
public class C
{
    public static System.Collections.IEnumerable M(int x)
    {
        return null;
    }

    // NB: We declare the interesting overload last so we know we're not
    // just picking the first one by mistake.
    public System.Collections.IEnumerable M()
    {
        yield return this;
    }
}";
            CheckIteratorOverloading(source, m => !m.IsStatic);
        }

        [Fact]
        public void IteratorOverloading_Abstractness()
        {
            var source = @"
public abstract class C
{
    public abstract System.Collections.IEnumerable M(int x);

    // NB: We declare the interesting overload last so we know we're not
    // just picking the first one by mistake.
    public System.Collections.IEnumerable M()
    {
        yield return this;
    }
}";
            CheckIteratorOverloading(source, m => !m.IsAbstract);
        }

        [Fact]
        public void IteratorOverloading_Arity1()
        {
            var source = @"
public class C
{
    public System.Collections.IEnumerable M<T>(int x)
    {
        return null;
    }

    // NB: We declare the interesting overload last so we know we're not
    // just picking the first one by mistake.
    public System.Collections.IEnumerable M()
    {
        yield return this;
    }
}";
            CheckIteratorOverloading(source, m => m.Arity == 0);
        }

        [Fact]
        public void IteratorOverloading_Arity2()
        {
            var source = @"
public class C
{
    public System.Collections.IEnumerable M(int x)
    {
        return null;
    }

    // NB: We declare the interesting overload last so we know we're not
    // just picking the first one by mistake.
    public System.Collections.IEnumerable M<T>()
    {
        yield return this;
    }
}";
            CheckIteratorOverloading(source, m => m.Arity == 1);
        }

        [Fact]
        public void IteratorOverloading_Constraints1()
        {
            var source = @"
public class C
{
    public System.Collections.IEnumerable M<T>(int x)
        where T : struct
    {
        return null;
    }

    // NB: We declare the interesting overload last so we know we're not
    // just picking the first one by mistake.
    public System.Collections.IEnumerable M<T>()
        where T : class
    {
        yield return this;
    }
}";
            CheckIteratorOverloading(source, m => m.TypeParameters.Single().HasReferenceTypeConstraint);
        }

        [Fact]
        public void IteratorOverloading_Constraints2()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public System.Collections.IEnumerable M<T, U>(int x)
        where T : class
        where U : IEnumerable<T>
    {
        return null;
    }

    // NB: We declare the interesting overload last so we know we're not
    // just picking the first one by mistake.
    public System.Collections.IEnumerable M<T, U>()
        where U : class
        where T : IEnumerable<U>
    {
        yield return this;
    }
}";
            // NOTE: This isn't the feature we're switching on, but it is a convenient
            // differentiator.
            CheckIteratorOverloading(source, m => m.ParameterCount == 0);
        }

        private static void CheckIteratorOverloading(string source, Func<MethodSymbol, bool> isDesiredOverload)
        {
            var comp1 = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var ref1 = comp1.EmitToImageReference();
            var comp2 = CreateCompilationWithMscorlib("", new[] { ref1 }, options: TestOptions.DebugDll);

            var originalType = comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var iteratorMethod = originalType.GetMembers("M").OfType<MethodSymbol>().Single(isDesiredOverload);

            var stateMachineType = originalType.GetMembers().OfType<NamedTypeSymbol>().Single(t => GeneratedNames.GetKind(t.Name) == GeneratedNameKind.StateMachineType);
            var moveNextMethod = stateMachineType.GetMember<MethodSymbol>("MoveNext");

            var guessedIterator = CompilationContext.GetSubstitutedSourceMethod(moveNextMethod, sourceMethodMustBeInstance: true);
            Assert.Equal(iteratorMethod, guessedIterator.OriginalDefinition);
        }
    }
}
