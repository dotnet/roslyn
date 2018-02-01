// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal class DocumentKey : IEquatable<DocumentKey>
    {
        private readonly AbstractProject _hostProject;
        private readonly string _moniker;

        public AbstractProject HostProject { get { return _hostProject; } }
        public string Moniker { get { return _moniker; } }

        public DocumentKey(AbstractProject hostProject, string moniker)
        {
            Contract.ThrowIfNull(hostProject);
            Contract.ThrowIfNull(moniker);

            _hostProject = hostProject;
            _moniker = moniker;
        }

        public bool Equals(DocumentKey other)
        {
            return other != null &&
                HostProject == other.HostProject &&
                Moniker.Equals(other.Moniker, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(HostProject.GetHashCode(), StringComparer.OrdinalIgnoreCase.GetHashCode(Moniker));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DocumentKey);
        }
    }
}
