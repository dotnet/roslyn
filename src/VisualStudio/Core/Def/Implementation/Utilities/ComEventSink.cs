// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class ComEventSink
    {
        public static ComEventSink Advise<T>(object obj, T sink) where T : class
        {
            if (!typeof(T).IsInterface)
            {
                throw new InvalidOperationException();
            }

            if (!(obj is IConnectionPointContainer connectionPointContainer))
            {
                throw new ArgumentException("Not an IConnectionPointContainer", nameof(obj));
            }

            connectionPointContainer.FindConnectionPoint(typeof(T).GUID, out var connectionPoint);
            if (connectionPoint == null)
            {
                throw new InvalidOperationException("Could not find connection point for " + typeof(T).FullName);
            }

            connectionPoint.Advise(sink, out var cookie);

            return new ComEventSink(connectionPoint, cookie);
        }

        private readonly IConnectionPoint _connectionPoint;
        private readonly uint _cookie;
        private bool _unadvised;

        public ComEventSink(IConnectionPoint connectionPoint, uint cookie)
        {
            _connectionPoint = connectionPoint;
            _cookie = cookie;
        }

        public void Unadvise()
        {
            if (_unadvised)
            {
                throw new InvalidOperationException("Already unadvised.");
            }

            _connectionPoint.Unadvise(_cookie);
            _unadvised = true;
        }
    }
}
