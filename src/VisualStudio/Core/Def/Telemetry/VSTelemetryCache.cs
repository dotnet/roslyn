// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => s_eventMap.GetOrAdd(new Key(functionId, eventKey), CreateEventName);

        public static string GetPropertyName(this FunctionId functionId, string propertyKey)
            => s_propertyMap.GetOrAdd(new Key(functionId, propertyKey), CreatePropertyName);

        private static string CreateEventName(Key key)
            => (EventPrefix + Enum.GetName(typeof(FunctionId), key.FunctionId).Replace('_', '/') + (key.ItemKey == null ? string.Empty : ("/" + key.ItemKey))).ToLowerInvariant();

        private static string CreatePropertyName(Key key)
            => (PropertyPrefix + Enum.GetName(typeof(FunctionId), key.FunctionId).Replace('_', '.') + "." + key.ItemKey).ToLowerInvariant();

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
                => Hash.Combine((int)FunctionId, ItemKey?.GetHashCode() ?? 0);

            public override bool Equals(object obj)
                => obj is Key && Equals((Key)obj);

            public bool Equals(Key key)
                => this.FunctionId == key.FunctionId && this.ItemKey == key.ItemKey;
        }
    }
}
