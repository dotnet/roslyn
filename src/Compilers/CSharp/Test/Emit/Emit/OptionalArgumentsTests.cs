// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class OptionalArgumentsTests : CSharpTestBase
    {
        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void TestDuplicateConstantAttributesMetadata()
        {
            var ilSource =
@".assembly extern System {}
.class public C
{
  .method public static object F0([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    ldarg.0
    ret
  }
  .method public static object F1([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    ldarg.0
    ret
  }
  .method public static object F2([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    ldarg.0
    ret
  }
  .method public static object F3([opt] object o)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = {string('s')} // [DefaultParameterValue('s')]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    ldarg.0
    ret
  }
  .method public static int32 F4([opt] int32 i)
  {
    .param [1]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = ( 01 00 08 01 00 00 00 00 00 ) // [DefaultParameterValue(1)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = ( 01 00 08 02 00 00 00 00 00 ) // [DefaultParameterValue(2)]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = ( 01 00 08 03 00 00 00 00 00 ) // [DefaultParameterValue(3)]
    ldarg.0
    ret
  }
  .method public static valuetype [mscorlib]System.DateTime F5([opt] valuetype [mscorlib]System.DateTime d)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 01 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 02 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    .custom instance void [System]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 03 00 00 00 00 00 00 00 00 00 ) // [DateTimeConstant(3)]
    ldarg.0
    ret
  }
  .method public static valuetype [mscorlib]System.Decimal F6([opt] valuetype [mscorlib]System.Decimal d)
  {
    .param [1]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 ) // [DecimalConstant(2)]
    .custom instance void [System]System.Runtime.CompilerServices.DecimalConstantAttribute::.ctor(uint8, uint8, uint32, uint32, uint32) = ( 01 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00 00 00 ) // [DecimalConstant(2)]
    ldarg.0
    ret
  }
}";
            var csharpSource =
@"class P
{
    static void Main()
    {
        Report(C.F0());
        Report(C.F1());
        Report(C.F2());
        Report(C.F3());
        Report(C.F4());
        Report(C.F5().Ticks);
        Report(C.F6());
    }
    static void Report(object o)
    {
        var value = o is System.DateTime ? ((System.DateTime)o).ToString(""MM'/'dd'/'yyyy HH':'mm':'ss"") : o;
        System.Console.WriteLine(""{0}: {1}"", o.GetType(), value);
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource, TargetFramework.Mscorlib461, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput:
@"System.Reflection.Missing: System.Reflection.Missing
System.DateTime: 01/01/0001 00:00:00
System.DateTime: 01/01/0001 00:00:00
System.DateTime: 01/01/0001 00:00:00
System.Int32: 0
System.Int64: 3
System.Decimal: 3");
        }

        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [Fact]
        public void TestDuplicateConstantAttributesSameValues()
        {
            var source1 =
@"using System.Runtime.CompilerServices;
public class C
{
    public object F([DecimalConstant(0, 0, 0, 0, 1)]decimal o = 1)
    {
        return o;
    }
    public object this[decimal a, [DecimalConstant(0, 0, 0, 0, 2)]decimal b = 2]
    {
        get { return b; }
        set { }
    }
    public static object D(decimal o)
    {
        return o;
    }
}
public delegate object D([DecimalConstant(0, 0, 0, 0, 3)]decimal o = 3);
";
            var comp1 = CreateCompilation(source1, options: TestOptions.DebugDll);
            comp1.VerifyDiagnostics();
            CompileAndVerify(comp1, sourceSymbolValidator: module =>
                {
                    var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                    VerifyDefaultValueAttribute(type.GetMember<MethodSymbol>("F").Parameters[0], "DecimalConstantAttribute", 1, false);
                    VerifyDefaultValueAttribute(type.GetMember<PropertySymbol>("this[]").Parameters[1], "DecimalConstantAttribute", 2, false);
                    VerifyDefaultValueAttribute(type.GetMember<MethodSymbol>("get_Item").Parameters[1], "DecimalConstantAttribute", 2, false);
                    VerifyDefaultValueAttribute(type.GetMember<MethodSymbol>("set_Item").Parameters[1], "DecimalConstantAttribute", 2, false);
                    type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("D");
                    VerifyDefaultValueAttribute(type.GetMember<MethodSymbol>("Invoke").Parameters[0], "DecimalConstantAttribute", 3, false);
                    VerifyDefaultValueAttribute(type.GetMember<MethodSymbol>("BeginInvoke").Parameters[0], "DecimalConstantAttribute", 3, false);
                });
            var source2 =
@"class P
{
    static void Main()
    {
        var c = new C();
        Report(c.F());
        Report(c[0]);
        D d = C.D;
        Report(d());
    }
    static void Report(object o)
    {
        System.Console.WriteLine(o);   
    }
}";
            var comp2a = CreateCompilation(
                source2,
                references: new[] { new CSharpCompilationReference(comp1) },
                options: TestOptions.DebugExe);
            comp2a.VerifyDiagnostics();
            CompileAndVerify(comp2a, expectedOutput:
@"1
2
3");
            var comp2b = CreateCompilation(
                source2,
                references: new[] { MetadataReference.CreateFromStream(comp1.EmitToStream()) },
                options: TestOptions.DebugExe);
            comp2b.VerifyDiagnostics();
            CompileAndVerify(comp2b, expectedOutput:
@"1
2
3");
        }

        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [Fact]
        public void TestDuplicateConstantAttributesSameValues_PartialMethods()
        {
            var source =
@"using System.Runtime.CompilerServices;
partial class C
{
    static partial void F(decimal o = 2);
}
partial class C
{
    static partial void F([DecimalConstant(0, 0, 0, 0, 2)]decimal o) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, sourceSymbolValidator: module =>
                {
                    var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                    VerifyDefaultValueAttribute(type.GetMember<MethodSymbol>("F").Parameters[0], "DecimalConstantAttribute", 2, false);
                });
        }

        private static void VerifyDefaultValueAttribute(ParameterSymbol parameter, string expectedAttributeName, object expectedDefault, bool hasDefault)
        {
            var attributes = parameter.GetAttributes();
            if (expectedAttributeName == null)
            {
                Assert.Equal(0, attributes.Length);
            }
            else
            {
                Assert.Equal(1, attributes.Length);
                var attribute = attributes[0];
                var argument = attribute.ConstructorArguments.Last();
                Assert.Equal(expectedAttributeName, attribute.AttributeClass.Name);
                Assert.Equal(expectedDefault, argument.Value);
                Assert.Equal(hasDefault, ((Cci.IParameterDefinition)parameter.GetCciAdapter()).HasDefaultValue);
            }
            if (hasDefault)
            {
                Assert.Equal(expectedDefault, parameter.ExplicitDefaultValue);
            }
        }

        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [Fact]
        public void TestDuplicateConstantAttributesDifferentValues()
        {
            var source =
@"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
interface I
{
    void F1([DefaultParameterValue(1)]int o = 2);
    void F2([DefaultParameterValue(1)]decimal o = 2);
    void F4([DecimalConstant(0, 0, 0, 0, 1)]decimal o = 2);
    void F6([DateTimeConstant(1), DefaultParameterValue(1), DecimalConstant(0, 0, 0, 0, 1)]int o = 1);
    void F7([DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)]decimal o = 2);
    object this[int a, [DefaultParameterValue(1)]int o = 2] { get; set; }
    object this[[DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)]int o] { get; set; }
}
delegate void D([DecimalConstant(0, 0, 0, 0, 3)]decimal b = 4);
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,14): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                //     void F1([DefaultParameterValue(1)]int o = 2);
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue").WithLocation(5, 14),
                // (6,14): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                //     void F2([DefaultParameterValue(1)]decimal o = 2);
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue").WithLocation(6, 14),
                // (6,51): error CS8017: The parameter has multiple distinct default values.
                //     void F2([DefaultParameterValue(1)]decimal o = 2);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "2").WithLocation(6, 51),
                // (7,57): error CS8017: The parameter has multiple distinct default values.
                //     void F4([DecimalConstant(0, 0, 0, 0, 1)]decimal o = 2);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "2").WithLocation(7, 57),
                // (8,35): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                //     void F6([DateTimeConstant(1), DefaultParameterValue(1), DecimalConstant(0, 0, 0, 0, 1)]int o = 1);
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue").WithLocation(8, 35),
                // (8,61): error CS8017: The parameter has multiple distinct default values.
                //     void F6([DateTimeConstant(1), DefaultParameterValue(1), DecimalConstant(0, 0, 0, 0, 1)]int o = 1);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DecimalConstant(0, 0, 0, 0, 1)").WithLocation(8, 61),
                // (8,100): error CS8017: The parameter has multiple distinct default values.
                //     void F6([DateTimeConstant(1), DefaultParameterValue(1), DecimalConstant(0, 0, 0, 0, 1)]int o = 1);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "1").WithLocation(8, 100),
                // (9,35): error CS8017: The parameter has multiple distinct default values.
                //     void F7([DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)]decimal o = 2);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DecimalConstant(0, 0, 0, 0, 2)").WithLocation(9, 35),
                // (9,67): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                //     void F7([DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)]decimal o = 2);
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue").WithLocation(9, 67),
                // (9,104): error CS8017: The parameter has multiple distinct default values.
                //     void F7([DateTimeConstant(2), DecimalConstant(0, 0, 0, 0, 2), DefaultParameterValue(2)]decimal o = 2);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "2").WithLocation(9, 104),
                // (10,25): error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                //     object this[int a, [DefaultParameterValue(1)]int o = 2] { get; set; }
                Diagnostic(ErrorCode.ERR_DefaultValueUsedWithAttributes, "DefaultParameterValue").WithLocation(10, 25),
                // (10,58): error CS8017: The parameter has multiple distinct default values.
                //     object this[int a, [DefaultParameterValue(1)]int o = 2] { get; set; }
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "2").WithLocation(10, 58),
                // (11,44): error CS8017: The parameter has multiple distinct default values.
                //     object this[[DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)]int o] { get; set; }
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DecimalConstant(0, 0, 0, 0, 0)").WithLocation(11, 44),
                // (11,76): error CS8017: The parameter has multiple distinct default values.
                //     object this[[DefaultParameterValue(0), DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)]int o] { get; set; }
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DateTimeConstant(0)").WithLocation(11, 76),
                // (5,47): error CS8017: The parameter has multiple distinct default values.
                //     void F1([DefaultParameterValue(1)]int o = 2);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "2").WithLocation(5, 47),
                // (13,61): error CS8017: The parameter has multiple distinct default values.
                // delegate void D([DecimalConstant(0, 0, 0, 0, 3)]decimal b = 4);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "4").WithLocation(13, 61)
                );
        }

        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [Fact]
        public void TestDuplicateConstantAttributesDifferentValues_PartialMethods()
        {
            var source =
@"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
partial class C
{
    partial void F1([DefaultParameterValue(1)]int o) {}
    partial void F9([DefaultParameterValue(0)]int o);
}
partial class C
{
    partial void F1(int o = 2);
    partial void F9([DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)]int o) {}
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,22): error CS8017: The parameter has multiple distinct default values.
                //     partial void F9([DefaultParameterValue(0)]int o);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DefaultParameterValue(0)"),
                // (10,29): error CS8017: The parameter has multiple distinct default values.
                //     partial void F1(int o = 2);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "2"),
                // (11,54): error CS8017: The parameter has multiple distinct default values.
                //     partial void F9([DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)]int o) {}
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DateTimeConstant(0)"));
        }

        /// <summary>
        /// Should not report differences if either value is bad.
        /// </summary>
        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [Fact]
        public void TestDuplicateConstantAttributesDifferentValues_BadValue()
        {
            var source =
@"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
interface I
{
    void M1([DefaultParameterValue(typeof(C)), DecimalConstantAttribute(0, 0, 0, 0, 0)] decimal o);
    void M2([DefaultParameterValue(0), DecimalConstantAttribute(0, 0, 0, 0, typeof(C))] decimal o);
    void M3([DefaultParameterValue(0), DecimalConstantAttribute(0, 0, 0, 0, 0)] decimal o);
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,40): error CS8017: The parameter has multiple distinct default values.
                //     void M3([DefaultParameterValue(0), DecimalConstantAttribute(0, 0, 0, 0, 0)] decimal o);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DecimalConstantAttribute(0, 0, 0, 0, 0)").WithLocation(7, 40),
                // (6,84): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2([DefaultParameterValue(0), DecimalConstantAttribute(0, 0, 0, 0, typeof(C))] decimal o);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C").WithLocation(6, 84),
                // (5,43): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
                //     void M1([DefaultParameterValue(typeof(C)), DecimalConstantAttribute(0, 0, 0, 0, 0)] decimal o);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C").WithLocation(5, 43),
                // (5,48): error CS8017: The parameter has multiple distinct default values.
                //     void M1([DefaultParameterValue(typeof(C)), DecimalConstantAttribute(0, 0, 0, 0, 0)] decimal o);
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "DecimalConstantAttribute(0, 0, 0, 0, 0)").WithLocation(5, 48)
                );
        }

        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [Fact]
        public void TestExplicitConstantAttributesOnFields_Errors()
        {
            var source =
@"
using System;
using System.Runtime.CompilerServices;

class C
{
    [DecimalConstant(0, 0, 0, 0, 0)] public decimal F0 = 1;

    [DateTimeConstant(0)] public DateTime F1 = default(DateTime);

    [DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 0)] public DateTime F2 = default(DateTime);

    [DateTimeConstant(0), DateTimeConstant(0)] public DateTime F3 = default(DateTime);

    [DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 1)] public decimal F4 = 0;

    [DateTimeConstant(1), DateTimeConstant(0)] public DateTime F5 = default(DateTime);

    [DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)] public DateTime F6 = default(DateTime);

    [DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)] public decimal F7 = 0;

    [DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)] public int F8 = 0;

    [DecimalConstant(0, 0, 0, 0, 0)] public const int F9 = 0;

    [DateTimeConstant(0)] public const int F10 = 0;

    [DateTimeConstant(0)] public const decimal F12 = 0;

    [DecimalConstant(0, 0, 0, 0, 0)] public const decimal F14 = 1;
}";
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
// (11,38): error CS0579: Duplicate 'DecimalConstant' attribute
//     [DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 0)] public DateTime F2 = default(DateTime);
Diagnostic(ErrorCode.ERR_DuplicateAttribute, "DecimalConstant").WithArguments("DecimalConstant"),
// (13,27): error CS0579: Duplicate 'DateTimeConstant' attribute
//     [DateTimeConstant(0), DateTimeConstant(0)] public DateTime F3 = default(DateTime);
Diagnostic(ErrorCode.ERR_DuplicateAttribute, "DateTimeConstant").WithArguments("DateTimeConstant"),
// (15,38): error CS0579: Duplicate 'DecimalConstant' attribute
//     [DecimalConstant(0, 0, 0, 0, 0), DecimalConstant(0, 0, 0, 0, 1)] public decimal F4 = 0;
Diagnostic(ErrorCode.ERR_DuplicateAttribute, "DecimalConstant").WithArguments("DecimalConstant"),
// (17,27): error CS0579: Duplicate 'DateTimeConstant' attribute
//     [DateTimeConstant(1), DateTimeConstant(0)] public DateTime F5 = default(DateTime);
Diagnostic(ErrorCode.ERR_DuplicateAttribute, "DateTimeConstant").WithArguments("DateTimeConstant"),
// (19,38): error CS8027: The field has multiple distinct constant values.
//     [DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)] public DateTime F6 = default(DateTime);
Diagnostic(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, "DateTimeConstant(0)"),
// (21,38): error CS8027: The field has multiple distinct constant values.
//     [DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)] public decimal F7 = 0;
Diagnostic(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, "DateTimeConstant(0)"),
// (23,38): error CS8027: The field has multiple distinct constant values.
//     [DecimalConstant(0, 0, 0, 0, 0), DateTimeConstant(0)] public int F8 = 0;
Diagnostic(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, "DateTimeConstant(0)"),
// (25,6): error CS8027: The field has multiple distinct constant values.
//     [DecimalConstant(0, 0, 0, 0, 0)] public const int F9 = 0;
Diagnostic(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, "DecimalConstant(0, 0, 0, 0, 0)"),
// (27,6): error CS8027: The field has multiple distinct constant values.
//     [DateTimeConstant(0)] public const int F10 = 0;
Diagnostic(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, "DateTimeConstant(0)"),
// (29,6): error CS8027: The field has multiple distinct constant values.
//     [DateTimeConstant(0)] public const decimal F12 = 0;
Diagnostic(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, "DateTimeConstant(0)"),
// (31,6): error CS8027: The field has multiple distinct constant values.
//     [DecimalConstant(0, 0, 0, 0, 0)] public const decimal F14 = 1;
Diagnostic(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, "DecimalConstant(0, 0, 0, 0, 0)")
                );
        }

        [WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")]
        [Fact]
        public void TestExplicitConstantAttributesOnFields_Valid()
        {
            var source =
@"
using System;
using System.Runtime.CompilerServices;

class C
{
    [DecimalConstantAttribute(0, 128, 0, 0, 7)] public const decimal F15 = -7;
}";
            var comp = CreateCompilation(source);

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var field = (PEFieldSymbol)module.GlobalNamespace.GetTypeMember("C").GetField("F15");
                var attribute = ((PEModuleSymbol)module).GetCustomAttributesForToken(field.Handle).Single();

                Assert.Equal("System.Runtime.CompilerServices.DecimalConstantAttribute", attribute.AttributeClass.ToTestDisplayString());
            });
        }
    }
}
