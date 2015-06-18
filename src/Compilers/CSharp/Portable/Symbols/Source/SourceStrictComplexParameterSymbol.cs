using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SourceStrictComplexParameterSymbol : SourceComplexParameterSymbol
    {
        private readonly Binder _tempBinder;

        internal SourceStrictComplexParameterSymbol(
            DiagnosticBag diagnostics,
            Binder binder,
            Symbol owner,
            int ordinal,
            TypeSymbol parameterType,
            RefKind refKind,
            ImmutableArray<CustomModifier> customModifiers,
            bool hasByRefBeforeCustomModifiers,
            string name,
            ImmutableArray<Location> locations,
            SyntaxReference syntaxRef,
            ConstantValue defaultSyntaxValue,
            bool isParams,
            bool isExtensionMethodThis)
        : base(
            owner,
            ordinal,
            parameterType,
            refKind,
            customModifiers,
            hasByRefBeforeCustomModifiers,
            name,
            locations,
            syntaxRef,
            defaultSyntaxValue,
            isParams,
            isExtensionMethodThis)
        {
            _tempBinder = binder;
            var unused = GetAttributesBag(diagnostics);
            _lazyDefaultSyntaxValue = MakeDefaultExpression(diagnostics, binder);
            _tempBinder = null; // no need to keep it around anymore, just uses up a lot of memory
        }

        protected override Binder ParameterBinder => _tempBinder;
    }
}
