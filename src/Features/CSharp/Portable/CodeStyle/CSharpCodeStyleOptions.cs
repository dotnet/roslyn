// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static class CSharpCodeStyleOptions
    {
        internal const string FeatureName = "CSharpCodeStyle";

        public static readonly Option<bool> UseVarWhenDeclaringLocals = new Option<bool>(FeatureName, "UseVarWhenDeclaringLocals", defaultValue: true);
    }
}
