// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// MEF metadata class used to find exports declared for a specific <see cref="IEventListener"/>.
    /// </summary>
    internal class EventListenerMetadata : WorkspaceKindMetadata
    {
        public string Service { get; }

        public EventListenerMetadata(IDictionary<string, object> data) :
            base(data)
        {
            this.Service = (string)data.GetValueOrDefault("Service");
        }

        public EventListenerMetadata(string service, params string[] workspaceKinds) :
            base(workspaceKinds)
        {
            if (workspaceKinds?.Length == 0)
            {
                throw new ArgumentException(nameof(workspaceKinds));
            }

            this.Service = service ?? throw new ArgumentException(nameof(service));
        }
    }
}
