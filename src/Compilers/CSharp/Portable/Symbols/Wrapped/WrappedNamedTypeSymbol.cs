﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a named type that is based on another named type.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedNamedTypeSymbol : NamedTypeSymbol
    {
        /// <summary>
        /// The underlying NamedTypeSymbol.
        /// </summary>
        protected readonly NamedTypeSymbol _underlyingType;

        public WrappedNamedTypeSymbol(NamedTypeSymbol underlyingType)
        {
            Debug.Assert((object)underlyingType != null);
            _underlyingType = underlyingType;
        }

        public NamedTypeSymbol UnderlyingNamedType
        {
            get
            {
                return _underlyingType;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingType.IsImplicitlyDeclared; }
        }

        public override int Arity
        {
            get
            {
                return _underlyingType.Arity;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return _underlyingType.MightContainExtensionMethods;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingType.Name;
            }
        }

        public override string MetadataName
        {
            get
            {
                return _underlyingType.MetadataName;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingType.HasSpecialName;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return _underlyingType.MangleName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingType.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingType.DeclaredAccessibility;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return _underlyingType.TypeKind;
            }
        }

        internal override bool IsInterface
        {
            get
            {
                return _underlyingType.IsInterface;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingType.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingType.DeclaringSyntaxReferences;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingType.IsStatic;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _underlyingType.IsAbstract;
            }
        }

        internal override bool IsMetadataAbstract
        {
            get
            {
                return _underlyingType.IsMetadataAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _underlyingType.IsSealed;
            }
        }

        internal override bool IsMetadataSealed
        {
            get
            {
                return _underlyingType.IsMetadataSealed;
            }
        }
        
        internal override bool HasCodeAnalysisEmbeddedAttribute => _underlyingType.HasCodeAnalysisEmbeddedAttribute;

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _underlyingType.ObsoleteAttributeData; }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return _underlyingType.ShouldAddWinRTMembers; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get { return _underlyingType.IsWindowsRuntimeImport; }
        }

        internal override TypeLayout Layout
        {
            get { return _underlyingType.Layout; }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return _underlyingType.MarshallingCharSet; }
        }

        public override bool IsSerializable
        {
            get { return _underlyingType.IsSerializable; }
        }

        internal override bool IsByRefLikeType
        {
            get { return _underlyingType.IsByRefLikeType; }
        }

        internal override bool IsReadOnly
        {
            get { return _underlyingType.IsReadOnly; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return _underlyingType.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return _underlyingType.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return _underlyingType.GetAppliedConditionalSymbols();
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return _underlyingType.GetAttributeUsageInfo();
        }

        internal override bool GetGuidString(out string guidString)
        {
            return _underlyingType.GetGuidString(out guidString);
        }
    }
}
