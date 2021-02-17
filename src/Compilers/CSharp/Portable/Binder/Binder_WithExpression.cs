﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private BoundExpression BindWithExpression(WithExpressionSyntax syntax, BindingDiagnosticBag diagnostics)
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
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);

                cloneMethod = SynthesizedRecordClone.FindValidCloneMethod(receiverType is TypeParameterSymbol typeParameter ? typeParameter.EffectiveBaseClass(ref useSiteInfo) : receiverType, ref useSiteInfo);
                if (cloneMethod is null)
                {
                    hasErrors = true;
                    diagnostics.Add(ErrorCode.ERR_NoSingleCloneMethod, syntax.Expression.Location, receiverType);
                }
                else
                {
                    cloneMethod.AddUseSiteInfo(ref useSiteInfo);
                }

                diagnostics.Add(syntax.Expression, useSiteInfo);
            }

            var initializer = BindInitializerExpression(
                syntax.Initializer,
                receiverType,
                syntax.Expression,
                isForNewInstance: true,
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
