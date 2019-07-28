// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A NoPiaIllegalGenericInstantiationSymbol is a special kind of ErrorSymbol that represents a
    /// generic type instantiation that cannot cross assembly boundaries according to NoPia rules.
    /// </summary>
    internal class NoPiaIllegalGenericInstantiationSymbol : ErrorTypeSymbol
    {
        private readonly ModuleSymbol _exposingModule;
        private readonly NamedTypeSymbol _underlyingSymbol;

        public NoPiaIllegalGenericInstantiationSymbol(ModuleSymbol exposingModule, NamedTypeSymbol underlyingSymbol)
        {
            _exposingModule = exposingModule;
            _underlyingSymbol = underlyingSymbol;
        }

        internal override bool MangleName
        {
            get
            {
                Debug.Assert(Arity == 0);
                return false;
            }
        }

        public NamedTypeSymbol UnderlyingSymbol
        {
            get
            {
                return _underlyingSymbol;
            }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                if (_underlyingSymbol.IsErrorType())
                {
                    DiagnosticInfo underlyingInfo = ((ErrorTypeSymbol)_underlyingSymbol).ErrorInfo;

                    if ((object)underlyingInfo != null)
                    {
                        return underlyingInfo;
                    }
                }

                return new CSDiagnosticInfo(ErrorCode.ERR_GenericsUsedAcrossAssemblies, _underlyingSymbol, _exposingModule.ContainingAssembly);
            }
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
        {
            return ReferenceEquals(this, t2);
        }
    }
}
