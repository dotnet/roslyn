// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SubstitutedTypeParameterSymbol : TypeParameterSymbol
    {
        private readonly Symbol container;
        private readonly TypeMap map;
        private readonly TypeParameterSymbol substitutedFrom;

        internal SubstitutedTypeParameterSymbol(Symbol newContainer, TypeMap map, TypeParameterSymbol substitutedFrom)
        {
            this.container = newContainer;
            // it is important that we don't use the map here in the constructor, as the map is still being filled
            // in by TypeMap.WithAlphaRename.  Instead, we can use the map lazily when yielding the constraints.
            this.map = map;
            this.substitutedFrom = substitutedFrom;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return substitutedFrom.TypeParameterKind;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.container;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.substitutedFrom.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.substitutedFrom.DeclaringSyntaxReferences;
            }
        }

        public override TypeParameterSymbol OriginalDefinition
        {
            get
            {
                // A substituted type parameter symbol is used as a type parameter of a frame type for lambda-captured
                // variables within a generic method.  In that case the frame's own type parameter is an original.
                return
                    ContainingSymbol.OriginalDefinition != substitutedFrom.ContainingSymbol.OriginalDefinition ? this :
                    substitutedFrom.OriginalDefinition;
            }
        }

        public override TypeParameterSymbol ReducedFrom
        {
            get
            {
                if (this.container.Kind == SymbolKind.Method)
                {
                    MethodSymbol reducedFrom = ((MethodSymbol)this.container).ReducedFrom;

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
                return this.substitutedFrom.HasConstructorConstraint;
            }
        }

        public override int Ordinal
        {
            get
            {
                return this.substitutedFrom.Ordinal;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return this.substitutedFrom.Variance;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return this.substitutedFrom.HasValueTypeConstraint;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return this.substitutedFrom.HasReferenceTypeConstraint;
            }
        }

        public override string Name
        {
            get
            {
                return this.substitutedFrom.Name
#if DEBUG_ALPHA // turn on DEBUG_ALPHA to help diagnose issues around type parameter alpha-renaming
                    + "-" + nextSequence++
#endif
                    ;
            }
        }

#if DEBUG_ALPHA
        private static int nextSequence = 1;
#endif

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return this.substitutedFrom.IsImplicitlyDeclared;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.substitutedFrom.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.substitutedFrom.GetAttributes();
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            this.substitutedFrom.EnsureAllConstraintsAreResolved();
        }

        internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.map.SubstituteTypes(this.substitutedFrom.GetConstraintTypes(inProgress)).WhereAsArray(IsNotObjectFunc).Distinct();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.map.SubstituteNamedTypes(this.substitutedFrom.GetInterfaces(inProgress));
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.map.SubstituteNamedType(this.substitutedFrom.GetEffectiveBaseClass(inProgress));
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            return this.map.SubstituteType(this.substitutedFrom.GetDeducedBaseType(inProgress));
        }

        private static Func<TypeSymbol, bool> IsNotObjectFunc = type => type.SpecialType != SpecialType.System_Object;
    }
}