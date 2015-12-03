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
        private readonly IEnumerable<IOption> _options = new List<IOption>
            {
                SimplificationOptions.PreferAliasToQualification,
                SimplificationOptions.PreferOmittingModuleNamesInQualification,
                SimplificationOptions.PreferImplicitTypeInference,
                SimplificationOptions.PreferImplicitTypeInLocalDeclaration,
                SimplificationOptions.AllowSimplificationToGenericType,
                SimplificationOptions.AllowSimplificationToBaseType,
                SimplificationOptions.QualifyFieldAccess,
                SimplificationOptions.QualifyPropertyAccess,
                SimplificationOptions.QualifyMethodAccess,
                SimplificationOptions.QualifyEventAccess,
                SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
                SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess
            }.ToImmutableArray();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
