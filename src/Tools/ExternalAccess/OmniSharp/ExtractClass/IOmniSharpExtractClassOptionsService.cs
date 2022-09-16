// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ExtractClass
{
    internal interface IOmniSharpExtractClassOptionsService
    {
        Task<OmniSharpExtractClassOptions?> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol originalType, ImmutableArray<ISymbol> selectedMembers);
    }

    internal sealed class OmniSharpExtractClassOptions
    {
        public string FileName { get; }
        public string TypeName { get; }
        public bool SameFile { get; }
        public ImmutableArray<OmniSharpExtractClassMemberAnalysisResult> MemberAnalysisResults { get; }

        public OmniSharpExtractClassOptions(
            string fileName,
            string typeName,
            bool sameFile,
            ImmutableArray<OmniSharpExtractClassMemberAnalysisResult> memberAnalysisResults)
        {
            FileName = fileName;
            TypeName = typeName;
            SameFile = sameFile;
            MemberAnalysisResults = memberAnalysisResults;
        }
    }
    internal sealed class OmniSharpExtractClassMemberAnalysisResult
    {
        /// <summary>
        /// The member needs to be pulled up.
        /// </summary>
        public ISymbol Member { get; }

        /// <summary>
        /// Whether to make the member abstract when added to the new class
        /// </summary>
        public bool MakeAbstract { get; }

        public OmniSharpExtractClassMemberAnalysisResult(
            ISymbol member,
            bool makeAbstract)
        {
            Member = member;
            MakeAbstract = makeAbstract;
        }
    }
}
