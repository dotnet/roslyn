// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class HideByNameTests : CSharpTestBase
    {
        #region Methods

        [WorkItem(545796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545796")]
        [Fact]
        public void MethodOverloadResolutionHidesByNameStatic()
        {
            var il = @"
.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public static string Foo(string x) cil managed
  {
    ldarg.0
    ret
  }

} // end of class A

.class public auto ansi B
       extends A
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }

  .method public static void Foo(int32 x) cil managed
  {
    ret
  }

} // end of class B";

            var csharp = @"
class Program
{
    static void Main()
    {
        B.Foo("""");
    }
}";

            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics(
                // (6,15): error CS1503: Argument 1: cannot convert from 'string' to 'int'
                //         B.Foo("");
                Diagnostic(ErrorCode.ERR_BadArgType, @"""""").WithArguments("1", "string", "int"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void MethodOverloadResolutionHidesByNameInstance()
        {
            var il = @"
.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public instance string Foo(string x) cil managed
  {
    ldarg.0
    ret
  }

} // end of class A

.class public auto ansi B
       extends A
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }

  .method public instance void Foo(int32 x) cil managed
  {
    ret
  }

} // end of class B";

            var csharp = @"
class Program
{
    static void Main()
    {
        new B().Foo("""");
    }
}";

            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics(
                // (6,21): error CS1503: Argument 1: cannot convert from 'string' to 'int'
                //         new B().Foo("");
                Diagnostic(ErrorCode.ERR_BadArgType, @"""""").WithArguments("1", "string", "int"));
        }

        [Fact]
        public void MethodOverloadResolutionHidesByNameOverride()
        {
            var il = @"
.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  M(int32 x) cil managed
  {
    ret
  }

} // end of class A

.class public auto ansi B
       extends A
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }

  .method public virtual instance void 
          M(int32 x) cil managed
  {
    ret
  }

} // end of class B";

            var csharp = @"
public class C : B
{
    public override void M(int i)
    {
    }
}

class Program
{
    static void Main()
    {
        new B().M(1);
        new C().M(2);
    }
}";

            var comp = CreateCompilationWithCustomILSource(csharp, il);
            CompileAndVerify(comp).VerifyIL("Program.Main", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  newobj     ""B..ctor()""
  IL_0005:  ldc.i4.1
  IL_0006:  callvirt   ""void B.M(int)""
  IL_000b:  newobj     ""C..ctor()""
  IL_0010:  ldc.i4.2
  IL_0011:  callvirt   ""void B.M(int)""
  IL_0016:  ret
}");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void MethodOverloadResolutionHidesByNameParams()
        {
            var il = @"
.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  // params
  .method public hidebysig newslot virtual 
          instance void  M(int32[] a) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = {}
    ret
  }

} // end of class A

.class public auto ansi B
       extends A
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }

  // Not params
  .method public virtual instance void 
          M(int32[] a) cil managed
  {
    ret
  }

} // end of class B";

            var csharp = @"
class Program
{
    static void Main()
    {
        new B().M(1, 2); // This would work if B.M was not hide-by-name (since A.M is params)
    }
}";

            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics(
                // (6,9): error CS1501: No overload for method 'M' takes 2 arguments
                //         new B().M(1, 2); // This would work if B.M was not hide-by-name (since A.M is params)
                Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "2"));
        }

        [Fact]
        public void MethodOverridingHidesByName()
        {
            var il = @"
.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  M(int32 a) cil managed
  {
    ret
  }

} // end of class A

.class public auto ansi B
       extends A
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }

  .method public newslot virtual 
          instance void  M(int64 a) cil managed
  {
    ret
  }

} // end of class B";

            var csharp = @"
public class C : B
{
    public override void M(int a) { }
}";

            // NOTE: unlike overload resolution, override resolution does not respect hide-by-name.
            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics();
        }

        [Fact]
        public void MethodInterfaceImplementationHidesByName()
        {
            var il = @"
.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot virtual 
          instance void  M(int32 a) cil managed
  {
    ret
  }

} // end of class A

.class public auto ansi B
       extends A
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }

  .method public newslot virtual 
          instance void  M(int64 a) cil managed
  {
    ret
  }

} // end of class B";

            var csharp = @"
interface I
{
    void M(int a);
}

public class C : B, I
{
}";

            // NOTE: unlike overload resolution, implicit interface implementation resolution does not respect hide-by-name.
            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics();
        }

        #endregion Methods

        #region Indexers

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void IndexerOverloadResolutionHidesByNameInstance()
        {
            var il = @"
.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig newslot specialname virtual 
          instance int32  get_Item(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .property instance int32 Item(int32)
  {
    .get instance int32 A::get_Item(int32)
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class A

.class public auto ansi B
       extends A
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public newslot specialname virtual 
          instance int32  get_Item(string x) cil managed
  {
    ldc.i4.0
    ret
  }

  .property instance int32 Item(string)
  {
    .get instance int32 B::get_Item(string)
  }

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }
} // end of class B";

            var csharp = @"
class Program
{
    static void Main()
    {
        int x = new B()[0];
    }
}";

            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics(
                // (6,25): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         int x = new B()[0];
                Diagnostic(ErrorCode.ERR_BadArgType, "0").WithArguments("1", "int", "string"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void IndexerOverloadResolutionHidesByNameOverride()
        {
            var il = @"
.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig newslot specialname virtual 
          instance void  set_Item(int32 x, int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Item(int32)
  {
    .set instance void A::set_Item(int32, int32)
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class A

.class public auto ansi B
       extends A
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public specialname virtual 
          instance void  set_Item(int32 x, int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Item(int32)
  {
    .set instance void B::set_Item(int32, int32)
  }

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }
} // end of class B";

            var csharp = @"
public class C : B
{
    public override int this[int x] { set { } }
}

class Program
{
    static void Main()
    {
        new B()[1] = 2;
        new C()[2] = 1;
    }
}";

            var comp = CreateCompilationWithCustomILSource(csharp, il);
            CompileAndVerify(comp).VerifyIL("Program.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  newobj     ""B..ctor()""
  IL_0005:  ldc.i4.1
  IL_0006:  ldc.i4.2
  IL_0007:  callvirt   ""void B.this[int].set""
  IL_000c:  newobj     ""C..ctor()""
  IL_0011:  ldc.i4.2
  IL_0012:  ldc.i4.1
  IL_0013:  callvirt   ""void B.this[int].set""
  IL_0018:  ret
}");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void IndexerOverloadResolutionHidesByNameParams()
        {
            var il = @"
.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig newslot specialname virtual 
          instance int32  get_Item(int32[] x) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = {}
    ldc.i4.0
    ret
  }

  .property instance int32 Item(int32[])
  {
    .get instance int32 A::get_Item(int32[])
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class A

.class public auto ansi B
       extends A
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public specialname virtual 
          instance int32  get_Item(int32[] x) cil managed
  {
    ldc.i4.0
    ret
  }

  .property instance int32 Item(int32[])
  {
    .get instance int32 B::get_Item(int32[])
  }

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }
} // end of class B";

            var csharp = @"
class Program
{
    static void Main()
    {
        int x = new B()[1, 2]; // This would work if B.Item was not hide-by-name (since A.Item is params)
    }
}";

            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics(
                // (6,17): error CS1501: No overload for method 'this' takes 2 arguments
                //         int x = new B()[1, 2]; // This would work if B.Item was not hide-by-name (since A.Item is params)
                Diagnostic(ErrorCode.ERR_BadArgCount, "new B()[1, 2]").WithArguments("this", "2"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void IndexerOverridingHidesByName()
        {
            var il = @"
.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig newslot specialname virtual 
          instance void  set_Item(int32 x, int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Item(int32)
  {
    .set instance void A::set_Item(int32, int32)
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class A

.class public auto ansi B
       extends A
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public newslot specialname virtual 
          instance void  set_Item(int64 x, int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Item(int64)
  {
    .set instance void B::set_Item(int64, int32)
  }

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }
} // end of class B";

            var csharp = @"
public class C : B
{
    public override int this[int x] { set { } }
}";

            // NOTE: unlike overload resolution, override resolution does not respect hide-by-name.
            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics();
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void IndexerInterfaceImplementationHidesByName()
        {
            var il = @"
.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig newslot specialname virtual 
          instance void  set_Item(int32 x, int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Item(int32)
  {
    .set instance void A::set_Item(int32, int32)
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class A

.class public auto ansi B
       extends A
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public newslot specialname virtual 
          instance void  set_Item(int64 x, int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Item(int64)
  {
    .set instance void B::set_Item(int64, int32)
  }

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void A::.ctor()
    ret
  }
} // end of class B";

            var csharp = @"
interface I
{
    int this[int x] { set; }
}

public class C : B, I
{
}";

            // NOTE: unlike overload resolution, implicit interface implementation resolution does not respect hide-by-name.
            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics();
        }

        #endregion Indexers

        [Fact, WorkItem(897971, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/897971")]
        public void LocalHideFieldByName()
        {
            CreateCompilationWithMscorlib(@"
using System;

public class M
{
    int x;
    void P1(int[] xs)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            int x = xs[i];
            Console.WriteLine(x);
        }
        for (int i = 0; i < xs.Length; i++)
        {
            x = xs[i];
            Console.WriteLine(x);
        }
        x = xs.Length;
    }
}
").VerifyDiagnostics();

            CreateCompilationWithMscorlib(@"
using System;

public class M
{
    int x;
    public virtual void P2(int[] xs)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            for (int j = 0; j < xs.Length; j++)
            {
                int x = xs[j];
                Console.WriteLine(x);
            }
            x = xs.Length;
        }
    }
}
").VerifyDiagnostics();

            CreateCompilationWithMscorlib(@"
using System;

public class M
{
    int x;
    void P1(int[] xs)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            int x = xs[i];
            Console.WriteLine(x);
        }
        for (int i = 0; i < xs.Length; i++)
        {
            x = xs[i];
            Console.WriteLine(x);
        }
        this.x = xs.Length;
    }
    public virtual void P2(int[] xs)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            for (int j = 0; j < xs.Length; j++)
            {
                int x = xs[j];
                Console.WriteLine(x);
            }
            this.x = xs.Length;
        }
    }
}
").VerifyDiagnostics();
        }
    }
}
