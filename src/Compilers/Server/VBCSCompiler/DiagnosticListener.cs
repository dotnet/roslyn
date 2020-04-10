﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal interface IDiagnosticListener
    {
        /// <summary>
        /// Called when the server updates the keep alive value.
        /// </summary>
        void UpdateKeepAlive(TimeSpan timeSpan);

        /// <summary>
        /// Called each time the server listens for new connections.
        /// </summary>
        void ConnectionListening();

        /// <summary>
        /// Called when a connection to the server occurs.
        /// </summary>
        void ConnectionReceived();

        /// <summary>
        /// Called when a connection has finished processing and notes the <paramref name="reason"/>
        /// </summary>
        void ConnectionCompleted(CompletionReason reason);

        /// <summary>
        /// Called when the server is shutting down because the keep alive timeout was reached.
        /// </summary>
        void KeepAliveReached();
    }

    internal sealed class EmptyDiagnosticListener : IDiagnosticListener
    {
        public void UpdateKeepAlive(TimeSpan timeSpan)
        {
        }

        public void ConnectionListening()
        {
        }

        public void ConnectionReceived()
        {
        }

        public void ConnectionCompleted(CompletionReason reason)
        {
        }

        public void KeepAliveReached()
        {
        }
    }
}
