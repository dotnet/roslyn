// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    [Guid("6F05B225-83BF-40A3-A32A-47FA8443E868")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsFlightEvents
    {
        void OnFlightsChanged();
    }

    [Guid("61A8FB20-45DF-11E5-A151-FEFF819CDC9F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsExperimentationService
    {
        [DispId(1610678272)]
        Array AllEnabledCachedFlights { get; }

        [return: ComAliasName("EnvDTE.ULONG_PTR")]
        uint AdviseFlightEvents(IVsFlightEvents flightSink);
        bool IsCachedFlightEnabled([ComAliasName("OLE.LPCOLESTR")] string flightName);
        IVsTask IsFlightEnabledAsync([ComAliasName("OLE.LPCOLESTR")] string flightName);
        void Start();
        void UnadviseFlightEvents([ComAliasName("EnvDTE.ULONG_PTR")] uint cookie);
    }

    [Guid("DFF66CB5-603C-4716-89BD-24BD0E8C172C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface SVsExperimentationService
    {
    }
}