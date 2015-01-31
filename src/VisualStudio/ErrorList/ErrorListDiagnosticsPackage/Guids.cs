// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// MUST match guids.h

using System;

namespace Microsoft.VisualStudio.ErrorListDiagnosticsPackage
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1303:ConstFieldNamesMustBeginWithUpperCaseLetter", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:NonPrivateReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Reviewed.")]
    internal static class GuidList
    {
        public const string guidErrorListDiagnosticsPkgString = "96a53aba-906c-4e94-98a3-79c9b93a4b18";
        public const string guidErrorListDiagnosticsCmdSetString = "a79c0801-0bc6-4931-baaf-39c7f3d3c4be";
        public const string guidToolWindowPersistanceString = "bc3a5e44-d836-454f-952c-24800004d073";

        public static readonly Guid guidErrorListDiagnosticsCmdSet = new Guid(guidErrorListDiagnosticsCmdSetString);
    }
}
