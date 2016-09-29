﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableDiagnosticListener : IDiagnosticListener
    {
        public int ListeningCount;
        public int ConnectionCount;
        public int CompletedCount;
        public DateTime? LastProcessedTime;
        public TimeSpan? KeepAlive;
        public bool HasDetectedBadConnection;
        public bool HitKeepAliveTimeout;
        public event EventHandler Listening;

        public void ConnectionListening()
        {
            ListeningCount++;
            Listening?.Invoke(this, EventArgs.Empty);
        }

        public void ConnectionReceived()
        {
            ConnectionCount++;
        }

        public void ConnectionCompleted(int count)
        {
            CompletedCount += count;
            LastProcessedTime = DateTime.Now;
        }

        public void UpdateKeepAlive(TimeSpan timeSpan)
        {
            KeepAlive = timeSpan;
        }

        public void ConnectionRudelyEnded()
        {
            HasDetectedBadConnection = true;
        }

        public void KeepAliveReached()
        {
            HitKeepAliveTimeout = true;
        }
    }
}
