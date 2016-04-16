// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EELocalConstantSymbol : EELocalSymbolBase
    {
        private readonly MethodSymbol _method;
        private readonly string _name;
        private readonly TypeSymbol _type;
        private readonly ConstantValue _value;

        public EELocalConstantSymbol(
            MethodSymbol method,
            string name,
            TypeSymbol type,
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
            return new EELocalConstantSymbol(method, _name, type.Type, _value);
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
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _method; }
        }

        public override TypeSymbol Type
        {
            get { return _type; }
        }

        internal override bool IsPinned
        {
            get { return false; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return false; }
        }

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics)
        {
            if (diagnostics != null && _value.IsBad)
            {
                diagnostics.Add(ErrorCode.ERR_BadPdbData, Location.None, Name);
            }

            return _value;
        }

        internal override RefKind RefKind
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
