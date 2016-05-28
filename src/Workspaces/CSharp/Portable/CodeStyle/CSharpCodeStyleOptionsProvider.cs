// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    [ExportOptionProvider, Shared]
    internal class CSharpCodeStyleOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = new List<IOption>
            {
                CodeStyleOptions.QualifyFieldAccess,
                CodeStyleOptions.QualifyPropertyAccess,
                CodeStyleOptions.QualifyMethodAccess,
                CodeStyleOptions.QualifyEventAccess,
                CSharpCodeStyleOptions.UseVarWhenDeclaringLocals,
                CSharpCodeStyleOptions.UseImplicitTypeWherePossible,
                CSharpCodeStyleOptions.UseImplicitTypeWhereApparent,
                CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes
            }.ToImmutableArray();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
