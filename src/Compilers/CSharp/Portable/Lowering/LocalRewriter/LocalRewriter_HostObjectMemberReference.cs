// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            Debug.Assert(_previousSubmissionFields != null);
            Debug.Assert(_factory.TopLevelMethod is { IsStatic: false });
            Debug.Assert(_factory.CurrentType is { });

            var syntax = node.Syntax;
            var hostObjectReference = _previousSubmissionFields.GetHostObjectField();
            var thisReference = new BoundThisReference(syntax, _factory.CurrentType);
            return new BoundFieldAccess(syntax, thisReference, hostObjectReference, constantValueOpt: null);
        }
    }
}
