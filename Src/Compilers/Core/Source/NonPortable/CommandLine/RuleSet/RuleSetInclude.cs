// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Xml;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a Include tag in a RuleSet file.
    /// </summary>
    public class RuleSetInclude
    {
        private readonly string includePath;
        /// <summary>
        /// The path of the included file.
        /// </summary>
        public string IncludePath
        {
            get { return includePath; }
        }

        private readonly ReportDiagnostic action;
        /// <summary>
        /// The effective action to apply on this included ruleset.
        /// </summary>
        public ReportDiagnostic Action
        {
            get { return action; }
        }

        /// <summary>
        /// Create a RuleSetInclude given the includepath and the effective action.
        /// </summary>
        public  RuleSetInclude(string includePath, ReportDiagnostic action)
        {
            this.includePath = includePath;
            this.action = action;
        }

        /// <summary>
        /// Gets the RuleSet associated with this ruleset include
        /// </summary>
        /// <param name="parent">The parent of this ruleset include</param>
        public RuleSet LoadRuleSet(RuleSet parent)
        {
            // Try to load the rule set
            RuleSet ruleSet = null;
            Exception ex = null;

            string path = includePath;
            try
            {
                path = GetIncludePath(parent);
                ruleSet = RuleSetProcessor.LoadFromFile(path);
            }
            catch (ArgumentException e)
            { ex = e; }
            catch (FileNotFoundException)
            {
                // The compiler uses the same rule set files as FxCop, but doesn't have all of
                // the same logic for resolving included files. For the moment, just ignore any
                // includes we can't resolve.
            }
            catch (IOException e)
            { ex = e; }
            catch (UriFormatException e)
            { ex = e; }
            catch (SecurityException e)
            { ex = e; }
            catch (UnauthorizedAccessException e)
            { ex = e; }
            catch (XmlException e)
            { ex = e; }
            catch (InvalidRuleSetException e)
            { ex = e; }

            if (ex != null)
            {
                throw new InvalidRuleSetException(string.Format(CodeAnalysisResources.InvalidRuleSetInclude, path, ex.Message));
            }

            return ruleSet;
        }

        /// <summary>
        /// Returns a full path to the include file. Relative paths are expanded relative to the current rule set file.
        /// </summary>
        /// <param name="parent">The parent of this rule set include</param>
        private string GetIncludePath(RuleSet parent)
        {
            List<string> found = new List<string>();
            string expandedPath = Environment.ExpandEnvironmentVariables(includePath);

            // If a full path is specified then use it
            if (Path.IsPathRooted(expandedPath))
            {
                if (File.Exists(expandedPath))
                {
                    found.Add(expandedPath);
                }
            }

            // If the current rule set is backed by a file then try to find the include file relative to it
            if (parent != null && !string.IsNullOrEmpty(parent.FilePath))
            {
                string local = Path.Combine(Path.GetDirectoryName(parent.FilePath), expandedPath);
                if (File.Exists(local))
                {
                    found.Add(local);
                }
            }

            // If we still couldn't find it then throw an exception;
            if (found.Count == 0)
            {
                throw new FileNotFoundException(string.Format(CodeAnalysisResources.FailedToResolveRuleSetName, includePath), includePath);
            }

            // Return the canonical full path
            Debug.Assert(found.Count > 0);
            return Path.GetFullPath(found[0]);
        }
    }
}
