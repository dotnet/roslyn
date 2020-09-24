// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo : DiagnosticInfo
    {
        private DiagnosticInfo? _lazyActualUnmanagedCallersOnlyDiagnostic;

        private readonly MethodSymbol _method;
        private readonly bool _isDelegateConversion;

        internal LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo(MethodSymbol method, bool isDelegateConversion)
            : base(CSharp.MessageProvider.Instance, (int)ErrorCode.Unknown)
        {
            _method = method;
            _lazyActualUnmanagedCallersOnlyDiagnostic = null;
            _isDelegateConversion = isDelegateConversion;
        }

        internal override DiagnosticInfo GetResolvedInfo()
        {
            if (_lazyActualUnmanagedCallersOnlyDiagnostic is null)
            {
                UnmanagedCallersOnlyAttributeData? unmanagedCallersOnlyAttributeData = _method.GetUnmanagedCallersOnlyAttributeData(forceComplete: true);
                Debug.Assert(!ReferenceEquals(unmanagedCallersOnlyAttributeData, UnmanagedCallersOnlyAttributeData.Uninitialized));
                Debug.Assert(!ReferenceEquals(unmanagedCallersOnlyAttributeData, UnmanagedCallersOnlyAttributeData.AttributePresentDataNotBound));

                var info = unmanagedCallersOnlyAttributeData is null
                    ? CSDiagnosticInfo.VoidDiagnosticInfo
                    : new CSDiagnosticInfo(_isDelegateConversion
                                               ? ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate
                                               : ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly,
                                           _method);

                Interlocked.CompareExchange(ref _lazyActualUnmanagedCallersOnlyDiagnostic, info, null);
            }

            return _lazyActualUnmanagedCallersOnlyDiagnostic;
        }
    }
}
