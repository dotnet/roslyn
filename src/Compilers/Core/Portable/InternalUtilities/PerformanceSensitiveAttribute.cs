﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Indicates that a code element is performance sensitive under a known scenario.
    /// </summary>
    /// <remarks>
    /// <para>When applying this attribute, only explicitly set the values for properties specifically indicated by the
    /// test/measurement technique described in the associated <see cref="Uri"/>.</para>
    /// </remarks>
    [Conditional("EMIT_CODE_ANALYSIS_ATTRIBUTES")]
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    internal sealed class PerformanceSensitiveAttribute : Attribute
    {
        public PerformanceSensitiveAttribute(string uri)
        {
            Uri = uri;
        }

        /// <summary>
        /// Gets the location where the original problem is documented, likely with steps to reproduce the issue and/or
        /// validate performance related to a change in the method.
        /// </summary>
        public string Uri
        {
            get;
        }

        /// <summary>
        /// Gets or sets a description of the constraint imposed by the original performance issue.
        /// </summary>
        /// <remarks>
        /// <para>Constraints are normally specified by other specific properties that allow automated validation of the
        /// constraint. This property supports documenting constraints which cannot be described in terms of other
        /// constraint properties.</para>
        /// </remarks>
        public string Constraint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether captures are allowed.
        /// </summary>
        public bool AllowCaptures
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether enumeration of a generic <see cref="IEnumerable{T}"/> is allowed.
        /// </summary>
        public bool AllowGenericEnumeration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether locks are allowed.
        /// </summary>
        public bool AllowLocks
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the asynchronous state machine typically completes synchronously.
        /// </summary>
        /// <remarks>
        /// <para>When <see langword="true"/>, validation of this performance constraint typically involves analyzing
        /// the method to ensure synchronous completion of the state machine does not require the allocation of a
        /// <see cref="Task"/>, either through caching the result or by using ValueTask.</para>
        /// </remarks>
        public bool OftenCompletesSynchronously
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is an entry point to a parallel algorithm.
        /// </summary>
        /// <remarks>
        /// <para>Parallelization APIs and algorithms, e.g. <c>Parallel.ForEach</c>, may be efficient for parallel entry
        /// points (few direct calls but large amounts of iterative work), but are problematic when called inside the
        /// iterations themselves. Performance-sensitive code should avoid the use of heavy parallization APIs except
        /// for known entry points to the parallel portion of code.</para>
        /// </remarks>
        public bool IsParallelEntry
        {
            get;
            set;
        }
    }
}
