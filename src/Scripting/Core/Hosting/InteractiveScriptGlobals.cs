// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Defines global members that common REPL (Read Eval Print Loop) hosts make available in 
    /// the interactive session.
    /// </summary>
    /// <remarks>
    /// It is recommended for hosts to expose the members defined by this class and implement 
    /// the same semantics, so that they can run scripts written against standard hosts. 
    /// 
    /// Specialized hosts that target niche scenarios might choose to not provide this functionality.
    /// </remarks>
    public class InteractiveScriptGlobals
    {
        private readonly TextWriter _outputWriter;
        private readonly ObjectFormatter _objectFormatter;

        /// <summary>
        /// Arguments given to the script.
        /// </summary>
        public IList<string> Args { get; }

        /// <summary>
        /// Pretty-prints an object.
        /// </summary>
        public void Print(object value)
        {
            _outputWriter.WriteLine(_objectFormatter.FormatObject(value, PrintOptions));
        }

        public IList<string> ReferencePaths { get; }
        public IList<string> SourcePaths { get; }

        public PrintOptions PrintOptions { get; }

        public InteractiveScriptGlobals(TextWriter outputWriter, ObjectFormatter objectFormatter)
        {
            if (outputWriter == null)
            {
                throw new ArgumentNullException(nameof(outputWriter));
            }

            if (objectFormatter == null)
            {
                throw new ArgumentNullException(nameof(objectFormatter));
            }

            Debug.Assert(outputWriter != null);
            Debug.Assert(objectFormatter != null);

            ReferencePaths = new SearchPaths();
            SourcePaths = new SearchPaths();
            Args = new List<string>();

            PrintOptions = new PrintOptions();

            _outputWriter = outputWriter;
            _objectFormatter = objectFormatter;
        }
    }
}
