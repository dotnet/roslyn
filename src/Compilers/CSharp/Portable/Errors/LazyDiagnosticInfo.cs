// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class LazyDiagnosticInfo : DiagnosticInfo
    {
        private DiagnosticInfo _lazyInfo;

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

        protected abstract DiagnosticInfo ResolveInfo();
    }
}
