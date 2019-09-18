using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal class UnitTestingRemoteHostClientServiceWrapper : IUnitTestingRemoteHostClientServiceAccessor
    {
        private readonly IRemoteHostClientService _implementation;

        public UnitTestingRemoteHostClientServiceWrapper(IRemoteHostClientService implementation)
            => _implementation = implementation;

        public void Enable()
        {
            throw new NotImplementedException();
        }
    }
}
