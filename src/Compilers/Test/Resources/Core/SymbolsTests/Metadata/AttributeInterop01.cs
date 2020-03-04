// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib("InteropAttributes")]
[assembly: PrimaryInteropAssembly(1, 2)]
[assembly: Guid("1234C65D-1234-447A-B786-64682CBEF136")]

[assembly: BestFitMapping(false, ThrowOnUnmappableChar = true)]

[assembly: AutomationProxy(false)] // not embed
[assembly: ClassInterface(ClassInterfaceType.AutoDual)] // not embed
[assembly: ComCompatibleVersion(1, 2, 3, 4)] // not embed
[assembly: ComConversionLoss()] // not embed
[assembly: ComVisible(true)] // not embed
[assembly: TypeLibVersion(1, 0)] // not embed

namespace Interop
{
    [ComImport, Guid("ABCDEF5D-2448-447A-B786-64682CBEF123")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeLibImportClass(typeof(object)), TypeLibType(TypeLibTypeFlags.FAggregatable)]
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

    [TypeLibType(TypeLibTypeFlags.FAppObject)]
    public enum EGoo
    {
        One, Two, Three
    }

    [Serializable, ComVisible(false)]
    [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, BestFitMapping = true, CharSet = CharSet.Ansi, SetLastError = true, ThrowOnUnmappableChar = true)]
    public delegate void DGoo(char p1, sbyte p2);

    [TypeIdentifier("1234C65D-1234-447A-B786-64682CBEF136", "SGoo, INteropAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Guid("C3957C2A-07DD-4A56-AF01-FFD56664600F"), BestFitMapping(false, ThrowOnUnmappableChar = true)]
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Pack = 8, Size = 64)]
    public struct SGoo
    {
        [FieldOffset(0)]
        public sbyte field01;
        [FieldOffset(8), TypeLibVar(TypeLibVarFlags.FReadOnly)]
        public byte field02;
        [FieldOffset(16), MarshalAs(UnmanagedType.I2)]
        [TypeLibVar(TypeLibVarFlags.FDisplayBind)]
        public short field03;
        [FieldOffset(24), MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 9)]
        public int[] nAry;
    }

    [ComDefaultInterface(typeof(object)), ProgId("ProgId")]
    public class CGoo
    {
        [DllImport("app.dll")]
        static extern bool DllImport();
    }

    [ComImport, TypeLibType(TypeLibTypeFlags.FAggregatable)]
    [Guid("A88A175D-2448-447A-B786-CCC82CBEF156"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    [CoClass(typeof(CBar))]
    public interface IBar
    {
        [DispId(10)]
        long MarshalAsGetProperty { [return: MarshalAs(UnmanagedType.I8)] get; }

        [DispId(20), IndexerNameAttribute("MyIndex")]
        int this[int idx] { get; set; }

        [DispId(30), PreserveSig]
        int MixedAttrMethod1([In] [MarshalAs(UnmanagedType.U4)] uint v1, [In, Out][MarshalAs(UnmanagedType.I4)] ref int v2);

        [DispId(40), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void IDispatchParameters([MarshalAs(UnmanagedType.IDispatch)] object v1, [Out] [MarshalAs(UnmanagedType.IUnknown)] out object v2);

        [DispId(50), TypeLibFunc(TypeLibFuncFlags.FBindable)]
        void SCodeParameter([MarshalAs(UnmanagedType.Error)] int v1);

        [DispId(60)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string VariantParameters([MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = "YumYum", MarshalType = "IUnknown")] object v1, [In][Out] ref object v2);

        [LCIDConversion(1)]
        void DecimalStringParameter([In] decimal v1, [MarshalAs(UnmanagedType.LPStr)] string v2, [MarshalAs(UnmanagedType.LPWStr)] string v3);
        void CurrencyParameter([In, MarshalAs(UnmanagedType.Currency)] decimal v1);
        // int MixedAttrMethod([In] [ComAliasName(stdole.OLE_COLOR)]uint v1, [In][Out][MarshalAs(UnmanagedType.I4)] ref int v2);
    }

    [Guid("666A175D-2448-447A-B786-CCC82CBEF156"), BestFitMapping(true, ThrowOnUnmappableChar = false)]
    [StructLayout(LayoutKind.Auto)]
    public struct SBar
    {
        [MarshalAs(UnmanagedType.BStr)]
        public string s1;
        [MarshalAs(UnmanagedType.LPStr)]
        public string s2;
        [MarshalAs(UnmanagedType.LPWStr), TypeLibVar(TypeLibVarFlags.FDisplayBind)]
        public object o1;
        [MarshalAs(UnmanagedType.Currency), TypeLibVar(TypeLibVarFlags.FReadOnly)]
        public object o2;
    }

    // not import
    [Guid("CCCA175D-2448-447A-B786-CCC82CBEF156")]
    [Serializable, BestFitMapping(true), StructLayout(LayoutKind.Explicit)]
    public class CBar
    {
        [DispId(1000), MarshalAs(UnmanagedType.I4), FieldOffset(0), TypeLibVar(TypeLibVarFlags.FHidden)]
        int field;
    }
}
