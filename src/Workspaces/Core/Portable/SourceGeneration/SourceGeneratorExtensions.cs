// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SourceGeneration;

internal static class SourceGeneratorExtensions
{
    /// <summary>
    /// Determines if this generator is considered required, and should still run when the 
    /// solution <see cref="SolutionCompilationState.GeneratedDocumentCreationPolicy"/> is set to
    /// <see cref="SolutionCompilationState.GeneratedDocumentCreationPolicy.CreateOnlyRequired"/>.
    /// </summary>
    /// <param name="generator">The generator to test</param>
    /// <returns><c>True</c> if the generator is considered 'required'</returns>
    /// <remarks>
    /// Currently, only Razor is considered to be a required generator.
    /// </remarks>
    public static bool IsRequiredGenerator(this ISourceGenerator generator)
    {
        // For now, we hard code the required generator list to Razor.
        // In the future we might want to expand this to e.g. run any generators with open generated files
        return generator.GetGeneratorType().FullName == "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";
    }
}
