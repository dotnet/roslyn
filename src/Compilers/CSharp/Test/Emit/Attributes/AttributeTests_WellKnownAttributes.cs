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
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class WellKnownAttributeTestsUtil
    {
        public static bool? HasLocalsInit(this CompilationVerifier verifier, string methodName, bool realIL = false)
        {
            var il = verifier.VisualizeIL(methodName, realIL);

            if (il.Contains(".locals init ("))
            {
                return true;
            }
            if (il.Contains(".locals ("))
            {
                return false;
            }
            return null;
        }
    }

    public class AttributeTests_WellKnownAttributes : WellKnownAttributesTestBase
    {
        #region Misc

        [Fact]
        public void TestInteropAttributes01()
        {
            var source = CreateCompilationWithMscorlib40(@"
using System;
using System.Runtime.InteropServices;

[assembly: ComCompatibleVersion(1, 2, 3, 4)]
[ComImport(), Guid(""ABCDEF5D-2448-447A-B786-64682CBEF123"")]
[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[TypeLibImportClass(typeof(object)), TypeLibType(TypeLibTypeFlags.FAggregatable)]
[BestFitMapping(false, ThrowOnUnmappableChar = true)]
public interface IGoo
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

                // IGoo
                var igoo = m.GlobalNamespace.GetTypeMember("IGoo");
                Assert.Equal(6, igoo.GetAttributes().Length);

                // get attr by NamedTypeSymbol
                attrSym = igoo.GetAttribute(ciSym);
                Assert.Equal("ComImportAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);
                Assert.Equal(0, attrSym.CommonNamedArguments.Length);

                attrSym = igoo.GetAttribute(guidSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, "ABCDEF5D-2448-447A-B786-64682CBEF123");
                // get attr by ctor
                attrSym = igoo.GetAttribute(itCtor);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)ComInterfaceType.InterfaceIsIUnknown);

                attrSym = igoo.GetAttribute(tLibSym);
                attrSym.VerifyValue(0, TypedConstantKind.Type, typeof(object));

                attrSym = igoo.GetAttribute(tLTypeSym);
                attrSym.VerifyValue(0, TypedConstantKind.Enum, (int)TypeLibTypeFlags.FAggregatable);

                attrSym = igoo.GetAttribute(bfmSym);
                attrSym.VerifyValue(0, TypedConstantKind.Primitive, false);
                attrSym.VerifyNamedArgumentValue(0, "ThrowOnUnmappableChar", TypedConstantKind.Primitive, true);

                // =============================
                var mem = (MethodSymbol)igoo.GetMembers("DoSomething").First();
                Assert.Equal(1, mem.GetAttributes().Length);
                attrSym = mem.GetAttributes().First();
                Assert.Equal("AllowReversePInvokeCallsAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                mem = (MethodSymbol)igoo.GetMembers("Register").First();
                attrSym = mem.GetAttributes().First();
                Assert.Equal("ComRegisterFunctionAttribute", attrSym.AttributeClass.Name);
                Assert.Equal(0, attrSym.CommonConstructorArguments.Length);

                mem = (MethodSymbol)igoo.GetMembers("UnRegister").First();
                Assert.Equal(1, mem.GetAttributes().Length);

                mem = (MethodSymbol)igoo.GetMembers("LibFunc").First();
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
            var source = CreateCompilationWithMscorlib40(@"
using System;
using System.Runtime.InteropServices;

[assembly: PrimaryInteropAssembly(1, 2)]

[assembly: Guid(""1234C65D-1234-447A-B786-64682CBEF136"")]
[ComVisibleAttribute(false)]
[UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true, ThrowOnUnmappableChar = true)]
public delegate void DGoo(char p1, sbyte p2);

[ComDefaultInterface(typeof(object)), ProgId(""ProgId"")]
public class CGoo
{
    [DispIdAttribute(123)]
    [LCIDConversion(1), ComConversionLoss()]
    public void Method(sbyte p1, string p2)
    {
    }
}

[ComVisible(true), TypeIdentifier(""1234C65D-1234-447A-B786-64682CBEF136"", ""EGoo, InteropAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"")]
public enum EGoo
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
                // delegate DGoo
                var type1 = globalNS.GetTypeMember("DGoo");
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

                // class CGoo
                var type2 = globalNS.GetTypeMember("CGoo");
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

                //' enum EGoo
                var sourceAssembly = assembly as SourceAssemblySymbol;
                if (sourceAssembly != null)
                {
                    // Because this is a nopia local type it is only visible from the source assembly.
                    var type3 = globalNS.GetTypeMember("EGoo");
                    Assert.Equal(2, type3.GetAttributes().Length);

                    attrSym = type3.GetAttribute(comvSym);
                    attrSym.VerifyValue(0, TypedConstantKind.Primitive, true);

                    attrSym = type3.GetAttribute(tidSym);
                    attrSym.VerifyValue(1, TypedConstantKind.Primitive, "EGoo, InteropAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

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

        [ConditionalFact(typeof(DesktopOnly))]
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
            CompileAndVerifyWithMscorlib46(text, references: new[] { TestBase.SystemRef_v46 }, sourceSymbolValidator: attributeValidator);
        }

        [Fact]
        [WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")]
        public void DateTimeConstantAttribute()
        {
            #region "Source"
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class Bar
{
    public void Method([DateTimeConstant(-1)]DateTime p1) { }
}
";
            #endregion

            // The native C# compiler emits this:
            // .param[1]
            // .custom instance void[mscorlib] System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = (
            //         01 00 ff ff ff ff ff ff ff ff 00 00
            // )
            Action<IModuleSymbol> verifier = (module) =>
                {
                    var bar = (NamedTypeSymbol)((ModuleSymbol)module).GlobalNamespace.GetMember("Bar");
                    var method = (MethodSymbol)bar.GetMember("Method");
                    var parameters = method.GetParameters();
                    var theParameter = (PEParameterSymbol)parameters[0];
                    var peModule = (PEModuleSymbol)module;

                    Assert.Equal(ParameterAttributes.HasDefault, theParameter.Flags); // native compiler has None instead

                    // let's find the attribute in the PE metadata
                    var attributeInfo = PEModule.FindTargetAttribute(peModule.Module.MetadataReader, theParameter.Handle, AttributeDescription.DateTimeConstantAttribute);
                    Assert.True(attributeInfo.HasValue);

                    long attributeValue;
                    Assert.True(peModule.Module.TryExtractLongValueFromAttribute(attributeInfo.Handle, out attributeValue));
                    Assert.Equal(-1L, attributeValue); // check the attribute is constructed with a -1

                    // check .param has no value
                    var constantValue = peModule.Module.GetParamDefaultValue(theParameter.Handle);
                    Assert.Equal(ConstantValue.Null, constantValue);
                };

            var comp = CompileAndVerify(source, symbolValidator: verifier);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")]
        public void DateTimeConstantAttributeReferencedViaRef()
        {
            #region "Source"
            var source1 = @"
using System;
using System.Runtime.CompilerServices;

public class Bar
{
    public void Method([DateTimeConstant(-1)]DateTime p1) { }
}
";

            var source2 = @"
public class Consumer
{
    public static void M()
    {
        new Bar().Method();
    }
}
";
            #endregion

            var libComp = CreateCompilation(source1);
            var libCompRef = new CSharpCompilationReference(libComp);

            var comp2 = CreateCompilation(source2, new[] { libCompRef });
            comp2.VerifyDiagnostics(
                // (6,19): error CS7036: There is no argument given that corresponds to the required formal parameter 'p1' of 'Bar.Method(DateTime)'
                //         new Bar().Method();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Method").WithArguments("p1", "Bar.Method(System.DateTime)").WithLocation(6, 19)
                );

            // The native compiler also gives an error: error CS1501: No overload for method 'Method' takes 0 arguments
            var libAssemblyRef = libComp.EmitToImageReference();
            var comp3 = CreateCompilation(source2, new[] { libAssemblyRef });
            comp3.VerifyDiagnostics(
                // (6,19): error CS7036: There is no argument given that corresponds to the required formal parameter 'p1' of 'Bar.Method(DateTime)'
                //         new Bar().Method();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Method").WithArguments("p1", "Bar.Method(System.DateTime)").WithLocation(6, 19)
                );
        }

        [Fact]
        [WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")]
        public void DateTimeConstantAttributeWithBadDefaultValue()
        {
            #region "Source"
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class Bar
{
    public DateTime M1([DateTimeConstant(-1)] DateTime x = default(DateTime)) { return x; }
    public static void Main()
    {
        Console.WriteLine(new Bar().M1().Ticks);
    }
}
";
            #endregion

            // The native C# compiler would succeed and emit this:
            // .method public hidebysig instance void M1([opt] valuetype[mscorlib] System.DateTime x) cil managed
            // {
            // .param [1] = nullref
            // .custom instance void[mscorlib] System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 FF FF FF FF FF FF FF FF 00 00 )

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,60): error CS8017: The parameter has multiple distinct default values.
                //     public DateTime M1([DateTimeConstant(-1)] DateTime x = default(DateTime)) { return x; }
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "default(DateTime)").WithLocation(7, 60)
                );
        }

        [Fact]
        [WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")]
        public void DateTimeConstantAttributeWithValidDefaultValue()
        {
            #region "Source"
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class Bar
{
    public DateTime M1([DateTimeConstant(42)] DateTime x = default(DateTime)) { return x; }
    public static void Main()
    {
        Console.WriteLine(new Bar().M1().Ticks);
    }
}
";
            #endregion

            // The native C# compiler emits this:
            // .param [1] = nullref
            // .custom instance void[mscorlib] System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = (01 00 2A 00 00 00 00 00 00 00 00 00 )

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,60): error CS8017: The parameter has multiple distinct default values.
                //     public DateTime M1([DateTimeConstant(42)] DateTime x = default(DateTime)) { return x; }
                Diagnostic(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, "default(DateTime)").WithLocation(7, 60)
                );
        }

        [Fact]
        [WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")]
        public void DateTimeConstantAttributeWithBadDefaultValueOnField()
        {
            #region "Source"
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
   [DateTimeConstant(-1)]
   public DateTime F = default(DateTime);

   public static void Main()
   {
     System.Console.WriteLine(new C().F.Ticks);
   }
}
";
            #endregion

            // The native C# compiler emits this:
            // .field public valuetype[mscorlib] System.DateTime F
            // .custom instance void[mscorlib] System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 FF FF FF FF FF FF FF FF 00 00 )

            // using the native compiler, this code outputs 0
            var comp = CompileAndVerify(source, expectedOutput: "0");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")]
        public void DateTimeConstantAttributeWithValidDefaultValueOnField()
        {
            #region "Source"
            var source = @"
using System;
using System.Runtime.CompilerServices;

public class C
{
   [DateTimeConstant(42)]
   public DateTime F = default(DateTime);

   public static void Main()
   {
      System.Console.WriteLine(new C().F.Ticks);
   }
}
";
            #endregion

            // The native C# compiler emits this:
            // .field public valuetype[mscorlib] System.DateTime F
            // .custom instance void[mscorlib] System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 2A 00 00 00 00 00 00 00 00 00 )

            // Using the native compiler, the code executes to output 0
            var comp = CompileAndVerify(source, expectedOutput: "0");
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")]
        public void LoadingDateTimeConstantWithBadValue()
        {
            var ilsource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig instance valuetype [mscorlib]System.DateTime
          Method([opt] valuetype [mscorlib]System.DateTime p) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 FF FF FF FF FF FF FF FF 00 00 )
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (valuetype [mscorlib]System.DateTime V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C::Method

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

} // end of class C

";

            var cssource = @"
public class D
{
    public static void Main()
    {
        System.Console.WriteLine(new C().Method().Ticks);
    }
}
";

            var ilReference = CompileIL(ilsource);
            CompileAndVerify(cssource, expectedOutput: "0", references: new[] { ilReference });
            // The native compiler would produce a working exe, but that exe would fail at runtime
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
            CreateCompilation(source).VerifyDiagnostics(
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
    public void M([DefaultParameterValue((short)1)]int goo) 
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

            CompileAndVerify(source, symbolValidator: verifier);
        }

        [Fact]
        public void DPV_String()
        {
            var compilation = CreateCompilation(@"
using System.Runtime.InteropServices;

public class C
{
    public void M([DefaultParameterValue(""default str"")]string str) 
    {
    }
}
");

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
            var compilation = CreateCompilation(@"
using System.Runtime.InteropServices;

public class C
{
    public void M([Optional]int i) 
    {
    }
}
");

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
            var c1 = CreateCompilation(@"
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
");

            var c2 = CreateCompilation(@"
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
            var compilation = CreateCompilation(@"
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
            var compilation = CreateCompilation(@"
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
            CompileAndVerify(source, assemblyValidator: (assembly) =>
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

            CompileAndVerify(source, assemblyValidator: (assembly) =>
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
            CompileAndVerify(text);
        }

        [Fact, WorkItem(544934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544934")]
        public void Bug13129()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
class C
{
    static void Goo([Optional][DefaultParameterValue(5)] decimal? x)
    {
        Console.WriteLine(x);
    }
    static void Main()
    {
        Goo();
    }
}";
            CompileAndVerify(source, expectedOutput: @"5");
        }

        [Fact]
        public void OptionalParameterInTheMiddle()
        {
            var compilation = CreateCompilation(@"
using System.Runtime.InteropServices;
using System;

public class X
{  
    public int InTheMiddle(int a, [Optional, DefaultParameterValue((short)1)]int b, int c){
        return 2;
    } 
}");

            CompileAndVerify(compilation);
        }

        [Fact]
        public void OptionalAttributeParameter_Numeric()
        {
            var compilation = CreateCompilation(@"
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
            var compilation = CreateCompilation(@"
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

        [Fact, WorkItem(546785, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546785")]
        public void OptionalAttributeOnPartialMethodParameters()
        {
            var source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

partial class C
{
    partial void Goo([Optional] int x);
    partial void Goo([DefaultParameterValue(0)] int x) { }

    partial void Goo2([DefaultParameterValue(0)] int x);
    partial void Goo2([Optional] int x) { }

    partial void Goo3([Optional][DefaultParameterValue(0)] int x);
    partial void Goo3(int x) { }

    partial void Goo4(int x);
    partial void Goo4([Optional][DefaultParameterValue(0)] int x) { }
}
";
            Action<SourceOrdinaryMethodSymbol> partialValidator = (SourceOrdinaryMethodSymbol sourceMethod) =>
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

                var sourceMethod = typeC.GetMember<SourceOrdinaryMethodSymbol>("Goo");
                partialValidator(sourceMethod);

                sourceMethod = typeC.GetMember<SourceOrdinaryMethodSymbol>("Goo2");
                partialValidator(sourceMethod);

                sourceMethod = typeC.GetMember<SourceOrdinaryMethodSymbol>("Goo3");
                partialValidator(sourceMethod);

                sourceMethod = typeC.GetMember<SourceOrdinaryMethodSymbol>("Goo4");
                partialValidator(sourceMethod);
            };

            CompileAndVerify(source, sourceSymbolValidator: sourceValidator);
        }

        [WorkItem(544303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544303")]
        [Fact]
        public void OptionalAttributeBindingCycle()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
 
[Goo]
public class Goo: Attribute
{
    public Goo([Optional][Goo]int y) {}
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
 
public class Goo: Attribute
{
    public Goo([Optional(isOpt: false)][Goo]int y) {}
    public static void Main() {}
}";

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (15,17): warning CS0436: The type 'System.Runtime.InteropServices.OptionalAttribute' in '' conflicts with the imported type 'System.Runtime.InteropServices.OptionalAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public Goo([Optional(isOpt: false)][Goo]int y) {}
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Optional").WithArguments("", "System.Runtime.InteropServices.OptionalAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Runtime.InteropServices.OptionalAttribute").WithLocation(15, 17),
                // (15,41): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'Goo.Goo(int)'
                //     public Goo([Optional(isOpt: false)][Goo]int y) {}
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Goo").WithArguments("y", "Goo.Goo(int)").WithLocation(15, 41));
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
 
public class Goo: Attribute
{
    public Goo([Optional][Goo]int y) {}
    public static void Main() {}
}";

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (16,17): warning CS0436: The type 'System.Runtime.InteropServices.OptionalAttribute' in '' conflicts with the imported type 'System.Runtime.InteropServices.OptionalAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public Goo([Optional][Goo]int y) {}
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Optional").WithArguments("", "System.Runtime.InteropServices.OptionalAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Runtime.InteropServices.OptionalAttribute"),
                // (16,17): error CS0592: Attribute 'Optional' is not valid on this declaration type. It is only valid on 'class' declarations.
                //     public Goo([Optional][Goo]int y) {}
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
 
public class Goo: Attribute
{
    public Goo([Optional(new Goo())][Goo]int y) {}
    public static void Main() {}
}";

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                // (16,17): warning CS0436: The type 'System.Runtime.InteropServices.OptionalAttribute' in '' conflicts with the imported type 'System.Runtime.InteropServices.OptionalAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public Goo([Optional(new Goo())][Goo]int y) {}
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Optional").WithArguments("", "System.Runtime.InteropServices.OptionalAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Runtime.InteropServices.OptionalAttribute").WithLocation(16, 17),
                // (16,30): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'Goo.Goo(int)'
                //     public Goo([Optional(new Goo())][Goo]int y) {}
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Goo").WithArguments("y", "Goo.Goo(int)").WithLocation(16, 30),
                // (16,38): error CS7036: There is no argument given that corresponds to the required formal parameter 'y' of 'Goo.Goo(int)'
                //     public Goo([Optional(new Goo())][Goo]int y) {}
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Goo").WithArguments("y", "Goo.Goo(int)").WithLocation(16, 38));
        }

        [Fact, WorkItem(546624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546624")]
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
            CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: "");
        }

        [Fact, WorkItem(546624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546624")]
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

            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(1036356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1036356")]
        public void EnumAsDefaultParameterValue()
        {
            const string source = @"
using System;
using System.Runtime.InteropServices;

class Program
{
    static void Goo([Optional][DefaultParameterValue(DayOfWeek.Monday)] Enum x) 
    {
    }

    static void Main()
    {
        Goo();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS0029: Cannot implicitly convert type 'int' to 'Enum'
                //         Goo();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Goo()").WithArguments("int", "System.Enum").WithLocation(13, 9));
        }

        #endregion

        #region DecimalConstantAttribute

        [Fact, WorkItem(544438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544438"), WorkItem(538206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538206")]
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
    public void Goo1([Optional][DecimalConstant(0, 0, (uint)0, (uint)0, (uint)100)] decimal i)
    {
        Console.Write(i);
    }

    public void Goo3([Optional][DateTimeConstant(200)] DateTime dt)
    {
        Console.Write(dt.Ticks);
    }

    public void Goo4([Optional][DefaultParameterValue(300)] int i)
    {
        Console.Write(i);
    }

    public void Goo5([Optional][DefaultParameterValue(400)] object i)
    {
        Console.Write(i);
    }
}

class Test
{
    public static void Main()
    {
        var p = new Parent();
        p.Goo1();
        p.Goo3();
        p.Goo4();
        p.Goo5();
    }
}
";
            CompileAndVerify(source, expectedOutput: @"100200300400");
        }

        [WorkItem(544516, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544516")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/23760")]
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/23760")]
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
        public void InAttribute_RefParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([In]ref int p) { }
}").VerifyDiagnostics();
        }

        [Fact]
        public void OutAttribute_RefParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([Out]ref int p) { }
}").VerifyDiagnostics(
                // (5,39): error CS0662: Cannot specify the Out attribute on a ref parameter without also specifying the In attribute.
                //     public static void M([Out]ref int p) { }
                Diagnostic(ErrorCode.ERR_OutAttrOnRefParam, "p").WithLocation(5, 39));
        }

        [Fact]
        public void InAndOutAttributes_RefParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([In, Out]ref int p) { }
}").VerifyDiagnostics();
        }

        [Fact]
        public void InAttribute_OutParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([In]out int p) { p = 0; }
}").VerifyDiagnostics(
                // (5,38): error CS0036: An out parameter cannot have the In attribute
                //     public static void M([In]out int p) { p = 0; }
                Diagnostic(ErrorCode.ERR_InAttrOnOutParam, "p").WithLocation(5, 38));
        }

        [Fact]
        public void OutAttribute_OutParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([Out]out int p) { p = 0; }
}").VerifyDiagnostics();
        }

        [Fact]
        public void InAndOutAttributes_OutParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([In, Out]out int p) { p = 0; }
}").VerifyDiagnostics(
                // (5,43): error CS0036: An out parameter cannot have the In attribute
                //     public static void M([In, Out]out int p) { p = 0; }
                Diagnostic(ErrorCode.ERR_InAttrOnOutParam, "p").WithLocation(5, 43));
        }

        [Fact]
        public void InAttribute_InParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([In]in int p) { }
}").VerifyDiagnostics();
        }

        [Fact]
        public void OutAttribute_InParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([Out]in int p) { }
}").VerifyDiagnostics(
                // (5,38): error CS8355: An in parameter cannot have the Out attribute.
                //     public static void M([Out]in int p) { }
                Diagnostic(ErrorCode.ERR_OutAttrOnInParam, "p").WithLocation(5, 38));
        }

        [Fact]
        public void OutAndInAttributes_InParameter()
        {
            CreateCompilation(@"
using System.Runtime.InteropServices;
class C
{
    public static void M([Out, In]in int p) { }
}").VerifyDiagnostics(
                // (5,42): error CS8355: An in parameter cannot have the Out attribute.
                //     public static void M([Out, In]in int p) { }
                Diagnostic(ErrorCode.ERR_OutAttrOnInParam, "p").WithLocation(5, 42));
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
            var source = CreateCompilation(@"
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
        [WorkItem(544180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544180"), WorkItem(545030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545030")]
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

            CreateCompilation(source).VerifyDiagnostics(
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

    [DllImport(""goo"", EntryPoint = null)]
    public extern static void F3();

    [DllImport(""goo"", EntryPoint = """")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(544176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544176")]
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
                    MethodImportAttributes.ExactSpelling |
                    MethodImportAttributes.CharSetUnicode |
                    MethodImportAttributes.SetLastError |
                    MethodImportAttributes.CallingConventionCDecl |
                    MethodImportAttributes.BestFitMappingEnable |
                    MethodImportAttributes.ThrowOnUnmappableCharEnable, ((Cci.IPlatformInvokeInformation)info).Flags);
            });
        }

        [Fact]
        [WorkItem(544601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544601")]
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
    [DllImport(""goo"")]
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
                            Assert.Equal("goo", moduleName);
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

        [WorkItem(544238, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544238")]
        [WorkItem(544163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544163")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
            var compilation = CreateCompilation(@"
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

    [DllImport(""goo"")]
    public extern static void f2();

    [DllImport(""goo"", PreserveSig=true)]
    public extern static void f3();

    // false
    [DllImport(""goo"", PreserveSig=false)]
    public extern static void f4();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""goo"", PreserveSig=true)]
    public extern static void f5();

    // false
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""goo"", PreserveSig=false)]
    public extern static void f6();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [PreserveSig]
    abstract public void f7();

    [DllImport(""goo"")]
    [PreserveSig]
    public extern static void f8();

    [PreserveSig]
    [DllImport(""goo"", PreserveSig=true)]
    public extern static void f9();

    [DllImport(""goo"", PreserveSig=false)]
    [PreserveSig]
    public extern static void f10();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""goo"", PreserveSig=true)]
    [PreserveSig]
    public extern static void f11();

    [DllImport(""goo"", PreserveSig=false)]
    [PreserveSig]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    public extern static void f12();

    [DllImport(""goo"", PreserveSig=false)]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [PreserveSig]
    public extern static void f13();

    [PreserveSig]
    [DllImport(""goo"", PreserveSig=false)]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    public extern static void f14();

    // false
    [PreserveSig]
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""goo"", PreserveSig=false)]
    public extern static void f15();

    // false
    [MethodImpl(MethodImplOptions.PreserveSig)]
    [PreserveSig]
    [DllImport(""goo"", PreserveSig=false)]
    public extern static void f16();

    [MethodImpl(MethodImplOptions.PreserveSig)]
    [DllImport(""goo"", PreserveSig=false)]
    [PreserveSig]
    public extern static void f17();
    
    public static void f18() {}

    [MethodImpl(MethodImplOptions.Synchronized)]
    [DllImport(""goo"", PreserveSig=false)]
    [PreserveSig]
    public extern static void f19();

    [PreserveSig]
    [DllImport(""goo"")]
    [MethodImpl(MethodImplOptions.Synchronized)]
    public extern static void f20();

    [PreserveSig]
    [DllImport(""goo"", PreserveSig=false)]
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
            CreateCompilation(source).VerifyDiagnostics(
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

        [Fact, WorkItem(544518, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544518")]
        public void DllImport_DefaultCharSet1()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[module: DefaultCharSet(CharSet.Ansi)]

abstract class C
{
    [DllImport(""goo"")]
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
    [DllImport(""goo"")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
	     
	  void goo() 
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
            CreateCompilation(source).VerifyDiagnostics(
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

        [Fact]
        public void TestComImportAttribute()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
class A
{
    public static extern void Goo();
}

public class MainClass
{
    public static int Main ()
    {
        A.Goo();
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

                var methodGoo = (Cci.IMethodDefinition)typeA.GetMember("Goo");
                Assert.True(methodGoo.IsExternal);
            };

            Action<ModuleSymbol> metadataValidator = (ModuleSymbol m) =>
            {
                var typeA = m.GlobalNamespace.GetTypeMember("A");
                Assert.True(typeA.IsComImport);
                Assert.Equal(1, typeA.GetAttributes().Length);

                var ctorA = typeA.InstanceConstructors.First();
                Assert.False(ctorA.IsExtern);

                var methodGoo = (MethodSymbol)typeA.GetMember("Goo");
                Assert.False(methodGoo.IsExtern);
            };

            // the resulting code does not need to verify
            // This is consistent with Dev10 behavior
            CompileAndVerify(source, options: TestOptions.ReleaseDll, verify: Verification.Fails, sourceSymbolValidator: sourceValidator, symbolValidator: metadataValidator);
        }

        [Fact, WorkItem(544507, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544507")]
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
            var compDll = CreateCompilationWithMscorlib40AndSystemCore(source, assemblyName: "NewOnInterface_FromMetadata");

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
            CompileAndVerify(source2, references: new[] { compDll.ToMetadataReference() }, expectedOutput: expectedOutput);
            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CompileAndVerify(source2, references: new[] { compDll.EmitToImageReference() }, expectedOutput: expectedOutput);
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
            var compDll = CreateCompilationWithMscorlib40AndSystemCore(source, assemblyName: "NewOnInterface_GenericTypeCoClass");

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
            CompileAndVerify(source2, references: new[] { compDll.ToMetadataReference() }, expectedOutput: expectedOutput);
            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CompileAndVerify(source2, references: new[] { compDll.EmitToImageReference() }, expectedOutput: expectedOutput);
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
            CreateCompilation(source).VerifyDiagnostics(
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
            var compDll = CreateCompilationWithMscorlib40AndSystemCore(source, assemblyName: "NewOnInterface_InaccessibleInterface");

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
            CreateCompilation(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
                // (6,29): error CS0122: 'Wrapper.IWorksheet' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "IWorksheet").WithArguments("Wrapper.IWorksheet").WithLocation(6, 29));

            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CreateCompilation(source2, references: new[] { compDll.EmitToImageReference() }).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            var compDll = CreateCompilationWithMscorlib40AndSystemCore(source, assemblyName: "NewOnInterface_InaccessibleCoClass");

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
            CreateCompilation(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
                // (6,21): error CS0122: 'Wrapper.WorksheetClass.WorksheetClass()' is inaccessible due to its protection level
                //         var a = new Wrapper.IWorksheet();
                Diagnostic(ErrorCode.ERR_BadAccess, "Wrapper.IWorksheet").WithArguments("Wrapper.WorksheetClass.WorksheetClass()").WithLocation(6, 21));

            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CreateCompilation(source2, references: new[] { compDll.EmitToImageReference() }).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            var compDll = CreateCompilationWithMscorlib40AndSystemCore(source, assemblyName: "NewOnInterface_CoClass_Without_ComImport");

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
            CreateCompilation(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
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
            CreateCompilation(source2, references: new[] { assemblyRef }).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            var compDll = CreateCompilationWithMscorlib40AndSystemCore(source, assemblyName: "NewOnInterface_StructTypeInCoClassAttribute");

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
            CreateCompilation(source2, references: new[] { compDll.ToMetadataReference() }).VerifyDiagnostics(
                // (6,17): error CS0144: Cannot create an instance of the abstract class or interface 'IWorksheet'
                //         var a = new IWorksheet();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new IWorksheet()").WithArguments("IWorksheet").WithLocation(6, 17));

            // Using assembly file reference to test PENamedTypeSymbol symbol CoClass type
            CreateCompilation(source2, references: new[] { compDll.EmitToImageReference() }).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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

            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource);

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

            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource);

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

            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource);

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

            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource);

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
            CreateCompilation(source).VerifyDiagnostics(
                // (16,13): error CS0144: Cannot create an instance of the abstract class or interface 'InterfaceType'
                // [AAttribute(new InterfaceType())]
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new InterfaceType()").WithArguments("InterfaceType").WithLocation(16, 13));
        }

        [Fact, WorkItem(544237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544237")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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

        [WorkItem(545490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545490")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
                // (5,17): error CS0591: Invalid value for argument to 'Guid' attribute
                // [assembly: Guid("69D3E2A0BB0F--4FE3-9860-ED714C510756")]    // invalid format
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, @"""69D3E2A0BB0F--4FE3-9860-ED714C510756""").WithArguments("Guid").WithLocation(5, 17));
        }

        #endregion

        #region SpecialNameAttribute

        [Fact, WorkItem(544392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544392")]
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

        [Fact, WorkItem(544392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544392")]
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

        [Fact]
        [WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")]
        void SerializableFromPE()
        {
            string lib_cs = @"
using System;
[Serializable, Bob]
public class C
{
}
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class BobAttribute : Attribute
{
}";
            var lib_comp = CreateCompilation(lib_cs);
            verify(lib_comp, isSerializablePresent: true);

            var client1 = CreateCompilation("", references: new[] { lib_comp.ToMetadataReference() });
            verify(client1, isSerializablePresent: true);

            var client2 = CreateCompilation("", references: new[] { lib_comp.EmitToImageReference() });
            verify(client2, isSerializablePresent: false);

            void verify(CSharpCompilation comp, bool isSerializablePresent)
            {
                INamedTypeSymbol typeC = comp.GetTypeByMetadataName("C");
                var expectedAttributes = isSerializablePresent ? new[] { "System.SerializableAttribute", "BobAttribute" } : new[] { "BobAttribute" };
                AssertEx.SetEqual(expectedAttributes, typeC.GetAttributes().Select(a => a.ToString()));

                Assert.True(typeC.IsSerializable);

                INamedTypeSymbol typeBobAttribute = comp.GetTypeByMetadataName("BobAttribute");
                Assert.False(typeBobAttribute.IsSerializable);
            }
        }

        [Fact]
        [WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")]
        public void TestIsSerializableProperty()
        {
            string missing = @"
public class TopLevel
{
    public class Nested { }
}
public class TopLevel<T>
{
    public class Nested<U> { }
}
public class Constructed<T> { }
";

            string source = @"
public class C<T>
{
    public class Nested { }
}

[System.Serializable]
public class CS<T>
{
    [System.Serializable]
    public class NestedS { }
}

public class SubstitutedNested : C<int>.Nested { }
public class SubstitutedNestedS : CS<int>.NestedS { }

public class Constructed : C<int> { }
public class ConstructedS : CS<int> { }

public class MissingTopLevel : TopLevel { }
public class MissingNested : TopLevel.Nested { }
public class MissingConstructed : Constructed<int> { }

public class MissingSubstitutedNested<T, U> : TopLevel<T>.Nested<U> { }

namespace System
{
    [System.Serializable]
    public struct ValueTuple<T1, T2> { }
}

public class ValueTupleS
{
    (int, int) M() => throw null;
}
";

            string errors = @"
public class ExtendedError : ExtendedErrorBase { }
public class Unbound : Constructed<> { }
";
            var lib = CreateCompilationWithMscorlib46(missing, assemblyName: "missing");
            lib.VerifyDiagnostics();
            var comp = CreateCompilationWithMscorlib46(source, references: new[] { lib.EmitToImageReference() });
            comp.VerifyDiagnostics();
            var comp2 = CreateCompilationWithMscorlib46(errors, references: new[] { comp.EmitToImageReference() });

            var substitutedNested = comp.GetTypeByMetadataName("SubstitutedNested").BaseType();
            Assert.IsType<SubstitutedNestedTypeSymbol>(substitutedNested);
            Assert.False(((INamedTypeSymbol)substitutedNested).IsSerializable);

            var substitutedNestedS = comp.GetTypeByMetadataName("SubstitutedNestedS").BaseType();
            Assert.IsType<SubstitutedNestedTypeSymbol>(substitutedNestedS);
            Assert.True(((INamedTypeSymbol)substitutedNestedS).IsSerializable);

            var valueTupleS = comp.GetTypeByMetadataName("ValueTupleS").GetMember("M").GetTypeOrReturnType().Type;
            Assert.IsType<TupleTypeSymbol>(valueTupleS);
            Assert.True(((INamedTypeSymbol)valueTupleS).IsSerializable);

            var constructed = comp.GetTypeByMetadataName("Constructed").BaseType();
            Assert.IsType<ConstructedNamedTypeSymbol>(constructed);
            Assert.False(((INamedTypeSymbol)constructed).IsSerializable);

            var constructedS = comp.GetTypeByMetadataName("ConstructedS").BaseType();
            Assert.IsType<ConstructedNamedTypeSymbol>(constructedS);
            Assert.True(((INamedTypeSymbol)constructedS).IsSerializable);

            var extendedError = comp2.GetTypeByMetadataName("ExtendedError").BaseType();
            Assert.IsType<ExtendedErrorTypeSymbol>(extendedError);
            Assert.False(((INamedTypeSymbol)extendedError).IsSerializable);

            var topLevel = comp2.GetTypeByMetadataName("MissingTopLevel").BaseType();
            Assert.IsType<MissingMetadataTypeSymbol.TopLevel>(topLevel);
            Assert.False(((INamedTypeSymbol)topLevel).IsSerializable);

            var nested = comp2.GetTypeByMetadataName("MissingNested").BaseType();
            Assert.IsType<MissingMetadataTypeSymbol.Nested>(nested);
            Assert.False(((INamedTypeSymbol)nested).IsSerializable);

            var constructedError = comp2.GetTypeByMetadataName("MissingConstructed").BaseType();
            Assert.IsType<ConstructedErrorTypeSymbol>(constructedError);
            Assert.False(((INamedTypeSymbol)constructedError).IsSerializable);

            var nestedSubstitutedError = comp2.GetTypeByMetadataName("MissingSubstitutedNested`2").BaseType().ConstructedFrom;
            Assert.IsType<SubstitutedNestedErrorTypeSymbol>(nestedSubstitutedError);
            Assert.False(((INamedTypeSymbol)nestedSubstitutedError).IsSerializable);

            var unbound = comp2.GetTypeByMetadataName("Unbound").BaseType().TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
            Assert.IsType<UnboundArgumentErrorTypeSymbol>(unbound);
            Assert.False(((INamedTypeSymbol)unbound).IsSerializable);

            var script = CreateCompilation("", parseOptions: TestOptions.Script);
            var scriptClass = script.GetTypeByMetadataName("Script");
            Assert.IsType<ImplicitNamedTypeSymbol>(scriptClass);
            Assert.False(((INamedTypeSymbol)scriptClass).IsSerializable);
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
            var compilation = CreateCompilation(source);

            // Verify attributes from source and then load metadata to see attributes are written correctly.
            var comp = CompileAndVerify(
                compilation,
                expectedSignatures: new[]
                {
                    Signature("AttributeTest.MyClass", "UseParams", ".method public hidebysig static System.Void UseParams([System.ParamArrayAttribute()] System.Int32[] list) cil managed"),
                    Signature("AttributeTest.MyClass", "NoParams", ".method public hidebysig static System.Void NoParams(System.Object list) cil managed"),
                },
                symbolValidator: module =>
                {
                    var @namespace = module.GlobalNamespace.GetNestedNamespace("AttributeTest");
                    var type = @namespace.GetTypeMember("MyClass");

                    var useParamsMethod = type.GetMethod("UseParams");
                    var paramsParameter = useParamsMethod.Parameters[0];
                    VerifyParamArrayAttribute(paramsParameter);

                    var noParamsMethod = type.GetMethod("NoParams");
                    var noParamsParameter = noParamsMethod.Parameters[0];
                    Assert.Empty(noParamsParameter.GetAttributes());
                });
        }

        #endregion

        #region AttributeUsageAttribute

        [WorkItem(541733, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541733")]
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
            var compilation = CreateCompilationWithMscorlib40(syntaxTree);

            var comp = compilation.VerifyDiagnostics(
                // test.cs(4,6): warning CS0436: The type 'AttributeUsageAttribute' in 'test.cs' conflicts with the imported type 'AttributeUsageAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'test.cs'.
                //     [AttributeUsage(AttributeTargets.Class)]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AttributeUsage").WithArguments("test.cs", "System.AttributeUsageAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.AttributeUsageAttribute").WithLocation(4, 6),
                // test.cs(5,6): warning CS0436: The type 'AttributeUsageAttribute' in 'test.cs' conflicts with the imported type 'AttributeUsageAttribute' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in 'test.cs'.
                //     [AttributeUsage(AttributeTargets.Class)]
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "AttributeUsage").WithArguments("test.cs", "System.AttributeUsageAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.AttributeUsageAttribute").WithLocation(5, 6),
                // test.cs(5,6): error CS0579: Duplicate 'AttributeUsage' attribute
                //     [AttributeUsage(AttributeTargets.Class)]
                Diagnostic(ErrorCode.ERR_DuplicateAttribute, "AttributeUsage").WithArguments("AttributeUsage").WithLocation(5, 6));
        }

        [WorkItem(541733, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541733")]
        [WorkItem(546102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546102")]
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
            var compilation = CreateCompilation(syntaxTree, options: TestOptions.ReleaseDll);

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

        [WorkItem(546102, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546102")]
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

        [WorkItem(546056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546056")]
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
            CreateCompilation(source).VerifyDiagnostics(
                // (7,54): error CS0103: The name 'Missing' does not exist in the current context
                // 	public const AttributeTargets badAttributeTargets = Missing;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Missing").WithArguments("Missing"),
                // (4,17): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [AttributeUsage(badAttributeTargets)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "badAttributeTargets"));
        }

        #endregion

        #region InternalsVisibleToAttribute

        [WorkItem(542173, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542173")]
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
            var comp1 = CreateCompilation(text1, options: opt);
            var compref1 = new CSharpCompilationReference(comp1);
            var comp2 = CreateCompilation(text2, references: new[] { compref1 }, options: opt, assemblyName: "Child");
            var comp3 = CreateCompilation(text3, references: new[] { compref1, new CSharpCompilationReference(comp2) }, options: opt, assemblyName: "Child2");
            // OK
            comp3.VerifyDiagnostics();

            comp3 = CreateCompilation(text3, references: new[] { compref1, new CSharpCompilationReference(comp2) }, options: opt, assemblyName: "Child2");
            comp3.VerifyDiagnostics();
        }

        #endregion

        #region CustomConstantAttribute

        [Fact, WorkItem(544440, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544440"), WorkItem(538206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538206")]
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
    public void Goo2([Optional][ObjectConstant(1000)] object obj)
    {
        Console.WriteLine(obj);
    }

    public static void Main()
    {
        new Test().Goo2();
    }
}
";
            #endregion

            CompileAndVerify(source, expectedOutput: @"System.Reflection.Missing")
                .VerifyIL("Test.Main", @"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  ldsfld     ""object System.Type.Missing""
  IL_000a:  call       ""void Test.Goo2(object)""
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

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics();
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
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestTypeLibVersionAttribute_Invalid()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: TypeLibVersionAttribute(-1, int.MinValue)]
";
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestComCompatibleVersionAttribute_Invalid()
        {
            var source = @"
using System.Runtime.InteropServices;

[assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)]
";
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
                // (4,12): error CS7036: There is no argument given that corresponds to the required formal parameter 'build' of 'System.Runtime.InteropServices.ComCompatibleVersionAttribute.ComCompatibleVersionAttribute(int, int, int, int)'
                // [assembly: ComCompatibleVersionAttribute("str", 0)]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, @"ComCompatibleVersionAttribute(""str"", 0)").WithArguments("build", "System.Runtime.InteropServices.ComCompatibleVersionAttribute.ComCompatibleVersionAttribute(int, int, int, int)").WithLocation(4, 12));
        }

        #endregion

        #region WindowsRuntimeImportAttribute

        [Fact]
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

            var verifier = CompileAndVerify(source, sourceSymbolValidator: sourceValidator, symbolValidator: metadataValidator, verify: Verification.Fails, targetFramework: TargetFramework.Mscorlib40);
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

        [Fact, WorkItem(546062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546062")]
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

[GooAttribute.BarAttribute.Baz]
[Obsolete(""Blah"")]
class GooAttribute : Attribute
{
    class BazAttribute : Attribute { }

    class BarAttribute : GooAttribute { }
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

interface IGoo<T> {}
[Obsolete]
class SelfReferenceInBase : IGoo<SelfReferenceInBase> {}

class SelfReferenceInBase1 : IGoo<SelfReferenceInBase> {}

";
            CreateCompilation(source).VerifyDiagnostics(
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
                // class SelfReferenceInBase1 : IGoo<SelfReferenceInBase> {}
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
        public void TestObsoleteAttributeOnMembersAndAccessors()
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
        
        var p3 = t.Prop2;
        t.Prop2 = p3;

        var p4 = t.Prop3;
        t.Prop3 = p4;

        var p5 = t.Prop4;
        t.Prop4 = p5;
        
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
        set {}
    }

    public int Prop3
    {
        get { return 10; }
        [Obsolete] set { }
    }

    public int Prop4
    {
        [Obsolete] get { return 10; }
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
            CreateCompilationWithMscorlib40(source, new[] { ExtensionAssemblyRef }).VerifyDiagnostics(
                // (98,10): error CS8423: Attribute 'System.ObsoleteAttribute' is not valid on event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Obsolete] add {}
                Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(98, 10),
                // (99,10): error CS8423: Attribute 'System.ObsoleteAttribute' is not valid on event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Obsolete("Don't use remove accessor")] remove {}
                Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(99, 10),
                // (8,9): warning CS0612: 'Test.ObsoleteMethod1()' is obsolete
                //         ObsoleteMethod1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "ObsoleteMethod1()").WithArguments("Test.ObsoleteMethod1()").WithLocation(8, 9),
                // (9,9): warning CS0618: 'Test.ObsoleteMethod2()' is obsolete: 'Do not call this method'
                //         ObsoleteMethod2();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "ObsoleteMethod2()").WithArguments("Test.ObsoleteMethod2()", "Do not call this method").WithLocation(9, 9),
                // (10,9): error CS0619: 'Test.ObsoleteMethod3()' is obsolete: ''
                //         ObsoleteMethod3();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "ObsoleteMethod3()").WithArguments("Test.ObsoleteMethod3()", "").WithLocation(10, 9),
                // (11,9): warning CS0612: 'Test.ObsoleteMethod5()' is obsolete
                //         ObsoleteMethod5();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "ObsoleteMethod5()").WithArguments("Test.ObsoleteMethod5()").WithLocation(11, 9),
                // (15,9): warning CS0618: 'Test.ObsoleteMethod4()' is obsolete: 'Do not call this method'
                //         t.ObsoleteMethod4();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteMethod4()").WithArguments("Test.ObsoleteMethod4()", "Do not call this method").WithLocation(15, 9),
                // (16,17): warning CS0618: 'Test.field1' is obsolete: 'Do not use this field'
                //         var f = t.field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.field1").WithArguments("Test.field1", "Do not use this field").WithLocation(16, 17),
                // (17,18): warning CS0618: 'Test.Property1' is obsolete: 'Do not use this property'
                //         var p1 = t.Property1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.Property1").WithArguments("Test.Property1", "Do not use this property").WithLocation(17, 18),
                // (18,18): warning CS0618: 'Test.Property2' is obsolete: 'Do not use this property'
                //         var p2 = t.Property2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.Property2").WithArguments("Test.Property2", "Do not use this property").WithLocation(18, 18),
                // (20,18): warning CS0612: 'Test.Prop2.get' is obsolete
                //         var p3 = t.Prop2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "t.Prop2").WithArguments("Test.Prop2.get").WithLocation(20, 18),
                // (24,9): warning CS0612: 'Test.Prop3.set' is obsolete
                //         t.Prop3 = p4;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "t.Prop3").WithArguments("Test.Prop3.set").WithLocation(24, 9),
                // (26,18): warning CS0612: 'Test.Prop4.get' is obsolete
                //         var p5 = t.Prop4;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "t.Prop4").WithArguments("Test.Prop4.get").WithLocation(26, 18),
                // (27,9): warning CS0612: 'Test.Prop4.set' is obsolete
                //         t.Prop4 = p5;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "t.Prop4").WithArguments("Test.Prop4.set").WithLocation(27, 9),
                // (32,9): warning CS0618: 'TestExtension.ObsoleteExtensionMethod1(Test)' is obsolete: 'Do not call this extension method'
                //         t.ObsoleteExtensionMethod1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteExtensionMethod1()").WithArguments("TestExtension.ObsoleteExtensionMethod1(Test)", "Do not call this extension method").WithLocation(32, 9),
                // (34,28): warning CS0618: 'Test.ObsoleteMethod4(int)' is obsolete: 'Do not call this method'
                //         Action<int> func = t.ObsoleteMethod4;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteMethod4").WithArguments("Test.ObsoleteMethod4(int)", "Do not call this method").WithLocation(34, 28),
                // (36,24): warning CS0618: 'Test.ObsoleteMethod4()' is obsolete: 'Do not call this method'
                //         Action func1 = t.ObsoleteMethod4;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.ObsoleteMethod4").WithArguments("Test.ObsoleteMethod4()", "Do not call this method").WithLocation(36, 24),
                // (38,30): warning CS0618: 'Test.Property1' is obsolete: 'Do not use this property'
                //         Test t1 = new Test { Property1 = 10, Property2 =20};
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Property1").WithArguments("Test.Property1", "Do not use this property").WithLocation(38, 30),
                // (38,46): warning CS0618: 'Test.Property2' is obsolete: 'Do not use this property'
                //         Test t1 = new Test { Property1 = 10, Property2 =20};
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Property2").WithArguments("Test.Property2", "Do not use this property").WithLocation(38, 46),
                // (39,18): warning CS0612: 'Test.this[int]' is obsolete
                //         var i1 = t1[10];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "t1[10]").WithArguments("Test.this[int]").WithLocation(39, 18),
                // (42,9): warning CS0612: 'GenericTest<int>.ObsoleteMethod1<U>()' is obsolete
                //         gt.ObsoleteMethod1<double>();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "gt.ObsoleteMethod1<double>()").WithArguments("GenericTest<int>.ObsoleteMethod1<U>()").WithLocation(42, 9),
                // (43,18): warning CS0618: 'GenericTest<int>.field1' is obsolete: 'Do not use this field'
                //         var gf = gt.field1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "gt.field1").WithArguments("GenericTest<int>.field1", "Do not use this field").WithLocation(43, 18),
                // (44,19): warning CS0618: 'GenericTest<int>.Property1' is obsolete: 'Do not use this property'
                //         var gp1 = gt.Property1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "gt.Property1").WithArguments("GenericTest<int>.Property1", "Do not use this property").WithLocation(44, 19),
                // (30,9): warning CS0618: 'Test.event1' is obsolete: 'Do not use this event'
                //         t.event1 += () => { };
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "t.event1").WithArguments("Test.event1", "Do not use this event").WithLocation(30, 9),
                // (45,9): warning CS0618: 'GenericTest<int>.event1' is obsolete: 'Do not use this event'
                //         gt.event1 += (i) => { };
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "gt.event1").WithArguments("GenericTest<int>.event1", "Do not use this event").WithLocation(45, 9),
                // (121,28): warning CS0067: The event 'GenericTest<T>.event1' is never used
                //     public event Action<T> event1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "event1").WithArguments("GenericTest<T>.event1").WithLocation(121, 28));
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
            CreateCompilation(source).VerifyDiagnostics(
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

        [Fact, WorkItem(546062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546062")]
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

    public int Prop2 { [Obsolete(""Do not use Prop2.Get"")] get; set; }

    public int Prop3 { get; [Obsolete(""Do not use Prop3.Get"", true)] set; }

    [Obsolete(""Do not use field1"", true)]
    public TestClass field1;

    [Obsolete(""Do not use event"", true)]
    public Action event1;
}
";
            var peReference = MetadataReference.CreateFromStream(CreateCompilation(peSource).EmitToStream());

            var source = @"
public class Test
{
    public static void goo1(TestClass1 c) {}
    public static void goo2(TestClass2 c) {}
    public static void goo3(TestClass3 c) {}
    public static void goo4(TestClass4 c) {}

    public static void Main()
    {
        TestClass c = new TestClass();
        c.TestMethod();
        var i = c.Prop1;
        c = c.field1;
        c.event1();
        c.event1 += () => {};
        c.Prop2 = 42;
        i = c.Prop2;
        c.Prop3 = 42;
        i = c.Prop3;
    }
}
";
            CreateCompilation(source, new[] { peReference }).VerifyDiagnostics(
                // (5,29): warning CS0618: 'TestClass2' is obsolete: 'TestClass2 is obsolete'
                //     public static void goo2(TestClass2 c) {}
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "TestClass2").WithArguments("TestClass2", "TestClass2 is obsolete").WithLocation(5, 29),
                // (6,29): error CS0619: 'TestClass3' is obsolete: 'Do not use TestClass3'
                //     public static void goo3(TestClass3 c) {}
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "TestClass3").WithArguments("TestClass3", "Do not use TestClass3").WithLocation(6, 29),
                // (7,29): warning CS0618: 'TestClass4' is obsolete: 'TestClass4 is obsolete'
                //     public static void goo4(TestClass4 c) {}
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "TestClass4").WithArguments("TestClass4", "TestClass4 is obsolete").WithLocation(7, 29),
                // (4,29): warning CS0612: 'TestClass1' is obsolete
                //     public static void goo1(TestClass1 c) {}
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "TestClass1").WithArguments("TestClass1").WithLocation(4, 29),
                // (12,9): warning CS0618: 'TestClass.TestMethod()' is obsolete: 'Do not use TestMethod'
                //         c.TestMethod();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c.TestMethod()").WithArguments("TestClass.TestMethod()", "Do not use TestMethod").WithLocation(12, 9),
                // (13,17): warning CS0618: 'TestClass.Prop1' is obsolete: 'Do not use Prop1'
                //         var i = c.Prop1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c.Prop1").WithArguments("TestClass.Prop1", "Do not use Prop1").WithLocation(13, 17),
                // (14,13): error CS0619: 'TestClass.field1' is obsolete: 'Do not use field1'
                //         c = c.field1;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "c.field1").WithArguments("TestClass.field1", "Do not use field1").WithLocation(14, 13),
                // (15,9): error CS0619: 'TestClass.event1' is obsolete: 'Do not use event'
                //         c.event1();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "c.event1").WithArguments("TestClass.event1", "Do not use event").WithLocation(15, 9),
                // (16,9): error CS0619: 'TestClass.event1' is obsolete: 'Do not use event'
                //         c.event1 += () => {};
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "c.event1").WithArguments("TestClass.event1", "Do not use event").WithLocation(16, 9),
                // (18,13): warning CS0618: 'TestClass.Prop2.get' is obsolete: 'Do not use Prop2.Get'
                //         i = c.Prop2;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c.Prop2").WithArguments("TestClass.Prop2.get", "Do not use Prop2.Get").WithLocation(18, 13),
                // (19,9): error CS0619: 'TestClass.Prop3.set' is obsolete: 'Do not use Prop3.Get'
                //         c.Prop3 = 42;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "c.Prop3").WithArguments("TestClass.Prop3.set", "Do not use Prop3.Get").WithLocation(19, 9));
        }

        [Fact]
        public void TestObsoleteAttributeOnOverriddenMembers()
        {
            var source = @"
using System;
class C1
{
    public virtual void goo() {}
}
class C2 : C1
{
    [Obsolete]
    public override void goo() {}
}
class C3 : C1
{
    [Obsolete]
    public new void goo() {}
}
class C4 : C1
{
    public override void goo() {}
}
class C5 : C4
{
    [Obsolete]
    public override void goo() {}
}
class C6 : C5
{
    public override void goo() {}
}

class D1
{
    [Obsolete]
    public virtual void goo() {}
}
class D2 : D1
{
    public override void goo() {}
}
class D3 : D1
{
    public new void goo() {}
}
class D4 : D1
{
    [Obsolete]
    public override void goo() {}
}
class D5 : D4
{
    public override void goo() {}
}
class D6 : D5
{
    [Obsolete]
    public override void goo() {}
}

class E1
{
    public virtual int Goo {get; set;}
}
class E2 : E1
{
    public override int Goo { [Obsolete] get; set;}
}
class E3 : E1
{
    public new int Goo { [Obsolete] get; set;}
}
class E4 : E1
{
    public override int Goo {get; set;}
}
class E5 : E4
{
    public override int Goo { [Obsolete] get; set;}
}
class E6 : E5
{
    public override int Goo {get; set;}
}

class F1
{
    public virtual int Goo { [Obsolete] get; set;}
}
class F2 : F1
{
    public override int Goo {get; set;}
}
class F3 : F1
{
    public new int Goo {get; set;}
}
class F4 : F1
{
    public override int Goo { [Obsolete] get; set;}
}
class F5 : F4
{
    public override int Goo {get; set;}
}
class F6 : F5
{
    public override int Goo { [Obsolete] get; set;}
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,26): warning CS0809: Obsolete member 'C2.goo()' overrides non-obsolete member 'C1.goo()'
                //     public override void goo() {}
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "goo").WithArguments("C2.goo()", "C1.goo()").WithLocation(10, 26),
                // (90,30): warning CS0672: Member 'F2.Goo.get' overrides obsolete member 'F1.Goo.get'. Add the Obsolete attribute to 'F2.Goo.get'.
                //     public override int Goo {get; set;}
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "get").WithArguments("F2.Goo.get", "F1.Goo.get").WithLocation(90, 30),
                // (77,42): warning CS0809: Obsolete member 'E5.Goo.get' overrides non-obsolete member 'E1.Goo.get'
                //     public override int Goo { [Obsolete] get; set;}
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "get").WithArguments("E5.Goo.get", "E1.Goo.get").WithLocation(77, 42),
                // (51,26): warning CS0672: Member 'D5.goo()' overrides obsolete member 'D1.goo()'. Add the Obsolete attribute to 'D5.goo()'.
                //     public override void goo() {}
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "goo").WithArguments("D5.goo()", "D1.goo()").WithLocation(51, 26),
                // (38,26): warning CS0672: Member 'D2.goo()' overrides obsolete member 'D1.goo()'. Add the Obsolete attribute to 'D2.goo()'.
                //     public override void goo() {}
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "goo").WithArguments("D2.goo()", "D1.goo()").WithLocation(38, 26),
                // (24,26): warning CS0809: Obsolete member 'C5.goo()' overrides non-obsolete member 'C1.goo()'
                //     public override void goo() {}
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "goo").WithArguments("C5.goo()", "C1.goo()").WithLocation(24, 26),
                // (102,30): warning CS0672: Member 'F5.Goo.get' overrides obsolete member 'F1.Goo.get'. Add the Obsolete attribute to 'F5.Goo.get'.
                //     public override int Goo {get; set;}
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "get").WithArguments("F5.Goo.get", "F1.Goo.get").WithLocation(102, 30),
                // (65,42): warning CS0809: Obsolete member 'E2.Goo.get' overrides non-obsolete member 'E1.Goo.get'
                //     public override int Goo { [Obsolete] get; set;}
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "get").WithArguments("E2.Goo.get", "E1.Goo.get").WithLocation(65, 42));
        }

        [Fact]
        public void TestConsumptionOfObsoleteAttributeOnOverriddenAccessors()
        {
            var source = @"
using System;

class Base
{
    public virtual int Boo { [Obsolete] get; set;}
    public virtual int Goo { get; set; }
    public virtual int Hoo { [Obsolete(""Base.Hoo is Obsolete"", true)] get; set; }
    public virtual int Joo { [Obsolete(""Base.Joo is Obsolete"", false)] get; set; }
    [Obsolete(""Base.Koo is Obsolete"")] public virtual int Koo {  get; set; }
}
class Derived : Base
{
    public override int Boo { get; set; }
    public override int Goo { [Obsolete] get; set; }
    public override int Hoo { [Obsolete(""Derived.Hoo is Obsolete"", false)] get; set; }
    public override int Joo { [Obsolete(""Derived.Joo is Obsolete"", true)] get; set; }
    public override int Koo { [Obsolete(""Derived.Koo is Obsolete"")] get; set; }
}

public class Program
{
    public void Main()
    {
        var derived = new Derived();
		_ = derived.Boo;
        _ = derived.Goo;
        _ = derived.Hoo;
        _ = derived.Joo;
        _ = derived.Koo;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,31): warning CS0672: Member 'Derived.Boo.get' overrides obsolete member 'Base.Boo.get'. Add the Obsolete attribute to 'Derived.Boo.get'.
                //     public override int Boo { get; set; }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "get").WithArguments("Derived.Boo.get", "Base.Boo.get").WithLocation(14, 31),
                // (15,42): warning CS0809: Obsolete member 'Derived.Goo.get' overrides non-obsolete member 'Base.Goo.get'
                //     public override int Goo { [Obsolete] get; set; }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "get").WithArguments("Derived.Goo.get", "Base.Goo.get").WithLocation(15, 42),
                // (18,25): warning CS0672: Member 'Derived.Koo' overrides obsolete member 'Base.Koo'. Add the Obsolete attribute to 'Derived.Koo'.
                //     public override int Koo { [Obsolete("Derived.Koo is Obsolete")] get; set; }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "Koo").WithArguments("Derived.Koo", "Base.Koo").WithLocation(18, 25),
                // (18,69): warning CS0809: Obsolete member 'Derived.Koo.get' overrides non-obsolete member 'Base.Koo.get'
                //     public override int Koo { [Obsolete("Derived.Koo is Obsolete")] get; set; }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "get").WithArguments("Derived.Koo.get", "Base.Koo.get").WithLocation(18, 69),
                // (26,7): warning CS0612: 'Base.Boo.get' is obsolete
                // 		_ = derived.Boo;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "derived.Boo").WithArguments("Base.Boo.get").WithLocation(26, 7),
                // (28,13): error CS0619: 'Base.Hoo.get' is obsolete: 'Base.Hoo is Obsolete'
                //         _ = derived.Hoo;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "derived.Hoo").WithArguments("Base.Hoo.get", "Base.Hoo is Obsolete").WithLocation(28, 13),
                // (29,13): warning CS0618: 'Base.Joo.get' is obsolete: 'Base.Joo is Obsolete'
                //         _ = derived.Joo;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "derived.Joo").WithArguments("Base.Joo.get", "Base.Joo is Obsolete").WithLocation(29, 13),
                // (30,13): warning CS0618: 'Base.Koo' is obsolete: 'Base.Koo is Obsolete'
                //         _ = derived.Koo;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "derived.Koo").WithArguments("Base.Koo", "Base.Koo is Obsolete").WithLocation(30, 13));
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
            CreateCompilation(source).VerifyDiagnostics(
                // (7,15): warning CS0618: 'Test.F1' is obsolete: 'F1 is obsolete'
                //     [SomeAttr(F1)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F1").WithArguments("Test.F1", "F1 is obsolete").WithLocation(7, 15),
                // (15,15): error CS0619: 'Test.F2' is obsolete: 'F2 is obsolete'
                //     [SomeAttr(F2)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F2").WithArguments("Test.F2", "F2 is obsolete").WithLocation(15, 15),
                // (11,15): warning CS0618: 'Test.F3' is obsolete: 'F3 is obsolete'
                //     [SomeAttr(F3)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F3").WithArguments("Test.F3", "F3 is obsolete").WithLocation(11, 15),
                // (18,15): error CS0619: 'Test.F4' is obsolete: 'blah'
                //     [Obsolete(F4, true)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F4").WithArguments("Test.F4", "blah").WithLocation(18, 15),
                // (21,15): error CS0120: An object reference is required for the non-static field, method, or property 'Test.F5'
                //     [Obsolete(F5)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "F5").WithArguments("Test.F5").WithLocation(21, 15),
                // (24,15): error CS0120: An object reference is required for the non-static field, method, or property 'Test.P1'
                //     [Obsolete(P1, true)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P1").WithArguments("Test.P1").WithLocation(24, 15),
                // (28,15): warning CS0612: 'Test.P2' is obsolete
                //     [SomeAttr(P2, true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "P2").WithArguments("Test.P2").WithLocation(28, 15),
                // (28,15): error CS0120: An object reference is required for the non-static field, method, or property 'Test.P2'
                //     [SomeAttr(P2, true)]
                Diagnostic(ErrorCode.ERR_ObjectRequired, "P2").WithArguments("Test.P2").WithLocation(28, 15),
                // (28,6): error CS1729: 'SomeAttr' does not contain a constructor that takes 2 arguments
                //     [SomeAttr(P2, true)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "SomeAttr(P2, true)").WithArguments("SomeAttr", "2").WithLocation(28, 6),
                // (31,15): error CS1503: Argument 1: cannot convert from 'method group' to 'string'
                //     [Obsolete(Method1)]
                Diagnostic(ErrorCode.ERR_BadArgType, "Method1").WithArguments("1", "method group", "string").WithLocation(31, 15),
                // (35,16): warning CS0612: 'Test.Method2()' is obsolete
                //     [SomeAttr1(Method2)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Method2").WithArguments("Test.Method2()").WithLocation(35, 16),
                // (35,6): error CS0181: Attribute constructor parameter 'x' has type 'Action', which is not a valid attribute parameter type
                //     [SomeAttr1(Method2)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "SomeAttr1").WithArguments("x", "System.Action").WithLocation(35, 6),
                // (43,15): error CS0619: 'Test.F7' is obsolete: 'F7 is obsolete'
                //     [Obsolete(F7, true)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F7").WithArguments("Test.F7", "F7 is obsolete").WithLocation(43, 15),
                // (44,15): warning CS0618: 'Test.F6' is obsolete: 'F6 is obsolete'
                //     [SomeAttr(F6)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F6").WithArguments("Test.F6", "F6 is obsolete").WithLocation(44, 15),
                // (45,15): error CS0619: 'Test.F7' is obsolete: 'F7 is obsolete'
                //     [SomeAttr(F7)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F7").WithArguments("Test.F7", "F7 is obsolete").WithLocation(45, 15),
                // (38,15): warning CS0618: 'Test.F6' is obsolete: 'F6 is obsolete'
                //     [Obsolete(F6)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F6").WithArguments("Test.F6", "F6 is obsolete").WithLocation(38, 15),
                // (39,15): warning CS0618: 'Test.F6' is obsolete: 'F6 is obsolete'
                //     [SomeAttr(F6)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F6").WithArguments("Test.F6", "F6 is obsolete").WithLocation(39, 15),
                // (40,15): error CS0619: 'Test.F7' is obsolete: 'F7 is obsolete'
                //     [SomeAttr(F7)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "F7").WithArguments("Test.F7", "F7 is obsolete").WithLocation(40, 15));
        }

        [WorkItem(546064, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546064")]
        [Fact]
        public void TestObsoleteAttributeCycles_02()
        {
            var source = @"
[Goo]
class Goo: Base {}

[Goo]
class Base: System.Attribute
{
    public class Nested: Goo {}
}
";
            CompileAndVerify(source);

            source = @"
using System;

[Obsolete]
public class SomeType
{
    public static SomeType Instance;
    public const  string Message = ""goo"";
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
            CreateCompilation(source, null, TestOptions.ReleaseDll.WithConcurrentBuild(false)).VerifyDiagnostics(
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
    public const  string Message = ""goo"";
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
    public static SomeType someProp { get => new SomeType(); set {} }

    public static string someProp2 { [Obsolete] get => new SomeType().ToString(); }

    public static SomeType someProp3 { [Obsolete] get => new SomeType(); }

    [Obsolete]
    SomeType this[int x] { get { SomeType y = new SomeType(); return y; } }

    [Obsolete]
    SomeType goo(SomeType x)
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
            CreateCompilation(source).VerifyDiagnostics(
                // (27,19): warning CS0612: 'SomeType' is obsolete
                //     public static SomeType someProp3 { [Obsolete] get => new SomeType(); }
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "SomeType").WithArguments("SomeType").WithLocation(27, 19),
                // (20,28): warning CS0067: The event 'Test.someEvent' is never used
                //     event Action<SomeType> someEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "someEvent").WithArguments("Test.someEvent").WithLocation(20, 28));
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
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestObsoleteAttributeP2PReference()
        {
            string s = @"
using System;
[Obsolete]
public class C { 
    [Obsolete]
    public void Goo() {} 
}
";
            var other = CreateCompilation(s);

            s = @"
public class A
{
    protected A(C o)
    {
        o.Goo();
    }
}
";
            CreateCompilation(s, new[] { new CSharpCompilationReference(other) }).VerifyDiagnostics(
                // (3,17): warning CS0612: 'C' is obsolete
                //     protected A(C o)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "C").WithArguments("C"),
                // (5,9): warning CS0612: 'C.Goo()' is obsolete
                //         o.Goo();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "o.Goo()").WithArguments("C.Goo()"));
        }

        [Fact]
        [WorkItem(546455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546455"), WorkItem(546456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546456"), WorkItem(546457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546457")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(546636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546636")]
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
    [Obsolete(""Property"", true)]
    public int Prop2
    {
        get; set;
    }
    public int Prop3
    {
        get; [Obsolete(""setter"", true)]set;
    }
    [Obsolete(""Property"", true)]
    public int Prop4
    {
        get; [Obsolete(""setter"", true)]set;
    }
    public int Prop5
    {
        [Obsolete(""setter"", true)]get; set;
    }
    [Obsolete(""Field"", true)]
    public int Field;
}

[Att]
[Att(Field = 1)]
[Att(Prop = 1)]
[Att(Prop2 = 1)]
[Att(Prop3 = 1)]
[Att(Prop4 = 1)]
[Att(Prop5 = 1)]
public class Test
{
    [Att()]
    public static void Main() { }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (37,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att").WithArguments("Att.Att()", "Constructor").WithLocation(37, 2),
                // (38,6): error CS0619: 'Att.Field' is obsolete: 'Field'
                // [Att(Field = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Field = 1").WithArguments("Att.Field", "Field").WithLocation(38, 6),
                // (38,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Field = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Field = 1)").WithArguments("Att.Att()", "Constructor").WithLocation(38, 2),
                // (39,6): error CS0619: 'Att.Prop' is obsolete: 'Property'
                // [Att(Prop = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop = 1").WithArguments("Att.Prop", "Property").WithLocation(39, 6),
                // (39,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Prop = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Prop = 1)").WithArguments("Att.Att()", "Constructor").WithLocation(39, 2),
                // (40,6): error CS0619: 'Att.Prop2' is obsolete: 'Property'
                // [Att(Prop2 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop2 = 1").WithArguments("Att.Prop2", "Property").WithLocation(40, 6),
                // (40,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Prop2 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Prop2 = 1)").WithArguments("Att.Att()", "Constructor").WithLocation(40, 2),
                // (41,6): error CS0619: 'Att.Prop3.set' is obsolete: 'setter'
                // [Att(Prop3 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop3 = 1").WithArguments("Att.Prop3.set", "setter").WithLocation(41, 6),
                // (41,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Prop3 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Prop3 = 1)").WithArguments("Att.Att()", "Constructor").WithLocation(41, 2),
                // (42,6): error CS0619: 'Att.Prop4' is obsolete: 'Property'
                // [Att(Prop4 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop4 = 1").WithArguments("Att.Prop4", "Property").WithLocation(42, 6),
                // (42,6): error CS0619: 'Att.Prop4.set' is obsolete: 'setter'
                // [Att(Prop4 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop4 = 1").WithArguments("Att.Prop4.set", "setter").WithLocation(42, 6),
                // (42,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Prop4 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Prop4 = 1)").WithArguments("Att.Att()", "Constructor").WithLocation(42, 2),
                // (43,2): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                // [Att(Prop5 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att(Prop5 = 1)").WithArguments("Att.Att()", "Constructor").WithLocation(43, 2),
                // (46,6): error CS0619: 'Att.Att()' is obsolete: 'Constructor'
                //     [Att()]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Att()").WithArguments("Att.Att()", "Constructor").WithLocation(46, 6));
        }

        [Fact]
        public void TestOverridenObsoleteSetterOnAttributes()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class BaseAtt : Attribute
{
    public virtual int Prop
    {
        get { return 1; }
        [Obsolete(""setter"", true)] set { }
    }

    public virtual int Prop1
    {
        get { return 1; }
        [Obsolete(""setter"", true)] set { }
    }

    public virtual int Prop2
    {
        get { return 1; }
        [Obsolete(""base setter"", true)] set { }
    }

    public virtual int Prop3
    {
        get { return 1; }
        set { }
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class DerivedAtt : BaseAtt
{
    public override int Prop
    {
        get { return 1; }
    }

    public override int Prop1
    {
        get { return 1; }
        set { }
    }

    public override int Prop2
    {
        get { return 1; }
        [Obsolete(""derived setter"", true)] set { }
    }

    public override int Prop3
    {
        get { return 1; }
        [Obsolete(""setter"", true)] set { }
    }
}

[DerivedAtt(Prop = 1)]
[DerivedAtt(Prop1 = 1)]
[DerivedAtt(Prop2 = 1)]
[DerivedAtt(Prop3 = 1)]
public class Test
{
    public static void Main() { }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (43,9): warning CS0672: Member 'DerivedAtt.Prop1.set' overrides obsolete member 'BaseAtt.Prop1.set'. Add the Obsolete attribute to 'DerivedAtt.Prop1.set'.
                //         set { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "set").WithArguments("DerivedAtt.Prop1.set", "BaseAtt.Prop1.set").WithLocation(43, 9),
                // (55,36): warning CS0809: Obsolete member 'DerivedAtt.Prop3.set' overrides non-obsolete member 'BaseAtt.Prop3.set'
                //         [Obsolete("setter", true)] set { }
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "set").WithArguments("DerivedAtt.Prop3.set", "BaseAtt.Prop3.set").WithLocation(55, 36),
                // (59,13): error CS0619: 'BaseAtt.Prop.set' is obsolete: 'setter'
                // [DerivedAtt(Prop = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop = 1").WithArguments("BaseAtt.Prop.set", "setter").WithLocation(59, 13),
                // (60,13): error CS0619: 'BaseAtt.Prop1.set' is obsolete: 'setter'
                // [DerivedAtt(Prop1 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop1 = 1").WithArguments("BaseAtt.Prop1.set", "setter").WithLocation(60, 13),
                // (61,13): error CS0619: 'BaseAtt.Prop2.set' is obsolete: 'base setter'
                // [DerivedAtt(Prop2 = 1)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Prop2 = 1").WithArguments("BaseAtt.Prop2.set", "base setter").WithLocation(61, 13));
        }

        [Fact]
        public void TestObsoleteAttributeOnIndexerAccessors()
        {
            var source = @"
using System;

class C1
{
    public int this[int index] { [Obsolete] get => 1; set {} }
}

class C2
{
    public int this[int index] { get => 1; [Obsolete] set {} }
}

public class Program
{
    public void Main()
    {
        var c1 = new C1();
        c1[0] = c1[0];
        var c2 = new C2();
        c2[0] = c2[0];
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (19,17): warning CS0612: 'C1.this[int].get' is obsolete
                //         c1[0] = c1[0];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c1[0]").WithArguments("C1.this[int].get").WithLocation(19, 17),
                // (21,9): warning CS0612: 'C2.this[int].set' is obsolete
                //         c2[0] = c2[0];
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "c2[0]").WithArguments("C2.this[int].set").WithLocation(21, 9));
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(546766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546766")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(547024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547024")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
                // (4,1): info CS8019: Unnecessary using directive.
                // using X = A;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = A;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using Y = A.B; 
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = A.B;"));
        }

        [Fact]
        public void TestObsoleteAndPropertyAccessors()
        {
            var source0 =
@"using System;
namespace Windows.Foundation.Metadata
{
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }
    }
    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}";
            var source1 =
@"using Windows.Foundation.Metadata;
[Deprecated(null, DeprecationType.Deprecate, 0)] class A { }
[Deprecated(null, DeprecationType.Deprecate, 0)] class B { }
[Deprecated(null, DeprecationType.Deprecate, 0)] class C { }
class D
{
    object P { get { return new A(); } }
    [Deprecated(null, DeprecationType.Deprecate, 0)] object Q { get { return new B(); } }
    object R { [Deprecated(null, DeprecationType.Deprecate, 0)] get { return new C(); } }
}";
            var comp = CreateCompilation(new[] { Parse(source0), Parse(source1) });
            comp.VerifyDiagnostics(
                // (7,33): warning CS0612: 'A' is obsolete
                //     object P { get { return new A(); } }
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A").WithArguments("A").WithLocation(7, 33));
        }

        [Fact]
        public void TestObsoleteAndEventAccessors()
        {
            var source0 =
@"using System;
namespace Windows.Foundation.Metadata
{
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }
    }
    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}";
            var source1 =
@"using System;
using Windows.Foundation.Metadata;
[Deprecated(null, DeprecationType.Deprecate, 0)] class A { }
[Deprecated(null, DeprecationType.Deprecate, 0)] class B { }
[Deprecated(null, DeprecationType.Deprecate, 0)] class C { }
class D
{
    event EventHandler E
    {
        add { }
        remove { M(new A()); }
    }
    [Deprecated(null, DeprecationType.Deprecate, 0)] event EventHandler F
    {
        add { }
        remove { M(new B()); }
    }
    event EventHandler G
    {
        add { }
        [Deprecated(null, DeprecationType.Deprecate, 0)] remove { M(new C()); }
    }
    static void M(object o) { }
}";
            var comp = CreateCompilation(new[] { Parse(source0), Parse(source1) });
            comp.VerifyDiagnostics(
                // (11,24): warning CS0612: 'A' is obsolete
                //         remove { M(new A()); }
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A").WithArguments("A").WithLocation(11, 24),
                // (21,10): error CS8423: Attribute 'Windows.Foundation.Metadata.DeprecatedAttribute' is not valid on event accessors. It is only valid on 'assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter' declarations.
                //         [Deprecated(null, DeprecationType.Deprecate, 0)] remove { M(new C()); }
                Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Deprecated").WithArguments("Windows.Foundation.Metadata.DeprecatedAttribute", "assembly, module, class, struct, enum, constructor, method, property, indexer, field, event, interface, parameter, delegate, return, type parameter").WithLocation(21, 10));
        }

        [Fact]
        [WorkItem(531071, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531071")]
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
            CreateCompilation(source).VerifyDiagnostics(
                // (12,22): error CS0619: 'A' is obsolete: 'Do not use'
                //     public class E : Z { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Z").WithArguments("N.A", "Do not use").WithLocation(12, 22),
                // (13,27): error CS0619: 'A' is obsolete: 'Do not use'
                //     public class D : List<Y>
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use").WithLocation(13, 27),
                // (10,22): error CS0619: 'A' is obsolete: 'Do not use'
                //     public class B : X { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "X").WithArguments("N.A", "Do not use").WithLocation(10, 22),
                // (11,22): error CS0619: 'A' is obsolete: 'Do not use'
                //     public class C : Y { }
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use").WithLocation(11, 22),
                // (16,16): error CS0619: 'A' is obsolete: 'Do not use'
                //         public Y y1;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use").WithLocation(16, 16),
                // (17,21): error CS0619: 'A' is obsolete: 'Do not use'
                //         public List<Y> y2;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Y").WithArguments("N.A", "Do not use").WithLocation(17, 21),
                // (18,16): error CS0619: 'A' is obsolete: 'Do not use'
                //         public Z z;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Z").WithArguments("N.A", "Do not use").WithLocation(18, 16),
                // (15,16): error CS0619: 'A' is obsolete: 'Do not use'
                //         public X x;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "X").WithArguments("N.A", "Do not use").WithLocation(15, 16));
        }

        [ConditionalFact(typeof(IsEnglishLocal), Reason = "https://github.com/dotnet/roslyn/issues/28328")]
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
                // (12,26): warning CS0672: Member 'B.M<T>()' overrides obsolete member 'A.M<T>()'. Add the Obsolete attribute to 'B.M<T>()'.
                //     public override void M<T>() { }
                Diagnostic(ErrorCode.WRN_NonObsoleteOverridingObsolete, "M").WithArguments("B.M<T>()", "A.M<T>()"),
                // (19,9): warning CS0612: 'A.M<T>()' is obsolete
                //         b.M<int>();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "b.M<int>()").WithArguments("A.M<T>()"));
        }

        [Fact]
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(580832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/580832")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(531148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531148")]
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

            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(531148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531148")]
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

            CreateCompilation(source).VerifyDiagnostics(
                // (25,17): warning CS0618: 'A<int>.implicit operator int(A<int>)' is obsolete: 'A<T> to T'
                //         int i = ai;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "ai").WithArguments("A<int>.implicit operator int(A<int>)", "A<T> to T"),
                // (26,14): warning CS0618: 'A<int>.implicit operator A<int>(int)' is obsolete: 'T to A<T>'
                //         ai = i;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "i").WithArguments("A<int>.implicit operator A<int>(int)", "T to A<T>"));
        }

        [Fact]
        [WorkItem(531148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531148")]
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

            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(531148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531148")]
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

            CreateCompilation(source).VerifyDiagnostics(
                // (19,14): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //         args[c].ToString();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c").WithArguments("Convertible.implicit operator int(Convertible)", "To int"));
        }

        [Fact]
        [WorkItem(531148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531148")]
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

            CreateCompilation(source).VerifyDiagnostics(
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
        [WorkItem(531148, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531148")]
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

            CreateCompilation(source).VerifyDiagnostics(
                // (18,17): warning CS0618: 'Convertible.implicit operator int(Convertible)' is obsolete: 'To int'
                //         int i = c ?? 1;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "c ?? 1").WithArguments("Convertible.implicit operator int(Convertible)", "To int"));
        }

        [Fact]
        [WorkItem(656345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/656345")]
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
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            // Used to assert because it depended on some lazy state being evaluated but didn't
            // actually trigger evaluation.
            model.GetSymbolInfo(syntax);

            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(656345, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/656345")]
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
            var comp = CreateCompilation(source);
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
        [WorkItem(665595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665595")]
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
            var comp1 = CreateCompilation(source1);
            comp1.VerifyDiagnostics();

            var comp2 = CreateCompilation(source2, new[] { comp1.EmitToImageReference() });

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
        [WorkItem(668365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/668365")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
            var comp = CreateCompilation(source);

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
            var compilation1 = CreateEmptyCompilation(source1, WinRtRefs, TestOptions.ReleaseDll);

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
        Class6 x6 = new Class6();

        object x5;
        x5=x1;
        x5 = x2;
        x5 = x3;
        x5 = x4;
        x5 = x6.P1;
        x6.P1 = 1;
        x5 = x6.P2;
        x6.P2 = 1;
        x6.E1 += null;
        x6.E1 -= null;
    }
}

class Class6
{
    public int P1
    {
        [Deprecated(""P1.get is deprecated."", DeprecationType.Remove, 1)]
        get
        {
            return 1;
        }
        set {}
    }

    public int P2
    {
        get
        {
            return 1;
        }
        [Deprecated(""P1.get is deprecated."", DeprecationType.Remove, 1)]
        set {}
    }

    public event System.Action E1
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
            var compilation2 = CreateEmptyCompilation(source2, WinRtRefs.Concat(new[] { new CSharpCompilationReference(compilation1) }), TestOptions.ReleaseDll);

            var expected = new[] {
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
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Class4").WithArguments("Class4", "Class4 is deprecated.").WithLocation(11, 9),
                // (19,14): error CS0619: 'Class6.P1.get' is obsolete: 'P1.get is deprecated.'
                //         x5 = x6.P1;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "x6.P1").WithArguments("Class6.P1.get", "P1.get is deprecated.").WithLocation(19, 14),
                // (22,9): error CS0619: 'Class6.P2.set' is obsolete: 'P1.get is deprecated.'
                //         x6.P2 = 1;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "x6.P2").WithArguments("Class6.P2.set", "P1.get is deprecated.").WithLocation(22, 9),
                // (52,10): error CS8423: Attribute 'Windows.Foundation.Metadata.DeprecatedAttribute' is not valid on event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Deprecated("E1.add is deprecated.", DeprecationType.Remove, 1)]
                Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Deprecated").WithArguments("Windows.Foundation.Metadata.DeprecatedAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(52, 10)
                                 };

            compilation2.VerifyDiagnostics(expected);

            compilation2 = CreateEmptyCompilation(source2, WinRtRefs.Concat(new[] { compilation1.EmitToImageReference() }), TestOptions.ReleaseDll);
            compilation2.VerifyDiagnostics(expected);
        }

        /// <summary>
        /// Report warning or error based on last attribute.
        /// </summary>
        [WorkItem(18755, "https://github.com/dotnet/roslyn/issues/18755")]
        [Fact]
        public void TestMultipleDeprecatedAttributes()
        {
            var source =
@"using Windows.Foundation.Metadata;
class C
{
    [Deprecated(""Removed"", DeprecationType.Remove, 0)]
    [Deprecated(""Deprecated"", DeprecationType.Deprecate, 0)]
    static void F() { }
    [Deprecated(""Deprecated"", DeprecationType.Deprecate, 0)]
    [Deprecated(""Removed"", DeprecationType.Remove, 0)]
    static void G() { }
    static void Main()
    {
        F();
        G();
    }
}";
            var compilation = CreateEmptyCompilation(source, WinRtRefs, TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (12,9): warning CS0618: 'C.F()' is obsolete: 'Deprecated'
                //         F();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "F()").WithArguments("C.F()", "Deprecated").WithLocation(12, 9),
                // (13,9): error CS0619: 'C.G()' is obsolete: 'Removed'
                //         G();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "G()").WithArguments("C.G()", "Removed").WithLocation(13, 9));
        }

        private const string DeprecatedAttributeSourceTH1 =
@"using System;

namespace Windows.Foundation.Metadata
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }

        // this signature is only used in TH1 metadata
        // see: https://github.com/dotnet/roslyn/issues/10630
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version, Type contract)
        {
        }
    }

    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}";

        [Fact]
        public void TestDeprecatedAttributeTH1()
        {
            var source1 = @"
using Windows.Foundation.Metadata;

public class Test
{
        [Deprecated(""hello"", DeprecationType.Deprecate, 1, typeof(int))]
        public static void Goo()
        {
        }

        [Deprecated(""hi"", DeprecationType.Deprecate, 1)]
        public static void Bar()
        {
        }
}
";
            var compilation1 = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(DeprecatedAttributeSourceTH1), Parse(source1) });

            var source2 = @"
namespace ConsoleApplication74
{
    class Program
    {
        static void Main(string[] args)
        {
            Test.Goo();
            Test.Bar();
        }
    }
}
";
            var compilation2 = CreateCompilationWithMscorlib40AndSystemCore(source2, new[] { compilation1.EmitToImageReference() });

            compilation2.VerifyDiagnostics(
    // (8,13): warning CS0618: 'Test.Goo()' is obsolete: 'hello'
    //             Test.Goo();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Goo()").WithArguments("Test.Goo()", "hello").WithLocation(8, 13),
    // (9,13): warning CS0618: 'Test.Bar()' is obsolete: 'hi'
    //             Test.Bar();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Bar()").WithArguments("Test.Bar()", "hi").WithLocation(9, 13)
);

            var compilation3 = CreateCompilationWithMscorlib40AndSystemCore(source2, new[] { new CSharpCompilationReference(compilation1) });

            compilation3.VerifyDiagnostics(
    // (8,13): warning CS0618: 'Test.Goo()' is obsolete: 'hello'
    //             Test.Goo();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Goo()").WithArguments("Test.Goo()", "hello").WithLocation(8, 13),
    // (9,13): warning CS0618: 'Test.Bar()' is obsolete: 'hi'
    //             Test.Bar();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Bar()").WithArguments("Test.Bar()", "hi").WithLocation(9, 13)
);
        }

        private const string DeprecatedAttributeSourceTH2 =
@"using System;

namespace Windows.Foundation.Metadata
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }

        // this signature is only used in TH2 metadata and onwards
        // see: https://github.com/dotnet/roslyn/issues/10630
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version, String contract)
        {
        }
    }

    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}";

        [Fact]
        public void TestDeprecatedAttributeTH2()
        {
            var source1 = @"
using Windows.Foundation.Metadata;

public class Test
{
        [Deprecated(""hello"", DeprecationType.Deprecate, 1, ""hello"")]
        public static void Goo()
        {
        }

        [Deprecated(""hi"", DeprecationType.Deprecate, 1)]
        public static void Bar()
        {
        }
}
";
            var compilation1 = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(DeprecatedAttributeSourceTH2), Parse(source1) });

            var source2 = @"
namespace ConsoleApplication74
{
    class Program
    {
        static void Main(string[] args)
        {
            Test.Goo();
            Test.Bar();
        }
    }
}
";
            var compilation2 = CreateCompilationWithMscorlib40AndSystemCore(source2, new[] { compilation1.EmitToImageReference() });

            compilation2.VerifyDiagnostics(
    // (8,13): warning CS0618: 'Test.Goo()' is obsolete: 'hello'
    //             Test.Goo();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Goo()").WithArguments("Test.Goo()", "hello").WithLocation(8, 13),
    // (9,13): warning CS0618: 'Test.Bar()' is obsolete: 'hi'
    //             Test.Bar();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Bar()").WithArguments("Test.Bar()", "hi").WithLocation(9, 13)
);

            var compilation3 = CreateCompilationWithMscorlib40AndSystemCore(source2, new[] { new CSharpCompilationReference(compilation1) });

            compilation3.VerifyDiagnostics(
    // (8,13): warning CS0618: 'Test.Goo()' is obsolete: 'hello'
    //             Test.Goo();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Goo()").WithArguments("Test.Goo()", "hello").WithLocation(8, 13),
    // (9,13): warning CS0618: 'Test.Bar()' is obsolete: 'hi'
    //             Test.Bar();
    Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Test.Bar()").WithArguments("Test.Bar()", "hi").WithLocation(9, 13)
);
        }

        [Fact, WorkItem(858839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858839")]
        public void Bug858839_1()
        {
            var source1 = @"
using Windows.Foundation.Metadata;

public class MainPage
{
    public static void Main(string[] args)
    {
    }
    
    private static void TestGoo1(IGoo1 a, ConcreteGoo1 b)
    {
        a.Goo(); // IGoo1
        b.Goo(); // ConcreteGoo1
    }

    private static void TestGoo2(IGoo2 a, ConcreteGoo2 b)
    {
        a.Goo(); // IGoo2
        b.Goo(); // ConcreteGoo2
    }

    private static void TestGoo3(IGoo3 a, ConcreteGoo3 b)
    {
        a.Goo(); // IGoo3
        b.Goo(); // ConcreteGoo3
    }
}

public interface IGoo1
{
    [Deprecated(""IGoo1.Goo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    void Goo();
}

public sealed class ConcreteGoo1 : IGoo1
{
    public void Goo()
    {
    }
}

public interface IGoo2
{
    void Goo();
}

public sealed class ConcreteGoo2 : IGoo2
{
    [Deprecated(""ConcreteGoo2.Goo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    public void Goo()
    {
    }
}

public interface IGoo3
{
    [Deprecated(""IGoo3.Goo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    void Goo();
}

public sealed class ConcreteGoo3 : IGoo3
{
    [Deprecated(""ConcreteGoo3.Goo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    public void Goo()
    {
    }
}

public sealed class ConcreteGoo4 : IGoo1
{
    void IGoo1.Goo()
    {
    }
}

public sealed class ConcreteGoo5 : IGoo1
{
    [Deprecated(""ConcreteGoo5.Goo has been deprecated"", DeprecationType.Deprecate, 0, Platform.Windows)]
    void IGoo1.Goo()
    {
    }
}
";
            var compilation1 = CreateEmptyCompilation(source1, WinRtRefs, TestOptions.ReleaseDll);

            var expected = new[] {
                // (12,9): warning CS0618: 'IGoo1.Goo()' is obsolete: 'IGoo1.Goo has been deprecated'
                //         a.Goo(); // IGoo1
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.Goo()").WithArguments("IGoo1.Goo()", "IGoo1.Goo has been deprecated").WithLocation(12, 9),
                // (19,9): warning CS0618: 'ConcreteGoo2.Goo()' is obsolete: 'ConcreteGoo2.Goo has been deprecated'
                //         b.Goo(); // ConcreteGoo2
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "b.Goo()").WithArguments("ConcreteGoo2.Goo()", "ConcreteGoo2.Goo has been deprecated").WithLocation(19, 9),
                // (24,9): warning CS0618: 'IGoo3.Goo()' is obsolete: 'IGoo3.Goo has been deprecated'
                //         a.Goo(); // IGoo3
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.Goo()").WithArguments("IGoo3.Goo()", "IGoo3.Goo has been deprecated").WithLocation(24, 9),
                // (25,9): warning CS0618: 'ConcreteGoo3.Goo()' is obsolete: 'ConcreteGoo3.Goo has been deprecated'
                //         b.Goo(); // ConcreteGoo3
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "b.Goo()").WithArguments("ConcreteGoo3.Goo()", "ConcreteGoo3.Goo has been deprecated").WithLocation(25, 9)
                                 };

            compilation1.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(858839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858839")]
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
            var compilation1 = CreateEmptyCompilation(source1, WinRtRefs, TestOptions.ReleaseDll);

            //compilation1.VerifyDiagnostics();

            var source2 = @"
using System;

class Test
{
    public static void F(IExceptionalInterface i)
    {
        i.ExceptionalProp = ""goo"";
        Console.WriteLine(i.ExceptionalProp);
        }
    }
";
            var compilation2 = CreateEmptyCompilation(source2, WinRtRefs.Concat(new[] { new CSharpCompilationReference(compilation1) }), TestOptions.ReleaseDll);

            var expected = new[] {
                // (8,9): error CS0619: 'IExceptionalInterface.ExceptionalProp.set' is obsolete: 'Changed my mind; don't put this prop.'
                //         i.ExceptionalProp = "goo";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "i.ExceptionalProp").WithArguments("IExceptionalInterface.ExceptionalProp.set", "Changed my mind; don't put this prop.").WithLocation(8, 9),
                // (9,27): error CS0619: 'IExceptionalInterface.ExceptionalProp.get' is obsolete: 'Actually, don't even use the prop at all.'
                //         Console.WriteLine(i.ExceptionalProp);
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "i.ExceptionalProp").WithArguments("IExceptionalInterface.ExceptionalProp.get", "Actually, don't even use the prop at all.").WithLocation(9, 27)
                                 };

            compilation2.VerifyDiagnostics(expected);
        }

        [Fact, WorkItem(530801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530801")]
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
            var cscomp = CreateEmptyCompilation(cssource, new[] { MscorlibRef, ilReference }, TestOptions.ReleaseExe);

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

        [Fact, WorkItem(530801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530801")]
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
            var cscomp = CreateEmptyCompilation(cssource, new[] { MscorlibRef, ilReference }, TestOptions.ReleaseExe);

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

        #region SkipLocalsInitAttribute

        [Fact]
        public void SkipLocalsInitRequiresUnsafe()
        {
            var source = @"
using System.Runtime.CompilerServices;

[module: SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

[SkipLocalsInitAttribute]
public class C
{
    [SkipLocalsInitAttribute]
    public void M()
    { }

    [SkipLocalsInitAttribute]
    public int P => 0;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,10): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // [module: SkipLocalsInitAttribute]
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "SkipLocalsInitAttribute").WithLocation(4, 10),
                // (13,2): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // [SkipLocalsInitAttribute]
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "SkipLocalsInitAttribute").WithLocation(13, 2),
                // (16,6): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     [SkipLocalsInitAttribute]
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "SkipLocalsInitAttribute").WithLocation(16, 6),
                // (20,6): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     [SkipLocalsInitAttribute]
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "SkipLocalsInitAttribute").WithLocation(20, 6)
                );
        }

        [Fact]
        public void SkipLocalsInitAttributeOnMethod()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M_skip()
    {
        int x = 2;
        x = x + x + x;
    }

    public void M_init()
    {
        int x = 2;
        x = x + x + x;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.True(comp.HasLocalsInit("C.M_init", realIL: true));
            Assert.False(comp.HasLocalsInit("C.M_skip", realIL: true));
            Assert.True(comp.HasLocalsInit("C.M_init", realIL: false));
            Assert.False(comp.HasLocalsInit("C.M_skip", realIL: false));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnPartialMethod()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

partial class C
{
    partial void M()
    {
        int x = 1;
        x = x + x + x;
    }
}

partial class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    partial void M();
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.M"));
        }

        [Fact]
        public void WhenMethodsDifferBySkipLocalsInitAttributeTheyMustHaveDifferentRVA()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public unsafe void M_skip()
    {
        int *ptr = stackalloc int[10];
        System.Console.WriteLine(ptr[0]);
    }

    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public unsafe void M_skip_copy()
    {
        int *ptr = stackalloc int[10];
        System.Console.WriteLine(ptr[0]);
    }

    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public unsafe void M_skip_diff()
    {
        int *ptr = stackalloc int[11];
        System.Console.WriteLine(ptr[0]);
    }

    public unsafe void M_init()
    {
        int *ptr = stackalloc int[10];
        System.Console.WriteLine(ptr[0]);
    }

    public unsafe void M_init_copy()
    {
        int *ptr = stackalloc int[10];
        System.Console.WriteLine(ptr[0]);
    }

    public unsafe void M_init_diff()
    {
        int *ptr = stackalloc int[11];
        System.Console.WriteLine(ptr[0]);
    }
}
";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            var metadata = ModuleMetadata.CreateFromStream(comp.EmitToStream());
            var peReader = metadata.Module.GetMetadataReader();

            TypeDefinition typeC = default;

            foreach (var typeHandle in peReader.TypeDefinitions)
            {
                var type = peReader.GetTypeDefinition(typeHandle);
                var name = peReader.GetString(type.Name);

                if (name == "C")
                {
                    typeC = type;
                    break;
                }
            }

            Assert.NotEqual(typeC, default);

            MethodDefinition methodInit = default;
            MethodDefinition methodSkip = default;
            MethodDefinition methodInitCopy = default;
            MethodDefinition methodSkipCopy = default;
            MethodDefinition methodInitDiff = default;
            MethodDefinition methodSkipDiff = default;

            foreach (var methodHandle in typeC.GetMethods())
            {
                var method = peReader.GetMethodDefinition(methodHandle);
                var name = peReader.GetString(method.Name);

                if (name == "M_init")
                {
                    methodInit = method;
                }
                else if (name == "M_skip")
                {
                    methodSkip = method;
                }
                else if (name == "M_init_copy")
                {
                    methodInitCopy = method;
                }
                else if (name == "M_skip_copy")
                {
                    methodSkipCopy = method;
                }
                else if (name == "M_init_diff")
                {
                    methodInitDiff = method;
                }
                else if (name == "M_skip_diff")
                {
                    methodSkipDiff = method;
                }
            }

            Assert.NotEqual(methodInit, default);
            Assert.NotEqual(methodSkip, default);
            Assert.NotEqual(methodInitCopy, default);
            Assert.NotEqual(methodSkipCopy, default);
            Assert.NotEqual(methodInitDiff, default);
            Assert.NotEqual(methodSkipDiff, default);

            Assert.NotEqual(methodInit.RelativeVirtualAddress, methodSkip.RelativeVirtualAddress);
            Assert.Equal(methodInit.RelativeVirtualAddress, methodInitCopy.RelativeVirtualAddress);
            Assert.Equal(methodSkip.RelativeVirtualAddress, methodSkipCopy.RelativeVirtualAddress);
            Assert.NotEqual(methodInit.RelativeVirtualAddress, methodInitDiff.RelativeVirtualAddress);
            Assert.NotEqual(methodSkip.RelativeVirtualAddress, methodSkipDiff.RelativeVirtualAddress);
        }

        [Fact]
        public void SkipLocalsInitAttributeOnAssemblyDoesNotPropagateToMethod()
        {
            var source = @"
[assembly: System.Runtime.CompilerServices.SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    public void M()
    {
        int x = 2;
        x = x + x + x;
    }
}
";

            var comp = CompileAndVerify(source);

            Assert.True(comp.HasLocalsInit("C.M"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnMethodPropagatesToLocalFunction()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        void F()
        {
            int x = 2;
            x = x + x + x;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<M>g__F|0_0"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnMethodPropagatesToLambda()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        System.Action L = () =>
        {
            int x = 2;
            x = x + x + x;
        };
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<>c.<M>b__0_0"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnMethodPropagatesToNestedLambdaAndLocalFunction()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        void F()
        {
            int y = 1;
            y = y + y + y;

            void FF()
            {
                int x = 2;
                x = x + x + x;
            }

            System.Action FL = () =>
            {
                int x = 3;
                x = x + x + x;
            };
        }

        System.Action L = () =>
        {
            int y = 4;
            y = y + y + y;

            void LF()
            {
                int x = 5;
                x = x + x + x;
            }

            System.Action LL = () =>
            {
                int x = 6;
                x = x + x + x;
            };
        };
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<M>g__F|0_0")); // F
            Assert.False(comp.HasLocalsInit("C.<M>g__FF|0_2")); // FF
            Assert.False(comp.HasLocalsInit("C.<>c.<M>b__0_3")); // FL
            Assert.False(comp.HasLocalsInit("C.<>c.<M>b__0_1")); // L
            Assert.False(comp.HasLocalsInit("C.<M>g__LF|0_4")); // LF
            Assert.False(comp.HasLocalsInit("C.<>c.<M>b__0_5")); // LL
        }

        [Fact]
        public void SkipLocalsInitAttributeOnIteratorPropagatesToItsSynthesizedMethods()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public System.Collections.IEnumerable M_skip()
    {
        yield return 1;
        yield return 2;
    }

    public System.Collections.IEnumerable M_init()
    {
        yield return 3;
        yield return 4;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<M_skip>d__0.System.Collections.IEnumerator.MoveNext"));
            Assert.True(comp.HasLocalsInit("C.<M_init>d__1.System.Collections.IEnumerator.MoveNext"));
            Assert.False(comp.HasLocalsInit("C.<M_skip>d__0.System.Collections.Generic.IEnumerable<object>.GetEnumerator"));
            Assert.True(comp.HasLocalsInit("C.<M_init>d__1.System.Collections.Generic.IEnumerable<object>.GetEnumerator"));

            // The following methods do not contain locals, so the attribute should not alter their behavior

            Assert.Null(comp.HasLocalsInit("C.<M_skip>d__0.System.IDisposable.Dispose"));
            Assert.Null(comp.HasLocalsInit("C.<M_init>d__1.System.IDisposable.Dispose"));
            Assert.Null(comp.HasLocalsInit("C.<M_skip>d__0.System.Collections.IEnumerable.GetEnumerator"));
            Assert.Null(comp.HasLocalsInit("C.<M_init>d__1.System.Collections.IEnumerable.GetEnumerator"));
            Assert.Null(comp.HasLocalsInit("C.<M_skip>d__0.System.Collections.IEnumerator.get_Current"));
            Assert.Null(comp.HasLocalsInit("C.<M_init>d__1.System.Collections.IEnumerator.get_Current"));
            Assert.Null(comp.HasLocalsInit("C.<M_skip>d__0.System.Collections.IEnumerator.Reset"));
            Assert.Null(comp.HasLocalsInit("C.<M_init>d__1.System.Collections.IEnumerator.Reset"));
            Assert.Null(comp.HasLocalsInit("C.<M_skip>d__0.System.Collections.Generic.IEnumerator<object>.get_Current"));
            Assert.Null(comp.HasLocalsInit("C.<M_init>d__1.System.Collections.Generic.IEnumerator<object>.get_Current"));
            Assert.Null(comp.HasLocalsInit("C.<M_skip>d__0..ctor"));
            Assert.Null(comp.HasLocalsInit("C.<M_init>d__1..ctor"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnMethodPropagatesToIteratorLocalFunction()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        System.Collections.IEnumerable F()
        {
            yield return 1;
            yield return 2;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<<M>g__F|0_0>d.System.Collections.IEnumerator.MoveNext"));
            Assert.False(comp.HasLocalsInit("C.<<M>g__F|0_0>d.System.Collections.Generic.IEnumerable<object>.GetEnumerator"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnAsyncPropagatesToItsSynthesizedMethods()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public async Task M_skip()
    {
        await Task.Yield();
    }

    public async Task M_init()
    {
        await Task.Yield();
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<M_skip>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"));
            Assert.True(comp.HasLocalsInit("C.<M_init>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"));

            // The following method does not contain locals, so the attribute should not alter its behavior

            Assert.Null(comp.HasLocalsInit("C.<M_skip>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine"));
            Assert.Null(comp.HasLocalsInit("C.<M_init>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnMethodPropagatesToAsyncLocalFunction()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        async Task F()
        {
            await Task.Yield();
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<<M>g__F|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnMethodPropagatesToAsyncLambda()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        System.Action L = async () =>
        {
            await Task.Yield();
        };
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<>c.<<M>b__0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext"));
        }

        [Fact]
        public void AnonymousTypeTemplateSymbolDelegatesToModuleWhenAskedAboutSkipLocalsInitAttribute()
        {
            var source_init = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        var anon = new { Value = 1 };
    }
}
";

            var source_skip = @"
[module: System.Runtime.CompilerServices.SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    public void M()
    {
        var anon = new { Value = 1 };
    }
}
";

            var comp_init = CompileAndVerify(source_init, options: TestOptions.UnsafeReleaseDll);
            var comp_skip = CompileAndVerify(source_skip, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.Null(comp_init.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.GetHashCode"));
            Assert.Null(comp_init.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>..ctor"));
            Assert.True(comp_init.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.Equals"));
            Assert.True(comp_init.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.ToString"));
            Assert.Null(comp_init.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.Value.get"));

            Assert.Null(comp_skip.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.GetHashCode"));
            Assert.Null(comp_skip.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>..ctor"));
            Assert.False(comp_skip.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.Equals"));
            Assert.False(comp_skip.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.ToString"));
            Assert.Null(comp_skip.HasLocalsInit("<>f__AnonymousType0<<Value>j__TPar>.Value.get"));
        }

        [Fact]
        public void SynthesizedClosureEnvironmentNeverSkipsLocalsInit()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M()
    {
        int x = 1;
        System.Action L = () => x = x + x + x;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll);

            Assert.Null(comp.HasLocalsInit("C.<>c__DisplayClass0_0..ctor"));
        }

        [Fact]
        public void SynthesizedEmbeddedAttributeSymbolDelegatesToModuleWhenAskedAboutSkipLocalsInitAttribute()
        {
            var source_init = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public void M(in int x)
    {
    }
}
";

            var source_skip = @"
[module: System.Runtime.CompilerServices.SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    public void M(in int x)
    {
    }
}
";

            var comp_init = CompileAndVerify(source_init, options: TestOptions.UnsafeReleaseDll);
            var comp_skip = CompileAndVerify(source_skip, options: TestOptions.UnsafeReleaseDll);

            Assert.Null(comp_init.HasLocalsInit("Microsoft.CodeAnalysis.EmbeddedAttribute..ctor"));
            Assert.Null(comp_init.HasLocalsInit("System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor"));

            Assert.Null(comp_skip.HasLocalsInit("Microsoft.CodeAnalysis.EmbeddedAttribute..ctor"));
            Assert.Null(comp_skip.HasLocalsInit("System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor"));
        }

        [Fact]
        public void SourceMemberMethodSymbolDelegatesToTypeWhenSkipLocalsInitAttributeIsNotFound()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C_init
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public void M()
    {
        int x = 1;
        x = x + x + x;
    }
}

[System.Runtime.CompilerServices.SkipLocalsInitAttribute]
public class C_skip
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public void M()
    {
        int x = 1;
        x = x + x + x;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.True(comp.HasLocalsInit("C_init.M"));
            Assert.False(comp.HasLocalsInit("C_skip.M"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnPropertyPropagatesToBothAccessors()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public int P_skip
    {
        get
        {
            int x = 1;
            return x + x + x;
        }

        set
        {
            int x = 2;
            x = x + x + x;
        }
    }

    public int P_init
    {
        get
        {
            int x = 3;
            return x + x + x;
        }

        set
        {
            int x = 4;
            x = x + x + x;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.P_skip.get"));
            Assert.True(comp.HasLocalsInit("C.P_init.get"));
            Assert.False(comp.HasLocalsInit("C.P_skip.set"));
            Assert.True(comp.HasLocalsInit("C.P_init.set"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnAccessor()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    public int P1
    {
        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        get
        {
            int x = 1;
            return x + x + x;
        }

        set
        {
            int x = 2;
            x = x + x + x;
        }
    }

    public int P2
    {
        get
        {
            int x = 3;
            return x + x + x;
        }

        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        set
        {
            int x = 4;
            x = x + x + x;
        }
    }

    public int P3
    {
        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        get
        {
            int x = 5;
            return x + x + x;
        }

        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        set
        {
            int x = 6;
            x = x + x + x;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.P1.get"));
            Assert.True(comp.HasLocalsInit("C.P1.set"));
            Assert.True(comp.HasLocalsInit("C.P2.get"));
            Assert.False(comp.HasLocalsInit("C.P2.set"));
            Assert.False(comp.HasLocalsInit("C.P3.get"));
            Assert.False(comp.HasLocalsInit("C.P3.set"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnIteratorGetAccessor()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    public System.Collections.IEnumerable P
    {
        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        get
        {
            yield return 1;
            yield return 2;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.<get_P>d__1.System.Collections.IEnumerator.MoveNext"));
            Assert.False(comp.HasLocalsInit("C.<get_P>d__1.System.Collections.Generic.IEnumerable<object>.GetEnumerator"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnPropertyAndAccessor()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public int P1
    {
        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        get
        {
            int x = 1;
            return x + x + x;
        }

        set
        {
            int x = 2;
            x = x + x + x;
        }
    }

    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public int P2
    {
        get
        {
            int x = 3;
            return x + x + x;
        }

        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        set
        {
            int x = 4;
            x = x + x + x;
        }
    }

    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public int P3
    {
        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        get
        {
            int x = 5;
            return x + x + x;
        }

        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        set
        {
            int x = 6;
            x = x + x + x;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.P1.get"));
            Assert.False(comp.HasLocalsInit("C.P1.set"));
            Assert.False(comp.HasLocalsInit("C.P2.get"));
            Assert.False(comp.HasLocalsInit("C.P2.set"));
            Assert.False(comp.HasLocalsInit("C.P3.get"));
            Assert.False(comp.HasLocalsInit("C.P3.set"));
        }

        [Fact]
        public void SourcePropertySymbolDelegatesToTypeWhenSkipLocalsInitAttributeIsNotFound()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C_init
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public int P
    {
        get
        {
            int x = 1;
            return x + x + x;
        }

        set
        {
            int x = 2;
            x = x + x + x;
        }
    }
}

[System.Runtime.CompilerServices.SkipLocalsInitAttribute]
public class C_skip
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public int P
    {
        get
        {
            int x = 3;
            return x + x + x;
        }

        set
        {
            int x = 4;
            x = x + x + x;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.True(comp.HasLocalsInit("C_init.P.get"));
            Assert.False(comp.HasLocalsInit("C_skip.P.get"));
            Assert.True(comp.HasLocalsInit("C_init.P.set"));
            Assert.False(comp.HasLocalsInit("C_skip.P.set"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnAutoProperty()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public int P
    {
        get; set;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll);

            // No locals are expected. We are just making sure it still works.

            Assert.Null(comp.HasLocalsInit("C.P.get"));
            Assert.Null(comp.HasLocalsInit("C.P.set"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnExpressionBodiedProperty()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

public class C
{
    int p;
    int p2;
    int p3;

    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public int P
    {
        get => p;
        set => p = value;
    }

    public int P2
    {
        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        get => p2;

        [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
        set => p2 = value;
    }

    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    public int P3 => p3;
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll);

            // No locals are expected. We are just making sure it still works.

            Assert.Null(comp.HasLocalsInit("C.P.get"));
            Assert.Null(comp.HasLocalsInit("C.P.set"));
            Assert.Null(comp.HasLocalsInit("C.P2.get"));
            Assert.Null(comp.HasLocalsInit("C.P2.set"));
            Assert.Null(comp.HasLocalsInit("C.P3.get"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnClassPropagatesToItsMembers()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

[System.Runtime.CompilerServices.SkipLocalsInitAttribute]
class C_skip
{
    int P
    {
        get
        {
            int x = 1;
            return x + x + x;
        }

        set
        {
            int x = 2;
            x = x + x + x;
        }
    }

    void M()
    {
        int x = 3;
        x = x + x + x;
    }

    class C2
    {
        void M2()
        {
            int x = 4;
            x = x + x + x;
        }
    }

    event System.EventHandler E
    {
        add
        {
            int x = 4;
            x = x + x + x;
        }
        remove
        {
            int x = 4;
            x = x + x + x;
        }
    }
}

class C_init
{
    int P
    {
        get
        {
            int x = 1;
            return x + x + x;
        }

        set
        {
            int x = 2;
            x = x + x + x;
        }
    }

    void M()
    {
        int x = 3;
        x = x + x + x;
    }

    class C2
    {
        void M2()
        {
            int x = 4;
            x = x + x + x;
        }
    }

    event System.EventHandler E
    {
        add
        {
            int x = 4;
            x = x + x + x;
        }
        remove
        {
            int x = 4;
            x = x + x + x;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.True(comp.HasLocalsInit("C_init.P.get"));
            Assert.False(comp.HasLocalsInit("C_skip.P.get"));
            Assert.True(comp.HasLocalsInit("C_init.P.set"));
            Assert.False(comp.HasLocalsInit("C_skip.P.set"));
            Assert.True(comp.HasLocalsInit("C_init.M"));
            Assert.False(comp.HasLocalsInit("C_skip.M"));
            Assert.True(comp.HasLocalsInit("C_init.C2.M2"));
            Assert.False(comp.HasLocalsInit("C_skip.C2.M2"));
            Assert.True(comp.HasLocalsInit("C_init.E.add"));
            Assert.True(comp.HasLocalsInit("C_init.E.remove"));
            Assert.False(comp.HasLocalsInit("C_skip.E.add"));
            Assert.False(comp.HasLocalsInit("C_skip.E.remove"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnClassKeepsPropagatingToNestedClasses()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

[System.Runtime.CompilerServices.SkipLocalsInitAttribute]
class C
{
    class C2
    {
        void M2()
        {
            int x = 2;
            x = x + x + x;
        }

        class C3
        {
            void M3()
            {
                int x = 3;
                x = x + x + x;
            }
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.C2.M2"));
            Assert.False(comp.HasLocalsInit("C.C2.C3.M3"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnNestedClassPropagatesToItsMembers()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

class C
{
    [System.Runtime.CompilerServices.SkipLocalsInitAttribute]
    class C2
    {
        int P2
        {
            get
            {
                int x = 1;
                return x + x + x;
            }

            set
            {
                int x = 2;
                x = x + x + x;
            }
        }

        void M2()
        {
            int x = 3;
            x = x + x + x;
        }

        class C3
        {
            void M3()
            {
                int x = 4;
                x = x + x + x;
            }
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.C2.P2.get"));
            Assert.False(comp.HasLocalsInit("C.C2.P2.set"));
            Assert.False(comp.HasLocalsInit("C.C2.M2"));
            Assert.False(comp.HasLocalsInit("C.C2.C3.M3"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnPartialClassPropagatesToItsMembers()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

partial class C
{
    int P
    {
        get
        {
            int x = 1;
            return x = x + x + x;
        }

        set
        {
            int x = 2;
            x = x + x + x;
        }
    }

    void M()
    {
        int x = 3;
        x = x + x + x;
    }

    class C2
    {
        void M2()
        {
            int x = 4;
            x = x + x + x;
        }
    }
}

[System.Runtime.CompilerServices.SkipLocalsInitAttribute]
partial class C
{
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.P.get"));
            Assert.False(comp.HasLocalsInit("C.P.set"));
            Assert.False(comp.HasLocalsInit("C.M"));
            Assert.False(comp.HasLocalsInit("C.C2.M2"));
        }

        [Fact]
        public void SourceNamedTypeSymbolDelegatesToContainingTypeWhenSkipLocalsInitAttributeIsNotFound()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

class C_init
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    class C
    {
        void M()
        {
            int x = 1;
            x = x + x + x;
        }
    }
}

[System.Runtime.CompilerServices.SkipLocalsInitAttribute]
public class C_skip
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    class C
    {
        void M()
        {
            int x = 1;
            x = x + x + x;
        }
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.True(comp.HasLocalsInit("C_init.C.M"));
            Assert.False(comp.HasLocalsInit("C_skip.C.M"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnModule()
        {
            var source = @"
[module: System.Runtime.CompilerServices.SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

class C
{
    void M()
    {
        int x = 1;
        x = x + x + x;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.M"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnExeModule()
        {
            var source = @"
[module: System.Runtime.CompilerServices.SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

class C
{
    public static void Main()
    {
        int x = 1;
        x = x + x + x;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.UnsafeDebugExe, verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.Main"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnNetmodule()
        {
            var source = @"
[module: System.Runtime.CompilerServices.SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

class C
{
    void M()
    {
        int x = 1;
        x = x + x + x;
    }
}
";

            var comp = CompileAndVerify(source, options: TestOptions.DebugModule.WithAllowUnsafe(true), verify: Verification.Fails);

            Assert.False(comp.HasLocalsInit("C.M"));
        }

        [Fact]
        public void SkipLocalsInitAttributeOnModuleAsReferenceDoesNotAlterBehavior()
        {
            var metadata_source = @"
[module: System.Runtime.CompilerServices.SkipLocalsInitAttribute]

namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}
";

            var source = @"
class C
{
    void M()
    {
        int x = 1;
        x = x + x + x;
    }
}
";

            var metadata_comp = CreateCompilation(metadata_source, options: TestOptions.DebugModule.WithAllowUnsafe(true));
            var comp = CompileAndVerify(source, references: new[] { metadata_comp.EmitToImageReference() });

            Assert.True(comp.HasLocalsInit("C.M"));
        }

        [Fact]
        public void SkipLocalsInitOnEventAccessors()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    class SkipLocalsInitAttribute : System.Attribute
    {
    }
}

class C
{
    [System.Runtime.CompilerServices.SkipLocalsInit]
    event System.EventHandler E
    {
        add
        {
            int x = 1;
            x += x + 1;
        }
        remove
        {
            int x = 1;
            x += x + 1;
        }
    }

    event System.EventHandler E2
    {
        [System.Runtime.CompilerServices.SkipLocalsInit]
        add
        {
            int x = 1;
            x += x + 1;
        }
        remove
        {
            int x = 1;
            x += x + 1;
        }
    }

}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            var verifier = CompileAndVerify(comp, verify: Verification.Fails);
            const string il = @"
{
  // Code size       10 (0xa)
  .maxstack  3
  .locals (int V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  add
  IL_0008:  stloc.0
  IL_0009:  ret
}";
            verifier.VerifyIL("C.E.add", il);
            verifier.VerifyIL("C.E.remove", il);
            verifier.VerifyIL("C.E2.add", il);
            verifier.VerifyIL("C.E2.remove", il.Replace(".locals", ".locals init"));
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

        [Fact, WorkItem(10639, "https://github.com/dotnet/roslyn/issues/10639")]
        public void UsingStaticDirectiveDoesNotIgnoreObsoleteAttribute_DifferentSeverity()
        {
            var source = @"
using System;
using static TestError;
using static TestWarning;

[Obsolete (""Broken Error Class"", true)]
static class TestError
{
    public static void TestErrorFunc()
    {

    }
}

[Obsolete (""Broken Warning Class"", false)]
static class TestWarning
{
    public static void TestWarningFunc()
    {

    }
}

class Test
{
    public static void Main()
    {
        TestErrorFunc();
        TestWarningFunc();
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (3,14): error CS0619: 'TestError' is obsolete: 'Broken Error Class'
                // using static TestError;
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "TestError").WithArguments("TestError", "Broken Error Class").WithLocation(3, 14),
                // (4,14): warning CS0618: 'TestWarning' is obsolete: 'Broken Warning Class'
                // using static TestWarning;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "TestWarning").WithArguments("TestWarning", "Broken Warning Class").WithLocation(4, 14));
        }

        [Fact, WorkItem(10639, "https://github.com/dotnet/roslyn/issues/10639")]
        public void UsingStaticDirectiveDoesNotIgnoreObsoleteAttribute_NestedClasses()
        {
            var source = @"
using System;
using static ActiveParent.ObsoleteChild;
using static ObsoleteParent.ActiveChild;
using static BothObsoleteParent.BothObsoleteChild;

static class ActiveParent
{
    [Obsolete]
    public static class ObsoleteChild
    {
        public static void ObsoleteChildFunc()
        {

        }
    }
}

[Obsolete]
static class ObsoleteParent
{
    public static class ActiveChild
    {
        public static void ActiveChildFunc()
        {

        }
    }
}

[Obsolete]
static class BothObsoleteParent
{
    [Obsolete]
    public static class BothObsoleteChild
    {
        public static void BothObsoleteFunc()
        {

        }
    }
}

class Test
{
    public static void Main()
    {
        ObsoleteChildFunc();
        ActiveChildFunc();
        BothObsoleteFunc();
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // (3,14): warning CS0612: 'ActiveParent.ObsoleteChild' is obsolete
                // using static ActiveParent.ObsoleteChild;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "ActiveParent.ObsoleteChild").WithArguments("ActiveParent.ObsoleteChild").WithLocation(3, 14),
                // (4,14): warning CS0612: 'ObsoleteParent' is obsolete
                // using static ObsoleteParent.ActiveChild;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "ObsoleteParent").WithArguments("ObsoleteParent").WithLocation(4, 14),
                // (5,14): warning CS0612: 'BothObsoleteParent' is obsolete
                // using static BothObsoleteParent.BothObsoleteChild;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "BothObsoleteParent").WithArguments("BothObsoleteParent").WithLocation(5, 14),
                // (5,14): warning CS0612: 'BothObsoleteParent.BothObsoleteChild' is obsolete
                // using static BothObsoleteParent.BothObsoleteChild;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "BothObsoleteParent.BothObsoleteChild").WithArguments("BothObsoleteParent.BothObsoleteChild").WithLocation(5, 14));
        }

        [Fact, WorkItem(19394, "https://github.com/dotnet/roslyn/issues/19394")]
        public void WellKnownTypeAsStruct_DefaultConstructor_DynamicAttribute()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public struct DynamicAttribute
    {
        public DynamicAttribute(bool[] transformFlags)
        {
        }
    }
}
class T
{
    void M(dynamic x) {}
}";

            CreateCompilation(code).VerifyDiagnostics().VerifyEmitDiagnostics(
                // error CS0616: 'System.Runtime.CompilerServices.DynamicAttribute' is not an attribute class
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass).WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(1, 1));
        }

        [Fact, WorkItem(19394, "https://github.com/dotnet/roslyn/issues/19394")]
        public void WellKnownTypeAsStruct_NonDefaultConstructor_DynamicAttribute_Array()
        {
            var compilation = CreateCompilationWithCSharp(@"
using System;
namespace System.Runtime.CompilerServices
{
    public struct DynamicAttribute
    {
        public DynamicAttribute(bool[] transformFlags)
        {
        }
    }
}
public class Program
{
    public static void Test(dynamic[] x)
    {
        Console.WriteLine(x.Length);
        foreach (var y in x)
        {
            Console.WriteLine(y);
        }
    }
    public static void Main()
    {
        Test(new dynamic[] { ""first"", ""second"" });
    }
}", options: TestOptions.ReleaseExe);

            CompileAndVerify(
                compilation,
                expectedOutput: @"
2
first
second",
                symbolValidator: module =>
                {
                    var attribute = module.ContainingAssembly.GetTypeByMetadataName("Program").GetMethod("Test").Parameters.Single().GetAttributes().Single();

                    Assert.Equal("System.Runtime.CompilerServices.DynamicAttribute", attribute.AttributeClass.ToTestDisplayString());
                    Assert.True(attribute.AttributeClass.IsStructType());
                    Assert.Equal(module.ContainingAssembly, attribute.AttributeClass.ContainingAssembly);
                    Assert.Equal("transformFlags", attribute.AttributeConstructor.Parameters.Single().Name);
                });
        }

        [Fact, WorkItem(19394, "https://github.com/dotnet/roslyn/issues/19394")]
        public void WellKnownTypeAsStruct_DefaultConstructor_IsReadOnlyAttribute()
        {
            var code = @"
namespace System.Runtime.CompilerServices
{
    public struct IsReadOnlyAttribute
    {
    }
}
class Test
{
    void M(in int x)
    {
    }
}";

            CreateCompilation(code).VerifyDiagnostics().VerifyEmitDiagnostics(
                // error CS0616: 'IsReadOnlyAttribute' is not an attribute class
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass).WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(1, 1));
        }

        [Fact]
        public void TestObsoleteOnPropertyAccessorUsedInNameofAndXmlDocComment()
        {
            var code = @"
using System;
/// <summary>
/// <see cref=""Prop""/>
/// </summary>
class C
{
    const string str = nameof(Prop);
    
    public int Prop { [Obsolete] get; [Obsolete] set; }
}
";

            CreateCompilation(code).VerifyDiagnostics().VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestObsoleteOnPropertyAndAccessors()
        {
            var code = @"
using System;
class C
{
    public void M() => Prop = Prop;

    [Obsolete]
    public int Prop { [Obsolete] get; [Obsolete] set; }
}
";

            CreateCompilation(code).VerifyDiagnostics(
                // (5,24): warning CS0612: 'C.Prop' is obsolete
                //     public void M() => Prop = Prop;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Prop").WithArguments("C.Prop").WithLocation(5, 24),
                // (5,24): warning CS0612: 'C.Prop.set' is obsolete
                //     public void M() => Prop = Prop;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Prop").WithArguments("C.Prop.set").WithLocation(5, 24),
                // (5,31): warning CS0612: 'C.Prop' is obsolete
                //     public void M() => Prop = Prop;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Prop").WithArguments("C.Prop").WithLocation(5, 31),
                // (5,31): warning CS0612: 'C.Prop.get' is obsolete
                //     public void M() => Prop = Prop;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Prop").WithArguments("C.Prop.get").WithLocation(5, 31));
        }

        [Fact]
        public void TestObsoleteOnPropertyAccessorCSharp7()
        {
            var code = @"
using System;
class C
{
    public int Prop { [Obsolete] get; set; }
}
";

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3)).VerifyDiagnostics(
                // (4,24): error CS8652: The feature 'obsolete on property accessor' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public int Prop { [Obsolete] get; set; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "Obsolete").WithArguments("obsolete on property accessor", "8.0").WithLocation(5, 24));
        }

        [Fact]
        public void TestDeprecatedOnPropertyAccessorCSharp7()
        {
            var code = @"
using Windows.Foundation.Metadata;
class C
{
    public int Prop { [Deprecated(""don't use this"", DeprecationType.Remove, 50331648u)] get; set; }
}
";

            CreateEmptyCompilation(code, references: WinRtRefs, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3)).VerifyDiagnostics(
                // (5,24): error CS8652: The feature 'obsolete on property accessor' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public int Prop { [Deprecated("don't use this", DeprecationType.Remove, 50331648u)] get; set; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, @"Deprecated(""don't use this"", DeprecationType.Remove, 50331648u)").WithArguments("obsolete on property accessor", "8.0").WithLocation(5, 24));
        }

        [Fact]
        public void TestObsoleteOnEventAccessorCSharp7()
        {
            var code = @"
using System;
class C
{
        public event System.Action E
    {
        [Obsolete]
        add
        {
        }
        remove
        {
        }
    }
}
";

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3)).VerifyDiagnostics(
                // (7,10): error CS8423: Attribute 'System.ObsoleteAttribute' is not valid on event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Obsolete]
                Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Obsolete").WithArguments("System.ObsoleteAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(7, 10));
        }

        [Fact]
        public void TestDeprecatedOnEventAccessorCSharp7()
        {
            var code = @"
using Windows.Foundation.Metadata;
class C
{
    public event System.Action E
    {
        [Deprecated(""don't use this"", DeprecationType.Remove, 50331648u)]
        add
        {
        }
        remove
        {
        }
    }
}
";

            CreateEmptyCompilation(code, references: WinRtRefs, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3)).VerifyDiagnostics(
                // (7,10): error CS8423: Attribute 'Windows.Foundation.Metadata.DeprecatedAttribute' is not valid on event accessors. It is only valid on 'class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate' declarations.
                //         [Deprecated("don't use this", DeprecationType.Remove, 50331648u)]
                Diagnostic(ErrorCode.ERR_AttributeNotOnEventAccessor, "Deprecated").WithArguments("Windows.Foundation.Metadata.DeprecatedAttribute", "class, struct, enum, constructor, method, property, indexer, field, event, interface, delegate").WithLocation(7, 10));
        }
    }
}
