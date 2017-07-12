// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Represents the information which uniquely defines a document -- the project which contains
    /// it and the moniker.
    /// 
    /// Immutable, since this object is used as a key into some dictionaries.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal class DocumentKey : IEquatable<DocumentKey>
    {
        public IVisualStudioHostProject HostProject { get; }
        public string Moniker { get; }
        public bool IsAdditionalFile { get; }

        public DocumentKey(IVisualStudioHostProject hostProject, string moniker, bool isAdditionalFile)
        {
            Contract.ThrowIfNull(hostProject);
            Contract.ThrowIfNull(moniker);

            HostProject = hostProject;
            Moniker = moniker;
            IsAdditionalFile = isAdditionalFile;
        }

        public bool Equals(DocumentKey other)
        {
            return other != null &&
                HostProject == other.HostProject &&
                Moniker.Equals(other.Moniker, StringComparison.OrdinalIgnoreCase) &&
                IsAdditionalFile.Equals(other.IsAdditionalFile);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(HostProject.GetHashCode(),
                Hash.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(Moniker),
                    IsAdditionalFile.GetHashCode()));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DocumentKey);
        }

        private string GetDebuggerDisplay()
        {
            return $"{Moniker} (additional: {IsAdditionalFile})";
        }
    }
}
