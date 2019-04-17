// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    class LiveShareConstants
    {
        // The service name for an LSP server implemented using Roslyn designed to be used with the Roslyn client
        public const string RoslynContractName = "Roslyn";
        // The service name for an LSP server implemented using Roslyn designed to be used with the LSP SDK client
        public const string RoslynLSPSDKContractName = "RoslynLSPSDK";
    }
}
