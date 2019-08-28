// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Compares descriptors by the values that we write to a SARIF log and nothing else.
    ///
    /// We cannot just use <see cref="DiagnosticDescriptor"/>'s built-in implementation
    /// of <see cref="IEquatable{DiagnosticDescriptor}"/> for two reasons:
    ///
    /// 1. <see cref="DiagnosticDescriptor.MessageFormat"/> is part of that built-in 
    ///    equatability, but we do not write it out, and so descriptors differing only
    ///    by MessageFormat (common) would lead to duplicate rule metadata entries in
    ///    the log.
    ///
    /// 2. <see cref="DiagnosticDescriptor.CustomTags"/> is *not* part of that built-in
    ///    equatability, but we do write them out, and so descriptors differing only
    ///    by CustomTags (rare) would cause only one set of tags to be reported in the
    ///    log.
    /// </summary>
    internal sealed class SarifDiagnosticComparer : IEqualityComparer<DiagnosticDescriptor>
    {
        public static readonly SarifDiagnosticComparer Instance = new SarifDiagnosticComparer();

        private SarifDiagnosticComparer()
        {
        }

        public bool Equals(DiagnosticDescriptor x, DiagnosticDescriptor y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            // The properties are guaranteed to be non-null by DiagnosticDescriptor invariants.
            Debug.Assert(x.Description != null && x.Title != null && x.CustomTags != null);
            Debug.Assert(y.Description != null && y.Title != null && y.CustomTags != null);

            return (x.Category == y.Category
                && x.DefaultSeverity == y.DefaultSeverity
                && x.Description.Equals(y.Description)
                && x.HelpLinkUri == y.HelpLinkUri
                && x.Id == y.Id
                && x.IsEnabledByDefault == y.IsEnabledByDefault
                && x.Title.Equals(y.Title)
                && x.CustomTags.SequenceEqual(y.CustomTags));
        }

        public int GetHashCode(DiagnosticDescriptor obj)
        {
            if (obj is null)
            {
                return 0;
            }

            // The properties are guaranteed to be non-null by DiagnosticDescriptor invariants.
            Debug.Assert(obj.Category != null && obj.Description != null && obj.HelpLinkUri != null
                && obj.Id != null && obj.Title != null && obj.CustomTags != null);

            return Hash.Combine(obj.Category.GetHashCode(),
                Hash.Combine(obj.DefaultSeverity.GetHashCode(),
                Hash.Combine(obj.Description.GetHashCode(),
                Hash.Combine(obj.HelpLinkUri.GetHashCode(),
                Hash.Combine(obj.Id.GetHashCode(),
                Hash.Combine(obj.IsEnabledByDefault.GetHashCode(),
                Hash.Combine(obj.Title.GetHashCode(),
                Hash.CombineValues(obj.CustomTags))))))));
        }
    }
}