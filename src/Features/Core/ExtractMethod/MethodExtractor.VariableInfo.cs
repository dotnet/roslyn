// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected class VariableInfo
        {
            private readonly VariableSymbol variableSymbol;
            private readonly VariableStyle variableStyle;
            private readonly bool useAsReturnValue;

            public VariableInfo(
                VariableSymbol variableSymbol,
                VariableStyle variableStyle,
                bool useAsReturnValue = false)
            {
                this.variableSymbol = variableSymbol;
                this.variableStyle = variableStyle;
                this.useAsReturnValue = useAsReturnValue;
            }

            public bool UseAsReturnValue
            {
                get
                {
                    Contract.ThrowIfFalse(!this.useAsReturnValue || this.variableStyle.ReturnStyle.ReturnBehavior != ReturnBehavior.None);
                    return this.useAsReturnValue;
                }
            }

            public bool CanBeUsedAsReturnValue
            {
                get
                {
                    return this.variableStyle.ReturnStyle.ReturnBehavior != ReturnBehavior.None;
                }
            }

            public bool UseAsParameter
            {
                get
                {
                    return (!this.useAsReturnValue && this.variableStyle.ParameterStyle.ParameterBehavior != ParameterBehavior.None) ||
                           (this.useAsReturnValue && this.variableStyle.ReturnStyle.ParameterBehavior != ParameterBehavior.None);
                }
            }

            public ParameterBehavior ParameterModifier
            {
                get
                {
                    return this.useAsReturnValue ? this.variableStyle.ReturnStyle.ParameterBehavior : this.variableStyle.ParameterStyle.ParameterBehavior;
                }
            }

            public DeclarationBehavior GetDeclarationBehavior(CancellationToken cancellationToken)
            {
                if (this.useAsReturnValue)
                {
                    return this.variableStyle.ReturnStyle.DeclarationBehavior;
                }

                if (this.variableSymbol.GetUseSaferDeclarationBehavior(cancellationToken))
                {
                    return this.variableStyle.ParameterStyle.SaferDeclarationBehavior;
                }

                return this.variableStyle.ParameterStyle.DeclarationBehavior;
            }

            public ReturnBehavior ReturnBehavior
            {
                get
                {
                    if (this.useAsReturnValue)
                    {
                        return this.variableStyle.ReturnStyle.ReturnBehavior;
                    }

                    return ReturnBehavior.None;
                }
            }

            public static VariableInfo CreateReturnValue(VariableInfo variable)
            {
                Contract.ThrowIfNull(variable);
                Contract.ThrowIfFalse(variable.CanBeUsedAsReturnValue);
                Contract.ThrowIfFalse(variable.ParameterModifier == ParameterBehavior.Out || variable.ParameterModifier == ParameterBehavior.Ref);

                return new VariableInfo(variable.variableSymbol, variable.variableStyle, useAsReturnValue: true);
            }

            public void AddIdentifierTokenAnnotationPair(
                List<Tuple<SyntaxToken, SyntaxAnnotation>> annotations, CancellationToken cancellationToken)
            {
                this.variableSymbol.AddIdentifierTokenAnnotationPair(annotations, cancellationToken);
            }

            public string Name
            {
                get { return this.variableSymbol.Name; }
            }

            public bool OriginalTypeHadAnonymousTypeOrDelegate
            {
                get { return this.variableSymbol.OriginalTypeHadAnonymousTypeOrDelegate; }
            }

            public ITypeSymbol GetVariableType(SemanticDocument document)
            {
                return document.SemanticModel.ResolveType(this.variableSymbol.OriginalType);
            }

            public SyntaxToken GetIdentifierTokenAtDeclaration(SemanticDocument document)
            {
                return document.GetTokenWithAnnotaton(this.variableSymbol.IdentifierTokenAnnotation);
            }

            public SyntaxToken GetIdentifierTokenAtDeclaration(SyntaxNode node)
            {
                return node.GetAnnotatedTokens(this.variableSymbol.IdentifierTokenAnnotation).SingleOrDefault();
            }

            public static int Compare(VariableInfo left, VariableInfo right)
            {
                return VariableSymbol.Compare(left.variableSymbol, right.variableSymbol);
            }
        }
    }
}
