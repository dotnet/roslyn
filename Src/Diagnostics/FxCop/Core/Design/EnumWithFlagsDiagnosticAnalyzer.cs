// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// Implements CA1027 and CA2217
    /// 
    /// 1) CA1027: Mark enums with FlagsAttribute
    /// 
    /// Cause:
    /// The values of a public enumeration are powers of two or are combinations of other values that are defined in the enumeration,
    /// and the System.FlagsAttribute attribute is not present.
    /// To reduce false positives, this rule does not report a violation for enumerations that have contiguous values.
    /// 
    /// 2) CA2217: Do not mark enums with FlagsAttribute
    /// 
    /// Cause:
    /// An externally visible enumeration is marked with FlagsAttribute and it has one or more values that are not powers of two or
    /// a combination of the other defined values on the enumeration.
    /// </summary>
    public abstract class EnumWithFlagsDiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleIdMarkEnumsWithFlags = "CA1027";
        internal const string RuleIdDoNotMarkEnumsWithFlags = "CA2217";
        internal const string RuleNameForExportAttribute = "EnumWithFlagsAttributeRules";

        internal static DiagnosticDescriptor Rule1027 = new DiagnosticDescriptor(RuleIdMarkEnumsWithFlags,
                                                                             FxCopRulesResources.MarkEnumsWithFlags,
                                                                             FxCopRulesResources.MarkEnumsWithFlagsMessage,
                                                                             FxCopDiagnosticCategory.Design,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: false,
                                                                             description: FxCopRulesResources.MarkEnumsWithFlagsDescription,
                                                                             helpLink: "http://msdn.microsoft.com/library/ms182159.aspx",
                                                                             customTags: DiagnosticCustomTags.Microsoft);

        internal static DiagnosticDescriptor Rule2217 = new DiagnosticDescriptor(RuleIdDoNotMarkEnumsWithFlags,
                                                                             FxCopRulesResources.DoNotMarkEnumsWithFlags,
                                                                             FxCopRulesResources.DoNotMarkEnumsWithFlagsMessage,
                                                                             FxCopDiagnosticCategory.Usage,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: false,
                                                                             description: FxCopRulesResources.DoNotMarkEnumsWithFlagsDescription,
                                                                             helpLink: "http://msdn.microsoft.com/library/ms182335.aspx",
                                                                             customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = ImmutableArray.Create(Rule1027, Rule2217);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return supportedDiagnostics;
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (symbol != null &&
                symbol.TypeKind == TypeKind.Enum &&
                symbol.DeclaredAccessibility == Accessibility.Public)
            {
                var flagsAttributeType = WellKnownTypes.FlagsAttribute(compilation);
                if (flagsAttributeType == null)
                {
                    return;
                }

                IList<ulong> memberValues;
                if (DiagnosticHelpers.TryGetEnumMemberValues(symbol, out memberValues))
                {
                    bool hasFlagsAttribute = symbol.GetAttributes().Any(a => a.AttributeClass == flagsAttributeType);
                    if (hasFlagsAttribute)
                    {
                        // Check "CA2217: Do not mark enums with FlagsAttribute"
                        IEnumerable<ulong> missingValues;
                        if (!ShouldBeFlags(memberValues, out missingValues))
                        {
                            Contract.ThrowIfNull(missingValues);

                            var missingValuesString = missingValues.Select(v => v.ToString()).Aggregate((i, j) => i + ", " + j);
                            var location = GetDiagnosticLocation(symbol.DeclaringSyntaxReferences[0].GetSyntax());
                            addDiagnostic(location.CreateDiagnostic(Rule2217, symbol.Name, missingValuesString));
                        }
                    }
                    else
                    {
                        // Check "CA1027: Mark enums with FlagsAttribute"
                        // Ignore continguous value enums to reduce noise.
                        if (!IsContiguous(memberValues) && ShouldBeFlags(memberValues))
                        {
                            var location = GetDiagnosticLocation(symbol.DeclaringSyntaxReferences[0].GetSyntax());
                            addDiagnostic(location.CreateDiagnostic(Rule1027, symbol.Name));
                        }
                    }
                }
            }
        }

        protected abstract Location GetDiagnosticLocation(SyntaxNode type);

        private static bool IsContiguous(IList<ulong> list)
        {
            Contract.ThrowIfNull(list);

            bool first = true;
            ulong previous = 0;
            foreach (var element in list.Order())
            {
                if (first)
                {
                    first = false;
                }
                else if (element != previous + 1)
                {
                    return false;
                }

                previous = element;
            }

            return true;
        }

        // algorithm makes sure that any enum values that are not powers of two
        // are just combinations of the other powers of two
        private static bool ShouldBeFlags(IList<ulong> enumValues, out IEnumerable<ulong> missingValues)
        {
            ulong missingBits = GetMissingBitsInBinaryForm(enumValues);
            missingValues = GetIndividualBits(missingBits);
            return missingBits == 0;
        }

        // algorithm makes sure that any enum values that are not powers of two
        // are just combinations of the other powers of two
        private static bool ShouldBeFlags(IList<ulong> enumValues)
        {
            return GetMissingBitsInBinaryForm(enumValues) == 0;
        }

        private static ulong GetMissingBitsInBinaryForm(IList<ulong> values)
        {
            Contract.ThrowIfNull(values);

            // all the powers of two that are individually represented
            ulong powersOfTwo = 0;
            bool foundNonPowerOfTwo = false;

            foreach (var value in values)
            {
                if (IsPowerOfTwo(value))
                {
                    powersOfTwo |= value;
                }
                else
                {
                    foundNonPowerOfTwo = true;
                }
            }

            if (foundNonPowerOfTwo)
            {
                // since this is not all powers of two, we need to make sure that each bit
                // is represented by an individual enum value
                ulong missingBits = 0;
                foreach (ulong value in values)
                {
                    if ((value & powersOfTwo) != value)
                    {
                        // we found a value where one of the bits is not represented individually in the enum
                        missingBits |= value & ~(value & powersOfTwo);
                    }
                }

                return missingBits;
            }
            else if (!IsPowerOfTwo(powersOfTwo + 1))
            {
                // All values are powers of two, but one of the powers of two in the sequence is missing.
                // Example: { 1, 2, 4, 16 }, value missing is "8".
                // Compute the missing powers of two.
                var highestSetBit = (ulong)Math.Log(powersOfTwo, 2);
                var nextPowerOfTwo = (ulong)Math.Pow(2, highestSetBit + 1);
                return nextPowerOfTwo - (powersOfTwo + 1);
            }

            return 0;
        }

        private static IEnumerable<ulong> GetIndividualBits(ulong value)
        {
            if (value != 0)
            {
                ulong current = value;
                int shifted = 0;

                while (current != 0)
                {
                    if ((current & 1) == 1)
                    {
                        yield return ((ulong)1) << shifted;
                    }

                    shifted++;
                    current = current >> 1;
                }
            }
        }

        private static bool IsPowerOfTwo(ulong number)
        {
            return number == 0 || ((number & (number - 1)) == 0);
        }
    }
}
