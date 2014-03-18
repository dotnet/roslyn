// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// An expression that represents a (name, value) pair and that is typically used in method calls, custom attributes and object initializers.
    /// </summary>
    internal sealed class MetadataNamedArgument : Cci.IMetadataNamedArgument
    {
        private readonly ISymbol entity;
        private readonly Cci.ITypeReference type;
        private readonly Cci.IMetadataExpression value;

        public MetadataNamedArgument(ISymbol entity, Cci.ITypeReference type, Cci.IMetadataExpression value)
        {
            // entity must be one of INamedEntity or IFieldDefinition or IPropertyDefinition
            this.entity = entity;
            this.type = type;
            this.value = value;
        }

        /// <summary>
        /// The name of the parameter or property or field that corresponds to the argument.
        /// </summary>
        string Cci.IMetadataNamedArgument.ArgumentName
        {
            get
            {
                return this.entity.Name;
            }
        }

        /// <summary>
        /// The value of the argument.
        /// </summary>
        Cci.IMetadataExpression Cci.IMetadataNamedArgument.ArgumentValue
        {
            get
            {
                return this.value;
            }
        }

        /// <summary>
        /// True if the named argument provides the value of a field.
        /// </summary>
        bool Cci.IMetadataNamedArgument.IsField
        {
            get
            {
                return this.entity is Cci.IFieldDefinition;
            }
        }

        void Cci.IMetadataExpression.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.ITypeReference Cci.IMetadataExpression.Type
        {
            get { return this.type; }
        }
    }
}
