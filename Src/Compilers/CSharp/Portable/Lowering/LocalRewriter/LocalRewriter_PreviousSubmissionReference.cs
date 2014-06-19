// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class LocalRewriter
    {
        public override BoundNode VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            var targetType = (ImplicitNamedTypeSymbol)node.Type;
            Debug.Assert(targetType.TypeKind == TypeKind.Submission);
            Debug.Assert(!factory.CurrentMethod.IsStatic);

            Debug.Assert(previousSubmissionFields != null);

            var syntax = node.Syntax;
            var targetScriptReference = previousSubmissionFields.GetOrMakeField(targetType);
            var thisReference = new BoundThisReference(syntax, factory.CurrentClass);
            return new BoundFieldAccess(syntax, thisReference, targetScriptReference, ConstantValue.NotAvailable);
        }

    }
}
