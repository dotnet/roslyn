// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : ServiceHubServiceBase
    {
        public CodeAnalysisService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            StartService();
        }
    }
}
