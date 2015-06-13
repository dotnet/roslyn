// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// <summary>
        /// Returns a value indicating whether the specified symbol is a static
        /// holder type.
        /// </summary>
        /// <param name="symbol">
        /// The symbol being examined.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="symbol"/> is a static holder type;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// A symbol is a static holder type if it is a class with at least one
        /// "qualifying member" (<see cref="IsQualifyingMember(ISymbol)"/>) and no
        /// "disqualifying members" (<see cref="IsDisqualifyingMember(ISymbol)"/>).
        /// </remarks>
        internal static bool IsStaticHolderType(this INamedTypeSymbol symbol)
        {
            if (!symbol.IsReferenceType)
            {
                return false;
            }

            List<ISymbol> declaredMembers = symbol.GetMembers()
                .Where(m => !m.IsImplicitlyDeclared)
                .ToList();

            List<ISymbol> qualifyingMembers = declaredMembers
                .Where(IsQualifyingMember)
                .ToList();

            List<ISymbol> disqualifyingMembers = declaredMembers
                .Where(IsDisqualifyingMember)
                .ToList();

            return qualifyingMembers.Count > 0 && disqualifyingMembers.Count == 0;
        }

        /// <summary>
        /// Returns a value indicating whether the specified symbol qualifies as a
        /// member of a static holder class.
        /// </summary>
        /// <param name="member">
        /// The member being examined.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="member"/> qualifies as a member of
        /// a static holder class; otherwise <c>false</c>.
        /// </returns>
        private static bool IsQualifyingMember(ISymbol member)
        {
            // A type member *does* qualify as a member of a static holder class,
            // because even though it is *not* static, it is nevertheless not
            // per-instance.
            if (member.IsType())
            {
                return true;
            }

            // An user-defined operator method is not a valid member of a static holder
            // class, because even though it is static, it takes instances as
            // parameters, so presumably the author of the class intended for it to be
            // instantiated.
            if (member.IsUserDefinedOperator())
            {
                return false;
            }

            return member.IsStatic;
        }

        /// <summary>
        /// Returns a value indicating whether the presence of the specified symbol
        /// disqualifies a class from being considered a static holder class.
        /// </summary>
        /// <param name="member">
        /// The member being examined.
        /// </param>
        /// <returns>
        /// <c>true</c> if the presence of <paramref name="member"/> disqualifies the
        /// current type as a static holder class; otherwise <c>false</c>.
        /// </returns>
        private static bool IsDisqualifyingMember(ISymbol member)
        {
            // An user-defined operator method disqualifies a class from being considered
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