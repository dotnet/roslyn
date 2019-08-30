// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a Include tag in a RuleSet file.
    /// </summary>
    public class RuleSetInclude
    {
        private readonly string _includePath;
        /// <summary>
        /// The path of the included file.
        /// </summary>
        public string IncludePath
        {
            get { return _includePath; }
        }

        private readonly ReportDiagnostic _action;
        /// <summary>
        /// The effective action to apply on this included ruleset.
        /// </summary>
        public ReportDiagnostic Action
        {
            get { return _action; }
        }

        /// <summary>
        /// Create a RuleSetInclude given the include path and the effective action.
        /// </summary>
        public RuleSetInclude(string includePath, ReportDiagnostic action)
        {
            _includePath = includePath;
            _action = action;
        }

        /// <summary>
        /// Gets the RuleSet associated with this ruleset include
        /// </summary>
        /// <param name="parent">The parent of this ruleset include</param>
        public RuleSet LoadRuleSet(RuleSet parent)
        {
            // Try to load the rule set
            RuleSet ruleSet = null;

            string path = _includePath;
            try
            {
                path = GetIncludePath(parent);
                if (path == null)
                {
                    return null;
                }

                ruleSet = RuleSetProcessor.LoadFromFile(path);
            }
            catch (FileNotFoundException)
            {
                // The compiler uses the same rule set files as FxCop, but doesn't have all of
                // the same logic for resolving included files. For the moment, just ignore any
                // includes we can't resolve.
            }
            catch (Exception e)
            {
                throw new InvalidRuleSetException(string.Format(CodeAnalysisResources.InvalidRuleSetInclude, path, e.Message));
            }

            return ruleSet;
        }

        /// <summary>
        /// Returns a full path to the include file. Relative paths are expanded relative to the current rule set file.
        /// </summary>
        /// <param name="parent">The parent of this rule set include</param>
        private string GetIncludePath(RuleSet parent)
        {
            var resolvedIncludePath = ResolveIncludePath(_includePath, parent?.FilePath);
            if (resolvedIncludePath == null)
            {
                return null;
            }

            // Return the canonical full path
            return Path.GetFullPath(resolvedIncludePath);
        }

        private static string ResolveIncludePath(string includePath, string parentRulesetPath)
        {
            var resolvedIncludePath = ResolveIncludePathCore(includePath, parentRulesetPath);
            if (resolvedIncludePath == null && PathUtilities.IsUnixLikePlatform)
            {
                // Attempt to resolve legacy ruleset includes after replacing Windows style directory separator char with current plaform's directory separator char.
                includePath = includePath.Replace('\\', Path.DirectorySeparatorChar);
                resolvedIncludePath = ResolveIncludePathCore(includePath, parentRulesetPath);
            }

            return resolvedIncludePath;
        }

        private static string ResolveIncludePathCore(string includePath, string parentRulesetPath)
        {
            includePath = Environment.ExpandEnvironmentVariables(includePath);

            // If a full path is specified then use it
            if (Path.IsPathRooted(includePath))
            {
                if (File.Exists(includePath))
                {
                    return includePath;
                }
            }
            else if (!string.IsNullOrEmpty(parentRulesetPath))
            {
                // Otherwise, try to find the include file relative to the parent ruleset.
                includePath = PathUtilities.CombinePathsUnchecked(Path.GetDirectoryName(parentRulesetPath), includePath);
                if (File.Exists(includePath))
                {
                    return includePath;
                }
            }

            return null;
        }
    }
}
