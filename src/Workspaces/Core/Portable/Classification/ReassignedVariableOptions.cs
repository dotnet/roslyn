// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Classification
{
    internal class ClassificationOptions
    {
        public static PerLanguageOption2<bool> ClassifyReassignedVariables =
           new PerLanguageOption2<bool>(nameof(ClassificationOptions), nameof(ClassifyReassignedVariables), defaultValue: false,
               storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.{nameof(ClassificationOptions)}.{nameof(ClassifyReassignedVariables)}"));
    }

    [ExportOptionProvider, Shared]
    internal class ClassificationOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ClassificationOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            ClassificationOptions.ClassifyReassignedVariables);
    }
}
