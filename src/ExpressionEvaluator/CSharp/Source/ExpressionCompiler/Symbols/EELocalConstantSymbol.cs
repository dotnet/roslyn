// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EELocalConstantSymbol : EELocalSymbolBase
    {
        private readonly MethodSymbol _method;
        private readonly string _name;
        private readonly TypeWithAnnotations _type;
        private readonly ConstantValue _value;

        public EELocalConstantSymbol(
            MethodSymbol method,
            string name,
            TypeSymbol type,
            ConstantValue value)
            : this(method, name, TypeWithAnnotations.Create(type), value)
        {
        }

        public EELocalConstantSymbol(
            MethodSymbol method,
            string name,
            TypeWithAnnotations type,
            ConstantValue value)
        {
            _method = method;
            _name = name;
            _type = type;
            _value = value;
        }

        internal override EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            var type = typeMap.SubstituteType(_type);
            return new EELocalConstantSymbol(method, _name, type, _value);
        }

        public override string Name
        {
            get { return _name; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.Constant; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { throw ExceptionUtilities.Unreachable(); }
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
            get { return false; }
        }

        internal override bool IsKnownToReferToTempIfReferenceType
        {
            get { return false; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return false; }
        }

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag diagnostics)
        {
            if (diagnostics != null && _value.IsBad)
            {
                diagnostics.Add(ErrorCode.ERR_BadPdbData, Location.None, Name);
            }

            return _value;
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return NoLocations; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }
    }
}
