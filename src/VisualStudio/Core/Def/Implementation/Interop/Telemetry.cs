// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
 
namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    [ComImport]
    [CompilerGenerated]
    [Guid("09FCE009-9ADE-4210-A7C9-7842B7607660")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    internal interface IVsTelemetryPropertyBag
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Struct)]
        object GetProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);
        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBoolProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] bool value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDoubleProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetIntProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetLongProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetShortProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] short value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetStringProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool ContainsProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [DispId(1610678282)]
        string[] AllPropertyNames
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
            get;
        }
        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGuidProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid szValue);
    }
    
    [ComImport]
    [CompilerGenerated]
    [Guid("4DC591E8-7970-47FD-917E-624A8684D2D0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    internal interface IVsTelemetryEvent
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetName();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Struct)]
        object GetProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBoolProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] bool value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDoubleProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetIntProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetLongProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetShortProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] short value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetStringProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool ContainsProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [DispId(1610678282)]
        string[] AllPropertyNames
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
            get;
        }

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptOutFriendlyFlag([In] bool bOptOutFriendly);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddPropertyBag([In, MarshalAs(UnmanagedType.Interface)] IVsTelemetryPropertyBag pPropertyBag);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemovePropertyBag([In, MarshalAs(UnmanagedType.Interface)] IVsTelemetryPropertyBag pPropertyBag);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetIntPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetLongPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDoublePiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetStringPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGuidProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGuidPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid varValue);
    }

    [ComImport]
    [CompilerGenerated]
    [Guid("8F0E37F9-B1F6-4247-9C91-D801F92710A5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    internal interface IVsTelemetryActivity : IVsTelemetryEvent
    {
        #region IVsTelemetryEvent
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetName();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Struct)]
        object GetProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetBoolProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] bool value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDoubleProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetIntProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetLongProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetShortProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] short value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetStringProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool ContainsProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [DispId(1610678282)]
        string[] AllPropertyNames
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
            get;
        }

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptOutFriendlyFlag([In] bool bOptOutFriendly);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddPropertyBag([In, MarshalAs(UnmanagedType.Interface)] IVsTelemetryPropertyBag pPropertyBag);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemovePropertyBag([In, MarshalAs(UnmanagedType.Interface)] IVsTelemetryPropertyBag pPropertyBag);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetIntPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetLongPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDoublePiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetStringPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGuidProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetGuidPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid varValue);
        #endregion
        
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Start();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void End();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void EndWithDuration([In] long eventDuration);
        
        [DispId(1610743811)]
        Guid CorrelationId
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            get;
        }
    }
    
    [ComImport]
    [CompilerGenerated]
    [Guid("DEB68DE7-7104-4168-AB7D-F8CDE0C19DA4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    internal interface IVsTelemetryContext
    {
        [DispId(1610678272)]
        string ContextName
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Struct)]
        object GetProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostBoolProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] bool value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostDoubleProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostIntProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostLongProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostShortProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] short value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostStringProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedBoolProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] bool value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedDoubleProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedIntProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedLongProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedShortProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] short value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedStringProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveSharedProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Close();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostIntPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostLongPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostDoublePiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostStringPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedIntPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedLongPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedDoublePiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedStringPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostGuidProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedGuidProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostGuidPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedGuidPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid varValue);
    }
    
    [ComImport]
    [CompilerGenerated]
    [Guid("2C1F9C40-19AE-4697-A459-226943802123")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    internal interface IVsTelemetrySession
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetSessionId();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string SerializeSettings();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostSimpleEvent([In, MarshalAs(UnmanagedType.LPWStr)] string szEventName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostEvent([In, MarshalAs(UnmanagedType.Interface)] IVsTelemetryEvent eventObject);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostBoolProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] bool value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostDoubleProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostIntProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostLongProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostShortProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] short value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostStringProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedBoolProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] bool value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedDoubleProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedIntProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedLongProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedShortProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] short value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedStringProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string szValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveSharedProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetryContext CreateContext([In, MarshalAs(UnmanagedType.LPWStr)] string szContextName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool IsUserOptedIn();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool IsUserMicrosoftInternal();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        bool CanCollectPrivateInformation();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetUserOptedIn([In] bool IsUserOptedIn);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RegisterPropertyBag([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyBagName, [In, MarshalAs(UnmanagedType.Interface)] IVsTelemetryPropertyBag pPropertyBag);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetryPropertyBag GetPropertyBag([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyBagName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void UnregisterPropertyBag([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyBagName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostIntPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostLongPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostDoublePiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostStringPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.Struct)] object varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedIntPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] int varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedLongPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] long varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedDoublePiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] double varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedStringPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In, MarshalAs(UnmanagedType.LPWStr)] string varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Struct)]
        object GetSharedProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetryContext GetContext([In, MarshalAs(UnmanagedType.LPWStr)] string szContextName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostGuidProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedGuidProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void PostGuidPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid varValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSharedGuidPiiProperty([In, MarshalAs(UnmanagedType.LPWStr)] string szPropertyName, [In] Guid varValue);
    }

    [ComImport]
    [CompilerGenerated]
    [Guid("5C7E7029-A00C-4F57-BE15-6AC5D43E78CB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    internal interface IVsTelemetryService
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetrySession GetDefaultSession();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetryEvent CreateEvent([In, MarshalAs(UnmanagedType.LPWStr)] string szEventName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetryActivity CreateActivity([In, MarshalAs(UnmanagedType.LPWStr)] string szActivityName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetryActivity CreateActivityWithParentCorrelationId([In, MarshalAs(UnmanagedType.LPWStr)] string szActivityName, [In] ref Guid parentCorrelationId);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Interface)]
        IVsTelemetryPropertyBag CreatePropertyBag();
    }
}
