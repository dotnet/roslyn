// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Instances of this <see cref="SemanticModel"/> can be exposed to external consumers.
    /// Other types of <see cref="CSharpSemanticModel"/> are not designed for direct exposure 
    /// and their implementation might not be able to handle external requests properly.
    /// </summary>
    internal abstract partial class PublicSemanticModel : CSharpSemanticModel
    {
        protected AttributeSemanticModel CreateModelForAttribute(Binder enclosingBinder, AttributeSyntax attribute, MemberSemanticModel containingModel)
        {
            AliasSymbol aliasOpt;
            var attributeType = (NamedTypeSymbol)enclosingBinder.BindType(attribute.Name, BindingDiagnosticBag.Discarded, out aliasOpt).Type;

            // For attributes where a nameof could introduce some type parameters, we need to track the attribute target
            Symbol? attributeTarget = getAttributeTarget(attribute.Parent?.Parent);

            return AttributeSemanticModel.Create(
                this,
                attribute,
                attributeType,
                aliasOpt,
                attributeTarget,
                enclosingBinder,
                containingModel?.GetRemappedSymbols());

            Symbol? getAttributeTarget(SyntaxNode? targetSyntax)
            {
                return targetSyntax switch
                {
                    BaseMethodDeclarationSyntax or
                        LocalFunctionStatementSyntax or
                        ParameterSyntax or
                        TypeParameterSyntax or
                        IndexerDeclarationSyntax or
                        AccessorDeclarationSyntax or
                        DelegateDeclarationSyntax => GetDeclaredSymbolForNode(targetSyntax).GetSymbol(),
                    AnonymousFunctionExpressionSyntax anonymousFunction => GetSymbolInfo(anonymousFunction).Symbol.GetSymbol(),
                    _ => null
                };
            }
        }

        internal sealed override SemanticModel ContainingPublicModelOrSelf => this;
    }
}
