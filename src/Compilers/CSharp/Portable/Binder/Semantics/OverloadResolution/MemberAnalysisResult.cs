﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
    internal struct MemberAnalysisResult
    {
        // put these first for better packing
        public readonly ImmutableArray<Conversion> ConversionsOpt;
        public readonly ImmutableArray<int> BadArgumentsOpt;
        public readonly ImmutableArray<int> ArgsToParamsOpt;

        public readonly int BadParameter;
        public readonly MemberResolutionKind Kind;

        /// <summary>
        /// Omit ref feature for COM interop: We can pass arguments by value for ref parameters if we are invoking a method/property on an instance of a COM imported type.
        /// This property returns a flag indicating whether we had any ref omitted argument for the given call.
        /// </summary>
        public readonly bool HasAnyRefOmittedArgument;

        private MemberAnalysisResult(MemberResolutionKind kind)
            : this(kind, default(ImmutableArray<int>), default(ImmutableArray<int>), default(ImmutableArray<Conversion>))
        {
        }

        private MemberAnalysisResult(
            MemberResolutionKind kind,
            ImmutableArray<int> badArgumentsOpt,
            ImmutableArray<int> argsToParamsOpt,
            ImmutableArray<Conversion> conversionsOpt,
            int missingParameter = -1,
            bool hasAnyRefOmittedArgument = false)
        {
            this.Kind = kind;
            this.BadArgumentsOpt = badArgumentsOpt;
            this.ArgsToParamsOpt = argsToParamsOpt;
            this.ConversionsOpt = conversionsOpt;
            this.BadParameter = missingParameter;
            this.HasAnyRefOmittedArgument = hasAnyRefOmittedArgument;
        }

        public override bool Equals(object obj)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override int GetHashCode()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public Conversion ConversionForArg(int arg)
        {
            if (this.ConversionsOpt.IsDefault)
            {
                return Conversion.Identity;
            }

            return this.ConversionsOpt[arg];
        }

        public int ParameterFromArgument(int arg)
        {
            Debug.Assert(arg >= 0);
            if (ArgsToParamsOpt.IsDefault)
            {
                return arg;
            }
            Debug.Assert(arg < ArgsToParamsOpt.Length);
            return ArgsToParamsOpt[arg];
        }

        // A method may be applicable, but worse than another method.
        public bool IsApplicable
        {
            get
            {
                switch (this.Kind)
                {
                    case MemberResolutionKind.ApplicableInNormalForm:
                    case MemberResolutionKind.ApplicableInExpandedForm:
                    case MemberResolutionKind.Worse:
                    case MemberResolutionKind.Worst:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsValid
        {
            get
            {
                switch (this.Kind)
                {
                    case MemberResolutionKind.ApplicableInNormalForm:
                    case MemberResolutionKind.ApplicableInExpandedForm:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <remarks>
        /// Returns false for <see cref="MemberResolutionKind.UnsupportedMetadata"/>
        /// because those diagnostics are only reported if no other candidates are
        /// available.
        /// </remarks>
        internal bool HasUseSiteDiagnosticToReportFor(Symbol symbol)
        {
            // There is a use site diagnostic to report here, but it is not reported
            // just because this member was a candidate - only if it "wins".
            return !SuppressUseSiteDiagnosticsForKind(this.Kind) &&
                (object)symbol != null && symbol.GetUseSiteDiagnostic() != null;
        }

        private static bool SuppressUseSiteDiagnosticsForKind(MemberResolutionKind kind)
        {
            switch (kind)
            {
                case MemberResolutionKind.UnsupportedMetadata:
                    return true;
                case MemberResolutionKind.NoCorrespondingParameter:
                case MemberResolutionKind.NoCorrespondingNamedParameter:
                case MemberResolutionKind.NameUsedForPositional:
                case MemberResolutionKind.RequiredParameterMissing:
                case MemberResolutionKind.LessDerived:
                    // Dev12 checks all of these things before considering use site diagnostics.
                    // That is, use site diagnostics are suppressed for candidates rejected for these reasons.
                    return true;
                default:
                    return false;
            }
        }

        public static MemberAnalysisResult ArgumentParameterMismatch(ArgumentAnalysisResult argAnalysis)
        {
            switch (argAnalysis.Kind)
            {
                case ArgumentAnalysisResultKind.NoCorrespondingParameter:
                    return NoCorrespondingParameter(argAnalysis.ArgumentPosition);
                case ArgumentAnalysisResultKind.NoCorrespondingNamedParameter:
                    return NoCorrespondingNamedParameter(argAnalysis.ArgumentPosition);
                case ArgumentAnalysisResultKind.RequiredParameterMissing:
                    return RequiredParameterMissing(argAnalysis.ParameterPosition);
                case ArgumentAnalysisResultKind.NameUsedForPositional:
                    return NameUsedForPositional(argAnalysis.ArgumentPosition);
                default:
                    throw ExceptionUtilities.UnexpectedValue(argAnalysis.Kind);
            }
        }

        public static MemberAnalysisResult NameUsedForPositional(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.NameUsedForPositional,
                ImmutableArray.Create<int>(argumentPosition),
                default(ImmutableArray<int>),
                default(ImmutableArray<Conversion>));
        }

        public static MemberAnalysisResult NoCorrespondingParameter(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.NoCorrespondingParameter,
                ImmutableArray.Create<int>(argumentPosition),
                default(ImmutableArray<int>),
                default(ImmutableArray<Conversion>));
        }

        public static MemberAnalysisResult NoCorrespondingNamedParameter(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.NoCorrespondingNamedParameter,
                ImmutableArray.Create<int>(argumentPosition),
                default(ImmutableArray<int>),
                default(ImmutableArray<Conversion>));
        }

        public static MemberAnalysisResult RequiredParameterMissing(int parameterPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.RequiredParameterMissing,
                default(ImmutableArray<int>),
                default(ImmutableArray<int>),
                default(ImmutableArray<Conversion>),
                missingParameter: parameterPosition);
        }

        public static MemberAnalysisResult UseSiteError()
        {
            return new MemberAnalysisResult(MemberResolutionKind.UseSiteError);
        }

        public static MemberAnalysisResult UnsupportedMetadata()
        {
            return new MemberAnalysisResult(MemberResolutionKind.UnsupportedMetadata);
        }

        public static MemberAnalysisResult BadArgumentConversions(ImmutableArray<int> argsToParamsOpt, ImmutableArray<int> badArguments, ImmutableArray<Conversion> conversions)
        {
            Debug.Assert(conversions.Length != 0);
            Debug.Assert(badArguments.Length != 0);
            return new MemberAnalysisResult(
                MemberResolutionKind.BadArguments,
                badArguments,
                argsToParamsOpt,
                conversions);
        }

        public static MemberAnalysisResult InaccessibleTypeArgument()
        {
            return new MemberAnalysisResult(MemberResolutionKind.InaccessibleTypeArgument);
        }

        public static MemberAnalysisResult TypeInferenceFailed()
        {
            return new MemberAnalysisResult(MemberResolutionKind.TypeInferenceFailed);
        }

        public static MemberAnalysisResult TypeInferenceExtensionInstanceArgumentFailed()
        {
            return new MemberAnalysisResult(MemberResolutionKind.TypeInferenceExtensionInstanceArgument);
        }

        public static MemberAnalysisResult ConstructedParameterFailedConstraintsCheck(int parameterPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.ConstructedParameterFailedConstraintCheck,
                default(ImmutableArray<int>),
                default(ImmutableArray<int>),
                default(ImmutableArray<Conversion>),
                missingParameter: parameterPosition);
        }

        public static MemberAnalysisResult LessDerived()
        {
            return new MemberAnalysisResult(MemberResolutionKind.LessDerived);
        }

        public static MemberAnalysisResult NormalForm(ImmutableArray<int> argsToParamsOpt, ImmutableArray<Conversion> conversions, bool hasAnyRefOmittedArgument)
        {
            return new MemberAnalysisResult(MemberResolutionKind.ApplicableInNormalForm, default(ImmutableArray<int>), argsToParamsOpt, conversions, hasAnyRefOmittedArgument: hasAnyRefOmittedArgument);
        }

        public static MemberAnalysisResult ExpandedForm(ImmutableArray<int> argsToParamsOpt, ImmutableArray<Conversion> conversions, bool hasAnyRefOmittedArgument)
        {
            return new MemberAnalysisResult(MemberResolutionKind.ApplicableInExpandedForm, default(ImmutableArray<int>), argsToParamsOpt, conversions, hasAnyRefOmittedArgument: hasAnyRefOmittedArgument);
        }

        public static MemberAnalysisResult Worse()
        {
            return new MemberAnalysisResult(MemberResolutionKind.Worse);
        }

        public static MemberAnalysisResult Worst()
        {
            return new MemberAnalysisResult(MemberResolutionKind.Worst);
        }
    }
}
