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

        public static readonly Option<CodeStyleOption> UseVarForIntrinsicTypes = new Option<CodeStyleOption>(FeatureName, "UseImplicitTypingForIntrinsics", defaultValue: CodeStyleOption.Default);
        public static readonly Option<CodeStyleOption> UseVarWhenTypeIsApparent = new Option<CodeStyleOption>(FeatureName, "UseImplicitTypingWhereApparent", defaultValue: CodeStyleOption.Default);
        public static readonly Option<CodeStyleOption> UseVarWherePossible = new Option<CodeStyleOption>(FeatureName, "UseImplicitTypingWherePossible", defaultValue: CodeStyleOption.Default);
    }
}
