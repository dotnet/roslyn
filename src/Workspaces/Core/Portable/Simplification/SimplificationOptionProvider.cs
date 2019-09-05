// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Simplification
{
    [ExportOptionProvider, Shared]
    internal class SimplificationOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public SimplificationOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            SimplificationOptions.PreferAliasToQualification,
                SimplificationOptions.PreferOmittingModuleNamesInQualification,
                SimplificationOptions.PreferImplicitTypeInference,
                SimplificationOptions.PreferImplicitTypeInLocalDeclaration,
                SimplificationOptions.AllowSimplificationToGenericType,
                SimplificationOptions.AllowSimplificationToBaseType,
                SimplificationOptions.NamingPreferences);
    }
}
