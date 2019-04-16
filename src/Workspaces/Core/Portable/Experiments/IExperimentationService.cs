// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Experiments
{
    internal interface IExperimentationService : IWorkspaceService
    {
        bool IsExperimentEnabled(ExperimentName experimentName);
    }

    [ExportWorkspaceService(typeof(IExperimentationService)), Shared]
    internal class DefaultExperimentationService : IExperimentationService
    {
        public bool IsExperimentEnabled(ExperimentName experimentName) => false;
    }

    internal static class WellKnownExperimentNames
    {
        public readonly static ExperimentName RoslynOOP64bit = nameof(RoslynOOP64bit);
        public readonly static ExperimentName CompletionAPI = nameof(CompletionAPI);
        public readonly static ExperimentName PartialLoadMode = nameof(PartialLoadMode);
        public readonly static ExperimentName ToggleBlockComment = nameof(ToggleBlockComment);
    }

    internal struct ExperimentName
    {
        public const string DefaultPrefix = "Roslyn.";

        /// <summary>
        /// experiement name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// experiment name with a prefix
        /// 
        /// we need this since some experimentation service requires certain prefix in its name.
        /// </summary>
        public string NameWithPrefix { get; }

        /// <summary>
        /// create name and name with a prefix from given experiement name
        /// </summary>
        public ExperimentName(string experimentName)
            : this(experimentName, DefaultPrefix + experimentName)
        {
        }

        public ExperimentName(string name, string nameWithPrefix)
        {
            Contract.ThrowIfFalse(nameWithPrefix.IndexOf(".") > 0);

            Name = name;
            NameWithPrefix = nameWithPrefix;
        }

        public static implicit operator ExperimentName(string experimentName)
        {
            return new ExperimentName(experimentName);
        }
    }
}
