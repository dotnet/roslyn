// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Represents a method of a generic type instantiation.
    /// e.g. 
    /// A{int}.M()
    /// A.B{int}.C.M()
    /// </summary>
    internal class SpecializedMethodReference : MethodReference, Microsoft.Cci.ISpecializedMethodReference
    {
        public SpecializedMethodReference(MethodSymbol underlyingMethod)
            : base(underlyingMethod)
        {
        }

        public override void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.ISpecializedMethodReference)this);
        }

        Microsoft.Cci.IMethodReference Microsoft.Cci.ISpecializedMethodReference.UnspecializedVersion
        {
            get
            {
                return (MethodSymbol)UnderlyingMethod.OriginalDefinition;
            }
        }

        public override Microsoft.Cci.ISpecializedMethodReference AsSpecializedMethodReference
        {
            get
            {
                return this;
            }
        }
    }
}
