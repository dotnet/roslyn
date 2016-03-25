// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitTupleCreationExpression(BoundTupleCreationExpression node)
        {
            var rewrittenArguments = VisitList(node.Arguments);
            return new BoundObjectCreationExpression(node.Syntax, node.ConstructorOpt, rewrittenArguments);
        }
    }
}
