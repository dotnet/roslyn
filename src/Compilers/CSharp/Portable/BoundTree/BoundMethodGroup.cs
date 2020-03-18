// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
            bool hasErrors = false)
            : this(syntax, typeArgumentsOpt, name, methods, lookupResult.SingleSymbolOrDefault, lookupResult.Error, flags, receiverOpt, lookupResult.Kind, hasErrors)
        {
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
