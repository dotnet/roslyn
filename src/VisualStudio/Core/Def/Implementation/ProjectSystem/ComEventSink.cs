// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal static class ComEventSink
    {
        public static IComEventSink Advise<T>(object obj, T sink) where T : class
        {
            if (!typeof(T).IsInterface)
            {
                throw new InvalidOperationException();
            }

            var connectionPointContainer = obj as IConnectionPointContainer;
            if (connectionPointContainer == null)
            {
                throw new ArgumentException("Not an IConnectionPointContainer", nameof(obj));
            }

            IConnectionPoint connectionPoint;
            connectionPointContainer.FindConnectionPoint(typeof(T).GUID, out connectionPoint);
            if (connectionPoint == null)
            {
                throw new InvalidOperationException("Could not find connection point for " + typeof(T).FullName);
            }

            uint cookie;
            connectionPoint.Advise(sink, out cookie);

            return new ComEventSinkImpl(connectionPoint, cookie);
        }

        private class ComEventSinkImpl : IComEventSink
        {
            private IConnectionPoint _connectionPoint;
            private uint _cookie;
            private bool _unadvised;

            public ComEventSinkImpl(IConnectionPoint connectionPoint, uint cookie)
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
}
