// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal abstract partial class AbstractLegacyProject : IProjectSiteEx
    {
        private readonly Stack<VisualStudioProject.BatchScope> _batchScopes = new Stack<VisualStudioProject.BatchScope>();

        public void StartBatch()
        {
            _batchScopes.Push(VisualStudioProject.CreateBatchScope());
        }

        public void EndBatch()
        {
            Contract.ThrowIfFalse(_batchScopes.Count > 0);
            var scope = _batchScopes.Pop();
            scope.Dispose();
        }

        public void AddFileEx([MarshalAs(UnmanagedType.LPWStr)] string filePath, [MarshalAs(UnmanagedType.LPWStr)] string linkMetadata)
        {
            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //var sourceCodeKind = extension.Equals(".csx", StringComparison.OrdinalIgnoreCase)
            //    ? SourceCodeKind.Script
            //    : SourceCodeKind.Regular;
            AddFile(filePath, linkMetadata, SourceCodeKind.Regular);
        }

        public void SetProperty([MarshalAs(UnmanagedType.LPWStr)] string property, [MarshalAs(UnmanagedType.LPWStr)] string value)
        {
            // TODO: Handle the properties we care about.
        }
    }
}
