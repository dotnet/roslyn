// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopRules.DiagnosticProviders.Usage;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.FxCopRules.DiagnosticProviders.Utilities
{
    public static class DescendIntoHelper
    {
        // determines if the search descends only into the node's type-level children.
        internal static bool DescendIntoOnlyTypeLevelDeclaration(SyntaxNode node)
        {
            return !node.IsKind(SyntaxKind.ConstructorDeclaration)
                && !node.IsKind(SyntaxKind.DestructorDeclaration)
                && !node.IsKind(SyntaxKind.MethodDeclaration);
        }
    }
}
