// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Service that client will connect to to make service hub alive even when there is
    /// no other people calling service hub.
    /// 
    /// basically, this is used to manage lifetime of the service hub.
    /// </summary>
    internal class RemoteHostService : ServiceHubServiceBase
    {
        private const string LoggingFunctionIdTextFileName = "ServiceHubFunctionIds.txt";

        private string _host;

        static RemoteHostService()
        {
            // this is the very first service which will be called from client (VS)
            // we set up logger here
            RoslynLogger.SetLogger(new EtwLogger(GetLoggingChecker()));
        }

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

        private static Func<FunctionId, bool> GetLoggingChecker()
        {
            try
            {
                var loggingConfigFile = Path.Combine(typeof(RemoteHostService).Assembly.Location, LoggingFunctionIdTextFileName);

                if (File.Exists(loggingConfigFile))
                {
                    var set = new HashSet<FunctionId>();

                    var functionIdType = typeof(FunctionId);
                    var functionIdStrings = File.ReadAllLines(loggingConfigFile);

                    foreach (var functionIdString in functionIdStrings)
                    {
                        try
                        {
                            set.Add((FunctionId)Enum.Parse(functionIdType, functionIdString.Trim(), ignoreCase: true));
                        }
                        catch
                        {
                            // unknown functionId, move on
                            continue;
                        }
                    }

                    return id => set.Contains(id);
                }
            }
            catch
            {
                // we don't care any exception here. 
                // this is for debugging and performance investigation purpose.
            }

            // if there was any kind of issue, 
            // don't log anything
            return _ => false;
        }
    }
}
