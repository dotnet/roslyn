// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    /// <summary>
    /// This interface allows the host to set up a telemetry service during package initialization.
    /// </summary>
    internal interface IRoslynTelemetrySetup
    {
        void Initialize(IServiceProvider serviceProvider);
    }
}
