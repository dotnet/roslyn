// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A specific location for binding.
    /// </summary>
    [Flags]
    internal enum BinderFlags : uint
    {
        None, // No specific location
        SuppressConstraintChecks = 1 << 0,
        SuppressObsoleteChecks = 1 << 1,
        ConstructorInitializer = 1 << 2,
        FieldInitializer = 1 << 3,
        ObjectInitializerMember = 1 << 4,    // object initializer field/property access
        CollectionInitializerAddMethod = 1 << 5,   // used for collection initializer add method overload resolution diagnostics
        AttributeArgument = 1 << 6,
        GenericConstraintsClause = 1 << 7, // "where" clause (used for cycle checking)
        Cref = 1 << 8, // documentation comment cref
        CrefParameterOrReturnType = 1 << 9, // Same as Cref, but lookup considers inherited members

        /// <summary>
        /// Indicates that the current context allows unsafe constructs.
        /// </summary>
        /// <remarks>
        /// NOTE: Dev10 doesn't seem to treat attributes as being within the unsafe region.
        /// Fortunately, not following this behavior should not be a breaking change since
        /// attribute arguments have to be constants and there are no constants of unsafe
        /// types.
        /// </remarks>
        UnsafeRegion = 1 << 10,

        /// <summary>
        /// Indicates that the unsafe diagnostics are not reported in the current context, regardless
        /// of whether or not it is (part of) an unsafe region.
        /// </summary>
        SuppressUnsafeDiagnostics = 1 << 11,

        /// <summary>
        /// Indicates that this binder is being used to answer SemanticModel questions (i.e. not
        /// for batch compilation).
        /// </summary>
        /// <remarks>
        /// Imports touched by a binder with this flag set are not consider "used".
        /// </remarks>
        SemanticModel = 1 << 12,

        EarlyAttributeBinding = 1 << 13,

        /// <summary>Remarks, mutually exclusive with <see cref="UncheckedRegion"/>.</summary>
        CheckedRegion = 1 << 14,
        /// <summary>Remarks, mutually exclusive with <see cref="CheckedRegion"/>.</summary>
        UncheckedRegion = 1 << 15,

        // Each of these produces a different diagnostic, so we need separate flags.
        InLockBody = 1 << 16, // body, not the expression
        InCatchBlock = 1 << 17,
        InFinallyBlock = 1 << 18,
        InTryBlockOfTryCatch = 1 << 19, // try block must have at least one catch clause
        InCatchFilter = 1 << 20,

        // Indicates that this binder is inside of a finally block that is nested inside
        // of a catch block. This flag resets at every catch clause in the binder chain.
        // This flag is only used to support CS0724. Implies that InFinallyBlock and
        // InCatchBlock are also set.
        InNestedFinallyBlock = 1 << 21,

        IgnoreAccessibility = 1 << 22,

        ParameterDefaultValue = 1 << 23,

        /// <summary>
        /// In the debugger, one can take the address of a managed object.
        /// </summary>
        AllowManagedAddressOf = 1 << 24,

        /// <summary>
        /// In the debugger, the context is always unsafe, but one can still await.
        /// </summary>
        AllowAwaitInUnsafeContext = 1 << 25,

        /// <summary>
        /// Ignore duplicate types from the cor library.
        /// </summary>
        IgnoreCorLibraryDuplicatedTypes = 1 << 26,

        /// <summary>
        /// Set for a binder used to bind a using directive target
        /// </summary>
        InUsing = 1 << 27,

        /// <summary>
        /// When binding imports in scripts/submissions, using aliases (other than from the current submission)
        /// are considered but other imports are not.
        /// </summary>
        InScriptUsing = 1 << 28,

        /// <summary>
        /// In a file that has been included in the compilation via #load.
        /// </summary>
        InLoadedSyntaxTree = 1 << 29,

        /// <summary>
        /// This is a <see cref="ContextualAttributeBinder"/>, or has <see cref="ContextualAttributeBinder"/> as its parent.
        /// </summary>
        InContextualAttributeBinder = 1 << 30,

        /// <summary>
        /// Are we binding for the purpose of an Expression Evaluator
        /// </summary>
        InEEMethodBinder = 1U << 31,

        // Groups

        AllClearedAtExecutableCodeBoundary = InLockBody | InCatchBlock | InCatchFilter | InFinallyBlock | InTryBlockOfTryCatch | InNestedFinallyBlock,
    }
}
