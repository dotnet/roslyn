// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpVarReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpVarReducer() : base(s_pool)
        {
        }

        public override bool IsApplicable(OptionSet optionSet)
            => optionSet.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes).Value ||
               optionSet.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent).Value ||
               optionSet.GetOption(CSharpCodeStyleOptions.VarElsewhere).Value;
    }
}
