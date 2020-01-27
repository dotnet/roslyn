// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

[module: UnverifiableCodeAttribute()]
namespace EventNS
{
    /// <summary>
    /// Source Interface
    /// </summary>
    [ComImport, Guid("904458F3-005B-4DFD-8581-E9832D7FA407")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch), TypeLibType(TypeLibTypeFlags.FDual)]
    public interface IEvents
    {
        [DispId(101), PreserveSig]
        void OnEvent01();
        [DispId(102), PreserveSig]
        void OnEvent02(object i1);
        [DispId(103), PreserveSig]
        void OnEvent03(object i1, [Optional, IUnknownConstant] object i2, bool b = false);
    }

    /// <summary>
    /// Event Interface
    /// </summary>
    [ComEventInterface(typeof(IEvents), typeof(int))]
    [ComVisible(false), TypeLibType(TypeLibTypeFlags.FHidden)]
    public interface IEvents_Event
    {
        event OnEvent01EventHandler OnEvent01;
        event OnEvent02EventHandler OnEvent02;
        event OnEvent03EventHandler OnEvent03;
    }

    [ComSourceInterfaces(typeof(IEvents))]
    public class NetImpl : IEvents
    {
        void IEvents.OnEvent01() {  }
        void IEvents.OnEvent02(object i1) { }
        void IEvents.OnEvent03(object i1, object i2, bool b) { }
    }
    /// <summary>
    /// delegate
    /// </summary>
    public delegate void OnEvent01EventHandler();
    public delegate void OnEvent02EventHandler(object i1);
    public delegate void OnEvent03EventHandler(object i1, [Optional, IUnknownConstant] object i2, bool b = false);

    [ComImport, Guid("01230DD5-2448-447A-B786-64682CBEFEEE")]
    [TypeLibType(TypeLibTypeFlags.FAggregatable)]
    public interface IOptional
    {
        int MethodRef([Optional, DefaultParameterValue(88)] ref int v);
        ulong MethodRef1([Optional, DefaultParameterValue(99ul)] ref ulong v);
        string MethodRef2([In, Out, Optional, DefaultParameterValue("Ref")] ref string v);
        MyEnum MethodRef3([In, Out, Optional, DefaultParameterValue(MyEnum.two)] ref MyEnum v);

        string NandOMethod6([Optional, DefaultParameterValue(' ')] char v1,
                    [Optional] [DefaultParameterValue(0.0f)] float v2,
                    [In, Optional, DefaultParameterValue(-1)] int v3,
                    [In][Optional][DefaultParameterValue(null)] string v4);
        void MethodWithConstantValues(
            [DateTimeConstant(123456)]DateTime p1, 
            [DecimalConstant(0,0,100,100,100)] decimal p2,
            [Optional, IDispatchConstant] ref object p3);
    }

    [Guid("31230DD5-2448-447A-B786-64682CBEFEEE"), Flags]
    public enum MyEnum : sbyte
    { 
        [NonSerialized]zero = 0, one = 1, two = 2, [Obsolete("message", false)]three = 4 
    }
}
