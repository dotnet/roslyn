// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

namespace Microsoft.RoslynTools.PRTagger;

public record VSBuildContext(string vsBuild, string vsCommit, string previousVsCommitSha);
