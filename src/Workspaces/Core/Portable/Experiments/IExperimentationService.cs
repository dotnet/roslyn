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
        public bool ReturnValue = false;
        public bool IsExperimentEnabled(string experimentName) => ReturnValue;
    }

    internal static class WellKnownExperimentNames
    {
        public const string RoslynOOP64bit = nameof(RoslynOOP64bit);
        public const string CompletionAPI = nameof(CompletionAPI);
        public const string PartialLoadMode = "Roslyn.PartialLoadMode";
        public const string TypeImportCompletion = "Roslyn.TypeImportCompletion";
        public const string TargetTypedCompletionFilter = "Roslyn.TargetTypedCompletionFilter";
        public const string RoslynToggleBlockComment = "Roslyn.ToggleBlockComment";
        public const string RoslynToggleLineComment = "Roslyn.ToggleLineComment";
        public const string NativeEditorConfigSupport = "Roslyn.NativeEditorConfigSupport";
    }
}
