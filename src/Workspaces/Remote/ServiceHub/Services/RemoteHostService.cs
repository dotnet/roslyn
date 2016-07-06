// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: everything in Remote directory should move to its own dll
    //       and all nuget reference to devcore (servicehub/JsonRPC) should move to there as well.
    //       thsi is just temporary for now.
    internal class RemoteHostService : ServiceHubJsonRpcServiceBase
    {
        private string _host;

        public RemoteHostService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
            // this service provide a way for client to make sure remote host is alive
        }

        public string Connect(string host)
        {
            var existing = Interlocked.CompareExchange(ref _host, host, null);

            if (existing != null && existing != host)
            {
                LogError($"{host} is given for {existing}");
            }

            return _host;
        }
    }
}
