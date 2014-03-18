// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class CurrentProcessFileSystemDiscoveryService : IFileSystemDiscoveryService
    {
        public static readonly CurrentProcessFileSystemDiscoveryService Instance = new CurrentProcessFileSystemDiscoveryService();

        private CurrentProcessFileSystemDiscoveryService()
        {
        }

        public string CurrentDirectory
        {
            get { return Directory.GetCurrentDirectory(); }
        }
    }
}
