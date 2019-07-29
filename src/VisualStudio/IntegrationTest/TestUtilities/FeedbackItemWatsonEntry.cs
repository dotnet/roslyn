// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Mapper for the Watson entry in the Event Log
    /// </summary>
    [DataContract]
    internal class FeedbackItemWatsonEntry
    {
        /// <summary>
        /// The time the event happend (UTC)
        /// </summary>
        [DataMember(Name = "eventTime")]
        public DateTime EventTime { get; }

        /// <summary>
        /// Bucket Id
        /// </summary>
        [DataMember(Name = "faultBucket")]
        public string FaultBucket { get; }

        /// <summary>
        /// Bucket Hash (might replace ID which would be deprecated, so sending for future proofing)
        /// </summary>
        [DataMember(Name = "hashedBucket")]
        public string HashedBucket { get; }

        /// <summary>
        /// Watson Report ID
        /// </summary>
        [DataMember(Name = "watsonReportId")]
        public string WatsonReportId { get; }

        /// <summary>
        /// The name of the event (some possible ones: "AppHangB1", "AppHangXProcB1", "MoAppHang","MoAppHangXProc","AppCrash","Crash32","Crash64","MoAppCrash","BEX","BEX64","clr20r3","MoBEX"
        /// </summary>
        [DataMember(Name = "eventName")]
        public string EventName { get; }

        /// <summary>
        /// The CAB unique ID (can be empty - 0)
        /// </summary>
        [DataMember(Name = "cabId")]
        public string CabId { get; }

        /// <summary>
        /// The name of the application causing the event (we have a list of VS EXEs that we grab for), e.g. "devenv.exe"
        /// </summary>
        [DataMember(Name = "applicationName")]
        public string ApplicationName { get; }

        /// <summary>
        /// The version of the application causing the event, e.g. "14.0.23107.0"
        /// </summary>
        [DataMember(Name = "applicationVersion")]
        public string ApplicationVersion { get; }

        /// <summary>
        /// The faulting module (what inside the app is causing the event), e.g. "ntdll.dll"
        /// </summary>
        [DataMember(Name = "faultingModule")]
        public string FaultingModule { get; }

        /// <summary>
        /// The faulting module version
        /// </summary>
        [DataMember(Name = "faultModuleVersion")]
        public string FaultingModuleVersion { get; }

        /// <summary>
        /// FaultBucket is the first property on the log entry
        /// </summary>
        private const int FaultBucketIndex = 0;

        /// <summary>
        /// EventName index in the log entry properties (2)
        /// </summary>
        internal const int EventNameIndex = 2;

        /// <summary>
        /// CabId index in the log entry properties
        /// </summary>
        private const int CabIdIndex = 4;

        /// <summary>
        /// Application name is contained in the P1 bucket parameter
        /// </summary>
        internal const int ApplicationNameIndex = 5;

        /// <summary>
        /// Application version is the P2 bucket parameter
        /// </summary>
        private const int ApplicationVersionIndex = 6;

        /// <summary>
        /// Faulting module is the P4 bucket parameter
        /// </summary>
        private const int FaultingModuleIndex = 8;

        /// <summary>
        /// Faulting module version is the P5 bucket parameter
        /// </summary>
        private const int FaultingModuleVersionindex = 9;

        /// <summary>
        /// WatsonReportId index in the log entry properties
        /// </summary>
        private const int WatsonReportIdIndex = 19;

        /// <summary>
        /// HashedBucket index in the log entry properties
        /// </summary>
        private const int HashedBucketIndex = 21;

        /// <summary>
        /// Constructor for a FeedbackItemWatsonEntry based on an EventRecord for future easiness of reading and modifying
        /// </summary>
        public FeedbackItemWatsonEntry(EventRecord eventLogRecord)
        {
            EventTime = eventLogRecord.TimeCreated.Value.ToUniversalTime();
            FaultBucket = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, FaultBucketIndex);
            HashedBucket = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, HashedBucketIndex);
            WatsonReportId = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, WatsonReportIdIndex);
            EventName = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, EventNameIndex);
            CabId = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, CabIdIndex);
            ApplicationName = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, ApplicationNameIndex);
            ApplicationVersion = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, ApplicationVersionIndex);
            FaultingModule = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, FaultingModuleIndex);
            FaultingModuleVersion = EventLogCollector.GetEventRecordPropertyToString(eventLogRecord, FaultingModuleVersionindex);
        }

        /// <summary>
        /// Used to make sure we aren't adding dupe entries to the list of Watson entries
        /// </summary>
        public override bool Equals(object obj)
        {
            if ((obj is FeedbackItemWatsonEntry watsonEntry)
                && (EventName == watsonEntry.EventName)
                && (ApplicationName == watsonEntry.ApplicationName)
                && (ApplicationVersion == watsonEntry.ApplicationVersion)
                && (FaultingModule == watsonEntry.FaultingModule)
                && (FaultingModuleVersion == watsonEntry.FaultingModuleVersion))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return EventName.GetHashCode() ^ ApplicationName.GetHashCode() ^ ApplicationVersion.GetHashCode() ^ FaultingModule.GetHashCode() ^ FaultingModuleVersion.GetHashCode();
        }
    }
}
