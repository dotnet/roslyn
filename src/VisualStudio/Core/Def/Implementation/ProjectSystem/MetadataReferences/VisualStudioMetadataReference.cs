// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Holds a <see cref="Snapshot" /> that represents an individual metadata reference at a certain point in time. A <see cref="Snapshot"/>
    /// is what is actually passed to the compiler as a <see cref="MetadataReference"/>. This type monitors the file for changes and provides new
    /// <see cref="Snapshot"/>s if needed.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed partial class VisualStudioMetadataReference : IDisposable
    {
        private readonly VisualStudioMetadataReferenceManager _provider;
        private readonly MetadataReferenceProperties _properties;
        private readonly FileChangeTracker _fileChangeTracker;

        private Snapshot _currentSnapshot;

        /// <summary>
        /// Event that is raised on the UI thread when this metadata reference is updated on disk.
        /// </summary>
        public event EventHandler<UpdatedOnDiskEventArgs> UpdatedOnDisk;

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
            _fileChangeTracker.StartFileChangeListeningAsync();
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
                    UpdateSnapshot();
                }

                return _currentSnapshot;
            }
        }

        private void OnUpdatedOnDisk(object sender, EventArgs e)
        {
            var beforeSnapshot = CurrentSnapshot;

            UpdateSnapshot();
            UpdatedOnDisk?.Invoke(this, new UpdatedOnDiskEventArgs(beforeSnapshot, _currentSnapshot));
        }

        public void Dispose()
        {
            _fileChangeTracker.Dispose();
            _fileChangeTracker.UpdatedOnDisk -= OnUpdatedOnDisk;

            _provider.StopTrackingSharedMetadataReference(this);
        }

        private void UpdateSnapshot()
        {
            _currentSnapshot = new Snapshot(_provider, Properties, this.FilePath, _fileChangeTracker);
        }

        private string GetDebuggerDisplay()
        {
            return Path.GetFileName(this.FilePath);
        }

        public class UpdatedOnDiskEventArgs : EventArgs
        {
            public UpdatedOnDiskEventArgs(PortableExecutableReference before, PortableExecutableReference after)
            {
                Before = before;
                After = after;
            }

            public PortableExecutableReference Before { get; }
            public PortableExecutableReference After { get; }
        }
    }
}
