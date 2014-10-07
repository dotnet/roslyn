using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal abstract class DelegatedProjectInfo : ProjectInfo
    {
        protected abstract ProjectInfo ProjectInfo { get; }

        public override ProjectId Id
        {
            get { return this.ProjectInfo.Id; }
        }

        public override string Name
        {
            get { return this.ProjectInfo.Name; }
        }

        public override string AssemblyName
        {
            get { return this.ProjectInfo.AssemblyName; }
        }

        public override string Language
        {
            get { return this.ProjectInfo.Language; }
        }

        public override string FilePath
        {
            get { return this.ProjectInfo.FilePath; }
        }

        public override string OutputFilePath
        {
            get { return this.ProjectInfo.OutputFilePath; }
        }

        public override CommonCompilationOptions CompilationOptions
        {
            get { return this.ProjectInfo.CompilationOptions; }
        }

        public override CommonParseOptions ParseOptions
        {
            get { return this.ProjectInfo.ParseOptions; }
        }

        public override IEnumerable<DocumentInfo> Documents
        {
            get { return this.ProjectInfo.Documents; }
        }

        public override IEnumerable<ProjectReference> ProjectReferences
        {
            get { return this.ProjectInfo.ProjectReferences; }
        }

        public override IEnumerable<MetadataReference> MetadataReferences
        {
            get { return this.ProjectInfo.MetadataReferences; }
        }

        public override FileResolver FileResolver
        {
            get { return this.ProjectInfo.FileResolver; }
        }

        public override bool IsSubmission
        {
            get { return this.ProjectInfo.IsSubmission; }
        }

        public override Type HostObjectType
        {
            get { return this.ProjectInfo.HostObjectType; }
        }

        public override VersionStamp Version
        {
            get { return this.ProjectInfo.Version; }
        }
    }
}