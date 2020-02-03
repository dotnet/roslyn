// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib("MockInterop")]
[assembly: PrimaryInteropAssembly(30303,33)]
[assembly: Guid("71B8C65D-7748-447A-B786-64682CBEF136")]
[assembly: BestFitMapping(false, ThrowOnUnmappableChar = true)]

[assembly: AutomationProxy(false)] // not embed
[assembly: ClassInterface(ClassInterfaceType.AutoDual)] // not embed
[assembly: ComCompatibleVersion(1, 2, 3, 4)] // not embed
[assembly: ComConversionLoss()] // not embed
[assembly: ComVisible(true)] // not embed
[assembly: TypeLibVersion(1, 0)] // not embed
// [assembly: SetWin32ContextInIDispatch()]
// [assembly: IDispatchImpl(IDispatchImplType.CompatibleImpl)] // not embed

namespace MockInterop01
{
    // [TypeIdentifier("71B8C65D-7748-447A-B786-64682CBEF136", "MockInterop01.InteropEnum")]
    [Flags, Serializable, Guid("EEEE0B17-2558-447D-B786-84682CBEF136")]
    public enum InteropEnum : uint
    {
        None,
        Red = 0x0001,
        Blue = 0x0002,
        White = 0x0004,
        All = 0x0007
    }

    // [TypeIdentifier("71B8C65D-7748-447A-B786-64682CBEF136", "MockInterop01.UnionStruct")]
    [Guid("5720C751-2222-447A-B786-64682CBEF122")]
    [StructLayout(LayoutKind.Explicit)]
    public struct UnionStruct
    {
        [FieldOffset(0)]
        [TypeLibVar(TypeLibVarFlags.FBindable), MarshalAs(UnmanagedType.I1)]
        public sbyte field01;
        [FieldOffset(0)]
        [TypeLibVar(TypeLibVarFlags.FBindable), MarshalAs(UnmanagedType.U2)]
        public ushort field02;
        [FieldOffset(0), MarshalAs(UnmanagedType.I4)]
        [TypeLibVar(TypeLibVarFlags.FBindable)]
        public int field03;
        [FieldOffset(0)]
        [TypeLibVar(TypeLibVarFlags.FBindable), MarshalAs(UnmanagedType.U8)]
        public ulong field04;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 16, Size = 8), ComConversionLoss]
    public struct ComplexStruct
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct InnerStruct
        {
            public Int16 x;
            public Int64 y;
            public IntPtr z;
        }
        [DispId(1)]
        public Guid GuidField;
        [DispId(2)]
        public Decimal DecimalField;
        [DispId(3), ComConversionLoss, ComAliasName("MockInterop01.UnionStruct"), MarshalAs(UnmanagedType.Struct)]
        public UnionStruct UnionField;
    }
     
    [ComImport /*, TypeIdentifier*/]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5720C75D-2448-447A-B786-64682CBEF156")]
    [TypeLibType(TypeLibTypeFlags.FAggregatable)]
    public interface IGoo
    {
        [DispId(1010)]
        InteropEnum IGooReadOnlyProp {
            [return: MarshalAs(UnmanagedType.U4), ComConversionLoss]
            get;
        }

        [DispId(1011)]
        [return: MarshalAs(UnmanagedType.Struct)]
        ComplexStruct MethodForStruct(ref UnionStruct p1, out InteropDeleWithStructArray p2);

        [DispId(1012)]
        string this[string p, IGoo p2] {
            [return: MarshalAs(UnmanagedType.BStr)]
            get; set; }

        [DispId(1013)]
        event InteropDeleWithStructArray IGooEvent;
    }

    [ComImport, Guid("ABCDEF5D-2448-447A-B786-64682CBEF123")]
    [TypeLibImportClass(typeof(object))]
    public interface IBar
    {
        [AllowReversePInvokeCalls()]
        object DoSomething(params string[] ary);
        [ComRegisterFunction()]
        object Register([MarshalAs(UnmanagedType.IDispatch), Optional, DefaultParameterValue(null)] ref object o);
        [ComUnregisterFunction()]
        void UnRegister([MarshalAs(UnmanagedType.IDispatch), Optional, IDispatchConstant()] object o);
        [TypeLibFunc(TypeLibFuncFlags.FDefaultBind)]
        void LibFunc([Optional, DecimalConstant(1, 2, (uint)3, (uint)4, (uint)5)] decimal p1, [Optional, In, Out, DateTimeConstant(123456)] DateTime p2);
    }

    /// <summary>
    /// Source Interface
    /// </summary>
    [ComImport, Guid("904458F3-005B-4DFD-8581-E9832D7FA433")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch), TypeLibType(TypeLibTypeFlags.FDispatchable)]
    public interface IEventSource
    {
        [DispId(101), PreserveSig]
        void Event01(IGoo p1);
        [DispId(102), PreserveSig]
        void Event02(InteropEnum p1);
        [DispId(103), PreserveSig]
        void Event03(ComplexStruct p1);
    }

    /// <summary>
    /// Event Interface
    /// </summary>
    [ComEventInterface(typeof(IEventSource), typeof(object))]
    public interface IEventEvent
    {
        event EventDele01 OnEvent01;
        event EventDele02 OnEvent02;
        event EventDele03 OnEvent03;
    }

    public delegate void EventDele01(IGoo p);
    public delegate void EventDele02(InteropEnum p);
    public delegate void EventDele03(ComplexStruct p);

    [ComVisible(false)]
    // [TypeIdentifier("71B8C65D-7748-447A-B786-64682CBEF136", "MockInterop01.InteropDeleWithStructArray")]
    [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, BestFitMapping = false, CharSet = CharSet.Auto)]
    public delegate void InteropDeleWithStructArray([In, Out, ComAliasName("MockInterop01.UnionStruct"), MarshalAs(UnmanagedType.LPArray)] UnionStruct[] p);
}
