// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EELocalSymbol : EELocalSymbolBase
    {
        private readonly MethodSymbol _method;
        private readonly TypeWithAnnotations _type;

        private readonly LocalDeclarationKind _declarationKind;
        private readonly bool _isCompilerGenerated;
        private readonly ImmutableArray<Location> _locations;
        private readonly string _nameOpt;
        private readonly bool _isPinned;
        private readonly RefKind _refKind;
        private readonly bool _canScheduleToStack;

        public EELocalSymbol(
            MethodSymbol method,
            ImmutableArray<Location> locations,
            string nameOpt,
            int ordinal,
            LocalDeclarationKind declarationKind,
            TypeSymbol type,
            RefKind refKind,
            bool isPinned,
            bool isCompilerGenerated,
            bool canScheduleToStack)
            : this(method, locations, nameOpt, ordinal, declarationKind, TypeWithAnnotations.Create(type), refKind, isPinned, isCompilerGenerated, canScheduleToStack)
        {
        }

        public EELocalSymbol(
            MethodSymbol method,
            ImmutableArray<Location> locations,
            string nameOpt,
            int ordinal,
            LocalDeclarationKind declarationKind,
            TypeWithAnnotations type,
            RefKind refKind,
            bool isPinned,
            bool isCompilerGenerated,
            bool canScheduleToStack)
        {
            Debug.Assert(method != null);
            Debug.Assert(ordinal >= -1);
            Debug.Assert(!locations.IsDefault);
            Debug.Assert((object)type != null);

            _method = method;
            _locations = locations;
            _nameOpt = nameOpt;
            Ordinal = ordinal;
            _declarationKind = declarationKind;
            _type = type;
            _refKind = refKind;
            _isPinned = isPinned;
            _isCompilerGenerated = isCompilerGenerated;
            _canScheduleToStack = canScheduleToStack;
        }

        internal override EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            var type = typeMap.SubstituteType(_type);
            return new EELocalSymbol(method, _locations, _nameOpt, Ordinal, _declarationKind, type, _refKind, _isPinned, _isCompilerGenerated, _canScheduleToStack);
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return _declarationKind; }
        }

        internal override bool CanScheduleToStack
        {
            get { return _canScheduleToStack; }
        }

        internal int Ordinal { get; }

        public override string Name
        {
            get { return _nameOpt; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _locations; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _method; }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _type; }
        }

        internal override bool IsPinned
        {
            get { return _isPinned; }
        }

        internal override bool IsKnownToReferToTempIfReferenceType
        {
            get { return false; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return _isCompilerGenerated; }
        }

        public override RefKind RefKind
        {
            get { return _refKind; }
        }
    }
}
