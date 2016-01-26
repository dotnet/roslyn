// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundMethodGroup : BoundMethodOrPropertyGroup
    {
        public BoundMethodGroup(
            CSharpSyntaxNode syntax,
            ImmutableArray<TypeSymbolWithAnnotations> typeArgumentsOpt,
            BoundExpression receiverOpt,
            string name,
            ImmutableArray<MethodSymbol> methods,
            LookupResult lookupResult,
            BoundMethodGroupFlags flags,
            bool hasErrors = false)
            : this(syntax, typeArgumentsOpt, name, methods, lookupResult.SingleSymbolOrDefault, lookupResult.Error, flags, receiverOpt, lookupResult.Kind, hasErrors)
        {
        }

        public MemberAccessExpressionSyntax MemberAccessExpressionSyntax
        {
            get
            {
                return this.Syntax as MemberAccessExpressionSyntax;
            }
        }

        public CSharpSyntaxNode NameSyntax
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

        public BoundExpression InstanceOpt
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
