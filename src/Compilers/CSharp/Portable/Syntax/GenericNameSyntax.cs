// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class GenericNameSyntax
    {
        public bool IsUnboundGenericName
        {
            get
            {
                return this.TypeArgumentList.Arguments.Any(SyntaxKind.OmittedTypeArgument);
            }
        }

        internal override string ErrorDisplayName()
        {
            var pb = PooledStringBuilder.GetInstance();
            pb.Builder.Append(Identifier.ValueText).Append("<").Append(',', Arity - 1).Append(">");
            return pb.ToStringAndFree();
        }
    }
}
