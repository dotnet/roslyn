// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class LocalRewriter
    {
        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            Debug.Assert(previousSubmissionFields != null);
            Debug.Assert(!factory.CurrentMethod.IsStatic);

            var syntax = node.Syntax;
            var hostObjectReference = previousSubmissionFields.GetHostObjectField();
            var thisReference = new BoundThisReference(syntax, factory.CurrentClass);
            return new BoundFieldAccess(syntax, thisReference, hostObjectReference, constantValueOpt: null);
        }
    }
}
