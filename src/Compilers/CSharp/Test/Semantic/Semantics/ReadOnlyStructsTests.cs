// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class ReadOnlyStructsTests : CompilingTestBase
    {
        [Fact()]
        public void WriteableInstanceAutoPropsInRoStructs()
        {
            var text = @"
public readonly struct A
{
    // ok
    int ro => 5;

    // error
    int rw {get; set;}

    // ok
    static int rws {get; set;}
}
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (8,9): error CS8515: Instance fields of readonly structs must be readonly.
                //     int rw {get; set;}
                Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "rw").WithLocation(8, 9)
    );
        }

        [Fact()]
        public void WriteableInstanceFieldsInRoStructs()
        {
            var text = @"
public readonly struct A
{
    // ok
    public static int s;

    // ok
    public readonly int ro;

    // error
    int x;    

    void AssignField()
    {
        // error
        this.x = 1;

        A a = default;
        // OK
        a.x = 2;
    }
}
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (11,9): error CS8514: Auto-implemented instance properties in readonly structs must be readonly.
                //     int x;    
                Diagnostic(ErrorCode.ERR_FieldsInRoStruct, "x").WithLocation(11, 9),
                // (16,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this.x = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this.x").WithArguments("this").WithLocation(16, 9)
    );
        }

        [Fact()]
        public void EventsInRoStructs()
        {
            var text = @"
using System;

public readonly struct A : I1
{
    //error
    public event System.Action e;

    //error
    public event Action ei1;

    //ok
    public static event Action es;

    A(int arg)
    {
        // ok
        e = () => { };
        ei1 = () => { };
        es = () => { };
        
        // ok
        M1(ref e);
    }

    //ok
    event Action I1.ei2
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    void AssignEvent()
    {
        // error
        e = () => { };

        // error
        M1(ref e);
    }

    static void M1(ref System.Action arg)
    {
    }
}

interface I1
{
    event System.Action ei1;
    event System.Action ei2;
}
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (7,32): error CS8516: Field-like events are not allowed in readonly structs.
                //     public event System.Action e;
                Diagnostic(ErrorCode.ERR_FieldlikeEventsInRoStruct, "e").WithLocation(7, 32),
                // (10,25): error CS8516: Field-like events are not allowed in readonly structs.
                //     public event Action ei1;
                Diagnostic(ErrorCode.ERR_FieldlikeEventsInRoStruct, "ei1").WithLocation(10, 25),
                // (43,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         e = () => { };
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "e").WithArguments("this").WithLocation(43, 9),
                // (46,16): error CS1604: Cannot assign to 'this' because it is read-only
                //         M1(ref e);
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "e").WithArguments("this").WithLocation(46, 16)
    );
        }

        [Fact()]
        public void UseWriteableInstanceFieldsInRoStructs()
        {
            var il = @"
.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method EmbeddedAttribute::.ctor

} // end of class Microsoft.CodeAnalysis.EmbeddedAttribute

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method IsReadOnlyAttribute::.ctor

} // end of class System.Runtime.CompilerServices.IsReadOnlyAttribute



.class public sequential ansi sealed beforefieldinit S1
       extends [mscorlib]System.ValueType
{
  .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 

   // WRITEABLE FIELD!!!
  .field public int32 'field'

} // end of class S1

";

            var csharp = @"
public class Program 
{ 
    public static void Main() 
    { 
        S1 s = new S1();
        s.field = 123;
        System.Console.WriteLine(s.field);
    }
}
";

            var comp = CreateCompilationWithCustomILSource(csharp, il, options:TestOptions.ReleaseExe);
            
            comp.VerifyDiagnostics();
            
            CompileAndVerify(comp, expectedOutput:"123");
        }

    }
}
