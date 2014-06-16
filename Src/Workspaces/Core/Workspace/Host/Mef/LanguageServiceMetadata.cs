// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used for finding <see cref="ILanguageService"/> and <see cref="ILanguageServiceFactory"/> exports.
    /// </summary>
    internal class LanguageServiceMetadata : LanguageMetadata
    {
        public string ServiceType { get; private set; }
        public string Layer { get; private set; }

        public IReadOnlyDictionary<string, object> Data { get; private set; }
        
        public LanguageServiceMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.ServiceType = (string)data.GetValueOrDefault("ServiceType");
            this.Layer = (string)data.GetValueOrDefault("Layer");
            this.Data = (IReadOnlyDictionary<string, object>)data;
        }
    }
}