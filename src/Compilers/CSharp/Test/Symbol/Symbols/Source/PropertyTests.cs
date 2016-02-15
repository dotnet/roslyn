// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public class PropertyTests : CSharpTestBase
    {
        [Fact]
        public void SetGetOnlyAutoPropNoExperimental()
        {
            // One would think this creates an error, but it doesn't because
            // language version is a property of the parser and there are
            // no syntactical changes to the language for get-only autoprops
            CreateCompilationWithMscorlib45(@"
class C
{
    public int P { get; }
}").VerifyDiagnostics();
        }

        [Fact]
        public void SetGetOnlyAutoPropInConstructor()
        {
            CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int P { get; }
    public C()
    {
        P = 10;
    }
}").VerifyDiagnostics();
        }


        [Fact]
        public void GetOnlyAutoPropBadOverride()
        {
            CreateExperimentalCompilationWithMscorlib45(@"

class Base
{
    public virtual int P { get; set; }

    public virtual int P1 { get { return 1; } set { } }
}

class C : Base
{
    public override int P { get; }
    public override int P1 { get; }

    public C()
    {
        P = 10;
        P1 = 10;
    }

}").VerifyDiagnostics(
    // (12,25): error CS8080: "Auto-implemented properties must override all accessors of the overridden property."
    //     public override int P { get; }
    Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P").WithArguments("C.P").WithLocation(12, 25),
    // (13,25): error CS8080: "Auto-implemented properties must override all accessors of the overridden property."
    //     public override int P1 { get; }
    Diagnostic(ErrorCode.ERR_AutoPropertyMustOverrideSet, "P1").WithArguments("C.P1").WithLocation(13, 25)

                );
        }

        [Fact]
        public void SetGetOnlyAutoPropOutOfConstructor()
        {
            CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int P { get; }
    public static int Ps { get; }

    public C()
    {
        Ps = 3;
    }

    public void M()
    {
        P = 10;
        C.Ps = 1;
    }
}

struct S
{
    public int P { get; }
    public static int Ps { get; }

    public S()
    {
        this = default(S);
        Ps = 5;
    }

    public void M()
    {
        P = 10;
        S.Ps = 1;
    }
}

").VerifyDiagnostics(
    // (24,12): error CS0568: Structs cannot contain explicit parameterless constructors
    //     public S()
    Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "S").WithLocation(24, 12),
    // (9,9): error CS0200: Property or indexer 'C.Ps' cannot be assigned to -- it is read only
    //         Ps = 3;
    Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Ps").WithArguments("C.Ps").WithLocation(9, 9),
    // (27,9): error CS0200: Property or indexer 'S.Ps' cannot be assigned to -- it is read only
    //         Ps = 5;
    Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "Ps").WithArguments("S.Ps").WithLocation(27, 9),
    // (14,9): error CS0200: Property or indexer 'C.P' cannot be assigned to -- it is read only
    //         P = 10;
    Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("C.P").WithLocation(14, 9),
    // (15,9): error CS0200: Property or indexer 'C.Ps' cannot be assigned to -- it is read only
    //         C.Ps = 1;
    Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "C.Ps").WithArguments("C.Ps").WithLocation(15, 9),
    // (32,9): error CS0200: Property or indexer 'S.P' cannot be assigned to -- it is read only
    //         P = 10;
    Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P").WithArguments("S.P").WithLocation(32, 9),
    // (33,9): error CS0200: Property or indexer 'S.Ps' cannot be assigned to -- it is read only
    //         S.Ps = 1;
    Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "S.Ps").WithArguments("S.Ps").WithLocation(33, 9)

    );
        }

        [Fact]
        public void StructWithSameNameFieldAndProperty()
        {
            var text = @"
struct S
{
    int a = 2;
    int a { get { return 1; } set {} }
}";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
    // (4,9): error CS0573: 'S': cannot have instance property or field initializers in structs
    //     int a = 2;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "a").WithArguments("S").WithLocation(4, 9),
    // (5,9): error CS0102: The type 'S' already contains a definition for 'a'
    //     int a { get { return 1; } set {} }
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "a").WithArguments("S", "a").WithLocation(5, 9),
    // (4,9): warning CS0414: The field 'S.a' is assigned but its value is never used
    //     int a = 2;
    Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("S.a").WithLocation(4, 9)
    );
        }

        [Fact]
        public void AutoWithInitializerInClass()
        {
            var text = @"class C
{
    public int P { get; set; } = 1;
    internal protected static long Q { get; } = 10;
    public decimal R { get; } = 300;
}";

            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var p = c.GetMember<PropertySymbol>("P");
            Assert.NotNull(p.GetMethod);
            Assert.NotNull(p.SetMethod);

            var q = c.GetMember<PropertySymbol>("Q");
            Assert.NotNull(q.GetMethod);
            Assert.Null(q.SetMethod);

            var r = c.GetMember<PropertySymbol>("R");
            Assert.NotNull(r.GetMethod);
            Assert.Null(r.SetMethod);
        }

        [Fact]
        public void AutoWithInitializerInStruct1()
        {
            var text = @"
struct S
{
    public int P { get; set; } = 1;
    internal static long Q { get; } = 10;
    public decimal R { get; } = 300;
}";

            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
    // (4,16): error CS0573: 'S': cannot have instance property or field initializers in structs
    //     public int P { get; set; } = 1;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "P").WithArguments("S").WithLocation(4, 16),
    // (6,20): error CS0573: 'S': cannot have instance property or field initializers in structs
    //     public decimal R { get; } = 300;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "R").WithArguments("S").WithLocation(6, 20)
                );
        }

        [Fact]
        public void AutoWithInitializerInStruct2()
        {
            var text = @"struct S
{
    public int P { get; set; } = 1;
    internal static long Q { get; } = 10;
    public decimal R { get; } = 300;

    public S(int i) : this() {}
}";

            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions);
            comp.VerifyDiagnostics(
    // (3,16): error CS0573: 'S': cannot have instance property or field initializers in structs
    //     public int P { get; set; } = 1;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "P").WithArguments("S").WithLocation(3, 16),
    // (5,20): error CS0573: 'S': cannot have instance property or field initializers in structs
    //     public decimal R { get; } = 300;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "R").WithArguments("S").WithLocation(5, 20)
);

            var global = comp.GlobalNamespace;
            var s = global.GetTypeMember("S");

            var p = s.GetMember<PropertySymbol>("P");
            Assert.NotNull(p.GetMethod);
            Assert.NotNull(p.SetMethod);

            var q = s.GetMember<PropertySymbol>("Q");
            Assert.NotNull(q.GetMethod);
            Assert.Null(q.SetMethod);

            var r = s.GetMember<PropertySymbol>("R");
            Assert.NotNull(r.GetMethod);
            Assert.Null(r.SetMethod);
        }

        [Fact]
        public void AutoInitializerInInterface()
        {
            var text = @"interface I
{
    int P { get; } = 0;
}";
            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions);

            comp.VerifyDiagnostics(
                // (3,9): error CS8035: Auto-implemented properties inside interfaces cannot have initializers.
                //     int P { get; } = 0;
                Diagnostic(ErrorCode.ERR_AutoPropertyInitializerInInterface, "P").WithArguments("I.P").WithLocation(3, 9));
        }

        [Fact]
        public void AutoNoSetOrInitializer()
        {
            var text = @"class C
{
    public int P { get; }
}";
            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AutoNoGet()
        {
            var text = @"class C
{
    public int P { set {} }
    public int Q { set; } = 0;
    public int R { set; }
}";
            var comp = CreateCompilationWithMscorlib(text, parseOptions: TestOptions.ExperimentalParseOptions);

            comp.VerifyDiagnostics(
// (4,20): error CS8034: Auto-implemented properties must have get accessors.
//     public int Q { set; } = 0;
Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "set").WithArguments("C.Q.set").WithLocation(4, 20),
// (5,20): error CS8034: Auto-implemented properties must have get accessors.
//     public int R { set; }
Diagnostic(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, "set").WithArguments("C.R.set").WithLocation(5, 20));
        }

        [WorkItem(542745, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542745")]
        [Fact()]
        public void AutoImplementedAccessorNotImplicitlyDeclared()
        {
            var text = @"
class A 
{
    abstract int P  { get; set; }
    internal int P2 { get; set; }
}

interface I 
{
    int Q {get; set; }
}
";
            // Per design meeting (see bug 11253), in C#, if there's a "get" or "set" written,
            // then IsImplicitDeclared should be false.

            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var i = global.GetTypeMembers("I", 0).Single();

            var p = a.GetMembers("P").SingleOrDefault() as PropertySymbol;
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
            Assert.False(p.SetMethod.IsImplicitlyDeclared);

            p = a.GetMembers("P2").SingleOrDefault() as PropertySymbol;
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
            Assert.False(p.SetMethod.IsImplicitlyDeclared);

            var q = i.GetMembers("Q").SingleOrDefault() as PropertySymbol;
            Assert.False(q.GetMethod.IsImplicitlyDeclared);
            Assert.False(q.SetMethod.IsImplicitlyDeclared);
        }

        [WorkItem(542746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542746")]
        [Fact]
        public void AutoImplementedBackingFieldLocation()
        {
            var text = @"
class C 
{
    int Prop { get; set; }

    struct S 
    {
        string Prop { get; set; }
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var type01 = global.GetTypeMembers("C").Single();
            var type02 = type01.GetTypeMembers("S").Single();

            var mems = type01.GetMembers();
            FieldSymbol backField = null;
            // search but not use internal backfield name 
            // there is exact ONE field symbol in this test
            foreach (var m in mems)
            {
                if (m.Kind == SymbolKind.Field)
                {
                    backField = m as FieldSymbol;
                    break;
                }
            }
            // Back field location should be same as Property
            Assert.NotNull(backField);
            Assert.False(backField.Locations.IsEmpty);
            var prop = type01.GetMembers("Prop").Single() as PropertySymbol;
            Assert.Equal(prop.Locations.Length, backField.Locations.Length);
            Assert.Equal(prop.Locations[0].ToString(), backField.Locations[0].ToString());
            // -------------------------------------
            mems = type02.GetMembers();
            backField = null;
            // search but not use internal backfield name 
            // there is exact ONE field symbol in this test
            foreach (var m in mems)
            {
                if (m.Kind == SymbolKind.Field)
                {
                    backField = m as FieldSymbol;
                    break;
                }
            }
            // Back field location should be same as Property
            Assert.NotNull(backField);
            Assert.False(backField.Locations.IsEmpty);
            prop = type02.GetMembers("Prop").Single() as PropertySymbol;
            Assert.Equal(prop.Locations.Length, backField.Locations.Length);
            Assert.Equal(prop.Locations[0].ToString(), backField.Locations[0].ToString());
        }

        [WorkItem(537401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537401")]
        [Fact]
        public void EventEscapedIdentifier()
        {
            var text = @"
delegate void @out();
class C1
{
    @out @in
    {
        get;
    };
}
";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            NamedTypeSymbol c1 = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("C1").Single();
            PropertySymbol ein = (PropertySymbol)c1.GetMembers("in").Single();
            Assert.Equal("in", ein.Name);
            Assert.Equal("C1.@in", ein.ToString());
            NamedTypeSymbol dout = (NamedTypeSymbol)ein.Type.TypeSymbol;
            Assert.Equal("out", dout.Name);
            Assert.Equal("@out", dout.ToString());
        }

        [ClrOnlyFact]
        public void PropertyNonDefaultAccessorNames()
        {
            var source = @"
class Program
{
    static void M(Valid i)
    {
        i.Instance = 0;
        System.Console.Write(""{0}"", i.Instance);
    }
    static void Main()
    {
        Valid.Static = 0;
        System.Console.Write(""{0}"", Valid.Static);
    }
}
";
            var compilation = CompileAndVerify(source, new[] { s_propertiesDll }, expectedOutput: "0");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldc.i4.0  
  IL_0001:  call       ""void Valid.Static.set""
  IL_0006:  ldstr      ""{0}""
  IL_000b:  call       ""int Valid.Static.get""
  IL_0010:  box        ""int""
  IL_0015:  call       ""void System.Console.Write(string, object)""
  IL_001a:  ret       
}
");
        }

        [WorkItem(528633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528633")]
        [Fact]
        public void MismatchedAccessorTypes()
        {
            var source = @"
class Program
{
    static void M(Mismatched i)
    {
        i.Instance = 0;
        System.Console.Write(""{0}"", i.Instance);
    }
    static void N(Signatures i)
    {
        i.StaticAndInstance = 0;
        i.GetUsedAsSet = 0;
    }
    static void Main()
    {
        Mismatched.Static = 0;
        System.Console.Write(""{0}"", Mismatched.Static);
    }
}
";
            var compilation = CompileWithCustomPropertiesAssembly(source);
            var actualErrors = compilation.GetDiagnostics();
            compilation.VerifyDiagnostics(
            // (6,11): error CS1061: 'Mismatched' does not contain a definition for 'Instance' and no extension method 'Instance' accepting a first argument of type 'Mismatched' could be found (are you missing a using directive or an assembly reference?)
            //         i.Instance = 0;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Instance").WithArguments("Mismatched", "Instance"),
            // (7,39): error CS1061: 'Mismatched' does not contain a definition for 'Instance' and no extension method 'Instance' accepting a first argument of type 'Mismatched' could be found (are you missing a using directive or an assembly reference?)
            //         System.Console.Write("{0}", i.Instance);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Instance").WithArguments("Mismatched", "Instance"),
            // (11,11): error CS1545: Property, indexer, or event 'Signatures.StaticAndInstance' is not supported by the language; try directly calling accessor methods 'Signatures.GoodStatic.get' or 'Signatures.GoodInstance.set'
            //         i.StaticAndInstance = 0;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "StaticAndInstance").WithArguments("Signatures.StaticAndInstance", "Signatures.GoodStatic.get", "Signatures.GoodInstance.set"),
            // (12,11): error CS1545: Property, indexer, or event 'Signatures.GetUsedAsSet' is not supported by the language; try directly calling accessor methods 'Signatures.GoodInstance.get' or 'Signatures.GoodInstance.get'
            //         i.GetUsedAsSet = 0;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "GetUsedAsSet").WithArguments("Signatures.GetUsedAsSet", "Signatures.GoodInstance.get", "Signatures.GoodInstance.get"),
            // (16,20): error CS0117: 'Mismatched' does not contain a definition for 'Static'
            //         Mismatched.Static = 0;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Static").WithArguments("Mismatched", "Static"),
            // (17,48): error CS0117: 'Mismatched' does not contain a definition for 'Static'
            //         System.Console.Write("{0}", Mismatched.Static);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Static").WithArguments("Mismatched", "Static"));
        }

        /// <summary>
        /// Properties should refer to methods
        /// in the type members collection.
        /// </summary>
        [ClrOnlyFact]
        public void MethodsAndAccessorsSame()
        {
            var source =
@"class A
{
    public static object P { get; set; }
    public object Q { get; set; }
}
class B<T>
{
    public static T P { get; set; }
    public T Q { get; set; }
}
class C : B<string>
{
}
";
            Action<ModuleSymbol> validator = module =>
            {
                // Non-generic type.
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
                Assert.Equal(type.TypeParameters.Length, 0);
                Assert.Same(type, type.ConstructedFrom);
                VerifyMethodsAndAccessorsSame(type, type.GetMember<PropertySymbol>("P"));
                VerifyMethodsAndAccessorsSame(type, type.GetMember<PropertySymbol>("Q"));

                // Generic type.
                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
                Assert.Equal(type.TypeParameters.Length, 1);
                Assert.Same(type, type.ConstructedFrom);
                VerifyMethodsAndAccessorsSame(type, type.GetMember<PropertySymbol>("P"));
                VerifyMethodsAndAccessorsSame(type, type.GetMember<PropertySymbol>("Q"));

                // Generic type with parameter substitution.
                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").BaseType;
                Assert.Equal(type.TypeParameters.Length, 1);
                Assert.NotSame(type, type.ConstructedFrom);
                VerifyMethodsAndAccessorsSame(type, type.GetMember<PropertySymbol>("P"));
                VerifyMethodsAndAccessorsSame(type, type.GetMember<PropertySymbol>("Q"));
            };
            CompileAndVerify(source: source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        private void VerifyMethodsAndAccessorsSame(NamedTypeSymbol type, PropertySymbol property)
        {
            VerifyMethodAndAccessorSame(type, property, property.GetMethod);
            VerifyMethodAndAccessorSame(type, property, property.SetMethod);
        }

        private void VerifyMethodAndAccessorSame(NamedTypeSymbol type, PropertySymbol property, MethodSymbol accessor)
        {
            Assert.NotNull(accessor);
            Assert.Equal(type, accessor.ContainingType);
            Assert.Equal(type, accessor.ContainingSymbol);

            var method = type.GetMembers(accessor.Name).Single();
            Assert.NotNull(method);
            Assert.Equal(accessor, method);

            Assert.True(accessor.MethodKind == MethodKind.PropertyGet || accessor.MethodKind == MethodKind.PropertySet,
                "Accessor kind: " + accessor.MethodKind.ToString());
            Assert.Equal(accessor.AssociatedSymbol, property);
        }

        [WorkItem(538789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538789")]
        [Fact]
        public void NoAccessors()
        {
            var source = @"
class Program
{
    static void M(NoAccessors i)
    {
        i.Instance = NoAccessors.Static;
    }
}
";
            var compilation = CompileWithCustomPropertiesAssembly(source);
            var actualErrors = compilation.GetDiagnostics();
            DiagnosticsUtils.VerifyErrorCodes(actualErrors,
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoSuchMemberOrExtension, Line = 6, Column = 11 },
                new ErrorDescription { Code = (int)ErrorCode.ERR_NoSuchMember, Line = 6, Column = 34 });

            var type = (PENamedTypeSymbol)compilation.GlobalNamespace.GetMembers("NoAccessors").Single();

            // Methods are available.
            Assert.NotNull(type.GetMembers("StaticMethod").SingleOrDefault());
            Assert.NotNull(type.GetMembers("InstanceMethod").SingleOrDefault());
            Assert.Equal(2, type.GetMembers().OfType<MethodSymbol>().Count());

            // Properties are not available.
            Assert.Null(type.GetMembers("Static").SingleOrDefault());
            Assert.Null(type.GetMembers("Instance").SingleOrDefault());
            Assert.Equal(0, type.GetMembers().OfType<PropertySymbol>().Count());
        }

        /// <summary>
        /// Calling bogus methods directly should be allowed.
        /// </summary>
        [ClrOnlyFact]
        public void CallMethodsDirectly()
        {
            var source = @"
class Program
{
    static void M(Mismatched i)
    {
        i.InstanceBoolSet(false);
        System.Console.Write(""{0}"", i.InstanceInt32Get());
    }
    static void Main()
    {
        Mismatched.StaticBoolSet(false);
        System.Console.Write(""{0}"", Mismatched.StaticInt32Get());
    }
}
";
            var compilation = CompileAndVerify(source, new[] { s_propertiesDll }, expectedOutput: "0");

            compilation.VerifyIL("Program.Main",
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldc.i4.0  
  IL_0001:  call       ""void Mismatched.StaticBoolSet(bool)""
  IL_0006:  ldstr      ""{0}""
  IL_000b:  call       ""int Mismatched.StaticInt32Get()""
  IL_0010:  box        ""int""
  IL_0015:  call       ""void System.Console.Write(string, object)""
  IL_001a:  ret       
}
");
        }

        [ClrOnlyFact]
        public void MethodsReferencedInMultipleProperties()
        {
            var source = @"
class Program
{
    static void M(Signatures i)
    {
        i.GoodInstance = 0;
        System.Console.Write(""{0}"", i.GoodInstance);
    }
    static void Main()
    {
        Signatures.GoodStatic = 0;
        System.Console.Write(""{0}"", Signatures.GoodStatic);
    }
}
";
            var verifier = CompileAndVerify(source, new[] { s_propertiesDll }, expectedOutput: "0");

            verifier.VerifyIL("Program.Main",
@"{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldc.i4.0  
  IL_0001:  call       ""void Signatures.GoodStatic.set""
  IL_0006:  ldstr      ""{0}""
  IL_000b:  call       ""int Signatures.GoodStatic.get""
  IL_0010:  box        ""int""
  IL_0015:  call       ""void System.Console.Write(string, object)""
  IL_001a:  ret       
}
");
            var type = (PENamedTypeSymbol)verifier.Compilation.GlobalNamespace.GetMembers("Signatures").Single();

            // Valid static property, property with signature that does not match accessors,
            // and property with accessors that do not match each other.
            var goodStatic = (PEPropertySymbol)type.GetMembers("GoodStatic").Single();
            var badStatic = (PEPropertySymbol)type.GetMembers("BadStatic").Single();
            var mismatchedStatic = (PEPropertySymbol)type.GetMembers("MismatchedStatic").Single();

            Assert.False(goodStatic.MustCallMethodsDirectly);
            Assert.True(badStatic.MustCallMethodsDirectly);
            Assert.True(mismatchedStatic.MustCallMethodsDirectly);

            VerifyAccessor(goodStatic.GetMethod, goodStatic, MethodKind.PropertyGet);
            VerifyAccessor(goodStatic.SetMethod, goodStatic, MethodKind.PropertySet);
            VerifyAccessor(badStatic.GetMethod, goodStatic, MethodKind.PropertyGet);
            VerifyAccessor(badStatic.SetMethod, goodStatic, MethodKind.PropertySet);
            VerifyAccessor(mismatchedStatic.GetMethod, goodStatic, MethodKind.PropertyGet);
            VerifyAccessor(mismatchedStatic.SetMethod, null, MethodKind.Ordinary);

            // Valid instance property, property with signature that does not match accessors,
            // and property with accessors that do not match each other.
            var goodInstance = (PEPropertySymbol)type.GetMembers("GoodInstance").Single();
            var badInstance = (PEPropertySymbol)type.GetMembers("BadInstance").Single();
            var mismatchedInstance = (PEPropertySymbol)type.GetMembers("MismatchedInstance").Single();

            Assert.False(goodInstance.MustCallMethodsDirectly);
            Assert.True(badInstance.MustCallMethodsDirectly);
            Assert.True(mismatchedInstance.MustCallMethodsDirectly);

            VerifyAccessor(goodInstance.GetMethod, goodInstance, MethodKind.PropertyGet);
            VerifyAccessor(goodInstance.SetMethod, goodInstance, MethodKind.PropertySet);
            VerifyAccessor(badInstance.GetMethod, goodInstance, MethodKind.PropertyGet);
            VerifyAccessor(badInstance.SetMethod, goodInstance, MethodKind.PropertySet);
            VerifyAccessor(mismatchedInstance.GetMethod, goodInstance, MethodKind.PropertyGet);
            VerifyAccessor(mismatchedInstance.SetMethod, null, MethodKind.Ordinary);

            // Mix of static and instance accessors.
            var staticAndInstance = (PEPropertySymbol)type.GetMembers("StaticAndInstance").Single();
            VerifyAccessor(staticAndInstance.GetMethod, goodStatic, MethodKind.PropertyGet);
            VerifyAccessor(staticAndInstance.SetMethod, goodInstance, MethodKind.PropertySet);
            Assert.True(staticAndInstance.MustCallMethodsDirectly);

            // Property with get and set accessors both referring to the same get method.
            var getUsedAsSet = (PEPropertySymbol)type.GetMembers("GetUsedAsSet").Single();
            VerifyAccessor(getUsedAsSet.GetMethod, goodInstance, MethodKind.PropertyGet);
            VerifyAccessor(getUsedAsSet.SetMethod, goodInstance, MethodKind.PropertyGet);
            Assert.True(getUsedAsSet.MustCallMethodsDirectly);
        }

        private void VerifyAccessor(MethodSymbol accessor, PEPropertySymbol associatedProperty, MethodKind methodKind)
        {
            Assert.NotNull(accessor);
            Assert.Equal(accessor.AssociatedSymbol, associatedProperty);
            Assert.Equal(accessor.MethodKind, methodKind);

            if (associatedProperty != null)
            {
                var method = (methodKind == MethodKind.PropertyGet) ? associatedProperty.GetMethod : associatedProperty.SetMethod;
                Assert.Equal(accessor, method);
            }
        }

        /// <summary>
        /// Support mixes of family and assembly.
        /// </summary>
        [Fact]
        public void FamilyAssembly()
        {
            var source = @"
class Program
{
    static void Main()
    {
        System.Console.Write(""{0}"", Signatures.StaticGet());
    }
}
";
            var compilation = CompileWithCustomPropertiesAssembly(source, TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
            var type = (PENamedTypeSymbol)compilation.GlobalNamespace.GetMembers("FamilyAssembly").Single();

            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyGetAssemblySetStatic").Single(),
                Accessibility.ProtectedOrInternal,
                Accessibility.Protected,
                Accessibility.Internal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyGetFamilyOrAssemblySetStatic").Single(),
                Accessibility.ProtectedOrInternal,
                Accessibility.Protected,
                Accessibility.ProtectedOrInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyGetFamilyAndAssemblySetStatic").Single(),
                Accessibility.Protected,
                Accessibility.Protected,
                Accessibility.ProtectedAndInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("AssemblyGetFamilyOrAssemblySetStatic").Single(),
                Accessibility.ProtectedOrInternal,
                Accessibility.Internal,
                Accessibility.ProtectedOrInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("AssemblyGetFamilyAndAssemblySetStatic").Single(),
                Accessibility.Internal,
                Accessibility.Internal,
                Accessibility.ProtectedAndInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyOrAssemblyGetFamilyOrAssemblySetStatic").Single(),
                Accessibility.ProtectedOrInternal,
                Accessibility.ProtectedOrInternal,
                Accessibility.ProtectedOrInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyOrAssemblyGetFamilyAndAssemblySetStatic").Single(),
                Accessibility.ProtectedOrInternal,
                Accessibility.ProtectedOrInternal,
                Accessibility.ProtectedAndInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyAndAssemblyGetFamilyAndAssemblySetStatic").Single(),
                Accessibility.ProtectedAndInternal,
                Accessibility.ProtectedAndInternal,
                Accessibility.ProtectedAndInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyAndAssemblyGetOnlyInstance").Single(),
                Accessibility.ProtectedAndInternal,
                Accessibility.ProtectedAndInternal,
                Accessibility.NotApplicable);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyOrAssemblySetOnlyInstance").Single(),
                Accessibility.ProtectedOrInternal,
                Accessibility.NotApplicable,
                Accessibility.ProtectedOrInternal);
            VerifyAccessibility(
                (PEPropertySymbol)type.GetMembers("FamilyAndAssemblyGetFamilyOrAssemblySetInstance").Single(),
                Accessibility.ProtectedOrInternal,
                Accessibility.ProtectedAndInternal,
                Accessibility.ProtectedOrInternal);
        }

        // Property and getter with void type. Dev10 treats this as a valid
        // property. Would it be better to treat the property as invalid?
        [Fact]
        public void VoidPropertyAndAccessorType()
        {
            const string ilSource = @"
.class public A
{
	.method public static void get_P() { ret }
	.property void P() { .get void A::get_P() }
	.method public instance void get_Q() { ret }
	.property void Q() { .get instance void A::get_Q() }
}
";
            const string cSharpSource = @"
class C
{
    static void M(A x)
    {
        N(A.P);
        N(x.Q);
    }
    static void N(object o)
    {
    }
}";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadArgType, "A.P").WithArguments("1", "void", "object"),
                Diagnostic(ErrorCode.ERR_BadArgType, "x.Q").WithArguments("1", "void", "object"));
        }

        /// <summary>
        /// Properties where the property and accessor signatures differ by
        /// modopt only should be supported (as in the native compiler).
        /// </summary>
        [ClrOnlyFact]
        public void SignaturesDifferByModOptsOnly()
        {
            const string ilSource =
@".class public A { }
.class public B { }
.class public C
{
	.method public instance int32 get_noopt()
	{
	    ldc.i4.0
		ret
	}
	.method public instance int32 modopt(A) get_returnopt()
	{
	    ldc.i4.0
		ret
	}
    .method public instance void set_noopt(int32 val)
	{
		ret
	}
    .method public instance void set_argopt(int32 modopt(A) val)
	{
		ret
	}
    .method public instance void modopt(A) set_returnopt(int32 val)
	{
		ret
	}
	// Modifier on property but not accessors.
	.property int32 modopt(A) P1()
	{
	    .get instance int32 C::get_noopt()
		.set instance void C::set_noopt(int32)
	}
	// Modifier on accessors but not property.
	.property int32 P2()
	{
	    .get instance int32 modopt(A) C::get_returnopt()
		.set instance void C::set_argopt(int32 modopt(A))
	}
	// Modifier on getter only.
	.property int32 P3()
	{
	    .get instance int32 modopt(A) C::get_returnopt()
		.set instance void C::set_noopt(int32)
	}
	// Modifier on setter only.
	.property int32 P4()
	{
	    .get instance int32 C::get_noopt()
		.set instance void C::set_argopt(int32 modopt(A))
	}
	// Modifier on setter return type.
	.property int32 P5()
	{
	    .get instance int32 C::get_noopt()
		.set instance void modopt(A) C::set_returnopt(int32)
	}
	// Modifier on property and different modifier on accessors.
	.property int32 modopt(B) P6()
	{
	    .get instance int32 modopt(A) C::get_returnopt()
		.set instance void C::set_argopt(int32 modopt(A))
	}
}";
            const string cSharpSource =
@"class D
{
    static void M(C c)
    {
        c.P1 = c.P1;
        c.P2 = c.P2;
        c.P3 = c.P3;
        c.P4 = c.P4;
        c.P5 = c.P5;
        c.P6 = c.P6;
    }
}";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [WorkItem(538956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538956")]
        [ClrOnlyFact]
        public void PropertyAccessorDoesNotHideMethod()
        {
            const string cSharpSource = @"
interface IA {
    string get_Foo();
}

interface IB : IA {
    int Foo { get; }
}

class Program {
    static void Main() {
        IB x = null;
        string s = x.get_Foo().ToLower();
    }
}
";
            CompileWithCustomILSource(cSharpSource, null);
        }

        [WorkItem(538956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538956")]
        [ClrOnlyFact]
        public void PropertyAccessorDoesNotConflictWithMethod()
        {
            const string cSharpSource = @"
interface IA {
    string get_Foo();
}

interface IB {
    int Foo { get; }
}

interface IC : IA, IB { }

class Program {
    static void Main() {
        IC x = null;
        string s = x.get_Foo().ToLower();
    }
}
";
            CompileWithCustomILSource(cSharpSource, null);
        }

        [WorkItem(538956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538956")]
        [Fact]
        public void PropertyAccessorCannotBeCalledAsMethod()
        {
            const string cSharpSource = @"
interface I {
    int Foo { get; }
}

class Program {
    static void Main() {
        I x = null;
        string s = x.get_Foo();
    }
}
";
            CreateCompilationWithMscorlib(cSharpSource).VerifyDiagnostics(
                // (9,22): error CS0571: 'I.Foo.get': cannot explicitly call operator or accessor
                //         string s = x.get_Foo();
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Foo").WithArguments("I.Foo.get"));
        }

        [WorkItem(538992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538992")]
        [Fact]
        public void CanNotAccessPropertyThroughParenthesizedType()
        {
            const string cSharpSource = @"
class Program
{
    static void Main()
    {
        (Program).X = 1;
    }

    static int X { get; set; }
}
";
            CreateCompilationWithMscorlib(cSharpSource).VerifyDiagnostics(
                // (6,10): error CS0119: 'Program' is a type, which is not valid in the given context
                //         (Program).X = 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type"));
        }

        [ClrOnlyFact]
        public void CanReadInstancePropertyWithStaticGetterAsStatic()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property instance int32 Foo() { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [Fact]
        public void CanNotReadInstancePropertyWithStaticGetterAsInstance()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property instance int32 Foo() { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    A a = null;
    int x = a.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (5,13): error CS0176: Member 'A.Foo' cannot be accessed with an instance reference; qualify it with a type name instead
                //     int x = a.Foo;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "a.Foo").WithArguments("A.Foo"));
        }

        [WorkItem(527658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527658")]
        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void CS1546ERR_BindToBogusProp1_PropertyWithPinnedModifierIsBogus()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property instance int32 pinned Foo() { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,18): error CS1546: Property, indexer, or event 'A.Foo' is not supported by the language; try directly calling accessor method 'A.get_Foo()'
                //     object x = A.Foo;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "Foo").WithArguments("A.Foo", "A.get_Foo()"));
        }

        [WorkItem(538850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538850")]
        [Fact]
        public void PropertyWithMismatchedReturnTypeOfGetterIsBogus()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property string Foo() { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,18): error CS1546: Property, indexer, or event 'A.Foo' is not supported by the language; try directly calling accessor method 'A.get_Foo()'
                //     object x = A.Foo;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "Foo").WithArguments("A.Foo", "A.get_Foo()"));
        }

        [WorkItem(527659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527659")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void PropertyWithCircularReturnTypeIsNotSupported()
        {
            const string ilSource = @"
.class public E extends E { }

.class public A {
  .method public static class E get_Foo() { ldnull throw }
  .property class E Foo() { .get class E A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
    B y = A.Foo; 
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
    // (5,11): error CS0268: Imported type 'E' is invalid. It contains a circular base class dependency.
    //     B y = A.Foo; 
    Diagnostic(ErrorCode.ERR_ImportedCircularBase, "A.Foo").WithArguments("E", "E"),
    // (5,11): error CS0029: Cannot implicitly convert type 'E' to 'B'
    //     B y = A.Foo; 
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "A.Foo").WithArguments("E", "B")
                );
            // Dev10 errors:
            // error CS0268: Imported type 'E' is invalid. It contains a circular base class dependency.
            // error CS0570: 'A.Foo' is not supported by the language
        }

        [WorkItem(527664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527664")]
        [Fact(Skip = "527664")]
        public void PropertyWithOpenGenericTypeAsTypeArgumentOfReturnTypeIsNotSupported()
        {
            const string ilSource = @"
.class public E<T> { }

.class public A {
  .method public static class E<class E> get_Foo() { ldnull throw }
  .property class E<class E> Foo() { .get class E<class E> A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
            // TODO: check diagnostics when it is implemented
        }

        [WorkItem(527657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527657")]
        [Fact(Skip = "527657")]
        public void Dev10IgnoresSentinelInPropertySignature()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo(...) { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadModOptProperty()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(int32) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [WorkItem(527660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527660")]
        [Fact(Skip = "527660")]
        public void CanReadPropertyWithModOptInBaseClassOfReturnType()
        {
            const string ilSource = @"
.class public E extends class [mscorlib]System.Collections.Generic.List`1<int32> modopt(int8) { }

.class public A  {
  .method public static class E get_Foo() { ldnull throw }
  .property class E Foo() { .get class E A::get_Foo() }
}

";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadPropertyOfArrayTypeWithModOptElement()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 modopt(int32)[] get_Foo() { ldnull throw }
  .property int32 modopt(int32)[] Foo() { .get int32 modopt(int32)[] A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int[] x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadModOptPropertyWithNonModOptGetter()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 modopt(int32) Foo() { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [WorkItem(527656, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527656")]
        [Fact(Skip = "527656")]
        public void CanReadNonModOptPropertyWithOpenGenericModOptGetter()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 modopt(class [mscorlib]System.IComparable`1) get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 modopt(class [mscorlib]System.IComparable`1) A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadNonModOptPropertyWithModOptGetter()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 modopt(int32) A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CompileWithCustomILSource(cSharpSource, ilSource);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadModOptPropertyWithDifferentModOptGetter()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(string) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics();
        }

        [WorkItem(538845, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538845")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadPropertyWithMultipleAndNestedModOpts()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(int8) modopt(native int modopt(uint8)*[] modopt(void)) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,15): error CS1546: Property, indexer, or event 'A.Foo' is not supported by the language; try directly calling accessor method 'A.get_Foo()'
                //     int x = A.Foo;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "Foo").WithArguments("A.Foo", "A.get_Foo()"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadPropertyWithModReqsNestedWithinModOpts()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 modopt(int32) get_Foo() { ldnull throw }
  .property int32 modopt(class [mscorlib]System.IComparable`1<method void*()[]> modreq(bool)) Foo() { .get int32 modopt(int32) A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    int x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,15): error CS1546: Property, indexer, or event 'A.Foo' is not supported by the language; try directly calling accessor method 'A.get_Foo()'
                //     int x = A.Foo;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "Foo").WithArguments("A.Foo", "A.get_Foo()"));
        }

        [WorkItem(538846, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538846")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanNotReadPropertyWithModReq()
        {
            const string ilSource = @"
.class public A {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 modreq(int8) Foo() { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,18): error CS1546: Property, indexer, or event 'A.Foo' is not supported by the language; try directly calling accessor method 'A.get_Foo()'
                //     object x = A.Foo;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "Foo").WithArguments("A.Foo", "A.get_Foo()"));
        }

        [WorkItem(527662, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527662")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CanReadPropertyWithModReqInBaseClassOfReturnType()
        {
            const string ilSource = @"
.class public E extends class [mscorlib]System.Collections.Generic.List`1<int32 modreq(int8)[]> { }

.class public A {
  .method public static class E get_Foo() { ldnull throw }
  .property class E Foo() { .get class E A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics();
        }

        [WorkItem(538787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538787")]
        [Fact]
        public void CanNotReadPropertyOfUnsupportedType()
        {
            const string ilSource = @"
.class public B
{
  .method public instance void .ctor()
  {
    ldarg.0
    call instance void class System.Object::.ctor()
    ret
  }
  .method public static method void*()[] get_Foo()
  {
    ldnull
    ret
  }
  .property method void*()[] Foo()
  {
    .get method void*()[] B::get_Foo()
  }
}
";
            const string cSharpSource = @"
class C {
    static void Main() {
        object foo = B.Foo;
    }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,24): error CS1546: Property, indexer, or event 'B.Foo' is not supported by the language; try directly calling accessor method 'B.get_Foo()'
                //         object foo = B.Foo;
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "Foo").WithArguments("B.Foo", "B.get_Foo()"));
        }

        [WorkItem(538791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538791")]
        [Fact]
        public void CanNotReadAmbiguousProperty()
        {
            const string ilSource = @"
.class public B
{
  .method public instance void .ctor()
  {
    ldarg.0
    call instance void class System.Object::.ctor()
    ret
  }
  .method public static int32 get_Foo()
  {
    ldnull
    throw
  }
  .method public static int32[] get_Foo()
  {
    ldnull
    throw
  }
  .property int32 Foo()
  {
    .get int32 B::get_Foo()
  }
  .property int32[] Foo()
  {
    .get int32[] B::get_Foo()
  }
}
";
            const string cSharpSource = @"
class C {
    static void Main() {
        object foo = B.Foo;
    }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,24): error CS0229: Ambiguity between 'B.Foo' and 'B.Foo'
                //         object foo = B.Foo;
                Diagnostic(ErrorCode.ERR_AmbigMember, "Foo").WithArguments("B.Foo", "B.Foo"));
        }

        [Fact]
        public void VoidReturningPropertyHidesMembersFromBase()
        {
            const string ilSource = @"
.class public B {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 B::get_Foo() }
}

.class public A extends B {
  .method public static void get_Foo() { ldnull throw }
  .property void Foo() { .get void A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,16): error CS0029: Cannot implicitly convert type 'void' to 'object'
                //     object x = A.Foo;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "A.Foo").WithArguments("void", "object"));
        }

        [WorkItem(527663, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527663")]
        [Fact]
        public void CanNotReadPropertyFromAmbiguousGenericClass()
        {
            const string ilSource = @"
.class public A`1<T> {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 A`1::get_Foo() }
}

.class public A<T> {
  .method public static int32 get_Foo() { ldnull throw }
  .property int32 Foo() { .get int32 A::get_Foo() }
}
";
            const string cSharpSource = @"
class B {
  static void Main() {
    object x = A<int>.Foo;
  }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,16): error CS0104: 'A<>' is an ambiguous reference between 'A<T>' and 'A<T>'
                //     object x = A<int>.Foo;
                Diagnostic(ErrorCode.ERR_AmbigContext, "A<int>").WithArguments("A<>", "A<T>", "A<T>").WithLocation(4, 16));
        }

        [WorkItem(538789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538789")]
        [Fact]
        public void PropertyWithoutAccessorsIsBogus()
        {
            const string ilSource = @"
.class public B {
  .method public instance void .ctor() {
    ldarg.0
    call instance void class System.Object::.ctor()
    ret
  }

  .property int32 Foo() { }
}
";
            const string cSharpSource = @"
class C {
    static void Main() {
        object foo = B.Foo;
    }
}
";
            CreateCompilationWithCustomILSource(cSharpSource, ilSource).VerifyDiagnostics(
                // (4,24): error CS0117: 'B' does not contain a definition for 'Foo'
                //         object foo = B.Foo;
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Foo").WithArguments("B", "Foo"));
        }

        [WorkItem(538946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538946")]
        [Fact]
        public void FalseAmbiguity()
        {
            const string text = @"
interface IA
{
    int Foo { get; }
}
 
interface IB<T> : IA { }
 
interface IC : IB<int>, IB<string> { }
 
class C
{
    static void Main()
    {
        IC x = null;
        int y = x.Foo;
    }
}
";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            var diagnostics = comp.GetDiagnostics();
            Assert.Empty(diagnostics);
        }

        [WorkItem(539320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539320")]
        [Fact]
        public void FalseWarningCS0109ForNewModifier()
        {
            const string text = @"
class MyBase
{
    public int MyProp
    {
        get
        {
            return 1;
        }
    }
}

class MyClass : MyBase
{
    int intI = 0;
    new int MyProp
    {
        get
        {
            return intI;
        }
        set
        {
            intI = value;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            var diagnostics = comp.GetDiagnostics();
            Assert.Empty(diagnostics);
        }

        [WorkItem(539319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539319")]
        [Fact]
        public void FalseErrorCS0103ForValueKeywordInExpImpl()
        {
            const string text = @"
interface MyInter
{
    int MyProp
    {
        get;
        set;
    }
}

class MyClass : MyInter
{
    static int intI = 0;
    int MyInter.MyProp
    {
        get
        {
            return intI;
        }
        set
        {
            intI = value;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            var diagnostics = comp.GetDiagnostics();
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ExplicitInterfaceImplementationSimple()
        {
            string text = @"
interface I
{
    int P { get; set; }
}

class C : I
{
    int I.P { get; set; }
}
";

            var comp = CreateCompilationWithMscorlib(Parse(text));

            var globalNamespace = comp.GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("I").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceProperty = (PropertySymbol)@interface.GetMembers("P").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("C").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(@interface));

            var classProperty = (PropertySymbol)@class.GetMembers("I.P").Single();

            CheckPropertyExplicitImplementation(@class, classProperty, interfaceProperty);
        }

        [Fact]
        public void ExplicitInterfaceImplementationGeneric()
        {
            string text = @"
namespace N
{
    interface I<T>
    {
        T P { get; set; }
    }
}

class C : N.I<int>
{
    int N.I<int>.P { get; set; }
}
";

            var comp = CreateCompilationWithMscorlib(Parse(text));

            var globalNamespace = comp.GlobalNamespace;
            var @namespace = (NamespaceSymbol)globalNamespace.GetMembers("N").Single();

            var @interface = (NamedTypeSymbol)@namespace.GetTypeMembers("I").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceProperty = (PropertySymbol)@interface.GetMembers("P").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("C").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var classProperty = (PropertySymbol)@class.GetMembers("N.I<System.Int32>.P").Single();

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceProperty = (PropertySymbol)substitutedInterface.GetMembers("P").Single();

            CheckPropertyExplicitImplementation(@class, classProperty, substitutedInterfaceProperty);
        }

        [Fact]
        public void ImportPropertiesWithParameters()
        {
            //TODO: To be implemented once indexer properties implemented
        }

        [WorkItem(539998, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539998")]
        [ClrOnlyFact]
        public void ImportDefaultPropertiesWithParameters()
        {
            var source = @"
class Program
{
    public static WithParameterizedProps testClass = new WithParameterizedProps();

    static void Main()
    {
        testClass.set_P(1, null);
        System.Console.Write(testClass.get_P(1));

        testClass.set_P(""2"", null);
        System.Console.Write(testClass.get_P(""2""));

        testClass.set_P(1, ""2"", null);
        System.Console.Write(testClass.get_P(1, ""2""));
    }
}
";
            var compilation = CreateCompilationWithMscorlib(
                source,
                new[] { TestReferences.SymbolsTests.Properties },
                TestOptions.ReleaseExe);

            Action<ModuleSymbol> validator = module =>
            {
                var program = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Program");
                var field = program.GetMember<FieldSymbol>("testClass");
                var type = field.Type.TypeSymbol;
                // Non-generic type.
                //var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("WithParameterizedProps");
                var getters = type.GetMembers("get_P").OfType<MethodSymbol>();
                Assert.Equal(2, getters.Count(getter => getter.Parameters.Length == 1));
                Assert.Equal(1, getters.Count(getter => getter.Parameters.Length == 2));

                Assert.True(getters.Any(getter => getter.Parameters[0].Type.SpecialType == SpecialType.System_Int32));
                Assert.True(getters.Any(getter => getter.Parameters[0].Type.SpecialType == SpecialType.System_String));
                Assert.True(getters.Any(getter =>
                    getter.Parameters.Length == 2 &&
                    getter.Parameters[0].Type.SpecialType == SpecialType.System_Int32 &&
                    getter.Parameters[1].Type.SpecialType == SpecialType.System_String));
            };
            // TODO: it would be nice to validate the emitted symbols, but CompileAndVerify currently has a limitation
            // where it won't pick up the referenced assemblies from the compilation when it creates the ModuleSymbol
            // for the emitted assembly (i.e. WithParameterizedProps will be missing).
            CompileAndVerify(compilation, sourceSymbolValidator: validator, /*symbolValidator: validator,*/ expectedOutput: "1221");
        }

        [WorkItem(540342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540342")]
        [ClrOnlyFact]
        public void NoSequencePointsForAutoPropertyAccessors()
        {
            var text = @"
class C
{
    object P { get; set; }
}";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        [WorkItem(541688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541688")]
        [Fact]
        public void Simple2()
        {
            var text =
@"
using System;
 
public class A : Attribute
{
    public A X { get; set; }
    public A X { get; set; }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var xs = a.GetMembers("X");
            Assert.Equal(2, xs.Length);
            foreach (var x in xs)
            {
                Assert.Equal(a, (x as PropertySymbol).Type.TypeSymbol); // duplicate, but all the same.
            }

            var errors = comp.GetDeclarationDiagnostics();
            var one = errors.Single();
            Assert.Equal(ErrorCode.ERR_DuplicateNameInClass, (ErrorCode)one.Code);
        }

        [WorkItem(528769, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528769")]
        [Fact]
        public void IndexerGetSetParameterLocation()
        {
            string text = @"using System;
class Test
{
      public int this[int i]
      {
          get { return i; }
          set { }
      }
}
";

            var comp = CreateCompilationWithMscorlib(text);

            var globalNamespace = comp.SourceModule.GlobalNamespace;

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Test").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var accessor = @class.GetMembers("get_Item").Single() as MethodSymbol;
            Assert.Equal(1, accessor.Parameters.Length);
            var locs = accessor.Parameters[0].Locations;
            // i
            Assert.Equal(1, locs.Length);
            Assert.True(locs[0].IsInSource, "InSource");

            accessor = @class.GetMembers("set_Item").Single() as MethodSymbol;
            Assert.Equal(2, accessor.Parameters.Length);
            // i
            locs = accessor.Parameters[0].Locations;
            Assert.Equal(1, locs.Length);
            Assert.True(locs[0].IsInSource, "InSource");
        }

        [WorkItem(545682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545682")]
        [ClrOnlyFact]
        public void PropertyWithParametersHidingMethod01()
        {
            var source1 =
@"Public Class A
    Public Shared Function P() As Object
        Return Nothing
    End Function
    Public Function Q() As Object
        Return Nothing
    End Function
End Class
Public Class B1
    Inherits A
    Public Shared Shadows Property P As Object
    Public Shadows Property Q As Object
End Class
Public Class B2
    Inherits A
    Public Shared Shadows ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Shadows ReadOnly Property Q(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void M(B1 b)
    {
        object o;
        o = B1.P;
        o = B1.P();
        o = b.Q;
        o = b.Q();
    }
}";
            var compilation2 = CompileAndVerify(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics();
            compilation2.VerifyIL("C.M(B1)",
@"{
  // Code size       27 (0x1b)
  .maxstack  1
  IL_0000:  call       ""object B1.P.get""
  IL_0005:  pop
  IL_0006:  call       ""object A.P()""
  IL_000b:  pop
  IL_000c:  ldarg.0
  IL_000d:  callvirt   ""object B1.Q.get""
  IL_0012:  pop
  IL_0013:  ldarg.0
  IL_0014:  callvirt   ""object A.Q()""
  IL_0019:  pop
  IL_001a:  ret
}");
            var source3 =
@"class C
{
    static void M(B2 b)
    {
        object o;
        o = B2.P;
        o = B2.P();
        o = b.Q;
        o = b.Q();
    }
}";
            var compilation3 = CreateCompilationWithMscorlib(source3, new[] { reference1 });
            compilation3.VerifyDiagnostics(
                // (6,16): error CS0428: Cannot convert method group 'P' to non-delegate type 'object'. Did you intend to invoke the method?
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "P").WithArguments("P", "object").WithLocation(6, 16),
                // (8,15): error CS0428: Cannot convert method group 'Q' to non-delegate type 'object'. Did you intend to invoke the method?
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Q").WithArguments("Q", "object").WithLocation(8, 15));
        }

        [ClrOnlyFact]
        public void PropertyWithParametersHidingMethod02()
        {
            var source1 =
@"Public Class A
    Public ReadOnly Property P(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class
Public Class B
    Inherits A
    Public Shadows ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            // Property with parameters hiding property with parameters.
            var source2 =
@"class C
{
    static void M(B b)
    {
        object o;
        o = b.P;
    }
}";
            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,15): error CS1546: Property, indexer, or event 'B.P[object]' is not supported by the language; try directly calling accessor method 'B.get_P(object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "P").WithArguments("B.P[object]", "B.get_P(object)").WithLocation(6, 15));
            // Property with parameters and extension method.
            var source3 =
@"class C
{
    static void M(A a)
    {
        object o;
        o = a.P;
    }
}
static class E
{
    internal static object P(this object o) { return null; }
}";
            var compilation3 = CreateCompilationWithMscorlibAndSystemCore(source3, new[] { reference1 });
            compilation3.VerifyDiagnostics(
                // (6,15): error CS0428: Cannot convert method group 'P' to non-delegate type 'object'. Did you intend to invoke the method?
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "P").WithArguments("P", "object").WithLocation(6, 15));
        }

        [ClrOnlyFact]
        public void PropertyWithParametersHidingMethod03()
        {
            var source1 =
@"Public Class A
    Public Function P1() As Object
        Return Nothing
    End Function
    Public Function P2() As Object
        Return Nothing
    End Function
    Public Function P3() As Object
        Return Nothing
    End Function
    Friend Function P4() As Object
        Return Nothing
    End Function
    Friend Function P5() As Object
        Return Nothing
    End Function
    Friend Function P6() As Object
        Return Nothing
    End Function
    Protected Function P7() As Object
        Return Nothing
    End Function
    Protected Function P8() As Object
        Return Nothing
    End Function
    Protected Function P9() As Object
        Return Nothing
    End Function
End Class
Public Class B
    Inherits A
    Public Shadows ReadOnly Property P1(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Friend Shadows ReadOnly Property P2(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Protected Shadows ReadOnly Property P3(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Shadows ReadOnly Property P4(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Friend Shadows ReadOnly Property P5(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Protected Shadows ReadOnly Property P6(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Shadows ReadOnly Property P7(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Friend Shadows ReadOnly Property P8(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Protected Shadows ReadOnly Property P9(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C : B
{
    void M()
    {
        object o;
        o = this.P1();
        o = this.P2();
        o = this.P3();
        o = this.P4();
        o = this.P5();
        o = this.P6();
        o = this.P7();
        o = this.P8();
        o = this.P9();
    }
}
class D
{
    static void M(B b)
    {
        object o;
        o = b.P1();
        o = b.P2();
        o = b.P3();
        o = b.P4();
        o = b.P5();
        o = b.P6();
        o = b.P7();
        o = b.P8();
        o = b.P9();
    }
}";
            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (9,18): error CS1955: Non-invocable member 'B.P4[object]' cannot be used like a method.
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P4").WithArguments("B.P4[object]").WithLocation(9, 18),
                // (10,18): error CS1061: 'C' does not contain a definition for 'P5' and no extension method 'P5' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P5").WithArguments("C", "P5").WithLocation(10, 18),
                // (11,18): error CS1955: Non-invocable member 'B.P6[object]' cannot be used like a method.
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P6").WithArguments("B.P6[object]").WithLocation(11, 18),
                // (25,15): error CS1955: Non-invocable member 'B.P4[object]' cannot be used like a method.
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P4").WithArguments("B.P4[object]").WithLocation(25, 15),
                // (26,15): error CS1061: 'B' does not contain a definition for 'P5' and no extension method 'P5' accepting a first argument of type 'B' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "P5").WithArguments("B", "P5").WithLocation(26, 15),
                // (27,15): error CS1955: Non-invocable member 'B.P6[object]' cannot be used like a method.
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P6").WithArguments("B.P6[object]").WithLocation(27, 15),
                // (28,15): error CS1955: Non-invocable member 'B.P7[object]' cannot be used like a method.
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P7").WithArguments("B.P7[object]").WithLocation(28, 15),
                // (29,15): error CS0122: 'A.P8()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "P8").WithArguments("A.P8()").WithLocation(29, 15),
                // (30,15): error CS1955: Non-invocable member 'B.P9[object]' cannot be used like a method.
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P9").WithArguments("B.P9[object]").WithLocation(30, 15));
        }

        [ClrOnlyFact]
        public void PropertyWithParametersAndOtherErrors()
        {
            var source1 =
@"Public Class A
    Public Shared ReadOnly Property P1(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public ReadOnly Property P2(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Protected ReadOnly Property P3(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void M(A a)
    {
        object o;
        o = a.P1;
        o = A.P2;
        o = a.P3;
    }
}";
            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,15): error CS1546: Property, indexer, or event 'A.P1[object]' is not supported by the language; try directly calling accessor method 'A.get_P1(object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "P1").WithArguments("A.P1[object]", "A.get_P1(object)").WithLocation(6, 15),
                // (7,15): error CS1546: Property, indexer, or event 'A.P2[object]' is not supported by the language; try directly calling accessor method 'A.get_P2(object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp1, "P2").WithArguments("A.P2[object]", "A.get_P2(object)").WithLocation(7, 15),
                // (8,15): error CS0122: 'A.P3[object]' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "P3").WithArguments("A.P3[object]").WithLocation(8, 15));
        }

        [ClrOnlyFact]
        public void SubstitutedPropertyWithParameters()
        {
            var source1 =
@"Public Class A(Of T)
    Public Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class B
{
    static void M(A<object> a)
    {
        object o;
        o = a.P;
        a.P = o;
        o = a.get_P(null);
        a.set_P(null, o);
    }
}";
            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (6,15): error CS1545: Property, indexer, or event 'A<object>.P[object]' is not supported by the language; try directly calling accessor methods 'A<object>.get_P(object)' or 'A<object>.set_P(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A<object>.P[object]", "A<object>.get_P(object)", "A<object>.set_P(object, object)").WithLocation(6, 15),
                // (7,11): error CS1545: Property, indexer, or event 'A<object>.P[object]' is not supported by the language; try directly calling accessor methods 'A<object>.get_P(object)' or 'A<object>.set_P(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "P").WithArguments("A<object>.P[object]", "A<object>.get_P(object)", "A<object>.set_P(object, object)").WithLocation(7, 11));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void DifferentAccessorSignatures_ByRef()
        {
            var source1 =
@".class public A1
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object v) { ret }
  .property instance object P(object)
  {
    .get instance object A1::get_P(object)
    .set instance void A1::set_P(object, object v)
  }
}
.class public A2
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object get_P(object i) { ldnull ret }
  .method public instance void set_P(object& i, object v) { ret }
  .property instance object P(object)
  {
    .get instance object A2::get_P(object)
    .set instance void A2::set_P(object&, object v)
  }
}
.class public A3
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object& v) { ret }
  .property instance object P(object)
  {
    .get instance object A3::get_P(object)
    .set instance void A3::set_P(object, object& v)
  }
}
.class public A4
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object& get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object v) { ret }
  .property instance object& P(object)
  {
    .get instance object& A4::get_P(object)
    .set instance void A4::set_P(object, object v)
  }
}
.class public A5
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object& get_P(object i) { ldnull ret }
  .method public instance void set_P(object& i, object v) { ret }
  .property instance object& P(object)
  {
    .get instance object& A5::get_P(object)
    .set instance void A5::set_P(object&, object v)
  }
}
.class public A6
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object& get_P(object i) { ldnull ret }
  .method public instance void set_P(object i, object& v) { ret }
  .property instance object& P(object)
  {
    .get instance object& A6::get_P(object)
    .set instance void A6::set_P(object, object& v)
  }
}
.class public A7
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object get_P(object& i) { ldnull ret }
  .method public instance void set_P(object i, object v) { ret }
  .property instance object P(object&)
  {
    .get instance object A7::get_P(object&)
    .set instance void A7::set_P(object, object v)
  }
}
.class public A8
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object get_P(object& i) { ldnull ret }
  .method public instance void set_P(object& i, object v) { ret }
  .property instance object P(object&)
  {
    .get instance object A8::get_P(object&)
    .set instance void A8::set_P(object&, object v)
  }
}
.class public A9
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = {string('P')}
  .method public instance object get_P(object& i) { ldnull ret }
  .method public instance void set_P(object i, object& v) { ret }
  .property instance object P(object&)
  {
    .get instance object A9::get_P(object&)
    .set instance void A9::set_P(object, object& v)
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"class C
{
    static void M(A1 _1, A2 _2, A3 _3, A4 _4, A5 _5, A6 _6, A7 _7, A8 _8, A9 _9)
    {
        object x = null;
        object y = null;
        _1[y] = _1[x];
        _2[y] = _2[x];
        _3[y] = _3[x];
        _4[y] = _4[x];
        _5[y] = _5[x];
        _6[y] = _6[x];
        _7[ref y] = _7[ref x];
        _8[ref y] = _8[ref x];
        _9[ref y] = _9[ref x];
    }
}";
            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (8,9): error CS1545: Property, indexer, or event 'A2.this[object]' is not supported by the language; try directly calling accessor methods 'A2.get_P(object)' or 'A2.set_P(ref object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_2[y]").WithArguments("A2.this[object]", "A2.get_P(object)", "A2.set_P(ref object, object)").WithLocation(8, 9),
                // (8,17): error CS1545: Property, indexer, or event 'A2.this[object]' is not supported by the language; try directly calling accessor methods 'A2.get_P(object)' or 'A2.set_P(ref object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_2[x]").WithArguments("A2.this[object]", "A2.get_P(object)", "A2.set_P(ref object, object)").WithLocation(8, 17),
                // (9,9): error CS1545: Property, indexer, or event 'A3.this[object]' is not supported by the language; try directly calling accessor methods 'A3.get_P(object)' or 'A3.set_P(object, ref object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_3[y]").WithArguments("A3.this[object]", "A3.get_P(object)", "A3.set_P(object, ref object)").WithLocation(9, 9),
                // (9,17): error CS1545: Property, indexer, or event 'A3.this[object]' is not supported by the language; try directly calling accessor methods 'A3.get_P(object)' or 'A3.set_P(object, ref object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_3[x]").WithArguments("A3.this[object]", "A3.get_P(object)", "A3.set_P(object, ref object)").WithLocation(9, 17),
                // (10,9): error CS1545: Property, indexer, or event 'A4.this[object]' is not supported by the language; try directly calling accessor methods 'A4.get_P(object)' or 'A4.set_P(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_4[y]").WithArguments("A4.this[object]", "A4.get_P(object)", "A4.set_P(object, object)").WithLocation(10, 9),
                // (10,17): error CS1545: Property, indexer, or event 'A4.this[object]' is not supported by the language; try directly calling accessor methods 'A4.get_P(object)' or 'A4.set_P(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_4[x]").WithArguments("A4.this[object]", "A4.get_P(object)", "A4.set_P(object, object)").WithLocation(10, 17),
                // (11,9): error CS1545: Property, indexer, or event 'A5.this[object]' is not supported by the language; try directly calling accessor methods 'A5.get_P(object)' or 'A5.set_P(ref object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_5[y]").WithArguments("A5.this[object]", "A5.get_P(object)", "A5.set_P(ref object, object)").WithLocation(11, 9),
                // (11,17): error CS1545: Property, indexer, or event 'A5.this[object]' is not supported by the language; try directly calling accessor methods 'A5.get_P(object)' or 'A5.set_P(ref object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_5[x]").WithArguments("A5.this[object]", "A5.get_P(object)", "A5.set_P(ref object, object)").WithLocation(11, 17),
                // (12,9): error CS1545: Property, indexer, or event 'A6.this[object]' is not supported by the language; try directly calling accessor methods 'A6.get_P(object)' or 'A6.set_P(object, ref object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_6[y]").WithArguments("A6.this[object]", "A6.get_P(object)", "A6.set_P(object, ref object)").WithLocation(12, 9),
                // (12,17): error CS1545: Property, indexer, or event 'A6.this[object]' is not supported by the language; try directly calling accessor methods 'A6.get_P(object)' or 'A6.set_P(object, ref object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_6[x]").WithArguments("A6.this[object]", "A6.get_P(object)", "A6.set_P(object, ref object)").WithLocation(12, 17),
                // (13,9): error CS1545: Property, indexer, or event 'A7.this[ref object]' is not supported by the language; try directly calling accessor methods 'A7.get_P(ref object)' or 'A7.set_P(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_7[ref y]").WithArguments("A7.this[ref object]", "A7.get_P(ref object)", "A7.set_P(object, object)").WithLocation(13, 9),
                // (13,21): error CS1545: Property, indexer, or event 'A7.this[ref object]' is not supported by the language; try directly calling accessor methods 'A7.get_P(ref object)' or 'A7.set_P(object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_7[ref x]").WithArguments("A7.this[ref object]", "A7.get_P(ref object)", "A7.set_P(object, object)").WithLocation(13, 21),
                // (14,9): error CS1545: Property, indexer, or event 'A8.this[ref object]' is not supported by the language; try directly calling accessor methods 'A8.get_P(ref object)' or 'A8.set_P(ref object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_8[ref y]").WithArguments("A8.this[ref object]", "A8.get_P(ref object)", "A8.set_P(ref object, object)").WithLocation(14, 9),
                /// (14,21): error CS1545: Property, indexer, or event 'A8.this[ref object]' is not supported by the language; try directly calling accessor methods 'A8.get_P(ref object)' or 'A8.set_P(ref object, object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_8[ref x]").WithArguments("A8.this[ref object]", "A8.get_P(ref object)", "A8.set_P(ref object, object)").WithLocation(14, 21),
                // (15,9): error CS1545: Property, indexer, or event 'A9.this[ref object]' is not supported by the language; try directly calling accessor methods 'A9.get_P(ref object)' or 'A9.set_P(object, ref object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_9[ref y]").WithArguments("A9.this[ref object]", "A9.get_P(ref object)", "A9.set_P(object, ref object)").WithLocation(15, 9),
                // (15,21): error CS1545: Property, indexer, or event 'A9.this[ref object]' is not supported by the language; try directly calling accessor methods 'A9.get_P(ref object)' or 'A9.set_P(object, ref object)'
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "_9[ref x]").WithArguments("A9.this[ref object]", "A9.get_P(ref object)", "A9.set_P(object, ref object)").WithLocation(15, 21));
        }

        #region "helpers"

        private static void CheckPropertyExplicitImplementation(NamedTypeSymbol @class, PropertySymbol classProperty, PropertySymbol interfaceProperty)
        {
            var interfacePropertyGetter = interfaceProperty.GetMethod;
            Assert.NotNull(interfacePropertyGetter);
            var interfacePropertySetter = interfaceProperty.SetMethod;
            Assert.NotNull(interfacePropertySetter);

            Assert.Equal(interfaceProperty, classProperty.ExplicitInterfaceImplementations.Single());

            var classPropertyGetter = classProperty.GetMethod;
            Assert.NotNull(classPropertyGetter);
            var classPropertySetter = classProperty.SetMethod;
            Assert.NotNull(classPropertySetter);

            Assert.Equal(interfacePropertyGetter, classPropertyGetter.ExplicitInterfaceImplementations.Single());
            Assert.Equal(interfacePropertySetter, classPropertySetter.ExplicitInterfaceImplementations.Single());

            var typeDef = (Microsoft.Cci.ITypeDefinition)@class;
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)@class.ContainingAssembly, EmitOptions.Default, OutputKind.DynamicallyLinkedLibrary,
                GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());

            var context = new EmitContext(module, null, new DiagnosticBag());
            var explicitOverrides = typeDef.GetExplicitImplementationOverrides(context);
            Assert.Equal(2, explicitOverrides.Count());
            Assert.True(explicitOverrides.All(@override => ReferenceEquals(@class, @override.ContainingType)));

            // We're not actually asserting that the overrides are in this order - set comparison just seems like overkill for two elements
            var getterOverride = explicitOverrides.First();
            Assert.Equal(classPropertyGetter, getterOverride.ImplementingMethod);
            Assert.Equal(interfacePropertyGetter.ContainingType, getterOverride.ImplementedMethod.GetContainingType(context));
            Assert.Equal(interfacePropertyGetter.Name, getterOverride.ImplementedMethod.Name);

            var setterOverride = explicitOverrides.Last();
            Assert.Equal(classPropertySetter, setterOverride.ImplementingMethod);
            Assert.Equal(interfacePropertySetter.ContainingType, setterOverride.ImplementedMethod.GetContainingType(context));
            Assert.Equal(interfacePropertySetter.Name, setterOverride.ImplementedMethod.Name);
            context.Diagnostics.Verify();
        }

        private static void VerifyAccessibility(PEPropertySymbol property, Accessibility propertyAccessibility, Accessibility getAccessibility, Accessibility setAccessibility)
        {
            Assert.Equal(property.DeclaredAccessibility, propertyAccessibility);
            Assert.False(property.MustCallMethodsDirectly);
            VerifyAccessorAccessibility(property.GetMethod, getAccessibility);
            VerifyAccessorAccessibility(property.SetMethod, setAccessibility);
        }

        private static void VerifyAccessorAccessibility(MethodSymbol accessor, Accessibility accessorAccessibility)
        {
            if (accessorAccessibility == Accessibility.NotApplicable)
            {
                Assert.Null(accessor);
            }
            else
            {
                Assert.NotNull(accessor);
                Assert.Equal(accessor.DeclaredAccessibility, accessorAccessibility);
            }
        }

        private CSharpCompilation CompileWithCustomPropertiesAssembly(string source, CSharpCompilationOptions options = null)
        {
            return CreateCompilationWithMscorlib(source, new[] { s_propertiesDll }, options ?? TestOptions.ReleaseDll);
        }

        private static readonly MetadataReference s_propertiesDll = TestReferences.SymbolsTests.Properties;

        #endregion

        [Fact]
        public void InteropDynamification()
        {
            var refSrc = @"
using System.Runtime.InteropServices;
[assembly: PrimaryInteropAssembly(0, 0)]
[assembly: Guid(""C35913A8-93FF-40B1-94AC-8F363CC17589"")]
[ComImport]
[Guid(""D541FDE7-A872-4AF7-8F68-DC9C2FC8DCC9"")]
public interface IA
{
    object P { get; }
    object M();
    string P2 { get; }
    string M2();
}";
            var refComp = CSharpCompilation.Create("DLL",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(refSrc) },
                references: new MetadataReference[] { MscorlibRef });

            var refData = AssemblyMetadata.CreateFromImage(refComp.EmitToArray());
            var mdRef = refData.GetReference(embedInteropTypes: false);
            var comp = CreateCompilationWithMscorlib("", new[] { mdRef });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.False(comp.ExternalReferences[1].Properties.EmbedInteropTypes);

            var ia = comp.GetTypeByMetadataName("IA");
            Assert.NotNull(ia);
            var iap = ia.GetMember<PropertySymbol>("P");
            Assert.False(iap.Type.IsDynamic());
            var iam = ia.GetMember<MethodSymbol>("M");
            Assert.False(iam.ReturnType.IsDynamic());

            var iap2 = ia.GetMember<PropertySymbol>("P2");
            Assert.Equal(SpecialType.System_String, iap2.Type.SpecialType);
            var iam2 = ia.GetMember<MethodSymbol>("M2");
            Assert.Equal(SpecialType.System_String, iam2.ReturnType.SpecialType);

            var compRef = refComp.ToMetadataReference(embedInteropTypes: false);
            comp = CreateCompilationWithMscorlib("", new[] { compRef });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.False(comp.ExternalReferences[1].Properties.EmbedInteropTypes);

            ia = comp.GetTypeByMetadataName("IA");
            Assert.NotNull(ia);
            iap = ia.GetMember<PropertySymbol>("P");
            Assert.False(iap.Type.IsDynamic());
            iam = ia.GetMember<MethodSymbol>("M");
            Assert.False(iam.ReturnType.IsDynamic());

            iap2 = ia.GetMember<PropertySymbol>("P2");
            Assert.Equal(SpecialType.System_String, iap2.Type.SpecialType);
            iam2 = ia.GetMember<MethodSymbol>("M2");
            Assert.Equal(SpecialType.System_String, iam2.ReturnType.SpecialType);

            mdRef = refData.GetReference(embedInteropTypes: true);
            comp = CreateCompilationWithMscorlib("", new[] { mdRef });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.True(comp.ExternalReferences[1].Properties.EmbedInteropTypes);

            ia = comp.GetTypeByMetadataName("IA");
            Assert.NotNull(ia);
            iap = ia.GetMember<PropertySymbol>("P");
            Assert.True(iap.Type.IsDynamic());
            iam = ia.GetMember<MethodSymbol>("M");
            Assert.True(iam.ReturnType.IsDynamic());

            iap2 = ia.GetMember<PropertySymbol>("P2");
            Assert.Equal(SpecialType.System_String, iap2.Type.SpecialType);
            iam2 = ia.GetMember<MethodSymbol>("M2");
            Assert.Equal(SpecialType.System_String, iam2.ReturnType.SpecialType);

            compRef = refComp.ToMetadataReference(embedInteropTypes: true);
            comp = CreateCompilationWithMscorlib("", new[] { compRef });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.True(comp.ExternalReferences[1].Properties.EmbedInteropTypes);

            ia = comp.GetTypeByMetadataName("IA");
            Assert.NotNull(ia);
            iap = ia.GetMember<PropertySymbol>("P");
            Assert.True(iap.Type.IsDynamic());
            iam = ia.GetMember<MethodSymbol>("M");
            Assert.True(iam.ReturnType.IsDynamic());

            iap2 = ia.GetMember<PropertySymbol>("P2");
            Assert.Equal(SpecialType.System_String, iap2.Type.SpecialType);
            iam2 = ia.GetMember<MethodSymbol>("M2");
            Assert.Equal(SpecialType.System_String, iam2.ReturnType.SpecialType);
            Assert.Equal(2, comp.ExternalReferences.Length);

            refSrc = @"
using System.Runtime.InteropServices;
[assembly: PrimaryInteropAssembly(0, 0)]
[assembly: Guid(""C35913A8-93FF-40B1-94AC-8F363CC17589"")]

public interface IA
{
    object P { get; }
    object M();
    string P2 { get; }
    string M2();
}";

            refComp = CSharpCompilation.Create("DLL",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(refSrc) },
                references: new[] { MscorlibRef });

            refData = AssemblyMetadata.CreateFromImage(refComp.EmitToArray());
            mdRef = refData.GetReference(embedInteropTypes: true);

            comp = CreateCompilationWithMscorlib("", new[] { mdRef });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.True(comp.ExternalReferences[1].Properties.EmbedInteropTypes);

            ia = comp.GetTypeByMetadataName("IA");
            Assert.NotNull(ia);
            iap = ia.GetMember<PropertySymbol>("P");
            Assert.Equal(SpecialType.System_Object, iap.Type.SpecialType);
            iam = ia.GetMember<MethodSymbol>("M");
            Assert.Equal(SpecialType.System_Object, iam.ReturnType.SpecialType);

            iap2 = ia.GetMember<PropertySymbol>("P2");
            Assert.Equal(SpecialType.System_String, iap2.Type.SpecialType);
            iam2 = ia.GetMember<MethodSymbol>("M2");
            Assert.Equal(SpecialType.System_String, iam2.ReturnType.SpecialType);

            compRef = refComp.ToMetadataReference(embedInteropTypes: true);
            comp = CreateCompilationWithMscorlib("", new[] { compRef });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.True(comp.ExternalReferences[1].Properties.EmbedInteropTypes);

            ia = comp.GetTypeByMetadataName("IA");
            Assert.NotNull(ia);
            iap = ia.GetMember<PropertySymbol>("P");
            Assert.Equal(SpecialType.System_Object, iap.Type.SpecialType);
            iam = ia.GetMember<MethodSymbol>("M");
            Assert.Equal(SpecialType.System_Object, iam.ReturnType.SpecialType);

            iap2 = ia.GetMember<PropertySymbol>("P2");
            Assert.Equal(SpecialType.System_String, iap2.Type.SpecialType);
            iam2 = ia.GetMember<MethodSymbol>("M2");
            Assert.Equal(SpecialType.System_String, iam2.ReturnType.SpecialType);
        }

        private delegate void VerifyType(bool isWinMd, params string[] expectedMembers);

        /// <summary>
        /// When the output type is .winmdobj properties should emit put_Property methods instead
        /// of set_Property methods.
        /// </summary>
        [ClrOnlyFact]
        public void WinRtPropertySet()
        {
            const string libSrc =
@"namespace Test
{
    public sealed class C
    {
        public int a;
        public int A
        {
            get { return a; }
            set { a = value; }
        }
    }
}";
            Func<string[], Action<ModuleSymbol>> getValidator = expectedMembers => m =>
            {
                var actualMembers =
                    m.GlobalNamespace.GetMember<NamespaceSymbol>("Test").
                    GetMember<NamedTypeSymbol>("C").GetMembers().ToArray();

                AssertEx.SetEqual(actualMembers.Select(s => s.Name), expectedMembers);
            };

            VerifyType verify = (winmd, expected) =>
             {
                 var validator = getValidator(expected);

                 // We should see the same members from both source and metadata
                 var verifier = CompileAndVerify(
                      libSrc,
                      sourceSymbolValidator: validator,
                      symbolValidator: validator,
                      options: winmd ? TestOptions.ReleaseWinMD : TestOptions.ReleaseDll);
                 verifier.VerifyDiagnostics();
             };

            // Test winmd
            verify(true,
                "a",
                "A",
                "get_A",
                "put_A",
                WellKnownMemberNames.InstanceConstructorName);

            // Test normal
            verify(false,
                "a",
                "A",
                "get_A",
                "set_A",
                WellKnownMemberNames.InstanceConstructorName);
        }

        /// <summary>
        /// Accessor type names that conflict should cause the appropriate diagnostic
        /// (i.e., set_ for dll, put_ for winmdobj)
        /// </summary>
        [Fact]
        public void WinRtPropertyAccessorNameConflict()
        {
            const string libSrc =
@"namespace Test
{
    public sealed class C
    {
        public int A
        {
            get; set;
        }
                
        public void put_A(int value) {}
        public void set_A(int value) {}
    }
}";
            var comp = CreateCompilationWithMscorlib(libSrc, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
    // (7,18): error CS0082: Type 'Test.C' already reserves a member called 'set_A' with the same parameter types
    //             get; set;
    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("set_A", "Test.C"));

            comp = CreateCompilationWithMscorlib(libSrc, options: TestOptions.ReleaseWinMD);
            comp.VerifyDiagnostics(
    // (7,18): error CS0082: Type 'Test.C' already reserves a member called 'put_A' with the same parameter types
    //             get; set;
    Diagnostic(ErrorCode.ERR_MemberReserved, "set").WithArguments("put_A", "Test.C"));
        }

        [Fact]
        public void AutoPropertiesBeforeCSharp3()
        {
            var source = @"
interface I
{
    int P { get; set; } // Fine
}

abstract class A
{
    public abstract int P { get; set; } // Fine
}

class C
{
    public int P { get; set; } // Error
}
";
            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp3)).VerifyDiagnostics();
            CreateCompilationWithMscorlib(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp2)).VerifyDiagnostics(
                // (14,16): error CS8023: Feature 'automatically implemented properties' is not available in C# 2.  Please use language version 3 or greater.
                //     public int P { get; set; } // Error
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "P").WithArguments("automatically implemented properties", "3"));
        }

        [WorkItem(1073332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073332")]
        [ClrOnlyFact]
        public void Bug1073332_01()
        {
            var text = @"
class Test
{
    static int[] property { get; } = { 1, 2, 3 };

    static void Main(string[] args)
    {
        foreach (var x in property)
        {
            System.Console.Write(x);
        }
    }
}
";
            CompileAndVerify(text, expectedOutput: "123").VerifyDiagnostics();
        }

        [WorkItem(1073332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073332")]
        [Fact]
        public void Bug1073332_02()
        {
            var text = @"
unsafe class Test
{
    int[] property { get; } = stackalloc int[256];

    static void Main(string[] args)
    {
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
    // (4,31): error CS1525: Invalid expression term 'stackalloc'
    //     int[] property { get; } = stackalloc int[256];
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc").WithLocation(4, 31),
    // (2,14): error CS0227: Unsafe code may only appear if compiling with /unsafe
    // unsafe class Test
    Diagnostic(ErrorCode.ERR_IllegalUnsafe, "Test").WithLocation(2, 14)
                );
        }


        [Fact, WorkItem(4696, "https://github.com/dotnet/roslyn/issues/4696")]
        public void LangVersioAndReadonlyAutoProperty()
        {
            var source = @"
public class Class1
{
    public Class1()
    {
        Prop1 = ""Test"";
    }

    public string Prop1 { get; }
}

abstract class Class2
{
    public abstract string Prop2 { get; }
}

interface I1
{
    string Prop3 { get; }
}
";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            comp.GetDeclarationDiagnostics().Verify(
    // (9,19): error CS8026: Feature 'readonly automatically implemented properties' is not available in C# 5.  Please use language version 6 or greater.
    //     public string Prop1 { get; }
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "Prop1").WithArguments("readonly automatically implemented properties", "6").WithLocation(9, 19)
                );
        }
    }
}
