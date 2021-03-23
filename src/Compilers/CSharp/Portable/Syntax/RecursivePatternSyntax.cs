// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class RecursivePatternSyntax
    {
        public RecursivePatternSyntax Update(TypeSyntax? type, PositionalPatternClauseSyntax? positionalPatternClause, PropertyPatternClauseSyntax? propertyPatternClause, VariableDesignationSyntax? designation)
        {
            return Update(type, positionalPatternClause, this.LengthPatternClause, propertyPatternClause, designation);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static RecursivePatternSyntax RecursivePattern(TypeSyntax? type, PositionalPatternClauseSyntax? positionalPatternClause, PropertyPatternClauseSyntax? propertyPatternClause, VariableDesignationSyntax? designation)
        {
            return RecursivePattern(type, positionalPatternClause, lengthPatternClause: null, propertyPatternClause, designation);
        }
    }
}
