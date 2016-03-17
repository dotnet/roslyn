// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Roslyn.VisualStudio.Test.Utilities;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    // TODO: This should probably be moved to the VisualStudioTestUtilities project and converted to a IntegrationHost factory
    public sealed class SharedIntegrationHost
    {
        private IntegrationHost _host;

        public IntegrationHost GetHost()
        {
            if (_host == null)
            {
                _host = new IntegrationHost();
            }

            _host.Initialize();
            return _host;
        }
    }
}
