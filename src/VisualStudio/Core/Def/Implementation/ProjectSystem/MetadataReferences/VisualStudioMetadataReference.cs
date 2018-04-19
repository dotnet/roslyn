﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed partial class VisualStudioMetadataReference : IDisposable
    {
        private readonly VisualStudioMetadataReferenceManager _provider;
        private readonly MetadataReferenceProperties _properties;
        private readonly FileChangeTracker _fileChangeTracker;

        private Snapshot _currentSnapshot;

        public event EventHandler UpdatedOnDisk;

        public VisualStudioMetadataReference(
            VisualStudioMetadataReferenceManager provider,
            string filePath,
            MetadataReferenceProperties properties)
        {
            Contract.ThrowIfTrue(properties.Kind != MetadataImageKind.Assembly);

            _provider = provider;
            _properties = properties;

            // We don't track changes to netmodules linked to the assembly.
            // Any legitimate change in a linked module will cause the assembly to change as well.
            _fileChangeTracker = new FileChangeTracker(provider.FileChangeService, filePath);
            _fileChangeTracker.UpdatedOnDisk += OnUpdatedOnDisk;
            // NOTE: We don't start the listening at this point because it hits the disk and we
            // don't need to do that until someone actually asks us to see the contents, below.
        }

        public string FilePath
        {
            get { return _fileChangeTracker.FilePath; }
        }

        public MetadataReferenceProperties Properties
        {
            get { return _properties; }
        }

        public PortableExecutableReference CurrentSnapshot
        {
            get
            {
                if (_currentSnapshot == null)
                {
                    _fileChangeTracker.StartFileChangeListeningAsync();
                    UpdateSnapshot();
                }

                // make sure we have file notification subscribed
                _fileChangeTracker.EnsureSubscription();
                return _currentSnapshot;
            }
        }

        private void OnUpdatedOnDisk(object sender, EventArgs e)
        {
            UpdatedOnDisk?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _fileChangeTracker.Dispose();
            _fileChangeTracker.UpdatedOnDisk -= OnUpdatedOnDisk;
        }

        public void UpdateSnapshot()
        {
            _currentSnapshot = new Snapshot(_provider, Properties, this.FilePath);
        }

        private string GetDebuggerDisplay()
        {
            return Path.GetFileName(this.FilePath);
        }
    }
}
