// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//#define DEBUG_ALPHA // turn on DEBUG_ALPHA to help diagnose issues around type parameter alpha-renaming

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SubstitutedTypeParameterSymbol : TypeParameterSymbol
    {
        private readonly Symbol _container;
        private readonly TypeMap _map;
        private readonly TypeParameterSymbol _substitutedFrom;
        private readonly int _ordinal;

#if DEBUG_ALPHA
        private static int _nextSequence = 1;
        private readonly int _mySequence;
#endif

        internal SubstitutedTypeParameterSymbol(Symbol newContainer, TypeMap map, TypeParameterSymbol substitutedFrom, int ordinal)
        {
            _container = newContainer;
            // it is important that we don't use the map here in the constructor, as the map is still being filled
            // in by TypeMap.WithAlphaRename.  Instead, we can use the map lazily when yielding the constraints.
            _map = map;
            _substitutedFrom = substitutedFrom;
            _ordinal = ordinal;
#if DEBUG_ALPHA
            _mySequence = _nextSequence++;
#endif
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return _substitutedFrom.TypeParameterKind;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _container;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _substitutedFrom.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _substitutedFrom.DeclaringSyntaxReferences;
            }
        }

        public override TypeParameterSymbol OriginalDefinition
        {
            get
            {
                // A substituted type parameter symbol is used as a type parameter of a frame type for lambda-captured
                // variables within a generic method.  In that case the frame's own type parameter is an original.
                return
                    ContainingSymbol.OriginalDefinition != _substitutedFrom.ContainingSymbol.OriginalDefinition ? this :
                    _substitutedFrom.OriginalDefinition;
            }
        }

        public override TypeParameterSymbol ReducedFrom
        {
            get
            {
                if (_container.Kind == SymbolKind.Method)
                {
                    MethodSymbol reducedFrom = ((MethodSymbol)_container).ReducedFrom;

                    if ((object)reducedFrom != null)
                    {
                        return reducedFrom.TypeParameters[this.Ordinal];
                    }
                }

                return null;
            }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                return _substitutedFrom.HasConstructorConstraint;
            }
        }

        public override int Ordinal
        {
            get
            {
                return _ordinal;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return _substitutedFrom.Variance;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return _substitutedFrom.HasValueTypeConstraint;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return _substitutedFrom.HasReferenceTypeConstraint;
            }
        }

        public override string Name
        {
            get
            {
                return _substitutedFrom.Name
#if DEBUG_ALPHA
                    + "#" + _mySequence
#endif
                    ;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _substitutedFrom.IsImplicitlyDeclared;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _substitutedFrom.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _substitutedFrom.GetAttributes();
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            _substitutedFrom.EnsureAllConstraintsAreResolved();
        }

        internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return _map.SubstituteTypesWithoutModifiers(_substitutedFrom.GetConstraintTypes(inProgress)).WhereAsArray(s_isNotObjectFunc).Distinct();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return _map.SubstituteNamedTypes(_substitutedFrom.GetInterfaces(inProgress));
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            return _map.SubstituteNamedType(_substitutedFrom.GetEffectiveBaseClass(inProgress));
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            return _map.SubstituteType(_substitutedFrom.GetDeducedBaseType(inProgress)).AsTypeSymbolOnly();
        }

        private static readonly Func<TypeSymbol, bool> s_isNotObjectFunc = type => type.SpecialType != SpecialType.System_Object;
    }
}
