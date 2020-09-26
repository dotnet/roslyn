// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class LazyDiagnosticInfo : DiagnosticInfo
    {
        private DiagnosticInfo? _lazyInfo;

        protected LazyDiagnosticInfo()
            : base(CSharp.MessageProvider.Instance, (int)ErrorCode.Unknown)
        {
        }

        internal sealed override DiagnosticInfo GetResolvedInfo()
        {
            if (_lazyInfo == null)
            {
                Interlocked.CompareExchange(ref _lazyInfo, ResolveInfo() ?? CSDiagnosticInfo.VoidDiagnosticInfo, null);
            }

            return _lazyInfo;
        }

        protected abstract DiagnosticInfo? ResolveInfo();
    }
}
