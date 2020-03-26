// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockDebuggeeModuleMetadataProvider : IDebuggeeModuleMetadataProvider
    {
        public FuncInOutOut<Guid, int, string, bool> IsEditAndContinueAvailable;
        public Action<Guid> PrepareModuleForUpdate;
        public Func<Guid, DebuggeeModuleInfo> TryGetBaselineModuleInfo;

        bool IDebuggeeModuleMetadataProvider.IsEditAndContinueAvailable(Guid mvid, out int errorCode, out string localizedMessage)
            => (IsEditAndContinueAvailable ?? throw new NotImplementedException())(mvid, out errorCode, out localizedMessage);

        void IDebuggeeModuleMetadataProvider.PrepareModuleForUpdate(Guid mvid)
            => (PrepareModuleForUpdate ?? throw new NotImplementedException())(mvid);

        DebuggeeModuleInfo IDebuggeeModuleMetadataProvider.TryGetBaselineModuleInfo(Guid mvid)
            => (TryGetBaselineModuleInfo ?? throw new NotImplementedException())(mvid);
    }
}
