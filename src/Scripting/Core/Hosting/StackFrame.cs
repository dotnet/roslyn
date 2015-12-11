// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <remarks>
    /// Based on <see cref="System.Diagnostics.StackFrame"/>
    /// </remarks>
    public struct StackFrame
    {
        /// <summary>
        /// Gets the method in which the frame is executing.
        /// </summary>
        /// <returns>
        /// The method in which the frame is executing.
        /// </returns>
        public MethodBase Method { get; }

        /// <summary>
        /// Gets the file name that contains the code that is executing. This information
        /// is typically extracted from the debugging symbols for the executable.
        /// </summary>
        /// <returns>
        /// The file name, or null if the file name cannot be determined.
        /// </returns>
        public string FileName { get; }

        /// <summary>
        /// Gets the line number in the file that contains the code that is executing. This
        /// information is typically extracted from the debugging symbols for the executable.
        /// </summary>
        /// <returns>
        /// The file line number, or 0 (zero) if the file line number cannot be determined.
        /// </returns>
        public int FileLineNumber { get; }

        /// <summary>
        /// Gets the column number in the file that contains the code that is executing.
        /// This information is typically extracted from the debugging symbols for the executable.
        /// </summary>
        /// <returns>
        /// The file column number, or 0 (zero) if the file column number cannot be determined.
        /// </returns>
        public int FileColumnNumber { get; }

        public StackFrame(
            MethodBase method,
            string fileName = null,
            int fileLineNumber = 0,
            int fileColumnNumber = 0)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (fileName == null)
            {
                if (fileLineNumber != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(fileLineNumber));
                }

                if (fileLineNumber != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(fileLineNumber));
                }
            }
            else
            {
                if (fileName.Length == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(fileName));
                }

                if (fileLineNumber <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(fileLineNumber));
                }

                if (fileLineNumber <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(fileLineNumber));
                }
            }

            Method = method;
            FileName = fileName;
            FileLineNumber = fileLineNumber;
            FileColumnNumber = fileColumnNumber;
        }

        public StackFrame(System.Diagnostics.StackFrame frame)
            : this(frame.GetMethod(), frame.GetFileName(), frame.GetFileLineNumber(), frame.GetFileLineNumber())
        {
        }
    }
}