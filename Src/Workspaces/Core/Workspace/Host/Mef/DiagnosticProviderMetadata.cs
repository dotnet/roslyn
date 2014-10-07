// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DiagnosticProviderMetadata : ILanguagesMetadata
    {
        public string[] Languages { get; private set; }

        public DiagnosticProviderMetadata(IDictionary<string, object> data)
        {
            this.Languages = (string[])data.GetValueOrDefault("Languages");
        }
    }
}
