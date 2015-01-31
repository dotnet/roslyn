// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
 
namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    [Guid("B567D0C0-9334-4EFB-811B-F12AEE408A3D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTelemetrySession
    {
        bool CanCollectPrivateInformation();
        IVsTelemetryContext CreateContext([ComAliasName("OLE.LPCOLESTR")]string szContextName);
        IVsTelemetryContext GetContext([ComAliasName("OLE.LPCOLESTR")]string szContextName);
        IVsTelemetryPropertyBag GetPropertyBag([ComAliasName("OLE.LPCOLESTR")]string szPropertyBagName);
        string GetSessionId();
        object GetSharedProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        bool IsUserMicrosoftInternal();
        bool IsUserOptedIn();
        void PostBoolProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, bool value);
        void PostDoublePiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double varValue);
        void PostDoubleProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double value);
        void PostEvent(IVsTelemetryEvent eventObject);
        void PostIntPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int varValue);
        void PostIntProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int value);
        void PostLongPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long varValue);
        void PostLongProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long value);
        void PostPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void PostProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void PostShortProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, short value);
        void PostSimpleEvent([ComAliasName("OLE.LPCOLESTR")]string szEventName);
        void PostStringPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string varValue);
        void PostStringProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string szValue);
        void RegisterPropertyBag([ComAliasName("OLE.LPCOLESTR")]string szPropertyBagName, IVsTelemetryPropertyBag pPropertyBag);
        void RemoveSharedProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        string SerializeSettings();
        void SetSharedBoolProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, bool value);
        void SetSharedDoublePiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double varValue);
        void SetSharedDoubleProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double value);
        void SetSharedIntPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int varValue);
        void SetSharedIntProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int value);
        void SetSharedLongPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long varValue);
        void SetSharedLongProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long value);
        void SetSharedPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetSharedProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetSharedShortProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, short value);
        void SetSharedStringPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string varValue);
        void SetSharedStringProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string szValue);
        void SetUserOptedIn(bool IsUserOptedIn);
        void UnregisterPropertyBag([ComAliasName("OLE.LPCOLESTR")]string szPropertyBagName);
    }

    [Guid("82CB18EA-5330-4ADB-BD79-EB964F30749D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTelemetryContext
    {
        [DispId(1610678272)]
        string ContextName { get; }
 
        void Close();
        object GetProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        void PostBoolProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, bool value);
        void PostDoublePiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double varValue);
        void PostDoubleProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double value);
        void PostIntPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int varValue);
        void PostIntProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int value);
        void PostLongPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long varValue);
        void PostLongProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long value);
        void PostPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void PostProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void PostShortProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, short value);
        void PostStringPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string varValue);
        void PostStringProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string szValue);
        void RemoveSharedProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        void SetSharedBoolProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, bool value);
        void SetSharedDoublePiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double varValue);
        void SetSharedDoubleProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double value);
        void SetSharedIntPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int varValue);
        void SetSharedIntProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int value);
        void SetSharedLongPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long varValue);
        void SetSharedLongProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long value);
        void SetSharedPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetSharedProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetSharedShortProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, short value);
        void SetSharedStringPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string varValue);
        void SetSharedStringProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string szValue);
    }

    [Guid("FC1510D1-1CE5-4EE9-B861-66714C95A242")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTelemetryPropertyBag
    {
        [DispId(1610678282)]
        Array AllPropertyNames { get; }
 
        bool ContainsProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        object GetProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        void RemoveProperty([ComAliasName("OLE.LPOLESTR")]string szPropertyName);
        void SetBoolProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, bool value);
        void SetDoubleProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double value);
        void SetIntProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int value);
        void SetLongProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long value);
        void SetProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetShortProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, short value);
        void SetStringProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string szValue);
    }

    [Guid("5DC5F85E-C49B-4CF6-97B7-BDEF7AD0ADE3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTelemetryEvent
    {
        [DispId(1610678282)]
        Array AllPropertyNames { get; }
 
        void AddPropertyBag(IVsTelemetryPropertyBag pPropertyBag);
        bool ContainsProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        string GetName();
        object GetProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        void RemovePropertyBag(IVsTelemetryPropertyBag pPropertyBag);
        void SetBoolProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, bool value);
        void SetDoublePiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double varValue);
        void SetDoubleProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double value);
        void SetIntPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int varValue);
        void SetIntProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int value);
        void SetLongPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long varValue);
        void SetLongProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long value);
        void SetOptOutFriendlyFlag(bool bOptOutFriendly);
        void SetPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetShortProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, short value);
        void SetStringPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string varValue);
        void SetStringProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string szValue);
    }

    [Guid("5C7E7029-A00C-4F57-BE15-6AC5D43E78CB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTelemetryService
    {
        IVsTelemetryActivity CreateActivity([ComAliasName("OLE.LPCOLESTR")]string szActivityName);
        IVsTelemetryActivity CreateActivityWithParentCorrelationId([ComAliasName("OLE.LPCOLESTR")]string szActivityName, [ComAliasName("OLE.REFGUID")]ref Guid parentCorrelationId);
        IVsTelemetryEvent CreateEvent([ComAliasName("OLE.LPCOLESTR")]string szEventName);
        IVsTelemetryPropertyBag CreatePropertyBag();
        IVsTelemetrySession GetDefaultSession();
    }

    [Guid("8F0E37F9-B1F6-4247-9C91-D801F92710A5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsTelemetryActivity : IVsTelemetryEvent
    {
        [DispId(1610678282)]
        Array AllPropertyNames { get; }
        [DispId(1610743811)]
        Guid CorrelationId { get; }
 
        void AddPropertyBag(IVsTelemetryPropertyBag pPropertyBag);
        bool ContainsProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        void End();
        void EndWithDuration(long eventDuration);
        string GetName();
        object GetProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName);
        void RemovePropertyBag(IVsTelemetryPropertyBag pPropertyBag);
        void SetBoolProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, bool value);
        void SetDoublePiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double varValue);
        void SetDoubleProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, double value);
        void SetIntPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int varValue);
        void SetIntProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, int value);
        void SetLongPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long varValue);
        void SetLongProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, long value);
        void SetOptOutFriendlyFlag(bool bOptOutFriendly);
        void SetPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, object varValue);
        void SetShortProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, short value);
        void SetStringPiiProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string varValue);
        void SetStringProperty([ComAliasName("OLE.LPCOLESTR")]string szPropertyName, [ComAliasName("OLE.LPCOLESTR")]string szValue);
        void Start();
    }
}
