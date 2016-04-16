// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            Debug.Assert(_previousSubmissionFields != null);
            Debug.Assert(!_factory.CurrentMethod.IsStatic);

            var syntax = node.Syntax;
            var hostObjectReference = _previousSubmissionFields.GetHostObjectField();
            var thisReference = new BoundThisReference(syntax, _factory.CurrentType);
            return new BoundFieldAccess(syntax, thisReference, hostObjectReference, constantValueOpt: null);
        }
    }
}
