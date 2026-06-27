// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.NET.ProjectData.Generators;

internal static class GeneratorSourceText
{
	public static SourceText From(string source)
		=> SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha256);
}
