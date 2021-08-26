// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundMethodGroup : BoundMethodOrPropertyGroup
    {
        public BoundMethodGroup(
            SyntaxNode syntax,
            ImmutableArray<TypeWithAnnotations> typeArgumentsOpt,
            BoundExpression receiverOpt,
            string name,
            ImmutableArray<MethodSymbol> methods,
            LookupResult lookupResult,
            BoundMethodGroupFlags flags,
            Binder binder,
            bool hasErrors = false)
            : this(syntax, typeArgumentsOpt, name, methods, lookupResult.SingleSymbolOrDefault, lookupResult.Error, flags, signature: CreateSignature(binder, syntax), receiverOpt, lookupResult.Kind, hasErrors)
        {
            Signature?.SetExpression(this);
        }

        private static FunctionSignature? CreateSignature(Binder binder, SyntaxNode syntax)
        {
            return syntax.IsFeatureEnabled(MessageID.IDS_FeatureInferredDelegateType) ?
                new FunctionSignature(binder, static (binder, expr) => binder.GetMethodGroupDelegateType((BoundMethodGroup)expr, out _)) :
                null;
        }

        public MemberAccessExpressionSyntax? MemberAccessExpressionSyntax
        {
            get
            {
                return this.Syntax as MemberAccessExpressionSyntax;
            }
        }

        public SyntaxNode NameSyntax
        {
            get
            {
                var memberAccess = this.MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    return memberAccess.Name;
                }
                else
                {
                    return this.Syntax;
                }
            }
        }

        public BoundExpression? InstanceOpt
        {
            get
            {
                if (this.ReceiverOpt == null || this.ReceiverOpt.Kind == BoundKind.TypeExpression)
                {
                    return null;
                }
                else
                {
                    return this.ReceiverOpt;
                }
            }
        }

        public bool SearchExtensionMethods
        {
            get
            {
                return (this.Flags & BoundMethodGroupFlags.SearchExtensionMethods) != 0;
            }
        }
    }
}
