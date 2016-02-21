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

        public static readonly Option<SimpleCodeStyleOption> UseVarForIntrinsicTypes = new Option<SimpleCodeStyleOption>(FeatureName, "UseImplicitTypingForIntrinsics", defaultValue: SimpleCodeStyleOption.Default);
        public static readonly Option<SimpleCodeStyleOption> UseVarWhenTypeIsApparent = new Option<SimpleCodeStyleOption>(FeatureName, "UseImplicitTypingWhereApparent", defaultValue: SimpleCodeStyleOption.Default);
        public static readonly Option<SimpleCodeStyleOption> UseVarWherePossible = new Option<SimpleCodeStyleOption>(FeatureName, "UseImplicitTypingWherePossible", defaultValue: SimpleCodeStyleOption.Default);
    }
}
