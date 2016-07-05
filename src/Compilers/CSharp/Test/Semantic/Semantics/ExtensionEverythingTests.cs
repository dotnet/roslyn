// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ExtensionEverything)]
    public class ExtensionEverythingTests : CompilingTestBase
    {
        private static readonly CSharpParseOptions parseOptions = TestOptions.Regular.WithExtensionEverythingFeature();
        private static readonly MetadataReference[] additionalRefs = new[] { SystemCoreRef };

        // PROTOTYPE: Test method group containing both old-style ext method and new-style.
        // PROTOTYPE: Extension method query invocation
        // PROTOTYPE: Call with receiver going through boxing as well as implicit reference conversion (and also reject invalid conversions)
        // PROTOTYPE: Overloaded (non-)ambiguous methods, properties, etc. - this is a working issue, lots of ambiguous cases to test.
        // PROTOTYPE: Generics - working issue, there's a lot of cases here.
        //   Specifically, two-phase type inference is not implemented
        // PROTOTYPE: default(string).ExtProp should not emit WRN_DotOnDefault
        // PROTOTYPE: Define indexer on array
        // PROTOTYPE: using static on ext class

        [Fact]
        public void BasicFunctionality()
        {
            var text = @"
class BaseClass
{
}

extension class ExtClass : BaseClass
{
    public void Ext()
    {
        System.Console.WriteLine(""Hello, world!"");
    }
}

class Program
{
    static void Main(string[] args)
    {
        new BaseClass().Ext();
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "Hello, world!",
                parseOptions: parseOptions);
        }

        [Fact]
        public void VariousMemberKinds()
        {
            var text = @"
using System;

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    public int ExtMethod()
    {
        return 2;
    }
    public int ExtProp
    {
        get { return 2; }
        set { Console.Write(value); }
    }
    public static int ExtStaticMethod()
    {
        return 2;
    }
    public static int ExtStaticProp
    {
        get { return 2; }
        set { Console.Write(value); }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var obj = new BaseClass();
        Console.Write(obj.ExtMethod());
        Console.Write(obj.ExtProp);
        obj.ExtProp = 2;
        Console.Write(BaseClass.ExtStaticMethod());
        Console.Write(BaseClass.ExtStaticProp);
        BaseClass.ExtStaticProp = 2;
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "222222",
                parseOptions: parseOptions)
                .VerifyIL("Program.Main", @"{
  // Code size       60 (0x3c)
  .maxstack  2
  IL_0000:  newobj     ""BaseClass..ctor()""
  IL_0005:  dup
  IL_0006:  call       ""int ExtClass.ExtMethod(BaseClass)""
  IL_000b:  call       ""void System.Console.Write(int)""
  IL_0010:  dup
  IL_0011:  call       ""int ExtClass.get_ExtProp(BaseClass)""
  IL_0016:  call       ""void System.Console.Write(int)""
  IL_001b:  ldc.i4.2
  IL_001c:  call       ""void ExtClass.set_ExtProp(BaseClass, int)""
  IL_0021:  call       ""int ExtClass.ExtStaticMethod()""
  IL_0026:  call       ""void System.Console.Write(int)""
  IL_002b:  call       ""int ExtClass.ExtStaticProp.get""
  IL_0030:  call       ""void System.Console.Write(int)""
  IL_0035:  ldc.i4.2
  IL_0036:  call       ""void ExtClass.ExtStaticProp.set""
  IL_003b:  ret
}");
        }

        // PROTOTYPE: Once the VariousExtendedKinds() test is unskipped and implementation fixed, this test is redundant
        [Fact]
        public void VariousExtendedKindsRestricted()
        {
            var text = @"
using System;

class BaseClass
{
}

static class BaseStaticClass
{
}

class BaseStruct
{
}

class IBaseInterface
{
}

enum BaseEnum
{
}

extension class ExtClass : BaseClass
{
    public void MemberClass() { Console.Write(1); }
    public static void StaticMemberClass() { Console.Write(5); }
    public static void DirectCallClass() { Console.Write('a'); }
}

extension class ExtStaticClass : BaseStaticClass
{
    public static void StaticMemberStaticClass() { Console.Write(6); }
    public static void DirectCallStaticClass() { Console.Write('b'); }
}

extension class ExtStruct : BaseStruct
{
    public void MemberStruct() { Console.Write(2); }
    public static void StaticMemberStruct() { Console.Write(7); }
    public static void DirectCallStruct() { Console.Write('c'); }
}

extension class ExtInterface : IBaseInterface
{
    public void MemberInterface() { Console.Write(3); }
    public static void StaticMemberInterface() { Console.Write(8); }
    public static void DirectCallInterface() { Console.Write('d'); }
}

extension class ExtEnum : BaseEnum
{
    public void MemberEnum() { Console.Write(4); }
    public static void StaticMemberEnum() { Console.Write(9); }
    public static void DirectCallEnum() { Console.Write('e'); }
}

class Program
{
    static void Main(string[] args)
    {
        BaseClass obj1 = default(BaseClass);
        BaseStruct obj2 = default(BaseStruct);
        IBaseInterface obj3 = default(IBaseInterface);
        BaseEnum obj4 = default(BaseEnum);
        obj1.MemberClass();
        obj2.MemberStruct();
        obj3.MemberInterface();
        obj4.MemberEnum();
        BaseClass.StaticMemberClass();
        BaseStaticClass.StaticMemberStaticClass();
        BaseStruct.StaticMemberStruct();
        IBaseInterface.StaticMemberInterface();
        BaseEnum.StaticMemberEnum();
        ExtClass.DirectCallClass();
        ExtStaticClass.DirectCallStaticClass();
        ExtStruct.DirectCallStruct();
        ExtInterface.DirectCallInterface();
        ExtEnum.DirectCallEnum();
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "123456789abcde",
                parseOptions: parseOptions)
                .VerifyIL("Program.Main", @"{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (BaseStruct V_0, //obj2
                IBaseInterface V_1, //obj3
                BaseEnum V_2) //obj4
  IL_0000:  ldnull
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldnull
  IL_0004:  stloc.1
  IL_0005:  ldc.i4.0
  IL_0006:  stloc.2
  IL_0007:  call       ""void ExtClass.MemberClass(BaseClass)""
  IL_000c:  ldloc.0
  IL_000d:  call       ""void ExtStruct.MemberStruct(BaseStruct)""
  IL_0012:  ldloc.1
  IL_0013:  call       ""void ExtInterface.MemberInterface(IBaseInterface)""
  IL_0018:  ldloc.2
  IL_0019:  call       ""void ExtEnum.MemberEnum(BaseEnum)""
  IL_001e:  call       ""void ExtClass.StaticMemberClass()""
  IL_0023:  call       ""void ExtStaticClass.StaticMemberStaticClass()""
  IL_0028:  call       ""void ExtStruct.StaticMemberStruct()""
  IL_002d:  call       ""void ExtInterface.StaticMemberInterface()""
  IL_0032:  call       ""void ExtEnum.StaticMemberEnum()""
  IL_0037:  call       ""void ExtClass.DirectCallClass()""
  IL_003c:  call       ""void ExtStaticClass.DirectCallStaticClass()""
  IL_0041:  call       ""void ExtStruct.DirectCallStruct()""
  IL_0046:  call       ""void ExtInterface.DirectCallInterface()""
  IL_004b:  call       ""void ExtEnum.DirectCallEnum()""
  IL_0050:  ret
}");
        }

        [Fact(Skip = "PROTOTYPE: Need to implement ambiguity resolution between static extension members that are extending different types")]
        public void VariousExtendedKinds()
        {
            var text = @"
using System;

class BaseClass
{
}

static class BaseStaticClass
{
}

class BaseStruct
{
}

class IBaseInterface
{
}

enum BaseEnum
{
}

extension class ExtClass : BaseClass
{
    public void Member() { Console.Write(1); }
    public static void StaticMember() { Console.Write(5); }
    public static void DirectCall() { Console.Write('a'); }
}

extension class ExtStaticClass : BaseStaticClass
{
    public static void StaticMember() { Console.Write(6); }
    public static void DirectCall() { Console.Write('b'); }
}

extension class ExtStruct : BaseStruct
{
    public void Member() { Console.Write(2); }
    public static void StaticMember() { Console.Write(7); }
    public static void DirectCall() { Console.Write('c'); }
}

extension class ExtInterface : IBaseInterface
{
    public void Member() { Console.Write(3); }
    public static void StaticMember() { Console.Write(8); }
    public static void DirectCall() { Console.Write('d'); }
}

extension class ExtEnum : BaseEnum
{
    public void Member() { Console.Write(4); }
    public static void StaticMember() { Console.Write(9); }
    public static void DirectCall() { Console.Write('e'); }
}

class Program
{
    static void Main(string[] args)
    {
        BaseClass obj1 = default(BaseClass);
        BaseStruct obj2 = default(BaseStruct);
        IBaseInterface obj3 = default(IBaseInterface);
        BaseEnum obj4 = default(BaseEnum);
        obj1.Member();
        obj2.Member();
        obj3.Member();
        obj4.Member();
        BaseClass.StaticMember();
        BaseStaticClass.StaticMember();
        BaseStruct.StaticMember();
        IBaseInterface.StaticMember();
        BaseEnum.StaticMember();
        ExtClass.DirectCall();
        ExtStaticClass.DirectCall();
        ExtStruct.DirectCall();
        ExtInterface.DirectCall();
        ExtEnum.DirectCall();
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "123456789abcde",
                parseOptions: parseOptions)
                .VerifyIL("Program.Main", @"{
}");
        }

        [Fact]
        public void UseOfThisBasic()
        {
            var text = @"
using System;

class BaseClass
{
    int x;
    public BaseClass()
    {
        x = 1;
    }
    public int MethodInstance() => x;
}

extension class ExtClass : BaseClass
{
    public int MethodInstanceExt() => this.MethodInstance() + 1;
}

class Program
{
    static void Main()
    {
        BaseClass obj = new BaseClass();
        Console.Write(obj.MethodInstance());
        Console.Write(obj.MethodInstanceExt());
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "12",
                parseOptions: parseOptions);
        }

        [Fact(Skip = "PROTOTYPE: Produces CS0535, extension class doesn't implement interface (same skip reason as VariousExtendedKindsRestricted)")]
        public void UseOfThis()
        {
            var text = @"
using System;

interface IInterface
{
    int MethodInterface();
}

class BaseClass : IInterface
{
    int x;
    public BaseClass()
    {
        x = 1;
    }
    public int MethodInstance() => x;
    int IInterface.MethodInterface() => x + 1;
}

extension class ExtInterface : IInterface
{
    public int MethodOtherExtInferace() => MethodInterface() + 3;
}

extension class ExtClass : BaseClass
{
    public int MethodInstanceExt() => MethodInstance() + 2;
    public int MethodInterfaceExt() => ((IInterface)this).MethodInterface() + 2;
}

class Program
{
    static void Main()
    {
        BaseClass obj = new BaseClass();
        Console.Write(obj.MethodInstance());
        Console.Write(((IInterface)obj).MethodInterface());
        Console.Write(obj.MethodInstanceExt());
        Console.Write(obj.MethodInterfaceExt());
        Console.Write(obj.MethodOtherExtInferace());
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "12345",
                parseOptions: parseOptions);
        }

        [Fact]
        public void DuckDiscovery()
        {
            var text = @"
using System;
using System.Threading.Tasks;

// Add() invocation requires that the class implements IEnumerable (even though it doesn't use it)
class BaseEnumerable : System.Collections.IEnumerable
{
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
}

class BaseClass
{
    public override string ToString() => ""3"";
}

class BaseEnumerator
{
    public bool accessed;
}

class BaseAwaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    void System.Runtime.CompilerServices.INotifyCompletion.OnCompleted(Action action) => action();
    public int GetResult() => 5; // PROTOTYPE: this method is not allowed to be an extension method, for some reason.
}

extension class ExtEnumerable : BaseEnumerable
{
    public void Add(int x) => Console.Write(x);
}

extension class ExtClass : BaseClass
{
    public void Add(int x) => Console.Write(x);
    // need new to hide *ExtClass*'s ToString method. Probably wrong to require it?
    public new string ToString() => ""wrong""; // should never get called, as object.ToString always wins (it's a real member)
    public BaseEnumerator GetEnumerator() => new BaseEnumerator();
    public BaseAwaiter GetAwaiter() => new BaseAwaiter();
}

extension class ExtEnumerator : BaseEnumerator
{
    public int Current => 4;
    public bool MoveNext()
    {
        // PROTOTYPE: test without 'this'?
        var temp = this.accessed;
        this.accessed = true;
        return !temp;
    }
    public void Dispose() { }
    public void Reset() { }
}

extension class ExtAwaiter : BaseAwaiter
{
    public bool IsCompleted => true;
    public void OnCompleted(Action action) => action();
}

class Program
{
    static async Task<int> Async(BaseClass obj)
    {
        return await obj;
    }
    static void Main(string[] args)
    {
        new BaseEnumerable { 1, 2 };
        var obj = new BaseClass();
        Console.Write(obj.ToString());
        // PROTOTYPE: Decide if we want extension foreach. (Old extension methods do not work)
        //foreach (var item in obj)
        //{
        //    Console.Write(item);
        //}
        Console.Write(Async(obj).Result);
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs.Concat(new[] { MscorlibRef_v46 }),
                expectedOutput: "1235",
                parseOptions: parseOptions);
        }

        [Fact]
        public void Params()
        {
            var text = @"
using System;

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    public void Method(params int[] arr) => Console.Write(string.Join("""", arr));
    public static void StaticMethod(params int[] arr) => Console.Write(string.Join("""", arr));
    public string this[params int[] arr] => string.Join("""", arr);
}

class Program
{
    static void Main(string[] args)
    {
        var obj = default(BaseClass);
        obj.Method(1, 2);
        BaseClass.StaticMethod(3, 4);
        Console.Write(obj[5, 6]);
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs.Concat(new[] { MscorlibRef_v46 }),
                expectedOutput: "123456",
                parseOptions: parseOptions)
                .VerifyIL("Program.Main", @"{
  // Code size       67 (0x43)
  .maxstack  5
  .locals init (BaseClass V_0) //obj
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.2
  IL_0004:  newarr     ""int""
  IL_0009:  dup
  IL_000a:  ldc.i4.0
  IL_000b:  ldc.i4.1
  IL_000c:  stelem.i4
  IL_000d:  dup
  IL_000e:  ldc.i4.1
  IL_000f:  ldc.i4.2
  IL_0010:  stelem.i4
  IL_0011:  call       ""void ExtClass.Method(BaseClass, params int[])""
  IL_0016:  ldc.i4.2
  IL_0017:  newarr     ""int""
  IL_001c:  dup
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.3
  IL_001f:  stelem.i4
  IL_0020:  dup
  IL_0021:  ldc.i4.1
  IL_0022:  ldc.i4.4
  IL_0023:  stelem.i4
  IL_0024:  call       ""void ExtClass.StaticMethod(params int[])""
  IL_0029:  ldloc.0
  IL_002a:  ldc.i4.2
  IL_002b:  newarr     ""int""
  IL_0030:  dup
  IL_0031:  ldc.i4.0
  IL_0032:  ldc.i4.5
  IL_0033:  stelem.i4
  IL_0034:  dup
  IL_0035:  ldc.i4.1
  IL_0036:  ldc.i4.6
  IL_0037:  stelem.i4
  IL_0038:  call       ""string ExtClass.get_Item(BaseClass, params int[])""
  IL_003d:  call       ""void System.Console.Write(string)""
  IL_0042:  ret
}");
        }

        [Fact]
        public void Delegate()
        {
            var text = @"
using System;

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    public void Method(int x) => Console.Write(x);
    public static void StaticMethod(int x) => Console.Write(x);
}

class Program
{
    static void Main(string[] args)
    {
        BaseClass obj = new BaseClass();
        Action<int> method = obj.Method;
        method(1);
        Action<int> staticMethod = BaseClass.StaticMethod;
        staticMethod(2);
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "12",
                parseOptions: parseOptions)
                .VerifyIL("Program.Main", @"{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  newobj     ""BaseClass..ctor()""
  IL_0005:  ldftn      ""void ExtClass.Method(BaseClass, int)""
  IL_000b:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0010:  ldc.i4.1
  IL_0011:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0016:  ldnull
  IL_0017:  ldftn      ""void ExtClass.StaticMethod(int)""
  IL_001d:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0022:  ldc.i4.2
  IL_0023:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0028:  ret
}");
        }

        [Fact]
        public void Using()
        {
            var text = @"
using System;

namespace One
{
    class BaseClass
    {
    }
}

extension class ExtGlobal : One.BaseClass
{
    public int Global => 3;
}

namespace Two
{
    using One;

    extension class ExtClass : BaseClass
    {
        public void Method() => Console.Write(1);
    }
}

namespace Prog
{
    using Two;
    class Program
    {
        static void Main()
        {
            var thing = new One.BaseClass();
            thing.Method();
            Four.FourClass.Test();
            Console.Write(thing.Global + 1);
        }
    }
}
";
            var text2 = @"
using System;

namespace Three
{
    using One;

    extension class ExtClass : BaseClass
    {
        public void Method() => Console.Write(2);
    }
}

namespace Four
{
    using Three;
    public class FourClass
    {
        public static void Test()
        {
            var thing = new One.BaseClass();
            thing.Method();
            Console.Write(thing.Global);
        }
    }
}
";

            CompileAndVerify(
                sources: new[] { text, text2 },
                additionalRefs: additionalRefs,
                expectedOutput: "1234",
                parseOptions: parseOptions);
        }

        [Fact]
        public void AmbiguityPriority()
        {
            var text = @"
using System;

class BaseClass
{
    public int PropertyThing { get { return 1; } set { Console.Write(value); } }
    public static int StaticProperty { get { return 5; } set { Console.Write(value); } }
}

extension class ExtClass : BaseClass
{
    public int PropertyThing { get { return 2; } set { Console.Write(value + 1); } }
    public static int StaticProperty { get { return 6; } set { Console.Write(value + 1); } }
}

interface IBase
{
}

interface IDerived : IBase
{
}

// these are all named separate things, because if they were named the same (and defined in separate ext classes),
// they would be ambigious from each other - as well as ambigious between ExtIBase and ExtIDerived.
// (but the base/derived is resolvable and what we're testing, same name is not resolveable)
extension class ExtIBase : IBase
{
    public string Property => ""wrong"";
    public static string StaticProperty => ""wrong"";
    public string Method() => ""wrong"";
    public static string StaticMethod() => ""wrong"";
}

extension class ExtIDerived : IDerived
{
    public string Property => ""9"";
    public static string StaticProperty => ""a"";
    public string Method() => ""b"";
    public static string StaticMethod() => ""c"";
}

static class StaticBaseOne { }
static class StaticBaseTwo { }

extension class StaticExtOne : StaticBaseOne
{
    public static string StaticExtension() => ""d"";
}
extension class StaticExtTwo : StaticBaseTwo
{
    public static string StaticExtension() => ""wrong"";
}

struct StructObj { }
extension class ExtStruct : StructObj
{
    public string StructObjMethod() => ""e"";
}
extension class ExtStructObj : object
{
    public string StructObjMethod() => ""wrong"";
}

class Program
{
    static void Main()
    {
        BaseClass obj = new BaseClass();
        Console.Write(obj.PropertyThing);
        Console.Write(2); // PROTOTYPE: figure out syntax to get ExtClass.Property
        obj.PropertyThing = 3;
        Console.Write(4); // PROTOTYPE: figure out syntax to set ExtClass.Property
        Console.Write(BaseClass.StaticProperty);
        Console.Write(ExtClass.StaticProperty);
        BaseClass.StaticProperty = 7;
        ExtClass.StaticProperty = 7;
        IDerived iDerived = null;
        // PROTOTYPE: Betterness not implemented for property overload resolution
        //Console.Write(iDerived.Property);
        // PROTOTYPE: LookupExtensionMembersInSingleBinder does not implement viability for multiple things named the same
        //Console.Write(IDerived.StaticProperty);
        Console.Write(iDerived.Method());
        Console.Write(IDerived.StaticMethod());
        // PROTOTYPE: Same thing
        //Console.Write(StaticBaseOne.StaticExtension());
        var structobj = new StructObj();
        Console.Write(structobj.StructObjMethod());
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "12345678bce",
                parseOptions: parseOptions);
        }

        [Fact(Skip = "PROTOTYPE: Fails due to ReducedExtension not disambiguating based on receiver.")]
        public void ArgumentOrdering()
        {
            // error CS0121: The call is ambiguous between the following methods or properties: 'IQueryable<int>.Sum()' and 'IQueryable<int?>.Sum()'
            var text = @"
using System;
using System.Linq;
using static EffectLogger;

class EffectLogger
{
    public static T Log<T>(T value, string log)
    {
        Console.Write(log);
        return value;
    }
}

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    public int this[int a, int b = 4, params int[] c]
    {
        get
        {
            return Log(a + b + c.Sum(), ""["" + a + b + string.Join("""", c) + ""]"");
        }
        set
        {
            Console.Write(""["" + a + b + string.Join("""", c) + ""]="" + value);
        }
    }
    public int Func(int a, int b = 4, params int[] c) =>
        Log(a + b + c.Sum(), ""("" + a + b + string.Join("","", c) + "")"");
    public static int StaticFunc(int a, int b = 4, params int[] c) =>
        Log(a + b + c.Sum(), ""{"" + a + b + string.Join("","", c) + ""}"");
}

class Program
{
    static void Main()
    {
        Console.Write(Log(new BaseClass(), ""1"")[c: new[] { Log(1, ""2""), Log(2, ""3"") }, a: Log(3, ""4"")]);
        Console.Write(Log(new BaseClass(), ""1"").Func(c: new[] { Log(1, ""2""), Log(2, ""3"") }, a: Log(3, ""4"")));
        Console.Write(BaseClass.StaticFunc(c: new[] { Log(1, ""1""), Log(2, ""2"") }, a: Log(3, ""3"")));
        Log(new BaseClass(), ""1"")[c: new[] { Log(1, ""2""), Log(2, ""3"") }, a: Log(3, ""4"")] = 5;
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "1234[3412]1234(3412)1234{3412}1234[3412]=5",
                parseOptions: parseOptions);
        }

        [Fact]
        public void ExpressionLambda()
        {
            var text = @"
using System;
using System.Linq.Expressions;

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    // expr trees don't allow assignment, so don't bother making setters.
    public int Method() => 2;
    public static int MethodStatic() => 2;
    public int Prop => 2;
    public static int PropStatic => 2;
    public int this[int x] => x;
}

class Program
{
    static void Main()
    {
        Expression<Func<BaseClass, int>> expr1 = obj => obj.Method();
        Expression<Func<BaseClass, int>> expr2 = obj => BaseClass.MethodStatic();
        Expression<Func<BaseClass, int>> expr3 = obj => obj.Prop;
        Expression<Func<BaseClass, int>> expr4 = obj => BaseClass.PropStatic;
        Expression<Func<BaseClass, int>> expr5 = obj => obj[2];
        Console.WriteLine(expr1);
        Console.WriteLine(expr2);
        Console.WriteLine(expr3);
        Console.WriteLine(expr4);
        Console.WriteLine(expr5);
    }
}
";

            // PROTOTYPE: This is just how it fell out of existing code. Probably want to change it.
            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: @"
obj => Method(obj)
obj => MethodStatic()
obj => get_Prop(obj)
obj => ExtClass.PropStatic
obj => get_Item(obj, 2)",
                parseOptions: parseOptions);
        }

        [Fact]
        public void EntryPointExtension()
        {
            var text = @"
using System;

class BaseClass
{
}

extension class Program : BaseClass
{
    static void Main()
    {
        Console.Write(""Hello, world!"");
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "Hello, world!",
                parseOptions: parseOptions);
        }

        [Fact(Skip = "PROTOTYPE: Receiver type inference doesn't work")]
        public void SimpleGeneric()
        {
            var text = @"
using System;

class BaseClass<T>
{
    public string Thing(T t) => t.ToString();
}

extension class ExtClass<T> : BaseClass<T>
{
    public T Id1(T t) => t;
    public T2 Id2<T2>(T2 t) => t;
    public string Id3(T t) => this.Thing(t);
}

static class Program
{
    static void Main()
    {
        var obj = new BaseClass<int>();
        Console.Write(obj.Id1(1));
        Console.Write(obj.Id2(2));
        Console.Write(obj.Id3(3));
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "123",
                parseOptions: parseOptions);
        }

        [Fact(Skip = "PROTOTYPE: Fails because boxing conversion not emitted")]
        public void Conversions()
        {
            var text = @"
using System;

interface BaseInterface { }
class BaseClass { }
class SomeClass : BaseClass, BaseInterface { }

extension class ExtClass : BaseClass
{
    public void MethodClass() => Console.Write(1);
}

extension class ExtInterface : BaseInterface
{
    public void MethodInterface() => Console.Write(2);
}

struct SomeStruct { }

extension class ExtObject : object
{
    public void MethodObject() => Console.Write(3);
}

struct SomeStructInterface : BaseInterface { }

static class Program
{
    static void Main()
    {
        var someClass = new SomeClass();
        someClass.MethodClass();
        someClass.MethodInterface();
        var someStruct = new SomeStruct();
        someStruct.MethodObject();
        var someStructInterface = new SomeStructInterface();
        someStructInterface.MethodInterface();
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "1232",
                parseOptions: parseOptions);
        }

        [Fact]
        public void DllImportExtern()
        {
            var text = @"
using System;
using System.Runtime.InteropServices;

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    [DllImport(""bogusAssembly"")]
    public static extern void ExternMethod();
}

class Program
{
    static void Main()
    {
        try
        {
            BaseClass.ExternMethod();
        }
        catch (System.DllNotFoundException)
        {
            Console.Write(1);
        }
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "1",
                parseOptions: parseOptions);
        }

        [Fact]
        public void COMClass()
        {
            var text = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""bb29cd77-7fc8-42c3-94ed-be985450be09"")]
public class BaseClassCOM
{
}

extension class ExtClass : BaseClassCOM
{
    public void Member() { Console.Write(1); }
    public int Property => 2; // there was strange things with properties in particular
    public static void StaticMember() { Console.Write(3); }
}

class Program
{
    static void Main(string[] args)
    {
        BaseClassCOM obj = default(BaseClassCOM);
        obj.Member();
        Console.Write(obj.Property);
        BaseClassCOM.StaticMember();
    }
}
";

            CompileAndVerify(
                source: text,
                additionalRefs: additionalRefs,
                expectedOutput: "123",
                parseOptions: parseOptions)
                .VerifyIL("Program.Main", @"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  dup
  IL_0002:  call       ""void ExtClass.Member(BaseClassCOM)""
  IL_0007:  call       ""int ExtClass.get_Property(BaseClassCOM)""
  IL_000c:  call       ""void System.Console.Write(int)""
  IL_0011:  call       ""void ExtClass.StaticMember()""
  IL_0016:  ret
}");
        }

        [Fact]
        public void ExtensionMethodInExtensionClass()
        {
            var text = @"
class Base
{
}

extension class Ext : Base {
    public static void ExtMethod(this Base param)
    {
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions).VerifyDiagnostics(
                // (7,24): error CS8207: An extension method cannot be defined in an extension class.
                //     public static void ExtMethod(this Base param)
                Diagnostic(ErrorCode.ERR_ExtensionMethodInExtensionClass, "ExtMethod").WithLocation(7, 24)
            );
        }

        [Fact(Skip = "PROTOTYPE: Static class extensions are not implemented yet")]
        public void InstanceInStaticExtension()
        {
            var text = @"
static class Base
{
}

extension class Ext : Base {
    public void ExtMethod() { }
}
";

            // PROTOTYPE: Wrong error, but there's no error message yet so this is just to keep the test failing
            CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions).VerifyDiagnostics(
                // (7,24): error CS8207: An extension method cannot be defined in an extension class.
                //     public static void ExtMethod(this Base param)
                Diagnostic(ErrorCode.ERR_ExtensionMethodInExtensionClass, "ExtMethod").WithLocation(7, 24)
            );
        }

        [Fact(Skip = "PROTOTYPE: ExtExt and ExtNothing do not produce diagnostics")]
        public void IncorrectExtendedType()
        {
            var text = @"
struct Base { }

extension class ExtNothing { }

unsafe extension class ExtPointer : Base* { }

extension class ExtArray : Base[] { }

extension class ExtTypeParam<T> : T { }

extension class ExtDynamic : dynamic { }

extension class ExtBase : Base { } // this is valid

extension class ExtExt : ExtBase { }

delegate void Del();

extension class ExtDelegate : Del { }
";

            // PROTOTYPE: Fix these error messages. Also make ExtNothing and ExtExt and ExtDelegate produce errors (they are not in the below list)
            CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.UnsafeReleaseDll, parseOptions: parseOptions).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadBaseType, "ExtExt").WithLocation(1, 1),
                Diagnostic(ErrorCode.ERR_BadBaseType, "ExtNothing").WithLocation(1, 1),

                // (6,37): error CS1521: Invalid base type
                // unsafe extension class ExtPointer : Base* { }
                Diagnostic(ErrorCode.ERR_BadBaseType, "Base*").WithLocation(6, 37),
                // (8,28): error CS1521: Invalid base type
                // extension class ExtArray : Base[] { }
                Diagnostic(ErrorCode.ERR_BadBaseType, "Base[]").WithLocation(8, 28),
                // (6,37): error CS0527: Type 'Base*' in interface list is not an interface
                // unsafe extension class ExtPointer : Base* { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Base*").WithArguments("Base*").WithLocation(6, 37),
                // (8,28): error CS0527: Type 'Base[]' in interface list is not an interface
                // extension class ExtArray : Base[] { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Base[]").WithArguments("Base[]").WithLocation(8, 28),
                // (10,35): error CS0689: Cannot derive from 'T' because it is a type parameter
                // extension class ExtTypeParam<T> : T { }
                Diagnostic(ErrorCode.ERR_DerivingFromATyVar, "T").WithArguments("T").WithLocation(10, 35),
                // (12,30): error CS1965: 'ExtDynamic': cannot derive from the dynamic type
                // extension class ExtDynamic : dynamic { }
                Diagnostic(ErrorCode.ERR_DeriveFromDynamic, "dynamic").WithArguments("ExtDynamic").WithLocation(12, 30),
                // (20,17): error CS0509: 'ExtDelegate': cannot derive from sealed type 'Del'
                // extension class ExtDelegate : Del { }
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "ExtDelegate").WithArguments("ExtDelegate", "Del").WithLocation(20, 17)
            );
        }

        [Fact]
        public void Circular()
        {
            var text = @"
using System.Collections.Generic;

extension class SimpleA : SimpleB { }
extension class SimpleB : SimpleA { }

extension class A : B { }
extension class B : C[] { }
extension class C : List<A> { }

class Program
{
    static void Main()
    {
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions).VerifyDiagnostics(
                // (4,17): error CS0103: The name 'SimpleB' does not exist in the current context
                // extension class SimpleA : SimpleB { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "SimpleA").WithArguments("SimpleB").WithLocation(4, 17),
                // (7,17): error CS0103: The name 'B' does not exist in the current context
                // extension class A : B { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("B").WithLocation(7, 17),
                // (5,17): error CS0103: The name 'SimpleA' does not exist in the current context
                // extension class SimpleB : SimpleA { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "SimpleB").WithArguments("SimpleA").WithLocation(5, 17),
                // (8,21): error CS0719: 'C': array elements cannot be of static type
                // extension class B : C[] { }
                Diagnostic(ErrorCode.ERR_ArrayOfStaticClass, "C").WithArguments("C").WithLocation(8, 21),
                // (9,26): error CS0718: 'A': static types cannot be used as type arguments
                // extension class C : List<A> { }
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "A").WithArguments("A").WithLocation(9, 26)
            );
        }

        // PROTOTYPE: Overloaded (non-)ambiguous methods, properties, etc.
        [Fact]
        public void Ambiguous()
        {
            var text = @"
using System;

class BaseClass { }

extension class ExtOne : BaseClass
{
    public int Method() => 2;
    public static int MethodStatic() => 2;
    public int Prop
    {
        get { return 2; }
        set { Console.Write(value); }
    }
    public static int PropStatic
    {
        get { return 2; }
        set { Console.Write(value); }
    }
    public int this[int x] => 2;
}

extension class ExtTwo : BaseClass
{
    public int Method() => 2;
    public static int MethodStatic() => 2;
    public int Prop
    {
        get { return 2; }
        set { Console.Write(value); }
    }
    public static int PropStatic
    {
        get { return 2; }
        set { Console.Write(value); }
    }
    public int this[int x] => 2;
}

class Program
{
    static void Main()
    {
        BaseClass obj = null;
        obj.Method();
        BaseClass.MethodStatic();
        Console.Write(obj.Prop);
        Console.Write(BaseClass.PropStatic);
        Console.Write(obj[2]);
    }
}
";

            // PROTOTYPE: The display string for instance extension members is a little weird.
            CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions).VerifyDiagnostics(
                // (45,13): error CS0121: The call is ambiguous between the following methods or properties: 'ExtOne.Method()' and 'ExtTwo.Method()'
                //         obj.Method();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("ExtOne.Method()", "ExtTwo.Method()").WithLocation(45, 13),
                // (46,19): error CS0121: The call is ambiguous between the following methods or properties: 'ExtOne.MethodStatic()' and 'ExtTwo.MethodStatic()'
                //         BaseClass.MethodStatic();
                Diagnostic(ErrorCode.ERR_AmbigCall, "MethodStatic").WithArguments("ExtOne.MethodStatic()", "ExtTwo.MethodStatic()").WithLocation(46, 19),
                // (47,27): error CS0229: Ambiguity between 'ExtOne.Prop' and 'ExtTwo.Prop'
                //         Console.Write(obj.Prop);
                Diagnostic(ErrorCode.ERR_AmbigMember, "Prop").WithArguments("ExtOne.Prop", "ExtTwo.Prop").WithLocation(47, 27),
                // (48,33): error CS0229: Ambiguity between 'ExtOne.PropStatic' and 'ExtTwo.PropStatic'
                //         Console.Write(BaseClass.PropStatic);
                Diagnostic(ErrorCode.ERR_AmbigMember, "PropStatic").WithArguments("ExtOne.PropStatic", "ExtTwo.PropStatic").WithLocation(48, 33),
                // (49,23): error CS0121: The call is ambiguous between the following methods or properties: 'ExtOne.this[int]' and 'ExtTwo.this[int]'
                //         Console.Write(obj[2]);
                Diagnostic(ErrorCode.ERR_AmbigCall, "obj[2]").WithArguments("ExtOne.this[int]", "ExtTwo.this[int]").WithLocation(49, 23)
            );
        }

        // This test is essentially just testing all the places in the compiler where the TypeKind enum is switched over, or otherwise checked.
        // It's useful because those points are usually where bugs are easy, since the exact kind of type is important at those points - so
        // extension classes usually have behavior that needs to be explicitly accounted for (and easily forgotten).
        [Fact]
        public void InvalidUses()
        {
            // PROTOTYPE: Todo, does ToTypeKind have to be modified? PENamedTypeSymbol.TypeKind? Also MightContainExtensionMethods, MakeModifiers
            // PROTOTYPE: Binder.AddMemberLookupSymbolsInfoInType enumerates TypeKind, but isn't tested here (not sure what it does or how to test it)
            var text = @"
using System;
using System.Collections;

[assembly: CLSCompliant(true)]

class BaseClass { }

extension class ExtClass : BaseClass
{
}

// Also need to test constraint on loaded type (ConstraintsHelper.ResolveBounds)
class ExtAsConstraint<T> where T : ExtClass { }
// Binder.ContainsNestedTypeOfUnconstructedGenericType specifically enumerates all TypeKinds,
// which we want to make sure we handle extension classes.
class UnqualifiedNestedTypeInCref<T>
{
    public extension class Inner : BaseClass { }
    public void M(Inner i) { } // should also emit diagnostic, can't have ext class as parameter
    /// <see cref=""UnqualifiedNestedTypeInCref{int}.M(Inner)""/> // WRN_UnqualifiedNestedTypeInCref
    public void N() { }
}
class NewExtClass
{
    void Test()
    {
        new ExtClass();
    }
}
[AttributeUsage(AttributeTargets.All)]
class ObjectParamAttribute : Attribute { public ObjectParamAttribute(object x) { } }
[ObjectParamAttribute(new ExtClass())]
class CanBeValidAttributeArgument { }
// Compilation options: MainTypeName == ""EntryPoint""
extension class EntryPoint : BaseClass
{
    static void Main() { }
}
class ExtAsNewConstraint
{
    void Foo<T>() where T : new() => new T();
    void TestFoo() => Foo<ExtClass>();
}
extension class ExtExplicitInterfaceImpl : BaseClass
{
    IEnumerator IEnumerable.GetEnumerator() => null;
}
extension class MemberNameSame : BaseClass
{
    void MemberNameSame() { }
}
// SourceMemberContainerTypeSymbol.CheckMembersAgainstBaseType should trigger in other examples
[Obsolete(""An attribute on a extension class"")] // ObsoleteAttribute just used as pre-existing attribute.
extension class AllowedAttributeLocations : BaseClass { }
[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid(""fb6e2361-f2da-4f7e-b1ff-76492acfed5d"")]
extension class ExtComImport : BaseClass { }
class Visibility
{
    private class BaseClass { }
    public extension class ExtClass : BaseClass { }
}
class VolatileField
{
    public volatile ExtClass field; // invalid due to extension class as field type,
    // but we're testing the detection processing of ERR_VolatileStruct (although that shouldn't be reported)
    public void SuppressCS0649FieldNeverUsed() => field = null;
}
interface VariantInterface<in TIn, out TOut> { }
class VariantInterfaceTest : VariantInterface<ExtClass, ExtClass> { }
extension class ExtCycle : ExtCycle { }
// TypeSymbolExtensions.GetDefaultBaseOrNull might also need modifying
";

            // Not tested, since it's impossible to get an expression of type `extension class`: ForEachLoopBinder.SatisfiesForEachPattern,
            // ConversionsBase.HasImplicitReferenceConversion, OperatorFacts.DefinitelyHasNoUserDefinedOperators,
            // All of the cases in MethodTypeInferrer, DataFlowPass.MarkFieldsUsed, AsyncMethodToStateMachineRewriter.GenerateAwaitOnCompleted{Dynamic}

            // AllowedAttributeLocations is fine, might want to move to success-test

            // PROTOTYPE: Diagnostics that should be here but aren't:
            // EntryPoint (possibly?)
            // MemberNameSame
            CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.ReleaseExe.WithMainTypeName("EntryPoint"), parseOptions: parseOptions).VerifyDiagnostics(
                // (51,10): error CS0542: 'MemberNameSame': member names cannot be the same as their enclosing type
                //     void MemberNameSame() { }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "MemberNameSame").WithArguments("MemberNameSame").WithLocation(51, 10),
                // (58,17): error CS0424: 'ExtComImport': a class with the ComImport attribute cannot specify a base class
                // extension class ExtComImport : BaseClass { }
                Diagnostic(ErrorCode.ERR_ComImportWithBase, "ExtComImport").WithArguments("ExtComImport").WithLocation(58, 17),
                // (66,30): error CS0723: Cannot declare a variable of static type 'ExtClass'
                //     public volatile ExtClass field; // invalid due to extension class as field type,
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "field").WithArguments("ExtClass").WithLocation(66, 30),
                // (62,28): error CS0060: Inconsistent accessibility: base class 'Visibility.BaseClass' is less accessible than class 'Visibility.ExtClass'
                //     public extension class ExtClass : BaseClass { }
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "ExtClass").WithArguments("Visibility.ExtClass", "Visibility.BaseClass").WithLocation(62, 28),
                // (72,17): error CS0103: The name 'ExtCycle' does not exist in the current context
                // extension class ExtCycle : ExtCycle { }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ExtCycle").WithArguments("ExtCycle").WithLocation(72, 17),
                // (14,36): error CS0717: 'ExtClass': static classes cannot be used as constraints
                // class ExtAsConstraint<T> where T : ExtClass { }
                Diagnostic(ErrorCode.ERR_ConstraintIsStaticClass, "ExtClass").WithArguments("ExtClass").WithLocation(14, 36),
                // (47,17): error CS0540: 'ExtExplicitInterfaceImpl.IEnumerable.GetEnumerator()': containing type does not implement interface 'IEnumerable'
                //     IEnumerator IEnumerable.GetEnumerator() => null;
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "IEnumerable").WithArguments("ExtExplicitInterfaceImpl.System.Collections.IEnumerable.GetEnumerator()", "System.Collections.IEnumerable").WithLocation(47, 17),
                // (20,17): error CS0721: 'UnqualifiedNestedTypeInCref<T>.Inner': static types cannot be used as parameters
                //     public void M(Inner i) { } // should also emit diagnostic, can't have ext class as parameter
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "M").WithArguments("UnqualifiedNestedTypeInCref<T>.Inner").WithLocation(20, 17),
                // (71,7): error CS0718: 'ExtClass': static types cannot be used as type arguments
                // class VariantInterfaceTest : VariantInterface<ExtClass, ExtClass> { }
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "VariantInterfaceTest").WithArguments("ExtClass").WithLocation(71, 7),
                // (71,7): error CS0718: 'ExtClass': static types cannot be used as type arguments
                // class VariantInterfaceTest : VariantInterface<ExtClass, ExtClass> { }
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "VariantInterfaceTest").WithArguments("ExtClass").WithLocation(71, 7),
                // (33,23): error CS0712: Cannot create an instance of the static class 'ExtClass'
                // [ObjectParamAttribute(new ExtClass())]
                Diagnostic(ErrorCode.ERR_InstantiatingStaticClass, "new ExtClass()").WithArguments("ExtClass").WithLocation(33, 23),
                // (28,9): error CS0712: Cannot create an instance of the static class 'ExtClass'
                //         new ExtClass();
                Diagnostic(ErrorCode.ERR_InstantiatingStaticClass, "new ExtClass()").WithArguments("ExtClass").WithLocation(28, 9),
                // (43,23): error CS0718: 'ExtClass': static types cannot be used as type arguments
                //     void TestFoo() => Foo<ExtClass>();
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "Foo<ExtClass>").WithArguments("ExtClass").WithLocation(43, 23)
            );
        }
    }
}
