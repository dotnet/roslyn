// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ReassignedVariable
{
    internal class ReassignedVariableOptions
    {
        public static PerLanguageOption2<bool> Underline =
           new PerLanguageOption2<bool>(nameof(ReassignedVariableOptions), nameof(Underline), defaultValue: false,
               storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ReassignedVariable.Underline"));
    }

    [ExportOptionProvider, Shared]
    internal class ReassignedVariableOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ReassignedVariableOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            ReassignedVariableOptions.Underline);
    }
}
