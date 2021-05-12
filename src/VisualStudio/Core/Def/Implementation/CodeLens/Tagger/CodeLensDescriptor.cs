// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens.Tagger
{
    internal class CodeLensDescriptor : ICodeLensDescriptor, IEquatable<CodeLensDescriptor>
    {
        public Document Document { get; }

        public string FilePath { get; }
        public string? OutputFilePath { get; }
        public Guid ProjectGuid { get; }

        public CodeLensNodeInfo Info { get; }

        public SyntaxNode SyntaxNode => Info.Node;
        public CodeElementKinds Kind => Info.Kind;
        public FileLinePositionSpan LineSpan => SyntaxNode.GetLocation().GetLineSpan();

        public CodeLensDescriptor(Guid projectGuid, Document document, CodeLensNodeInfo info)
        {
            Contract.ThrowIfNull(document.FilePath);

            Document = document;

            FilePath = document.FilePath;
            OutputFilePath = document.Project.OutputFilePath;
            ProjectGuid = projectGuid;
            Info = info;
        }

        public string ElementDescription => Info.Description;

        /// <summary>
        /// This sets <see cref="Microsoft.VisualStudio.Language.CodeLens.ICodeLensDescriptor.ApplicableSpan"/>
        /// which some CodeLens providers depend on.
        /// </summary>
        public Span? ApplicableSpan => SyntaxNode.Span.ToSpan();

        public override string ToString()
        {
            // This data is used for automation to identify this descriptor.
            if (!string.IsNullOrEmpty(this.ElementDescription))
            {
                return this.ElementDescription;
            }

            return base.ToString();
        }

        public override bool Equals(object obj)
            => obj is CodeLensDescriptor descriptor && Equals(descriptor);

        public override int GetHashCode()
        {
            return Hash.Combine(this.FilePath,
                   Hash.Combine(this.OutputFilePath,
                   Hash.Combine(this.ProjectGuid.GetHashCode(),
                                this.Info.GetHashCode())));
        }

        public bool Equals(CodeLensDescriptor other)
        {
            var result = this.FilePath == other.FilePath &&
                         this.OutputFilePath == other.OutputFilePath &&
                         this.ProjectGuid == other.ProjectGuid &&
                         this.Info == other.Info;

            return result;
        }
    }
}
