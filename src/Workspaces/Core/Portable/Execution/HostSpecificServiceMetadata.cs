// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// MEF metadata class used for finding <see cref="IHostSpecificService"/> exports.
    /// </summary>
    internal class HostSpecificServiceMetadata 
    {
        public string ServiceType { get; }
        public string Host { get; }
        public string Layer { get; }

        public IReadOnlyDictionary<string, object> Data { get; }

        public HostSpecificServiceMetadata(IDictionary<string, object> data)
        {
            this.ServiceType = (string)data.GetValueOrDefault("ServiceType");
            this.Host = (string)data.GetValueOrDefault("Host");
            this.Layer = (string)data.GetValueOrDefault("Layer");
        }
    }
}
