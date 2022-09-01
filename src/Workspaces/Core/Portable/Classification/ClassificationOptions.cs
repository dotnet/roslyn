// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Classification
{
    [DataContract]
    internal readonly struct ClassificationOptions
    {
        [ExportOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                ClassifyReassignedVariables);

            public static PerLanguageOption2<bool> ClassifyReassignedVariables =
               new(nameof(ClassificationOptions), nameof(ClassifyReassignedVariables), defaultValue: false,
                   storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ClassificationOptions)}.{nameof(ClassifyReassignedVariables)}"));
        }

        [DataMember(Order = 0)]
        public readonly bool ClassifyReassignedVariables;

        public ClassificationOptions(bool classifyReassignedVariables)
        {
            ClassifyReassignedVariables = classifyReassignedVariables;
        }

        public static ClassificationOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static ClassificationOptions From(OptionSet options, string language)
            => new(options.GetOption(Metadata.ClassifyReassignedVariables, language));
    }
}
