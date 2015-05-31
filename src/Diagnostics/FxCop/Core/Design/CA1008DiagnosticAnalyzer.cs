// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1008: Enums should have zero value
    /// 
    /// Cause:
    /// An enumeration without an applied System.FlagsAttribute does not define a member that has a value of zero;
    /// or an enumeration that has an applied FlagsAttribute defines a member that has a value of zero but its name is not 'None',
    /// or the enumeration defines multiple zero-valued members.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1008DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        /*
            Rule Description:
            The default value of an uninitialized enumeration, just like other value types, is zero.
            A non-flags−attributed enumeration should define a member that has the value of zero so that the default value is a valid value of the enumeration.
            If appropriate, name the member 'None'. Otherwise, assign zero to the most frequently used member.
            Note that, by default, if the value of the first enumeration member is not set in the declaration, its value is zero.

            If an enumeration that has the FlagsAttribute applied defines a zero-valued member, its name should be 'None' to indicate that no values have been set in the enumeration.
            Using a zero-valued member for any other purpose is contrary to the use of the FlagsAttribute in that the AND and OR bitwise operators are useless with the member.
            This implies that only one member should be assigned the value zero. Note that if multiple members that have the value zero occur in a flags-attributed enumeration,
            Enum.ToString() returns incorrect results for members that are not zero. 
        */

        internal const string RuleId = "CA1008";
        internal const string RuleRenameCustomTag = "RuleRename";
        internal const string RuleMultipleZeroCustomTag = "RuleMultipleZero";
        internal const string RuleNoZeroCustomTag = "RuleNoZero";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FxCopRulesResources.EnumsShouldHaveZeroValue), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.EnumsShouldHaveZeroValueDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        private static readonly LocalizableString s_localizableMessageRuleRename = new LocalizableResourceString(nameof(FxCopRulesResources.EnumsShouldZeroValueFlagsRename), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static DiagnosticDescriptor RuleRename = new DiagnosticDescriptor(RuleId,
                                                                       s_localizableTitle,
                                                                       s_localizableMessageRuleRename,
                                                                       FxCopDiagnosticCategory.Design,
                                                                       DiagnosticSeverity.Warning,
                                                                       isEnabledByDefault: false,
                                                                       description: s_localizableDescription,
                                                                       helpLinkUri: "http://msdn.microsoft.com/library/ms182149.aspx",
                                                                       customTags: DiagnosticCustomTags.Microsoft.Concat(RuleRenameCustomTag).ToArray());

        private static readonly LocalizableString s_localizableMessageRuleMultipleZero = new LocalizableResourceString(nameof(FxCopRulesResources.EnumsShouldZeroValueFlagsMultipleZero), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static DiagnosticDescriptor RuleMultipleZero = new DiagnosticDescriptor(RuleId,
                                                               s_localizableTitle,
                                                               s_localizableMessageRuleMultipleZero,
                                                               FxCopDiagnosticCategory.Design,
                                                               DiagnosticSeverity.Warning,
                                                               isEnabledByDefault: false,
                                                               description: s_localizableDescription,
                                                               helpLinkUri: "http://msdn.microsoft.com/library/ms182149.aspx",
                                                               customTags: DiagnosticCustomTags.Microsoft.Concat(RuleMultipleZeroCustomTag).ToArray());

        private static readonly LocalizableString s_localizableMessageRuleNoZero = new LocalizableResourceString(nameof(FxCopRulesResources.EnumsShouldZeroValueNotFlagsNoZeroValue), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static DiagnosticDescriptor RuleNoZero = new DiagnosticDescriptor(RuleId,
                                                               s_localizableTitle,
                                                               s_localizableMessageRuleNoZero,
                                                               FxCopDiagnosticCategory.Design,
                                                               DiagnosticSeverity.Warning,
                                                               isEnabledByDefault: false,
                                                               description: s_localizableDescription,
                                                               helpLinkUri: "http://msdn.microsoft.com/library/ms182149.aspx",
                                                               customTags: DiagnosticCustomTags.Microsoft.Concat(RuleNoZeroCustomTag).ToArray());

        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedRules = ImmutableArray.Create(RuleRename, RuleMultipleZero, RuleNoZero);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return s_supportedRules;
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (symbol.TypeKind != TypeKind.Enum)
            {
                return;
            }

            var flagsAttribute = WellKnownTypes.FlagsAttribute(compilation);
            if (flagsAttribute == null)
            {
                return;
            }

            var zeroValuedFields = GetZeroValuedFields(symbol).ToImmutableArray();

            bool hasFlagsAttribute = symbol.GetAttributes().Any(a => a.AttributeClass == flagsAttribute);
            if (hasFlagsAttribute)
            {
                CheckFlags(symbol, zeroValuedFields, addDiagnostic);
            }
            else
            {
                CheckNonFlags(symbol, zeroValuedFields, addDiagnostic);
            }
        }

        private void CheckFlags(INamedTypeSymbol namedType, ImmutableArray<IFieldSymbol> zeroValuedFields, Action<Diagnostic> addDiagnostic)
        {
            switch (zeroValuedFields.Length)
            {
                case 0:
                    break;

                case 1:
                    if (!IsMemberNamedNone(zeroValuedFields[0]))
                    {
                        // In enum '{0}', change the name of '{1}' to 'None'.
                        addDiagnostic(zeroValuedFields[0].CreateDiagnostic(RuleRename, namedType.Name, zeroValuedFields[0].Name));
                    }

                    break;

                default:
                    {
                        // Remove all members that have the value zero from {0} except for one member that is named 'None'.
                        addDiagnostic(namedType.CreateDiagnostic(RuleMultipleZero, namedType.Name));
                    }

                    break;
            }
        }

        private void CheckNonFlags(INamedTypeSymbol namedType, ImmutableArray<IFieldSymbol> zeroValuedFields, Action<Diagnostic> addDiagnostic)
        {
            if (zeroValuedFields.Length == 0)
            {
                // Add a member to {0} that has a value of zero with a suggested name of 'None'.
                addDiagnostic(namedType.CreateDiagnostic(RuleNoZero, namedType.Name));
            }
        }

        internal static IEnumerable<IFieldSymbol> GetZeroValuedFields(INamedTypeSymbol enumType)
        {
            var specialType = enumType.EnumUnderlyingType.SpecialType;
            foreach (IFieldSymbol field in enumType.GetMembers().Where(m => m.Kind == SymbolKind.Field))
            {
                if (field.HasConstantValue && IsZeroValueConstant(field.ConstantValue, specialType))
                {
                    yield return field;
                }
            }
        }

        private static bool IsZeroValueConstant(object value, SpecialType specialType)
        {
            ulong convertedValue;
            return DiagnosticHelpers.TryConvertToUInt64(value, specialType, out convertedValue) && convertedValue == 0;
        }

        public static bool IsMemberNamedNone(ISymbol symbol)
        {
            return string.Equals(symbol.Name, "none", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
