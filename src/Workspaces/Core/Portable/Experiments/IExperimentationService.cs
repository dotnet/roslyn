﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public DefaultExperimentationService()
        {
        }

        public bool IsExperimentEnabled(string experimentName) => ReturnValue;
    }

    internal static class WellKnownExperimentNames
    {
        public const string RoslynOOP64bit = nameof(RoslynOOP64bit);
        public const string PartialLoadMode = "Roslyn.PartialLoadMode";
        public const string TypeImportCompletion = "Roslyn.TypeImportCompletion";
        public const string TargetTypedCompletionFilter = "Roslyn.TargetTypedCompletionFilter";
        public const string NativeEditorConfigSupport = "Roslyn.NativeEditorConfigSupport";

        // Syntactic LSP experiment treatments.
        public const string SyntacticExp_LiveShareTagger_Remote = "Roslyn.LspTagger";
        public const string SyntacticExp_LiveShareTagger_TextMate = "Roslyn.TextMateTagger";
    }
}
