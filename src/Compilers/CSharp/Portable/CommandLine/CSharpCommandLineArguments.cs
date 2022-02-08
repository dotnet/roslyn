// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The command line arguments to a C# <see cref="CSharpCompiler"/>.
    /// </summary>
    public sealed class CSharpCommandLineArguments : CommandLineArguments
    {
        /// <summary>
        /// Gets the compilation options for the C# <see cref="Compilation"/>
        /// created from the <see cref="CSharpCompiler"/>.
        /// </summary>
        public new CSharpCompilationOptions CompilationOptions { get; internal set; }

        /// <summary>
        /// Gets the parse options for the C# <see cref="Compilation"/>.
        /// </summary>
        public new CSharpParseOptions ParseOptions { get; internal set; }

        protected override ParseOptions ParseOptionsCore
        {
            get { return ParseOptions; }
        }

        protected override CompilationOptions CompilationOptionsCore
        {
            get { return CompilationOptions; }
        }

        /// <value>
        /// Should the format of error messages include the line and column of
        /// the end of the offending text.
        /// </value>
        internal bool ShouldIncludeErrorEndLocation { get; set; }

        internal CSharpCommandLineArguments()
        {
            // Always initialized by CSharpCommandLineParser.Parse
            CompilationOptions = null!;
            ParseOptions = null!;
        }
    }
}
