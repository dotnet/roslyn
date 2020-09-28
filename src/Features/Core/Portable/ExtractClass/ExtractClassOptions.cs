// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.ExtractClass
{
    internal class ExtractClassOptions
    {
        public string FileName { get; }
        public string TypeName { get; }
        public bool SameFile { get; }
        public ImmutableArray<ExtractClassMemberAnalysisResult> MemberAnalysisResults { get; }

        public ExtractClassOptions(
            string fileName,
            string typeName,
            bool sameFile,
            ImmutableArray<ExtractClassMemberAnalysisResult> memberAnalysisResults)
        {
            FileName = fileName;
            TypeName = typeName;
            MemberAnalysisResults = memberAnalysisResults;
            SameFile = sameFile;
        }
    }

    internal class ExtractClassMemberAnalysisResult
    {
        /// <summary>
        /// The member needs to be pulled up.
        /// </summary>
        public ISymbol Member { get; }

        /// <summary>
        /// Whether to make the member abstract when added to the new class
        /// </summary>
        public bool MakeAbstract { get; }

        public ExtractClassMemberAnalysisResult(
            ISymbol member,
            bool makeAbstract)
        {
            Member = member;
            MakeAbstract = makeAbstract;
        }
    }
}
