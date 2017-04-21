// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Experiments
{
    internal interface IExperimentationService : IWorkspaceService
    {
        bool IsExperimentEnabled(string experimentName);
    }

    [ExportWorkspaceService(typeof(IExperimentationService)), Shared]
    internal class DefaultExperimentationService : IExperimentationService
    {
        public bool IsExperimentEnabled(string experimentName) => false;
    }
}