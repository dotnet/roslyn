// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
