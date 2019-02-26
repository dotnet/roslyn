// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Experiments
{
    internal interface IExperimentationServiceFactory : IWorkspaceService
    {
        ValueTask<IExperimentationService> GetExperimentationServiceAsync(CancellationToken cancellationToken);
    }

    internal interface IExperimentationService
    {
        bool IsExperimentEnabled(string experimentName);
    }

    [ExportWorkspaceService(typeof(IExperimentationServiceFactory)), Shared]
    internal class DefaultExperimentationServiceFactory : IExperimentationServiceFactory, IExperimentationService
    {
        [ImportingConstructor]
        public DefaultExperimentationServiceFactory()
        {
        }

        public ValueTask<IExperimentationService> GetExperimentationServiceAsync(CancellationToken cancellationToken)
            => new ValueTask<IExperimentationService>(this);

        public bool IsExperimentEnabled(string experimentName) => false;
    }

    internal static class WellKnownExperimentNames
    {
        public const string RoslynOOP64bit = nameof(RoslynOOP64bit);
        public const string PartialLoadMode = "Roslyn.PartialLoadMode";
        public const string TypeImportCompletion = "Roslyn.TypeImportCompletion";
        public const string TargetTypedCompletionFilter = "Roslyn.TargetTypedCompletionFilter";
        public const string NativeEditorConfigSupport = "Roslyn.NativeEditorConfigSupport";
    }
}
