// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class ParameterTypeInformation : Microsoft.Cci.IParameterTypeInformation
    {
        private readonly ParameterSymbol UnderlyingParameter;

        public ParameterTypeInformation(ParameterSymbol underlyingParameter)
        {
            Debug.Assert((object)underlyingParameter != null);

            this.UnderlyingParameter = underlyingParameter;
        }

        IEnumerable<Microsoft.Cci.ICustomModifier> Microsoft.Cci.IParameterTypeInformation.CustomModifiers
        {
            get
            {
                return UnderlyingParameter.CustomModifiers;
            }
        }

        bool Microsoft.Cci.IParameterTypeInformation.IsByReference
        {
            get
            {
                return UnderlyingParameter.RefKind != RefKind.None;
            }
        }

        bool Microsoft.Cci.IParameterTypeInformation.IsModified
        {
            get
            {
                return UnderlyingParameter.CustomModifiers.Length != 0;
            }
        }

        bool Microsoft.Cci.IParameterTypeInformation.HasByRefBeforeCustomModifiers
        {
            get
            {
                return UnderlyingParameter.HasByRefBeforeCustomModifiers;
            }
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.IParameterTypeInformation.GetType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return ((PEModuleBuilder)context.Module).Translate(UnderlyingParameter.Type, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);
        }

        ushort Microsoft.Cci.IParameterListEntry.Index
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

    internal sealed class ArgListParameterTypeInformation : Microsoft.Cci.IParameterTypeInformation
    {
        private readonly ushort ordinal;
        private readonly bool isByRef;
        private readonly Microsoft.Cci.ITypeReference type;

        public ArgListParameterTypeInformation(int ordinal, bool isByRef, Microsoft.Cci.ITypeReference type)
        {
            this.ordinal = (ushort)ordinal;
            this.isByRef = isByRef;
            this.type = type;
        }

        IEnumerable<Microsoft.Cci.ICustomModifier> Microsoft.Cci.IParameterTypeInformation.CustomModifiers
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomModifier>(); }
        }

        bool Microsoft.Cci.IParameterTypeInformation.IsByReference
        {
            get { return isByRef; }
        }

        bool Microsoft.Cci.IParameterTypeInformation.IsModified
        {
            get { return false; }
        }

        bool Microsoft.Cci.IParameterTypeInformation.HasByRefBeforeCustomModifiers
        {
            get { return false; }
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.IParameterTypeInformation.GetType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return type;
        }

        ushort Microsoft.Cci.IParameterListEntry.Index
        {
            get { return ordinal; }
        }
    }
}
