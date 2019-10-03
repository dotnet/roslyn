// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a parameter of a method or a property of a tuple type
    /// </summary>
    internal sealed class TupleParameterSymbol : WrappedParameterSymbol
    {
        private readonly Symbol _container;

        public TupleParameterSymbol(Symbol container, ParameterSymbol underlyingParameter)
            : base(underlyingParameter)
        {
            Debug.Assert((object)container != null);
            _container = container;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _container;
            }
        }

        public override int GetHashCode()
        {
            return _underlyingParameter.GetHashCode();
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            return Equals(obj as TupleParameterSymbol);
        }

        public bool Equals(TupleParameterSymbol other)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null && _container == other._container && _underlyingParameter == other._underlyingParameter;
        }
    }
}
