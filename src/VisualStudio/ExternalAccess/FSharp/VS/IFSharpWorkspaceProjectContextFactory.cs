// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    internal interface IFSharpWorkspaceProjectContextFactory
    {
        IFSharpWorkspaceProjectContext CreateProjectContext(string filePath, string uniqueName);
    }

    internal interface IFSharpWorkspaceProjectContext : IDisposable
    {
        string DisplayName { get; set; }
        ProjectId Id { get; }
        string FilePath { get; }
        int ProjectReferenceCount { get; }
        bool HasProjectReference(string filePath);
        int MetadataReferenceCount { get; }
        bool HasMetadataReference(string referencePath);
        void SetProjectReferences(IEnumerable<IFSharpWorkspaceProjectContext> projRefs);
        void SetMetadataReferences(IEnumerable<string> referencePaths);
        void AddMetadataReference(string referencePath);
        void AddSourceFile(string path, SourceCodeKind kind);
    }

    [Shared]
    [Export(typeof(FSharpWorkspaceProjectContextFactory))]
    [Export(typeof(IFSharpWorkspaceProjectContextFactory))]
    internal sealed class FSharpWorkspaceProjectContextFactory : IFSharpWorkspaceProjectContextFactory
    {
        private readonly IWorkspaceProjectContextFactory _factory;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpWorkspaceProjectContextFactory(IWorkspaceProjectContextFactory factory, IThreadingContext threadingContext)
        {
            _factory = factory;
            _threadingContext = threadingContext;
        }

        IFSharpWorkspaceProjectContext IFSharpWorkspaceProjectContextFactory.CreateProjectContext(string filePath, string uniqueName)
            => CreateProjectContext(filePath, uniqueName);

        public FSharpWorkspaceProjectContext CreateProjectContext(string filePath, string uniqueName)
            => CreateProjectContext(
                projectUniqueName: uniqueName,
                projectFilePath: filePath,
                projectGuid: Guid.NewGuid(),
                hierarchy: null,
                binOutputPath: null);

        public FSharpWorkspaceProjectContext CreateProjectContext(string projectUniqueName, string projectFilePath, Guid projectGuid, object? hierarchy, string? binOutputPath)
            => new(_threadingContext.JoinableTaskFactory.Run(() => _factory.CreateProjectContextAsync(
                id: projectGuid,
                uniqueName: projectUniqueName,
                languageName: LanguageNames.FSharp,
                data: new FSharpEvaluationData(projectFilePath, binOutputPath),
                hostObject: hierarchy,
                CancellationToken.None)));

        private sealed class FSharpEvaluationData : EvaluationData
        {
            private readonly string _projectFilePath;
            private readonly string? _binOutputPath;

            public FSharpEvaluationData(string projectFilePath, string? binOutputPath)
            {
                _projectFilePath = projectFilePath;
                _binOutputPath = binOutputPath;
            }

            public override string GetPropertyValue(string name)
                => name switch
                {
                    BuildPropertyNames.MSBuildProjectFullPath => _projectFilePath,
                    BuildPropertyNames.TargetPath => _binOutputPath ?? "",
                    _ => "",
                };

            public override ImmutableArray<string> GetItemValues(string name)
                => ImmutableArray<string>.Empty;
        }
    }

    internal sealed class FSharpWorkspaceProjectContext : IFSharpWorkspaceProjectContext
    {
        private readonly IWorkspaceProjectContext _vsProjectContext;

        private ImmutableDictionary<string, IFSharpWorkspaceProjectContext> _projectReferences;
        private ImmutableHashSet<string> _metadataReferences;

        public FSharpWorkspaceProjectContext(IWorkspaceProjectContext vsProjectContext)
        {
            _vsProjectContext = vsProjectContext;
            _projectReferences = ImmutableDictionary.Create<string, IFSharpWorkspaceProjectContext>(StringComparer.OrdinalIgnoreCase);
            _metadataReferences = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
            => _vsProjectContext.Dispose();

        public IVsLanguageServiceBuildErrorReporter2? BuildErrorReporter
            => _vsProjectContext as IVsLanguageServiceBuildErrorReporter2;

        public string DisplayName
        {
            get => _vsProjectContext.DisplayName;
            set => _vsProjectContext.DisplayName = value;
        }

        public string BinOutputPath
        {
            get => _vsProjectContext.BinOutputPath!;
            set => _vsProjectContext.BinOutputPath = value;
        }

        public ProjectId Id
            => _vsProjectContext.Id;

        public string FilePath
            => _vsProjectContext.ProjectFilePath!;

        public int ProjectReferenceCount
            => _projectReferences.Count;

        public bool HasProjectReference(string filePath)
            => _projectReferences.ContainsKey(filePath);

        public int MetadataReferenceCount
            => _metadataReferences.Count;

        public bool HasMetadataReference(string referencePath)
            => _metadataReferences.Contains(referencePath);

        public void SetProjectReferences(IEnumerable<IFSharpWorkspaceProjectContext> projRefs)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, IFSharpWorkspaceProjectContext>();

            foreach (var reference in _projectReferences.Values.Cast<FSharpWorkspaceProjectContext>())
            {
                _vsProjectContext.RemoveProjectReference(reference._vsProjectContext);
            }

            foreach (var reference in projRefs.Cast<FSharpWorkspaceProjectContext>())
            {
                _vsProjectContext.AddProjectReference(reference._vsProjectContext, MetadataReferenceProperties.Assembly);
                builder.Add(reference.FilePath, reference);
            }

            _projectReferences = builder.ToImmutable();
        }

        public void SetMetadataReferences(IEnumerable<string> referencePaths)
        {
            var builder = ImmutableHashSet.CreateBuilder<string>();

            foreach (var referencePath in _metadataReferences)
            {
                RemoveMetadataReference(referencePath);
            }

            foreach (var referencePath in referencePaths)
            {
                AddMetadataReference(referencePath);
                builder.Add(referencePath);
            }

            _metadataReferences = builder.ToImmutable();
        }

        public void RemoveMetadataReference(string referencePath)
            => _vsProjectContext.RemoveMetadataReference(referencePath);

        public void AddMetadataReference(string referencePath)
            => _vsProjectContext.AddMetadataReference(referencePath, MetadataReferenceProperties.Assembly);

        public void AddSourceFile(string path, SourceCodeKind kind)
            => _vsProjectContext.AddSourceFile(path, sourceCodeKind: kind);

        public void RemoveSourceFile(string path)
            => _vsProjectContext.RemoveSourceFile(path);
    }
}
