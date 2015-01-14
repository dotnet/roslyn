// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        }
    }
}
