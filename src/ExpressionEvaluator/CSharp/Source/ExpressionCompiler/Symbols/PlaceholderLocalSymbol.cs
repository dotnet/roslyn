// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Roslyn.Utilities;

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
        internal abstract BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax, DiagnosticBag diagnostics);

        internal static BoundExpression ConvertToLocalType(CSharpCompilation compilation, BoundExpression expr, TypeSymbol type, DiagnosticBag diagnostics)
        {
            // NOTE: This conversion can fail if some of the types involved are from not-yet-loaded modules.
            // For example, if System.Exception hasn't been loaded, then this call will fail for $stowedexception.
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = compilation.Conversions.ClassifyConversionFromExpression(expr, type, ref useSiteDiagnostics);
            diagnostics.Add(expr.Syntax, useSiteDiagnostics);

            return BoundConversion.Synthesized(
                expr.Syntax,
                expr,
                conversion,
                @checked: false,
                explicitCastInCode: false,
                constantValueOpt: null,
                type: type,
                hasErrors: !conversion.IsValid);
        }

        internal static MethodSymbol GetIntrinsicMethod(CSharpCompilation compilation, string methodName)
        {
            var type = compilation.GetTypeByMetadataName(ExpressionCompilerConstants.IntrinsicAssemblyTypeMetadataName);
            var members = type.GetMembers(methodName);
            Debug.Assert(members.Length == 1);
            return (MethodSymbol)members[0];
        }
    }
}
