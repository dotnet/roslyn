﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Called when one or more connections have completed processing.  The number of connections
        /// processed is provided in <paramref name="count"/>.
        /// </summary>
        void ConnectionProcessed(int count);

        /// <summary>
        /// Called when a bad client connection was detected and the server will be shutting down as a 
        /// result.
        /// </summary>
        void DetectedBadConnection();

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

        public void ConnectionProcessed(int count)
        {
        }

        public void DetectedBadConnection()
        {
        }

        public void KeepAliveReached()
        {
        }
    }
}
