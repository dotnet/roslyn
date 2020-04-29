// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class DebuggingSessionTelemetry
    {
        internal readonly struct Data
        {
            public readonly ImmutableArray<EditSessionTelemetry.Data> EditSessionData;
            public readonly int EmptyEditSessionCount;

            public Data(DebuggingSessionTelemetry telemetry)
            {
                EditSessionData = telemetry._editSessionData.ToImmutableArray();
                EmptyEditSessionCount = telemetry._emptyEditSessionCount;
            }
        }

        private readonly object _guard = new object();

        private readonly List<EditSessionTelemetry.Data> _editSessionData;
        private int _emptyEditSessionCount;

        public DebuggingSessionTelemetry()
            => _editSessionData = new List<EditSessionTelemetry.Data>();

        public Data GetDataAndClear()
        {
            lock (_guard)
            {
                var data = new Data(this);
                _editSessionData.Clear();
                _emptyEditSessionCount = 0;
                return data;
            }
        }

        public void LogEditSession(EditSessionTelemetry.Data editSessionTelemetryData)
        {
            lock (_guard)
            {
                if (editSessionTelemetryData.IsEmpty)
                {
                    _emptyEditSessionCount++;
                }
                else
                {
                    _editSessionData.Add(editSessionTelemetryData);
                }
            }
        }
    }
}
