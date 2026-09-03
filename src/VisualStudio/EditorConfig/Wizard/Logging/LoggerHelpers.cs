// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging
{
    internal static class LoggerHelpers
    {
        public const string ExceptionName = "vs/ide/vbcs/editorconfig/exception";
        public const string ErrorName = "vs/ide/vbcs/editorconfig/error";
        private const string EventPrefix = "vs/ide/vbcs/editorconfig/";
        private const string PropertyPrefix = "vs.ide.vbcs.editorconfig.";
        private static readonly ConcurrentDictionary<int, string> s_eventMap = new();
        private static readonly ConcurrentDictionary<(int id, string name), string> s_propertyMap = new();

        public static string GetEventName(EventId id)
             => s_eventMap.GetOrAdd(id.AsInt(), number => EventPrefix + GetTelemetryName(id, separator: '/'));

        public static string GetOperationName(OperationId id)
            => s_eventMap.GetOrAdd(id.AsInt(), number => EventPrefix + GetTelemetryName(id, separator: '/'));

        public static string GetUserTaskName(UserTask id)
            => s_eventMap.GetOrAdd(id.AsInt(), number => EventPrefix + GetTelemetryName(id, separator: '/'));

        public static string GetPropertyName(EventId id, string name)
            => s_propertyMap.GetOrAdd((id.AsInt(), name), key => PropertyPrefix + GetTelemetryName(id, separator: '.') + "." + name.ToLowerInvariant());

        public static string GetPropertyName(OperationId id, string name)
            => s_propertyMap.GetOrAdd((id.AsInt(), name), key => PropertyPrefix + GetTelemetryName(id, separator: '.') + "." + name.ToLowerInvariant());

        public static string GetPropertyName(UserTask id, string name)
            => s_propertyMap.GetOrAdd((id.AsInt(), name), key => PropertyPrefix + GetTelemetryName(id, separator: '.') + "." + name.ToLowerInvariant());

        public static string GetTelemetryName(EventId id, char separator)
            => Enum.GetName(typeof(EventId), id)!.Replace('_', separator).ToLowerInvariant();

        public static string GetTelemetryName(OperationId id, char separator)
            => Enum.GetName(typeof(OperationId), id)!.Replace('_', separator).ToLowerInvariant();

        public static string GetTelemetryName(UserTask id, char separator)
            => Enum.GetName(typeof(UserTask), id)!.Replace('_', separator).ToLowerInvariant();

        public static void SetProperties<T>(TelemetryEvent telemetryEvent, EventId eventId, ILogMessage<T> messages) where T : ILogMessageData
        {
            var seenNames = new HashSet<string>();
            foreach (var messageData in messages.GetMessageData())
            {
                var message = messageData.GetMessage();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var propertyName = GetPropertyName(eventId, messageData.Name);
                    if (seenNames.Add(propertyName))
                    {
                        telemetryEvent.Properties.Add(propertyName, message);
                    }
                }
            }
        }

        public static void SetProperties<T>(TelemetryEvent telemetryEvent, OperationId operationId, ILogMessage<T> messages) where T : ILogMessageData
        {
            var seenNames = new HashSet<string>();
            foreach (var messageData in messages.GetMessageData())
            {
                var message = messageData.GetMessage();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var propertyName = GetPropertyName(operationId, messageData.Name);
                    if (seenNames.Add(propertyName))
                    {
                        telemetryEvent.Properties.Add(propertyName, message);
                    }
                }
            }
        }

        public static void SetProperties<T>(TelemetryEvent telemetryEvent, UserTask operationId, ILogMessage<T> messages) where T : ILogMessageData
        {
            var seenNames = new HashSet<string>();
            foreach (var messageData in messages.GetMessageData())
            {
                var message = messageData.GetMessage();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var propertyName = GetPropertyName(operationId, messageData.Name);
                    if (seenNames.Add(propertyName))
                    {
                        telemetryEvent.Properties.Add(propertyName, message);
                    }
                }
            }
        }
    }
}

