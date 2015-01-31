// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal class CurrentWorkingDirectoryDiscoveryService : ICurrentWorkingDirectoryDiscoveryService
    {
        public static readonly CurrentWorkingDirectoryDiscoveryService Instance = new CurrentWorkingDirectoryDiscoveryService();

        private CurrentWorkingDirectoryDiscoveryService()
        {
        }

        public string CurrentDirectory
        {
            get { return Directory.GetCurrentDirectory(); }
        }
    }
}
