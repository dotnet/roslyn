// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultExperimentationService()
        {
        }

        public bool IsExperimentEnabled(string experimentName) => false;
    }

    internal static class WellKnownExperimentNames
    {
        public const string PartialLoadMode = "Roslyn.PartialLoadMode";
        public const string TypeImportCompletion = "Roslyn.TypeImportCompletion";
        public const string TargetTypedCompletionFilter = "Roslyn.TargetTypedCompletionFilter";
        public const string TriggerCompletionInArgumentLists = "Roslyn.TriggerCompletionInArgumentLists";
        public const string SQLiteInMemoryWriteCache = "Roslyn.SQLiteInMemoryWriteCache";

        /// <remarks>
        /// From https://devdiv.visualstudio.com/DevDiv/_git/VSLanguageServerClient?path=%2Fsrc%2Fproduct%2FRemoteLanguage%2FImpl%2FFeatures%2FDiagnostics%2FRemoteDiagnosticsBrokerHelper.cs&amp;version=GBdevelop&amp;line=28&amp;lineEnd=29&amp;lineStartColumn=1&amp;lineEndColumn=1&amp;lineStyle=plain
        /// </remarks>
        public const string LspPullDiagnostics = "VS.LSPPullModelDiagnosticVSLS";
    }
}
