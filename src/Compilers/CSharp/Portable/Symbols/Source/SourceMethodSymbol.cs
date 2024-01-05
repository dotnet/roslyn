// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class to represent all source method-like symbols. This includes
    /// things like ordinary methods and constructors, and functions
    /// like lambdas and local functions.
    /// </summary>
    internal abstract class SourceMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// If there are no constraints, returns an empty immutable array. Otherwise, returns an immutable
        /// array of types, indexed by the constrained type parameter in <see cref="MethodSymbol.TypeParameters"/>.
        /// </summary>
        public abstract ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes();

        /// <summary>
        /// If there are no constraints, returns an empty immutable array. Otherwise, returns an immutable
        /// array of kinds, indexed by the constrained type parameter in <see cref="MethodSymbol.TypeParameters"/>.
        /// </summary>
        public abstract ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds();

        protected static void ReportBadRefToken(TypeSyntax returnTypeSyntax, BindingDiagnosticBag diagnostics)
        {
            if (!returnTypeSyntax.HasErrors)
            {
                var refKeyword = returnTypeSyntax.GetFirstToken();
                diagnostics.Add(ErrorCode.ERR_UnexpectedToken, refKeyword.GetLocation(), refKeyword.ToString());
            }
        }

        protected bool AreContainingSymbolLocalsZeroed
        {
            get
            {
                if (ContainingSymbol is SourceMethodSymbol method)
                {
                    return method.AreLocalsZeroed;
                }
                else if (ContainingType is SourceMemberContainerTypeSymbol type)
                {
                    return type.AreLocalsZeroed;
                }
                else
                {
                    // Sometimes a source method symbol can be contained in a non-source symbol.
                    // For example in EE. We aren't concerned with respecting SkipLocalsInit in such cases.
                    return true;
                }
            }
        }

        internal void ReportAsyncParameterErrors(BindingDiagnosticBag diagnostics, Location location)
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_BadAsyncArgType, getLocation(parameter, location));
                }
                else if (parameter.Type.IsPointerOrFunctionPointer())
                {
                    diagnostics.Add(ErrorCode.ERR_UnsafeAsyncArgType, getLocation(parameter, location));
                }
                else if (parameter.Type.IsRestrictedType())
                {
                    diagnostics.Add(ErrorCode.ERR_BadSpecialByRefLocal, getLocation(parameter, location), parameter.Type);
                }
            }

            static Location getLocation(ParameterSymbol parameter, Location location)
                => parameter.TryGetFirstLocation() ?? location;
        }

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool UseUpdatedEscapeRules => ContainingModule.UseUpdatedEscapeRules;

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            return SourceMemberContainerTypeSymbol.HasAsyncMethodBuilderAttribute(this, out builderArgument);
        }
    }
}
