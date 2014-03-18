// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class TypeSymbolExtensions
    {
        static public NamedTypeSymbol AsUnboundGenericType(this NamedTypeSymbol type)
        {
            if (!type.IsGenericType)
            {
                return type;
            }

            var original = type.OriginalDefinition;
            int n = original.Arity;
            var constructedFrom = ((object)original.ContainingType == null) ?
                original :
                original.AsMember(original.ContainingType.AsUnboundGenericType());
            if (n == 0)
            {
                return constructedFrom;
            }

            var typeArguments = UnboundArgumentErrorTypeSymbol.CreateTypeArguments(
                constructedFrom.TypeParameters,
                n,
                new CSDiagnosticInfo(ErrorCode.ERR_UnexpectedUnboundGenericName));
            return constructedFrom.Construct(typeArguments, unbound: true);
        }
    }

    internal sealed class UnboundArgumentErrorTypeSymbol : ErrorTypeSymbol
    {
        public static ImmutableArray<TypeSymbol> CreateTypeArguments(ImmutableArray<TypeParameterSymbol> typeParameters, int n, DiagnosticInfo errorInfo)
        {
            var result = ArrayBuilder<TypeSymbol>.GetInstance();
            for (int i = 0; i < n; i++)
            {
                string name = (i < typeParameters.Length) ? typeParameters[i].Name : string.Empty;
                result.Add(new UnboundArgumentErrorTypeSymbol(name, errorInfo));
            }
            return result.ToImmutableAndFree();
        }

        public static readonly ErrorTypeSymbol Instance = new UnboundArgumentErrorTypeSymbol(string.Empty, new CSDiagnosticInfo(ErrorCode.ERR_UnexpectedUnboundGenericName));

        private readonly string name;
        private readonly DiagnosticInfo errorInfo;

        private UnboundArgumentErrorTypeSymbol(string name, DiagnosticInfo errorInfo)
        {
            this.name = name;
            this.errorInfo = errorInfo;
        }

        public override string Name
        {
            get
            {
                return this.name;
            }
        }

        internal override bool MangleName
        {
            get
            {
                Debug.Assert(Arity == 0);
                return false;
            }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return this.errorInfo;
            }
        }

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            if ((object)t2 == (object)this)
            {
                return true;
            }

            UnboundArgumentErrorTypeSymbol other = t2 as UnboundArgumentErrorTypeSymbol;
            return (object)other != null && string.Equals(other.name, this.name, StringComparison.Ordinal) && object.Equals(other.errorInfo, this.errorInfo);
        }

        public override int GetHashCode()
        {
            return this.errorInfo == null
                ? this.name.GetHashCode()
                : Hash.Combine(this.name, this.errorInfo.Code);
        }
    }
}
