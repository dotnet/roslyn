// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal readonly record struct ExtractMethodOptions(
        bool DontPutOutOrRefOnStruct)
    {
        public static readonly ExtractMethodOptions Default
          = new(
              DontPutOutOrRefOnStruct: Metadata.DontPutOutOrRefOnStruct.DefaultValue);

        public static ExtractMethodOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static ExtractMethodOptions From(OptionSet options, string language)
          => new(
              DontPutOutOrRefOnStruct: options.GetOption(Metadata.DontPutOutOrRefOnStruct, language));

        [ExportSolutionOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                DontPutOutOrRefOnStruct);

            private const string FeatureName = "ExtractMethodOptions";

            public static readonly PerLanguageOption2<bool> DontPutOutOrRefOnStruct = new(FeatureName, "DontPutOutOrRefOnStruct", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Don't Put Out Or Ref On Strcut")); // NOTE: the spelling error is what we've shipped and thus should not change
        }
    }
}
