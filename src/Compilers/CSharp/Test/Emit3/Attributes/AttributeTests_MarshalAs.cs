// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_MarshalAs : WellKnownAttributesTestBase
    {
        #region Helpers

        private void VerifyFieldMetadataDecoding(CompilationVerifier verifier, Dictionary<string, byte[]> blobs)
        {
            int count = 0;
            using (var assembly = AssemblyMetadata.CreateFromImage(verifier.EmittedAssemblyData))
            {
                var compilation = CreateEmptyCompilation(new SyntaxTree[0], new[] { assembly.GetReference() },
                    options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

                foreach (NamedTypeSymbol type in compilation.GlobalNamespace.GetMembers().Where(s => s.Kind == SymbolKind.NamedType))
                {
                    var fields = type.GetMembers().Where(s => s.Kind == SymbolKind.Field);
                    foreach (FieldSymbol field in fields)
                    {
                        Assert.Null(field.MarshallingInformation);
                        var blob = blobs[field.Name];
                        if (blob != null && blob[0] <= 0x50)
                        {
                            Assert.Equal((UnmanagedType)blob[0], field.MarshallingType);
                        }
                        else
                        {
                            Assert.Equal((UnmanagedType)0, field.MarshallingType);
                        }

                        count++;
                    }
                }
            }

            Assert.True(count > 0, "Expected at least one parameter");
        }

        private void VerifyParameterMetadataDecoding(CompilationVerifier verifier, Dictionary<string, byte[]> blobs)
        {
            int count = 0;
            using (var assembly = AssemblyMetadata.CreateFromImage(verifier.EmittedAssemblyData))
            {
                var compilation = CreateEmptyCompilation(
                    new SyntaxTree[0],
                    new[] { assembly.GetReference() },
                    options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

                foreach (NamedTypeSymbol type in compilation.GlobalNamespace.GetMembers().Where(s => s.Kind == SymbolKind.NamedType))
                {
                    var methods = type.GetMembers().Where(s => s.Kind == SymbolKind.Method);
                    foreach (MethodSymbol method in methods)
                    {
                        foreach (ParameterSymbol parameter in method.Parameters)
                        {
                            Assert.Null(parameter.MarshallingInformation);
                            var blob = blobs[method.Name + ":" + parameter.Name];
                            if (blob != null && blob[0] <= 0x50)
                            {
                                Assert.Equal((UnmanagedType)blob[0], parameter.MarshallingType);
                            }
                            else
                            {
                                Assert.Equal((UnmanagedType)0, parameter.MarshallingType);
                            }

                            count++;
                        }
                    }
                }
            }

            Assert.True(count > 0, "Expected at least one parameter");
        }

        #endregion

        #region Fields

        /// <summary>
        /// type only, others ignored, field type ignored
        /// </summary>
        [Fact]
        public void SimpleTypes()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs((short)0)]
    public X ZeroShort;

    [MarshalAs((UnmanagedType)0)]
    public X Zero;

    [MarshalAs((UnmanagedType)0x1FFFFFFF)]
    public X MaxValue;

    [MarshalAs((UnmanagedType)(0x123456))]
    public X _0x123456;

    [MarshalAs((UnmanagedType)(0x1000))]
    public X _0x1000;

    [MarshalAs(UnmanagedType.AnsiBStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X AnsiBStr;

    [MarshalAs(UnmanagedType.AsAny, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public double AsAny;

    [MarshalAs(UnmanagedType.Bool, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X Bool;

    [MarshalAs(UnmanagedType.BStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X BStr;

    [MarshalAs(UnmanagedType.Currency, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int Currency;

    [MarshalAs(UnmanagedType.Error, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int Error;

    [MarshalAs(UnmanagedType.FunctionPtr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int FunctionPtr;

    [MarshalAs(UnmanagedType.I1, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int I1;

    [MarshalAs(UnmanagedType.I2, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int I2;

    [MarshalAs(UnmanagedType.I4, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int I4;

    [MarshalAs(UnmanagedType.I8, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int I8;

    [MarshalAs(UnmanagedType.LPStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int LPStr;

    [MarshalAs(UnmanagedType.LPStruct, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int LPStruct;

    [MarshalAs(UnmanagedType.LPTStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int LPTStr;

    [MarshalAs(UnmanagedType.LPWStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int LPWStr;

    [MarshalAs(UnmanagedType.R4, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int R4;

    [MarshalAs(UnmanagedType.R8, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int R8;

    [MarshalAs(UnmanagedType.Struct, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int Struct;

    [MarshalAs(UnmanagedType.SysInt, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public decimal SysInt;

    [MarshalAs(UnmanagedType.SysUInt, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int[] SysUInt;

    [MarshalAs(UnmanagedType.TBStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public object[] TBStr;

    [MarshalAs(UnmanagedType.U1, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int U1;

    [MarshalAs(UnmanagedType.U2, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public double U2;

    [MarshalAs(UnmanagedType.U4, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public bool U4;

    [MarshalAs(UnmanagedType.U8, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public string U8;

    [MarshalAs(UnmanagedType.VariantBool, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int VariantBool;   
}";
            var blobs = new Dictionary<string, byte[]>
            {
                { "ZeroShort",    new byte[] { 0x00 } },
                { "Zero",         new byte[] { 0x00 } },
                { "MaxValue",     new byte[] { 0xdf, 0xff, 0xff, 0xff } },
                { "_0x1000",      new byte[] { 0x90, 0x00 } },
                { "_0x123456",    new byte[] { 0xC0, 0x12, 0x34, 0x56 } },
                { "AnsiBStr",     new byte[] { 0x23 } },
                { "AsAny",        new byte[] { 0x28 } },
                { "Bool",         new byte[] { 0x02 } },
                { "BStr",         new byte[] { 0x13 } },
                { "Currency",     new byte[] { 0x0f } },
                { "Error",        new byte[] { 0x2d } },
                { "FunctionPtr",  new byte[] { 0x26 } },
                { "I1",           new byte[] { 0x03 } },
                { "I2",           new byte[] { 0x05 } },
                { "I4",           new byte[] { 0x07 } },
                { "I8",           new byte[] { 0x09 } },
                { "LPStr",        new byte[] { 0x14 } },
                { "LPStruct",     new byte[] { 0x2b } },
                { "LPTStr",       new byte[] { 0x16 } },
                { "LPWStr",       new byte[] { 0x15 } },
                { "R4",           new byte[] { 0x0b } },
                { "R8",           new byte[] { 0x0c } },
                { "Struct",       new byte[] { 0x1b } },
                { "SysInt",       new byte[] { 0x1f } },
                { "SysUInt",      new byte[] { 0x20 } },
                { "TBStr",        new byte[] { 0x24 } },
                { "U1",           new byte[] { 0x04 } },
                { "U2",           new byte[] { 0x06 } },
                { "U4",           new byte[] { 0x08 } },
                { "U8",           new byte[] { 0x0a } },
                { "VariantBool",  new byte[] { 0x25 } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void SimpleTypes_Errors()
        {
            var source = @"
#pragma warning disable 169

using System.Runtime.InteropServices;

class X
{
    [MarshalAs((UnmanagedType)(-1))]
    X MinValue_1;

    [MarshalAs((UnmanagedType)0x20000000)]
    X MaxValue_1;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,16): error CS0591: Invalid value for argument to 'MarshalAs' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(UnmanagedType)(-1)").WithArguments("MarshalAs"),
                // (9,16): error CS0591: Invalid value for argument to 'MarshalAs' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(UnmanagedType)0x20000000").WithArguments("MarshalAs"));
        }

        /// <summary>
        /// (type, IidParamIndex), others ignored, field type ignored
        /// </summary>
        [Fact]
        public void ComInterfaces()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.IDispatch, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public byte IDispatch;

    [MarshalAs(UnmanagedType.Interface, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X Interface;

    [MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 2, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X[] IUnknown;

    [MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0x1FFFFFFF, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int MaxValue;

    [MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0x123456, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int _123456;

    [MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0x1000, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X _0x1000;

    [MarshalAs(UnmanagedType.IDispatch)]
    public int Default;
}
";
            var blobs = new Dictionary<string, byte[]>
            {
                { "IDispatch", new byte[] { 0x1a, 0x00 } },
                { "Interface", new byte[] { 0x1c, 0x01 } },
                { "IUnknown",  new byte[] { 0x19, 0x02 } },
                { "MaxValue",  new byte[] { 0x19, 0xdf, 0xff, 0xff, 0xff } },
                { "_123456",   new byte[] { 0x19, 0xc0, 0x12, 0x34, 0x56 } },
                { "_0x1000",   new byte[] { 0x19, 0x90, 0x00 } },
                { "Default",   new byte[] { 0x1a } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        [WorkItem(22512, "https://github.com/dotnet/roslyn/issues/22512")]
        public void ComInterfacesInProperties()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class X
{
    [field: MarshalAs(UnmanagedType.IDispatch, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public byte IDispatch { get; set; }

    [field: MarshalAs(UnmanagedType.Interface, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X Interface { get; set; }

    [field: MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 2, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X[] IUnknown { get; set; }

    [field: MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0x1FFFFFFF, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int MaxValue { get; set; }

    [field: MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0x123456, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int _123456 { get; set; }

    [field: MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0x1000, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public X _0x1000 { get; set; }

    [field: MarshalAs(UnmanagedType.IDispatch)]
    public int Default { get; set; }
}
";
            var blobs = new Dictionary<string, byte[]>
            {
                { "<IDispatch>k__BackingField", new byte[] { 0x1a, 0x00 } },
                { "<Interface>k__BackingField", new byte[] { 0x1c, 0x01 } },
                { "<IUnknown>k__BackingField",  new byte[] { 0x19, 0x02 } },
                { "<MaxValue>k__BackingField",  new byte[] { 0x19, 0xdf, 0xff, 0xff, 0xff } },
                { "<_123456>k__BackingField",   new byte[] { 0x19, 0xc0, 0x12, 0x34, 0x56 } },
                { "<_0x1000>k__BackingField",   new byte[] { 0x19, 0x90, 0x00 } },
                { "<Default>k__BackingField",   new byte[] { 0x1a } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void ComInterfaces_Errors()
        {
            var source = @"
#pragma warning disable 169

using System.Runtime.InteropServices;

class X
{
    [MarshalAs(UnmanagedType.IDispatch, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType=null, SizeConst=-1, SizeParamIndex=-1)]
    int IDispatch_MinValue_1;

    [MarshalAs(UnmanagedType.Interface, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType=null, SizeConst=-1, SizeParamIndex=-1)]
    int Interface_MinValue_1;

    [MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType=null, SizeConst=-1, SizeParamIndex=-1)]
    int IUnknown_MinValue_1;

    [MarshalAs(UnmanagedType.IUnknown, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = 0x20000000, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArraySubType=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType=null, SizeConst=-1, SizeParamIndex=-1)]
    int IUnknown_MaxValue_1;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,81): error CS0599: Invalid value for argument to 'MarshalAs' attribute
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "IidParameterIndex = -1").WithArguments("IidParameterIndex"),
                // (11,81): error CS0599: Invalid value for argument to 'MarshalAs' attribute
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "IidParameterIndex = -1").WithArguments("IidParameterIndex"),
                // (14,80): error CS0599: Invalid value for argument to 'MarshalAs' attribute
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "IidParameterIndex = -1").WithArguments("IidParameterIndex"),
                // (17,80): error CS0599: Invalid value for argument to 'MarshalAs' attribute
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "IidParameterIndex = 0x20000000").WithArguments("IidParameterIndex"));
        }

        /// <summary>
        /// (ArraySubType, SizeConst, SizeParamIndex), SafeArraySubType not allowed, others ignored
        /// </summary>
        [Fact]
        public void NativeTypeArray()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.LPArray)]
    public int LPArray0;

    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
         SafeArrayUserDefinedSubType = null)]
    public int LPArray1;

    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = 0, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
         SafeArrayUserDefinedSubType = null)]
    public int LPArray2;

    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = 0x1fffffff, SizeParamIndex = short.MaxValue, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
         SafeArrayUserDefinedSubType = null)]
    public int LPArray3;

    // NATIVE_TYPE_MAX = 0x50
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)0x50)]
    public int LPArray4;

    [MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)0x1fffffff)]
    public int LPArray5;

    [MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)0)]
    public int LPArray6;
}
";
            var blobs = new Dictionary<string, byte[]>
            {
                { "LPArray0", new byte[] { 0x2a, 0x50 } },
                { "LPArray1", new byte[] { 0x2a, 0x17 } },
                { "LPArray2", new byte[] { 0x2a, 0x17, 0x00, 0x00, 0x00 } },
                { "LPArray3", new byte[] { 0x2a, 0x17, 0xc0, 0x00, 0x7f, 0xff, 0xdf, 0xff, 0xff, 0xff, 0x01 } },
                { "LPArray4", new byte[] { 0x2a, 0x50 } },
                { "LPArray5", new byte[] { 0x2a, 0xdf, 0xff, 0xff, 0xff } },
                { "LPArray6", new byte[] { 0x2a, 0x00 } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void NativeTypeArray_ElementTypes()
        {
            StringBuilder source = new StringBuilder(@"
using System;
using System.Runtime.InteropServices;

class X
{
");
            var expectedBlobs = new Dictionary<string, byte[]>();

            for (int i = 0; i < sbyte.MaxValue; i++)
            {
                // CustomMarshaler is not allowed
                if (i != (int)UnmanagedType.CustomMarshaler)
                {
                    string fldName = string.Format("_{0:X}", i);
                    source.AppendLine(string.Format("[MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)0x{0:X})]int {1};", i, fldName));
                    expectedBlobs.Add(fldName, new byte[] { 0x2a, (byte)i });
                }
            }

            source.AppendLine("}");

            CompileAndVerifyFieldMarshal(source.ToString(), expectedBlobs);
        }

        [Fact]
        public void NativeTypeArray_Errors()
        {
            var source = @"
#pragma warning disable 169

using System.Runtime.InteropServices;

class X
{
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int LPArray_e0;
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = -1)]                                                                                             int LPArray_e1;
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = 0, SizeParamIndex = -1)]                                                                         int LPArray_e2;
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = int.MaxValue, SizeParamIndex = short.MaxValue)]                                                  int LPArray_e3;
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = int.MaxValue/4 + 1, SizeParamIndex = short.MaxValue)]                                                   int LPArray_e4;
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.CustomMarshaler)]                                                                                                       int LPArray_e5;
    [MarshalAs(UnmanagedType.LPArray, SafeArraySubType=VarEnum.VT_I1)]                                                                                                                     int LPArray_e6;
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)0x20000000)]                                                                                                           int LPArray_e7;
    [MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)(-1))]                                                                                                                 int LPArray_e8;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,79): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int LPArray_e0;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArraySubType = VarEnum.VT_BSTR"),
                // (8,151): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int LPArray_e0;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = -1").WithArguments("SizeConst"),
                // (8,167): error CS0599: Invalid value for named attribute argument 'SizeParamIndex'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int LPArray_e0;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeParamIndex = -1").WithArguments("SizeParamIndex"),
                // (9,79): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = -1)]                                                                                             int LPArray_e1;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = -1").WithArguments("SizeConst"),
                // (10,94): error CS0599: Invalid value for named attribute argument 'SizeParamIndex'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = 0, SizeParamIndex = -1)]                                                                         int LPArray_e2;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeParamIndex = -1").WithArguments("SizeParamIndex"),
                // (11,79): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = int.MaxValue, SizeParamIndex = short.MaxValue)]                                                  int LPArray_e3;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = int.MaxValue").WithArguments("SizeConst"),
                // (12,72): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = int.MaxValue/4 + 1, SizeParamIndex = short.MaxValue)]                                                   int LPArray_e4;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = int.MaxValue/4 + 1").WithArguments("SizeConst"),
                // (13,39): error CS0599: Invalid value for named attribute argument 'ArraySubType'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.CustomMarshaler)]                                                                                                       int LPArray_e5;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "ArraySubType = UnmanagedType.CustomMarshaler").WithArguments("ArraySubType"),
                // (14,39): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.LPArray, SafeArraySubType=VarEnum.VT_I1)]                                                                                                                     int LPArray_e6;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArraySubType=VarEnum.VT_I1"),
                // (15,39): error CS0599: Invalid value for named attribute argument 'ArraySubType'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)0x20000000)]                                                                                                           int LPArray_e7;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "ArraySubType = (UnmanagedType)0x20000000").WithArguments("ArraySubType"),
                // (16,39): error CS0599: Invalid value for named attribute argument 'ArraySubType'
                //     [MarshalAs(UnmanagedType.LPArray, ArraySubType = (UnmanagedType)(-1))]                                                                                                                 int LPArray_e8;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "ArraySubType = (UnmanagedType)(-1)").WithArguments("ArraySubType"));
        }

        /// <summary>
        /// (ArraySubType, SizeConst), (SizeParamIndex, SafeArraySubType) not allowed, others ignored
        /// </summary>
        [Fact]
        public void NativeTypeFixedArray()
        {
            var source = @"
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.ByValArray)]
    public int ByValArray0;

    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
         SafeArrayUserDefinedSubType = null)]
    public int ByValArray1;

    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = 0, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
         SafeArrayUserDefinedSubType = null)]
    public int ByValArray2;

    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = (int.MaxValue - 3) / 4, IidParameterIndex = -1,
        MarshalCookie = null, MarshalType = null, MarshalTypeRef = null, SafeArrayUserDefinedSubType = null)]
    public int ByValArray3;

    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny)]
    public int ByValArray4;

    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.CustomMarshaler)]
    public int ByValArray5;
}
";
            var blobs = new Dictionary<string, byte[]>
            {
                { "ByValArray0", new byte[] { 0x1e, 0x01 } },
                { "ByValArray1", new byte[] { 0x1e, 0x01, 0x17 } },
                { "ByValArray2", new byte[] { 0x1e, 0x00, 0x17 } },
                { "ByValArray3", new byte[] { 0x1e, 0xdf, 0xff, 0xff, 0xff, 0x17} },
                { "ByValArray4", new byte[] { 0x1e, 0x01, 0x28 } },
                { "ByValArray5", new byte[] { 0x1e, 0x01, 0x2c } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            verifier.VerifyDiagnostics(
                // (6,6): warning CS9125: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValArray)]
                Diagnostic(ErrorCode.WRN_ByValArraySizeConstRequired, "MarshalAs(UnmanagedType.ByValArray)").WithLocation(6, 6),
                // (9,6): warning CS9125: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
                Diagnostic(ErrorCode.WRN_ByValArraySizeConstRequired, @"MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
         SafeArrayUserDefinedSubType = null)").WithLocation(9, 6),
                // (21,6): warning CS9125: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny)]
                Diagnostic(ErrorCode.WRN_ByValArraySizeConstRequired, "MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny)").WithLocation(21, 6),
                // (24,6): warning CS9125: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.CustomMarshaler)]
                Diagnostic(ErrorCode.WRN_ByValArraySizeConstRequired, "MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.CustomMarshaler)").WithLocation(24, 6));
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void NativeTypeFixedArray_ElementTypes()
        {
            StringBuilder source = new StringBuilder(@"
using System;
using System.Runtime.InteropServices;

class X
{
");
            var expectedBlobs = new Dictionary<string, byte[]>();

            for (int i = 0; i < sbyte.MaxValue; i++)
            {
                string fldName = string.Format("_{0:X}", i);
                source.AppendLine(string.Format("[MarshalAs(UnmanagedType.ByValArray, ArraySubType = (UnmanagedType)0x{0:X})]int {1};", i, fldName));
                expectedBlobs.Add(fldName, new byte[] { 0x1e, 0x01, (byte)i });
            }

            source.AppendLine("}");

            CompileAndVerifyFieldMarshal(source.ToString(), expectedBlobs);
        }

        [Fact]
        public void NativeTypeFixedArray_Errors()
        {
            var source = @"
#pragma warning disable 169

using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int ByValArray_e1;
    [MarshalAs(UnmanagedType.ByValArray, SizeParamIndex = short.MaxValue)]                                                                                                                    int ByValArray_e2;
    [MarshalAs(UnmanagedType.ByValArray, SafeArraySubType = VarEnum.VT_I2)]                                                                                                                   int ByValArray_e3;
    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = 0x20000000)]                                                                                     int ByValArray_e4;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,82): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int ByValArray_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArraySubType = VarEnum.VT_BSTR"),
                // (8,154):error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int ByValArray_e1;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = -1").WithArguments("SizeConst"),
                // (8,170): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]int ByValArray_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeParamIndex = -1"),
                // (9,6): warning CS9124: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValArray, SizeParamIndex = short.MaxValue)]                                                                                                                    int ByValArray_e2;
                Diagnostic(ErrorCode.WRN_ByValArraySizeConstRequired, "MarshalAs(UnmanagedType.ByValArray, SizeParamIndex = short.MaxValue)"),
                // (9,42): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValArray, SizeParamIndex = short.MaxValue)]                                                                                                                    int ByValArray_e2;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeParamIndex = short.MaxValue"),
                // (10,6): warning CS9124: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValArray, SafeArraySubType = VarEnum.VT_I2)]                                                                                                                   int ByValArray_e3;
                Diagnostic(ErrorCode.WRN_ByValArraySizeConstRequired, "MarshalAs(UnmanagedType.ByValArray, SafeArraySubType = VarEnum.VT_I2)"),
                // (10,42): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValArray, SafeArraySubType = VarEnum.VT_I2)]                                                                                                                   int ByValArray_e3;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArraySubType = VarEnum.VT_I2"),
                // (11,82): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValTStr, SizeConst = 0x20000000)]                                                                                     int ByValArray_e4;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = 0x20000000").WithArguments("SizeConst"));
        }

        [Fact]
        [WorkItem(68988, "https://github.com/dotnet/roslyn/issues/68988")]
        public void NativeTypeFixedArray_SizeConstWarning_RespectsWarningLevel()
        {
            var source = @"
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.ByValArray)]
    public int ByValArray0;
}
";
            CreateCompilation(
                source,
                options: TestOptions.ReleaseDll.WithWarningLevel(7))
                .VerifyDiagnostics();

            CreateCompilation(
                source,
                options: TestOptions.ReleaseDll.WithWarningLevel(8))
                .VerifyDiagnostics(
                // (6,6): warning CS9125: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValArray)]
                Diagnostic(ErrorCode.WRN_ByValArraySizeConstRequired, "MarshalAs(UnmanagedType.ByValArray)").WithLocation(6, 6));
        }

        /// <summary>
        /// (SafeArraySubType, SafeArrayUserDefinedSubType), (ArraySubType, SizeConst, SizeParamIndex) not allowed,
        /// (SafeArraySubType, SafeArrayUserDefinedSubType) not allowed together unless VT_DISPATCH, VT_UNKNOWN, VT_RECORD; others ignored.
        /// </summary>
        [Fact]
        public void NativeTypeSafeArray()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.SafeArray)]
    public int SafeArray0;

    [MarshalAs(UnmanagedType.SafeArray, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR)]
    public int SafeArray1;

    [MarshalAs(UnmanagedType.SafeArray, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArrayUserDefinedSubType = typeof(X))]
    public int SafeArray2;

    [MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType = null)]
    public int SafeArray3;

    [MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType = typeof(void))]
    public int SafeArray4;

    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_EMPTY)]
    public int SafeArray8;

    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD, SafeArrayUserDefinedSubType = typeof(int*[][]))]
    public int SafeArray9;

    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD, SafeArrayUserDefinedSubType = typeof(Nullable<>))]
    public int SafeArray10;
}
";
            var arrayAqn = Encoding.ASCII.GetBytes("System.Int32*[][], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var openGenericAqn = Encoding.ASCII.GetBytes("System.Nullable`1, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            var blobs = new Dictionary<string, byte[]>
            {
                { "SafeArray0", new byte[] { 0x1d } },
                { "SafeArray1", new byte[] { 0x1d, 0x08 } },
                { "SafeArray2", new byte[] { 0x1d } },
                { "SafeArray3", new byte[] { 0x1d } },
                { "SafeArray4", new byte[] { 0x1d } },
                { "SafeArray8", new byte[] { 0x1d, 0x00 } },
                { "SafeArray9", new byte[] { 0x1d, 0x24, (byte)arrayAqn.Length }.Append(arrayAqn) },
                { "SafeArray10", new byte[] { 0x1d, 0x24, (byte)openGenericAqn.Length }.Append(openGenericAqn) },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void NativeTypeSafeArray_CCIOnly()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class C<T> 
{
    public class D<S>
    {
        public class E { }
    }
}

public class X
{
    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD, SafeArrayUserDefinedSubType = typeof(C<int>.D<bool>.E))]
    public int SafeArray11;
}
";
            var nestedAqn = Encoding.ASCII.GetBytes("C`1+D`1+E[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]");

            var blobs = new Dictionary<string, byte[]>
            {
                { "SafeArray11", new byte[] { 0x1d, 0x24, 0x80, 0xc4 }.Append(nestedAqn) },
            };

            // RefEmit has slightly different encoding of the type name
            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void NativeTypeSafeArray_RefEmitDiffers()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType = typeof(List<X>[][]))]
    int SafeArray5;

    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN, SafeArrayUserDefinedSubType = typeof(X))]
    int SafeArray6;

    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_RECORD, SafeArrayUserDefinedSubType = typeof(X))]
    int SafeArray7;
}
";
            var e = Encoding.ASCII;

            var cciBlobs = new Dictionary<string, byte[]>
            {
                { "SafeArray5", new byte[] { 0x1d, 0x09, 0x75 }.Append(e.GetBytes("System.Collections.Generic.List`1[X][][], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")) },
                { "SafeArray6", new byte[] { 0x1d, 0x0d, 0x01, 0x58 } },
                { "SafeArray7", new byte[] { 0x1d, 0x24, 0x01, 0x58 } },
            };

            CompileAndVerifyFieldMarshal(source, cciBlobs);
        }

        [Fact]
        public void NativeTypeSafeArray_Errors()
        {
            var source = @"
#pragma warning disable 169

using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.SafeArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int SafeArray_e1;
    [MarshalAs(UnmanagedType.SafeArray, ArraySubType = UnmanagedType.ByValTStr)]                                                                                                                int SafeArray_e2;
    [MarshalAs(UnmanagedType.SafeArray, SizeConst = 1)]                                                                                                                                         int SafeArray_e3;
    [MarshalAs(UnmanagedType.SafeArray, SizeParamIndex = 1)]                                                                                                                                    int SafeArray_e4;
    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null)]                                                                                int SafeArray_e5;
    [MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType = null, SafeArraySubType = VarEnum.VT_BLOB)]                                                                                int SafeArray_e6;
    [MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType = typeof(int), SafeArraySubType = 0)]                                                                                       int SafeArray_e7;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,41): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int SafeArray_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "ArraySubType = UnmanagedType.ByValTStr"),
                // (8,153): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int SafeArray_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeConst = -1"),
                // (8,169): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int SafeArray_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeParamIndex = -1"),
                // (8,117): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int SafeArray_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType = null"),
                // (9,41): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, ArraySubType = UnmanagedType.ByValTStr)]                                                                                                                int SafeArray_e2;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "ArraySubType = UnmanagedType.ByValTStr"),
                // (10,41): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, SizeConst = 1)]                                                                                                                                         int SafeArray_e3;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeConst = 1"),
                // (11,41): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, SizeParamIndex = 1)]                                                                                                                                    int SafeArray_e4;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeParamIndex = 1"),
                // (12,77): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null)]                                                                                int SafeArray_e5;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType = null"),
                // (13,41): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType = null, SafeArraySubType = VarEnum.VT_BLOB)]                                                                                int SafeArray_e6;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType = null"),
                // (14,41): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType = typeof(int), SafeArraySubType = 0)]                                                                                       int SafeArray_e7;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType = typeof(int)"));
        }

        /// <summary>
        /// (SizeConst - required), (SizeParamIndex, ArraySubType) not allowed
        /// </summary>
        [Fact]
        public void NativeTypeFixedSysString()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
    public int ByValTStr1;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x1fffffff, SafeArrayUserDefinedSubType = typeof(int), IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null)]
    public int ByValTStr2;
}
";
            var blobs = new Dictionary<string, byte[]>
            {
                { "ByValTStr1", new byte[] { 0x17, 0x01 } },
                { "ByValTStr2", new byte[] { 0x17, 0xdf, 0xff, 0xff, 0xff } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void NativeTypeFixedSysString_Errors()
        {
            var source = @"
#pragma warning disable 169

using System;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.ByValTStr, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int ByValTStr_e1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = -1)]                                                                                                                                        int ByValTStr_e2;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Int32.MaxValue / 4 + 1)]                                                                                                                    int ByValTStr_e3;
    [MarshalAs(UnmanagedType.ByValTStr)]                                                                                                                                                        int ByValTStr_e4;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1, SizeParamIndex=1)]                                                                                                                       int ByValTStr_e5;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1, ArraySubType = UnmanagedType.ByValTStr)]                                                                                                 int ByValTStr_e6;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1, SafeArraySubType = VarEnum.VT_BSTR)]                                                                                                     int ByValTStr_e7;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,41): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValTStr, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int ByValTStr_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "ArraySubType = UnmanagedType.ByValTStr"),
                // (9,153): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.ByValTStr, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int ByValTStr_e1;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = -1").WithArguments("SizeConst"),
                // (9,169): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValTStr, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int ByValTStr_e1;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeParamIndex = -1"),
                // (9,6): error CS7046: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValTStr, ArraySubType = UnmanagedType.ByValTStr, SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]   int ByValTStr_e1;
                Diagnostic(ErrorCode.ERR_AttributeParameterRequired1, "MarshalAs").WithArguments("SizeConst"),
                // (10,41): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = -1)]                                                                                                                                        int ByValTStr_e2;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = -1").WithArguments("SizeConst"),
                // (10,6): error CS7046: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = -1)]                                                                                                                                        int ByValTStr_e2;
                Diagnostic(ErrorCode.ERR_AttributeParameterRequired1, "MarshalAs").WithArguments("SizeConst"),
                // (11,41): error CS0599: Invalid value for named attribute argument 'SizeConst'
                //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Int32.MaxValue / 4 + 1)]                                                                                                                    int ByValTStr_e3;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "SizeConst = Int32.MaxValue / 4 + 1").WithArguments("SizeConst"),
                // (12,6): error CS7046: Attribute parameter 'SizeConst' must be specified.
                //     [MarshalAs(UnmanagedType.ByValTStr)]                                                                                                                                                        int ByValTStr_e4;
                Diagnostic(ErrorCode.ERR_AttributeParameterRequired1, "MarshalAs").WithArguments("SizeConst"),
                // (13,56): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1, SizeParamIndex=1)]                                                                                                                       int ByValTStr_e5;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "SizeParamIndex=1"),
                // (14,56): error CS7045: Parameter not valid for the specified unmanaged type.
                //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1, ArraySubType = UnmanagedType.ByValTStr)]                                                                                                 int ByValTStr_e6;
                Diagnostic(ErrorCode.ERR_ParameterNotValidForType, "ArraySubType = UnmanagedType.ByValTStr"));
        }

        /// <summary>
        /// Custom (MarshalType, MarshalTypeRef, MarshalCookie) one of {MarshalType, MarshalTypeRef} required, others ignored
        /// </summary>
        [Fact]
        public void CustomMarshal()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = null)]
    public int CustomMarshaler1;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = null)]
    public int CustomMarshaler2;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""foo"", MarshalTypeRef = typeof(int))]
    public int CustomMarshaler3;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""\u1234f\0oozzz"")]
    public int CustomMarshaler4;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""f\0oozzz"")]
    public int CustomMarshaler5;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"")]
    public int CustomMarshaler6;

    [MarshalAs(UnmanagedType.CustomMarshaler, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
        SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    public int CustomMarshaler7;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(int))]
    public int CustomMarshaler8;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(int), MarshalType = ""foo"", MarshalCookie = ""hello\0world(\u1234)"")]
    public int CustomMarshaler9;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = null, MarshalTypeRef = typeof(int))]
    public int CustomMarshaler10;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""foo"", MarshalTypeRef = null)]
    public int CustomMarshaler11;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = null, MarshalTypeRef = null)]
    public int CustomMarshaler12;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""aaa\0bbb"", MarshalCookie = ""ccc\0ddd"" )]
    public int CustomMarshaler13;

    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""\uD869\uDED6"", MarshalCookie = ""\uD869\uDED6"" )]
    public int CustomMarshaler14;
}
";
            var blobs = new Dictionary<string, byte[]>
            {
                { "CustomMarshaler1",  new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x00 } },
                { "CustomMarshaler2",  new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x00 } },
                { "CustomMarshaler3",  new byte[] { 0x2c, 0x00, 0x00, 0x03, 0x66, 0x6f, 0x6f, 0x00 } },
                { "CustomMarshaler4",  new byte[] { 0x2c, 0x00, 0x00, 0x0a, 0xe1, 0x88, 0xb4, 0x66, 0x00, 0x6f, 0x6f, 0x7a, 0x7a, 0x7a, 0x00 } },
                { "CustomMarshaler5",  new byte[] { 0x2c, 0x00, 0x00, 0x07, 0x66, 0x00, 0x6f, 0x6f, 0x7a, 0x7a, 0x7a, 0x00 } },
                { "CustomMarshaler6",  new byte[] { 0x2c, 0x00, 0x00, 0x60 }.Append(Encoding.UTF8.GetBytes("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\0")) },
                { "CustomMarshaler7",  new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x00 } },
                { "CustomMarshaler8",  new byte[] { 0x2c, 0x00, 0x00, 0x59 }.Append(Encoding.UTF8.GetBytes("System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\0")) },
                { "CustomMarshaler9",  new byte[] { 0x2c, 0x00, 0x00, 0x03, 0x66, 0x6f, 0x6f, 0x10, 0x68, 0x65, 0x6c, 0x6c, 0x6f, 0x00, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0x28, 0xe1, 0x88, 0xb4, 0x29 } },
                { "CustomMarshaler10", new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x00 } },
                { "CustomMarshaler11", new byte[] { 0x2c, 0x00, 0x00, 0x03, 0x66, 0x6f, 0x6f, 0x00 } },
                { "CustomMarshaler12", new byte[] { 0x2c, 0x00, 0x00, 0x00, 0x00 } },
                { "CustomMarshaler13", new byte[] { 0x2c, 0x00, 0x00, 0x07, 0x61, 0x61, 0x61, 0x00, 0x62, 0x62, 0x62, 0x07, 0x63, 0x63, 0x63, 0x00, 0x64, 0x64, 0x64 } },
                { "CustomMarshaler14", new byte[] { 0x2c, 0x00, 0x00, 0x04, 0xf0, 0xaa, 0x9b, 0x96, 0x04, 0xf0, 0xaa, 0x9b, 0x96 } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs);
            VerifyFieldMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void CustomMarshal_Errors()
        {
            var source = @"
#pragma warning disable 169

using System.Runtime.InteropServices;

public class X
{
    [MarshalAs(UnmanagedType.CustomMarshaler)]int CustomMarshaler_e0;
    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""a\udc00b"", MarshalCookie = ""b"" )]int CustomMarshaler_e1;
    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""x"", MarshalCookie = ""y\udc00"" )]int CustomMarshaler_e2;
}
";
            // Dev10 encodes incomplete surrogates, we don't.

            CreateCompilation(source).VerifyDiagnostics(
                // (8,6): error CS7047: Attribute parameter 'MarshalType' or 'MarshalTypeRef' must be specified.
                //     [MarshalAs(UnmanagedType.CustomMarshaler)]int CustomMarshaler_e0;
                Diagnostic(ErrorCode.ERR_AttributeParameterRequired2, "MarshalAs").WithArguments("MarshalType", "MarshalTypeRef"),
                // (9,47): error CS0599: Invalid value for named attribute argument 'MarshalType'
                //     [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "a\udc00b", MarshalCookie = "b" )]int CustomMarshaler_e1;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, @"MarshalType = ""a\udc00b""").WithArguments("MarshalType"),
                // (10,66): error CS0599: Invalid value for named attribute argument 'MarshalCookie'
                //     [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "x", MarshalCookie = "y\udc00" )]int CustomMarshaler_e2;
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, @"MarshalCookie = ""y\udc00""").WithArguments("MarshalCookie"));
        }

        [Fact]
        public void EventAndEnumMembers()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [field: MarshalAs(UnmanagedType.Bool)]
    event Action e;
}

enum E
{
    [MarshalAs(UnmanagedType.Bool)]
    X = 1
}

";

            CompileAndVerifyFieldMarshal(source, (name, _omitted1) => (name == "e" || name == "X") ? new byte[] { 0x02 } : null);
        }

        #endregion

        #region Parameters and Return Values

        [Fact]
        public void Parameters()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

class X
{
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static X foo(

        [MarshalAs(UnmanagedType.IDispatch)]
        ref int IDispatch,

        [MarshalAs(UnmanagedType.LPArray)]
        out int LPArray0,

        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_EMPTY)]
        int SafeArray8,
    
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""aaa\0bbb"", MarshalCookie = ""ccc\0ddd"" )]
        int CustomMarshaler13
    )
    {
        throw null;
    }
}
";
            var blobs = new Dictionary<string, byte[]>()
            {
                { "foo:",                  new byte[] { 0x14 } }, // return value
                { "foo:IDispatch",         new byte[] { 0x1a } },
                { "foo:LPArray0",          new byte[] { 0x2a, 0x50 } },
                { "foo:SafeArray8",        new byte[] { 0x1d, 0x00 } },
                { "foo:CustomMarshaler13", new byte[] { 0x2c, 0x00, 0x00, 0x07, 0x61, 0x61, 0x61, 0x00, 0x62, 0x62, 0x62, 0x07, 0x63, 0x63, 0x63, 0x00, 0x64, 0x64, 0x64 } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs, isField: false);
            VerifyParameterMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void Parameters_LocalFunction()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

class X
{
    void M()
    {
        [return: MarshalAs(UnmanagedType.LPStr)]
        static X local(

            [MarshalAs(UnmanagedType.IDispatch)]
            ref int IDispatch,

            [MarshalAs(UnmanagedType.LPArray)]
            out int LPArray0,

            [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_EMPTY)]
            int SafeArray8,

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""aaa\0bbb"", MarshalCookie = ""ccc\0ddd"" )]
            int CustomMarshaler13
        )
        {
            throw null;
        }
    }
}
";
            var blobs = new Dictionary<string, byte[]>()
            {
                { "<M>g__local|0_0:",                  new byte[] { 0x14 } }, // return value
                { "<M>g__local|0_0:IDispatch",         new byte[] { 0x1a } },
                { "<M>g__local|0_0:LPArray0",          new byte[] { 0x2a, 0x50 } },
                { "<M>g__local|0_0:SafeArray8",        new byte[] { 0x1d, 0x00 } },
                { "<M>g__local|0_0:CustomMarshaler13", new byte[] { 0x2c, 0x00, 0x00, 0x07, 0x61, 0x61, 0x61, 0x00, 0x62, 0x62, 0x62, 0x07, 0x63, 0x63, 0x63, 0x00, 0x64, 0x64, 0x64 } },
            };

            var verifier = CompileAndVerifyFieldMarshal(source, blobs, isField: false);
            VerifyParameterMetadataDecoding(verifier, blobs);
        }

        [Fact]
        public void MarshalAs_AllParameterTargets_PartialMethods()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

public partial class X
{
    partial void F([MarshalAs(UnmanagedType.BStr)] int pf);
    partial void F(int pf) { }

    partial void G(int pg);
    partial void G([MarshalAs(UnmanagedType.BStr)] int pg) {}
    
    partial void H([MarshalAs(UnmanagedType.BStr)] int ph) {}
    partial void H(int ph);

    partial void I(int pi) { }
    partial void I([MarshalAs(UnmanagedType.BStr)] int pi);
}
";
            var blobs = new Dictionary<string, byte[]>()
            {
                {"F:pf", new byte[] {0x13}},
                {"G:pg", new byte[] {0x13}},
                {"H:ph", new byte[] {0x13}},
                {"I:pi", new byte[] {0x13}},
            };

            CompileAndVerifyFieldMarshal(source, blobs, isField: false);
        }

        [WorkItem(544508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544508")]
        [Fact]
        public void Parameters_Property_Accessors()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public interface I
{
    string P
    {
        [return: MarshalAs(UnmanagedType.BStr)]
        get;
        
        [param: MarshalAs(UnmanagedType.BStr)]
        set;
    }
}";
            CompileAndVerifyFieldMarshal(source, new Dictionary<string, byte[]>()
                {
                    { "get_P:", new byte[] { 0x13 } }, // return value for get accessor
                    { "set_P:" + ParameterSymbol.ValueParameterName, new byte[] { 0x13 } }, // value parameter for set accessor
                },
                isField: false);
        }

        [WorkItem(544508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544508")]
        [Fact]
        public void Parameters_Event_Accessors()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

class C
{
    event Action<string> E
    {
        [param: MarshalAs(UnmanagedType.BStr)]
        add { }
        [param: MarshalAs(UnmanagedType.BStr)]
        remove { }
    }
}";
            CompileAndVerifyFieldMarshal(source, new Dictionary<string, byte[]>()
                {
                    { "add_E:" + ParameterSymbol.ValueParameterName, new byte[] { 0x13 } },
                    { "remove_E:" + ParameterSymbol.ValueParameterName, new byte[] { 0x13 } },
                },
                isField: false);
        }

        [Fact]
        public void Parameters_Indexer_Getter()
        {
            var source = @"
using System.Runtime.InteropServices;
public class C
{
    public int this[[MarshalAs(UnmanagedType.BStr)]int a, [MarshalAs(UnmanagedType.BStr)]int b]
    {
        get { return 0; }
    }
}
";
            CompileAndVerifyFieldMarshal(source, new Dictionary<string, byte[]>()
                {
                    { "get_Item:a", new byte[] { 0x13 } },
                    { "get_Item:b", new byte[] { 0x13 } },
                },
                isField: false);
        }

        [Fact]
        public void Parameters_Indexer_Setter()
        {
            var source = @"
using System.Runtime.InteropServices;
public class C
{
    public int this[[MarshalAs(UnmanagedType.BStr)]int a, [MarshalAs(UnmanagedType.BStr)]int b]
    {
        [param: MarshalAs(UnmanagedType.BStr)]
        set { }
    }
}
";
            CompileAndVerifyFieldMarshal(source, new Dictionary<string, byte[]>()
                {
                    { "set_Item:" + ParameterSymbol.ValueParameterName, new byte[] { 0x13 } },
                    { "set_Item:a", new byte[] { 0x13 } },
                    { "set_Item:b", new byte[] { 0x13 } },
                },
                isField: false);
        }

        [WorkItem(544509, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544509")]
        [Fact]
        public void Parameters_DelegateType()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

class C
{
    [return: MarshalAs(UnmanagedType.BStr)]
    public delegate string Delegate(
        [In, MarshalAs(UnmanagedType.BStr)]string p1,
        [param: In, Out, MarshalAs(UnmanagedType.BStr)]ref string p2,
        [Out, MarshalAs(UnmanagedType.BStr)]out string p3);
}";
            var marshalAsBstr = new byte[] { 0x13 };

            CompileAndVerifyFieldMarshal(source, new Dictionary<string, byte[]>()
                {
                    { ".ctor:object", null },
                    { ".ctor:method", null },
                    { "Invoke:",  marshalAsBstr}, // return value
                    { "Invoke:p1", marshalAsBstr },
                    { "Invoke:p2", marshalAsBstr },
                    { "Invoke:p3", marshalAsBstr },
                    { "BeginInvoke:p1", marshalAsBstr },
                    { "BeginInvoke:p2", marshalAsBstr },
                    { "BeginInvoke:p3", marshalAsBstr },
                    { "BeginInvoke:object", null },
                    { "BeginInvoke:callback", null },
                    { "EndInvoke:", marshalAsBstr },
                    { "EndInvoke:p1", marshalAsBstr },
                    { "EndInvoke:p2", marshalAsBstr },
                    { "EndInvoke:p3", marshalAsBstr },
                    { "EndInvoke:result", null },
                },
                isField: false);
        }

        [Fact]
        public void Parameters_Errors()
        {
            var source = @"
#pragma warning disable 169

using System.Runtime.InteropServices;

class X
{
    public static void f1(
        [MarshalAs(UnmanagedType.ByValArray)]
        int ByValArray,

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        int ByValTStr
    ) 
    {
    }

    [return: MarshalAs(UnmanagedType.ByValArray)]
    public static int f2() { return 0; }

    [return: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
    public static int f3() { return 0; }

    [MarshalAs(UnmanagedType.VBByRefStr)]
    public int field;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,20): error CS7055: Unmanaged type 'ByValArray' is only valid for fields.
                Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValArray").WithArguments("ByValArray"),
                // (10,20): error CS7055: Unmanaged type 'ByValTStr' is only valid for fields.
                Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValTStr").WithArguments("ByValTStr"),
                // (16,24): error CS7055: Unmanaged type 'ByValArray' is only valid for fields.
                Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValArray").WithArguments("ByValArray"),
                // (19,24): error CS7055: Unmanaged type 'ByValTStr' is only valid for fields.
                Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValTStr").WithArguments("ByValTStr"),
                // (22,16): error CS7054: Unmanaged type 'VBByRefStr' not valid for fields.
                Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeNotValidForFields, "UnmanagedType.VBByRefStr").WithArguments("VBByRefStr"),

                // TODO (tomat): remove

                // (23,16): warning CS0649: Field 'X.field' is never assigned to, and will always have its default value 0
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("X.field", "0"));
        }

        [Fact]
        public void Parameters_Errors_LocalFunction()
        {
            var source = @"
#pragma warning disable 8321 // Unreferenced local function

using System.Runtime.InteropServices;

class X
{
    void M()
    {
        static void f1(
            [MarshalAs(UnmanagedType.ByValArray)]
            int ByValArray,

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
            int ByValTStr
        )
        {
        }

        [return: MarshalAs(UnmanagedType.ByValArray)]
        static int f2() { return 0; }

        [return: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        static int f3() { return 0; }
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                    // (11,24): error CS7055: Unmanaged type 'ByValArray' is only valid for fields.
                    //             [MarshalAs(UnmanagedType.ByValArray)]
                    Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValArray").WithArguments("ByValArray").WithLocation(11, 24),
                    // (14,24): error CS7055: Unmanaged type 'ByValTStr' is only valid for fields.
                    //             [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
                    Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValTStr").WithArguments("ByValTStr").WithLocation(14, 24),
                    // (20,28): error CS7055: Unmanaged type 'ByValArray' is only valid for fields.
                    //         [return: MarshalAs(UnmanagedType.ByValArray)]
                    Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValArray").WithArguments("ByValArray").WithLocation(20, 28),
                    // (23,28): error CS7055: Unmanaged type 'ByValTStr' is only valid for fields.
                    //         [return: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
                    Diagnostic(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValTStr").WithArguments("ByValTStr").WithLocation(23, 28));
        }

        /// <summary>
        ///  type only, only on parameters
        /// </summary>
        [Fact]
        public void NativeTypeByValStr()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

class X
{
    [return: MarshalAs(UnmanagedType.VBByRefStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
            SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
    static void f(
        [MarshalAs(UnmanagedType.VBByRefStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
            SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
        ref int VBByRefStr_e1,

        [MarshalAs(UnmanagedType.VBByRefStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
            SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
        char[] VBByRefStr_e2,

        [MarshalAs(UnmanagedType.VBByRefStr, ArraySubType = UnmanagedType.ByValTStr, IidParameterIndex = -1, MarshalCookie = null, MarshalType = null, MarshalTypeRef = null,
            SafeArraySubType = VarEnum.VT_BSTR, SafeArrayUserDefinedSubType = null, SizeConst = -1, SizeParamIndex = -1)]
        int VBByRefStr_e3)
    { }
}
";
            CompileAndVerifyFieldMarshal(source, new Dictionary<string, byte[]>
            {
                { "f:",              new byte[] { 0x22 } },  // return value
                { "f:VBByRefStr_e1", new byte[] { 0x22 } },
                { "f:VBByRefStr_e2", new byte[] { 0x22 } },
                { "f:VBByRefStr_e3", new byte[] { 0x22 } },
            },
            isField: false);
        }

        [Fact, WorkItem(545374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545374")]
        public void ImportOptionalMarshalAsParameter()
        {
            string text1 = @"
using System.Runtime.InteropServices;

public class P2<T>
{
    public int Foo([Optional][MarshalAs(UnmanagedType.IDispatch)] T i)
    {
        if (i == null) 
            return 0;
        return 1;
    }
}
";
            string text2 = @"
class C
{
    public static void Main()
    {
        P2<object> p2 = new P2<object>();
        System.Console.WriteLine(p2.Foo());
    }
}
";
            var comp1 = CreateCompilation(text1, assemblyName: "OptionalMarshalAsLibrary");
            var comp2 = CreateCompilation(text2,
                options: TestOptions.ReleaseExe,
                references: new[] { comp1.EmitToImageReference() },  // it has to be real assembly, Comp2comp reference OK
                assemblyName: "APP");

            CompileAndVerify(comp2, expectedOutput: @"0").VerifyIL("C.Main", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  newobj     ""P2<object>..ctor()""
  IL_0005:  ldnull
  IL_0006:  callvirt   ""int P2<object>.Foo(object)""
  IL_000b:  call       ""void System.Console.WriteLine(int)""
  IL_0010:  ret
}
");
        }

        #endregion
    }
}
