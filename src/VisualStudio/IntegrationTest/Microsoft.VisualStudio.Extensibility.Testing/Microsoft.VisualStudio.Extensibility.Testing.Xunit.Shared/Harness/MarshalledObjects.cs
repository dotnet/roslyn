// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Remoting;

    internal sealed class MarshalledObjects : IDisposable
    {
        private readonly List<MarshalByRefObject> _marshalledObjects = new();

        public void Add(MarshalByRefObject marshalledObject)
            => _marshalledObjects.Add(marshalledObject);

        public void Dispose()
        {
            foreach (var marshalledObject in _marshalledObjects)
            {
                if (!RemotingServices.IsTransparentProxy(marshalledObject))
                {
                    RemotingServices.Disconnect(marshalledObject);
                }
            }
        }
    }
}
