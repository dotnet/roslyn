// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDynamicObjectCreationExpression
    {
        public override MethodSymbol? Constructor => null;
        public override bool Expanded => false;
        public override ImmutableArray<int> ArgsToParamsOpt => default;
        public override BitVector DefaultArguments => default;
    }
}
