// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts a <see cref="WithExpressionSyntax"/> into a <see cref="BoundExpression"/>.
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindWithExpression(WithExpressionSyntax syntax, DiagnosticBag diagnostics)
        {
            var receiver = BindRValueWithoutTargetType(syntax.Expression, diagnostics);
            var receiverType = receiver.Type;

            var lookupResult = LookupResult.GetInstance();
            bool hasErrors = false;

            if (receiverType is null || receiverType.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_InvalidWithReceiverType, syntax.Expression.Location);
                receiverType = CreateErrorType();
            }

            MethodSymbol? cloneMethod = null;
            if (!receiverType.IsErrorType())
            {
                HashSet<DiagnosticInfo>? useSiteDiagnostics = null;

                cloneMethod = SynthesizedRecordClone.FindValidCloneMethod(receiverType, ref useSiteDiagnostics);
                if (cloneMethod is null)
                {
                    hasErrors = true;
                    diagnostics.Add(ErrorCode.ERR_NoSingleCloneMethod, syntax.Expression.Location, receiverType);
                }
                else if (cloneMethod.GetUseSiteDiagnostic() is DiagnosticInfo info)
                {
                    (useSiteDiagnostics ??= new HashSet<DiagnosticInfo>()).Add(info);
                }

                diagnostics.Add(syntax.Expression, useSiteDiagnostics);
            }

            var initializer = BindInitializerExpression(
                syntax.Initializer,
                receiverType,
                syntax.Expression,
                diagnostics);

            // N.B. Since we only don't parse nested initializers in syntax there should be no extra
            // errors we need to check for here.

            return new BoundWithExpression(
                syntax,
                receiver,
                cloneMethod,
                initializer,
                receiverType,
                hasErrors: hasErrors);
        }
    }
}
