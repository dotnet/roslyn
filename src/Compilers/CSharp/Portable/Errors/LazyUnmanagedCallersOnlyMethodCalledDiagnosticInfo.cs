// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo : LazyDiagnosticInfo
    {
        private readonly MethodSymbol _method;
        private readonly bool _isDelegateConversion;

        internal LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo(MethodSymbol method, bool isDelegateConversion)
        {
            _method = method;
            _isDelegateConversion = isDelegateConversion;
        }

        private LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo(LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo original, DiagnosticSeverity severity) : base(original, severity)
        {
            _method = original._method;
            _isDelegateConversion = original._isDelegateConversion;
        }

        protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
        {
            return new LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo(this, severity);
        }

        protected override DiagnosticInfo? ResolveInfo()
        {
            UnmanagedCallersOnlyAttributeData? unmanagedCallersOnlyAttributeData = _method.GetUnmanagedCallersOnlyAttributeData(forceComplete: true);
            Debug.Assert(!ReferenceEquals(unmanagedCallersOnlyAttributeData, UnmanagedCallersOnlyAttributeData.Uninitialized));
            Debug.Assert(!ReferenceEquals(unmanagedCallersOnlyAttributeData, UnmanagedCallersOnlyAttributeData.AttributePresentDataNotBound));

            return unmanagedCallersOnlyAttributeData is null
                ? null
                : new CSDiagnosticInfo(_isDelegateConversion
                                           ? ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate
                                           : ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly,
                                       _method);
        }
    }
}
