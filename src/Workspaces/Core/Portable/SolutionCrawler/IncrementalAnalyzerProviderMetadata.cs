﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class IncrementalAnalyzerProviderMetadata
    {
        public bool HighPriorityForActiveFile { get; }
        public string Name { get; }
        public string[] WorkspaceKinds { get; }

        public IncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
        {
            this.HighPriorityForActiveFile = (bool)data.GetValueOrDefault("HighPriorityForActiveFile");
            this.Name = (string)data.GetValueOrDefault("Name");
            this.WorkspaceKinds = (string[])data.GetValueOrDefault("WorkspaceKinds");
        }

        public IncrementalAnalyzerProviderMetadata(string name, bool highPriorityForActiveFile, params string[] workspaceKinds)
        {
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
            this.Name = name;
            this.WorkspaceKinds = workspaceKinds;
        }
    }
}
