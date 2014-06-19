using System;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Common.Semantics
{
    /// <summary>
    /// Options that can be used to modify the symbol lookup mechanism. Multiple options can be combined together.
    /// </summary>
    [Flags]
    public enum CommonLookupOptions
    {
        /// <summary>
        /// Consider all symbols.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Consider only namespaces and types.
        /// </summary>
        NamespacesOrTypesOnly = 1 << 1,

        /// <summary>
        /// Look only for label symbols.  This must be exclusive of all other options.
        /// </summary>
        LabelsOnly = 1 << 2,

        /// <summary>
        /// Do not consider symbols that are instance members.
        /// </summary>
        MustNotBeInstance = 1 << 3,

        /// <summary>
        /// Include extension methods.
        /// </summary>
        IncludeExtensionMethods = 1 << 4,

        /// <summary>
        /// Ignore 'throughType' in accessibility checking. Used in checking accessibility of symbols accessed via 'MyBase' or 'base'.
        /// </summary>
        UseBaseReferenceAccessibility = 1 << 5,

        /// <summary>
        /// Consider only symbols that are instance members. Valid with IncludeExtensionMethods
        /// since extension methods are invoked on an instance.
        /// </summary>
        MustBeInstance = 1 << 6,
    }
}