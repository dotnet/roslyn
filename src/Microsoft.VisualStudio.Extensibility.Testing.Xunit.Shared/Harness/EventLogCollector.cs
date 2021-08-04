// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.Eventing.Reader;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Helper class to read the Application Event Log for Watson and .NET Runtime entries.
    /// </summary>
    internal static class EventLogCollector
    {
        /// <summary>
        /// The name of the Event Log to query.
        /// </summary>
        private const string EventLogName = "Application";

        /// <summary>
        /// We want to get either the entries for the past day or the last 5 (whichever has a greater count).
        /// </summary>
        private const int MinimumEntries = 5;

        /// <summary>
        /// We want to get either the entries for the past day or the last 5 (whichever has a greater count).
        /// </summary>
        private const int DaysToGetEventsFor = 1;

        /// <summary>
        /// We don't want to add events that are older than a week.
        /// </summary>
        private const int MaxDaysToGetEventsFor = 7;

        /// <summary>
        /// For Watson, the Provider Name in the Event Log is "Windows Error Reporting".
        /// </summary>
        private const string WatsonProviderName = "Windows Error Reporting";

        /// <summary>
        /// For Watson, the Event Id in the Event Log that we are interested in is 1001.
        /// </summary>
        private const int WatsonEventId = 1001;

        /// <summary>
        /// Each entry in the EventLog has 22 Properties: 0-bucketId, 1-eventTypeId, 2-eventName, 3-response, 4-cabId, 5:14-bucketParameters P1:P10,
        /// 15-attachedFiles, 16-location, 17-analysisSymbol, 18-recheck, 19-reportId, 20-reportStatus, 21-hashedBucket.
        /// </summary>
        private const int WatsonEventLogEntryPropertyCount = 22;

        /// <summary>
        /// FaultBucket is the first property on the log entry.
        /// </summary>
        private const int FaultBucketIndex = 0;

        /// <summary>
        /// For .DotNetRuntime, the Provider Name in the Event Log.
        /// </summary>
        private const string DotNetProviderName = ".NET Runtime";

        /// <summary>
        /// The Event Id in the Event Log for .DotNetRuntime that we want to scope down to
        /// 1023 - ERT_UnmanagedFailFast, 1025 - ERT_ManagedFailFast, 1026 - ERT_UnhandledException, 1027 - ERT_StackOverflow, 1028 - ERT_CodeContractFailed.
        /// </summary>
        private static readonly ImmutableArray<int> DotNetEventId = ImmutableArray.Create(1023, 1024, 1025, 1026, 1027, 1028);

        /// <summary>
        /// List of EventNames to exclude from our search in the Event Log.
        /// </summary>
        private static readonly HashSet<string> ExcludedEventNames = new HashSet<string>()
        {
            "VisualStudioNonFatalErrors",
            "VisualStudioNonFatalErrors2",
        };

        /// <summary>
        /// List of VS EXEs to search in the Event Log for.
        /// </summary>
        private static readonly HashSet<string> VsRelatedExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "devenv.exe",
            "csc.exe",
            "csi.exe",
            "git.exe",
            "msbuild.exe",
            "MSBuildTaskHost.exe",
            "mspdbsrv.exe",
            "MStest.exe",
            "RunTests.exe",
            "ServiceHub.Host.CLR.exe",
            "ServiceHub.Host.CLR.x64.exe",
            "ServiceHub.Host.CLR.x86.exe",
            "ServiceHub.IdentityHost.exe",
            "ServiceHub.RoslynCodeAnalysisService.exe",
            "ServiceHub.RoslynCodeAnalysisService32.exe",
            "ServiceHub.RoslynCodeAnalysisServiceS.exe",
            "ServiceHub.SettingsHost.exe",
            "ServiceHub.VSDetouredHost.exe",
            "vbc.exe",
            "VBCSCompiler.exe",
            "VStest.Console.Exe",
            "VSTest.DiscoveryEngine.exe",
            "VSTest.DiscoveryEngine.x86.exe",
            "vstest.executionengine.appcontainer.exe",
            "vstest.executionengine.appcontainer.x86.exe",
            "vstest.executionengine.clr20.exe",
            "VSTest.executionEngine.exe",
            "VSTest.executionEngine.x86.exe",
            "xunit.console.exe",
            "xunit.console.x86.exe",
        };

        /// <summary>
        /// Get the WER entries for VS and VS related EXEs from the Event Log and write them to a file.
        /// </summary>
        internal static void TryWriteWatsonEntriesToFile(string filePath)
        {
            try
            {
                // Use a HashSet to make sure the entries we add aren't duplicates (calls the Equals override from FeedbackItemWatsonEntry)
                var watsonEntries = new HashSet<FeedbackItemWatsonEntry>();

                // We need to search in the Application Event Log, since that's where Watson logs entries
                var eventLogQuery = new EventLogQuery(EventLogName, PathType.LogName)
                {
                    // Read events in descending order, so we can get either the last 5 entries or the past day of entries, whichever has a bigger count
                    ReverseDirection = true,
                };

                var eventLogReader = new EventLogReader(eventLogQuery);
                EventRecord eventLogRecord;
                var watsonEntriesCount = 0;
                while ((eventLogRecord = eventLogReader.ReadEvent()) != null)
                {
                    // We only want the last 5 entries or the past day of entries, whichever has a bigger count
                    if (IsLastDayOrLastFiveRecentEntry(eventLogRecord, watsonEntriesCount))
                    {
                        // Filter the entries by Watson specific ones for VS EXEs
                        if (IsValidWatsonEntry(eventLogRecord))
                        {
                            var entry = new FeedbackItemWatsonEntry(eventLogRecord);
                            watsonEntries.Add(entry);

                            // If the entry doesn't have a valid BucketId, we don't want it to count towards the maxCount we send
                            if (!string.IsNullOrWhiteSpace(GetEventRecordPropertyToString(eventLogRecord, FaultBucketIndex)))
                            {
                                watsonEntriesCount++;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (watsonEntries.Any())
                {
                    var watsonEntriesStringBuilder = new StringBuilder();
                    foreach (var entry in watsonEntries)
                    {
                        watsonEntriesStringBuilder.AppendLine($"Event Time (UTC): {entry.EventTime}");
                        watsonEntriesStringBuilder.AppendLine($"Application Name: {entry.ApplicationName}");
                        watsonEntriesStringBuilder.AppendLine($"Application Version: {entry.ApplicationVersion}");
                        watsonEntriesStringBuilder.AppendLine($"Faulting Module: {entry.FaultingModule}");
                        watsonEntriesStringBuilder.AppendLine($"Faulting Module Version: {entry.FaultingModuleVersion}");
                        watsonEntriesStringBuilder.AppendLine($"Event Name: {entry.EventName}");
                        watsonEntriesStringBuilder.AppendLine($"Cab Id: {entry.CabId}");
                        watsonEntriesStringBuilder.AppendLine($"Fault Bucket: {entry.FaultBucket}");
                        watsonEntriesStringBuilder.AppendLine($"Hashed Bucket: {entry.HashedBucket}");
                        watsonEntriesStringBuilder.AppendLine($"Watson Report Id: {entry.WatsonReportId}");
                        watsonEntriesStringBuilder.AppendLine();
                    }

                    File.WriteAllText(filePath, watsonEntriesStringBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(filePath, ex.ToString());
            }
        }

        /// <summary>
        /// Get the .NET Runtime entries from the Event Log and write them to a file.
        /// </summary>
        internal static void TryWriteDotNetEntriesToFile(string filePath)
        {
            try
            {
                var dotNetEntries = new HashSet<FeedbackItemDotNetEntry>();

                // We need to search in the Application Event Log, since that's where .NetRuntime logs entries
                var eventLogQuery = new EventLogQuery(EventLogName, PathType.LogName)
                {
                    // Read events in descending order, so we can get either the last 5 entries or the past day of entries, whichever has a bigger count
                    ReverseDirection = true,
                };

                var eventLogReader = new EventLogReader(eventLogQuery);
                EventRecord eventLogRecord;
                while ((eventLogRecord = eventLogReader.ReadEvent()) != null)
                {
                    // We only want the last 5 entries or the past day of entries, whichever has a bigger count
                    if (IsLastDayOrLastFiveRecentEntry(eventLogRecord, dotNetEntries.Count))
                    {
                        // Filter the entries by .NetRuntime specific ones
                        if (IsValidDotNetEntry(eventLogRecord, out var entry))
                        {
                            dotNetEntries.Add(entry);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (dotNetEntries.Any())
                {
                    var dotNetEntriesStringBuilder = new StringBuilder();
                    foreach (var entry in dotNetEntries)
                    {
                        dotNetEntriesStringBuilder.AppendLine($"Event Time (UTC): {entry.EventTime}");
                        dotNetEntriesStringBuilder.AppendLine($"Event ID: {entry.EventId}");
                        dotNetEntriesStringBuilder.AppendLine($"Data: {entry.Data.Replace("\n", "\r\n")}");
                        dotNetEntriesStringBuilder.AppendLine();
                    }

                    File.WriteAllText(filePath, dotNetEntriesStringBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(filePath, ex.ToString());
            }
        }

        /// <summary>
        /// Returns true if this is one of the last 5 entries over the past week or the past day of entries, whichever has a bigger count.
        /// </summary>
        /// <param name="eventLogRecord">Event entry to be checked.</param>
        /// <param name="entriesCount">List of already valid entries.</param>
        private static bool IsLastDayOrLastFiveRecentEntry(EventRecord eventLogRecord, int entriesCount)
        {
            // This is local time (it will be later converted to UTC when we send the feedback)
            if (eventLogRecord.TimeCreated.HasValue
                && (eventLogRecord.TimeCreated.Value > DateTime.Now.AddDays(-MaxDaysToGetEventsFor))
                && ((eventLogRecord.TimeCreated.Value > DateTime.Now.AddDays(-DaysToGetEventsFor)) || (entriesCount < MinimumEntries)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifies if an entry is a valid Watson one by checking:
        /// the provider, if it's for VS EXEs or the installer EXEs, and it's not a VisualStudioNonFatalErrors or VisualStudioNonFatalErrors2.
        /// </summary>
        /// <param name="eventLogRecord">Entry to be checked.</param>
        private static bool IsValidWatsonEntry(EventRecord eventLogRecord)
        {
            if (StringComparer.InvariantCultureIgnoreCase.Equals(eventLogRecord.ProviderName, WatsonProviderName)
                && (eventLogRecord.Id == WatsonEventId)
                && (eventLogRecord.Properties.Count >= WatsonEventLogEntryPropertyCount)
                && (!ExcludedEventNames.Contains(GetEventRecordPropertyToString(eventLogRecord, FeedbackItemWatsonEntry.EventNameIndex)))
                && VsRelatedExes.Contains(GetEventRecordPropertyToString(eventLogRecord, FeedbackItemWatsonEntry.ApplicationNameIndex)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifies if an entry is a valid .NET one by checking:
        /// the provider, if it's for certain event log IDs and for VS related EXEs.
        /// </summary>
        /// <param name="eventLogRecord">Entry to be checked.</param>
        private static bool IsValidDotNetEntry(EventRecord eventLogRecord, out FeedbackItemDotNetEntry dotNetEntry)
        {
            if (StringComparer.InvariantCultureIgnoreCase.Equals(eventLogRecord.ProviderName, DotNetProviderName)
                 && DotNetEventId.Contains(eventLogRecord.Id))
            {
                dotNetEntry = new FeedbackItemDotNetEntry(eventLogRecord);
                foreach (var app in VsRelatedExes)
                {
                    if (dotNetEntry.Data.IndexOf(app, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            dotNetEntry = null;
            return false;
        }

        /// <summary>
        /// Given the EventRecord and the index in it, get its value as a string (empty if it's null).
        /// </summary>
        /// <param name="eventLogRecord">EventRecord.</param>
        /// <param name="index">Index in the EventRecord.</param>
        /// <returns>string if not null or string.Empty.</returns>
        internal static string GetEventRecordPropertyToString(EventRecord eventLogRecord, int index)
        {
            if (eventLogRecord.Properties[index].Value == null)
            {
                return string.Empty;
            }
            else
            {
                return eventLogRecord.Properties[index].Value.ToString();
            }
        }
    }
}
