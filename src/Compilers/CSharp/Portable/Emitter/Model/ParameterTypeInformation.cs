// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class ParameterTypeInformation : Cci.IParameterTypeInformation
    {
        private readonly ParameterSymbol _underlyingParameter;

        public ParameterTypeInformation(ParameterSymbol underlyingParameter)
        {
            Debug.Assert((object)underlyingParameter != null);

            _underlyingParameter = underlyingParameter;
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(_underlyingParameter.TypeWithAnnotations.CustomModifiers);
            }
        }

        bool Cci.IParameterTypeInformation.IsByReference
        {
            get
            {
                return _underlyingParameter.RefKind != RefKind.None;
            }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.RefCustomModifiers
        {
            get
            {
                return ImmutableArray<Cci.ICustomModifier>.CastUp(_underlyingParameter.RefCustomModifiers);
            }
        }

        Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(_underlyingParameter.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        ushort Cci.IParameterListEntry.Index
        {
            get
            {
                return (ushort)_underlyingParameter.Ordinal;
            }
        }

        public override string ToString()
        {
            return _underlyingParameter.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }
    }

    internal sealed class ArgListParameterTypeInformation : Cci.IParameterTypeInformation
    {
        private readonly ushort _ordinal;
        private readonly bool _isByRef;
        private readonly Cci.ITypeReference _type;

        public ArgListParameterTypeInformation(int ordinal, bool isByRef, Cci.ITypeReference type)
        {
            _ordinal = (ushort)ordinal;
            _isByRef = isByRef;
            _type = type;
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers
        {
            get { return ImmutableArray<Cci.ICustomModifier>.Empty; }
        }

        bool Cci.IParameterTypeInformation.IsByReference
        {
            get { return _isByRef; }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.RefCustomModifiers
        {
            get { return ImmutableArray<Cci.ICustomModifier>.Empty; }
        }

        Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
        {
            return _type;
        }

        ushort Cci.IParameterListEntry.Index
        {
            get { return _ordinal; }
        }
    }
}
