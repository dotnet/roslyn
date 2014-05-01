// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class ParameterTypeInformation : Cci.IParameterTypeInformation
    {
        private readonly ParameterSymbol UnderlyingParameter;

        public ParameterTypeInformation(ParameterSymbol underlyingParameter)
        {
            Debug.Assert((object)underlyingParameter != null);

            this.UnderlyingParameter = underlyingParameter;
        }

        IEnumerable<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers
        {
            get
            {
                return UnderlyingParameter.CustomModifiers;
            }
        }

        bool Cci.IParameterTypeInformation.IsByReference
        {
            get
            {
                return UnderlyingParameter.RefKind != RefKind.None;
            }
        }

        bool Cci.IParameterTypeInformation.IsModified
        {
            get
            {
                return UnderlyingParameter.CustomModifiers.Length != 0;
            }
        }

        bool Cci.IParameterTypeInformation.HasByRefBeforeCustomModifiers
        {
            get
            {
                return UnderlyingParameter.HasByRefBeforeCustomModifiers;
            }
        }

        Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingParameter.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        ushort Cci.IParameterListEntry.Index
        {
            get
            {
                return (ushort)UnderlyingParameter.Ordinal;
            }
        }

        public override string ToString()
        {
            return UnderlyingParameter.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }
    }

    internal sealed class ArgListParameterTypeInformation : Cci.IParameterTypeInformation
    {
        private readonly ushort ordinal;
        private readonly bool isByRef;
        private readonly Cci.ITypeReference type;

        public ArgListParameterTypeInformation(int ordinal, bool isByRef, Cci.ITypeReference type)
        {
            this.ordinal = (ushort)ordinal;
            this.isByRef = isByRef;
            this.type = type;
        }

        IEnumerable<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers
        {
            get { return SpecializedCollections.EmptyEnumerable<Cci.ICustomModifier>(); }
        }

        bool Cci.IParameterTypeInformation.IsByReference
        {
            get { return isByRef; }
        }

        bool Cci.IParameterTypeInformation.IsModified
        {
            get { return false; }
        }

        bool Cci.IParameterTypeInformation.HasByRefBeforeCustomModifiers
        {
            get { return false; }
        }

        Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
        {
            return type;
        }

        ushort Cci.IParameterListEntry.Index
        {
            get { return ordinal; }
        }
    }
}
