// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem
{
    internal class CurrentWorkingDirectoryDiscoveryService : ICurrentWorkingDirectoryDiscoveryService
    {
        public static readonly CurrentWorkingDirectoryDiscoveryService Instance = new CurrentWorkingDirectoryDiscoveryService();

        private CurrentWorkingDirectoryDiscoveryService()
        {
        }

        public string WorkingDirectory
        {
            get { return Directory.GetCurrentDirectory(); }
        }

        public static ICurrentWorkingDirectoryDiscoveryService GetService(ITextSnapshot textSnapshot)
        {
            ICurrentWorkingDirectoryDiscoveryService result;
            return textSnapshot.TextBuffer.Properties.TryGetProperty(typeof(ICurrentWorkingDirectoryDiscoveryService), out result)
                ? result
                : Instance;
        }
    }
}
