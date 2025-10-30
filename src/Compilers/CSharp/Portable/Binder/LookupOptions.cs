// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Options that can be used to modify the symbol lookup mechanism. 
    /// </summary>
    /// <remarks>
    /// Multiple options can be combined together.  LookupOptions.AreValid checks for valid combinations.
    /// </remarks>
    [Flags]
    internal enum LookupOptions
    {
        /// <summary>
        /// Consider all symbols, using normal accessibility rules.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Consider only namespace aliases and extern aliases.
        /// </summary>
        NamespaceAliasesOnly = 1 << 1,

        /// <summary>
        /// Consider only namespaces and types.
        /// </summary>
        NamespacesOrTypesOnly = 1 << 2,

        /// <summary>
        /// Consider non-members, plus invocable members.
        /// </summary>
        MustBeInvocableIfMember = 1 << 3,

        /// <summary>
        /// Consider only symbols that are instance members. Valid with IncludeExtensionMethods
        /// since extension methods are invoked on an instance.
        /// </summary>
        MustBeInstance = 1 << 4,

        /// <summary>
        /// Do not consider symbols that are instance members.
        /// </summary>
        MustNotBeInstance = 1 << 5,

        /// <summary>
        /// Do not consider symbols that are namespaces.
        /// </summary>
        MustNotBeNamespace = 1 << 6,

        /// <summary>
        /// Consider methods of any arity when arity zero is specified. Because type parameters can be inferred, it is
        /// often desired to consider generic methods when no type arguments were present.
        /// </summary>
        AllMethodsOnArityZero = 1 << 7,

        /// <summary>
        /// Look only for label symbols.  This must be exclusive of all other options.
        /// </summary>
        LabelsOnly = 1 << 8,

        /// <summary>
        /// Usually, when determining if a member is accessible, both the type of the receiver
        /// and the type containing the access are used.  If this flag is specified, then only
        /// the containing type will be used (i.e. as if you've written base.XX).
        /// </summary>
        UseBaseReferenceAccessibility = 1 << 9,

        /// <summary>
        /// Include extension members.
        /// </summary>
        IncludeExtensionMembers = 1 << 10,

        /// <summary>
        /// Consider only attribute types.
        /// </summary>
        AttributeTypeOnly = (1 << 11) | NamespacesOrTypesOnly,

        /// <summary>
        /// Consider lookup name to be a verbatim identifier.
        /// If this flag is specified, then only one lookup is performed for attribute name: lookup with the given name,
        /// and attribute name lookup with "Attribute" suffix is skipped.
        /// </summary>
        VerbatimNameAttributeTypeOnly = (1 << 12) | AttributeTypeOnly,

        /// <summary>
        /// Consider named types of any arity when arity zero is specified. It is specifically desired for nameof in such situations: nameof(System.Collections.Generic.List)
        /// </summary>
        AllNamedTypesOnArityZero = 1 << 13,

        /// <summary>
        /// Do not consider symbols that are method type parameters.
        /// </summary>
        MustNotBeMethodTypeParameter = 1 << 14,

        /// <summary>
        /// Consider only symbols that are abstract or virtual.
        /// </summary>
        MustBeAbstractOrVirtual = 1 << 15,

        /// <summary>
        /// Do not consider symbols that are parameters.
        /// </summary>
        MustNotBeParameter = 1 << 16,

        /// <summary>
        /// Consider only symbols that are user-defined operators.
        /// </summary>
        MustBeOperator = 1 << 17,
    }

    internal static class LookupOptionExtensions
    {
        /// <summary>
        /// Are these options valid in their current combination?
        /// </summary>
        /// <remarks>
        /// Some checks made here:
        /// 
        /// - Default is valid.
        /// - If LabelsOnly is set, it must be the only option.
        /// - If one of MustBeInstance or MustNotBeInstance are set, the other one must not be set.
        /// - If any of MustNotBeInstance, MustBeInstance, or MustNotBeNonInvocableMember are set,
        ///   the options are considered valid.
        /// - If MustNotBeNamespace is set, neither NamespaceAliasesOnly nor NamespacesOrTypesOnly must be set.
        /// - Otherwise, only one of NamespaceAliasesOnly, NamespacesOrTypesOnly, or AllMethodsOnArityZero must be set.
        /// </remarks>
        internal static bool AreValid(this LookupOptions options)
        {
            if (options == LookupOptions.Default)
            {
                return true;
            }

            if ((options & LookupOptions.LabelsOnly) != 0)
            {
                return options == LookupOptions.LabelsOnly;
            }

            // These are exclusive; both must not be present.
            LookupOptions mustBeAndNotBeInstance = (LookupOptions.MustBeInstance | LookupOptions.MustNotBeInstance);
            if ((options & mustBeAndNotBeInstance) == mustBeAndNotBeInstance)
            {
                return false;
            }

            // If MustNotBeNamespace or MustNotBeMethodTypeParameter is set, neither NamespaceAliasesOnly nor NamespacesOrTypesOnly must be set.
            if ((options & (LookupOptions.MustNotBeNamespace | LookupOptions.MustNotBeMethodTypeParameter)) != 0 &&
                (options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly)) != 0)
            {
                return false;
            }

            LookupOptions onlyOptions = options &
                (LookupOptions.NamespaceAliasesOnly
                 | LookupOptions.NamespacesOrTypesOnly
                 | LookupOptions.AllMethodsOnArityZero);

            return OnlyOneBitSet(onlyOptions);
        }

        internal static void ThrowIfInvalid(this LookupOptions options)
        {
            if (!options.AreValid())
            {
                throw new ArgumentException(CSharpResources.LookupOptionsHasInvalidCombo);
            }
        }

        private static bool OnlyOneBitSet(LookupOptions o)
        {
            return (o & (o - 1)) == 0;
        }

        internal static bool CanConsiderMembers(this LookupOptions options)
        {
            return (options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly | LookupOptions.LabelsOnly)) == 0;
        }

        internal static bool CanConsiderLocals(this LookupOptions options)
        {
            return (options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly | LookupOptions.LabelsOnly)) == 0;
        }

        internal static bool CanConsiderTypes(this LookupOptions options)
        {
            return (options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.MustBeInvocableIfMember | LookupOptions.MustBeInstance | LookupOptions.LabelsOnly)) == 0;
        }

        internal static bool CanConsiderNamespaces(this LookupOptions options)
        {
            return (options & (LookupOptions.MustNotBeNamespace | LookupOptions.MustBeInvocableIfMember | LookupOptions.MustBeInstance | LookupOptions.LabelsOnly)) == 0;
        }

        internal static bool IsAttributeTypeLookup(this LookupOptions options)
        {
            return (options & LookupOptions.AttributeTypeOnly) == LookupOptions.AttributeTypeOnly;
        }

        internal static bool IsVerbatimNameAttributeTypeLookup(this LookupOptions options)
        {
            return (options & LookupOptions.VerbatimNameAttributeTypeOnly) == LookupOptions.VerbatimNameAttributeTypeOnly;
        }
    }
}
