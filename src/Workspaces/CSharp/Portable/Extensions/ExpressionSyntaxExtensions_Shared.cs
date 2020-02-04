// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ExpressionSyntaxExtensions
    {
        public static bool IsRightSideOfQualifiedName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.QualifiedName) && ((QualifiedNameSyntax)expression.Parent).Right == expression;
        }

        public static bool IsMemberAccessExpressionName(this ExpressionSyntax expression)
        {
            return (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) && ((MemberAccessExpressionSyntax)expression.Parent).Name == expression) ||
                   IsMemberBindingExpressionName(expression);
        }

        private static bool IsMemberBindingExpressionName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.MemberBindingExpression) &&
                ((MemberBindingExpressionSyntax)expression.Parent).Name == expression;
        }
    }
}
