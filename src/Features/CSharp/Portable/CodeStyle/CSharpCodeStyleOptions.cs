// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static class CSharpCodeStyleOptions
    {
        internal const string FeatureName = "CSharpCodeStyle";

        // TODO: get sign off on public api changes.
        public static readonly Option<bool> UseVarWhenDeclaringLocals = new Option<bool>(FeatureName, "UseVarWhenDeclaringLocals", defaultValue: true);

        public static readonly Option<SimpleCodeStyleOption> UseImplicitTypeForIntrinsicTypes = new Option<SimpleCodeStyleOption>(FeatureName, nameof(UseImplicitTypeForIntrinsicTypes), defaultValue: SimpleCodeStyleOption.Default);
        public static readonly Option<SimpleCodeStyleOption> UseImplicitTypeWhereApparent = new Option<SimpleCodeStyleOption>(FeatureName, nameof(UseImplicitTypeWhereApparent), defaultValue: SimpleCodeStyleOption.Default);
        public static readonly Option<SimpleCodeStyleOption> UseImplicitTypeWherePossible = new Option<SimpleCodeStyleOption>(FeatureName, nameof(UseImplicitTypeWherePossible), defaultValue: SimpleCodeStyleOption.Default);
    }
}
