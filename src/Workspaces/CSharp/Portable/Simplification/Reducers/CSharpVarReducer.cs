// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;

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
