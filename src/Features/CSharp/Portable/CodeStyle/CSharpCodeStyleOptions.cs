// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal static class CSharpCodeStyleOptions
    {
        internal const string FeatureName = "CSharpCodeStyle";

        // TODO (BalajiK): repurpose this instead of adding a new one below?
        public static readonly Option<bool> UseVarWhenDeclaringLocals = new Option<bool>(FeatureName, "UseVarWhenDeclaringLocals", defaultValue: true);

        public static readonly Option<bool> UseVarForIntrinsicTypes = new Option<bool>(FeatureName, "UseImplicitTypingForIntrinsics", defaultValue: false);
        public static readonly Option<bool> UseVarWhenTypeIsApparent = new Option<bool>(FeatureName, "UseImplicitTypingWhereApparent", defaultValue: false);
        public static readonly Option<bool> UseVarWherePossible = new Option<bool>(FeatureName, "UseImplicitTypingWherePossible", defaultValue: false);
    }
}
