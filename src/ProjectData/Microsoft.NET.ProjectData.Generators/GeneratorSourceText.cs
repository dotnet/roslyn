// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.NET.ProjectData.Generators;

internal static class GeneratorSourceText
{
	public static SourceText From(string source)
		=> SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha256);
}
