// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal abstract class PlaceholderLocalSymbol : EELocalSymbolBase
    {
        private readonly MethodSymbol _method;
        private readonly string _name;
        private readonly TypeSymbol _type;

        internal PlaceholderLocalSymbol(MethodSymbol method, string name, TypeSymbol type)
        {
            _method = method;
            _name = name;
            _type = type;
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.RegularVariable; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { throw ExceptionUtilities.Unreachable; }
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
            get { return true; }
        }

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _method; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return NoLocations; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        internal abstract override bool IsWritable { get; }

        internal override EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            // Placeholders should be rewritten (as method calls)
            // rather than copied as locals to the target method.
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Rewrite the local reference as a call to a synthesized method.
        /// </summary>
        internal abstract BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax);

        internal static BoundExpression ConvertToLocalType(CSharpCompilation compilation, BoundExpression expr, TypeSymbol type)
        {
            HashSet<DiagnosticInfo> unusedUseSiteDiagnostics = null;
            var conversion = compilation.Conversions.ClassifyConversionFromExpression(expr, type, ref unusedUseSiteDiagnostics);
            Debug.Assert(conversion.IsValid);
            Debug.Assert(unusedUseSiteDiagnostics == null || unusedUseSiteDiagnostics.All(d => d.Severity < DiagnosticSeverity.Error));

            return BoundConversion.Synthesized(
                expr.Syntax,
                expr,
                conversion,
                @checked: false,
                explicitCastInCode: false,
                constantValueOpt: null,
                type: type);
        }
    }
}
