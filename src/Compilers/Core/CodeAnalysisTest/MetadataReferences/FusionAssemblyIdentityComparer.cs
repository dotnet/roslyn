// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal sealed class FusionAssemblyIdentityComparer
    {
        // internal for testing
        internal enum AssemblyComparisonResult
        {
            Unknown = 0,

            EquivalentFullMatch = 1,      // all fields match
            EquivalentWeakNamed = 2,      // match based on weak-name, version numbers ignored
            EquivalentFxUnified = 3,      // match based on FX-unification of version numbers
            EquivalentUnified = 4,        // match based on legacy-unification of version numbers
            NonEquivalentVersion = 5,     // all fields match except version field
            NonEquivalent = 6,            // no match

            EquivalentPartialMatch = 7,
            EquivalentPartialWeakNamed = 8,
            EquivalentPartialUnified = 9,
            EquivalentPartialFxUnified = 10,
            NonEquivalentPartialVersion = 11
        }

        private static readonly object s_assemblyIdentityGate = new object();

        internal static AssemblyIdentityComparer.ComparisonResult CompareAssemblyIdentity(string fullName1, string fullName2, bool ignoreVersion, FusionAssemblyPortabilityPolicy policy, out bool unificationApplied)
        {
            unificationApplied = false;
            bool equivalent;
            AssemblyComparisonResult result;
            IntPtr asmConfigCookie = policy == null ? IntPtr.Zero : policy.ConfigCookie;
            int hr = DefaultModelCompareAssemblyIdentity(fullName1, ignoreVersion, fullName2, ignoreVersion, out equivalent, out result, asmConfigCookie);
            if (hr != 0 || !equivalent)
            {
                return AssemblyIdentityComparer.ComparisonResult.NotEquivalent;
            }

            switch (result)
            {
                case AssemblyComparisonResult.EquivalentFullMatch:
                    // all properties match
                    return AssemblyIdentityComparer.ComparisonResult.Equivalent;

                case AssemblyComparisonResult.EquivalentWeakNamed:
                    // both names are weak (have no public key token) and their simple names match:
                    return AssemblyIdentityComparer.ComparisonResult.Equivalent;

                case AssemblyComparisonResult.EquivalentFxUnified:
                    // Framework assembly with unified version.
                    unificationApplied = true;
                    return AssemblyIdentityComparer.ComparisonResult.Equivalent;

                case AssemblyComparisonResult.EquivalentUnified:
                    // Strong named, all properties but version match.
                    Debug.Assert(ignoreVersion);
                    return AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion;

                default:
                    // Partial name was specified:
                    return equivalent ? AssemblyIdentityComparer.ComparisonResult.Equivalent : AssemblyIdentityComparer.ComparisonResult.NotEquivalent;
            }
        }

        internal static int DefaultModelCompareAssemblyIdentity(
            string identity1,
            bool isUnified1,
            string identity2,
            bool isUnified2,
            out bool areEquivalent,
            out AssemblyComparisonResult result,
            IntPtr asmConfigCookie)
        {
            lock (s_assemblyIdentityGate)
            {
                return CompareAssemblyIdentityWithConfig(
                    identity1,
                    isUnified1,
                    identity2,
                    isUnified2,
                    asmConfigCookie,
                    out areEquivalent,
                    out result);
            }
        }

        // internal for testing
        [DllImport("clr", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int CompareAssemblyIdentityWithConfig(
            [MarshalAs(UnmanagedType.LPWStr)] string identity1,
            bool isUnified1,
            [MarshalAs(UnmanagedType.LPWStr)] string identity2,
            bool isUnified2,
            IntPtr asmConfigCookie,
            out bool areEquivalent,
            out AssemblyComparisonResult result);
    }
}
