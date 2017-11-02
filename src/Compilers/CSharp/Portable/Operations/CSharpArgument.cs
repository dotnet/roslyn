// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class BaseCSharpArgument : BaseArgument
    {
        public BaseCSharpArgument(ArgumentKind argumentKind, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        public override CommonConversion InConversion => new CommonConversion(exists:true, isIdentity:true, isNumeric:false, isReference:false, methodSymbol:null);

        public override CommonConversion OutConversion => new CommonConversion(exists: true, isIdentity: true, isNumeric: false, isReference: false, methodSymbol: null);
    }

    internal sealed class CSharpArgument : BaseCSharpArgument
    {
        public CSharpArgument(ArgumentKind argumentKind, IParameterSymbol parameter, IOperation value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)
        {
            ValueImpl = value;
        }

        protected override IOperation ValueImpl { get; }
    }

    internal sealed class LazyCSharpArgument : BaseCSharpArgument
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyCSharpArgument(ArgumentKind argumentKind, IParameterSymbol parameter, Lazy<IOperation> value, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) :
            base(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)
        {
            _lazyValue = value ?? throw new ArgumentNullException(nameof(value));
        }        

        protected override IOperation ValueImpl => _lazyValue.Value;
    }
}
