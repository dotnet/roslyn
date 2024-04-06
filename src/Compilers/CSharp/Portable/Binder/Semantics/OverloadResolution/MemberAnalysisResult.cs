// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
    internal
#if !DEBUG
    readonly
#endif
    struct MemberAnalysisResult
    {
#if DEBUG
        private readonly ImmutableArray<Conversion> _conversionsOpt;
        public ImmutableArray<Conversion> ConversionsOpt
        {
            get
            {
                Debug.Assert(!_argumentsCoerced);
                return _conversionsOpt;
            }
            private init
            {
                _conversionsOpt = value;
            }
        }

        /// <summary>
        /// A bit vector representing whose true bits indicate indices of bad arguments
        /// </summary>
        /// <remarks>
        /// The capacity of this BitVector might not match the parameter count of the method overload being resolved.
        /// For example, if a method overload has 5 parameters and the second parameter is the only bad parameter, then this
        /// BitVector could end up with Capacity being 2 where BadArguments[0] is false and BadArguments[1] is true.
        /// </remarks>
        private readonly BitVector _badArgumentsOpt;
        public BitVector BadArgumentsOpt
        {
            get
            {
                Debug.Assert(!_argumentsCoerced);
                return _badArgumentsOpt;
            }
            private init
            {
                _badArgumentsOpt = value;
            }
        }

        private readonly ImmutableArray<int> _argsToParamsOpt;
        public ImmutableArray<int> ArgsToParamsOpt
        {
            get
            {
                Debug.Assert(!_argumentsCoerced);
                return _argsToParamsOpt;
            }
            private init
            {
                _argsToParamsOpt = value;
            }
        }

        private readonly ImmutableArray<TypeParameterDiagnosticInfo> _constraintFailureDiagnostics;
        public ImmutableArray<TypeParameterDiagnosticInfo> ConstraintFailureDiagnostics
        {
            get
            {
                Debug.Assert(!_argumentsCoerced);
                return _constraintFailureDiagnostics;
            }
            private init
            {
                _constraintFailureDiagnostics = value;
            }
        }

        private bool _argumentsCoerced;
#else
        // put these first for better packing
        public readonly ImmutableArray<Conversion> ConversionsOpt;

        /// <summary>
        /// A bit vector representing whose true bits indicate indices of bad arguments
        /// </summary>
        /// <remarks>
        /// The capacity of this BitVector might not match the parameter count of the method overload being resolved.
        /// For example, if a method overload has 5 parameters and the second parameter is the only bad parameter, then this
        /// BitVector could end up with Capacity being 2 where BadArguments[0] is false and BadArguments[1] is true.
        /// </remarks>
        public readonly BitVector BadArgumentsOpt;
        public readonly ImmutableArray<int> ArgsToParamsOpt;
        public readonly ImmutableArray<TypeParameterDiagnosticInfo> ConstraintFailureDiagnostics;
#endif

        public readonly int BadParameter;
        public readonly MemberResolutionKind Kind;
        public readonly TypeWithAnnotations ParamsElementTypeOpt;

        /// <summary>
        /// Omit ref feature for COM interop: We can pass arguments by value for ref parameters if we are invoking a method/property on an instance of a COM imported type.
        /// This property returns a flag indicating whether we had any ref omitted argument for the given call.
        /// </summary>
        public readonly bool HasAnyRefOmittedArgument;

        private MemberAnalysisResult(
            MemberResolutionKind kind,
            BitVector badArgumentsOpt = default,
            ImmutableArray<int> argsToParamsOpt = default,
            ImmutableArray<Conversion> conversionsOpt = default,
            int missingParameter = -1,
            bool hasAnyRefOmittedArgument = false,
            ImmutableArray<TypeParameterDiagnosticInfo> constraintFailureDiagnosticsOpt = default,
            TypeWithAnnotations paramsElementTypeOpt = default)
        {
            Debug.Assert(kind != MemberResolutionKind.ApplicableInExpandedForm || paramsElementTypeOpt.HasType);

            this.Kind = kind;
            this.ParamsElementTypeOpt = paramsElementTypeOpt;
            this.BadArgumentsOpt = badArgumentsOpt;
            this.ArgsToParamsOpt = argsToParamsOpt;
            this.ConversionsOpt = conversionsOpt;
            this.BadParameter = missingParameter;
            this.HasAnyRefOmittedArgument = hasAnyRefOmittedArgument;
            this.ConstraintFailureDiagnostics = constraintFailureDiagnosticsOpt.NullToEmpty();
        }

        public override bool Equals(object obj)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override int GetHashCode()
        {
            throw ExceptionUtilities.Unreachable();
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

        public int FirstBadArgument => BadArgumentsOpt.TrueBits().First();

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
                (object)symbol != null && symbol.GetUseSiteInfo().DiagnosticInfo != null;
        }

        private static bool SuppressUseSiteDiagnosticsForKind(MemberResolutionKind kind)
        {
            switch (kind)
            {
                case MemberResolutionKind.UnsupportedMetadata:
                    return true;
                case MemberResolutionKind.NoCorrespondingParameter:
                case MemberResolutionKind.NoCorrespondingNamedParameter:
                case MemberResolutionKind.DuplicateNamedArgument:
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
                case ArgumentAnalysisResultKind.DuplicateNamedArgument:
                    return DuplicateNamedArgument(argAnalysis.ArgumentPosition);
                case ArgumentAnalysisResultKind.RequiredParameterMissing:
                    return RequiredParameterMissing(argAnalysis.ParameterPosition);
                case ArgumentAnalysisResultKind.NameUsedForPositional:
                    return NameUsedForPositional(argAnalysis.ArgumentPosition);
                case ArgumentAnalysisResultKind.BadNonTrailingNamedArgument:
                    return BadNonTrailingNamedArgument(argAnalysis.ArgumentPosition);
                default:
                    throw ExceptionUtilities.UnexpectedValue(argAnalysis.Kind);
            }
        }

        public static MemberAnalysisResult NameUsedForPositional(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.NameUsedForPositional,
                badArgumentsOpt: CreateBadArgumentsWithPosition(argumentPosition));
        }

        public static MemberAnalysisResult BadNonTrailingNamedArgument(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.BadNonTrailingNamedArgument,
                badArgumentsOpt: CreateBadArgumentsWithPosition(argumentPosition));
        }

        public static MemberAnalysisResult NoCorrespondingParameter(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.NoCorrespondingParameter,
                badArgumentsOpt: CreateBadArgumentsWithPosition(argumentPosition));
        }

        public static MemberAnalysisResult NoCorrespondingNamedParameter(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.NoCorrespondingNamedParameter,
                badArgumentsOpt: CreateBadArgumentsWithPosition(argumentPosition));
        }

        public static MemberAnalysisResult DuplicateNamedArgument(int argumentPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.DuplicateNamedArgument,
                badArgumentsOpt: CreateBadArgumentsWithPosition(argumentPosition));
        }

        internal static BitVector CreateBadArgumentsWithPosition(int argumentPosition)
        {
            var badArguments = BitVector.Create(argumentPosition + 1);
            badArguments[argumentPosition] = true;
            return badArguments;
        }

        public static MemberAnalysisResult RequiredParameterMissing(int parameterPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.RequiredParameterMissing,
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

        public static MemberAnalysisResult BadArgumentConversions(ImmutableArray<int> argsToParamsOpt, BitVector badArguments, ImmutableArray<Conversion> conversions, TypeWithAnnotations paramsElementTypeOpt)
        {
            Debug.Assert(conversions.Length != 0);
            Debug.Assert(badArguments.TrueBits().Any());
            return new MemberAnalysisResult(
                MemberResolutionKind.BadArgumentConversion,
                badArguments,
                argsToParamsOpt,
                conversions,
                paramsElementTypeOpt: paramsElementTypeOpt);
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

        public static MemberAnalysisResult StaticInstanceMismatch()
        {
            return new MemberAnalysisResult(MemberResolutionKind.StaticInstanceMismatch);
        }

        public static MemberAnalysisResult ConstructedParameterFailedConstraintsCheck(int parameterPosition)
        {
            return new MemberAnalysisResult(
                MemberResolutionKind.ConstructedParameterFailedConstraintCheck,
                missingParameter: parameterPosition);
        }

        public static MemberAnalysisResult WrongRefKind()
        {
            return new MemberAnalysisResult(MemberResolutionKind.WrongRefKind);
        }

        public static MemberAnalysisResult WrongReturnType()
        {
            return new MemberAnalysisResult(MemberResolutionKind.WrongReturnType);
        }

        public static MemberAnalysisResult LessDerived()
        {
            return new MemberAnalysisResult(MemberResolutionKind.LessDerived);
        }

        public static MemberAnalysisResult NormalForm(ImmutableArray<int> argsToParamsOpt, ImmutableArray<Conversion> conversions, bool hasAnyRefOmittedArgument)
        {
            return new MemberAnalysisResult(MemberResolutionKind.ApplicableInNormalForm, BitVector.Null, argsToParamsOpt, conversions, hasAnyRefOmittedArgument: hasAnyRefOmittedArgument);
        }

        public static MemberAnalysisResult ExpandedForm(ImmutableArray<int> argsToParamsOpt, ImmutableArray<Conversion> conversions, bool hasAnyRefOmittedArgument, TypeWithAnnotations paramsElementType)
        {
            return new MemberAnalysisResult(MemberResolutionKind.ApplicableInExpandedForm, BitVector.Null, argsToParamsOpt, conversions, hasAnyRefOmittedArgument: hasAnyRefOmittedArgument, paramsElementTypeOpt: paramsElementType);
        }

        public static MemberAnalysisResult Worse()
        {
            return new MemberAnalysisResult(MemberResolutionKind.Worse);
        }

        public static MemberAnalysisResult Worst()
        {
            return new MemberAnalysisResult(MemberResolutionKind.Worst);
        }

        internal static MemberAnalysisResult ConstraintFailure(ImmutableArray<TypeParameterDiagnosticInfo> constraintFailureDiagnostics)
        {
            return new MemberAnalysisResult(MemberResolutionKind.ConstraintFailure, constraintFailureDiagnosticsOpt: constraintFailureDiagnostics);
        }

        internal static MemberAnalysisResult WrongCallingConvention()
        {
            return new MemberAnalysisResult(MemberResolutionKind.WrongCallingConvention);
        }

        [Conditional("DEBUG")]
        public void ArgumentsWereCoerced()
        {
#if DEBUG
            _argumentsCoerced = true;
#endif
        }
    }
}
