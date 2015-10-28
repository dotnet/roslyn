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
            TypeSymbolWithAnnotations parameterType,
            RefKind refKind,
            bool hasByRefBeforeCustomModifiers,
            string name,
            ImmutableArray<Location> locations,
            SyntaxReference syntaxRef,
            ConstantValue defaultSyntaxValue,
            bool isParams,
            bool isExtensionMethodThis)
        : base(
            owner: owner,
            ordinal: ordinal,
            parameterType: parameterType,
            refKind: refKind,
            name: name,
            locations: locations,
            syntaxRef: syntaxRef,
            defaultSyntaxValue: defaultSyntaxValue,
            isParams: isParams,
            isExtensionMethodThis: isExtensionMethodThis)
        {
            _tempBinder = binder;
            var unused = GetAttributesBag(diagnostics);
            _lazyDefaultSyntaxValue = MakeDefaultExpression(diagnostics, binder);
            _tempBinder = null; // no need to keep it around anymore, just uses up a lot of memory
        }

        protected override Binder ParameterBinder => _tempBinder;
    }
}
