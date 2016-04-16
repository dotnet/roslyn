// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// An interface to be implemented in the SolutionExplorerShim project to register the stuff needed there.
    /// </summary>
    internal interface IAnalyzerNodeSetup
    {
        void Initialize(IServiceProvider serviceProvider);
        void Unregister();
    }
}
