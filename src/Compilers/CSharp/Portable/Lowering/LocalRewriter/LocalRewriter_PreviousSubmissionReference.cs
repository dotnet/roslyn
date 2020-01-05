// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Debug.Assert(!_factory.TopLevelMethod.IsStatic);

            Debug.Assert(_previousSubmissionFields != null);

            var syntax = node.Syntax;
            var targetScriptReference = _previousSubmissionFields.GetOrMakeField(targetType);
            var thisReference = new BoundThisReference(syntax, _factory.CurrentType);
            return new BoundFieldAccess(syntax, thisReference, targetScriptReference, ConstantValue.NotAvailable);
        }
    }
}
