// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.RuntimeMembers;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Reports all use site errors in special or well known symbols required for anonymous types
        /// </summary>
        /// <returns>true if there was at least one error</returns>
        public bool ReportMissingOrErroneousSymbols(BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;

            ReportErrorOnSymbol(System_Object, diagnostics, ref hasErrors);
            ReportErrorOnSymbol(System_Void, diagnostics, ref hasErrors);
            ReportErrorOnSymbol(System_Boolean, diagnostics, ref hasErrors);
            ReportErrorOnSymbol(System_String, diagnostics, ref hasErrors);
            ReportErrorOnSymbol(System_Int32, diagnostics, ref hasErrors);

            ReportErrorOnSpecialMember(System_Object__Equals, SpecialMember.System_Object__Equals, diagnostics, ref hasErrors);
            ReportErrorOnSpecialMember(System_Object__ToString, SpecialMember.System_Object__ToString, diagnostics, ref hasErrors);
            ReportErrorOnSpecialMember(System_Object__GetHashCode, SpecialMember.System_Object__GetHashCode, diagnostics, ref hasErrors);
            ReportErrorOnWellKnownMember(System_String__Format_IFormatProvider, WellKnownMember.System_String__Format_IFormatProvider, diagnostics, ref hasErrors);

            // optional synthesized attributes:
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Diagnostics_DebuggerBrowsableAttribute__ctor));

            ReportErrorOnWellKnownMember(System_Collections_Generic_EqualityComparer_T__Equals,
                                         WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals,
                                         diagnostics, ref hasErrors);
            ReportErrorOnWellKnownMember(System_Collections_Generic_EqualityComparer_T__GetHashCode,
                                         WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode,
                                         diagnostics, ref hasErrors);
            ReportErrorOnWellKnownMember(System_Collections_Generic_EqualityComparer_T__get_Default,
                                         WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default,
                                         diagnostics, ref hasErrors);

            return hasErrors;
        }

        public bool ReportMissingOrErroneousSymbolsForDelegates(BindingDiagnosticBag diagnostics)
        {
            bool hasErrors = false;

            ReportErrorOnSymbol(System_Object, diagnostics, ref hasErrors);
            ReportErrorOnSymbol(System_IntPtr, diagnostics, ref hasErrors);
            ReportErrorOnSymbol(System_MulticastDelegate, diagnostics, ref hasErrors);

            return hasErrors;
        }

        #region Error reporting implementation

        private static void ReportErrorOnSymbol(Symbol symbol, BindingDiagnosticBag diagnostics, ref bool hasError)
        {
            if ((object)symbol == null)
            {
                return;
            }

            hasError |= diagnostics.ReportUseSite(symbol, NoLocation.Singleton);
        }

        private static void ReportErrorOnSpecialMember(Symbol symbol, SpecialMember member, BindingDiagnosticBag diagnostics, ref bool hasError)
        {
            if ((object)symbol == null)
            {
                MemberDescriptor memberDescriptor = SpecialMembers.GetDescriptor(member);
                diagnostics.Add(ErrorCode.ERR_MissingPredefinedMember, NoLocation.Singleton,
                    memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name);
                hasError = true;
            }
            else
            {
                ReportErrorOnSymbol(symbol, diagnostics, ref hasError);
            }
        }

        private static void ReportErrorOnWellKnownMember(Symbol symbol, WellKnownMember member, BindingDiagnosticBag diagnostics, ref bool hasError)
        {
            if ((object)symbol == null)
            {
                MemberDescriptor memberDescriptor = WellKnownMembers.GetDescriptor(member);
                diagnostics.Add(ErrorCode.ERR_MissingPredefinedMember, NoLocation.Singleton,
                    memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name);
                hasError = true;
            }
            else
            {
                ReportErrorOnSymbol(symbol, diagnostics, ref hasError);
                ReportErrorOnSymbol(symbol.ContainingType, diagnostics, ref hasError);
            }
        }

        #endregion

        #region Symbols

        public NamedTypeSymbol System_Object
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Object); }
        }

        public NamedTypeSymbol System_Void
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Void); }
        }

        public NamedTypeSymbol System_Boolean
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Boolean); }
        }

        public NamedTypeSymbol System_String
        {
            get { return Compilation.GetSpecialType(SpecialType.System_String); }
        }

        public NamedTypeSymbol System_Int32
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Int32); }
        }

        public NamedTypeSymbol System_IntPtr
        {
            get { return Compilation.GetSpecialType(SpecialType.System_IntPtr); }
        }

        public NamedTypeSymbol System_MulticastDelegate
        {
            get { return Compilation.GetSpecialType(SpecialType.System_MulticastDelegate); }
        }

        public NamedTypeSymbol System_Diagnostics_DebuggerBrowsableState
        {
            get { return Compilation.GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerBrowsableState); }
        }

        public MethodSymbol System_Object__Equals
        {
            get { return this.Compilation.GetSpecialTypeMember(SpecialMember.System_Object__Equals) as MethodSymbol; }
        }

        public MethodSymbol System_Object__ToString
        {
            get { return this.Compilation.GetSpecialTypeMember(SpecialMember.System_Object__ToString) as MethodSymbol; }
        }

        public MethodSymbol System_Object__GetHashCode
        {
            get { return this.Compilation.GetSpecialTypeMember(SpecialMember.System_Object__GetHashCode) as MethodSymbol; }
        }

        public MethodSymbol System_Collections_Generic_EqualityComparer_T__Equals
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals) as MethodSymbol; }
        }

        public MethodSymbol System_Collections_Generic_EqualityComparer_T__GetHashCode
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode) as MethodSymbol; }
        }

        public MethodSymbol System_Collections_Generic_EqualityComparer_T__get_Default
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default) as MethodSymbol; }
        }

        public MethodSymbol System_String__Format_IFormatProvider
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Format_IFormatProvider) as MethodSymbol; }
        }

        #endregion
    }
}
