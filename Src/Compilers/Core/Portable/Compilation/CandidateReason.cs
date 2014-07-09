// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Indicates the reasons why a candidate (or set of candidate) symbols were not considered
    /// correct in SemanticInfo. Higher values take precedence over lower values, so if, for
    /// example, there a symbol with a given name that was inaccessible, and other with the wrong
    /// arity, only the inaccessible one would be reported in the SemanticInfo.
    /// </summary>
    public enum CandidateReason
    {
        // Implementation note. Values in this enumeration should generally be kept in sync with the
        // language-specific LookupResultKind enumeration.

        /// <summary>
        /// No CandidateSymbols.
        /// </summary>
        None,

        /// <summary>
        /// Only a type or namespace was valid in the given location, but the candidate symbols was
        /// of the wrong kind.
        /// </summary>
        NotATypeOrNamespace,

        /// <summary>
        /// Only an event was valid in the given location, but the candidate symbols was
        /// of the wrong kind.
        /// </summary>
        NotAnEvent,

        /// <summary>
        /// The candidate symbol must be a WithEvents member, but it was not. 
        /// </summary>
        NotAWithEventsMember,

        /// <summary>
        /// Only an attribute type was valid in the given location, but the candidate symbol was
        /// of the wrong kind.
        /// </summary>
        NotAnAttributeType,

        /// <summary>
        /// The candidate symbol takes a different number of type parameters that was required.
        /// </summary>
        WrongArity,

        /// <summary>
        /// The candidate symbol existed, but was not allowed to be created in a new expression. 
        /// For example, interfaces, static classes, and unconstrained type parameters.
        /// </summary>
        NotCreatable,

        /// <summary>
        /// The candidate symbol existed, but was not allowed to be referenced. For example, the
        /// "get_XXX" method used to implement a property named "XXX" may not be directly
        /// referenced. Similarly, the type "System.Void" can not be directly referenced.
        /// Also occurs if "this" is used in a context (static method or field initializer)
        /// where "this" is not available.
        /// </summary>
        NotReferencable,

        /// <summary>
        /// The candidate symbol had an accessibility modifier (private, protected, ...) that made
        /// it inaccessible.
        /// </summary>
        Inaccessible,

        /// <summary>
        /// The candidate symbol was in a place where a value was required, but was not a value
        /// (e.g., was a type or namespace).
        /// </summary>
        NotAValue,

        /// <summary>
        /// The candidate symbol was in a place where a variable (or sometimes, a property) was
        /// required, but was not allowed there because it isn't a symbol that can be assigned to. 
        /// For example, the left hand side of an assignment, or a ref or out parameter.
        /// </summary>
        NotAVariable,

        /// <summary>
        /// The candidate symbol was used in a way that an invocable member (method, or variable of
        /// delegate type) was required, but the candidate symbol was not invocable.
        /// </summary>
        NotInvocable,

        /// <summary>
        /// The candidate symbol must be an instance variable, but was used as static, or the
        /// reverse. 
        /// </summary>
        StaticInstanceMismatch,

        /// <summary>
        /// Overload resolution did not choose a method. The candidate symbols are the methods there
        /// were considered during overload resolution (which may or may not be applicable methods). 
        /// </summary>
        OverloadResolutionFailure,

        /// <summary>
        /// Method could not be selected statically.
        /// The candidate symbols are the methods there were considered during overload resolution 
        /// (which may or may not be applicable methods). 
        /// </summary>
        LateBound,

        /// <summary>
        /// Multiple ambiguous symbols were available with the same name. This can occur if "using"
        /// statements bring multiple namespaces into scope, and the same type is available in
        /// multiple. This can also occur if multiple properties of the same name are available in a
        /// multiple interface inheritance situation.
        /// </summary>
        Ambiguous,
    }
}