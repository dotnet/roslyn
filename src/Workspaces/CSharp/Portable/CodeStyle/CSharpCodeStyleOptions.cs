// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static class CSharpCodeStyleOptions
    {
        internal const string FeatureName = "CSharpCodeStyle";

        // TODO: get sign off on public api changes.
        public static readonly Option<bool> UseVarWhenDeclaringLocals = new Option<bool>(FeatureName, "UseVarWhenDeclaringLocals", defaultValue: true);

        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeForIntrinsicTypes = new Option<CodeStyleOption<bool>>(FeatureName, nameof(UseImplicitTypeForIntrinsicTypes), defaultValue: CodeStyleOption<bool>.Default);
        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWhereApparent = new Option<CodeStyleOption<bool>>(FeatureName, nameof(UseImplicitTypeWhereApparent), defaultValue: CodeStyleOption<bool>.Default);
        public static readonly Option<CodeStyleOption<bool>> UseImplicitTypeWherePossible = new Option<CodeStyleOption<bool>>(FeatureName, nameof(UseImplicitTypeWherePossible), defaultValue: CodeStyleOption<bool>.Default);
    }
}
