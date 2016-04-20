// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

///////////////////////////////////////////////////////////////////////////////
//
//
///////////////////////////////////////////////////////////////////////////////

#pragma warning disable 3001

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Shell.Interop
{
    [ComImport()]
    [ComVisible(false)]
    [Guid("C1F63D0C-4CAE-4907-BE74-EEB75D386ECB")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsSqm
    {
        void GetSessionStartTime(
            [Out] out System.Runtime.InteropServices.ComTypes.FILETIME time
            );
        void GetFlags(
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 flags
            );
        void SetFlags(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 flags
            );
        void ClearFlags(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 flags
            );
        void AddItemToStream(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void SetDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        // OBSOLETE IN SQMAPI.DLL. DO NOT CALL.
        void GetDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 value
            );
        void EnterTaggedAssert(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dwTag,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dwPossibleBuild,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dwActualBuild
            );
        void RecordCmdData(
            [In] ref Guid pguidCmdGroup,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void GetHashOfGuid(
            [In] ref Guid hashGuid,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 resultantHash
            );
        void GetHashOfString(
            [In, MarshalAs(UnmanagedType.BStr)] string hashString,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 resultantHash
            );
        void IncrementDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );

        void SetDatapointBits(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );

        void SetDatapointIfMax(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void SetDatapointIfMin(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void AddToDatapointAverage(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void StartDatapointTimer(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void RecordDatapointTimer(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void AccumulateDatapointTimer(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void AddTimerToDatapointAverage(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void AddArrayToStream(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4, SizeParamIndex = 2)] System.UInt32[] data,
            [In, MarshalAs(UnmanagedType.I4)] int count
        );
    }

    [ComImport()]
    [ComVisible(false)]
    [Guid("BE5F55EB-F02D-4217-BCB6-A290800AF6C4")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsSqm2
    {
        void SetBoolDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 fValue
            );

        void SetStringDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.BStr)] string strValue
            );

        void AddToStreamDWord(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 cTuple,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );

        void AddToStreamString(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 cTuple,
            [In, MarshalAs(UnmanagedType.BStr)] string strValue
            );

        void GetObfuscatedString(
            [In, MarshalAs(UnmanagedType.BStr)] string input,
            [Out, MarshalAs(UnmanagedType.BStr)] out string output
            );
    }

    [ComImport()]
    [ComVisible(false)]
    [Guid("B17A7D4A-C1A3-45A2-B916-826C3ABA067E")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsSqmMulti
    {
        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool GetOptInStatus();
        void UnloadSessions(
            );
        void EndAllSessionsAndAbortUploads(
            );
        void BeginSession(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionType,
            [In, MarshalAs(UnmanagedType.VariantBool)] System.Boolean alwaysSend,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 sessionHandle
            );
        void EndSession(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle
            );
        void RegisterSessionHandle(
            [In] ref Guid sessionIdentifier,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dwSessionHandle
        );
        [return: MarshalAs(UnmanagedType.U4)]
        int GetSessionHandleByIdentifier(
            [In] ref Guid sessionIdentifier
         );
        void GetSessionStartTime(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [Out] out System.Runtime.InteropServices.ComTypes.FILETIME time
            );
        Guid GetGlobalSessionGuid();
        [return: MarshalAs(UnmanagedType.U4)]
        int GetGlobalSessionHandle();
        void SetGlobalSessionGuid(
            [In] ref Guid pguidSessionGuid
            );
        void GetFlags(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 flags
            );
        void SetFlags(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 flags
            );
        void ClearFlags(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 flags
            );
        void SetDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void SetBoolDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 fValue
            );
        void SetStringDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.BStr)] string strValue
            );
        void SetDatapointBits(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void IncrementDatapoint(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );

        void SetDatapointIfMax(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void SetDatapointIfMin(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void AddToDatapointAverage(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void StartDatapointTimer(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void RecordDatapointTimer(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void AccumulateDatapointTimer(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void AddTimerToDatapointAverage(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID
            );
        void AddItemToStream(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void AddArrayToStream(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4, SizeParamIndex = 2)] System.UInt32[] data,
            [In, MarshalAs(UnmanagedType.I4)] int count
        );
        void AddToStreamDWord(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 cTuple,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void AddToStreamString(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 cTuple,
            [In, MarshalAs(UnmanagedType.BStr)] string strValue
            );
        void RecordCmdData(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 sessionHandle,
            [In] ref Guid pguidCmdGroup,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 dataPointID,
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 value
            );
        void GetHashOfGuid(
            [In] ref Guid hashGuid,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 resultantHash
            );
        void GetHashOfString(
            [In, MarshalAs(UnmanagedType.BStr)] string hashString,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 resultantHash
            );
        void SetProperty(
             [In, MarshalAs(UnmanagedType.U4)] System.UInt32 propid,
             [In] ref Guid varKey,
             [In] object varValue
            );
        void Get64BitHashOfString(
            [In, MarshalAs(UnmanagedType.BStr)] string hashString,
            [Out, MarshalAs(UnmanagedType.U8)] out System.UInt64 resultantHash
            );
    }

    [ComImport()]
    [ComVisible(false)]
    [Guid("16be4288-950b-4265-b0dc-280b89ca9979")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsSqmOptinManager
    {
        void GetOptinStatus(
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 optinStatus,
            [Out, MarshalAs(UnmanagedType.U4)] out System.UInt32 preferences
            );

        void SetOptinStatus(
            [In, MarshalAs(UnmanagedType.U4)] System.UInt32 optinStatus
            );
    }

    [ComImport()]
    [ComVisible(false)]
    [Guid("2508FDF0-EF80-4366-878E-C9F024B8D981")]
    internal interface SVsLog
    {
    }
}
