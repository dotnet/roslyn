// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    // A candidate can be discarded because there are too many or too few arguments, argument ref/out-ness is wrong,
    // argument is not convertible to parameter type, type inference fails to infer valid type parameters, generic arity
    // is wrong, member is on a less-derived type than an applicable member on a more-derived type, or the candidate is
    // worse than another candidate.

    /// <summary>
    /// Indicates why the compiler accepted or rejected the member during overload resolution.
    /// </summary>
    internal enum MemberResolutionKind : byte
    {
        /// <summary>
        /// No resolution has (yet) been determined.
        /// </summary>
        None,

        /// <summary>
        /// The candidate member was accepted in its normal (non-expanded) form.
        /// </summary>
        ApplicableInNormalForm,

        /// <summary>
        /// The candidate member was accepted in its expanded form, after expanding a "params" parameter.
        /// </summary>
        ApplicableInExpandedForm,

        // following results are invalid (i.e. overload resolution rejected this candidate)

        /// <summary>
        /// The candidate member was rejected because an inferred type argument is inaccessible.
        /// </summary>
        InaccessibleTypeArgument,

        /// <summary>
        /// The candidate member was rejected because an argument was specified that did not have a corresponding
        /// parameter.
        /// </summary>
        NoCorrespondingParameter,

        /// <summary>
        /// The candidate member was rejected because a named argument was specified that did not have a corresponding
        /// parameter.
        /// </summary>
        NoCorrespondingNamedParameter,

        /// <summary>
        /// The candidate member was rejected because there were two named arguments with the same parameter name.
        /// </summary>
        DuplicateNamedArgument,

        /// <summary>
        /// The candidate member was rejected because a required parameter had no corresponding argument.
        /// </summary>
        RequiredParameterMissing,

        /// <summary>
        /// The candidate member was rejected because a named argument was used that corresponded to a previously-given positional argument.
        /// </summary>
        NameUsedForPositional,

        /// <summary>
        /// The candidate member was rejected because a named argument was used out-of-position and followed by unnamed arguments.
        /// </summary>
        BadNonTrailingNamedArgument,

        /// <summary>
        /// The candidate member was rejected because it is not supported by the language or cannot be used 
        /// given the current set of assembly references.
        /// </summary>
        UseSiteError,

        /// <summary>
        /// The candidate member was rejected because it is not supported by the language.
        /// </summary>
        /// <remarks>
        /// No diagnostics will be reported for such candidates unless they "win" overload resolution.
        /// </remarks>
        UnsupportedMetadata,

        /// <summary>
        /// The candidate member was rejected because an argument could not be converted to the appropriate parameter
        /// type.
        /// </summary>
        BadArgumentConversion,

        /// <summary>
        /// The candidate member was rejected because type inference failed.
        /// </summary>
        TypeInferenceFailed,

        /// <summary>
        /// The extension method candidate was rejected because type
        /// inference based on the "instance" argument failed.
        /// </summary>
        TypeInferenceExtensionInstanceArgument,

        /// <summary>
        /// The candidate member was rejected because a constraint on the type of a parameter was not satisfied.
        /// </summary>
        ConstructedParameterFailedConstraintCheck,

        /// <summary>
        /// The candidate method's type arguments do not satisfy their constraints.
        /// </summary>
        ConstraintFailure,

        /// <summary>
        /// The candidate member was rejected because it was an instance member accessed from a type,
        /// or a static member accessed from an instance.
        /// </summary>
        StaticInstanceMismatch,

        /// <summary>
        /// The candidate member was rejected because its calling convention did not match the function pointer
        /// calling convention.
        /// </summary>
        WrongCallingConvention,

        /// <summary>
        /// The candidate method in a delegate conversion was rejected because the ref kind of its return does not match the delegate.
        /// </summary>
        WrongRefKind,

        /// <summary>
        /// The candidate method in a delegate conversion was rejected because its return type does not match the return type of the delegate.
        /// </summary>
        WrongReturnType,

        /// <summary>
        /// The candidate member was rejected because another member further down in the inheritance hierarchy was
        /// present.
        /// </summary>
        LessDerived,

        /// <summary>
        /// The candidate member was rejected because it was considered worse that another member (according to section
        /// 7.5.3.2 of the language specification).
        /// </summary>
        Worse,

        /// <summary>
        /// Same as <see cref="Worse"/>, but the candidate shouldn't be mentioned in an ambiguity diagnostics.
        /// </summary>
        Worst,
    }
}
