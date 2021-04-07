// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized local variable.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class InterpolatedStringBuilderLocalSymbol : LocalSymbol
    {
        private readonly SyntaxNode _syntax;

        public InterpolatedStringBuilderLocalSymbol(
            MethodSymbol? containingMethod,
            SyntaxNode syntax,
            TypeWithAnnotations typeWithAnnotations,
            uint valEscapeScope
#if DEBUG
            ,
            int createdAtLineNumber,
            string createdAtFilePath
#endif
            )
        {
            ContainingSymbol = containingMethod;
            _syntax = syntax;
            TypeWithAnnotations = typeWithAnnotations;
            ValEscapeScope = valEscapeScope;
#if DEBUG
            _createdAtFilePath = createdAtFilePath;
            _createdAtLineNumber = createdAtLineNumber;
#endif
        }

#if DEBUG
        private readonly int _createdAtLineNumber;
        private readonly string _createdAtFilePath;
#endif

        internal override SynthesizedLocalKind SynthesizedKind => SynthesizedLocalKind.InterpolatedStringBuilder;
        public override TypeWithAnnotations TypeWithAnnotations { get; }
        internal override uint ValEscapeScope { get; }

        public override string? Name => null;
        internal override uint RefEscapeScope => throw ExceptionUtilities.Unreachable;
        internal override bool IsCompilerGenerated => true;
        public override RefKind RefKind => RefKind.None;
        public override Symbol? ContainingSymbol { get; }
        public override ImmutableArray<Location> Locations => ImmutableArray.Create(_syntax.Location);
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray.Create(_syntax.GetReference());
        internal override LocalDeclarationKind DeclarationKind => LocalDeclarationKind.None;
        internal override SyntaxNode? ScopeDesignatorOpt => null;
        internal override bool IsImportedFromMetadata => false;
        internal override SyntaxToken IdentifierToken => default;
        internal override bool IsPinned => false;

        internal override ConstantValue? GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag? diagnostics = null)
            => null;

        internal override ImmutableBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue)
            => ImmutableBindingDiagnostic<AssemblySymbol>.Empty;

        internal override SyntaxNode GetDeclaratorSyntax() => _syntax;

        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
            => throw ExceptionUtilities.Unreachable;

#if DEBUG
        private static int _nextSequence = 0;
        // Produce a token that helps distinguish one variable from another when debugging
        private readonly int _sequence = System.Threading.Interlocked.Increment(ref _nextSequence);

        internal string DumperString()
        {
            var builder = new StringBuilder();
            builder.Append(Type.ToDisplayString(SymbolDisplayFormat.TestFormat));
            builder.Append(' ');
            builder.Append(SynthesizedKind.ToString());
            builder.Append('.');
            builder.Append(_sequence);
            return builder.ToString();
        }
#endif

        internal override string GetDebuggerDisplay()
        {
            var builder = new StringBuilder();
            builder.Append('<');
            builder.Append(SynthesizedKind.ToString());
            builder.Append('>');
#if DEBUG
            builder.Append('.');
            builder.Append(_sequence);
#endif
            builder.Append(' ');
            builder.Append(Type.ToDisplayString(SymbolDisplayFormat.TestFormat));

#if DEBUG
            builder.Append(" @");
            builder.Append(_createdAtFilePath);
            builder.Append('(');
            builder.Append(_createdAtLineNumber);
            builder.Append(')');
#endif

            return builder.ToString();
        }
    }
}
