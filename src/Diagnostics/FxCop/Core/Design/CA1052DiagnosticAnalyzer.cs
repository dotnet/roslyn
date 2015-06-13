using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.AnalyzerPowerPack.Utilities;

namespace Microsoft.AnalyzerPowerPack.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CA1052DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        public const string DiagnosticId = "CA1052";

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(AnalyzerPowerPackRulesResources.StaticHolderTypesShouldBeStaticOrNotInheritable),
            AnalyzerPowerPackRulesResources.ResourceManager,
            typeof(AnalyzerPowerPackRulesResources));

        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(AnalyzerPowerPackRulesResources.StaticHolderTypeIsNotStatic),
            AnalyzerPowerPackRulesResources.ResourceManager,
            typeof(AnalyzerPowerPackRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerPowerPackDiagnosticCategory.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/library/ms182168.aspx",
            customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        protected override void AnalyzeSymbol(
            INamedTypeSymbol symbol,
            Compilation compilation,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions options,
            CancellationToken cancellationToken)
        {
            if (symbol.IsStaticHolderType()
                && !symbol.IsStatic
                && (symbol.IsPublic() || symbol.IsProtected()))
            {
                addDiagnostic(symbol.CreateDiagnostic(Rule, symbol.Name));
            }
        }
    }

    internal static class CA1052Extensions
    {
        internal static bool IsStaticHolderType(this INamedTypeSymbol symbol)
        {
            List<ISymbol> declaredMembers = symbol.GetMembers()
                .Where(m => !m.IsImplicitlyDeclared)
                .ToList();

            // Don't consider a class with no declared members to be a static holder class.
            if (declaredMembers.Count == 0)
            {
                return false;
            }

            List<ISymbol> disqualifyingMembers = declaredMembers
                .Where(IsDisqualifyingMember)
                .ToList();

            return disqualifyingMembers.Count == 0;
        }

        // Disqualify the class from being a static holder class if it has any of the
        // following:
        //     - Any operator overload method (because, even though they are declared
        //       static, they take instances as parameters, so presumably the author
        //       of the class intends for it to be instantiated).
        //     - Any declared instance member other than a default constructor.
        private static bool IsDisqualifyingMember(ISymbol member)
        {
            // An operator overload method disqualifies a class from being considered
            // a static holder, because even though it is static, it takes instances as
            // parameters, so presumably the author of the class intended for it to be
            // instantiated.
            if (member.IsUserDefinedOperator())
            {
                return true;
            }
            
            // A type member does *not* disqualify a class from being considered a static
            // holder, because even though it is *not* static, it is nevertheless not
            // per-instance.
            if (member.IsType())
            {
                return false;
            }

            // Any instance member other than a default constructor disqualifies a class
            // from being considered a static holder class.
            return !member.IsStatic && !member.IsDefaultConstructor();
        }

        internal static bool IsProtected(this ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Protected;
        }

        public static bool IsDefaultConstructor(this ISymbol symbol)
        {
            return symbol.IsConstructor() && symbol.GetParameters().Length == 0;
        }
    }
}