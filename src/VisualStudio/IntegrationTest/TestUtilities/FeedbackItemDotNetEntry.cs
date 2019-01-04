// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Mapper for the .NetRuntime entry in the Event Log
    /// </summary>
    [DataContract]
    internal class FeedbackItemDotNetEntry
    {
        /// <summary>
        /// The time the event happend (UTC)
        /// </summary>
        [DataMember(Name = "eventTime")]
        public DateTime EventTime { get; set; }

        /// <summary>
        /// The .NET Runtime event id (this is set by .NET and we get it from the Event Log, so we can better differenciate between them)
        /// As defined in CLR code:  ndp\clr\src\vm\eventreporter.cpp, these IDs are:
        /// 1023 - ERT_UnmanagedFailFast, 1025 - ERT_ManagedFailFast, 1026 - ERT_UnhandledException, 1027 - ERT_StackOverflow, 1028 - ERT_CodeContractFailed
        /// </summary>
        [DataMember(Name = "eventId")]
        public int EventId { get; set; }

        /// <summary>
        /// The event log properties to be passed as one string. E.g.
        /// Application: CSAv.exe, Framework version: v4.0.30319,
        /// Description: The application requested termination through System.Environment.FailFast(string message)
        /// Stack: at CSAv.Program.GetModuleFileName(IntPtr, Int32, Int32)
        /// </summary>
        [DataMember(Name = "data")]
        public string Data { get; set; }

        /// <summary>
        /// Constructor for the FeedbackItemDotNetEntry based on an EventRecord from the EventLog
        /// </summary>
        public FeedbackItemDotNetEntry(EventRecord eventLogRecord)
        {
            EventTime = eventLogRecord.TimeCreated.Value.ToUniversalTime();
            EventId = eventLogRecord.Id;
            Data = string.Join(";", eventLogRecord.Properties.Select(pr => pr.Value ?? string.Empty));
        }

        /// <summary>
        /// Used to make sure we aren't adding dupe entries to the list of Watson entries
        /// </summary>
        public override bool Equals(object obj)
        {
            if ((obj is FeedbackItemDotNetEntry dotNetEntry)
                && (EventId == dotNetEntry.EventId)
                && (Data == dotNetEntry.Data))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return EventId.GetHashCode() ^ Data.GetHashCode();
        }
    }
}
