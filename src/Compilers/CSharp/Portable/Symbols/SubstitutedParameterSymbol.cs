// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SubstitutedParameterSymbol : WrappedParameterSymbol
    {
        // initially set to map which is only used to get the type, which is once computed is stored here.
        private object mapOrType;

        private readonly Symbol containingSymbol;

        internal SubstitutedParameterSymbol(MethodSymbol containingSymbol, TypeMap map, ParameterSymbol originalParameter) :
            this((Symbol)containingSymbol, map, originalParameter)
        {
        }

        internal SubstitutedParameterSymbol(PropertySymbol containingSymbol, TypeMap map, ParameterSymbol originalParameter) :
            this((Symbol)containingSymbol, map, originalParameter)
        {
        }

        private SubstitutedParameterSymbol(Symbol containingSymbol, TypeMap map, ParameterSymbol originalParameter) :
            base(originalParameter)
        {
            Debug.Assert(originalParameter.IsDefinition);
            this.containingSymbol = containingSymbol;
            this.mapOrType = map;
        }

        public override ParameterSymbol OriginalDefinition
        {
            get { return underlyingParameter.OriginalDefinition; }
        }

        public override Symbol ContainingSymbol
        {
            get { return containingSymbol; }
        }

        public override TypeSymbol Type
        {
            get
            {
                var mapOrType = this.mapOrType;
                var type = mapOrType as TypeSymbol;
                if (type != null)
                {
                    return type;
                }

                type = ((TypeMap)mapOrType).SubstituteType(this.underlyingParameter.Type);
                this.mapOrType = type;
                return type;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return underlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }
    }
}