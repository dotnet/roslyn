// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class TypeSymbolExtensions
    {
        public static NamedTypeSymbol AsUnboundGenericType(this NamedTypeSymbol type)
        {
            if (!type.IsGenericType)
            {
                // This exception is part of the public contract of NamedTypeSymbol.ConstructUnboundGenericType
                throw new InvalidOperationException();
            }

            var original = type.OriginalDefinition;
            int n = original.Arity;
            NamedTypeSymbol originalContainingType = original.ContainingType;

            var constructedFrom = ((object)originalContainingType == null) ?
                original :
                original.AsMember(originalContainingType.IsGenericType ? originalContainingType.AsUnboundGenericType() : originalContainingType);
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
        public static ImmutableArray<TypeWithAnnotations> CreateTypeArguments(ImmutableArray<TypeParameterSymbol> typeParameters, int n, DiagnosticInfo errorInfo)
        {
            var result = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            for (int i = 0; i < n; i++)
            {
                string name = (i < typeParameters.Length) ? typeParameters[i].Name : string.Empty;
                result.Add(TypeWithAnnotations.Create(new UnboundArgumentErrorTypeSymbol(name, errorInfo)));
            }
            return result.ToImmutableAndFree();
        }

        public static readonly ErrorTypeSymbol Instance = new UnboundArgumentErrorTypeSymbol(string.Empty, new CSDiagnosticInfo(ErrorCode.ERR_UnexpectedUnboundGenericName));

        private readonly string _name;
        private readonly DiagnosticInfo _errorInfo;

        private UnboundArgumentErrorTypeSymbol(string name, DiagnosticInfo errorInfo, TupleExtraData? tupleData = null)
            : base(tupleData)
        {
            _name = name;
            _errorInfo = errorInfo;
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            return new UnboundArgumentErrorTypeSymbol(_name, _errorInfo, newData);
        }

        public override string Name
        {
            get
            {
                return _name;
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

        internal override bool IsFileLocal => false;
        internal override FileIdentifier? AssociatedFileIdentifier => null;

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return _errorInfo;
            }
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            if ((object)t2 == (object)this)
            {
                return true;
            }

            UnboundArgumentErrorTypeSymbol? other = t2 as UnboundArgumentErrorTypeSymbol;
            return (object?)other != null && string.Equals(other._name, _name, StringComparison.Ordinal) && object.Equals(other._errorInfo, _errorInfo);
        }

        public override int GetHashCode()
        {
            return _errorInfo == null
                ? _name.GetHashCode()
                : Hash.Combine(_name, _errorInfo.Code);
        }
    }
}
