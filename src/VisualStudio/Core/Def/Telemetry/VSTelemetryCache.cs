// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal static class VSTelemetryCache
    {
        private const string EventPrefix = "vs/ide/vbcs/";
        private const string PropertyPrefix = "vs.ide.vbcs.";

        // these don't have concurrency limit on purpose to reduce chance of lock contention. 
        // if that becomes a problem - by showing up in our perf investigation, then we will consider adding concurrency limit.
        private static readonly ConcurrentDictionary<Key, string> s_eventMap = new ConcurrentDictionary<Key, string>();
        private static readonly ConcurrentDictionary<Key, string> s_propertyMap = new ConcurrentDictionary<Key, string>();

        public static string GetEventName(this FunctionId functionId, string eventKey = null)
        {
            return s_eventMap.GetOrAdd(new Key(functionId, eventKey), CreateEventName);
        }

        public static string GetPropertyName(this FunctionId functionId, string propertyKey)
        {
            return s_propertyMap.GetOrAdd(new Key(functionId, propertyKey), CreatePropertyName);
        }

        private static string CreateEventName(Key key)
        {
            return (EventPrefix + Enum.GetName(typeof(FunctionId), key.FunctionId).Replace('_', '/') + (key.ItemKey == null ? string.Empty : ("/" + key.ItemKey))).ToLowerInvariant();
        }

        private static string CreatePropertyName(Key key)
        {
            return (PropertyPrefix + Enum.GetName(typeof(FunctionId), key.FunctionId).Replace('_', '.') + "." + key.ItemKey).ToLowerInvariant();
        }

        private struct Key : IEquatable<Key>
        {
            public readonly FunctionId FunctionId;
            public readonly string ItemKey;

            public Key(FunctionId functionId, string itemKey)
            {
                this.FunctionId = functionId;
                this.ItemKey = itemKey;
            }

            public override int GetHashCode()
            {
                return Hash.Combine((int)FunctionId, ItemKey?.GetHashCode() ?? 0);
            }

            public override bool Equals(object obj)
            {
                return obj is Key && Equals((Key)obj);
            }

            public bool Equals(Key key)
            {
                return this is { FunctionId: key.FunctionId, ItemKey: key.ItemKey };
            }
        }
    }
}
