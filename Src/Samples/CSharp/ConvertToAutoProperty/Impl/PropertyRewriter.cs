// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ConvertToAutoPropertyCS
{
    internal class PropertyRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel semanticModel;
        private readonly ISymbol backingField;
        private readonly PropertyDeclarationSyntax property;

        public PropertyRewriter(SemanticModel semanticModel, ISymbol backingField, PropertyDeclarationSyntax property)
        {
            this.semanticModel = semanticModel;
            this.backingField = backingField;
            this.property = property;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax name)
        {
            if (backingField != null)
            {
                if (name.Identifier.ValueText.Equals(backingField.Name))
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(name);

                    // Check binding info
                    if (symbolInfo.Symbol != null &&
                        Equals(symbolInfo.Symbol.OriginalDefinition, backingField))
                    {
                        name = name.WithIdentifier(
                            SyntaxFactory.Identifier(property.Identifier.ValueText));

                        return name.WithAdditionalAnnotations(Formatter.Annotation);
                    }
                }
            }

            return name;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == property)
            {
                // Add an annotation to format the new property.
                return ConvertToAutoProperty(propertyDeclaration).WithAdditionalAnnotations(Formatter.Annotation);
            }

            return base.VisitPropertyDeclaration(propertyDeclaration);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax field)
        {
            // Retrieve the symbol for the field
            if (field.Declaration.Variables.Count == 1)
            {
                if (object.Equals(semanticModel.GetDeclaredSymbol(field), backingField))
                {
                    return null;
                }
            }

            return field;
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax variable)
        {
            // Retrieve the symbol for the variable declarator
            var field = variable.Parent.Parent as FieldDeclarationSyntax;
            if (field != null && field.Declaration.Variables.Count == 1)
            {
                if (object.Equals(semanticModel.GetDeclaredSymbol(variable), backingField))
                {
                    return null;
                }
            }

            return variable;
        }

        private PropertyDeclarationSyntax ConvertToAutoProperty(PropertyDeclarationSyntax propertyDeclaration)
        {
            // Produce the new property.
            var newProperty = property
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List(new[]
                            {
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            })));

            return newProperty;
        }
    }
}