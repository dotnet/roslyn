// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
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
        public bool ReturnValue = false;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultExperimentationService()
        {
        }

        public bool IsExperimentEnabled(string experimentName) => ReturnValue;
    }

    internal static class WellKnownExperimentNames
    {
        public const string PartialLoadMode = "Roslyn.PartialLoadMode";
        public const string TypeImportCompletion = "Roslyn.TypeImportCompletion";
        public const string TargetTypedCompletionFilter = "Roslyn.TargetTypedCompletionFilter";
        public const string TriggerCompletionInArgumentLists = "Roslyn.TriggerCompletionInArgumentLists";
        public const string SQLiteInMemoryWriteCache = "Roslyn.SQLiteInMemoryWriteCache";
    }
}
