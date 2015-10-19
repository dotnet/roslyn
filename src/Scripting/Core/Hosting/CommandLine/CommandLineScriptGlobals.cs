﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Defines global members that common command line script hosts expose to the hosted scripts.
    /// </summary>
    /// <remarks>
    /// It is recommended for hosts to expose the members defined by this class and implement 
    /// the same semantics, so that they can run scripts written against standard hosts. 
    /// 
    /// Specialized hosts that target niche scenarios might choose to not provide this functionality.
    /// </remarks>
    public class CommandLineScriptGlobals
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

        internal ObjectFormattingOptions PrintOptions { get; }

        public CommandLineScriptGlobals(TextWriter outputWriter, ObjectFormatter objectFormatter)
        {
            if (outputWriter == null)
            {
                throw new ArgumentNullException(nameof(outputWriter));
            }

            if (objectFormatter == null)
            {
                throw new ArgumentNullException(nameof(objectFormatter));
            }

            PrintOptions = ObjectFormattingOptions.Default;

            _outputWriter = outputWriter;
            _objectFormatter = objectFormatter;

            Args = new List<string>();
        }
    }
}
