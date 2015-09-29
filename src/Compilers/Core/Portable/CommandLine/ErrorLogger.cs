// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.Serialization.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used for logging all compiler diagnostics into a given <see cref="Stream"/>.
    /// This logger is responsible for closing the given stream on <see cref="Dispose"/>.
    /// </summary>
    internal partial class ErrorLogger : IDisposable
    {
        // Internal for testing purposes.
        internal const string OutputFormatVersion = "0.1";

        private const string indentDelta = "  ";
        private const char groupStartChar = '{';
        private const char groupEndChar = '}';
        private const char listStartChar = '[';
        private const char listEndChar = ']';

        private readonly StreamWriter _writer;
        private readonly DataContractJsonSerializer _jsonStringSerializer;

        private string _currentIndent;
        private bool _reportedAnyIssues;

        public ErrorLogger(Stream stream, string toolName, string toolFileVersion, Version toolAssemblyVersion)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.Position == 0);

            _writer = new StreamWriter(stream);
            _jsonStringSerializer = new DataContractJsonSerializer(typeof(string));
            _currentIndent = string.Empty;
            _reportedAnyIssues = false;

            WriteHeader(toolName, toolFileVersion, toolAssemblyVersion);
        }

        private void WriteHeader(string toolName, string toolFileVersion, Version toolAssemblyVersion)
        {
            StartGroup();

            WriteSimpleKeyValuePair(WellKnownStrings.OutputFormatVersion, OutputFormatVersion, isFirst: true);

            var toolInfo = GetToolInfo(toolName, toolFileVersion, toolAssemblyVersion);
            WriteKeyValuePair(WellKnownStrings.ToolInfo, toolInfo, isFirst: false);

            WriteKey(WellKnownStrings.Issues, isFirst: false);
            StartList();
        }

        private Value GetToolInfo(string toolName, string toolFileVersion, Version toolAssemblyVersion)
        {
            var builder = ArrayBuilder<KeyValuePair<string, Value>>.GetInstance();
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.ToolName, toolName));
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.ToolAssemblyVersion, toolAssemblyVersion.ToString(fieldCount: 3)));
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.ToolFileVersion, GetToolFileVersionSubStr(toolFileVersion)));
            return Value.Create(builder.ToImmutableAndFree(), this);
        }

        private string GetToolFileVersionSubStr(string toolFileVersion)
        {
            // Our log format specifies that fileVersion can have at most 3 fields.

            int count = 0;
            for (int i = 0; i < toolFileVersion.Length; i++)
            {
                if (toolFileVersion[i] == '.')
                {
                    if (count == 2)
                    {
                        return toolFileVersion.Substring(0, i);
                    }

                    count++;
                }
            }

            return toolFileVersion;
        }

        internal static void LogDiagnostic(Diagnostic diagnostic, CultureInfo culture, ErrorLogger errorLogger)
        {
            if (errorLogger != null)
            {
#pragma warning disable RS0013 // We need to invoke Diagnostic.Descriptor here to log all the metadata properties of the diagnostic.
                var issue = new Issue(diagnostic.Id, diagnostic.GetMessage(culture),
                    diagnostic.Descriptor.Description.ToString(culture), diagnostic.Descriptor.Title.ToString(culture),
                    diagnostic.Category, diagnostic.Descriptor.HelpLinkUri, diagnostic.IsEnabledByDefault, diagnostic.IsSuppressed,
                    diagnostic.DefaultSeverity, diagnostic.Severity, diagnostic.WarningLevel, diagnostic.Location,
                    diagnostic.AdditionalLocations, diagnostic.CustomTags, diagnostic.Properties);
#pragma warning restore RS0013

                errorLogger.LogIssue(issue);
            }
        }

        private void LogIssue(Issue issue)
        {
            var issueValue = GetIssueValue(issue);
            WriteValue(issueValue, isFirst: !_reportedAnyIssues, valueInList: true);
            _reportedAnyIssues = true;
        }

        private Value GetIssueValue(Issue issue)
        {
            var builder = ArrayBuilder<KeyValuePair<string, Value>>.GetInstance();
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.DiagnosticId, issue.Id));

            var locationsValue = GetLocationsValue(issue.Location, issue.AdditionalLocations);
            builder.Add(KeyValuePair.Create(WellKnownStrings.Locations, locationsValue));

            var message = string.IsNullOrEmpty(issue.Message) ? WellKnownStrings.None : issue.Message;
            var description = issue.Description;
            if (string.IsNullOrEmpty(description))
            {
                builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.FullMessage, message));
            }
            else
            {
                builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.ShortMessage, message));
                builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.FullMessage, description));
            }

            var propertiesValue = GetPropertiesValue(issue);
            builder.Add(KeyValuePair.Create(WellKnownStrings.Properties, propertiesValue));

            return Value.Create(builder.ToImmutableAndFree(), this);
        }

        private Value GetLocationsValue(Location location, IReadOnlyList<Location> additionalLocations)
        {
            var builder = ArrayBuilder<Value>.GetInstance();

            var locationValue = GetLocationValue(location);
            if (locationValue != null)
            {
                builder.Add(locationValue);
            }

            if (additionalLocations?.Count > 0)
            {
                foreach (var additionalLocation in additionalLocations)
                {
                    locationValue = GetLocationValue(additionalLocation);
                    if (locationValue != null)
                    {
                        builder.Add(locationValue);
                    }
                }
            }

            return Value.Create(builder.ToImmutableAndFree(), this);
        }

        private Value GetLocationValue(Location location)
        {
            if (location.SourceTree == null)
            {
                return null;
            }

            var builder = ArrayBuilder<KeyValuePair<string, Value>>.GetInstance();
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.LocationSyntaxTreePath, location.SourceTree.FilePath));

            var spanInfoValue = GetSpanInfoValue(location.GetLineSpan());
            builder.Add(KeyValuePair.Create(WellKnownStrings.LocationSpanInfo, spanInfoValue));

            var coreLocationValue = Value.Create(builder.ToImmutableAndFree(), this);

            // Our log format requires this to be wrapped.
            var wrapperList = Value.Create(ImmutableArray.Create(coreLocationValue), this);
            var wrapperKvp = KeyValuePair.Create(WellKnownStrings.Location, wrapperList);
            return Value.Create(ImmutableArray.Create(wrapperKvp), this);
        }

        private Value GetSpanInfoValue(FileLinePositionSpan lineSpan)
        {
            var builder = ArrayBuilder<KeyValuePair<string, Value>>.GetInstance();
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.LocationSpanStartLine, lineSpan.StartLinePosition.Line));
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.LocationSpanStartColumn, lineSpan.StartLinePosition.Character));
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.LocationSpanEndLine, lineSpan.EndLinePosition.Line));
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.LocationSpanEndColumn, lineSpan.EndLinePosition.Character));
            return Value.Create(builder.ToImmutableAndFree(), this);
        }

        private Value GetPropertiesValue(Issue issue)
        {
            var builder = ArrayBuilder<KeyValuePair<string, Value>>.GetInstance();

            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.Severity, issue.Severity.ToString()));
            if (issue.Severity == DiagnosticSeverity.Warning)
            {
                builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.WarningLevel, issue.WarningLevel.ToString()));
            }

            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.DefaultSeverity, issue.DefaultSeverity.ToString()));

            if (!string.IsNullOrEmpty(issue.Title))
            {
                builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.Title, issue.Title));
            }

            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.Category, issue.Category));

            if (!string.IsNullOrEmpty(issue.HelpLink))
            {
                builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.HelpLink, issue.HelpLink));
            }

            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.IsEnabledByDefault, issue.IsEnabledByDefault.ToString()));
            builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.IsSuppressedInSource, issue.IsSuppressedInSource.ToString()));

            if (issue.CustomTags.Count > 0)
            {
                builder.Add(CreateSimpleKeyValuePair(WellKnownStrings.CustomTags, issue.CustomTags.WhereNotNull().Join(";")));
            }

            if (issue.CustomProperties.Length > 0)
            {
                var customPropertiesBuilder = ArrayBuilder<KeyValuePair<string, Value>>.GetInstance();
                foreach (var kvp in issue.CustomProperties)
                {
                    customPropertiesBuilder.Add(CreateSimpleKeyValuePair(kvp.Key, kvp.Value));
                }

                var customPropertiesValue = Value.Create(customPropertiesBuilder.ToImmutableAndFree(), this);
                builder.Add(KeyValuePair.Create(WellKnownStrings.CustomProperties, customPropertiesValue));
            }

            return Value.Create(builder.ToImmutableAndFree(), this);
        }

        #region Helper methods for core logging

        private void WriteKeyValuePair(KeyValuePair<string, Value> kvp, bool isFirst)
        {
            WriteKeyValuePair(kvp.Key, kvp.Value, isFirst);
        }

        private void WriteKeyValuePair(string key, Value value, bool isFirst)
        {
            WriteKey(key, isFirst);
            WriteValue(value);
        }

        private void WriteSimpleKeyValuePair(string key, string value, bool isFirst)
        {
            WriteKey(key, isFirst);
            WriteValue(value);
        }

        private void WriteKey(string key, bool isFirst)
        {
            StartNewEntry(isFirst);
            _writer.Write($"\"{key}\": ");
        }

        private void WriteValue(Value value, bool isFirst = true, bool valueInList = false)
        {
            if (!isFirst || valueInList)
            {
                StartNewEntry(isFirst);
            }

            value.Write();
        }

        private void WriteValue(string value)
        {
            _writer.Flush();
            _jsonStringSerializer.WriteObject(_writer.BaseStream, value);
        }

        private void WriteValue(int value)
        {
            _writer.Write(value);
        }

        private void StartNewEntry(bool isFirst)
        {
            if (!isFirst)
            {
                _writer.WriteLine(',');
            }
            else
            {
                _writer.WriteLine();
            }

            _writer.Write(_currentIndent);
        }

        private void StartGroup()
        {
            StartGroupOrListCommon(groupStartChar);
        }

        private void EndGroup()
        {
            EndGroupOrListCommon(groupEndChar);
        }

        private void StartList()
        {
            StartGroupOrListCommon(listStartChar);
        }

        private void EndList()
        {
            EndGroupOrListCommon(listEndChar);
        }

        private void StartGroupOrListCommon(char startChar)
        {
            _writer.Write(startChar);
            IncreaseIndentation();
        }

        private void EndGroupOrListCommon(char endChar)
        {
            _writer.WriteLine();
            DecreaseIndentation();
            _writer.Write(_currentIndent + endChar);
        }

        private void IncreaseIndentation()
        {
            _currentIndent += indentDelta;
        }

        private void DecreaseIndentation()
        {
            _currentIndent = _currentIndent.Substring(indentDelta.Length);
        }

        private KeyValuePair<string, Value> CreateSimpleKeyValuePair(string key, string value)
        {
            var stringValue = Value.Create(value, this);
            return KeyValuePair.Create(key, stringValue);
        }

        private KeyValuePair<string, Value> CreateSimpleKeyValuePair(string key, int value)
        {
            var intValue = Value.Create(value, this);
            return KeyValuePair.Create(key, intValue);
        }

        #endregion

        public void Dispose()
        {
            // End issues list.
            EndList();

            // End dictionary for log file key-value pairs.
            EndGroup();

            _writer.Dispose();
        }
    }
}
