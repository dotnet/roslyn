// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using PEParameterSymbol = Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEParameterSymbol;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_WellKnownAttributes : WellKnownAttributesTestBase
    {
        #region Misc

        [Fact]
        public void TestInteropAttributes01()
        {
            var source = CreateCompilationWithMscorlib(@"
using System;
using System.Runtime.InteropServices;

[assembly: ComCompatibleVersion(1, 2, 3, 4)]
[ComImport(), Guid(""ABCDEF5D-2448-447A-B786-64682CBEF123"")]
[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[TypeLibImportClass(typeof(object)), TypeLibType(TypeLibTypeFlags.FAggregatable)]
[BestFitMapping(false, ThrowOnUnmappableChar = true)]
public interface IFoo
{

    [AllowReversePInvokeCalls()]
    void DoSomething();
    [ComRegisterFunction()]

    void Register(object o);
    [ComUnregisterFunction()]

    void UnRegister();
    [TypeLibFunc(TypeLibFuncFlags.FDefaultBind)]
    void LibFunc();
}
class C
{
    public static void Main() {}
}
");

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var assembly = m.ContainingSymbol;

                // Assembly
                var attrs = assembly.GetAttributes();
                Assert.Equal(1, attrs.Length);
                var attrSym = attrs.First();
                Assert.Equal("ComCompatibleVersionAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(4, attrSym.CommonConstructorArguments.Length);
                Assert.Equal(0, attrSym.CommonNamedArguments.Length);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, 1);

                // get expected attr symbol
                var interopNS = Get_System_Runtime_InteropServices_NamespaceSymbol(m);

                var guidSym = interopNS.GetTypeMember("GuidAttribute");
                var ciSym = interopNS.GetTypeMember("ComImportAttribute");
                var iTypeSym = interopNS.GetTypeMember("InterfaceTypeAttribute");
                var itCtor = iTypeSym.Constructors.First();
                var tLibSym = interopNS.GetTypeMember("TypeLibImportClassAttribute");
                var tLTypeSym = interopNS.GetTypeMember("TypeLibTypeAttribute");
                var bfmSym = interopNS.GetTypeMember("BestFitMappingAttribute");

                // IFoo
                var ifoo = m.GlobalNamespace.GetTypeMember("IFoo");
                Assert.Equal(6, ifoo.GetAttributes().Length);

                // get attr by NamedTypeSymbol
                attrSym = ifoo.GetAttribute(ciSym);
                Assert.Equal("ComImportAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);
                Assert.Equal(0, attrSym.CommonNamedArguments.Length);

                attrSym = ifoo.GetAttribute(guidSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, "ABCDEF5D-2448-447A-B786-64682CBEF123");
                // get attr by ctor
                attrSym = ifoo.GetAttribute(itCtor);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)ComInterfaceType.InterfaceIsIUnknown);

                attrSym = ifoo.GetAttribute(tLibSym);
                attrSym.VerifyValue(0, TypedConstantKind.Type, typeof(object));

                attrSym = ifoo.GetAttribute(tLTypeSym);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)TypeLibTypeFlags.FAggregatable);

                attrSym = ifoo.GetAttribute(bfmSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, false);
                attrSym.VerifyNamedArgumentValue(0, "ThrowOnUnmappableChar", TypedConstantKind.Primitive, true);

                // =============================
                var mem = (MethodSymbol)ifoo.GetMembers("DoSomething").First();
                Assert.Equal(1, mem.GetAttributes().Length);
                attrSym = mem.GetAttributes().First();
                Assert.Equal("AllowReversePInvokeCallsAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                mem = (MethodSymbol)ifoo.GetMembers("Register").First();
                attrSym = mem.GetAttributes().First();
                Assert.Equal("ComRegisterFunctionAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                mem = (MethodSymbol)ifoo.GetMembers("UnRegister").First();
                Assert.Equal(1, mem.GetAttributes().Length);

                mem = (MethodSymbol)ifoo.GetMembers("LibFunc").First();
                attrSym = mem.GetAttributes().First();
                Assert.Equal(1, attrSym.CommonConstructorArguments.Length);
                // 32
                Assert.Equal(TypeLibFuncFlags.FDefaultBind, (TypeLibFuncFlags)attrSym.CommonConstructorArguments[0].Value);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [Fact]
        public void TestInteropAttributes02()
        {
            var source = CreateCompilationWithMscorlib(@"
using System;
using System.Runtime.InteropServices;

[assembly: PrimaryInteropAssembly(1, 2)]

[assembly: Guid(""1234C65D-1234-447A-B786-64682CBEF136"")]
[ComVisibleAttribute(false)]
[UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true, ThrowOnUnmappableChar = true)]
public delegate void DFoo(char p1, sbyte p2);

[ComDefaultInterface(typeof(object)), ProgId(""ProgId"")]
public class CFoo
{
    [DispIdAttribute(123)]
    [LCIDConversion(1), ComConversionLoss()]
    public void Method(sbyte p1, string p2)
    {
    }
}

[ComVisible(true), TypeIdentifier(""1234C65D-1234-447A-B786-64682CBEF136"", ""EFoo, InteropAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"")]
public enum EFoo
{
    One,
    [TypeLibVar(TypeLibVarFlags.FDisplayBind)]
    Two,
    [Obsolete(""message"", false)]
    Three
}
class C
{
    public static void Main() {}
}
");

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var assembly = m.ContainingSymbol;

                // get expected attr symbol
                NamespaceSymbol interopNS = Get_System_Runtime_InteropServices_NamespaceSymbol(m);

                var comvSym = interopNS.GetTypeMember("ComVisibleAttribute");
                var ufPtrSym = interopNS.GetTypeMember("UnmanagedFunctionPointerAttribute");
                var comdSym = interopNS.GetTypeMember("ComDefaultInterfaceAttribute");
                var pgidSym = interopNS.GetTypeMember("ProgIdAttribute");
                var tidSym = interopNS.GetTypeMember("TypeIdentifierAttribute");
                var dispSym = interopNS.GetTypeMember("DispIdAttribute");
                var lcidSym = interopNS.GetTypeMember("LCIDConversionAttribute");
                var comcSym = interopNS.GetTypeMember("ComConversionLossAttribute");

                var globalNS = m.GlobalNamespace;
                // delegate DFoo
                var type1 = globalNS.GetTypeMember("DFoo");
                Assert.Equal(2, type1.GetAttributes().Length);

                var attrSym = type1.GetAttribute(comvSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, false);

                attrSym = type1.GetAttribute(ufPtrSym);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)CallingConvention.StdCall);
                // 3

                attrSym.VerifyNamedArgumentValue(0, "BestFitMapping", TypedConstantKind.Primitive, true);
                attrSym.VerifyNamedArgumentValue(1, "CharSet", TypedConstantKind.Enum, (int)CharSet.Ansi);
                attrSym.VerifyNamedArgumentValue(2, "SetLastError", TypedConstantKind.Primitive, true);
                attrSym.VerifyNamedArgumentValue(3, "ThrowOnUnmappableChar", TypedConstantKind.Primitive, true);

                // class CFoo
                var type2 = globalNS.GetTypeMember("CFoo");
                Assert.Equal(2, type2.GetAttributes().Length);

                attrSym = type2.GetAttribute(comdSym);
                attrSym.VerifyValue(0, TypedConstantKind.Type, typeof(object));

                attrSym = type2.GetAttribute(pgidSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, "ProgId");

                var method = (MethodSymbol)type2.GetMembers("Method").First();
                attrSym = method.GetAttribute(dispSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, 123);

                attrSym = method.GetAttribute(lcidSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, 1);

                attrSym = method.GetAttribute(comcSym);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                //' enum EFoo
                var sourceAssembly = assembly as SourceAssemblySymbol;
                if (sourceAssembly != null)
                {
                    // Because this is a nopia local type it is only visible from the source assembly.
                    var type3 = globalNS.GetTypeMember("EFoo");
                    Assert.Equal(2, type3.GetAttributes().Length);

                    attrSym = type3.GetAttribute(comvSym);
                    attrSym.VerifyValue(0, TypedConstantKind.Primitive, true);

                    attrSym = type3.GetAttribute(tidSym);
                    attrSym.VerifyValue(1, TypedConstantKind.Primitive, "EFoo, InteropAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

                    var field = (FieldSymbol)type3.GetMembers("One").First();
                    Assert.Equal(0, field.GetAttributes().Length);

                    field = (FieldSymbol)type3.GetMembers("Two").First();
                    Assert.Equal(1, field.GetAttributes().Length);
                    attrSym = field.GetAttributes().First();
                    attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)TypeLibVarFlags.FDisplayBind);

                    field = (FieldSymbol)type3.GetMembers("Three").First();
                    attrSym = field.GetAttributes().First();
                    attrSym.VerifyValue(0, TypedConstantKind.Primitive, "message");
                    attrSym.VerifyValue(1, TypedConstantKind.Primitive, false);
                }
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator, symbolValidator: null);
        }

        [Fact]
        public void TestPseudoAttributes1()
        {
            #region "Source"
            var text = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[ComImport(), Guid(""6B29FC40-CA47-1067-B31D-00DD010662DA"")]
public interface IBar
{
    ulong Method1([OptionalAttribute(), DefaultParameterValue(99uL)]ref ulong v);

    string Method2([InAttribute(), Out(), DefaultParameterValue(""Ref"")]ref string v);

    object Method3(
        [InAttribute(), OptionalAttribute(), DefaultParameterValue(' ')]char v1, 
        [Out()][OptionalAttribute()][DefaultParameterValue(0f)]float v2, 
        [InAttribute()][OptionalAttribute()][DefaultParameterValue(null)]string v3);

    [PreserveSig()]
    void Method4(
        [DateTimeConstant(123456)]DateTime p1, 
        [DecimalConstant(0, 0, 100, 100, 100)]decimal p2, 
        [OptionalAttribute(), IDispatchConstant()]ref object p3);
}

[Serializable(), StructLayout(LayoutKind.Explicit, Size = 16, Pack = 8, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
public class CBar
{
    [NonSerialized(), MarshalAs(UnmanagedType.I8), FieldOffset(0)]
    public long field;
}

class C
{
    public static void Main() {}
}
";

            #endregion

            #region Verifier
            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var assembly = m.ContainingSymbol;

                // get expected attr symbol
                NamespaceSymbol sysNS = Get_System_NamespaceSymbol(m);
                NamespaceSymbol interopNS = Get_System_Runtime_InteropServices_NamespaceSymbol(sysNS);
                NamespaceSymbol compsrvNS = Get_System_Runtime_CompilerServices_NamespaceSymbol(sysNS);

                var serSym = sysNS.GetTypeMember("SerializableAttribute");
                var nosSym = sysNS.GetTypeMember("NonSerializedAttribute");

                var ciptSym = interopNS.GetTypeMember("ComImportAttribute");
                var laySym = interopNS.GetTypeMember("StructLayoutAttribute");
                var sigSym = interopNS.GetTypeMember("PreserveSigAttribute");
                var offSym = interopNS.GetTypeMember("FieldOffsetAttribute");
                var mshSym = interopNS.GetTypeMember("MarshalAsAttribute");


                var optSym = interopNS.GetTypeMember("OptionalAttribute");
                var inSym = interopNS.GetTypeMember("InAttribute");
                var outSym = interopNS.GetTypeMember("OutAttribute");
                // non pseudo
                var dtcSym = compsrvNS.GetTypeMember("DateTimeConstantAttribute");
                var dmcSym = compsrvNS.GetTypeMember("DecimalConstantAttribute");
                var iscSym = compsrvNS.GetTypeMember("IDispatchConstantAttribute");

                var globalNS = m.GlobalNamespace;
                // Interface IBar
                var type1 = globalNS.GetTypeMember("IBar");
                var attrSym = type1.GetAttribute(ciptSym);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                MethodSymbol method = default(MethodSymbol);
                ParameterSymbol parm = default(ParameterSymbol);
                var sourceAssembly = assembly as SourceAssemblySymbol;
                if (sourceAssembly != null)
                {
                    // Default attribute is in system.dll not mscorlib. Only do this check for source attributes.
                    var defvSym = interopNS.GetTypeMember("DefaultParameterValueAttribute");
                    method = type1.GetMember<MethodSymbol>("Method1");
                    parm = method.Parameters[0];
                    attrSym = parm.GetAttribute(defvSym);
                    attrSym.VerifyValue(0, TypedConstantKind.Primitive, 99uL);
                    attrSym = parm.GetAttribute(optSym);
                    Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                    method = type1.GetMember<MethodSymbol>("Method2");
                    parm = method.Parameters[0];
                    Assert.Equal(3, parm.GetAttributes().Length);
                    attrSym = parm.GetAttribute(defvSym);
                    attrSym.VerifyValue(0, TypedConstantKind.Primitive, "Ref");
                    attrSym = parm.GetAttribute(inSym);
                    Assert.Equal(0, attrSym.CommonConstructorArguments.Length);
                    attrSym = parm.GetAttribute(outSym);
                    Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                    method = type1.GetMember<MethodSymbol>("Method3");
                    parm = method.Parameters[1];
                    // v2
                    Assert.Equal(3, parm.GetAttributes().Length);
                    attrSym = parm.GetAttribute(defvSym);
                    attrSym.VerifyValue(0, TypedConstantKind.Primitive, 0f);
                    attrSym = parm.GetAttribute(optSym);
                    Assert.Equal(0, attrSym.CommonConstructorArguments.Length);
                    attrSym = parm.GetAttribute(outSym);
                    Assert.Equal(0, attrSym.CommonConstructorArguments.Length);
                }

                method = type1.GetMember<MethodSymbol>("Method4");
                attrSym = method.GetAttributes().First();
                Assert.Equal("PreserveSigAttribute", attrSym.AttributeClass.Name);

                parm = method.Parameters[0];
                attrSym = parm.GetAttributes().First();
                Assert.Equal("DateTimeConstantAttribute", attrSym.AttributeClass.Name);
                // attrSym.VerifyValue(0, TypedConstantKind.Primitive, 123456);

                parm = method.Parameters[1];
                attrSym = parm.GetAttributes().First();
                Assert.Equal("DecimalConstantAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(5, attrSym.CommonConstructorArguments.Length);
                attrSym.VerifyValue(2, TypedConstantKind.Primitive, 100);

                parm = method.Parameters[2];
                attrSym = parm.GetAttribute(iscSym);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                // class CBar
                var type2 = globalNS.GetTypeMember("CBar");
                Assert.Equal(2, type2.GetAttributes().Length);

                attrSym = type2.GetAttribute(serSym);
                Assert.Equal("SerializableAttribute", attrSym.AttributeClass.Name);

                attrSym = type2.GetAttribute(laySym);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)LayoutKind.Explicit);
                Assert.Equal(3, attrSym.CommonNamedArguments.Length);
                attrSym.VerifyNamedArgumentValue(0, "Size", TypedConstantKind.Primitive, 16);
                attrSym.VerifyNamedArgumentValue(1, "Pack", TypedConstantKind.Primitive, 8);
                attrSym.VerifyNamedArgumentValue(2, "CharSet", TypedConstantKind.Enum, (int)CharSet.Unicode);

                var field = (FieldSymbol)type2.GetMembers("field").First();
                Assert.Equal(3, field.GetAttributes().Length);
                attrSym = field.GetAttribute(nosSym);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);
                attrSym = field.GetAttribute(mshSym);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)UnmanagedType.I8);
                attrSym = field.GetAttribute(offSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, 0);
            };
            #endregion

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(text, additionalRefs: new[] { SystemRef }, sourceSymbolValidator: attributeValidator);
        }

        [Fact]
        public void TestDecimalConstantAttribute()
        {
            #region "Source"
            var text = @"
using System;
using System.Reflection;

namespace TestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var field in typeof(CCC).GetFields())
            {
                PrintAttribute(field);
            }
        }

        static void PrintAttribute(FieldInfo field )
        {
            var attr = field.GetCustomAttributesData()[0];
            Console.WriteLine(""{0}, {1}, {2}, {3}, {4}, {5}"",
                              attr.ConstructorArguments[0],
                              attr.ConstructorArguments[1],
                              attr.ConstructorArguments[2],
                              attr.ConstructorArguments[3],
                              attr.ConstructorArguments[4],
                              field.IsInitOnly);
        }
    }
}

public class CCC
{
    public const Decimal _Min = Decimal.MinValue;
    public const Decimal _Max = Decimal.MaxValue;
    public const Decimal _One = Decimal.One;
    public const Decimal _MinusOne = Decimal.MinusOne;
    public const Decimal _Zero = Decimal.Zero;
}";

            #endregion

            CompileAndVerify(
                text,
                additionalRefs: new[] { SystemRef },
                expectedOutput: @"
(Byte)0, (Byte)128, (UInt32)4294967295, (UInt32)4294967295, (UInt32)4294967295, True
(Byte)0, (Byte)0, (UInt32)4294967295, (UInt32)4294967295, (UInt32)4294967295, True
(Byte)0, (Byte)0, (UInt32)0, (UInt32)0, (UInt32)1, True
(Byte)0, (Byte)128, (UInt32)0, (UInt32)0, (UInt32)1, True
(Byte)0, (Byte)0, (UInt32)0, (UInt32)0, (UInt32)0, True");
        }

        #endregion

        #region DefaultParameterValueAttribute, OptionalAttribute

        [Fact]
        public void DPV_Decimal()
        {
            string source = @"
using System.Runtime.InteropServices;

public class C
{
    public static void f([Optional, DefaultParameterValue(default(decimal))]decimal a)
    {
    }
}
";
            CreateCompilationWithMscorlib(source, new[] { SystemRef }).VerifyDiagnostics(
                // (6,59): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "default(decimal)"));
        }

        [Fact]
        public void DPV_ImplicitConversion()
        {
            var source = @"
using System.Runtime.InteropServices;

public class C
{
    public void M([DefaultParameterValue((short)1)]int foo) 
    {
    }
}";

            Action<IModuleSymbol> verifier = (module) =>
            {
                var c = (NamedTypeSymbol)((ModuleSymbol)module).GlobalNamespace.GetMember("C");
                var m = (MethodSymbol)c.GetMember("M");
                var ps = m.GetParameters();

                //EDMAURER the language doesn't believe the parameter is optional and 
                //doesn't import the default parameter.
                Assert.False(ps[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => ps[0].ExplicitDefaultValue);

                var theParameter = (PEParameterSymbol)ps[0];
                object value = theParameter.ImportConstantValue().Value;

                Assert.True(value is short, "Expected value to be Int16");
                Assert.Equal((short)1, value);

                Assert.False(ps[0].IsOptional);
                Assert.Equal(0, ps[0].GetAttributes().Length);
            };

            CompileAndVerify(source, additionalRefs: new[] { SystemRef }, symbolValidator: verifier);
        }

        [Fact]
        public void DPV_String()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

public class C
{
    public void M([DefaultParameterValue(""default str"")]string str) 
    {
    }
}
", new[] { SystemRef });

            Action<ModuleSymbol> verifier = module =>
            {
                var c = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C");
                var m = (MethodSymbol)c.GetMember("M");
                var ps = m.GetParameters();

                var theParameter = (PEParameterSymbol)ps[0];
                Assert.Equal("default str", theParameter.ImportConstantValue().StringValue);

                Assert.False(ps[0].IsOptional);
                Assert.Equal(0, ps[0].GetAttributes().Length);
            };

            CompileAndVerify(compilation, symbolValidator: verifier);
        }

        [Fact]
        public void OptionalAttribute()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

public class C
{
    public void M([Optional]int i) 
    {
    }
}
", new[] { SystemRef });

            Action<ModuleSymbol> verifier = module =>
            {
                var c = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C");
                var m = (MethodSymbol)c.GetMember("M");
                var ps = m.GetParameters();

                Assert.False(ps[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => ps[0].ExplicitDefaultValue);
                Assert.True(ps[0].IsOptional);
                Assert.Equal(0, ps[0].GetAttributes().Length);
            };

            CompileAndVerify(compilation, symbolValidator: verifier);
        }

        [Fact]
        public void DPV_Optional_CallFromAnotherCompilation()
        {
            var c1 = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

public class C
{
    public int O([Optional]int i) 
    {
        return i;
    }

    public int D([DefaultParameterValue(1)]int i) 
    {
        return i;
    }

    public int OD([Optional, DefaultParameterValue(2)]int i) 
    {
        return i;
    }
}
", new[] { SystemRef });

            var c2 = CreateCompilationWithMscorlib(@"
public class D 
{
    public void M() 
    {
        C c = new C();
        c.O(10);
        c.O();
        c.D(20);
        // c.D();    ... can't call d with not arguments as it doesn't have [Optional] parameter
        c.OD(30);
        c.OD();
    }
}
", new[] { new CSharpCompilationReference(c1) });

            c2.VerifyDiagnostics();
        }

        [Fact]
        public void CustomDefaultParameterValueAttribute1()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class DefaultParameterValueAttribute : Attribute
	{
		public DefaultParameterValueAttribute()
		{
		}
	}
}

public class C 
{
	public static void M([Optional, DefaultParameterValue]int i) 
	{
	}

	public static void Main() 
	{
		M();
	}
}
");
            Action<ModuleSymbol> verifier = module =>
            {
                var c = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C");
                var m = (MethodSymbol)c.GetMember("M");
                var ps = m.GetParameters();

                // DPV is ignore if it has invalid signature
                Assert.False(ps[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => ps[0].ExplicitDefaultValue);
                Assert.True(ps[0].IsOptional);
            };

            CompileAndVerify(compilation, symbolValidator: verifier);
        }

        [Fact]
        public void CustomDefaultParameterValueAttribute2()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class DefaultParameterValueAttribute : Attribute
	{
		public DefaultParameterValueAttribute(object value, object trueValue)
		{
		}
	}
}

public class C 
{
	public static void M([Optional, DefaultParameterValue(null, 1)]int i) 
	{
	}

	public static void Main() 
	{
		M();
	}
}
");
            Action<ModuleSymbol> verifier = module =>
            {
                var c = (NamedTypeSymbol)module.GlobalNamespace.GetMember("C");
                var m = (MethodSymbol)c.GetMember("M");
                var ps = m.GetParameters();

                // DPV is ignore if it has invalid signature
                Assert.False(ps[0].HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(() => ps[0].ExplicitDefaultValue);
                Assert.True(ps[0].IsOptional);
            };

            CompileAndVerify(compilation, symbolValidator: verifier);
        }

        [Fact]
        public void DPV_Optional_Indexers()
        {
            string source = @"
using System.Runtime.InteropServices;

public class C
{
    public int this[[Optional, DefaultParameterValue(1)]int a, int b = 2, [Optional, DefaultParameterValue(null)]params int[] args]
    {
        get { return 0; }

        [param: Optional, DefaultParameterValue(3)]
        set {  }
}
}";
            CompileAndVerify(source, new[] { SystemRef }, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var paramDef in metadataReader.GetParameters())
                {
                    var param = metadataReader.GetParameter(paramDef);
                    Assert.Equal(ParameterAttributes.Optional | ParameterAttributes.HasDefault, param.Attributes);
                }

                foreach (var handle in metadataReader.GetConstants())
                {
                    var constant = metadataReader.GetConstant(handle);
                    var paramRow = metadataReader.GetParameter((ParameterHandle)constant.Parent);
                    string name = metadataReader.GetString(paramRow.Name);

                    byte[] expectedConstant;
                    switch (name)
                    {
                        case "args":
                            expectedConstant = new byte[] { 0x00, 0x00, 0x00, 0x00 };
                            break;

                        case "a":
                            expectedConstant = new byte[] { 0x01, 0x00, 0x00, 0x00 };
                            break;

                        case "b":
                            expectedConstant = new byte[] { 0x02, 0x00, 0x00, 0x00 };
                            break;

                        case "value":
                            expectedConstant = new byte[] { 0x03, 0x00, 0x00, 0x00 };
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    var actual = metadataReader.GetBlobBytes(constant.Value);
                    AssertEx.Equal(expectedConstant, actual);
                }
            });
        }

        [Fact]
        public void DPV_Optional_Delegates()
        {
            string source = @"
using System.Runtime.InteropServices;

public delegate void D([Optional, DefaultParameterValue(1)]ref int a, int b = 2, [Optional, DefaultParameterValue(null)]params int[] args);
";
            // Dev11: doesn't allow DPV(null) on int[], we do.

            CompileAndVerify(source, new[] { SystemRef }, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var methodHandle in metadataReader.MethodDefinitions)
                {
                    var methodDef = metadataReader.GetMethodDefinition(methodHandle);
                    string methodName = metadataReader.GetString(methodDef.Name);

                    foreach (var paramDef in methodDef.GetParameters())
                    {
                        var paramRow = metadataReader.GetParameter(paramDef);
                        string paramName = metadataReader.GetString(paramRow.Name);

                        ParameterAttributes expectedFlags;
                        string completeName = methodName + "." + paramName;
                        switch (completeName)
                        {
                            case "BeginInvoke.a":
                            case "BeginInvoke.args":
                            case "EndInvoke.a":
                            case "Invoke.a":
                            case "Invoke.b":
                            case "Invoke.args":
                                expectedFlags = ParameterAttributes.Optional | ParameterAttributes.HasDefault;
                                break;

                            case ".ctor.object":
                            case ".ctor.method":
                            case "BeginInvoke.b":
                            case "BeginInvoke.callback":
                            case "BeginInvoke.object":
                            case "EndInvoke.result":
                                expectedFlags = 0;
                                break;

                            default:
                                throw TestExceptionUtilities.UnexpectedValue(completeName);
                        }

                        Assert.Equal(expectedFlags, paramRow.Attributes);
                    }
                }

                foreach (var handle in metadataReader.GetConstants())
                {
                    var constant = metadataReader.GetConstant(handle);
                    var paramRow = metadataReader.GetParameter((ParameterHandle)constant.Parent);
                    string name = metadataReader.GetString(paramRow.Name);

                    byte[] expectedConstant;
                    switch (name)
                    {
                        case "a":
                            expectedConstant = new byte[] { 0x01, 0x00, 0x00, 0x00 };
                            break;

                        case "args":
                            expectedConstant = new byte[] { 0x00, 0x00, 0x00, 0x00 };  // null
                            break;

                        case "b":
                            expectedConstant = new byte[] { 0x02, 0x00, 0x00, 0x00 };
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    var actual = metadataReader.GetBlobBytes(constant.Value);
                    AssertEx.Equal(expectedConstant, actual);
                }
            });
        }

        [Fact]
        public void OptionalAttribute_AttributeArrayParameter()
        {
            var text = @"
using System;
using System.Runtime.InteropServices;

[A]
public class A : Attribute
{
    public A([Optional]int[] a)
    {
    }
}";

            CompileAndVerify(text);
        }

        [Fact]
        public void DefaultParameterValue_Null()
        {
            var text = @"using System.Runtime.InteropServices;
public class C { }
public class D { }

public interface ISomeInterface
{
    void Test1([DefaultParameterValue(null)]int[] arg);
    void Test2([DefaultParameterValue(null)]System.Type arg);
    void Test3([DefaultParameterValue(null)]System.Array arg);
    void Test4([DefaultParameterValue(null)]C arg);
    void Test5([DefaultParameterValue((C)null)]D arg);
    void Test6<T>([DefaultParameterValue(null)]T arg) where T : class;
}
";
            // Dev10 reports CS1909, we don't
            CompileAndVerify(text, additionalRefs: new[] { SystemRef });
        }

        [Fact, WorkItem(544934, "DevDiv")]
        public void Bug13129()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
class C
{
    static void Foo([Optional][DefaultParameterValue(5)] decimal? x)
    {
        Console.WriteLine(x);
    }
    static void Main()
    {
        Foo();
    }
}";
            CompileAndVerify(source, additionalRefs: new[] { SystemRef }, expectedOutput: @"5");
        }

        [Fact]
        public void OptionalParameterInTheMiddle()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;
using System;

public class X
{  
    public int InTheMiddle(int a, [Optional, DefaultParameterValue((short)1)]int b, int c){
        return 2;
    } 
}", new[] { SystemRef });

            CompileAndVerify(compilation);
        }

        [Fact]
        public void OptionalAttributeParameter_Numeric()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class X : Attribute
{
	public X(int x, int y, int z, [System.Runtime.InteropServices.Optional]int w)
	{
	}
}

public class C 
{
	public static void M([X(0, z: 2, y: 1)]int i) 
	{
	}
}
");
            CompileAndVerify(compilation);
        }

        [Fact]
        public void OptionalAttributeParameter_Enum()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;

public enum E { A, B, C }

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class X : Attribute
{
	public X(int x, [System.Runtime.InteropServices.Optional]int y, int z, [System.Runtime.InteropServices.Optional]E w)
	{
	}
}

public class C 
{
	public static void M([X(x:0, z: 2)]int i) 
	{
	}
}
");
            CompileAndVerify(compilation);
        }

        [Fact, WorkItem(546785, "DevDiv")]
        public void OptionalAttributeOnPartialMethodParameters()
        {
            var source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

partial class C
{
    partial void Foo([Optional] int x);
    partial void Foo([DefaultParameterValue(0)] int x) { }

    partial void Foo2([DefaultParameterValue(0)] int x);
    partial void Foo2([Optional] int x) { }

    partial void Foo3([Optional][DefaultParameterValue(0)] int x);
    partial void Foo3(int x) { }

    partial void Foo4(int x);
    partial void Foo4([Optional][DefaultParameterValue(0)] int x) { }
}
";
            Action<SourceMemberMethodSymbol> partialValidator = (SourceMemberMethodSymbol sourceMethod) =>
            {
                Assert.True(sourceMethod.IsPartial, "Not a partial method?");

                MethodSymbol partialDefPart = sourceMethod.IsPartialDefinition ? sourceMethod : sourceMethod.PartialDefinitionPart;
                var param = (SourceParameterSymbol)partialDefPart.Parameters[0];
                Assert.True(param.HasOptionalAttribute, "No OptionalAttribute?");

                MethodSymbol partialImplPart = sourceMethod.IsPartialImplementation ? sourceMethod : sourceMethod.PartialImplementationPart;
                param = (SourceParameterSymbol)partialImplPart.Parameters[0];
                Assert.True(param.HasOptionalAttribute, "No OptionalAttribute?");
            };

            Action<ModuleSymbol> sourceValidator = (ModuleSymbol m) =>
            {
                var typeC = m.GlobalNamespace.GetTypeMember("C");

                var sourceMethod = typeC.GetMember<SourceMemberMethodSymbol>("Foo");
                partialValidator(sourceMethod);

                sourceMethod = typeC.GetMember<SourceMemberMethodSymbol>("Foo2");
                partialValidator(sourceMethod);

                sourceMethod = typeC.GetMember<SourceMemberMethodSymbol>("Foo3");
                partialValidator(sourceMethod);

                sourceMethod = typeC.GetMember<SourceMemberMethodSymbol>("Foo4");
                partialValidator(sourceMethod);
            };

            CompileAndVerify(source, additionalRefs: new[] { SystemRef }, sourceSymbolValidator: sourceValidator);
        }

        [WorkItem(544303, "DevDiv")]
        [Fact]
        public void OptionalAttributeBindingCycle()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
 
[Foo]
public class Foo: Attribute
{
    public Foo([Optional][Foo]int y) {}
    public static void Main() {}
}";

            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void OptionalAttributeBindingCycle_02()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    public class OptionalAttribute: Attribute
    {
        public OptionalAttribute(bool isOpt = true) {}
    }
}
 
public class Foo: Attribute
{
    public Foo([Optional(isOpt: false)][Foo]int y) {}
    public static void Main() {}
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (15,17): warning CS0436: The type 'System.Runtime.InteropServices.OptionalAttribute' in '' conflicts with the imported type 'System.Runtime.InteropServices.OptionalAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public Foo([Optional(isOpt: false)][Foo]int y) {}
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Optional").WithArguments("", "System.Runtime.InteropServices.OptionalAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Runtime.InteropServices.OptionalAttribute").WithLocation(15, 17),
                // (15,41): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'Foo.Foo(int)'
                //     public Foo([Optional(isOpt: false)][Foo]int y) {}
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Foo").WithArguments("y", "Foo.Foo(int)").WithLocation(15, 41));
        }

        [Fact]
        public void OptionalAttributeBindingCycle_03()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class)]
    public class OptionalAttribute: Attribute
    {
        public OptionalAttribute() {}
    }
}
 
public class Foo: Attribute
{
    public Foo([Optional][Foo]int y) {}
    public static void Main() {}
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (16,17): warning CS0436: The type 'System.Runtime.InteropServices.OptionalAttribute' in '' conflicts with the imported type 'System.Runtime.InteropServices.OptionalAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public Foo([Optional][Foo]int y) {}
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Optional").WithArguments("", "System.Runtime.InteropServices.OptionalAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Runtime.InteropServices.OptionalAttribute"),
                // (16,17): error CS0592: Attribute 'Optional' is not valid on this declaration type. It is only valid on 'class' declarations.
                //     public Foo([Optional][Foo]int y) {}
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Optional").WithArguments("Optional", "class"));
        }

        [Fact]
        public void OptionalAttributeBindingCycle_04()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class)]
    public class OptionalAttribute: Attribute
    {
        public OptionalAttribute(object o) {}
    }
}
 
public class Foo: Attribute
{
    public Foo([Optional(new Foo())][Foo]int y) {}
    public static void Main() {}
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (16,17): warning CS0436: The type 'System.Runtime.InteropServices.OptionalAttribute' in '' conflicts with the imported type 'System.Runtime.InteropServices.OptionalAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public Foo([Optional(new Foo())][Foo]int y) {}
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Optional").WithArguments("", "System.Runtime.InteropServices.OptionalAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Runtime.InteropServices.OptionalAttribute").WithLocation(16, 17),
                // (16,30): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'Foo.Foo(int)'
                //     public Foo([Optional(new Foo())][Foo]int y) {}
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Foo").WithArguments("y", "Foo.Foo(int)").WithLocation(16, 30),
                // (16,38): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'Foo.Foo(int)'
                //     public Foo([Optional(new Foo())][Foo]int y) {}
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Foo").WithArguments("y", "Foo.Foo(int)").WithLocation(16, 38));
        }

        [Fact, WorkItem(546624, "DevDiv")]
        public void DPV_Optional_Valid()
        {
            string source = @"
using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
 
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustomAttribute : Attribute
{
    public MyCustomAttribute(SecurityAction action, [Optional][MarshalAs(UnmanagedType.Interface)]object x)
    {
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustom2Attribute : Attribute
{
    public MyCustom2Attribute(SecurityAction action, [Optional]object x)
    {
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustom3Attribute : Attribute
{
    public MyCustom3Attribute([Optional]object x)
    {
    }
}


[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustom4Attribute : Attribute
{
    public MyCustom4Attribute([Optional]SecurityAction x)
    {
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustom7Attribute : Attribute
{
    public MyCustom7Attribute(SecurityAction action, [Optional]int x)
    {
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustom10Attribute: Attribute
{
    public MyCustom10Attribute(SecurityAction x, [Optional][DefaultParameterValueAttribute(SecurityAction.Demand)]object y)
    {
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustom11Attribute: Attribute
{
    public MyCustom11Attribute([Optional][DefaultParameterValue(SecurityAction.Demand)]SecurityAction x)
    {
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyCustom12Attribute: Attribute
{
    public MyCustom12Attribute(SecurityAction action, [Optional][DefaultParameterValue(null)]object x)
    {
    }
}

[MyCustom(SecurityAction.Demand, null)]

[MyCustom2(SecurityAction.Demand, null)]

[MyCustom3(SecurityAction.Demand)]

[MyCustom4Attribute]
[MyCustom4(SecurityAction.Demand)]

[MyCustom7(SecurityAction.Demand, 0)]

[MyCustom10(SecurityAction.Demand, null)]
[MyCustom10(SecurityAction.Demand, SecurityAction.Demand)]

[MyCustom11()]
[MyCustom11(SecurityAction.Demand)]

[MyCustom12(SecurityAction.Demand)]
[MyCustom12(SecurityAction.Demand, 0)]

class C
{
    public static void Main()
    {
        typeof(C).GetCustomAttributes(false);
    }
}
";
            CompileAndVerify(source, additionalRefs: new[] { MscorlibRef, SystemRef }, options: TestOptions.ReleaseExe, expectedOutput: "");
        }

        [Fact, WorkItem(546624, "DevDiv")]
        public void CS7067ERR_BadAttributeParamDefaultArgument()
        {
            string source = @"
using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
 
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyPermissionAttribute : CodeAccessSecurityAttribute
{
    public MyPermissionAttribute(SecurityAction action, [Optional][MarshalAs(UnmanagedType.Interface)]object x) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyPermission2Attribute : CodeAccessSecurityAttribute
{
    public MyPermission2Attribute(SecurityAction action, [Optional]object x) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyPermission3Attribute : CodeAccessSecurityAttribute
{
    public MyPermission3Attribute([Optional]object x) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}


[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyPermission4Attribute : CodeAccessSecurityAttribute
{
    public MyPermission4Attribute([Optional]SecurityAction x) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyPermission5Attribute : CodeAccessSecurityAttribute
{
    public MyPermission5Attribute(SecurityAction action, object x = SecurityAction.Demand) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyPermission6Attribute : CodeAccessSecurityAttribute
{
    public MyPermission6Attribute(object x = SecurityAction.Demand) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class MyPermission7Attribute : CodeAccessSecurityAttribute
{
    public MyPermission7Attribute(SecurityAction action, [Optional]int x) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

class MyPermission8Attribute : CodeAccessSecurityAttribute
{
    public MyPermission8Attribute([Optional][DefaultParameterValueAttribute(null)]SecurityAction x) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

class MyPermission9Attribute : CodeAccessSecurityAttribute
{
    public MyPermission9Attribute([Optional][DefaultParameterValueAttribute(-1)]SecurityAction x) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}

class MyPermission10Attribute : CodeAccessSecurityAttribute
{
    public MyPermission10Attribute(SecurityAction x, [Optional][DefaultParameterValueAttribute(SecurityAction.Demand)]object y) : base(SecurityAction.Demand)
    {
    }
 
    public override IPermission CreatePermission()
    {
        return null;
    }
}



[MyPermission(SecurityAction.Demand)]
[MyPermission(SecurityAction.Demand, null)]

[MyPermission2(SecurityAction.Demand)]
[MyPermission2(SecurityAction.Demand, null)]

[MyPermission3()]
[MyPermission3(SecurityAction.Demand)]
[MyPermission3(null)]

[MyPermission4()]
[MyPermission4(SecurityAction.Demand)]
[MyPermission4(null)]

[MyPermission5(SecurityAction.Demand)]
[MyPermission5(SecurityAction.Demand, null)]

[MyPermission6()]
[MyPermission6(SecurityAction.Demand)]
[MyPermission6(null)]

[MyPermission7(SecurityAction.Demand)]
[MyPermission7(SecurityAction.Demand, 0)]

[MyPermission8()]
[MyPermission8(SecurityAction.Demand)]

[MyPermission9()]
[MyPermission9(SecurityAction.Demand)]

[MyPermission10(SecurityAction.Demand)]
[MyPermission10(SecurityAction.Demand, null)]

class C
{
   public static void Main() { }
}
";

            CreateCompilationWithMscorlib(source, references: new[] { SystemRef }).VerifyDiagnostics(
                // (63,65): error CS1763: 'x' is of type 'object'. A default parameter value of a reference type other than string can only be initialized with null
                //     public MyPermission5Attribute(SecurityAction action, object x = SecurityAction.Demand) : base(SecurityAction.Demand)
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "x").WithArguments("x", "object"),
                // (76,42): error CS1763: 'x' is of type 'object'. A default parameter value of a reference type other than string can only be initialized with null
                //     public MyPermission6Attribute(object x = SecurityAction.Demand) : base(SecurityAction.Demand)
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "x").WithArguments("x", "object"),
                // (101,46): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //     public MyPermission8Attribute([Optional][DefaultParameterValueAttribute(null)]SecurityAction x) : base(SecurityAction.Demand)
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValueAttribute"),
                // (113,46): error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                //     public MyPermission9Attribute([Optional][DefaultParameterValueAttribute(-1)]SecurityAction x) : base(SecurityAction.Demand)
                Diagnostic(ErrorCode.ERR_DefaultValueTypeMustMatch, "DefaultParameterValueAttribute"),
                // (137,2): error CS7067: Attribute constructor parameter 'x' is optional, but no default parameter value was specified.
                // [MyPermission(SecurityAction.Demand)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamDefaultArgument, "MyPermission").WithArguments("x"),
                // (140,2): error CS7067: Attribute constructor parameter 'x' is optional, but no default parameter value was specified.
                // [MyPermission2(SecurityAction.Demand)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamDefaultArgument, "MyPermission2").WithArguments("x"),
                // (143,2): error CS7067: Attribute constructor parameter 'x' is optional, but no default parameter value was specified.
                // [MyPermission3()]
                Diagnostic(ErrorCode.ERR_BadAttributeParamDefaultArgument, "MyPermission3").WithArguments("x"),
                // (149,16): error CS1503: Argument 1: cannot convert from '<null>' to 'System.Security.Permissions.SecurityAction'
                // [MyPermission4(null)]
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "System.Security.Permissions.SecurityAction"),
                // (167,2): error CS1763: 'y' is of type 'object'. A default parameter value of a reference type other than string can only be initialized with null
                // [MyPermission10(SecurityAction.Demand)]
                Diagnostic(ErrorCode.ERR_NotNullRefDefaultParameter, "MyPermission10(SecurityAction.Demand)").WithArguments("y", "object"),
                // (145,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MyPermission3(null)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MyPermission3"),
                // (147,2): error CS7049: Security attribute 'MyPermission4' has an invalid SecurityAction value '0'
                // [MyPermission4()]
                Diagnostic(ErrorCode.ERR_SecurityAttributeInvalidAction, "MyPermission4()").WithArguments("MyPermission4", "0"),
                // (156,2): error CS7048: First argument to a security attribute must be a valid SecurityAction
                // [MyPermission6(null)]
                Diagnostic(ErrorCode.ERR_SecurityAttributeMissingAction, "MyPermission6"));
        }

        [Fact]
        [WorkItem(1036356, "DevDiv")]
        public void EnumAsDefaultParameterValue()
        {
            const string source = @"
using System;
using System.Runtime.InteropServices;

class Program
{
    static void Foo([Optional][DefaultParameterValue(DayOfWeek.Monday)] Enum x) 
    {
    }

    static void Main()
    {
        Foo();
    }
}";
            var comp = CreateCompilationWithMscorlib(source, references: new[] { SystemRef });
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS0029: Cannot implicitly convert type 'int' to 'Enum'
                //         Foo();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Foo()").WithArguments("int", "System.Enum").WithLocation(13, 9));
        }

        #endregion

        #region DecimalConstantAttribute

        [Fact, WorkItem(544438, "DevDiv"), WorkItem(538206, "DevDiv")]
        public void DefaultParameterValueIntToObj()
        {
            // The native compiler's behavior:
            // It does honour int default values in attributes whether the parameter 
            // is int or object, and whether the attributes appear in source or metadata.
            // The native compiler does NOT honor decimal and datetime attributes in source
            // but does honour them in metadata.
            //
            // Roslyn removes this inconsistency; we honour the decimal and datetime
            // attributes whether they appear in metadata or source.

            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class Parent
{
    public void Foo1([Optional][DecimalConstant(0, 0, (uint)0, (uint)0, (uint)100)] decimal i)
    {
        Console.Write(i);
    }

    public void Foo3([Optional][DateTimeConstant(200)] DateTime dt)
    {
        Console.Write(dt.Ticks);
    }

    public void Foo4([Optional][DefaultParameterValue(300)] int i)
    {
        Console.Write(i);
    }

    public void Foo5([Optional][DefaultParameterValue(400)] object i)
    {
        Console.Write(i);
    }
}

class Test
{
    public static void Main()
    {
        var p = new Parent();
        p.Foo1();
        p.Foo3();
        p.Foo4();
        p.Foo5();
    }
}
";
            CompileAndVerify(source, additionalRefs: new[] { SystemRef }, expectedOutput: @"100200300400");
        }

        [WorkItem(544516, "DevDiv")]
        [Fact]
        public void DecimalConstantAttributesAsMetadata()
        {
            var source = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
public class MyClass
{
    //should have DecimalConstantAttribute.
    public const decimal DecI1 = 10000.213213M;
    //should not have DecimalConstantAttribute.
    public static readonly decimal DecI2 = 10000.213213M;
    public static void Main()
{
        FieldInfo fi = typeof(MyClass).GetField(""DecI1"");
        object[] Attrs = fi.GetCustomAttributes(false);
        if (Attrs != null && Attrs.Length == 1 && Attrs[0].GetType() == typeof(DecimalConstantAttribute))
    {
            Console.WriteLine(""Has DecimalConstantAttribute"");
    }
        fi = typeof(MyClass).GetField(""DecI2"");
        Attrs = fi.GetCustomAttributes(false);
        if (Attrs == null || Attrs.Length == 0)
    {
            Console.WriteLine(""No DecimalConstantAttribute"");
        }
    }
}";

            CompileAndVerify(source, expectedOutput: @"Has DecimalConstantAttribute
No DecimalConstantAttribute");
        }

        [Fact]
        public void DecimalConstant_Indexers()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class C
{
    public decimal this[[Optional, DecimalConstant(1,2,3,4,5)]decimal a, decimal b = 2m]
    {
        get { return 0; }

        [param: Optional, DecimalConstant(10,20,30,40,50)]
        set {  }
    }
}
";
            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("C", "get_Item",
                    ".method public hidebysig specialname instance System.Decimal get_Item(" +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(1, 2, 3, 4, 5)] [opt] System.Decimal a = -5534023223830852403.7, " +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(0, 0, 0, 0, 2)] [opt] System.Decimal b = 2) " +
                    "cil managed"),

                Signature("C", "set_Item",
                    ".method public hidebysig specialname instance System.Void set_Item(" +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(1, 2, 3, 4, 5)] [opt] System.Decimal a = -5534023223830852403.7, " +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(0, 0, 0, 0, 2)] [opt] System.Decimal b = 2, " +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(10, 20, 30, 40, 50)] [opt] System.Decimal value = -55340232238.3085240370) " +
                    "cil managed")
            });
        }

        [Fact]
        public void DecimalConstant_Delegates()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public delegate void D([Optional, DecimalConstantAttribute(hi: 3, sign: 2, mid: 4, low: 5, scale: 1)]ref decimal a, decimal b = 2m);
";
            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("D", "BeginInvoke",
                    ".method public hidebysig newslot virtual instance System.IAsyncResult BeginInvoke(" +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(1, 2, 3, 4, 5)] [opt] System.Decimal& a = -5534023223830852403.7, " +
                    "System.Decimal b, " +
                    "System.AsyncCallback callback, " +
                    "System.Object object) " +
                    "runtime managed"),

                Signature("D", "EndInvoke",
                    ".method public hidebysig newslot virtual instance System.Void EndInvoke(" +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(1, 2, 3, 4, 5)] [opt] System.Decimal& a = -5534023223830852403.7, " +
                    "System.IAsyncResult result) " +
                    "runtime managed"),

                Signature("D", "Invoke",
                    ".method public hidebysig newslot virtual instance System.Void Invoke(" +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(1, 2, 3, 4, 5)] [opt] System.Decimal& a = -5534023223830852403.7, " +
                    "[System.Runtime.CompilerServices.DecimalConstantAttribute(0, 0, 0, 0, 2)] [opt] System.Decimal b = 2) " +
                    "runtime managed")
            });
        }

        #endregion

        #region InAttribute, OutAttribute

        [Fact]
        public void InOutAttributes()
        {
            var source = @"
using System.Runtime.InteropServices;

class C
{
    public static void M1([In]ref int a, [In] int b, [In]params object[] c) { throw null; }
    public static void M2([Out]out int d, [Out] int e, [Out]params object[] f) { throw null; }
    public static void M3([In, Out]ref int g, [In, Out] int h, [In, Out]params object[] i) { throw null; }
    public static void M4([In]int j = 1, [Out]int k = 2, [In, Out]int l = 3) { throw null; }
    public static void M5(int m, out int n, ref int o) { throw null; }
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                Assert.Equal(15, metadataReader.GetTableRowCount(TableIndex.Param));

                foreach (var paramDef in metadataReader.GetParameters())
                {
                    var row = metadataReader.GetParameter(paramDef);
                    string name = metadataReader.GetString(row.Name);
                    ParameterAttributes expectedFlags;

                    switch (name)
                    {
                        case "m":
                        case "o":
                            expectedFlags = 0;
                            break;

                        case "a":
                        case "b":
                        case "c":
                            expectedFlags = ParameterAttributes.In;
                            break;

                        case "d":
                        case "e":
                        case "f":
                        case "n":
                            expectedFlags = ParameterAttributes.Out;
                            break;

                        case "g":
                        case "h":
                        case "i":
                            expectedFlags = ParameterAttributes.In | ParameterAttributes.Out;
                            break;

                        case "j":
                            expectedFlags = ParameterAttributes.In | ParameterAttributes.HasDefault | ParameterAttributes.Optional;
                            break;

                        case "k":
                            expectedFlags = ParameterAttributes.Out | ParameterAttributes.HasDefault | ParameterAttributes.Optional;
                            break;

                        case "l":
                            expectedFlags = ParameterAttributes.In | ParameterAttributes.Out | ParameterAttributes.HasDefault | ParameterAttributes.Optional;
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    Assert.Equal(expectedFlags, row.Attributes);
                }
            });
        }

        [Fact]
        public void InOutAttributes_Errors()
        {
            var source = @"
using System.Runtime.InteropServices;

class C
{
    public static void E1([In]out int b) { }
    public static void E2([Out]ref int a) { }
    public static void E3([In, Out]out int a) { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,28): error CS0036: An out parameter cannot have the In attribute
                Diagnostic(ErrorCode.ERR_InAttrOnOutParam, "In"),
                // (7,27): error CS0662: Cannot specify only Out attribute on a ref parameter. Use both In and Out attributes, or neither.
                Diagnostic(ErrorCode.ERR_OutAttrOnRefParam, "a"),
                // (8,28): error CS0036: An out parameter cannot have the In attribute
                Diagnostic(ErrorCode.ERR_InAttrOnOutParam, "In"),
                // (6,24): error CS0177: The out parameter 'b' must be assigned to before control leaves the current method
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "E1").WithArguments("b"),
                // (8,24): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "E3").WithArguments("a"));
        }

        [Fact]
        public void InOutAttributes_Delegates()
        {
            var source = @"
using System.Runtime.InteropServices;

public delegate int F([Out]int a, [In]int b, [In, Out]ref int c, [In]ref int d, ref int e, [Out]out int f, out int g);
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var paramDef in metadataReader.GetParameters())
                {
                    var row = metadataReader.GetParameter(paramDef);
                    string name = metadataReader.GetString(row.Name);
                    ParameterAttributes expectedFlags;

                    switch (name)
                    {
                        case "e":
                        case "callback":
                        case "object":
                        case "method":
                        case "result":
                            expectedFlags = 0;
                            break;

                        case "b":
                        case "d":
                            expectedFlags = ParameterAttributes.In;
                            break;

                        case "a":
                        case "g":
                        case "f":
                            expectedFlags = ParameterAttributes.Out;
                            break;

                        case "c":
                            expectedFlags = ParameterAttributes.In | ParameterAttributes.Out;
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    Assert.Equal(expectedFlags, row.Attributes);
                }
            });
        }

        [Fact]
        public void InOutAttributes_Indexers()
        {
            var source = @"
using System.Runtime.InteropServices;

public class C
{
    public int this[[Out]int a, [In]int b, [In, Out]int c, int d] {  get { return 0; }  set { } }
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var paramDef in metadataReader.GetParameters())
                {
                    var row = metadataReader.GetParameter(paramDef);
                    string name = metadataReader.GetString(row.Name);
                    ParameterAttributes expectedFlags;

                    switch (name)
                    {
                        case "d":
                        case "value":
                            expectedFlags = 0;
                            break;

                        case "b":
                            expectedFlags = ParameterAttributes.In;
                            break;

                        case "a":
                            expectedFlags = ParameterAttributes.Out;
                            break;

                        case "c":
                            expectedFlags = ParameterAttributes.In | ParameterAttributes.Out;
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    Assert.Equal(expectedFlags, row.Attributes);
                }
            });
        }

        [Fact]
        public void InOutAttributes_Accessors()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

class C
{
    event Action E
    {
        [param: In, Out]
        add { }

        [param: In, Out]
        remove { }
    }
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                ParameterHandle[] ps = metadataReader.GetParameters().ToArray();
                Assert.Equal(2, ps.Length);
                Assert.Equal(ParameterAttributes.In | ParameterAttributes.Out, metadataReader.GetParameter(ps[0]).Attributes);
                Assert.Equal(ParameterAttributes.In | ParameterAttributes.Out, metadataReader.GetParameter(ps[1]).Attributes);
            });
        }

        #endregion

        #region DllImportAttribute, MethodImplAttribute, DefaultCharSetAttribute

        [Fact]
        public void TestPseudoDllImport()
        {
            var source = CreateCompilationWithMscorlib(@"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

/// PreserveSigAttribute: automatically insert by compiler
public class DllImportTest
{
    //Metadata - .method public static pinvokeimpl(""unmanaged.dll"" lasterr fastcall)
    //            void  DllImportSub() cil managed preservesig
    [DllImport(""unmanaged.dll"", CallingConvention = CallingConvention.FastCall, SetLastError = true)]
    public static extern void DllImportSub();

    // Metadata  .method public static pinvokeimpl(""user32.dll"" unicode winapi) 
    //              int32  MessageBox(native int hwnd,  string t,  string caption, uint32 t2) cil managed preservesig
    //
    // MSDN has table for 'default' ExactSpelling value
    //   C#|C++: always 'false'
    //   VB: true if CharSet is ANSI|UniCode; otherwise false
    [DllImportAttribute(""user32.dll"", CharSet = CharSet.Unicode, ExactSpelling = false, EntryPoint = ""MessageBox"")]
    public static extern int MessageBox(IntPtr hwnd, string t, string caption, UInt32 t2);
}
class C
{
    public static void Main() {}
}
");

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                // get expected attr symbol
                var type1 = m.GlobalNamespace.GetTypeMember("DllImportTest");

                MethodSymbol method = default(MethodSymbol);
                method = type1.GetMember<MethodSymbol>("DllImportSub");
                var attrSym = method.GetAttributes().First();
                Assert.Equal("DllImportAttribute", attrSym.AttributeClass.Name);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, "unmanaged.dll");

                attrSym.VerifyNamedArgumentValue(0, "CallingConvention", TypedConstantKind.Enum, (int)CallingConvention.FastCall);
                attrSym.VerifyNamedArgumentValue(1, "SetLastError", TypedConstantKind.Primitive, true);

                method = (MethodSymbol)type1.GetMembers("MessageBox").First();
                attrSym = method.GetAttributes().First();
                Assert.Equal("DllImportAttribute", attrSym.AttributeClass.Name);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, "user32.dll");

                attrSym.VerifyNamedArgumentValue(0, "CharSet", TypedConstantKind.Enum, (int)CharSet.Unicode);
                attrSym.VerifyNamedArgumentValue(1, "ExactSpelling", TypedConstantKind.Primitive, false);
                attrSym.VerifyNamedArgumentValue(2, "EntryPoint", TypedConstantKind.Primitive, "MessageBox");
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator);
        }

        [Fact]
        [WorkItem(544180, "DevDiv"), WorkItem(545030, "DevDiv")]
        public void DllImport_AttributeRedefinition()
        {
            var source = @"
namespace System.Runtime.InteropServices
{
    [DllImport]
    public class DllImportAttribute { }
}
";

            // NOTE (tomat)
            // Dev10 reports:
            //   warning CS1685: The predefined type 'System.Runtime.InteropServices.DllImportAttribute' is defined in multiple assemblies in the global alias; 
            //                   using definition from 'c:\Windows\Microsoft.NET\Framework\v4.0.30319\mscorlib.dll'
            //   error CS0616: 'System.Runtime.InteropServices.DllImportAttribute' is not an attribute class
            // 
            // DllImportAttribute is defined both in source and PE.
            // Both Dev10 and Roslyn correctly bind to the source symbol.
            // Dev10 generates incorrect warning CS1685.
            // CONSIDER: We may want to generate warning CS0436 in Roslyn:
            //      (4,6): warning CS0436: The type 'DllImport' in '' conflicts with the imported type 'System.Runtime.InteropServices.DllImportAttribute' in 
            //      'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,6): error CS0616: 'System.Runtime.InteropServices.DllImportAttribute' is not an attribute class
                //     [DllImport]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "DllImport").WithArguments("System.Runtime.InteropServices.DllImportAttribute"));
        }

        [Fact]
        public void DllImport_InvalidArgs1()
        {
            var source = @"
using System.Runtime.InteropServices;

class C
{
    [DllImport(null)]
    public extern static void F1();

    [DllImport("""")]
    public extern static void F2();

    [DllImport(""foo"", EntryPoint = null)]
    public extern static void F3();

    [DllImport(""foo"", EntryPoint = """")]
    public extern static void F4();

    [DllImport(null, EntryPoint = null)]
    public extern static void F5();

    [DllImport(""\0"")]
    static extern void Empty1();

    [DllImport(""\0b"")]
    static extern void Empty2();

    [DllImport(""b\0"")]
    static extern void Empty3();

    [DllImport(""x\0y"")]
    static extern void Empty4();

    [DllImport(""x"", EntryPoint = ""x\0y"")]
    static extern void Empty5();

    [DllImport(""\uD800"")]
    static extern void LeadingSurrogate();

    [DllImport(""\uDC00"")]
    static extern void TrailingSurrogate();

    [DllImport(""\uDC00\uD800"")]
    static extern void ReversedSurrogates1();

    [DllImport(""x"", EntryPoint = ""\uDC00\uD800"")]
    static extern void ReversedSurrogates2();
}
";
            // Dev10 fails in Emit or emits invalid metadata
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "null").WithArguments("DllImport"),
                // (9,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""""").WithArguments("DllImport"),
                // (12,23): error CS0599: Invalid value for named attribute argument 'EntryPoint'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "EntryPoint = null").WithArguments("EntryPoint"),
                // (15,23): error CS0599: Invalid value for named attribute argument 'EntryPoint'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, @"EntryPoint = """"").WithArguments("EntryPoint"),
                // (18,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "null").WithArguments("DllImport"),
                // (18,22): error CS0599: Invalid value for named attribute argument 'EntryPoint'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "EntryPoint = null").WithArguments("EntryPoint"),
                // (21,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""\0""").WithArguments("DllImport"),
                // (24,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""\0b""").WithArguments("DllImport"),
                // (27,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""b\0""").WithArguments("DllImport"),
                // (30,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""x\0y""").WithArguments("DllImport"),
                // (33,21): error CS0599: Invalid value for named attribute argument 'EntryPoint'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, @"EntryPoint = ""x\0y""").WithArguments("EntryPoint"),
                // (36,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""\uD800""").WithArguments("DllImport"),
                // (39,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""\uDC00""").WithArguments("DllImport"),
                // (42,16): error CS0591: Invalid value for argument to 'DllImport' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""\uDC00\uD800""").WithArguments("DllImport"),
                // (45,21): error CS0599: Invalid value for named attribute argument 'EntryPoint'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, @"EntryPoint = ""\uDC00\uD800""").WithArguments("EntryPoint"));
        }

        [Fact]
        public void DllImport_SpecialCharactersInName()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
 
class Program
{
    [DllImport(""\uFFFF"")]
    static extern void InvalidCharacter();

    [DllImport(""\uD800\uDC00"")]
    static extern void SurrogatePairMin();

    [DllImport(""\uDBFF\uDFFF"")]
    static extern void SurrogatePairMax();
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                Assert.Equal(3, metadataReader.GetTableRowCount(TableIndex.ModuleRef));
                Assert.Equal(3, metadataReader.GetTableRowCount(TableIndex.ImplMap));

                foreach (var method in metadataReader.GetImportedMethods())
                {
                    var import = method.GetImport();
                    string moduleName = metadataReader.GetString(metadataReader.GetModuleReference(import.Module).Name);
                    string methodName = metadataReader.GetString(method.Name);
                    switch (methodName)
                    {
                        case "InvalidCharacter":
                            Assert.Equal("\uFFFF", moduleName);
                            break;

                        case "SurrogatePairMin":
                            Assert.Equal("\uD800\uDC00", moduleName);
                            break;

                        case "SurrogatePairMax":
                            Assert.Equal("\uDBFF\uDFFF", moduleName);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(methodName);
                    }
                }
            });
        }

        [Fact]
        [WorkItem(544176, "DevDiv")]
        public void TestPseudoAttributes_DllImport_AllTrue()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class C
{
    [DllImport(""mscorlib"", 
            EntryPoint = ""bar"", 
            CallingConvention = CallingConvention.Cdecl, 
            CharSet = CharSet.Unicode, 
            ExactSpelling = true, 
            PreserveSig = true,
            SetLastError = true, 
            BestFitMapping = true,
            ThrowOnUnmappableChar = true)]
    public static extern void M();
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                // ModuleRef:
                var moduleRefName = metadataReader.GetModuleReference(metadataReader.GetModuleReferences().Single()).Name;
                Assert.Equal("mscorlib", metadataReader.GetString(moduleRefName));

                // FileRef:
                // Although the Metadata spec says there should be a File entry for each ModuleRef entry 
                // Dev10 compiler doesn't add it and peverify doesn't complain.
                Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.File));

                // ImplMap:
                Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ImplMap));

                var import = metadataReader.GetImportedMethods().Single().GetImport();
                Assert.Equal("bar", metadataReader.GetString(import.Name));
                Assert.Equal(1, metadataReader.GetRowNumber(import.Module));
                Assert.Equal(
                    MethodImportAttributes.ExactSpelling |
                    MethodImportAttributes.CharSetUnicode |
                    MethodImportAttributes.SetLastError |
                    MethodImportAttributes.CallingConventionCDecl |
                    MethodImportAttributes.BestFitMappingEnable |
                    MethodImportAttributes.ThrowOnUnmappableCharEnable, import.Attributes);

                // MethodDef:
                MethodDefinitionHandle[] methodDefs = metadataReader.MethodDefinitions.AsEnumerable().ToArray();
                Assert.Equal(2, methodDefs.Length); // M, ctor
                Assert.Equal(MethodImplAttributes.PreserveSig, metadataReader.GetMethodDefinition(methodDefs[0]).ImplAttributes);
            },
            symbolValidator: module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMember<MethodSymbol>("M");
                var info = m.GetDllImportData();

                Assert.Equal("mscorlib", info.ModuleName);
                Assert.Equal("bar", info.EntryPointName);
                Assert.Equal(CharSet.Unicode, info.CharacterSet);
                Assert.True(info.ExactSpelling);
                Assert.True(info.SetLastError);
                Assert.Equal(true, info.BestFitMapping);
                Assert.Equal(true, info.ThrowOnUnmappableCharacter);

                Assert.Equal(
                    Cci.PInvokeAttributes.NoMangle |
                    Cci.PInvokeAttributes.CharSetUnicode |
                    Cci.PInvokeAttributes.SupportsLastError |
                    Cci.PInvokeAttributes.CallConvCdecl |
                    Cci.PInvokeAttributes.BestFitEnabled |
                    Cci.PInvokeAttributes.ThrowOnUnmappableCharEnabled, ((Cci.IPlatformInvokeInformation)info).Flags);
            });
        }

        [Fact]
        [WorkItem(544601, "DevDiv")]
        public void GetDllImportData_UnspecifiedProperties()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class C
{
    [DllImport(""mscorlib"")]
    public static extern void M();

    public static void N() { }
}
";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var m = c.GetMember<MethodSymbol>("M");
                var info = m.GetDllImportData();

                Assert.Equal("mscorlib", info.ModuleName);
                Assert.Equal(isFromSource ? null : "M", info.EntryPointName);
                Assert.Equal(CharSet.None, info.CharacterSet);
                Assert.Equal(CallingConvention.Winapi, info.CallingConvention);
                Assert.False(info.ExactSpelling);
                Assert.False(info.SetLastError);
                Assert.Equal(null, info.BestFitMapping);
                Assert.Equal(null, info.ThrowOnUnmappableCharacter);

                var n = c.GetMember<MethodSymbol>("N");
                Assert.Null(n.GetDllImportData());
            };

            CompileAndVerify(source, sourceSymbolValidator: validator(true), symbolValidator: validator(false));
        }

        [Fact]
        public void TestPseudoAttributes_DllImport_OperatorsAndAccessors()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class C
{
    [DllImport(""foo"")]
    public static extern int operator +(C a, C b);

    public extern static int F 
    { 
        [DllImport(""a"")]get;
        [DllImport(""b"")]set;
    }

    [method: DllImport(""c"")]
    public extern static event System.Action G;
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                // no backing fields should be generated -- all members are "extern" members:
                Assert.Equal(0, metadataReader.FieldDefinitions.AsEnumerable().Count());

                Assert.Equal(4, metadataReader.GetTableRowCount(TableIndex.ModuleRef));
                Assert.Equal(5, metadataReader.GetTableRowCount(TableIndex.ImplMap));
                var visitedEntryPoints = new Dictionary<string, bool>();

                foreach (var method in metadataReader.GetImportedMethods())
                {
                    string moduleName = metadataReader.GetString(metadataReader.GetModuleReference(method.GetImport().Module).Name);
                    string entryPointName = metadataReader.GetString(method.Name);
                    switch (entryPointName)
                    {
                        case "op_Addition":
                            Assert.Equal("foo", moduleName);
                            break;

                        case "get_F":
                            Assert.Equal("a", moduleName);
                            break;

                        case "set_F":
                            Assert.Equal("b", moduleName);
                            break;

                        case "add_G":
                        case "remove_G":
                            Assert.Equal("c", moduleName);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(entryPointName);
                    }

                    // This throws if we visit one entry point name twice.
                    // We used to incorrectly share entry point name among event accessors.
                    visitedEntryPoints.Add(entryPointName, true);
                }

                Assert.Equal(5, visitedEntryPoints.Count);
            });
        }

        [Fact]
        public void TestPseudoAttributes_DllImport_Flags()
        {
            var cases = new[] {
                // calling convention:
                new { attr = MakeDllImport(), expected = MethodImportAttributes.CallingConventionWinApi},                                        // M0
                new { attr = MakeDllImport(cc: CallingConvention.Cdecl), expected = MethodImportAttributes.CallingConventionCDecl },             // M1
                new { attr = MakeDllImport(cc: CallingConvention.FastCall), expected = MethodImportAttributes.CallingConventionFastCall },       // M2
                new { attr = MakeDllImport(cc: CallingConvention.StdCall), expected = MethodImportAttributes.CallingConventionStdCall },         // M3
                new { attr = MakeDllImport(cc: CallingConvention.ThisCall), expected = MethodImportAttributes.CallingConventionThisCall },       // M4
                new { attr = MakeDllImport(cc: CallingConvention.Winapi), expected = MethodImportAttributes.CallingConventionWinApi },           // M5

                // charset & exact spelling:                                                                                              
                new { attr = MakeDllImport(), expected = MethodImportAttributes.CallingConventionWinApi },                         // M6
                new { attr = MakeDllImport(charSet: CharSet.None), expected = MethodImportAttributes.CallingConventionWinApi },    // M7
                new { attr = MakeDllImport(charSet: CharSet.Ansi), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.CharSetAnsi },       // M8
                new { attr = MakeDllImport(charSet: CharSet.Unicode), expected =MethodImportAttributes.CallingConventionWinApi |  MethodImportAttributes.CharSetUnicode }, // M9
                new { attr = MakeDllImport(charSet: CharSet.Auto), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.CharSetAuto },       // M10

                new { attr = MakeDllImport(exactSpelling: true), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.ExactSpelling },            // M11
                new { attr = MakeDllImport(exactSpelling: false), expected = MethodImportAttributes.CallingConventionWinApi },                                      // M12

                new { attr = MakeDllImport(charSet: CharSet.Ansi, exactSpelling: true), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.ExactSpelling | MethodImportAttributes.CharSetAnsi },      // M13
                new { attr = MakeDllImport(charSet: CharSet.Ansi, exactSpelling: false), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.CharSetAnsi },                                // M14
                new { attr = MakeDllImport(charSet: CharSet.Unicode, exactSpelling: true), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.ExactSpelling | MethodImportAttributes.CharSetUnicode },// M15
                new { attr = MakeDllImport(charSet: CharSet.Unicode, exactSpelling: false), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.CharSetUnicode },                          // M16
                new { attr = MakeDllImport(charSet: CharSet.Auto, exactSpelling: true), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.ExactSpelling | MethodImportAttributes.CharSetAuto },      // M17
                new { attr = MakeDllImport(charSet: CharSet.Auto, exactSpelling: false), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.CharSetAuto },                                // M18

                // preservesig:
                new { attr = MakeDllImport(preserveSig: true), expected = MethodImportAttributes.CallingConventionWinApi},                                           // M19
                new { attr = MakeDllImport(preserveSig: false), expected = MethodImportAttributes.CallingConventionWinApi},                                          // M20

                // setLastError:
                new { attr = MakeDllImport(setLastError: true), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.SetLastError},      // M21
                new { attr = MakeDllImport(setLastError: false), expected = MethodImportAttributes.CallingConventionWinApi},                                         // M22

                // bestFitMapping:
                new { attr = MakeDllImport(bestFitMapping: true), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.BestFitMappingEnable},       // M23
                new { attr = MakeDllImport(bestFitMapping: false), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.BestFitMappingDisable},     // M24

                // throwOnUnmappableChar:
                new { attr = MakeDllImport(throwOnUnmappableChar: true), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.ThrowOnUnmappableCharEnable},       // M23
                new { attr = MakeDllImport(throwOnUnmappableChar: false), expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.ThrowOnUnmappableCharDisable},     // M24

                // invalid enum values (ignored)
                new { attr = "[DllImport(\"bar\", CharSet = (CharSet)15, SetLastError = true)]",
                      expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.SetLastError }, // M25

                // invalid enum values (ignored)
                new { attr = "[DllImport(\"bar\", CallingConvention = (CallingConvention)15, SetLastError = true)]",
                      expected = MethodImportAttributes.CallingConventionWinApi | MethodImportAttributes.SetLastError }, // M26
};

            StringBuilder sb = new StringBuilder(@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class C
{
");
            int i = 0;
            foreach (var testCase in cases)
            {
                sb.Append(testCase.attr);
                sb.AppendLine();
                sb.Append("static extern void M" + (i++) + "();");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            var code = sb.ToString();

            CompileAndVerify(code, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();
                Assert.Equal(cases.Length, metadataReader.GetTableRowCount(TableIndex.ImplMap));

                int j = 0;
                foreach (var method in metadataReader.GetImportedMethods())
                {
                    Assert.Equal(cases[j].expected, method.GetImport().Attributes);
                    j++;
                }
            });
        }

        private string MakeDllImport(
            CallingConvention? cc = null,
            CharSet? charSet = null,
            bool? exactSpelling = null,
            bool? preserveSig = null,
            bool? setLastError = null,
            bool? bestFitMapping = null,
            bool? throwOnUnmappableChar = null)
        {
            StringBuilder sb = new StringBuilder("[DllImport(\"bar\"");
            if (cc != null)
            {
                sb.Append(", CallingConvention = CallingConvention.");
                sb.Append(cc.Value);
            }

            if (charSet != null)
            {
                sb.Append(", CharSet = CharSet.");
                sb.Append(charSet.Value);
            }

            if (exactSpelling != null)
            {
                sb.Append(", ExactSpelling = ");
                sb.Append(exactSpelling.Value ? "true" : "false");
            }

            if (preserveSig != null)
            {
                sb.Append(", PreserveSig = ");
                sb.Append(preserveSig.Value ? "true" : "false");
            }

            if (setLastError != null)
            {
                sb.Append(", SetLastError = ");
                sb.Append(setLastError.Value ? "true" : "false");
            }

            if (bestFitMapping != null)
            {
                sb.Append(", BestFitMapping = ");
                sb.Append(bestFitMapping.Value ? "true" : "false");
            }

            if (throwOnUnmappableChar != null)
            {
                sb.Append(", ThrowOnUnmappableChar = ");
                sb.Append(throwOnUnmappableChar.Value ? "true" : "false");
            }

            sb.Append(")]");
            return sb.ToString();
        }

        [WorkItem(544238, "DevDiv")]
        [WorkItem(544163, "DevDiv")]
        [Fact]
        public void DllImport_InvalidCharsetValue_Null()
        {
            var source = @"
using System.Runtime.InteropServices;

class M
{
    [DllImport(""1"", CharSet = null)]
    static extern int F1();

    [DllImport(""2"", CharSet = CharSet)]
    static extern int F2();

    [DllImport(""3"", CharSet = ""str"")]
    static extern int F3();

    [DllImport(true)]
    static extern int F4();

    [DllImport(CharSet)]
    static extern int F5();

    [DllImport(1)]
    static extern int F6();
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,31): error CS0037: Cannot convert null to 'System.Runtime.InteropServices.CharSet' because it is a non-nullable value type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Runtime.InteropServices.CharSet"),
                // (9,31): error CS0119: 'System.Runtime.InteropServices.CharSet' is a type, which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "CharSet").WithArguments("System.Runtime.InteropServices.CharSet", "type"),
                // (12,31): error CS0029: Cannot implicitly convert type 'string' to 'System.Runtime.InteropServices.CharSet'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""str""").WithArguments("string", "System.Runtime.InteropServices.CharSet"),
                // (15,16): error CS1503: Argument 1: cannot convert from 'bool' to 'string'
                Diagnostic(ErrorCode.ERR_BadArgType, "true").WithArguments("1", "bool", "string"),
                // (18,16): error CS0119: 'System.Runtime.InteropServices.CharSet' is a type, which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "CharSet").WithArguments("System.Runtime.InteropServices.CharSet", "type"),
                // (21,16): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string"));
        }

        [Fact]
        public void TestMethodImplAttribute_VerifiableMD()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

abstract class C
{
    [MethodImpl(MethodImplOptions.ForwardRef)]
    public static void ForwardRef()  { System.Console.WriteLine(0); }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NoInlining() { System.Console.WriteLine(1); }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void NoOptimization() { System.Console.WriteLine(2); }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void Synchronized() { System.Console.WriteLine(3); }

    [MethodImpl(MethodImplOptions.InternalCall)]
    public extern static void InternalCallStatic();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public extern void InternalCallInstance();

    [MethodImpl(MethodImplOptions.InternalCall)]
    public abstract void InternalCallAbstract();
}
";
            Action<PEAssembly> validator = (assembly) =>
            {
                var peReader = assembly.GetMetadataReader();
                foreach (var methodHandle in peReader.MethodDefinitions)
                {
                    var methodDef = peReader.GetMethodDefinition(methodHandle);
                    var actualFlags = methodDef.ImplAttributes;
                    MethodImplAttributes expectedFlags;

                    string methodName = peReader.GetString(methodDef.Name);
                    switch (methodName)
                    {
                        case "NoInlining": expectedFlags = MethodImplAttributes.NoInlining; break;
                        case "NoOptimization": expectedFlags = MethodImplAttributes.NoOptimization; break;
                        case "Synchronized": expectedFlags = MethodImplAttributes.Synchronized; break;

                        case "InternalCallStatic":
                        case "InternalCallInstance":
                        case "InternalCallAbstract":
                            expectedFlags = MethodImplAttributes.InternalCall;
                            break;

                        case "ForwardRef":
                            expectedFlags = MethodImplAttributes.ForwardRef;
                            break;

                        case ".ctor": expectedFlags = MethodImplAttributes.IL; break;
                        default: throw TestExceptionUtilities.UnexpectedValue(methodName);
                    }

                    Assert.Equal(expectedFlags, actualFlags);
                }
            };

            CompileAndVerify(source, assemblyValidator: validator);
        }

        [Fact]
        public void TestMethodImplAttribute_UnverifiableMD()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Runtime.CompilerServices;

class C
{
    [MethodImpl(MethodImplOptions.Unmanaged)]
    public static void Unmanaged()  { System.Console.WriteLine(1); }          // peverify: type load failed
                                                                              
    [MethodImpl(MethodCodeType = MethodCodeType.Native)]                      
    public static void Native()  { System.Console.WriteLine(2); }             // peverify: type load failed
                                                                              
    [MethodImpl(MethodCodeType = MethodCodeType.OPTIL)]                       
    public static void OPTIL()  { System.Console.WriteLine(3); }              // peverify: type load failed

    [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
    public static void Runtime() { System.Console.WriteLine(4); }             // peverify: Runtime method shouldn't have a body

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static void InternalCallStatic() { System.Console.WriteLine(5); }  // peverify: InternalCall method shouldn't have a body

    [MethodImpl(MethodImplOptions.InternalCall)]
    public extern static void InternalCallGeneric1<T>();                      // peverify: type load failed (InternalCall method can't be generic)
}

class C<T>
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public extern static void InternalCallGeneric2();                         // peverify: type load failed (InternalCall method can't be in a generic type)
}
");
            compilation.VerifyDiagnostics();

            var image = compilation.EmitToStream();

            using (var metadata = ModuleMetadata.CreateFromStream(image))
            {
                var metadataReader = metadata.MetadataReader;
                foreach (var methodHandle in metadataReader.MethodDefinitions)
                {
                    var methodDef = metadataReader.GetMethodDefinition(methodHandle);
                    var actualFlags = methodDef.ImplAttributes;
                    MethodImplAttributes expectedFlags;

                    string methodName = metadataReader.GetString(methodDef.Name);
                    switch (methodName)
                    {
                        case "Unmanaged": expectedFlags = MethodImplAttributes.Unmanaged; break;
                        case "Native": expectedFlags = MethodImplAttributes.Native; break;
                        case "Runtime": expectedFlags = MethodImplAttributes.Runtime; break;
                        case "OPTIL": expectedFlags = MethodImplAttributes.OPTIL; break;
                        case ".ctor": expectedFlags = MethodImplAttributes.IL; break;
                        case "InternalCallStatic":
                        case "InternalCallGeneric1":
                        case "InternalCallGeneric2": expectedFlags = MethodImplAttributes.InternalCall; break;
                        default: throw TestExceptionUtilities.UnexpectedValue(methodName);
                    }

                    Assert.Equal(expectedFlags, actualFlags);
                }
            }
        }

        [Fact]
        public void TestMethodImplAttribute_PreserveSig()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

abstract class C
{
    C() {}

    [PreserveSig]
    abstract public void f0();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    abstract public void f1();

    [DllImport(""foo"")]
    public extern static void f2();

    [DllImport(""foo"", PreserveSig=true)]
    public extern static void f3();

    // false
    [DllImport(""foo"", PreserveSig=false)]
    public extern static void f4();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""foo"", PreserveSig=true)]
    public extern static void f5();

    // false
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""foo"", PreserveSig=false)]
    public extern static void f6();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [PreserveSig]
    abstract public void f7();

    [DllImport(""foo"")]
    [PreserveSig]
    public extern static void f8();

    [PreserveSig]
    [DllImport(""foo"", PreserveSig=true)]
    public extern static void f9();

    [DllImport(""foo"", PreserveSig=false)]
    [PreserveSig]
    public extern static void f10();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""foo"", PreserveSig=true)]
    [PreserveSig]
    public extern static void f11();

    [DllImport(""foo"", PreserveSig=false)]
    [PreserveSig]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    public extern static void f12();

    [DllImport(""foo"", PreserveSig=false)]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [PreserveSig]
    public extern static void f13();

    [PreserveSig]
    [DllImport(""foo"", PreserveSig=false)]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    public extern static void f14();

    // false
    [PreserveSig]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""foo"", PreserveSig=false)]
    public extern static void f15();

    // false
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [PreserveSig]
    [DllImport(""foo"", PreserveSig=false)]
    public extern static void f16();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""foo"", PreserveSig=false)]
    [PreserveSig]
    public extern static void f17();
    
    public static void f18() {}

    [MethodImpl(MethodImplOptions.Synchronized)]
    [DllImport(""foo"", PreserveSig=false)]
    [PreserveSig]
    public extern static void f19();

    [PreserveSig]
    [DllImport(""foo"")]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public extern static void f20();

    [PreserveSig]
    [DllImport(""foo"", PreserveSig=false)]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public extern static void f21();
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var peReader = assembly.GetMetadataReader();
                foreach (var methodHandle in peReader.MethodDefinitions)
                {
                    var row = peReader.GetMethodDefinition(methodHandle);
                    var actualFlags = row.ImplAttributes;
                    MethodImplAttributes expectedFlags;
                    var name = peReader.GetString(row.Name);

                    switch (name)
                    {
                        case ".ctor":
                        case "f4":
                        case "f6":
                        case "f15":
                        case "f16":
                        case "f18":
                            expectedFlags = 0;
                            break;

                        case "f0":
                        case "f1":
                        case "f2":
                        case "f3":
                        case "f5":
                        case "f7":
                        case "f8":
                        case "f9":
                        case "f10":
                        case "f11":
                        case "f12":
                        case "f13":
                        case "f14":
                        case "f17":
                            expectedFlags = MethodImplAttributes.PreserveSig;
                            break;

                        case "f19":
                        case "f20":
                            expectedFlags = MethodImplAttributes.PreserveSig | MethodImplAttributes.Synchronized;
                            break;

                        case "f21":
                            expectedFlags = MethodImplAttributes.Synchronized;
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    Assert.Equal(expectedFlags, actualFlags);
                }

                // no custom attributes applied on methods:
                foreach (var ca in peReader.CustomAttributes)
                {
                    var ctor = peReader.GetCustomAttribute(ca).Constructor;
                    Assert.NotEqual(ctor.Kind, HandleKind.MethodDefinition);
                }
            });
        }

        [Fact]
        public void TestMethodImplAttribute_PreserveSig_Invalid()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class C
{
    [MethodImpl(MethodImplOptions.PreserveSig)]
    public static void f1() { }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void MethodImplAttribute_Errors()
        {
            string source = @"
using System.Runtime.CompilerServices;

class Program1
{
    [MethodImpl((short)0)]              // ok
    void f0() { }

    [MethodImpl(1)]                     // error
    void f1() { }
                                        
    [MethodImpl(2)]                     // error
    void f2() { }                     
                                       
    [MethodImpl(3)]                     // error
    void f3() { }                     
                                      
    [MethodImpl(4)]                     // ok
    void f4() { }                      
                                      
    [MethodImpl(5)]                     // error
    void f5() { } 

    [MethodImpl((MethodImplOptions)2)]  // error
    void f6() { } 

    [MethodImpl(4, MethodCodeType = (MethodCodeType)8, MethodCodeType = (MethodCodeType)9)]  // errors
    void f7() { } 
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,17): error CS0591: Invalid value for argument to 'MethodImpl' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "1").WithArguments("MethodImpl"),
                // (12,17): error CS0591: Invalid value for argument to 'MethodImpl' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "2").WithArguments("MethodImpl"),
                // (15,17): error CS0591: Invalid value for argument to 'MethodImpl' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "3").WithArguments("MethodImpl"),
                // (21,17): error CS0591: Invalid value for argument to 'MethodImpl' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "5").WithArguments("MethodImpl"),
                // (27,17): error CS0591: Invalid value for argument to 'MethodImpl' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(MethodImplOptions)2").WithArguments("MethodImpl"),
                // (27,56): error CS0643: 'MethodCodeType' duplicate named attribute argument
                Diagnostic(ErrorCode.ERR_DuplicateNamedAttributeArgument, "MethodCodeType = (MethodCodeType)9").WithArguments("MethodCodeType"),
                // (27,20): error CS0599: Invalid value for named attribute argument 'MethodCodeType'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "MethodCodeType = (MethodCodeType)8").WithArguments("MethodCodeType"),
                // (27,56): error CS0599: Invalid value for named attribute argument 'MethodCodeType'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "MethodCodeType = (MethodCodeType)9").WithArguments("MethodCodeType"));
        }

        [Fact, WorkItem(544518, "DevDiv")]
        public void DllImport_DefaultCharSet1()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[module: DefaultCharSet(CharSet.Ansi)]

abstract class C
{
    [DllImport(""foo"")]
    static extern void f1();
}
";
            // Ref.Emit doesn't implement custom attributes yet
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ModuleRef));
                Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ImplMap));

                // the attribute is emitted:
                Assert.False(MetadataValidation.FindCustomAttribute(metadataReader, "DefaultCharSetAttribute").IsNil);

                var import = metadataReader.GetImportedMethods().Single().GetImport();
                Assert.Equal(MethodImportAttributes.CharSetAnsi, import.Attributes & MethodImportAttributes.CharSetMask);
            });
        }

        [Fact]
        public void DllImport_DefaultCharSet2()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[module: DefaultCharSet(CharSet.None)]

[StructLayout(LayoutKind.Explicit)]
abstract class C
{
    [DllImport(""foo"")]
    static extern void f1();
}
";
            // Ref.Emit doesn't implement custom attributes yet
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ModuleRef));
                Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ImplMap));

                // the attribute is emitted:
                Assert.False(MetadataValidation.FindCustomAttribute(metadataReader, "DefaultCharSetAttribute").IsNil);

                var import = metadataReader.GetImportedMethods().Single().GetImport();
                Assert.Equal(MethodImportAttributes.None, import.Attributes & MethodImportAttributes.CharSetMask);

                foreach (var typeHandle in metadataReader.TypeDefinitions)
                {
                    var def = metadataReader.GetTypeDefinition(typeHandle);
                    var name = metadataReader.GetString(def.Name);
                    switch (name)
                    {
                        case "C":
                            Assert.Equal(TypeAttributes.ExplicitLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit, def.Attributes);
                            break;

                        case "<Module>":
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }
                }
            });
        }

        [Fact]
        public void DllImport_DefaultCharSet_Errors()
        {
            var source = @"
using System.Runtime.InteropServices;

[module: DefaultCharSet((CharSet)int.MaxValue)]
";
            // Ref.Emit doesn't implement custom attributes yet
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,25): error CS0591: Invalid value for argument to 'DefaultCharSet' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(CharSet)int.MaxValue").WithArguments("DefaultCharSet"));
        }

        [Fact]
        public void DefaultCharSet_Types()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices; 
using System.Runtime.CompilerServices; 

[module:DefaultCharSet(CharSet.Unicode)] 

class C
{   
   class D
   {
	  int[] arr = new[] { 1,2,3,4,5,6,7,8,9,0, 1,2,3,4,5,6,7,8,9,0, 1,2,3,4,5,6,7,8,9,0, 1,2,3,4,5,6,7,8,9,0};
	     
	  void foo() 
	  {
	     int a = 1;
	     int b = 2;
	     var q = new { f = 1, g = 2 };
	     var z = new Action(() => Console.WriteLine(a + arr[b]));
	  }
	  
	  IEnumerable<int> En()
	  {
	     yield return 1;
	     yield return 2;
	  }
   }
}

[SpecialName]
public class Special { }

[StructLayout(LayoutKind.Sequential, Pack=4, Size=10)]
public struct SeqLayout { }

struct S { }
enum E { }
interface I { }
delegate void D();
";
            // Ref.Emit doesn't implement custom attributes yet
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var typeHandle in metadataReader.TypeDefinitions)
                {
                    var row = metadataReader.GetTypeDefinition(typeHandle);
                    var name = metadataReader.GetString(row.Name);
                    var actual = row.Attributes & TypeAttributes.StringFormatMask;

                    if (name == "<Module>" ||
                        name.StartsWith("__StaticArrayInitTypeSize=", StringComparison.Ordinal) ||
                        name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
                    {
                        Assert.Equal(TypeAttributes.AnsiClass, actual);
                    }
                    else
                    {
                        Assert.Equal(TypeAttributes.UnicodeClass, actual);
                    }
                }
            });
        }

        [Fact]
        public void DllImport_InvalidTargets()
        {
            var source = @"
using System.Runtime.InteropServices;

class C
{    
    [DllImport(""D.DLL"")]
    void F1() { }

    [DllImport(""D.DLL"")]
    enum E1 { a, b, c, d };

    [DllImport(""D.DLL"")]
    int F2(int bufSize, string buf);
    
    [DllImport(""D.DLL"")]
    public static int i =2;

    [DllImport(""d.dll"")]
    int F3 { get {return 0;}}

    [DllImport(""D.DLL""), DllImport(""GDI.DLL"")]
    static extern int F4(int bufSize, string buf);
  
    void F5()
    {
       [DllImport(""d.dll"")]
       int loc = i;
    }         
}

[DllImport(""dd.dllL"")]
public class C1 { }

[DllImport(""dd.dll"")]
public class C2 { }
";
            // Dev10 fails in Emit or emits invalid metadata
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (25,6): error CS1513: } expected {
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (29,1): error CS1022: Type or namespace definition, or end-of-file expected }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}"),
                // (13,9): error CS0501: 'C.F2(int, string)' must declare a body because it is not marked abstract, extern, or partial
                //     int F2(int bufSize, StringBuilder buf);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "F2").WithArguments("C.F2(int, string)"),
                // (6,6): error CS0601: The DllImport attribute must be specified on a method marked 'static' and 'extern'
                //     [DllImport("D.DLL")]
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport"),
                // (9,6): error CS0592: Attribute 'DllImport' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [DllImport("D.DLL")]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, @"DllImport").WithArguments("DllImport", "method"),
                // (12,6): error CS0601: The DllImport attribute must be specified on a method marked 'static' and 'extern'
                //     [DllImport("D.DLL")]
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport"),
                // (15,6): error CS0592: Attribute 'DllImport' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [DllImport("D.DLL")]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, @"DllImport").WithArguments("DllImport", "method"),
                // (18,6): error CS0592: Attribute 'DllImport' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [DllImport("d.dll")]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, @"DllImport").WithArguments("DllImport", "method"),
                // (21,26): error CS0579: Duplicate 'DllImport' attribute
                //     [DllImport("D.DLL"), DllImport("GDI.DLL")]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, @"DllImport").WithArguments("DllImport"),
                // (26,9): error CS0592: Attribute 'DllImport' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [DllImport("d.dll")]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, @"DllImport").WithArguments("DllImport", "method"),
                // (31,2): error CS0592: Attribute 'DllImport' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [DllImport("dd.dllL")]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, @"DllImport").WithArguments("DllImport", "method"),
                // (34,2): error CS0592: Attribute 'DllImport' is not valid on this declaration type. It is only valid on 'method' declarations.
                //     [DllImport("dd.dll")]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, @"DllImport").WithArguments("DllImport", "method"));
        }

        #endregion

        #region ComImportAttribute, CoClassAttribute

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6190")]
        public void TestComImportAttribute()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
class A
{
    public static extern void Foo();
}

public class MainClass
{
    public static int Main ()
    {
        A.Foo();
        return 0;
    }
}";
            Action<ModuleSymbol> sourceValidator = (ModuleSymbol m) =>
            {
                MethodImplAttributes expectedMethodImplAttributes = MethodImplAttributes.Managed | MethodImplAttributes.Runtime | MethodImplAttributes.InternalCall;
                var typeA = m.GlobalNamespace.GetTypeMember("A");
                Assert.True(typeA.IsComImport);
                Assert.Equal(2, typeA.GetAttributes().Length);

                var ctorA = typeA.InstanceConstructors.First();
                Assert.Equal(expectedMethodImplAttributes, ctorA.ImplementationAttributes);
                Assert.True(((Cci.IMethodDefinition)ctorA).IsExternal);

                var methodFoo = (Cci.IMethodDefinition)typeA.GetMember("Foo");
                Assert.True(methodFoo.IsExternal);
            };

            Action<ModuleSymbol> metadataValidator = (ModuleSymbol m) =>
            {
                var typeA = m.GlobalNamespace.GetTypeMember("A");
                Assert.True(typeA.IsComImport);
                Assert.Equal(1, typeA.GetAttributes().Length);

                var ctorA = typeA.InstanceConstructors.First();
                Assert.True(ctorA.IsExtern);

                var methodFoo = (MethodSymbol)typeA.GetMember("Foo");
                Assert.True(methodFoo.IsExtern);
            };

            // Verify that PEVerify will fail despite the fact that compiler produces no errors
            // This is consistent with Dev10 behavior
            //
            // Dev10 PEVerify failure:
            // [token  0x02000002] Type load failed.
            //
            // Dev10 Runtime Exception:
            // Unhandled Exception: System.TypeLoadException: Could not load type 'A' from assembly 'XXX' because the method 'Foo' has no implementation (no RVA).

            Assert.Throws(typeof(PeVerifyException), () => CompileAndVerify(source, options: TestOptions.ReleaseDll, sourceSymbolValidator: sourceValidator, symbolValidator: metadataValidator));
        }

        [Fact, WorkItem(544507, "DevDiv")]
        public void TestCoClassAttribute_NewOnInterface_FromSource()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

class WorksheetClass : IWorksheet
{
    int i;

    public WorksheetClass() { this.i = 0; }
    public WorksheetClass(int i) { this.i = i; }

    public int M1()
    {
        return i;
    }
}

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(WorksheetClass))]
interface IWorksheet
{
    int M1();
}

public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        Console.WriteLine(a.M1());

        IWorksheet b = new IWorksheet(1);
        Console.WriteLine(b.M1());

        var c = (WorksheetClass)new IWorksheet();
        Console.WriteLine(c.M1());
        
        return 0;
    }
}";
            Func<bool, Action<ModuleSymbol>> attributeValidator = isFromSource => (ModuleSymbol m) =>
            {
                NamespaceSymbol interopNS = Get_System_Runtime_InteropServices_NamespaceSymbol(m);
                var guidType = interopNS.GetTypeMember("GuidAttribute");
                var comImportType = interopNS.GetTypeMember("ComImportAttribute");
                var coClassType = interopNS.GetTypeMember("CoClassAttribute");

                var worksheetInterface = m.GlobalNamespace.GetTypeMember("IWorksheet");

                var attrs = worksheetInterface.GetAttributes().AsEnumerable();

                Assert.True(worksheetInterface.IsComImport, "Must be ComImport");
                if (isFromSource)
                {
                    Assert.Equal(3, attrs.Count());

                    attrs = worksheetInterface.GetAttributes(comImportType);
                    Assert.Equal(1, attrs.Count());
                }
                else
                {
                    Assert.Equal(2, attrs.Count());

                    // ComImportAttribute: Pseudo custom attribute shouldn't have been emitted
                    attrs = worksheetInterface.GetAttributes(comImportType);
                    Assert.Equal(0, attrs.Count());
                }

                attrs = worksheetInterface.GetAttributes(guidType);
                Assert.Equal(1, attrs.Count());

                attrs = worksheetInterface.GetAttributes(coClassType);
                Assert.Equal(1, attrs.Count());
            };

            string expectedOutput = @"0
1
0";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator(true), symbolValidator: attributeValidator(false), expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromMetadata()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class Wrapper
{
    public class WorksheetClass : IWorksheet
    {
        int i;

        public WorksheetClass() { this.i = 0; }
        public WorksheetClass(int i) { this.i = i; }

        public int M1()
        {
            return i;
        }
    }
}

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(Wrapper.WorksheetClass))]
public interface IWorksheet
{
    int M1();
}
";
            var compDll = CreateCompilationWithMscorlibAndSystemCore(source, assemblyName: "NewOnInterface_FromMetadata");

            var source2 = @"
using System;

public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        Console.WriteLine(a.M1());

        IWorksheet b = new IWorksheet(1);
        Console.WriteLine(b.M1());

        var c = (Wrapper.WorksheetClass)new IWorksheet();
        Console.WriteLine(c.M1());
        
        return 0;
    }
}";
            string expectedOutput = @"0
1
0";
            // Verify attributes from source and then load metadata to see attributes are written correctly.

            // Using metadata reference to test RetargetingNamedTypeSymbol CoClass type
            CompileAndVerify(source2, additionalRefs: new[] { compDll.ToMetadataReference() }, expectedOutput: expectedOutput);
            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CompileAndVerify(source2, additionalRefs: new[] { compDll.EmitToImageReference() }, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromSource_GenericTypeCoClass()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

public class WorksheetClass<T, U>: IWorksheet<T>
{
    public WorksheetClass(U x) { Console.WriteLine(x);}
}

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(WorksheetClass<int, string>))]
public interface IWorksheet<T>
{
}

public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet<int>(""string"");
        return 0;
    }
}";
            Func<bool, Action<ModuleSymbol>> attributeValidator = isFromSource => (ModuleSymbol m) =>
            {
                NamespaceSymbol interopNS = Get_System_Runtime_InteropServices_NamespaceSymbol(m);
                var guidType = interopNS.GetTypeMember("GuidAttribute");
                var comImportType = interopNS.GetTypeMember("ComImportAttribute");
                var coClassType = interopNS.GetTypeMember("CoClassAttribute");

                var worksheetInterface = m.GlobalNamespace.GetTypeMember("IWorksheet");

                var attrs = worksheetInterface.GetAttributes().AsEnumerable();

                Assert.True(worksheetInterface.IsComImport, "Must be ComImport");
                if (isFromSource)
                {
                    Assert.Equal(3, attrs.Count());

                    attrs = worksheetInterface.GetAttributes(comImportType);
                    Assert.Equal(1, attrs.Count());
                }
                else
                {
                    Assert.Equal(2, attrs.Count());

                    // ComImportAttribute: Pseudo custom attribute shouldn't have been emitted
                    attrs = worksheetInterface.GetAttributes(comImportType);
                    Assert.Equal(0, attrs.Count());
                }

                attrs = worksheetInterface.GetAttributes(guidType);
                Assert.Equal(1, attrs.Count());

                attrs = worksheetInterface.GetAttributes(coClassType);
                Assert.Equal(1, attrs.Count());
            };

            string expectedOutput = @"string";

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator(true), symbolValidator: attributeValidator(false), expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromMetadata_GenericTypeCoClass()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class WorksheetClass<T, U>: IWorksheet<T>
{
    public WorksheetClass(U x) { Console.WriteLine(x);}
}

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(WorksheetClass<int, string>))]
public interface IWorksheet<T>
{
}
";
            var compDll = CreateCompilationWithMscorlibAndSystemCore(source, assemblyName: "NewOnInterface_GenericTypeCoClass");

            var source2 = @"
using System;

public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet<int>(""string"");
        return 0;
    }
}";
            string expectedOutput = @"string";

            // Verify attributes from source and then load metadata to see attributes are written correctly.

            // Using metadata reference to test RetargetingNamedTypeSymbol CoClass type
            CompileAndVerify(source2, additionalRefs: new[] { compDll.ToMetadataReference() }, expectedOutput: expectedOutput);
            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CompileAndVerify(source2, additionalRefs: new[] { compDll.EmitToImageReference() }, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromSource_InaccessibleInterface()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

public class Wrapper
{
    public class WorksheetClass : IWorksheet
    {
    }

    [ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
    [CoClass(typeof(WorksheetClass))]
    private interface IWorksheet
    {
    }
}

public class MainClass
{
    public static int Main ()
    {
        var a = new Wrapper.IWorksheet();
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (22,29): error CS0122: 'Wrapper.IWorksheet' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "IWorksheet").WithArguments("Wrapper.IWorksheet").WithLocation(22, 29));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromMetadata_InaccessibleInterface()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class Wrapper
{
    public class WorksheetClass : IWorksheet
    {
    }

    [ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
    [CoClass(typeof(WorksheetClass))]
    private interface IWorksheet
    {
    }
}
";
            var compDll = CreateCompilationWithMscorlibAndSystemCore(source, assemblyName: "NewOnInterface_InaccessibleInterface");

            var source2 = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new Wrapper.IWorksheet();
        return 0;
    }
}";
            // Using metadata reference to test RetargetingNamedTypeSymbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
                // (6,29): error CS0122: 'Wrapper.IWorksheet' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "IWorksheet").WithArguments("Wrapper.IWorksheet").WithLocation(6, 29));

            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { compDll.EmitToImageReference() }).VerifyDiagnostics(
                // (6,29): error CS0122: 'Wrapper.IWorksheet' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "IWorksheet").WithArguments("Wrapper.IWorksheet").WithLocation(6, 29));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromSource_InaccessibleCoClass()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

public class Wrapper
{
    private class WorksheetClass : IWorksheet
    {
    }

    [ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
    [CoClass(typeof(WorksheetClass))]
    public interface IWorksheet
    {
    }
}

public class MainClass
{
    public static int Main ()
    {
        var a = new Wrapper.IWorksheet();
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (22,21): error CS0122: 'Wrapper.WorksheetClass.WorksheetClass()' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "Wrapper.IWorksheet").WithArguments("Wrapper.WorksheetClass.WorksheetClass()").WithLocation(22, 21));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromMetadata_InaccessibleCoClass()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class Wrapper
{
    private class WorksheetClass : IWorksheet
    {
    }

    [ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
    [CoClass(typeof(WorksheetClass))]
    public interface IWorksheet
    {
    }
}
";
            var compDll = CreateCompilationWithMscorlibAndSystemCore(source, assemblyName: "NewOnInterface_InaccessibleCoClass");

            var source2 = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new Wrapper.IWorksheet();
        return 0;
    }
}";

            // Using metadata reference to test RetargetingNamedTypeSymbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
                // (6,21): error CS0122: 'Wrapper.WorksheetClass.WorksheetClass()' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "Wrapper.IWorksheet").WithArguments("Wrapper.WorksheetClass.WorksheetClass()").WithLocation(6, 21));

            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { compDll.EmitToImageReference() }).VerifyDiagnostics(
                // (6,21): error CS0122: 'Wrapper.WorksheetClass.WorksheetClass()' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "Wrapper.IWorksheet").WithArguments("Wrapper.WorksheetClass.WorksheetClass()").WithLocation(6, 21));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromSource_CoClass_Without_ComImport()
        {
            string source = @"
using System.Runtime.InteropServices;

public class Wrapper
{
    private class WorksheetClass : IWorksheet
    {
    }

    [CoClass(typeof(WorksheetClass))]
    public interface IWorksheet
    {
    }
}

public class MainClass
{
    public static int Main ()
    {
        var a = new Wrapper.IWorksheet();
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,6): warning CS0684: 'IWorksheet' interface marked with 'CoClassAttribute' not marked with 'ComImportAttribute'
                //     [CoClass(typeof(WorksheetClass))]
                Diagnostic(ErrorCode.WRN_CoClassWithoutComImport, "CoClass(typeof(WorksheetClass))").WithArguments("IWorksheet").WithLocation(10, 6),
                // (20,17): error CS0144: Cannot create an instance of the abstract class or interface 'Wrapper.IWorksheet'
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new Wrapper.IWorksheet()").WithArguments("Wrapper.IWorksheet").WithLocation(20, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromMetadata_CoClass_Without_ComImport()
        {
            var source = @"
using System.Runtime.InteropServices;

public class Wrapper
{
    private class WorksheetClass : IWorksheet
    {
    }

    [CoClass(typeof(WorksheetClass))]
    public interface IWorksheet
    {
    }
}
";
            var compDll = CreateCompilationWithMscorlibAndSystemCore(source, assemblyName: "NewOnInterface_CoClass_Without_ComImport");

            var source2 = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new Wrapper.IWorksheet();
        return 0;
    }
}";

            // Using metadata reference to test RetargetingNamedTypeSymbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
                // (6,17): error CS0144: Cannot create an instance of the abstract class or interface 'Wrapper.IWorksheet'
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new Wrapper.IWorksheet()").WithArguments("Wrapper.IWorksheet").WithLocation(6, 17));

            var assemblyRef = compDll.EmitToImageReference(expectedWarnings: new[]
            {
                // (11,6): warning CS0684: 'IWorksheet' interface marked with 'CoClassAttribute' not marked with 'ComImportAttribute'
                //     [CoClass(typeof(WorksheetClass))]
                Diagnostic(ErrorCode.WRN_CoClassWithoutComImport, "CoClass(typeof(WorksheetClass))").WithArguments("IWorksheet")
            });

            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { assemblyRef }).VerifyDiagnostics(
                // (6,17): error CS0144: Cannot create an instance of the abstract class or interface 'Wrapper.IWorksheet'
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new Wrapper.IWorksheet()").WithArguments("Wrapper.IWorksheet").WithLocation(6, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromSource_StructTypeInCoClassAttribute()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(Wrapper.WorksheetClass))]
public interface IWorksheet
{
}
    
public class Wrapper
{
    public struct WorksheetClass : IWorksheet
    {
    }
}

public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (22,17): error CS0144: Cannot create an instance of the abstract class or interface 'IWorksheet'
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new IWorksheet()").WithArguments("IWorksheet").WithLocation(22, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_FromMetadata_StructTypeInCoClassAttribute()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(Wrapper.WorksheetClass))]
public interface IWorksheet
{
}
    
public class Wrapper
{
    public struct WorksheetClass : IWorksheet
    {
    }
}
";
            var compDll = CreateCompilationWithMscorlibAndSystemCore(source, assemblyName: "NewOnInterface_StructTypeInCoClassAttribute");

            var source2 = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";
            // Using metadata reference to test RetargetingNamedTypeSymbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
                // (6,17): error CS0144: Cannot create an instance of the abstract class or interface 'IWorksheet'
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new IWorksheet()").WithArguments("IWorksheet").WithLocation(6, 17));

            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CreateCompilationWithMscorlib(source2, references: new[] { compDll.EmitToImageReference() }).VerifyDiagnostics(
                // (6,17): error CS0144: Cannot create an instance of the abstract class or interface 'IWorksheet'
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new IWorksheet()").WithArguments("IWorksheet").WithLocation(6, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_InaccessibleTypeInCoClassAttribute()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(Wrapper.WorksheetClass))]
interface IWorksheet
{
}
    
public class Wrapper
{
    private class WorksheetClass : IWorksheet
    {
    }
}

public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,25): error CS0122: 'Wrapper.WorksheetClass' is inaccessible due to its protection level
                // [CoClass(typeof(Wrapper.WorksheetClass))]
                Diagnostic(ErrorCode.ERR_BadAccess, "WorksheetClass").WithArguments("Wrapper.WorksheetClass").WithLocation(6, 25),
                // (22,17): error CS0144: Cannot create an instance of the abstract class or interface 'IWorksheet'
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new IWorksheet()").WithArguments("IWorksheet").WithLocation(22, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_CoClassDoesntImplementInterface()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(WorksheetClass))]
interface IWorksheet
{
}
    
class WorksheetClass
{
}

public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_UsingCustomIL()
        {
            var ilSource = @"
.class interface public abstract auto ansi import IWorksheet
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 31 30 2D 30 30 30 30 2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30 30 30 30 30 30 34 36 00 00 )
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 0E 57 6F 72 6B 73 68 65 65 74 43 6C 61 73 73 00 00 )
}

.class public auto ansi beforefieldinit WorksheetClass extends [mscorlib]System.Object implements IWorksheet
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}";
            var source = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";

            var compilation = CreateCompilationWithCustomILSource(source, ilSource);

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_UsingCustomIL_StructTypeCoClass()
        {
            var ilSource = @"
.class interface public abstract auto ansi import IWorksheet
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 31 30 2D 30 30 30 30 2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30 30 30 30 30 30 34 36 00 00 )
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 0E 57 6F 72 6B 73 68 65 65 74 43 6C 61 73 73 00 00 )
}

.class sequential ansi sealed public beforefieldinit WorksheetClass
         extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
}
";
            var source = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";

            var compilation = CreateCompilationWithCustomILSource(source, ilSource);

            compilation.VerifyDiagnostics(
                // (6,17): error CS0144: Cannot create an instance of the abstract class or interface 'IWorksheet'
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new IWorksheet()").WithArguments("IWorksheet").WithLocation(6, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_UsingCustomIL_InvalidTypeName()
        {
            var ilSource = @"
.class interface public abstract auto ansi import IWorksheet
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 31 30 2D 30 30 30 30 2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30 30 30 30 30 30 34 36 00 00 )
  // correct attribute arguments signature: 01 00 0E 57 6F 72 6B 73 68 65 65 74 43 6C 61 73 73 00 00 
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 0E 59 6F 72 6B 73 68 65 65 74 43 6C 61 73 73 00 00 )
}

.class public auto ansi beforefieldinit WorksheetClass extends [mscorlib]System.Object implements IWorksheet
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}";
            var source = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";

            var compilation = CreateCompilationWithCustomILSource(source, ilSource);

            compilation.VerifyDiagnostics(
                // (6,17): error CS1613: The managed coclass wrapper class 'YorksheetClass' for interface 'IWorksheet' cannot be found (are you missing an assembly reference?)
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_MissingCoClass, "new IWorksheet()").WithArguments("YorksheetClass", "IWorksheet").WithLocation(6, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_UsingCustomIL_UnboundGenericTypeCoClass()
        {
            var ilSource = @"
.class interface public abstract auto ansi import IWorksheet
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 32 30 38 31 30 2D 30 30 30 30 2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30 30 30 30 30 30 34 36 00 00 )
  // [CoClass(typeof(WorksheetClass<>))]
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 10 57 6F 72 6B 73 68 65 65 74 43 6C 61 73 73 60 31 00 00 )
}

.class public auto ansi beforefieldinit WorksheetClass`1<T> extends [mscorlib]System.Object implements IWorksheet
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}";
            var source = @"
public class MainClass
{
    public static int Main ()
    {
        var a = new IWorksheet();
        return 0;
    }
}";

            var compilation = CreateCompilationWithCustomILSource(source, ilSource);

            compilation.VerifyDiagnostics(
                // (6,17): error CS1639: The managed coclass wrapper class signature 'WorksheetClass<>' for interface 'IWorksheet' is not a valid class name signature
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_BadCoClassSig, "new IWorksheet()").WithArguments("WorksheetClass<>", "IWorksheet").WithLocation(6, 17));
        }

        [Fact]
        public void TestCoClassAttribute_NewOnInterface_Within_AttributeArgument()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

class CoClassType : InterfaceType
{
}

class AAttribute: Attribute
{
  public AAttribute(object o) {}
}

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(CoClassType))]
[AAttribute(new InterfaceType())]
interface InterfaceType
{
}

public class MainClass
{
    public static int Main ()
    {
        return 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (16,13): error CS0144: Cannot create an instance of the abstract class or interface 'InterfaceType'
                // [AAttribute(new InterfaceType())]
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new InterfaceType()").WithArguments("InterfaceType").WithLocation(16, 13));
        }

        [Fact, WorkItem(544237, "DevDiv")]
        public void TestCoClassAttribute_NewOnInterface_NoConversion()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
 
[ComImport]
[Guid(""69D3E2A0-BB0F-4FE3-9860-ED714C510756"")]
[CoClass(typeof(StackOverflowException))]
interface I { }
 
class A
{
    static void Main()
    {
        var x = new I(); // error CS0030: Cannot convert type 'System.StackOverflowException' to 'I'
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,17): error CS0030: Cannot convert type 'System.StackOverflowException' to 'I'
                //         var x = new I(); // error CS0030: Cannot convert type 'System.StackOverflowException' to 'I'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "new I()").WithArguments("System.StackOverflowException", "I").WithLocation(14, 17));
        }

        #endregion

        #region GuidAttribute

        [Fact]
        public void TestInvalidGuidAttribute()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
 
[ComImport]
[Guid(""69D3E2A0-BB0F-4FE3-9860-ED714C510756"")]    // valid (36 chars)
class A {}

[Guid(""69D3E2A0-BB0F-4FE3-9860-ED714C51075"")]    // incorrect length (35 chars)
class B {}

[Guid(""69D3E2A0BB0F--4FE3-9860-ED714C510756"")]    // invalid format
class C {}

[Guid("""")]    // empty string
class D {}

[Guid(null)]    // null
class E {}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid("69D3E2A0-BB0F-4FE3-9860-ED714C51075")]    // incorrect length (35 chars)
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""69D3E2A0-BB0F-4FE3-9860-ED714C51075""").WithArguments("Guid").WithLocation(9, 7),
                // (12,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid("69D3E2A0BB0F--4FE3-9860-ED714C510756")]    // invalid format
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""69D3E2A0BB0F--4FE3-9860-ED714C510756""").WithArguments("Guid").WithLocation(12, 7),
                // (15,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid("")]    // empty string
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""""").WithArguments("Guid").WithLocation(15, 7),
                // (18,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid(null)]    // null
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "null").WithArguments("Guid").WithLocation(18, 7));
        }

        [WorkItem(545490, "DevDiv")]
        [Fact]
        public void TestInvalidGuidAttribute_02()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
 
// Following are alternate valid Guid formats, but disallowed by the native compiler. Ensure we disallow them.

[Guid(""69D3E2A0BB0F4FE39860ED714C510756"")]    // 32 digits, no hyphens
class A {}

[Guid(""{69D3E2A0-BB0F-4FE3-9860-ED714C510756}"")]    // 32 digits separated by hyphens, enclosed in braces
class B {}

[Guid(""(69D3E2A0-BB0F-4FE3-9860-ED714C510756)"")]    // 32 digits separated by hyphens, enclosed in parentheses
class C {}

[Guid(""{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}"")]    // Four hexadecimal values enclosed in braces, where the fourth value is a subset of eight hexadecimal values that is also enclosed in braces
class D {}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid("69D3E2A0BB0F4FE39860ED714C510756")]    // 32 digits, no hyphens
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""69D3E2A0BB0F4FE39860ED714C510756""").WithArguments("Guid").WithLocation(7, 7),
                // (10,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid("{69D3E2A0-BB0F-4FE3-9860-ED714C510756}")]    // 32 digits separated by hyphens, enclosed in braces
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""{69D3E2A0-BB0F-4FE3-9860-ED714C510756}""").WithArguments("Guid").WithLocation(10, 7),
                // (13,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid("(69D3E2A0-BB0F-4FE3-9860-ED714C510756)")]    // 32 digits separated by hyphens, enclosed in parentheses
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""(69D3E2A0-BB0F-4FE3-9860-ED714C510756)""").WithArguments("Guid").WithLocation(13, 7),
                // (16,7): error CS0591: Invalid value for argument to 'Guid' attribute
                // [Guid("{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}")]    // Four hexadecimal values enclosed in braces, where the fourth value is a subset of eight hexadecimal values that is also enclosed in braces
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}""").WithArguments("Guid").WithLocation(16, 7));
        }

        [Fact]
        public void TestInvalidGuidAttribute_Assembly()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""69D3E2A0BB0F--4FE3-9860-ED714C510756"")]    // invalid format
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,17): error CS0591: Invalid value for argument to 'Guid' attribute
                // [assembly: Guid("69D3E2A0BB0F--4FE3-9860-ED714C510756")]    // invalid format
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""69D3E2A0BB0F--4FE3-9860-ED714C510756""").WithArguments("Guid").WithLocation(5, 17));
        }

        #endregion

        #region SpecialNameAttribute

        [Fact, WorkItem(544392, "DevDiv")]
        public void SpecialName()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

[SpecialName]
class Z
{
    [SpecialName]
    void m() { }

    [SpecialName]
    int f;

    [SpecialName]
    int p1 { get; set; }

    [SpecialName]
    int p2 { get { return 1; } }

    [SpecialName]
    int p3
    {
        [SpecialName]
        get { return 1; }

        [SpecialName]
        set { }
    }

    [SpecialName]
    [field: SpecialName]
    [method: SpecialName]
    event Action e;
}

enum En
{
    [SpecialName]
    A = 1
}

[SpecialName]
struct S { }
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var ca in metadataReader.CustomAttributes)
                {
                    var name = MetadataValidation.GetAttributeName(metadataReader, ca);
                    Assert.NotEqual("SpecialNameAttribute", name);
                }

                foreach (var typeDef in metadataReader.TypeDefinitions)
                {
                    var row = metadataReader.GetTypeDefinition(typeDef);
                    var name = metadataReader.GetString(row.Name);
                    switch (name)
                    {
                        case "S":
                        case "Z":
                            Assert.Equal(TypeAttributes.SpecialName, row.Attributes & TypeAttributes.SpecialName);
                            break;

                        case "<Module>":
                        case "En":
                            Assert.Equal((TypeAttributes)0, row.Attributes & TypeAttributes.SpecialName);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }
                }

                foreach (var methodHandle in metadataReader.MethodDefinitions)
                {
                    var flags = metadataReader.GetMethodDefinition(methodHandle).Attributes;
                    Assert.Equal(MethodAttributes.SpecialName, flags & MethodAttributes.SpecialName);
                }

                foreach (var fieldDef in metadataReader.FieldDefinitions)
                {
                    var field = metadataReader.GetFieldDefinition(fieldDef);
                    var name = metadataReader.GetString(field.Name);
                    var flags = field.Attributes;
                    switch (name)
                    {
                        case "e":
                        case "f":
                        case "value__":
                        case "A":
                            Assert.Equal(FieldAttributes.SpecialName, flags & FieldAttributes.SpecialName);
                            break;

                        case "<p1>k__BackingField":
                            Assert.Equal((FieldAttributes)0, flags & FieldAttributes.SpecialName);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }
                }

                foreach (var propertyDef in metadataReader.PropertyDefinitions)
                {
                    var flags = metadataReader.GetPropertyDefinition(propertyDef).Attributes;
                    Assert.Equal(PropertyAttributes.SpecialName, flags & PropertyAttributes.SpecialName);
                }

                foreach (var eventDef in metadataReader.EventDefinitions)
                {
                    var flags = metadataReader.GetEventDefinition(eventDef).Attributes;
                    Assert.Equal(EventAttributes.SpecialName, flags & EventAttributes.SpecialName);
                }
            });
        }

        #endregion

        #region SerializableAttribute

        [Fact, WorkItem(544392, "DevDiv")]
        public void Serializable()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

[Serializable]
class A 
{ 
    [field: NonSerialized]
    event Action e;
}

[Serializable]
struct B 
{ 
    [NonSerialized]
    int x;
}

[Serializable]
enum E 
{
    [NonSerialized]
    A = 1 
}

[Serializable]
delegate void D();
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var ca in metadataReader.CustomAttributes)
                {
                    var name = MetadataValidation.GetAttributeName(metadataReader, ca);
                    Assert.NotEqual("SpecialNameAttribute", name);
                }

                foreach (var typeDef in metadataReader.TypeDefinitions)
                {
                    var row = metadataReader.GetTypeDefinition(typeDef);
                    var name = metadataReader.GetString(row.Name);
                    switch (name)
                    {
                        case "A":
                        case "B":
                        case "E":
                        case "D":
                            Assert.Equal(TypeAttributes.Serializable, row.Attributes & TypeAttributes.Serializable);
                            break;

                        case "<Module>":
                            Assert.Equal((TypeAttributes)0, row.Attributes & TypeAttributes.Serializable);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }
                }

                foreach (var fieldDef in metadataReader.FieldDefinitions)
                {
                    var field = metadataReader.GetFieldDefinition(fieldDef);
                    var name = metadataReader.GetString(field.Name);
                    var flags = field.Attributes;
                    switch (name)
                    {
                        case "e":
                        case "x":
                        case "A":
                            Assert.Equal(FieldAttributes.NotSerialized, flags & FieldAttributes.NotSerialized);
                            break;

                        case "value__":
                            Assert.Equal((FieldAttributes)0, flags & FieldAttributes.NotSerialized);
                            break;
                    }
                }
            });
        }

        #endregion

        #region ParamArrayAttribute

        [Fact]
        public void TestParamArrayAttributeForParams()
        {
            string source = @"
using System;
namespace AttributeTest
{
    public class MyClass 
    {

       public static void UseParams(params int[] list) 
       {
          for ( int i = 0 ; i < list.Length ; i++ )
             Console.WriteLine(list[i]);
          Console.WriteLine();
       }

       public static void NoParams(object list) 
       {
          Console.WriteLine();
       }

       public static void Main() 
       {
          UseParams(1, 2, 3);
          NoParams(1); 

          int[] myarray = new int[3] {10,11,12};
          UseParams(myarray);
       }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("AttributeTest");
                var type = (NamedTypeSymbol)ns.GetMember("MyClass");

                var useParamsMethod = (MethodSymbol)type.GetMember("UseParams");
                var paramsParameter = useParamsMethod.Parameters[0];
                VerifyParamArrayAttribute(paramsParameter, (SourceModuleSymbol)m);

                var noParamsMethod = (MethodSymbol)type.GetMember("NoParams");
                var noParamsParameter = noParamsMethod.Parameters[0];
                Assert.Equal(0, noParamsParameter.GetSynthesizedAttributes().Length);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            var comp = CompileAndVerify(
                compilation,
                sourceSymbolValidator: attributeValidator,
                symbolValidator: null,
                expectedSignatures: new[]
                {
                    Signature("AttributeTest.MyClass", "UseParams", ".method public hidebysig static System.Void UseParams([System.ParamArrayAttribute()] System.Int32[] list) cil managed"),
                    Signature("AttributeTest.MyClass", "NoParams", ".method public hidebysig static System.Void NoParams(System.Object list) cil managed"),
                });
        }

        #endregion

        #region AttributeUsageAttribute

        [WorkItem(541733, "DevDiv")]
        [Fact]
        public void TestSourceOverrideWellKnownAttribute_01()
        {
            string source = @"
namespace System
{
    [AttributeUsage(AttributeTargets.Class)]
    [AttributeUsage(AttributeTargets.Class)]
    class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets x)
        {
        }
    }
}";
            var syntaxTree = Parse(source, filename: "test.cs");
            var compilation = CreateCompilationWithMscorlib(syntaxTree);

            var comp = compilation.VerifyDiagnostics(
                // test.cs(4,3): warning CS0436: The type 'System.AttributeUsageAttribute' in 'test.cs' conflicts with the imported type 'System.AttributeUsageAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'test.cs'.
                // 	[AttributeUsage(AttributeTargets.Class)]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AttributeUsage").WithArguments("test.cs", "System.AttributeUsageAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.AttributeUsageAttribute"),
                // test.cs(5,3): warning CS0436: The type 'System.AttributeUsageAttribute' in 'test.cs' conflicts with the imported type 'System.AttributeUsageAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'test.cs'.
                // 	[AttributeUsage(AttributeTargets.Class)]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AttributeUsage").WithArguments("test.cs", "System.AttributeUsageAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.AttributeUsageAttribute"),
                // test.cs(5,3): error CS0579: Duplicate 'AttributeUsage' attribute
                // 	[AttributeUsage(AttributeTargets.Class)]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "AttributeUsage").WithArguments("AttributeUsage"));
        }

        [WorkItem(541733, "DevDiv")]
        [WorkItem(546102, "DevDiv")]
        [Fact]
        public void TestSourceOverrideWellKnownAttribute_02()
        {
            string source = @"
namespace System
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets x)
        {
        }

        public bool AllowMultiple
        {
            get { return false; }
            set { }
        }
    }
}";
            var syntaxTree = Parse(source, filename: "test.cs");
            var compilation = CreateCompilationWithMscorlib(syntaxTree, options: TestOptions.ReleaseDll);

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var ns = (NamespaceSymbol)m.GlobalNamespace.GetMember("System");
                var attrType = ns.GetTypeMember("AttributeUsageAttribute");

                var attrs = attrType.GetAttributes(attrType);
                Assert.Equal(2, attrs.Count());

                // Verify attributes
                var attrSym = attrs.First();
                Assert.Equal(1, attrSym.CommonConstructorArguments.Length);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)AttributeTargets.Class);
                Assert.Equal(1, attrSym.CommonNamedArguments.Length);
                attrSym.VerifyNamedArgumentValue(0, "AllowMultiple", TypedConstantKind.Primitive, true);

                attrSym = attrs.ElementAt(1);
                Assert.Equal(1, attrSym.CommonConstructorArguments.Length);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)AttributeTargets.Class);
                Assert.Equal(1, attrSym.CommonNamedArguments.Length);
                attrSym.VerifyNamedArgumentValue(0, "AllowMultiple", TypedConstantKind.Primitive, false);

                // Verify AttributeUsage
                var attributeUsage = attrType.GetAttributeUsageInfo();
                Assert.Equal(AttributeTargets.Class, attributeUsage.ValidTargets);
                Assert.Equal(true, attributeUsage.AllowMultiple);
                Assert.Equal(true, attributeUsage.Inherited);
            };

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator: attributeValidator, symbolValidator: attributeValidator);
        }

        [WorkItem(546102, "DevDiv")]
        [Fact]
        public void TestAttributeUsageAllowMultiple()
        {
            string source = @"
using System;

namespace System
{
    class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets x)
        {
        }

        public bool AllowMultiple
        {
            get { return false; }
            set { }
        }
    }
}

[A, A]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class A: Attribute {}
";
            CompileAndVerify(source);
        }

        [WorkItem(546056, "DevDiv")]
        [Fact]
        public void TestBadAttributeUsageArgument()
        {
            string source = @"
using System;

[AttributeUsage(badAttributeTargets)]
public class MyAttribute : Attribute
{
	public const AttributeTargets badAttributeTargets = Missing;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,54): error CS0103: The name 'Missing' does not exist in the current context
                // 	public const AttributeTargets badAttributeTargets = Missing;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Missing").WithArguments("Missing"),
                // (4,17): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [AttributeUsage(badAttributeTargets)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "badAttributeTargets"));
        }

        #endregion

        #region InternalsVisibleToAttribute

        [WorkItem(542173, "DevDiv")]
        [Fact]
        public void MergeMemberImplWithImportedInternals()
        {
            #region "Text"
            string text1 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Child"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Child2"")]

public abstract class Parent
{
    internal abstract string M1();
    public abstract int GetInt();
}
";

            string text2 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Child2"")]

public abstract class Child: Parent
{
    internal override string M1()
    {
        return ""Child"";
    }
}
";
            string text3 = @"
public class Child2: Child
{
    public override int GetInt()
    {
        return 2;
    }
}
";
            #endregion

            var opt = TestOptions.ReleaseDll;
            var comp1 = CreateCompilationWithMscorlib(text1, options: opt);
            var compref1 = new CSharpCompilationReference(comp1);
            var comp2 = CreateCompilationWithMscorlib(text2, references: new[] { compref1 }, options: opt, assemblyName: "Child");
            var comp3 = CreateCompilationWithMscorlib(text3, references: new[] { compref1, new CSharpCompilationReference(comp2) }, options: opt, assemblyName: "Child2");
            // OK
            comp3.VerifyDiagnostics();

            comp3 = CreateCompilationWithMscorlib(text3, references: new[] { compref1, new CSharpCompilationReference(comp2) }, options: opt, assemblyName: "Child2");
            comp3.VerifyDiagnostics();
        }

        #endregion

        #region CustomConstantAttribute

        [Fact, WorkItem(544440, "DevDiv"), WorkItem(538206, "DevDiv")]
        public void CustomConstantAttributeIntToObj()
        {
            #region "Source"
            string source = @"
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
sealed class ObjectConstantAttribute : CustomConstantAttribute
{
    private object _obj;
    public override object Value { get { return _obj; } }

    public ObjectConstantAttribute(object objectValue)
    {
        _obj = objectValue;
    }
}

public class Test
{
    public void Foo2([Optional][ObjectConstant(1000)] object obj)
    {
        Console.WriteLine(obj);
    }

    public static void Main()
    {
        new Test().Foo2();
    }
}
";
            #endregion

            CompileAndVerify(source, additionalRefs: new[] { SystemRef }, expectedOutput: @"System.Reflection.Missing")
                .VerifyIL("Test.Main", @"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  ldsfld     ""object System.Type.Missing""
  IL_000a:  call       ""void Test.Foo2(object)""
  IL_000f:  ret
}");
        }

        #endregion

        #region AssemblyKeyFileAttribute

        // See InternalsVisibleToAndStrongNameTests

        #endregion

        #region ClassInterfaceAttribute

        [Fact]
        public void TestClassInterfaceAttribute()
        {
            var source = @"
using System.Runtime.InteropServices;

// Valid cases

[assembly: ClassInterface(ClassInterfaceType.None)]

[ClassInterface(ClassInterfaceType.AutoDispatch)]
public class Class1 {}

[ClassInterface(ClassInterfaceType.AutoDual)]
public class Class2 {}

[ClassInterface((short)0)]
public class Class4 {}

[ClassInterface((short)1)]
public class Class5 {}

[ClassInterface((short)2)]
public class Class6 {}


// Invalid cases

[ClassInterface((ClassInterfaceType)(-1))]
public class InvalidClass1 {}

[ClassInterface((ClassInterfaceType)3)]
public class InvalidClass2 {}

[ClassInterface((short)(-1))]
public class InvalidClass3 {}

[ClassInterface((short)3)]
public class InvalidClass4 {}

[ClassInterface(System.Int32.MaxValue)]
public class InvalidClass5 { }

[ClassInterface(ClassInterfaceType.None)]
public interface InvalidTarget {}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (26,17): error CS0591: Invalid value for argument to 'ClassInterface' attribute
                // [ClassInterface((ClassInterfaceType)(-1))]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(ClassInterfaceType)(-1)").WithArguments("ClassInterface"),
                // (29,17): error CS0591: Invalid value for argument to 'ClassInterface' attribute
                // [ClassInterface((ClassInterfaceType)3)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(ClassInterfaceType)3").WithArguments("ClassInterface"),
                // (32,17): error CS0591: Invalid value for argument to 'ClassInterface' attribute
                // [ClassInterface((short)(-1))]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(short)(-1)").WithArguments("ClassInterface"),
                // (35,17): error CS0591: Invalid value for argument to 'ClassInterface' attribute
                // [ClassInterface((short)3)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(short)3").WithArguments("ClassInterface"),
                // (38,17): error CS1503: Argument 1: cannot convert from 'int' to 'System.Runtime.InteropServices.ClassInterfaceType'
                // [ClassInterface(System.Int32.MaxValue)]
                Diagnostic(ErrorCode.ERR_BadArgType, "System.Int32.MaxValue").WithArguments("1", "int", "System.Runtime.InteropServices.ClassInterfaceType"),
                // (41,2): error CS0592: Attribute 'ClassInterface' is not valid on this declaration type. It is only valid on 'assembly, class' declarations.
                // [ClassInterface(ClassInterfaceType.None)]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ClassInterface").WithArguments("ClassInterface", "assembly, class"));
        }

        #endregion

        #region InterfaceTypeAttribute

        [Fact]
        public void TestInterfaceTypeAttribute()
        {
            var source = @"
using System.Runtime.InteropServices;

// Valid cases

[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface Interface1 {}

[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface Interface2 {}

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface Interface4 {}

// ComInterfaceType.InterfaceIsIInspectable seems to be undefined in version of mscorlib used by the test framework.
[InterfaceType((ComInterfaceType)3)]
public interface Interface3 {}

[InterfaceType((short)0)]
public interface Interface5 {}

[InterfaceType((short)1)]
public interface Interface6 {}

[InterfaceType((short)2)]
public interface Interface7 {}

[InterfaceType((short)3)]
public interface Interface8 {}


// Invalid cases

[InterfaceType((ComInterfaceType)(-1))]
public interface InvalidInterface1 {}

[InterfaceType((ComInterfaceType)4)]
public interface InvalidInterface2 {}

[InterfaceType((short)(-1))]
public interface InvalidInterface3 {}

[InterfaceType((short)4)]
public interface InvalidInterface4 {}

[InterfaceType(System.Int32.MaxValue)]
public interface InvalidInterface5 {}

[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public class InvalidTarget {}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (34,16): error CS0591: Invalid value for argument to 'InterfaceType' attribute
                // [InterfaceType((ComInterfaceType)(-1))]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(ComInterfaceType)(-1)").WithArguments("InterfaceType"),
                // (37,16): error CS0591: Invalid value for argument to 'InterfaceType' attribute
                // [InterfaceType((ComInterfaceType)4)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(ComInterfaceType)4").WithArguments("InterfaceType"),
                // (40,16): error CS0591: Invalid value for argument to 'InterfaceType' attribute
                // [InterfaceType((short)(-1))]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(short)(-1)").WithArguments("InterfaceType"),
                // (43,16): error CS0591: Invalid value for argument to 'InterfaceType' attribute
                // [InterfaceType((short)4)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(short)4").WithArguments("InterfaceType"),
                // (46,16): error CS1503: Argument 1: cannot convert from 'int' to 'System.Runtime.InteropServices.ComInterfaceType'
                // [InterfaceType(System.Int32.MaxValue)]
                Diagnostic(ErrorCode.ERR_BadArgType, "System.Int32.MaxValue").WithArguments("1", "int", "System.Runtime.InteropServices.ComInterfaceType"),
                // (49,2): error CS0592: Attribute 'InterfaceType' is not valid on this declaration type. It is only valid on 'interface' declarations.
                // [InterfaceType(ComInterfaceType.InterfaceIsDual)]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "InterfaceType").WithArguments("InterfaceType", "interface"));
        }

        #endregion

        #region TypeLibVersionAttribute

        [Fact]
        public void TestTypeLibVersionAttribute_Valid()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: TypeLibVersionAttribute(0, int.MaxValue)]
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestTypeLibVersionAttribute_Valid2()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: TypeLibVersionAttribute(C.S * C.S, unchecked((int)((long)-int.MinValue - 1)))]
public class C
{
    public const short S = short.MaxValue;
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestTypeLibVersionAttribute_Invalid()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: TypeLibVersionAttribute(-1, int.MinValue)]
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,36): error CS0591: Invalid value for argument to 'TypeLibVersionAttribute' attribute
                // [assembly: TypeLibVersionAttribute(-1, int.MinValue)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "-1").WithArguments("TypeLibVersionAttribute"),
                // (4,40): error CS0591: Invalid value for argument to 'TypeLibVersionAttribute' attribute
                // [assembly: TypeLibVersionAttribute(-1, int.MinValue)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "int.MinValue").WithArguments("TypeLibVersionAttribute"));
        }

        [Fact]
        public void TestTypeLibVersionAttribute_Invalid_02()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: TypeLibVersionAttribute(""str"", 0)]
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,36): error CS1503: Argument 1: cannot convert from 'string' to 'int'
                // [assembly: TypeLibVersionAttribute("str", 0)]
                Diagnostic(ErrorCode.ERR_BadArgType, @"""str""").WithArguments("1", "string", "int"));
        }

        #endregion

        #region ComCompatibleVersionAttribute

        [Fact]
        public void TestComCompatibleVersionAttribute_Valid()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: ComCompatibleVersionAttribute(0, 0, 0, 0)]
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestComCompatibleVersionAttribute_Invalid()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)]
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,42): error CS0591: Invalid value for argument to 'ComCompatibleVersionAttribute' attribute
                // [assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "-1").WithArguments("ComCompatibleVersionAttribute"),
                // (4,46): error CS0591: Invalid value for argument to 'ComCompatibleVersionAttribute' attribute
                // [assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "-1").WithArguments("ComCompatibleVersionAttribute"),
                // (4,50): error CS0591: Invalid value for argument to 'ComCompatibleVersionAttribute' attribute
                // [assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "-1").WithArguments("ComCompatibleVersionAttribute"),
                // (4,54): error CS0591: Invalid value for argument to 'ComCompatibleVersionAttribute' attribute
                // [assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)]
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "-1").WithArguments("ComCompatibleVersionAttribute"));
        }

        [Fact]
        public void TestComCompatibleVersionAttribute_Invalid_02()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: ComCompatibleVersionAttribute(""str"", 0)]
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,12): error CS7036: There is no argument given that corresponds to the required formal parameter 'build' of 'System.Runtime.InteropServices.ComCompatibleVersionAttribute.ComCompatibleVersionAttribute(int, int, int, int)'
                // [assembly: ComCompatibleVersionAttribute("str", 0)]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, @"ComCompatibleVersionAttribute(""str"", 0)").WithArguments("build", "System.Runtime.InteropServices.ComCompatibleVersionAttribute.ComCompatibleVersionAttribute(int, int, int, int)").WithLocation(4, 12));
        }

        #endregion

        #region WindowsRuntimeImportAttribute

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6190")]
        public void TestWindowsRuntimeImportAttribute()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Delegate, Inherited = false)]
    internal sealed class WindowsRuntimeImportAttribute : Attribute
    {
        public WindowsRuntimeImportAttribute() { }
    }
}

[System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImport]
class A
{
    public static void Main() {}
}
";
            Action<ModuleSymbol> sourceValidator = (ModuleSymbol module) =>
            {
                NamespaceSymbol windowsRuntimeNS = Get_System_Runtime_InteropServices_WindowsRuntime_NamespaceSymbol(module);
                NamedTypeSymbol windowsRuntimeImportAttrType = windowsRuntimeNS.GetTypeMember("WindowsRuntimeImportAttribute");
                NamedTypeSymbol typeA = module.GlobalNamespace.GetTypeMember("A");

                Assert.Equal(1, typeA.GetAttributes(windowsRuntimeImportAttrType).Count());
                Assert.True(typeA.IsWindowsRuntimeImport, "Metadata flag not set for IsWindowsRuntimeImport");
            };

            Action<ModuleSymbol> metadataValidator = (ModuleSymbol module) =>
            {
                NamedTypeSymbol typeA = module.GlobalNamespace.GetTypeMember("A");
                Assert.Equal(0, typeA.GetAttributes().Length);
                Assert.True(typeA.IsWindowsRuntimeImport, "Metadata flag not set for IsWindowsRuntimeImport");
            };

            // Verify that PEVerify will fail despite the fact that compiler produces no errors
            // This is consistent with Dev10 behavior
            //
            // Dev10 PEVerify failure:
            // [token  0x02000003] Type load failed.
            //
            // Dev10 Runtime Exception:
            // Unhandled Exception: System.TypeLoadException: Windows Runtime types can only be declared in Windows Runtime assemblies.

            var verifier = CompileAndVerify(source, sourceSymbolValidator: sourceValidator, symbolValidator: metadataValidator, verify: false);
            verifier.EmitAndVerify("Type load failed.");
        }

        #endregion

        #region DynamicSecurityMethodAttribute

        [Fact]
        public void TestDynamicSecurityMethodAttribute()
        {
            var source = @"
using System;
using System.Security;

namespace System.Security
{
  // DynamicSecurityMethodAttribute:
  //  Indicates that calling the target method requires space for a security
  //  object to be allocated on the callers stack. This attribute is only ever
  //  set on certain security methods defined within mscorlib.
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false )] 
  sealed internal class DynamicSecurityMethodAttribute : System.Attribute
  {
  }
}

class A
{
  [DynamicSecurityMethodAttribute]
  public static void SecurityMethod() { }

  public static void Main()
  {
    SecurityMethod();
  }
}
";
            Action<ModuleSymbol> sourceValidator = (ModuleSymbol module) =>
            {
                NamespaceSymbol securityNS = Get_System_Security_NamespaceSymbol(module);
                NamedTypeSymbol dynamicSecurityMethodAttrType = securityNS.GetTypeMembers("DynamicSecurityMethodAttribute").Single(type => type.DeclaringSyntaxReferences.Any());
                NamedTypeSymbol typeA = module.GlobalNamespace.GetTypeMember("A");
                MethodSymbol method = typeA.GetMember<MethodSymbol>("SecurityMethod");

                Assert.Equal(1, method.GetAttributes(dynamicSecurityMethodAttrType).Count());
                Assert.True(method.RequiresSecurityObject, "Metadata flag RequiresSecurityObject is not set");
            };

            Action<ModuleSymbol> metadataValidator = (ModuleSymbol module) =>
            {
                NamedTypeSymbol typeA = module.GlobalNamespace.GetTypeMember("A");
                MethodSymbol method = typeA.GetMember<MethodSymbol>("SecurityMethod");

                Assert.Equal(0, method.GetAttributes().Length);
                Assert.True(method.RequiresSecurityObject, "Metadata flag RequiresSecurityObject is not set");
            };

            CompileAndVerify(source, sourceSymbolValidator: sourceValidator, symbolValidator: metadataValidator, expectedOutput: "");
        }

        #endregion

        #region ObsoleteAttribute

        [Fact, WorkItem(546062, "DevDiv")]
        public void TestObsoleteAttributeOnTypes()
        {
            var source = @"
using System;

class Test
{
    Class1 field1;
    Class1 Prop1 { get; set; }
    void Method1(Class1 c) {}

    public static void Main()
    {
        Class1 c = null;
        Test t = new Test();
        t.field1 = c;
        t.Prop1 = c;
        t.Method1(new Class1());

        Mydeleg x = (i) => i;
    }
}

[Obsolete(""Do not use this type"", true)]
class Class1 {}

[Obsolete]
[SelfRecursive]
class SelfRecursiveAttribute : Attribute {}

[Obsolete(""Do not use A1"", false)]
[A2]
class A1 : Attribute {}

[Obsolete]
[A1]
class A2: Attribute {}

[A1]
class A3: Attribute {}

class AttrWithType : Attribute
{
    public AttrWithType(Type t) {}
}

[Obsolete]
[Another]
class G<T, U> {}

[Obsolete]
[AttrWithType(typeof(G<int, AnotherAttribute>))]
class AnotherAttribute: Attribute {}

[AttrWithType(typeof(G<int, AnotherAttribute>))]
class AnotherAttribute1: Attribute {}

[System.Obsolete(""This message"" + "" should be concat'ed"", !(false))]
[SelfRecursive1]
class SelfRecursive1Attribute : Attribute {}

[Obsolete]
public delegate int Mydeleg(int x);

[FooAttribute.BarAttribute.Baz]
[Obsolete(""Blah"")]
class FooAttribute : Attribute
{
    class BazAttribute : Attribute { }

    class BarAttribute : FooAttribute { }
}

namespace TypeClashWithNS
{
    class BarAttribute : Attribute {}
}

[TypeClashWithNS.Bar]
class TypeClashWithNS : Attribute
{
    class BarAttribute : Attribute { }
}

interface IFoo<T> {}
[Obsolete]
class SelfReferenceInBase : IFoo<SelfReferenceInBase> {}

class SelfReferenceInBase1 : IFoo<SelfReferenceInBase> {}

";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (78,7): error CS0101: The namespace '<global namespace>' already contains a definition for 'TypeClashWithNS'
                // class TypeClashWithNS : Attribute
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "TypeClashWithNS").WithArguments("TypeClashWithNS", "<global namespace>").WithLocation(78, 7),
                // (53,29): warning CS0612: 'AnotherAttribute' is obsolete
                // [AttrWithType(typeof(G<int, AnotherAttribute>))]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "AnotherAttribute").WithArguments("AnotherAttribute").WithLocation(53, 29),
                // (53,22): warning CS0612: 'G<int, AnotherAttribute>' is obsolete
                // [AttrWithType(typeof(G<int, AnotherAttribute>))]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "G<int, AnotherAttribute>").WithArguments("G<int, AnotherAttribute>").WithLocation(53, 22),
                // (7,5): error CS0619: 'Class1' is obsolete: 'Do not use this type'
                //     Class1 Prop1 { get; set; }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class1").WithArguments("Class1", "Do not use this type").WithLocation(7, 5),
                // (87,35): warning CS0612: 'SelfReferenceInBase' is obsolete
                // class SelfReferenceInBase1 : IFoo<SelfReferenceInBase> {}
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "SelfReferenceInBase").WithArguments("SelfReferenceInBase").WithLocation(87, 35),
                // (6,5): error CS0619: 'Class1' is obsolete: 'Do not use this type'
                //     Class1 field1;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class1").WithArguments("Class1", "Do not use this type").WithLocation(6, 5),
                // (37,2): warning CS0618: 'A1' is obsolete: 'Do not use A1'
                // [A1]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "A1").WithArguments("A1", "Do not use A1").WithLocation(37, 2),
                // (8,18): error CS0619: 'Class1' is obsolete: 'Do not use this type'
                //     void Method1(Class1 c) {}
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class1").WithArguments("Class1", "Do not use this type").WithLocation(8, 18),
                // (12,9): error CS0619: 'Class1' is obsolete: 'Do not use this type'
                //         Class1 c = null;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class1").WithArguments("Class1", "Do not use this type").WithLocation(12, 9),
                // (16,23): error CS0619: 'Class1' is obsolete: 'Do not use this type'
                //         t.Method1(new Class1());
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class1").WithArguments("Class1", "Do not use this type").WithLocation(16, 23),
                // (18,9): warning CS0612: 'Mydeleg' is obsolete
                //         Mydeleg x = (i) => i;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Mydeleg").WithArguments("Mydeleg").WithLocation(18, 9));
        }

        [Fact]
        public void TestObsoleteAttributeOnMembers()
        {
            var source = @"
using System;

public class Test
{
    public static void Main()
    {
        ObsoleteMethod1();
        ObsoleteMethod2();
        ObsoleteMethod3();
        ObsoleteMethod5();

        Test t = new Test();

        t.ObsoleteMethod4();
        var f = t.field1;
        var p1 = t.Property1;
        var p2 = t.Property2;
        t.event1();
        t.event1 += () => { };

        t.ObsoleteExtensionMethod1();

        Action<int> func = t.ObsoleteMethod4;
        func(1);
        Action func1 = t.ObsoleteMethod4;
        func1();
        Test t1 = new Test { Property1 = 10, Property2 =20};
        var i1 = t1[10];

        GenericTest<int> gt = new GenericTest<int>();
        gt.ObsoleteMethod1<double>();
        var gf = gt.field1;
        var gp1 = gt.Property1;
        gt.event1 += (i) => { };
    }

    [Obsolete]
    public static void ObsoleteMethod1() { }

    [Obsolete(""Do not call this method"")]
    public static void ObsoleteMethod2() { }

    [Obsolete("""", true)]
    public static void ObsoleteMethod3() { }

    [Obsolete(""Do not call this method"")]
    public void ObsoleteMethod4() { }

    [Obsolete(""Do not call this method"")]
    public void ObsoleteMethod4(int x) { }

    [Obsolete(null, true)]
    public static void ObsoleteMethod5() { }

    [Obsolete(""Do not use this field"")]
    public int field1 = 0;

    [Obsolete(""Do not use this property"")]
    public int Property1 { get; set; }

    [Obsolete(""Do not use this property"")]
    public int Property2 { get { return 10; } set { } }

    [Obsolete(""Do not use this event"")]
    public event Action event1;

    public int Prop2
    {
        [Obsolete] get { return 10; }
    }

    public int Prop3
    {
        get { return 10; }
        [Obsolete] set { }
    }

    public event Action event2
    {
        [Obsolete] add {}
        [Obsolete(""Don't use remove accessor"")] remove {}
    }

    [Obsolete]
    public int this[int x]
    {
        get { return 10; }
    }
}

public class GenericTest<T>
{
    [Obsolete]
    public void ObsoleteMethod1<U>() { }

    [Obsolete(""Do not use this field"")]
    public T field1 = default(T);

    [Obsolete(""Do not use this property"")]
    public T Property1 { get; set; }

    [Obsolete(""Do not use this event"")]
    public event Action<T> event1;
}

public static class TestExtension
{
    [Obsolete(""Do not call this extension method"")]
    public static void ObsoleteExtensionMethod1(this Test t) { }
}
";
            CreateCompilationWithMscorlib(source, new[] { ExtensionAssemblyRef }).VerifyDiagnostics(
                // (65,10): error CS1667: Attribute 'Obsolete' is not valid on property or event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Obsolete] get { return 10; }
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate"),
                // (71,10): error CS1667: Attribute 'Obsolete' is not valid on property or event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Obsolete] set { }
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate"),
                // (76,10): error CS1667: Attribute 'Obsolete' is not valid on property or event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Obsolete] add {}
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate"),
                // (77,10): error CS1667: Attribute 'Obsolete' is not valid on property or event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Obsolete("Don't use remove accessor")] remove {}
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate"),
                // (8,9): warning CS0612: 'Test.ObsoleteMethod1()' is obsolete
                //         ObsoleteMethod1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "ObsoleteMethod1()").WithArguments("Test.ObsoleteMethod1()"),
                // (9,9): warning CS0618: 'Test.ObsoleteMethod2()' is obsolete: 'Do not call this method'
                //         ObsoleteMethod2();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "ObsoleteMethod2()").WithArguments("Test.ObsoleteMethod2()", "Do not call this method"),
                // (10,9): error CS0619: 'Test.ObsoleteMethod3()' is obsolete: ''
                //         ObsoleteMethod3();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "ObsoleteMethod3()").WithArguments("Test.ObsoleteMethod3()", ""),
                // (11,9): warning CS0612: 'Test.ObsoleteMethod5()' is obsolete
                //         ObsoleteMethod5();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "ObsoleteMethod5()").WithArguments("Test.ObsoleteMethod5()"),
                // (14,9): warning CS0618: 'Test.ObsoleteMethod4()' is obsolete: 'Do not call this method'
                //         t.ObsoleteMethod4();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteMethod4()").WithArguments("Test.ObsoleteMethod4()", "Do not call this method"),
                // (15,17): warning CS0618: 'Test.field1' is obsolete: 'Do not use this field'
                //         var f = t.field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.field1").WithArguments("Test.field1", "Do not use this field"),
                // (16,18): warning CS0618: 'Test.Property1' is obsolete: 'Do not use this property'
                //         var p1 = t.Property1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.Property1").WithArguments("Test.Property1", "Do not use this property"),
                // (17,18): warning CS0618: 'Test.Property2' is obsolete: 'Do not use this property'
                //         var p2 = t.Property2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.Property2").WithArguments("Test.Property2", "Do not use this property"),
                // (19,9): warning CS0618: 'Test.event1' is obsolete: 'Do not use this event'
                //         t.event1 += () => { };
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.event1").WithArguments("Test.event1", "Do not use this event"),
                // (21,9): warning CS0618: 'TestExtension.ObsoleteExtensionMethod1(Test)' is obsolete: 'Do not call this extension method'
                //         t.ObsoleteExtensionMethod1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteExtensionMethod1()").WithArguments("TestExtension.ObsoleteExtensionMethod1(Test)", "Do not call this extension method"),
                // (23,28): warning CS0618: 'Test.ObsoleteMethod4(int)' is obsolete: 'Do not call this method'
                //         Action<int> func = t.ObsoleteMethod4;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteMethod4").WithArguments("Test.ObsoleteMethod4(int)", "Do not call this method"),
                // (25,24): warning CS0618: 'Test.ObsoleteMethod4()' is obsolete: 'Do not call this method'
                //         Action func1 = t.ObsoleteMethod4;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteMethod4").WithArguments("Test.ObsoleteMethod4()", "Do not call this method"),
                // (29,30): warning CS0618: 'Test.Property1' is obsolete: 'Do not use this property'
                //         Test t1 = new Test { Property1 = 10, Property2 =20};
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Property1").WithArguments("Test.Property1", "Do not use this property"),
                // (29,46): warning CS0618: 'Test.Property2' is obsolete: 'Do not use this property'
                //         Test t1 = new Test { Property1 = 10, Property2 =20};
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Property2").WithArguments("Test.Property2", "Do not use this property"),
                // (28,18): warning CS0612: 'Test.this[int]' is obsolete
                //         var i1 = t1[10];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "t1[10]").WithArguments("Test.this[int]"),
                // (30,9): warning CS0612: 'GenericTest<int>.ObsoleteMethod1<U>()' is obsolete
                //         gt.ObsoleteMethod1<U>();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "gt.ObsoleteMethod1<double>()").WithArguments("GenericTest<int>.ObsoleteMethod1<U>()"),
                // (31,18): warning CS0618: 'GenericTest<int>.field1' is obsolete: 'Do not use this field'
                //         var gf = gt.field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "gt.field1").WithArguments("GenericTest<int>.field1", "Do not use this field"),
                // (32,19): warning CS0618: 'GenericTest<int>.Property1' is obsolete: 'Do not use this property'
                //         var gp1 = gt.Property1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "gt.Property1").WithArguments("GenericTest<int>.Property1", "Do not use this property"),
                // (33,9): warning CS0618: 'GenericTest<int>.event1' is obsolete: 'Do not use this event'
                //         gt.event1 += (i) => { };
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "gt.event1").WithArguments("GenericTest<int>.event1", "Do not use this event"),
                // (104,28): warning CS0067: The event 'GenericTest<T>.event1' is never used
                //     public event Action<T> event1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "event1").WithArguments("GenericTest<T>.event1"));
        }

        [Fact]
        public void TestObsoleteAttributeOnOperators()
        {
            var source = @"
using System;

public class Test
{
    public static void Main()
    {
        Test t = new Test();
        t = 10;
        t = (Test)""10"";

        Test c = new Test();
        Test c1 = -c;
        Test c2 = c++;
        bool b1 = c? true: false;
        if (c && c1)
        {
          c1 += c;
        }
    }

    [Obsolete]
    static public implicit operator Test(int value) { return new Test(); }

    [Obsolete]
    static public explicit operator Test(string value) { return new Test(); }

    [Obsolete]
    static public Test operator -(Test x) { return new Test(); }

    [Obsolete]
    static public Test operator ++(Test x) { return new Test(); }

    [Obsolete]
    static public bool operator true(Test x) { return true; }
    [Obsolete]
    static public bool operator false(Test x) { return false; }

    [Obsolete]
    static public Test operator +(Test x, Test y) { return new Test(); }

    [Obsolete]
    static public Test operator &(Test x, Test x2) { return new Test(); }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (27,13): warning CS0612: 'Test.implicit operator Test(int)' is obsolete
                //         t = 10;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "10").WithArguments("Test.implicit operator Test(int)"),
                // (28,13): warning CS0612: 'Test.explicit operator Test(string)' is obsolete
                //         t = (Test)"10";
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, @"(Test)""10""").WithArguments("Test.explicit operator Test(string)"),
                // (13,19): warning CS0612: 'Test.operator -(Test)' is obsolete
                //         Test c1 = -c;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "-c").WithArguments("Test.operator -(Test)"),
                // (14,19): warning CS0612: 'Test.operator ++(Test)' is obsolete
                //         Test c2 = c++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c++").WithArguments("Test.operator ++(Test)"),
                // (15,19): warning CS0612: 'Test.operator true(Test)' is obsolete
                //         bool b1 = c? true: false;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c").WithArguments("Test.operator true(Test)"),
                // (16,13): warning CS0612: 'Test.operator &(Test, Test)' is obsolete
                //         if (c && c1)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c && c1").WithArguments("Test.operator &(Test, Test)"),
                // (16,13): warning CS0612: 'Test.operator true(Test)' is obsolete
                //         if (c && c1)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c && c1").WithArguments("Test.operator true(Test)"),
                // (18,11): warning CS0612: 'Test.operator +(Test, Test)' is obsolete
                //           c1 += c;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c1 += c").WithArguments("Test.operator +(Test, Test)"));
        }

        [Fact, WorkItem(546062, "DevDiv")]
        public void TestObsoleteAttributeInMetadata()
        {
            var peSource = @"
using System;

[Obsolete]
public class TestClass1 {}

[Obsolete(""TestClass2 is obsolete"")]
public class TestClass2 {}

[Obsolete(""Do not use TestClass3"", true)]
public class TestClass3 {}

[Obsolete(""TestClass4 is obsolete"", false)]
public class TestClass4 {}

public class TestClass
{
    [Obsolete(""Do not use TestMethod"")]
    public void TestMethod() {}

    [Obsolete(""Do not use Prop1"", false)]
    public int Prop1 { get; set; }

    [Obsolete(""Do not use field1"", true)]
    public TestClass field1;

    [Obsolete(""Do not use event"", true)]
    public Action event1;
}
";
            var peReference = MetadataReference.CreateFromStream(CreateCompilationWithMscorlib(peSource).EmitToStream());

            var source = @"
public class Test
{
    public static void foo1(TestClass1 c) {}
    public static void foo2(TestClass2 c) {}
    public static void foo3(TestClass3 c) {}
    public static void foo4(TestClass4 c) {}

    public static void Main()
    {
        TestClass c = new TestClass();
        c.TestMethod();
        var i = c.Prop1;
        c = c.field1;
        c.event1();
        c.event1 += () => {};
    }
}
";
            CreateCompilationWithMscorlib(source, new[] { peReference }).VerifyDiagnostics(
                // (4,29): warning CS0612: 'TestClass1' is obsolete
                //     public static void foo1(TestClass1 c) {}
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "TestClass1").WithArguments("TestClass1"),
                // (5,29): warning CS0618: 'TestClass2' is obsolete: 'TestClass2 is obsolete'
                //     public static void foo2(TestClass2 c) {}
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "TestClass2").WithArguments("TestClass2", "TestClass2 is obsolete"),
                // (6,29): error CS0619: 'TestClass3' is obsolete: 'Do not use TestClass3'
                //     public static void foo3(TestClass3 c) {}
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "TestClass3").WithArguments("TestClass3", "Do not use TestClass3"),
                // (7,29): warning CS0618: 'TestClass4' is obsolete: 'TestClass4 is obsolete'
                //     public static void foo4(TestClass4 c) {}
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "TestClass4").WithArguments("TestClass4", "TestClass4 is obsolete"),
                // (12,9): warning CS0618: 'TestClass.TestMethod()' is obsolete: 'Do not use TestMethod'
                //         c.TestMethod();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c.TestMethod()").WithArguments("TestClass.TestMethod()", "Do not use TestMethod"),
                // (13,17): warning CS0618: 'TestClass.Prop1' is obsolete: 'Do not use Prop1'
                //         var i = c.Prop1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c.Prop1").WithArguments("TestClass.Prop1", "Do not use Prop1"),
                // (14,13): error CS0619: 'TestClass.field1' is obsolete: 'Do not use field1'
                //         c = c.field1;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "c.field1").WithArguments("TestClass.field1", "Do not use field1"),
                // (15,9): error CS0619: 'TestClass.event1' is obsolete: 'Do not use event'
                //         c.event1();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "c.event1").WithArguments("TestClass.event1", "Do not use event"),
                // (16,9): error CS0619: 'TestClass.event1' is obsolete: 'Do not use event'
                //         c.event1 += () => {};
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "c.event1").WithArguments("TestClass.event1", "Do not use event"));
        }

        [Fact]
        public void TestObsoleteAttributeOnOverriddenMembers()
        {
            var source = @"
using System;
class C1
{
    public virtual void foo() {}
}
class C2 : C1
{
    [Obsolete]
    public override void foo() {}
}
class C3 : C1
{
    [Obsolete]
    public new void foo() {}
}
class C4 : C1
{
    public override void foo() {}
}
class C5 : C4
{
    [Obsolete]
    public override void foo() {}
}
class C6 : C5
{
    public override void foo() {}
}

class D1
{
    [Obsolete]
    public virtual void foo() {}
}
class D2 : D1
{
    public override void foo() {}
}
class D3 : D1
{
    public new void foo() {}
}
class D4 : D1
{
    [Obsolete]
    public override void foo() {}
}
class D5 : D4
{
    public override void foo() {}
}
class D6 : D5
{
    [Obsolete]
    public override void foo() {}
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,26): warning CS0809: Obsolete member 'C2.foo()' overrides non-obsolete member 'C1.foo()'
                //     public override void foo() {}
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "foo").WithArguments("C2.foo()", "C1.foo()"),
                // (24,26): warning CS0809: Obsolete member 'C5.foo()' overrides non-obsolete member 'C1.foo()'
                //     public override void foo() {}
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "foo").WithArguments("C5.foo()", "C1.foo()"),
                // (38,26): warning CS0672: Member 'D2.foo()' overrides obsolete member 'D1.foo()'. Add the Obsolete attribute to 'D2.foo()'.
                //     public override void foo() {}
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "foo").WithArguments("D2.foo()", "D1.foo()"),
                // (51,26): warning CS0672: Member 'D5.foo()' overrides obsolete member 'D1.foo()'. Add the Obsolete attribute to 'D5.foo()'.
                //     public override void foo() {}
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "foo").WithArguments("D5.foo()", "D1.foo()"));
        }

        [Fact]
        public void TestObsoleteAttributeCycles()
        {
            var source = @"
using System;

public class Test
{
    [Obsolete(""F1 is obsolete"")]
    [SomeAttr(F1)]
    public const int F1 = 10;

    [Obsolete(""F2 is obsolete"", true)]
    [SomeAttr(F3)]
    public const int F2 = 10;
    
    [Obsolete(""F3 is obsolete"")]
    [SomeAttr(F2)]
    public const int F3 = 10;

    [Obsolete(F4, true)]
    public const string F4 = ""blah"";

    [Obsolete(F5)]
    public string F5 = ""blah"";

    [Obsolete(P1, true)]
    public string P1 { get { return ""blah""; } }

    [Obsolete]
    [SomeAttr(P2, true)]
    public string P2 { get { return ""blah""; } }

    [Obsolete(Method1)]
    public void Method1() {}

    [Obsolete()]
    [SomeAttr1(Method2)]
    public void Method2() {}

    [Obsolete(F6)]
    [SomeAttr(F6)]
    [SomeAttr(F7)]
    public const string F6 = ""F6 is obsolete"";

    [Obsolete(F7, true)]
    [SomeAttr(F6)]
    [SomeAttr(F7)]
    public const string F7 = ""F7 is obsolete"";
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class SomeAttr: Attribute
{
    public SomeAttr(int x) {}
    public SomeAttr(string x) {}
}
public class SomeAttr1: Attribute
{
    public SomeAttr1(Action x) {}
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (7,15): warning CS0618: 'Test.F1' is obsolete: 'F1 is obsolete'
                //     [SomeAttr(F1)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F1").WithArguments("Test.F1", "F1 is obsolete"),
                // (15,15): error CS0619: 'Test.F2' is obsolete: 'F2 is obsolete'
                //     [SomeAttr(F2)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F2").WithArguments("Test.F2", "F2 is obsolete"),
                // (11,15): warning CS0618: 'Test.F3' is obsolete: 'F3 is obsolete'
                //     [SomeAttr(F3)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F3").WithArguments("Test.F3", "F3 is obsolete"),
                // (18,15): error CS0619: 'Test.F4' is obsolete: 'blah'
                //     [Obsolete(F4, true)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F4").WithArguments("Test.F4", "blah"),
                // (21,15): error CS0120: An object reference is required for the non-static field, method, or property 'Test.F5'
                //     [Obsolete(F5)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F5").WithArguments("Test.F5"),
                // (24,15): error CS0120: An object reference is required for the non-static field, method, or property 'Test.P1'
                //     [Obsolete(P1, true)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P1").WithArguments("Test.P1"),
                // (28,15): warning CS0612: 'Test.P2' is obsolete
                //     [SomeAttr(P2, true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "P2").WithArguments("Test.P2"),
                // (28,15): error CS0120: An object reference is required for the non-static field, method, or property 'Test.P2'
                //     [SomeAttr(P2, true)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P2").WithArguments("Test.P2"),
                // (31,15): error CS1503: Argument 1: cannot convert from 'method group' to 'string'
                //     [Obsolete(Method1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "Method1").WithArguments("1", "method group", "string"),
                // (35,16): warning CS0612: 'Test.Method2()' is obsolete
                //     [SomeAttr1(Method2)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Method2").WithArguments("Test.Method2()"),
                // (35,6): error CS0181: Attribute constructor parameter 'x' has type 'System.Action', which is not a valid attribute parameter type
                //     [SomeAttr1(Method2)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "SomeAttr1").WithArguments("x", "System.Action"),
                // (43,15): error CS0619: 'Test.F7' is obsolete: 'F7 is obsolete'
                //     [Obsolete(F7, true)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F7").WithArguments("Test.F7", "F7 is obsolete"),
                // (44,15): warning CS0618: 'Test.F6' is obsolete: 'F6 is obsolete'
                //     [SomeAttr(F6)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F6").WithArguments("Test.F6", "F6 is obsolete"),
                // (45,15): error CS0619: 'Test.F7' is obsolete: 'F7 is obsolete'
                //     [SomeAttr(F7)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F7").WithArguments("Test.F7", "F7 is obsolete"),
                // (38,15): warning CS0618: 'Test.F6' is obsolete: 'F6 is obsolete'
                //     [Obsolete(F6)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F6").WithArguments("Test.F6", "F6 is obsolete"),
                // (39,15): warning CS0618: 'Test.F6' is obsolete: 'F6 is obsolete'
                //     [SomeAttr(F6)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F6").WithArguments("Test.F6", "F6 is obsolete"),
                // (40,15): error CS0619: 'Test.F7' is obsolete: 'F7 is obsolete'
                //     [SomeAttr(F7)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F7").WithArguments("Test.F7", "F7 is obsolete"));
        }

        [WorkItem(546064, "DevDiv")]
        [Fact]
        public void TestObsoleteAttributeCycles_02()
        {
            var source = @"
[Foo]
class Foo: Base {}

[Foo]
class Base: System.Attribute
{
    public class Nested: Foo {}
}
";
            CompileAndVerify(source);

            source = @"
using System;

[Obsolete]
public class SomeType
{
    public static SomeType Instance;
    public const  string Message = ""foo"";
}

public class SomeAttr : Attribute
{
    public SomeAttr(string message) {}
}

[Obsolete(SomeType.Message)]
public class Derived : Base
{
}

public class Base
{
    [Obsolete(SomeType.Message)]
    public SomeType SomeProp { get; set; }
}
";
            CreateCompilationWithMscorlib(source, null, TestOptions.ReleaseDll.WithConcurrentBuild(false)).VerifyDiagnostics(
                // (23,15): warning CS0612: 'SomeType' is obsolete
                //     [Obsolete(SomeType.Message)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "SomeType").WithArguments("SomeType"));
        }

        [Fact]
        public void TestObsoleteAttributeSuppress()
        {
            var source = @"

using System;
[Obsolete]
public class SomeType
{
    public static SomeType Instance;
    public const  string Message = ""foo"";
}

public class Test
{
    [Obsolete]
    SomeType someField = SomeType.Instance;

    [Obsolete]
    Func<SomeType> someFuncField = () => new SomeType();

    [Obsolete]
    event Action<SomeType> someEvent;

    [Obsolete]
    public static SomeType someProp { get; set; }

    [Obsolete]
    SomeType this[int x] { get { SomeType y = new SomeType(); return y; } }

    [Obsolete]
    SomeType foo(SomeType x)
    {
        SomeType y = new SomeType();
        return x;
    }

    [Obsolete]
    Test(SomeType x)
    {
        SomeType y = new SomeType();
    }
}
[Obsolete]
public class Base<T> {}
[Obsolete]
public class Derived : Base<Base<int>> {}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (20,28): warning CS0067: The event 'Test.someEvent' is never used
                //     event Action<SomeType> someEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "someEvent").WithArguments("Test.someEvent"));
        }

        [Fact]
        public void TestNestedTypeMember()
        {
            var source = @"
using System;
using System.Diagnostics;

[Conditional(Nested.ConstStr)]
[Outer]  // this attribute should not be emitted
class Outer: Attribute
{
  class Nested
  {
      public const string ConstStr = ""str"";
  }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestObsoleteAttributeP2PReference()
        {
            string s = @"
using System;
[Obsolete]
public class C { 
    [Obsolete]
    public void Foo() {} 
}
";
            var other = CreateCompilationWithMscorlib(s);

            s = @"
public class A
{
    protected A(C o)
    {
        o.Foo();
    }
}
";
            CreateCompilationWithMscorlib(s, new[] { new CSharpCompilationReference(other) }).VerifyDiagnostics(
                // (3,17): warning CS0612: 'C' is obsolete
                //     protected A(C o)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "C").WithArguments("C"),
                // (5,9): warning CS0612: 'C.Foo()' is obsolete
                //         o.Foo();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "o.Foo()").WithArguments("C.Foo()"));
        }

        [Fact]
        [WorkItem(546455, "DevDiv"), WorkItem(546456, "DevDiv"), WorkItem(546457, "DevDiv")]
        public void TestObsoleteAttributeOnCollectionInitializer()
        {
            var source = @"
using System;
using System.Collections;

class Test
{
    public static void Main()
    {
        B coll = new B { 1, new B(), ""a"", false };
    }
}

public class B : IEnumerable
{
    [Obsolete()]
    public void Add(long i)
    {
    }
    [Obsolete(""Don't use this overload"")]
    public void Add(B i)
    {
    }
    [Obsolete(""Don't use this overload"", true)]
    public void Add(string s)
    {
    }
    [Obsolete(null, true)]
    public void Add(bool s)
    {
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return null;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (9,26): warning CS1064: The best overloaded Add method 'B.Add(long)' for the collection initializer element is obsolete.
                //         B coll = new B { 1, new B(), "a", false };
                Diagnostic(ErrorCode.WRN_DeprecatedCollectionInitAdd, "1").WithArguments("B.Add(long)"),
                // (9,29): warning CS1062: The best overloaded Add method 'B.Add(B)' for the collection initializer element is obsolete. Don't use this overload
                //         B coll = new B { 1, new B(), "a", false };
                Diagnostic(ErrorCode.WRN_DeprecatedCollectionInitAddStr, "new B()").WithArguments("B.Add(B)", "Don't use this overload"),
                // (9,38): error CS1063: The best overloaded Add method 'B.Add(string)' for the collection initializer element is obsolete. Don't use this overload
                //         B coll = new B { 1, new B(), "a", false };
                Diagnostic(ErrorCode.ERR_DeprecatedCollectionInitAddStr, @"""a""").WithArguments("B.Add(string)", "Don't use this overload"),
                // (9,43): warning CS1064: The best overloaded Add method 'B.Add(bool)' for the collection initializer element is obsolete.
                //         B coll = new B { 1, new B(), "a", false };
                Diagnostic(ErrorCode.WRN_DeprecatedCollectionInitAdd, "false").WithArguments("B.Add(bool)"));
        }

        [Fact]
        [WorkItem(546636, "DevDiv")]
        public void TestObsoleteAttributeOnAttributes()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class Att : Attribute
{
    [Obsolete(""Constructor"", true)]
    public Att() { }
    [Obsolete(""Property"", true)]
    public int Prop
    {
        get { return 1; }
        set { }
    }
    [Obsolete(""Field"", true)]
    public int Field;
}

[Att]
[Att(Field = 1)]
[Att(Prop = 1)]
public class Test
{
    [Att()]
    public static void Main() { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (20,6): error CS0619: 'Att.Field' is obsolete: 'Field'
                // [Att(Field = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Field = 1").WithArguments("Att.Field", "Field"),
                // (20,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Field = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Field = 1)").WithArguments("Att.Att()", "Constructor"),
                // (21,6): error CS0619: 'Att.Prop' is obsolete: 'Property'
                // [Att(Prop = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop = 1").WithArguments("Att.Prop", "Property"),
                // (21,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Prop = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Prop = 1)").WithArguments("Att.Att()", "Constructor"),
                // (24,6): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                //     [Att()]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att()").WithArguments("Att.Att()", "Constructor"),
                // (19,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att").WithArguments("Att.Att()", "Constructor"));
        }

        [Fact]
        public void TestObsoleteAttributeOnMembers2()
        {
            var source = @"
using System;

namespace A.B
{
    [Obsolete]
    public class C
    {
        [Obsolete]
        public static int Field1 = 10;

        [Obsolete]
        public class D
        {
            [Obsolete]
            public static int Field2 = 20;
        }
    }

    [Obsolete]
    public class C1
    {
        public class D
        {
        }
    }

    [Obsolete]
    public class C2<T>
    {
        [Obsolete]
        public static int Field1 = 10;

        public class D { }

        [Obsolete]
        public class E<U> { }
    }
}

class B<T> { }
class D : B<A.B.C1.D> { }
class D1 : B<A.B.C2<int>.D> { }

class Program
{
    static void Main(string[] args)
    {
        var x = A.B.C.Field1;
        var x1 = A.B.C.D.Field2;
        var y = new A.B.C1.D();
        var y1 = new A.B.C2<int>.D();
        var y2 = A.B.C2<int>.Field1;
        var y3 = new A.B.C2<int>.E<int>();
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (42,13): warning CS0612: 'A.B.C1' is obsolete
                // class D : B<A.B.C1.D> { }
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C1").WithArguments("A.B.C1"),
                // (43,14): warning CS0612: 'A.B.C2<int>' is obsolete
                // class D1 : B<A.B.C2<int>.D> { }
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C2<int>").WithArguments("A.B.C2<int>"),
                // (49,17): warning CS0612: 'A.B.C' is obsolete
                //         var x = A.B.C.Field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C").WithArguments("A.B.C"),
                // (49,17): warning CS0612: 'A.B.C.Field1' is obsolete
                //         var x = A.B.C.Field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C.Field1").WithArguments("A.B.C.Field1"),
                // (50,18): warning CS0612: 'A.B.C' is obsolete
                //         var x1 = A.B.C.D.Field2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C").WithArguments("A.B.C"),
                // (50,18): warning CS0612: 'A.B.C.D' is obsolete
                //         var x1 = A.B.C.D.Field2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C.D").WithArguments("A.B.C.D"),
                // (50,18): warning CS0612: 'A.B.C.D.Field2' is obsolete
                //         var x1 = A.B.C.D.Field2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C.D.Field2").WithArguments("A.B.C.D.Field2"),
                // (51,21): warning CS0612: 'A.B.C1' is obsolete
                //         var y = new A.B.C1.D();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C1").WithArguments("A.B.C1"),
                // (52,22): warning CS0612: 'A.B.C2<int>' is obsolete
                //         var y1 = new A.B.C2<int>.D();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C2<int>").WithArguments("A.B.C2<int>"),
                // (53,18): warning CS0612: 'A.B.C2<int>' is obsolete
                //         var y2 = A.B.C2<int>.Field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C2<int>").WithArguments("A.B.C2<int>"),
                // (53,18): warning CS0612: 'A.B.C2<int>.Field1' is obsolete
                //         var y2 = A.B.C2<int>.Field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C2<int>.Field1").WithArguments("A.B.C2<int>.Field1"),
                // (54,22): warning CS0612: 'A.B.C2<int>' is obsolete
                //         var y3 = new A.B.C2<int>.E<int>();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C2<int>").WithArguments("A.B.C2<int>"),
                // (54,22): warning CS0612: 'A.B.C2<int>.E<int>' is obsolete
                //         var y3 = new A.B.C2<int>.E<int>();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A.B.C2<int>.E<int>").WithArguments("A.B.C2<int>.E<int>"));
        }

        [Fact]
        [WorkItem(546766, "DevDiv")]
        public void TestObsoleteAttributeOnMembers3()
        {
            var source = @"
using System;
 
class C
{
    [Obsolete(""Do not use"", true)]
    public C() { }
}
class D : C
{
    public D() { }
}
class E : C
{
    public E() : base() { }
}

class Event1
{
    [Obsolete(""Do not use"")]
    public event Action A;

    [field:Obsolete(""Do not use"")]
    public event Action A1;
  
    public void Test()
    {
        A();
        A1();
        A += () => {};
        A1 += () => {};
    }

    public static void Test1()
    {
        var e = new Event1();
        e.A();
        e.A1();
        e.A += () => {};
        e.A1 += () => {};
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,5): error CS0619: 'C.C()' is obsolete: 'Do not use'
                //     public D() { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "public D() { }").WithArguments("C.C()", "Do not use"),
                // (15,16): error CS0619: 'C.C()' is obsolete: 'Do not use'
                //     public E() : base() { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, ": base()").WithArguments("C.C()", "Do not use"),
                // (29,9): warning CS0618: 'Event1.A1' is obsolete: 'Do not use'
                //         A1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "A1").WithArguments("Event1.A1", "Do not use"),
                // (30,9): warning CS0618: 'Event1.A' is obsolete: 'Do not use'
                //         A += () => {};
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "A").WithArguments("Event1.A", "Do not use"),
                // (38,9): warning CS0618: 'Event1.A1' is obsolete: 'Do not use'
                //         e.A1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "e.A1").WithArguments("Event1.A1", "Do not use"),
                // (39,9): warning CS0618: 'Event1.A' is obsolete: 'Do not use'
                //         e.A += () => {};
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "e.A").WithArguments("Event1.A", "Do not use"));
        }

        [Fact]
        [WorkItem(547024, "DevDiv")]
        public void TestObsoleteForeachMembers()
        {
            var source =
@"using System;
public class MyEnumerator : IDisposable
{
    [Obsolete(""1"", false)]
    public bool MoveNext()
    {
        return false;
    }

    [Obsolete(""2"", false)]
    public int Current
    {
        get { return 0; }
    }

    [Obsolete(""3"", false)]
    public void Dispose()
    {
    }
}

class Foreachable
{
    [Obsolete(""4"", false)]
    public MyEnumerator GetEnumerator()
    {
        return new MyEnumerator();
    }
}

class Program
{
    public static void Main(string[] args)
    {
        foreach (var x in new Foreachable())
        {
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (35,9): warning CS0618: 'Foreachable.GetEnumerator()' is obsolete: '4'
                //         foreach (var x in new Foreachable())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "foreach").WithArguments("Foreachable.GetEnumerator()", "4"),
                // (35,9): warning CS0618: 'MyEnumerator.MoveNext()' is obsolete: '1'
                //         foreach (var x in new Foreachable())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "foreach").WithArguments("MyEnumerator.MoveNext()", "1"),
                // (35,9): warning CS0618: 'MyEnumerator.Current' is obsolete: '2'
                //         foreach (var x in new Foreachable())
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "foreach").WithArguments("MyEnumerator.Current", "2")
            );
        }

        [Fact]
        public void TestObsoleteAttributeSuppress2()
        {
            var source = @"
using System; 

using X = A;
using Y = A.B; 
[Obsolete(""Do not use"")]
class A { 
    public class B {  } 
}

[Obsolete]
interface I1 { void M(); }
#pragma warning disable 612
internal sealed class C1 : I1
#pragma warning restore 612
{
    void I1.M() {}
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,1): info CS8019: Unnecessary using directive.
                // using X = A;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = A;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using Y = A.B; 
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = A.B;"));
        }

        [Fact]
        [WorkItem(531071, "DevDiv")]
        public void TestObsoleteTypeParameterInAlias()
        {
            var source =
@"using System;
public class List<T> { }
namespace N
{
    using X = A;
    using Y = List<A>;
    using Z = List<A[]>;
    [Obsolete(""Do not use"", true)]
    public class A { }
    public class B : X { }
    public class C : Y { }
    public class E : Z { }
    public class D : List<Y>
    {
        public X x;
        public Y y1;
        public List<Y> y2;
        public Z z;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,22): error CS0619: 'N.A' is obsolete: 'Do not use'
                //     public class E : Z { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Z").WithArguments("N.A", "Do not use"),
                // (13,27): error CS0619: 'N.A' is obsolete: 'Do not use'
                //     public class D : List<Y>
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use"),
                // (10,22): error CS0619: 'N.A' is obsolete: 'Do not use'
                //     public class B : X { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "X").WithArguments("N.A", "Do not use"),
                // (11,22): error CS0619: 'N.A' is obsolete: 'Do not use'
                //     public class C : Y { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use"),
                // (16,16): error CS0619: 'N.A' is obsolete: 'Do not use'
                //         public Y y1;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use"),
                // (17,21): error CS0619: 'N.A' is obsolete: 'Do not use'
                //         public List<Y> y2;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use"),
                // (18,16): error CS0619: 'N.A' is obsolete: 'Do not use'
                //         public Z z;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Z").WithArguments("N.A", "Do not use"),
                // (15,16): error CS0619: 'N.A' is obsolete: 'Do not use'
                //         public X x;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "X").WithArguments("N.A", "Do not use")
                );
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_OnBase()
        {
            var source = @"
using System;

public class A
{
    [Obsolete]
    public virtual event Action E { add { } remove { } }
    [Obsolete]
    public virtual int P { get; set; }
    [Obsolete]
    public virtual void M() { }
}

public class B : A
{
    public override event Action E { add { } remove { } }
    public override int P { get; set; }
    public override void M() { }
}

public class C : B
{
    public override event Action E { add { } remove { } }
    public override int P { get; set; }
    public override void M() { }
}

class Test
{
    void M(A a, B b, C c)
    {
        a.E += null;
        a.P++;
        a.M();

        b.E += null;
        b.P++;
        b.M();

        c.E += null;
        c.P++;
        c.M();
    }
}
";
            // All member accesses produce obsolete warnings.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (17,25): warning CS0672: Member 'B.P' overrides obsolete member 'A.P'. Add the Obsolete attribute to 'B.P'.
                //     public override int P { get; set; }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "P").WithArguments("B.P", "A.P"),
                // (18,26): warning CS0672: Member 'B.M()' overrides obsolete member 'A.M()'. Add the Obsolete attribute to 'B.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("B.M()", "A.M()"),
                // (16,34): warning CS0672: Member 'B.E' overrides obsolete member 'A.E'. Add the Obsolete attribute to 'B.E'.
                //     public override event Action E { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "E").WithArguments("B.E", "A.E"),
                // (24,25): warning CS0672: Member 'C.P' overrides obsolete member 'A.P'. Add the Obsolete attribute to 'C.P'.
                //     public override int P { get; set; }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "P").WithArguments("C.P", "A.P"),
                // (25,26): warning CS0672: Member 'C.M()' overrides obsolete member 'A.M()'. Add the Obsolete attribute to 'C.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("C.M()", "A.M()"),
                // (23,34): warning CS0672: Member 'C.E' overrides obsolete member 'A.E'. Add the Obsolete attribute to 'C.E'.
                //     public override event Action E { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "E").WithArguments("C.E", "A.E"),

                // (32,9): warning CS0612: 'A.E' is obsolete
                //         a.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "a.E").WithArguments("A.E"),
                // (33,9): warning CS0612: 'A.P' is obsolete
                //         a.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "a.P").WithArguments("A.P"),
                // (34,9): warning CS0612: 'A.M()' is obsolete
                //         a.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "a.M()").WithArguments("A.M()"),
                // (36,9): warning CS0612: 'A.E' is obsolete
                //         b.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "b.E").WithArguments("A.E"),
                // (37,9): warning CS0612: 'A.P' is obsolete
                //         b.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "b.P").WithArguments("A.P"),
                // (38,9): warning CS0612: 'A.M()' is obsolete
                //         b.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "b.M()").WithArguments("A.M()"),
                // (40,9): warning CS0612: 'A.E' is obsolete
                //         c.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.E").WithArguments("A.E"),
                // (41,9): warning CS0612: 'A.P' is obsolete
                //         c.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.P").WithArguments("A.P"),
                // (42,9): warning CS0612: 'A.M()' is obsolete
                //         c.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.M()").WithArguments("A.M()"));
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_OnDerived()
        {
            var source = @"
using System;

public class A
{
    public virtual event Action E { add { } remove { } }
    public virtual int P { get; set; }
    public virtual void M() { }
}

public class B : A
{
    [Obsolete]
    public override event Action E { add { } remove { } }
    [Obsolete]
    public override int P { get; set; }
    [Obsolete]
    public override void M() { }
}

public class C : B
{
    public override event Action E { add { } remove { } }
    public override int P { get; set; }
    public override void M() { }
}

class Test
{
    void M(A a, B b, C c)
    {
        a.E += null;
        a.P++;
        a.M();

        b.E += null;
        b.P++;
        b.M();

        c.E += null;
        c.P++;
        c.M();
    }
}
";
            // No member accesses produce obsolete warnings.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (16,25): warning CS0809: Obsolete member 'B.P' overrides non-obsolete member 'A.P'
                //     public override int P { get; set; }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "P").WithArguments("B.P", "A.P"),
                // (18,26): warning CS0809: Obsolete member 'B.M()' overrides non-obsolete member 'A.M()'
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "M").WithArguments("B.M()", "A.M()"),
                // (14,34): warning CS0809: Obsolete member 'B.E' overrides non-obsolete member 'A.E'
                //     public override event Action E { add { } remove { } }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "E").WithArguments("B.E", "A.E"));
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_GenericType()
        {
            var source = @"
using System;

public class A<T>
{
    [Obsolete]
    public virtual void M() { }
}

public class B : A<int>
{
    public override void M() { }
}

public class C<T> : A<T>
{
    public override void M() { }
}

class Test
{
    void M(A<int> a, B b, C<char> c)
    {
        a.M();
        b.M();
        c.M();
    }
}
";
            // All member accesses produce obsolete warnings.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (17,26): warning CS0672: Member 'C<T>.M()' overrides obsolete member 'A<T>.M()'. Add the Obsolete attribute to 'C<T>.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("C<T>.M()", "A<T>.M()"),
                // (12,26): warning CS0672: Member 'B.M()' overrides obsolete member 'A<int>.M()'. Add the Obsolete attribute to 'B.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("B.M()", "A<int>.M()"),
                // (24,9): warning CS0612: 'A<int>.M()' is obsolete
                //         a.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "a.M()").WithArguments("A<int>.M()"),
                // (25,9): warning CS0612: 'A<int>.M()' is obsolete
                //         b.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "b.M()").WithArguments("A<int>.M()"),
                // (26,9): warning CS0612: 'A<char>.M()' is obsolete
                //         c.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c.M()").WithArguments("A<char>.M()"));
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_GenericMethod()
        {
            var source = @"
using System;

public class A
{
    [Obsolete]
    public virtual void M<T>() { }
}

public class B : A
{
    public override void M<T>() { }
}

class Test
{
    void M(B b)
    {
        b.M<int>();
    }
}
";
            // All member accesses produce obsolete warnings.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,26): warning CS0672: Member 'B.M<T>()' overrides obsolete member 'A.M<T>()'. Add the Obsolete attribute to 'B.M<T>()'.
                //     public override void M<T>() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("B.M<T>()", "A.M<T>()"),
                // (19,9): warning CS0612: 'A.M<T>()' is obsolete
                //         b.M<int>();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "b.M<int>()").WithArguments("A.M<T>()"));
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_OnBase_BaseCall()
        {
            var source = @"
using System;

public class A
{
    [Obsolete]
    public virtual event Action E { add { } remove { } }
    [Obsolete]
    public virtual int P { get; set; }
    [Obsolete]
    public virtual void M() { }
}

public class B : A
{
    public override event Action E { add { } remove { } }
    public override int P { get; set; }
    public override void M() { }

    private void Test()
    {
        base.E += null;
        base.P++;
        base.M();
    }
}

public class C : B
{
    public override event Action E { add { } remove { } }
    public override int P { get; set; }
    public override void M() { }

    private void Test()
    {
        base.E += null;
        base.P++;
        base.M();
    }
}
";
            // Reported in B.Test and C.Test against members of A.
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (17,25): warning CS0672: Member 'B.P' overrides obsolete member 'A.P'. Add the Obsolete attribute to 'B.P'.
                //     public override int P { get; set; }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "P").WithArguments("B.P", "A.P"),
                // (18,26): warning CS0672: Member 'B.M()' overrides obsolete member 'A.M()'. Add the Obsolete attribute to 'B.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("B.M()", "A.M()"),
                // (16,34): warning CS0672: Member 'B.E' overrides obsolete member 'A.E'. Add the Obsolete attribute to 'B.E'.
                //     public override event Action E { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "E").WithArguments("B.E", "A.E"),
                // (31,25): warning CS0672: Member 'C.P' overrides obsolete member 'A.P'. Add the Obsolete attribute to 'C.P'.
                //     public override int P { get; set; }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "P").WithArguments("C.P", "A.P"),
                // (32,26): warning CS0672: Member 'C.M()' overrides obsolete member 'A.M()'. Add the Obsolete attribute to 'C.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("C.M()", "A.M()"),
                // (30,34): warning CS0672: Member 'C.E' overrides obsolete member 'A.E'. Add the Obsolete attribute to 'C.E'.
                //     public override event Action E { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "E").WithArguments("C.E", "A.E"),

                // (23,9): warning CS0612: 'A.P' is obsolete
                //         base.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.P").WithArguments("A.P"),
                // (24,9): warning CS0612: 'A.M()' is obsolete
                //         base.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.M()").WithArguments("A.M()"),
                // (22,9): warning CS0612: 'A.E' is obsolete
                //         base.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.E").WithArguments("A.E"),
                // (37,9): warning CS0612: 'A.P' is obsolete
                //         base.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.P").WithArguments("A.P"),
                // (38,9): warning CS0612: 'A.M()' is obsolete
                //         base.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.M()").WithArguments("A.M()"),
                // (36,9): warning CS0612: 'A.E' is obsolete
                //         base.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.E").WithArguments("A.E"));
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_OnBaseAndDerived_BaseCall()
        {
            var source = @"
using System;

public class A
{
    [Obsolete]
    public virtual event Action E { add { } remove { } }
    [Obsolete]
    public virtual int P { get; set; }
    [Obsolete]
    public virtual void M() { }
}

public class B : A
{
    [Obsolete]
    public override event Action E { add { } remove { } }
    [Obsolete]
    public override int P { get; set; }
    [Obsolete]
    public override void M() { }

    private void Test()
    {
        base.E += null;
        base.P++;
        base.M();
    }
}

public class C : B
{
    public override event Action E { add { } remove { } }
    public override int P { get; set; }
    public override void M() { }

    private void Test()
    {
        base.E += null;
        base.P++;
        base.M();
    }
}
";
            // Reported in B.Test and C.Test against members of A (seems like C.Test should report against members of B,
            // but this is dev11's behavior).
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (34,25): warning CS0672: Member 'C.P' overrides obsolete member 'A.P'. Add the Obsolete attribute to 'C.P'.
                //     public override int P { get; set; }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "P").WithArguments("C.P", "A.P"),
                // (35,26): warning CS0672: Member 'C.M()' overrides obsolete member 'A.M()'. Add the Obsolete attribute to 'C.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("C.M()", "A.M()"),
                // (33,34): warning CS0672: Member 'C.E' overrides obsolete member 'A.E'. Add the Obsolete attribute to 'C.E'.
                //     public override event Action E { add { } remove { } }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "E").WithArguments("C.E", "A.E"),

                // (26,9): warning CS0612: 'A.P' is obsolete
                //         base.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.P").WithArguments("A.P"),
                // (27,9): warning CS0612: 'A.M()' is obsolete
                //         base.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.M()").WithArguments("A.M()"),
                // (25,9): warning CS0612: 'A.E' is obsolete
                //         base.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.E").WithArguments("A.E"),
                // (40,9): warning CS0612: 'A.P' is obsolete
                //         base.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.P").WithArguments("A.P"),
                // (41,9): warning CS0612: 'A.M()' is obsolete
                //         base.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.M()").WithArguments("A.M()"),
                // (39,9): warning CS0612: 'A.E' is obsolete
                //         base.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.E").WithArguments("A.E"));
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_OnDerived_BaseCall()
        {
            var source = @"
using System;

public class A
{
    public virtual event Action E { add { } remove { } }
    public virtual int P { get; set; }
    public virtual void M() { }
}

public class B : A
{
    [Obsolete]
    public override event Action E { add { } remove { } }
    [Obsolete]
    public override int P { get; set; }
    [Obsolete]
    public override void M() { }

    private void Test()
    {
        base.E += null;
        base.P++;
        base.M();
    }
}

public class C : B
{
    public override event Action E { add { } remove { } }
    public override int P { get; set; }
    public override void M() { }

    private void Test()
    {
        base.E += null;
        base.P++;
        base.M();
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (16,25): warning CS0809: Obsolete member 'B.P' overrides non-obsolete member 'A.P'
                //     public override int P { get; set; }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "P").WithArguments("B.P", "A.P"),
                // (18,26): warning CS0809: Obsolete member 'B.M()' overrides non-obsolete member 'A.M()'
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "M").WithArguments("B.M()", "A.M()"),
                // (14,34): warning CS0809: Obsolete member 'B.E' overrides non-obsolete member 'A.E'
                //     public override event Action E { add { } remove { } }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "E").WithArguments("B.E", "A.E"),

                // (37,9): warning CS0612: 'B.P' is obsolete
                //         base.P++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.P").WithArguments("B.P"),
                // (38,9): warning CS0612: 'B.M()' is obsolete
                //         base.M();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.M()").WithArguments("B.M()"),
                // (36,9): warning CS0612: 'B.E' is obsolete
                //         base.E += null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base.E").WithArguments("B.E"));
        }

        [Fact]
        [WorkItem(580832, "DevDiv")]
        public void ObsoleteOnVirtual_OnDerived_BaseCall2()
        {
            var source = @"
using System;

public class A
{
    public virtual int this[int x] { get { return 0; } set { } }
}

public class B : A
{
    [Obsolete]
    public B() { }

    [Obsolete]
    public override int this[int x] { get { return 0; } set { } }
}

public class C : B
{
    public C() { } // Implicit base constructor invocation.
    public C(int x) : base() { } // Doesn't override anything anyway.

    private void Test()
    {
        base[1]++;
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (15,25): warning CS0809: Obsolete member 'B.this[int]' overrides non-obsolete member 'A.this[int]'
                //     public override int this[int x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "this").WithArguments("B.this[int]", "A.this[int]"),

                // (20,5): warning CS0612: 'B.B()' is obsolete
                //     public C() { } // Implicit base constructor invocation.
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "public C() { }").WithArguments("B.B()"),
                // (21,21): warning CS0612: 'B.B()' is obsolete
                //     public C(int x) : base() { } // Doesn't override anything anyway.
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, ": base()").WithArguments("B.B()"),

                // (25,9): warning CS0612: 'B.this[int]' is obsolete
                //         base[1]++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "base[1]").WithArguments("B.this[int]"));
        }

        [Fact]
        [WorkItem(531148, "DevDiv")]
        public void ObsoleteUserDefinedConversion1()
        {
            var source = @"
using System;

class A
{
}

class B
{
    [Obsolete(""B to A"")]
    public static explicit operator B(A a)
    {
        return null;
    }

    [Obsolete(""A to B"")]
    public static implicit operator A(B b)
    {
        return null;
    }
}


class Test
{
    static void Main()
    {
        A a = new A();
        B b = (B)a;
        a = b;
        a = (A)(B)(A)b;
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (29,15): warning CS0618: 'B.explicit operator B(A)' is obsolete: 'B to A'
                //         B b = (B)a;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "(B)a").WithArguments("B.explicit operator B(A)", "B to A"),
                // (30,13): warning CS0618: 'B.implicit operator A(B)' is obsolete: 'A to B'
                //         a = b;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "b").WithArguments("B.implicit operator A(B)", "A to B"),
                // (31,19): warning CS0618: 'B.implicit operator A(B)' is obsolete: 'A to B'
                //         a = (A)(B)(A)b;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "(A)b").WithArguments("B.implicit operator A(B)", "A to B"),
                // (31,16): warning CS0618: 'B.explicit operator B(A)' is obsolete: 'B to A'
                //         a = (A)(B)(A)b;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "(B)(A)b").WithArguments("B.explicit operator B(A)", "B to A"),
                // (31,13): warning CS0618: 'B.implicit operator A(B)' is obsolete: 'A to B'
                //         a = (A)(B)(A)b;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "(A)(B)(A)b").WithArguments("B.implicit operator A(B)", "A to B"));
        }

        [Fact]
        [WorkItem(531148, "DevDiv")]
        public void ObsoleteUserDefinedConversion2()
        {
            var source = @"
using System;

class A<T>
{
    [Obsolete(""A<T> to T"")]
    public static implicit operator T(A<T> a)
    {
        return default(T);
    }

    [Obsolete(""T to A<T>"")]
    public static implicit operator A<T>(T t)
    {
        return null;
    }
}


class Test
{
    static void Main()
    {
        A<int> ai = new A<int>();
        int i = ai;
        ai = i;

        // These casts don't use the UDCs (at compile time).
        A<dynamic> ad = new A<dynamic>();
        dynamic d = ad;
        ad = d;
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (25,17): warning CS0618: 'A<int>.implicit operator int(A<int>)' is obsolete: 'A<T> to T'
                //         int i = ai;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "ai").WithArguments("A<int>.implicit operator int(A<int>)", "A<T> to T"),
                // (26,14): warning CS0618: 'A<int>.implicit operator A<int>(int)' is obsolete: 'T to A<T>'
                //         ai = i;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "i").WithArguments("A<int>.implicit operator A<int>(int)", "T to A<T>"));
        }

        [Fact]
        [WorkItem(531148, "DevDiv")]
        public void ObsoleteUserDefinedConversion3()
        {
            var source = @"
using System;

class Convertible
{
    [Obsolete(""To int"")]
    public static implicit operator int(Convertible c)
    {
        return 0;
    }

    [Obsolete(""To bool"")]
    public static implicit operator bool(Convertible c)
    {
        return false;
    }
}


class Test
{
    static void Main()
    {
        Convertible c = new Convertible();
        if (c)
        {
            switch (c)
            {
                case 0:
                    int x = c + 1;
                    x = +c;
                    break;
            }
        }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (25,13): warning CS0618: 'Convertible.implicit operator bool(Convertible)' is obsolete: 'To bool'
                //         if (c)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c").WithArguments("Convertible.implicit operator bool(Convertible)", "To bool"),
                // (27,21): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //             switch (c)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c").WithArguments("Convertible.implicit operator int(Convertible)", "To int"),
                // (30,29): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //                     int x = c + 1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c").WithArguments("Convertible.implicit operator int(Convertible)", "To int"),
                // (31,26): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //                     x = +c;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c").WithArguments("Convertible.implicit operator int(Convertible)", "To int"));
        }

        [Fact]
        [WorkItem(531148, "DevDiv")]
        public void ObsoleteUserDefinedConversion4()
        {
            var source = @"
using System;

class Convertible
{
    [Obsolete(""To int"")]
    public static implicit operator int(Convertible c)
    {
        return 0;
    }
}


class Test
{
    static void Main(string[] args)
    {
        Convertible c = new Convertible();
        args[c].ToString();
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (19,14): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //         args[c].ToString();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c").WithArguments("Convertible.implicit operator int(Convertible)", "To int"));
        }

        [Fact]
        [WorkItem(531148, "DevDiv")]
        public void ObsoleteUserDefinedConversion5()
        {
            var source = @"
using System;

class Convertible
{
    [Obsolete(""To int"")]
    public static implicit operator int(Convertible c)
    {
        return 0;
    }

    [Obsolete(""From int"")]
    public static implicit operator Convertible(int i)
    {
        return null;
    }
}

class Test
{
    static void Main(string[] args)
    {
        foreach (int i in new Convertible[1])
        {
            Convertible c = new Convertible();
            c++;
            c -= 2;
        }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (26,13): warning CS0618: 'Convertible.implicit operator Convertible(int)' is obsolete: 'From int'
                //             c++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c++").WithArguments("Convertible.implicit operator Convertible(int)", "From int"),
                // (26,13): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //             c++;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c++").WithArguments("Convertible.implicit operator int(Convertible)", "To int"),
                // (27,13): warning CS0618: 'Convertible.implicit operator Convertible(int)' is obsolete: 'From int'
                //             c -= 2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c -= 2").WithArguments("Convertible.implicit operator Convertible(int)", "From int"),
                // (27,13): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //             c -= 2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c -= 2").WithArguments("Convertible.implicit operator int(Convertible)", "To int"),
                // (23,9): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //         foreach (int i in new Convertible[1])
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "foreach").WithArguments("Convertible.implicit operator int(Convertible)", "To int"));
        }

        [Fact]
        [WorkItem(531148, "DevDiv")]
        public void ObsoleteUserDefinedConversion6()
        {
            var source = @"
using System;

struct Convertible
{
    [Obsolete(""To int"")]
    public static implicit operator int(Convertible c)
    {
        return 0;
    }
}

class Test
{
    static void Main(string[] args)
    {
        Convertible? c = null;
        int i = c ?? 1;
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (18,17): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //         int i = c ?? 1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c ?? 1").WithArguments("Convertible.implicit operator int(Convertible)", "To int"));
        }

        [Fact]
        [WorkItem(656345, "DevDiv")]
        public void ConditionalLazyObsoleteDiagnostic()
        {
            var source = @"
public class A
{
    protected virtual void M() { }
}

public class B : A
{
    protected override void M() { }
}

public class C : B
{
    void Test()
    {
        base.M();
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            // Used to assert because it depended on some lazy state being evaluated but didn't
            // actually trigger evaluation.
            model.GetSymbolInfo(syntax);

            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(656345, "DevDiv")]
        public void ConditionalLazyObsoleteDiagnosticInAttribute()
        {
            var source = @"
using System;

public class A
{
    protected virtual int M()
    {
        return 1;
    }
}

public class B : A
{
    protected override int M()
    {
        return 2;
    }
}

public class C : B
{
    [Num(base.M())]
    void Test()
    {
    }
}

public class NumAttribute : Attribute
{
    public NumAttribute(int x) { }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            // Used to assert because it depended on some lazy state being evaluated but didn't
            // actually trigger evaluation.
            model.GetSymbolInfo(syntax);

            comp.VerifyDiagnostics(
                // (22,10): error CS1512: Keyword 'base' is not available in the current context
                //     [Num(base.M())]
                Diagnostic(ErrorCode.ERR_BaseInBadContext, "base"));
        }

        [Fact]
        [WorkItem(665595, "DevDiv")]
        public void ConditionalLazyObsoleteDiagnosticInLazyObsoleteContext()
        {
            var source1 = @"
namespace System.Web.UI.Design
{
    public class ReadWriteControlDesignerBase
    {
        [Obsolete(""A"")]
        protected virtual void OnBehaviorAttached()
        {
        }
    }

    public class ReadWriteControlDesigner : ReadWriteControlDesignerBase
    {
        [Obsolete(""B"")]
        protected override void OnBehaviorAttached()
        {
        }
    }
}
";

            var source2 = @"
using System.Web.UI.Design;
 
class C : ReadWriteControlDesigner
{
    protected override void OnBehaviorAttached()
    {
        base.OnBehaviorAttached();
    }
}
";
            var comp1 = CreateCompilationWithMscorlib(source1);
            comp1.VerifyDiagnostics();

            var comp2 = CreateCompilationWithMscorlib(source2, new[] { comp1.EmitToImageReference() });

            var tree = comp2.SyntaxTrees.Single();
            var model = comp2.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Last(n => n.Identifier.ValueText == "OnBehaviorAttached");

            // Used to assert because it depended on some lazy state being evaluated but didn't
            // actually trigger evaluation.
            model.GetSymbolInfo(syntax);

            comp2.VerifyDiagnostics(
                // (6,29): warning CS0672: Member 'C.OnBehaviorAttached()' overrides obsolete member 'System.Web.UI.Design.ReadWriteControlDesignerBase.OnBehaviorAttached()'. Add the Obsolete attribute to 'C.OnBehaviorAttached()'.
                //     protected override void OnBehaviorAttached()
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "OnBehaviorAttached").WithArguments("C.OnBehaviorAttached()", "System.Web.UI.Design.ReadWriteControlDesignerBase.OnBehaviorAttached()"),
                // (8,9): warning CS0618: 'System.Web.UI.Design.ReadWriteControlDesignerBase.OnBehaviorAttached()' is obsolete: 'A'
                //         base.OnBehaviorAttached();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "base.OnBehaviorAttached()").WithArguments("System.Web.UI.Design.ReadWriteControlDesignerBase.OnBehaviorAttached()", "A"));
        }

        [Fact]
        [WorkItem(668365, "DevDiv")]
        public void ObsoleteOverrideChain()
        {
            var source = @"
using System;

public class A
{
    [Obsolete]
    public virtual void M() { }
}

public class B : A
{
    // Not obsolete
    public override void M() { }
}

public class C : B
{
    [Obsolete]
    public override void M() { }
}

public class D
{
    // Not obsolete
    public virtual void M() { }
}

public class E : D
{
    [Obsolete]
    public override void M() { }
}

public class F : E
{
    // Not obsolete
    public override void M() { }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (13,26): warning CS0672: Member 'B.M()' overrides obsolete member 'A.M()'. Add the Obsolete attribute to 'B.M()'.
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("B.M()", "A.M()"),
                // (31,26): warning CS0809: Obsolete member 'E.M()' overrides non-obsolete member 'D.M()'
                //     public override void M() { }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "M").WithArguments("E.M()", "D.M()"));
        }

        [Fact]
        public void DefaultValueOnParamsParameter()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class C
{
    public void M([Optional, DefaultParameterValue(null)]params int[] args)
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemRef });

            Action<ModuleSymbol> validator = module =>
            {
                var method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
                var param = method.Parameters.Single();

                Assert.True(param.IsParams);
                Assert.False(param.IsOptional);
                Assert.False(param.HasExplicitDefaultValue);
            };

            CompileAndVerify(comp, symbolValidator: validator, sourceSymbolValidator: validator); // NOTE: Illegal in dev11, but legal in roslyn.
        }

        [Fact]
        public void TestDeprecatedAttribute()
        {
            var source1 = @"
using Windows.Foundation.Metadata;

[Deprecated(""Class1 is deprecated."", DeprecationType.Deprecate, 0)]
public class Class1
{
}

[Deprecated(""Class2 is deprecated."", DeprecationType.Deprecate, 0, Platform.Windows)]
public class Class2
{
}

[Deprecated(""Class3 is deprecated."", DeprecationType.Remove, 1)]
public class Class3
{
}

[Deprecated(""Class4 is deprecated."", DeprecationType.Remove, 0, Platform.WindowsPhone)]
public class Class4
{
}
";
            var compilation1 = CreateCompilation(source1, WinRtRefs, TestOptions.ReleaseDll);

            compilation1.VerifyDiagnostics();

            var source2 = @"
using Windows.Foundation.Metadata;

class Class5
{
    void Test()
    {
        Class1 x1 = null;
        Class2 x2 = null;
        Class3 x3 = null;
        Class4 x4 = null;

        object x5;
        x5=x1;
        x5 = x2;
        x5 = x3;
        x5 = x4;
    }
}

class Class6
{
    int P1
    {
        [Deprecated(""P1.get is deprecated."", DeprecationType.Remove, 1)]
        get
        {
            return 1;
        }
    }

    event System.Action E1
    {
        [Deprecated(""E1.add is deprecated."", DeprecationType.Remove, 1)]
        add
        {
        }
        remove
        {
        }
    }
}
";
            var compilation2 = CreateCompilation(source2, WinRtRefs.Concat(new[] { new CSharpCompilationReference(compilation1) }), TestOptions.ReleaseDll);

            var expected = new[] {
                // (25,10): error CS1667: Attribute 'Windows.Foundation.Metadata.DeprecatedAttribute' is not valid on property or event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Deprecated("P1.get is deprecated.", DeprecationType.Remove, 1)]
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "Deprecated").WithArguments("Windows.Foundation.Metadata.DeprecatedAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(25, 10),
                // (34,10): error CS1667: Attribute 'Windows.Foundation.Metadata.DeprecatedAttribute' is not valid on property or event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Deprecated("E1.add is deprecated.", DeprecationType.Remove, 1)]
                Diagnostic(ErrorCode.ERR_AttributeNotOnAccessor, "Deprecated").WithArguments("Windows.Foundation.Metadata.DeprecatedAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(34, 10),
                // (8,9): warning CS0618: 'Class1' is obsolete: 'Class1 is deprecated.'
                //         Class1 x1 = null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Class1").WithArguments("Class1", "Class1 is deprecated.").WithLocation(8, 9),
                // (9,9): warning CS0618: 'Class2' is obsolete: 'Class2 is deprecated.'
                //         Class2 x2 = null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Class2").WithArguments("Class2", "Class2 is deprecated.").WithLocation(9, 9),
                // (10,9): error CS0619: 'Class3' is obsolete: 'Class3 is deprecated.'
                //         Class3 x3 = null;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class3").WithArguments("Class3", "Class3 is deprecated.").WithLocation(10, 9),
                // (11,9): error CS0619: 'Class4' is obsolete: 'Class4 is deprecated.'
                //         Class4 x4 = null;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class4").WithArguments("Class4", "Class4 is deprecated.").WithLocation(11, 9)
                                 };

            compilation2.VerifyDiagnostics(expected);

            compilation2 = CreateCompilation(source2, WinRtRefs.Concat(new[] { compilation1.EmitToImageReference() }), TestOptions.ReleaseDll);
            compilation2.VerifyDiagnostics(expected);
        }

        [Fact]
        public void TestDeprecatedAttribute1()
        {
            var source1 = @"
using System;
using Windows.Foundation.Metadata;

namespace Windows.Foundation.Metadata
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }

        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version, Type contract)
        {
        }
    }

    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}

public class Test
{
        [Deprecated(""hello"", DeprecationType.Deprecate, 1, typeof(int))]
        public static void Foo()
        {

        }

        [Deprecated(""hi"", DeprecationType.Deprecate, 1)]
        public static void Bar()
        {

        }
}
";
            var compilation1 = CreateCompilationWithMscorlibAndSystemCore(source1);

            var source2 = @"
namespace ConsoleApplication74
{
    class Program
    {
        static void Main(string[] args)
        {
            Test.Foo();
            Test.Bar();
        }
    }
}


";
            var compilation2 = CreateCompilationWithMscorlibAndSystemCore(source2, new[] { compilation1.EmitToImageReference() });


            compilation2.VerifyDiagnostics(
    // (8,13): warning CS0618: 'Test.Foo()' is obsolete: 'hello'
    //             Test.Foo();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Foo()").WithArguments("Test.Foo()", "hello").WithLocation(8, 13),
    // (9,13): warning CS0618: 'Test.Bar()' is obsolete: 'hi'
    //             Test.Bar();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Bar()").WithArguments("Test.Bar()", "hi").WithLocation(9, 13)
);

            var compilation3 = CreateCompilationWithMscorlibAndSystemCore(source2, new[] { new CSharpCompilationReference(compilation1) });


            compilation3.VerifyDiagnostics(
    // (8,13): warning CS0618: 'Test.Foo()' is obsolete: 'hello'
    //             Test.Foo();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Foo()").WithArguments("Test.Foo()", "hello").WithLocation(8, 13),
    // (9,13): warning CS0618: 'Test.Bar()' is obsolete: 'hi'
    //             Test.Bar();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Bar()").WithArguments("Test.Bar()", "hi").WithLocation(9, 13)
);
        }


        [Fact, WorkItem(858839, "DevDiv")]
        public void Bug858839_1()
        {
            var source1 = @"
using Windows.Foundation.Metadata;

public class MainPage
{
    public static void Main(string[] args)
    {
    }
    
    private static void TestFoo1(IFoo1 a, ConcreteFoo1 b)
    {
        a.Foo(); // IFoo1
        b.Foo(); // ConcreteFoo1
    }

    private static void TestFoo2(IFoo2 a, ConcreteFoo2 b)
    {
        a.Foo(); // IFoo2
        b.Foo(); // ConcreteFoo2
    }

    private static void TestFoo3(IFoo3 a, ConcreteFoo3 b)
    {
        a.Foo(); // IFoo3
        b.Foo(); // ConcreteFoo3
    }
}

public interface IFoo1
{
    [Deprecated(""IFoo1.Foo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    void Foo();
}

public sealed class ConcreteFoo1 : IFoo1
{
    public void Foo()
    {
    }
}

public interface IFoo2
{
    void Foo();
}

public sealed class ConcreteFoo2 : IFoo2
{
    [Deprecated(""ConcreteFoo2.Foo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    public void Foo()
    {
    }
}

public interface IFoo3
{
    [Deprecated(""IFoo3.Foo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    void Foo();
}

public sealed class ConcreteFoo3 : IFoo3
{
    [Deprecated(""ConcreteFoo3.Foo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    public void Foo()
    {
    }
}

public sealed class ConcreteFoo4 : IFoo1
{
    void IFoo1.Foo()
    {
    }
}

public sealed class ConcreteFoo5 : IFoo1
{
    [Deprecated(""ConcreteFoo5.Foo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    void IFoo1.Foo()
    {
    }
}
";
            var compilation1 = CreateCompilation(source1, WinRtRefs, TestOptions.ReleaseDll);

            var expected = new[] {
                // (12,9): warning CS0618: 'IFoo1.Foo()' is obsolete: 'IFoo1.Foo has been deprecated'
                //         a.Foo(); // IFoo1
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.Foo()").WithArguments("IFoo1.Foo()", "IFoo1.Foo has been deprecated").WithLocation(12, 9),
                // (19,9): warning CS0618: 'ConcreteFoo2.Foo()' is obsolete: 'ConcreteFoo2.Foo has been deprecated'
                //         b.Foo(); // ConcreteFoo2
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "b.Foo()").WithArguments("ConcreteFoo2.Foo()", "ConcreteFoo2.Foo has been deprecated").WithLocation(19, 9),
                // (24,9): warning CS0618: 'IFoo3.Foo()' is obsolete: 'IFoo3.Foo has been deprecated'
                //         a.Foo(); // IFoo3
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.Foo()").WithArguments("IFoo3.Foo()", "IFoo3.Foo has been deprecated").WithLocation(24, 9),
                // (25,9): warning CS0618: 'ConcreteFoo3.Foo()' is obsolete: 'ConcreteFoo3.Foo has been deprecated'
                //         b.Foo(); // ConcreteFoo3
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "b.Foo()").WithArguments("ConcreteFoo3.Foo()", "ConcreteFoo3.Foo has been deprecated").WithLocation(25, 9)
                                 };

            compilation1.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(858839, "DevDiv")]
        public void Bug858839_2()
        {
            var source1 = @"
using Windows.Foundation.Metadata;

public interface IExceptionalInterface
{
    string ExceptionalProp
    {
        [Deprecated(""Actually, don't even use the prop at all."", DeprecationType.Remove, 50331648u)]
        get;
        [Deprecated(""Changed my mind; don't put this prop."", DeprecationType.Remove, 33554432u)]
        set;
    }
}
";
            var compilation1 = CreateCompilation(source1, WinRtRefs, TestOptions.ReleaseDll);

            //compilation1.VerifyDiagnostics();

            var source2 = @"
using System;

class Test
{
    public static void F(IExceptionalInterface i)
    {
        i.ExceptionalProp = ""foo"";
        Console.WriteLine(i.ExceptionalProp);
        }
    }
";
            var compilation2 = CreateCompilation(source2, WinRtRefs.Concat(new[] { new CSharpCompilationReference(compilation1) }), TestOptions.ReleaseDll);

            var expected = new[] {
                // (8,9): error CS0619: 'IExceptionalInterface.ExceptionalProp.set' is obsolete: 'Changed my mind; don't put this prop.'
                //         i.ExceptionalProp = "foo";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "i.ExceptionalProp").WithArguments("IExceptionalInterface.ExceptionalProp.set", "Changed my mind; don't put this prop.").WithLocation(8, 9),
                // (9,27): error CS0619: 'IExceptionalInterface.ExceptionalProp.get' is obsolete: 'Actually, don't even use the prop at all.'
                //         Console.WriteLine(i.ExceptionalProp);
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "i.ExceptionalProp").WithArguments("IExceptionalInterface.ExceptionalProp.get", "Actually, don't even use the prop at all.").WithLocation(9, 27)
                                 };

            compilation2.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(530801, "DevDiv")]
        public void Bug530801DisallowRequiredAttributeCS0648()
        {
            var ilsource = @"
.class public auto ansi beforefieldinit Scenario1
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 59 53 79 73 74 65 6D 2E 49 6E 74 33 32 2C   // ..YSystem.Int32,
                                                                                                                                     20 6D 73 63 6F 72 6C 69 62 2C 20 56 65 72 73 69   //  mscorlib, Versi
                                                                                                                                     6F 6E 3D 34 2E 30 2E 30 2E 30 2C 20 43 75 6C 74   // on=4.0.0.0, Cult
                                                                                                                                     75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62   // ure=neutral, Pub
                                                                                                                                     6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 62 37 37 61   // licKeyToken=b77a
                                                                                                                                     35 63 35 36 31 39 33 34 65 30 38 39 00 00 )       // 5c561934e089..
  .field public int32 intVar
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Scenario1::.ctor

} // end of class Scenario1
";

            var cssource = @"
public class C
{
    static Scenario1 ss;
    public static int Main()
    {
        ss = new Scenario1();
        DoSomething(ss);
        return 1;
    }

    static void DoSomething(Scenario1 p)
    {
        System.Console.WriteLine(p);
    }
}
";

            var ilReference = CompileIL(ilsource);
            var cscomp = CreateCompilation(cssource, new[] { MscorlibRef, ilReference }, TestOptions.ReleaseExe);

            var expected = new[] {
                // (12,29): error CS0648: 'Scenario1' is a type not supported by the language
                //     static void DoSomething(Scenario1 p)
                Diagnostic(ErrorCode.ERR_BogusType, "Scenario1").WithArguments("Scenario1").WithLocation(12, 29),
                // (4,12): error CS0648: 'Scenario1' is a type not supported by the language
                //     static Scenario1 ss;
                Diagnostic(ErrorCode.ERR_BogusType, "Scenario1").WithArguments("Scenario1").WithLocation(4, 12),
                // (7,18): error CS0648: 'Scenario1' is a type not supported by the language
                //         ss = new Scenario1();
                Diagnostic(ErrorCode.ERR_BogusType, "Scenario1").WithArguments("Scenario1").WithLocation(7, 18)                                 };

            cscomp.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(530801, "DevDiv")]
        public void Bug530801DisallowRequiredAttributeCS0570()
        {
            var ilsource = @"
.class public auto ansi beforefieldinit RequiredAttr.Scenario1
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 59 53 79 73 74 65 6D 2E 49 6E 74 33 32 2C   // ..YSystem.Int32,
                                                                                                                                     20 6D 73 63 6F 72 6C 69 62 2C 20 56 65 72 73 69   //  mscorlib, Versi
                                                                                                                                     6F 6E 3D 34 2E 30 2E 30 2E 30 2C 20 43 75 6C 74   // on=4.0.0.0, Cult
                                                                                                                                     75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62   // ure=neutral, Pub
                                                                                                                                     6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 62 37 37 61   // licKeyToken=b77a
                                                                                                                                     35 63 35 36 31 39 33 34 65 30 38 39 00 00 )       // 5c561934e089..
  .field public int32 intVar
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Scenario1::.ctor

} // end of class RequiredAttr.Scenario1

.class public auto ansi beforefieldinit RequiredAttr.ReqAttrUsage
       extends [mscorlib]System.Object
{
  .field public class RequiredAttr.Scenario1 sc1_field
  .method public hidebysig newslot specialname virtual 
          instance class RequiredAttr.Scenario1 
          get_sc1_prop() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  1
    .locals (class RequiredAttr.Scenario1 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class RequiredAttr.Scenario1 RequiredAttr.ReqAttrUsage::sc1_field
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ret
  } // end of method ReqAttrUsage::get_sc1_prop

  .method public hidebysig instance class RequiredAttr.Scenario1 
          sc1_method() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  1
    .locals (class RequiredAttr.Scenario1 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class RequiredAttr.Scenario1 RequiredAttr.ReqAttrUsage::sc1_field
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ret
  } // end of method ReqAttrUsage::sc1_method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ReqAttrUsage::.ctor

  .property instance class RequiredAttr.Scenario1
          sc1_prop()
  {
    .get instance class RequiredAttr.Scenario1 RequiredAttr.ReqAttrUsage::get_sc1_prop()
  } // end of property ReqAttrUsage::sc1_prop
} // end of class RequiredAttr.ReqAttrUsage
";

            var cssource = @"
using RequiredAttr;

public class C
{
    public static int Main()
    {
        var r = new ReqAttrUsage();
        r.sc1_field = null;
        var o = r.sc1_prop;
        r.sc1_method();
        return 1;
    }
}
";

            var ilReference = CompileIL(ilsource);
            var cscomp = CreateCompilation(cssource, new[] { MscorlibRef, ilReference }, TestOptions.ReleaseExe);

            var expected = new[] {
                // (9,11): error CS0570: 'RequiredAttr.ReqAttrUsage.sc1_field' is not supported by the language
                //         r.sc1_field = null;
                Diagnostic(ErrorCode.ERR_BindToBogus, "sc1_field").WithArguments("RequiredAttr.ReqAttrUsage.sc1_field").WithLocation(9, 11),
                // (10,19): error CS0570: 'RequiredAttr.ReqAttrUsage.sc1_prop' is not supported by the language
                //         var o = r.sc1_prop;
                Diagnostic(ErrorCode.ERR_BindToBogus, "sc1_prop").WithArguments("RequiredAttr.ReqAttrUsage.sc1_prop").WithLocation(10, 19),
                // (11,11): error CS0570: 'RequiredAttr.ReqAttrUsage.sc1_method()' is not supported by the language
                //         r.sc1_method();
                Diagnostic(ErrorCode.ERR_BindToBogus, "sc1_method").WithArguments("RequiredAttr.ReqAttrUsage.sc1_method()").WithLocation(11, 11)                                 };

            cscomp.VerifyDiagnostics(expected);
        }

        #endregion

        [Fact, WorkItem(807, "https://github.com/dotnet/roslyn/issues/807")]
        public void TestAttributePropagationForAsyncAndIterators_01()
        {
            var source = CreateCompilationWithMscorlib45(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
    }

    [MyAttribute]
    [System.Diagnostics.DebuggerNonUserCodeAttribute]
    [System.Diagnostics.DebuggerHiddenAttribute]
    [System.Diagnostics.DebuggerStepperBoundaryAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    public async Task<int> test1()
    {
        return await DoNothing();
    }

    public async Task<int> test2()
    {
        return await DoNothing();
    }

    async Task<int> DoNothing()
    {
        return 1;
    }

    [MyAttribute]
    [System.Diagnostics.DebuggerNonUserCodeAttribute]
    [System.Diagnostics.DebuggerHiddenAttribute]
    [System.Diagnostics.DebuggerStepperBoundaryAttribute]
    [System.Diagnostics.DebuggerStepThroughAttribute]
    public IEnumerable<int> Test3()
    {
        yield return 1;
        yield return 2;
    }

    public IEnumerable<int> Test4()
    {
        yield return 1;
        yield return 2;
    }
}

class MyAttribute : System.Attribute
{ }
");

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var program = m.GlobalNamespace.GetTypeMember("Program");

                Assert.Equal("", CheckAttributePropagation(((NamedTypeSymbol)program.GetMember<MethodSymbol>("test1").
                                                                             GetAttribute("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value).
                                                                             GetMember<MethodSymbol>("MoveNext")));

                Assert.Equal(0, ((NamedTypeSymbol)program.GetMember<MethodSymbol>("test2").
                                                                             GetAttribute("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value).
                                                                             GetMember<MethodSymbol>("MoveNext").GetAttributes().Length);

                Assert.Equal("", CheckAttributePropagation(((NamedTypeSymbol)program.GetMember<MethodSymbol>("Test3").
                                                                             GetAttribute("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value).
                                                                             GetMember<MethodSymbol>("MoveNext")));

                Assert.Equal(0, ((NamedTypeSymbol)program.GetMember<MethodSymbol>("Test4").
                                                                             GetAttribute("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value).
                                                                             GetMember<MethodSymbol>("MoveNext").GetAttributes().Length);
            };

            CompileAndVerify(source, symbolValidator: attributeValidator);
        }

        private static string CheckAttributePropagation(Symbol symbol)
        {
            string result = "";

            if (symbol.GetAttributes("", "MyAttribute").Any())
            {
                result += "MyAttribute is present\n";
            }

            if (!symbol.GetAttributes("System.Diagnostics", "DebuggerNonUserCodeAttribute").Any())
            {
                result += "DebuggerNonUserCodeAttribute is missing\n";
            }

            if (!symbol.GetAttributes("System.Diagnostics", "DebuggerHiddenAttribute").Any())
            {
                result += "DebuggerHiddenAttribute is missing\n";
            }

            if (!symbol.GetAttributes("System.Diagnostics", "DebuggerStepperBoundaryAttribute").Any())
            {
                result += "DebuggerStepperBoundaryAttribute is missing\n";
            }

            if (!symbol.GetAttributes("System.Diagnostics", "DebuggerStepThroughAttribute").Any())
            {
                result += "DebuggerStepThroughAttribute is missing\n";
            }

            return result;
        }

        [Fact, WorkItem(4521, "https://github.com/dotnet/roslyn/issues/4521")]
        public void TestAttributePropagationForAsyncAndIterators_02()
        {
            var source = CreateCompilationWithMscorlib45(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[MyAttribute]
[System.Diagnostics.DebuggerNonUserCodeAttribute]
[System.Diagnostics.DebuggerStepThroughAttribute]
class Program1
{
    static void Main(string[] args)
    {
    }

    public async Task<int> test1()
    {
        return await DoNothing();
    }

    async Task<int> DoNothing()
    {
        return 1;
    }

    public IEnumerable<int> Test3()
    {
        yield return 1;
        yield return 2;
    }
}

class Program2
{
    static void Main(string[] args)
    {
    }

    public async Task<int> test2()
    {
        return await DoNothing();
    }

    async Task<int> DoNothing()
    {
        return 1;
    }

    public IEnumerable<int> Test4()
    {
        yield return 1;
        yield return 2;
    }
}

class MyAttribute : System.Attribute
{ }
");

            Action<ModuleSymbol> attributeValidator = (ModuleSymbol m) =>
            {
                var program1 = m.GlobalNamespace.GetTypeMember("Program1");
                var program2 = m.GlobalNamespace.GetTypeMember("Program2");

                Assert.Equal("DebuggerHiddenAttribute is missing\nDebuggerStepperBoundaryAttribute is missing\n", 
                                                   CheckAttributePropagation(((NamedTypeSymbol)program1.GetMember<MethodSymbol>("test1").
                                                                             GetAttribute("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value)));

                Assert.Equal("DebuggerNonUserCodeAttribute is missing\nDebuggerHiddenAttribute is missing\nDebuggerStepperBoundaryAttribute is missing\nDebuggerStepThroughAttribute is missing\n",
                                                   CheckAttributePropagation(((NamedTypeSymbol)program2.GetMember<MethodSymbol>("test2").
                                                                             GetAttribute("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value)));

                Assert.Equal("DebuggerHiddenAttribute is missing\nDebuggerStepperBoundaryAttribute is missing\n", 
                                                   CheckAttributePropagation(((NamedTypeSymbol)program1.GetMember<MethodSymbol>("Test3").
                                                                             GetAttribute("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value)));

                Assert.Equal("DebuggerNonUserCodeAttribute is missing\nDebuggerHiddenAttribute is missing\nDebuggerStepperBoundaryAttribute is missing\nDebuggerStepThroughAttribute is missing\n",
                                                   CheckAttributePropagation(((NamedTypeSymbol)program2.GetMember<MethodSymbol>("Test4").
                                                                             GetAttribute("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").
                                                                             ConstructorArguments.Single().Value)));
            };

            CompileAndVerify(source, symbolValidator: attributeValidator);
        }
    }
}
