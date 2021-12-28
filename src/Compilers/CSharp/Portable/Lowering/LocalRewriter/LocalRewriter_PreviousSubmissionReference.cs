// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            var targetType = (ImplicitNamedTypeSymbol)node.Type;
            Debug.Assert(targetType.TypeKind == TypeKind.Submission);
            Debug.Assert(_factory.TopLevelMethod is { IsStatic: false });
            Debug.Assert(_factory.CurrentType is { });

            Debug.Assert(_previousSubmissionFields != null);

            var syntax = node.Syntax;
            var targetScriptReference = _previousSubmissionFields.GetOrMakeField(targetType);
            var thisReference = new BoundThisReference(syntax, _factory.CurrentType);
            return new BoundFieldAccess(syntax, thisReference, targetScriptReference, ConstantValue.NotAvailable);
        }
    }
}
