// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// CommandLineBuilder derived class for specialized logic specific to MSBuild tasks
    /// </summary>
    public class CommandLineBuilderExtension : CommandLineBuilder
    {
        private bool _isQuotingRequired;

        protected override bool IsQuotingRequired(string parameter)
            => _isQuotingRequired || base.IsQuotingRequired(parameter);

        /// <summary>
        /// Set a boolean switch iff its value exists and its value is 'true'.
        /// </summary>
        internal void AppendWhenTrue
            (
            string switchName,
            PropertyDictionary bag,
            string parameterName
            )
        {
            object? obj = bag[parameterName];
            // If the switch isn't set, don't add it to the command line.
            if (obj != null)
            {
                bool value = (bool)obj;

                if (value)
                {
                    AppendSwitch(switchName);
                }
            }
        }

        /// <summary>
        /// Set a boolean switch only if its value exists.
        /// </summary>
        internal void AppendPlusOrMinusSwitch
            (
            string switchName,
            PropertyDictionary bag,
            string parameterName
            )
        {
            object? obj = bag[parameterName];
            // If the switch isn't set, don't add it to the command line.
            if (obj != null)
            {
                bool value = (bool)obj;
                // Do not quote - or + as they are part of the switch
                AppendSwitchUnquotedIfNotNull(switchName, (value ? "+" : "-"));
            }
        }

        /// <summary>
        /// Set a switch if its value exists by choosing from the input choices
        /// </summary>
        internal void AppendByChoiceSwitch
            (
            string switchName,
            PropertyDictionary bag,
            string parameterName,
            string choice1,
            string choice2
            )
        {
            object? obj = bag[parameterName];
            // If the switch isn't set, don't add it to the command line.
            if (obj != null)
            {
                bool value = (bool)obj;
                AppendSwitchUnquotedIfNotNull(switchName, (value ? choice1 : choice2));
            }
        }

        /// <summary>
        /// Set an integer switch only if its value exists.
        /// </summary>
        internal void AppendSwitchWithInteger
            (
            string switchName,
            PropertyDictionary bag,
            string parameterName
            )
        {
            object? obj = bag[parameterName];
            // If the switch isn't set, don't add it to the command line.
            if (obj != null)
            {
                int value = (int)obj;
                AppendSwitchIfNotNull(switchName, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Adds an aliased switch, used for ResGen:
        ///      /reference:Goo=System.Xml.dll
        /// </summary>
        internal void AppendSwitchAliased(string switchName, string alias, string parameter)
        {
            AppendSwitchUnquotedIfNotNull(switchName, alias + "=");
            AppendTextWithQuoting(parameter);
        }

        /// <summary>
        /// Appends a switch with a value that is force quoted. This will quote as if <see cref="CommandLineBuilder.IsQuotingRequired(string)"/>
        /// returns true for <paramref name="parameter"/>. That means even simple values will be quoted.
        /// </summary>
        internal void AppendTextWithForceQuoting(string parameter)
        {
            Debug.Assert(!_isQuotingRequired);
            _isQuotingRequired = true;
            try
            {
                AppendTextWithQuoting(parameter);
            }
            finally
            {
                _isQuotingRequired = false;
            }
        }

        /// <summary>
        /// Appends a switch with a value that is force quoted. This will quote as if <see cref="CommandLineBuilder.IsQuotingRequired(string)"/>
        /// returns true for <paramref name="parameter"/>. That means even simple values will be quoted.
        /// </summary>
        internal void AppendSwitchForceQuoted(string switchName, string parameter)
        {
            AppendSwitch(switchName);
            AppendTextWithForceQuoting(parameter);
        }

        /// <summary>
        /// Adds a nested switch, used by SGen.exe.  For example:
        ///     /compiler:"/keyfile:\"c:\some folder\myfile.snk\""
        /// </summary>
        internal void AppendNestedSwitch(string outerSwitchName, string innerSwitchName, string parameter)
        {
            string quotedParameter = GetQuotedText(parameter);
            AppendSwitchIfNotNull(outerSwitchName, innerSwitchName + quotedParameter);
        }

        /// <summary>
        /// Returns a quoted string appropriate for appending to a command line.
        /// </summary>
        /// <remarks>
        /// Escapes any double quotes in the string.
        /// </remarks>
        protected string GetQuotedText(string unquotedText)
        {
            StringBuilder quotedText = new StringBuilder();

            AppendQuotedTextToBuffer(quotedText, unquotedText);

            return quotedText.ToString();
        }

        /// <summary>
        /// Appends a command-line switch that takes a compound string parameter. The parameter is built up from the item-spec and
        /// the specified attributes. The switch is appended as many times as there are parameters given.
        /// </summary>
        internal void AppendSwitchIfNotNull
        (
            string switchName,
            ITaskItem[]? parameters,
            string[] attributes
        )
        {
            AppendSwitchIfNotNull(switchName, parameters, attributes, treatAsFlags: null);
        }

        /// <summary>
        /// Append a switch if 'parameter' is not null.
        /// Split on the characters provided.
        /// </summary>
        internal void AppendSwitchWithSplitting(string switchName, string? parameter, string delimiter, params char[] splitOn)
        {
            if (parameter != null)
            {
                string[] splits = parameter.Split(splitOn, /* omitEmptyEntries */ StringSplitOptions.RemoveEmptyEntries);
                string[] splitAndTrimmed = new string[splits.Length];
                for (int i = 0; i < splits.Length; ++i)
                {
                    splitAndTrimmed[i] = splits[i].Trim();
                }
                AppendSwitchIfNotNull(switchName, splitAndTrimmed, delimiter);
            }
        }

        /// <summary>
        /// Returns true if the parameter is empty in spirits, 
        /// even if it contains the separators and white space only
        /// Split on the characters provided.
        /// </summary>
        internal static bool IsParameterEmpty([NotNullWhen(false)] string? parameter, params char[] splitOn)
        {
            if (parameter != null)
            {
                string[] splits = parameter.Split(splitOn, /* omitEmptyEntries */ StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < splits.Length; ++i)
                {
                    if (!String.IsNullOrEmpty(splits[i].Trim()))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Designed to handle the /link and /embed switches:
        ///
        ///      /resource:&lt;filename>[,&lt;name>[,Private]]
        ///      /linkresource:&lt;filename>[,&lt;name>[,Private]]
        /// 
        /// Where the last flag--Private--is either present or not present
        /// depending on whether the ITaskItem has a Private="True" attribute.
        /// </summary>
        internal void AppendSwitchIfNotNull
        (
            string switchName,
            ITaskItem[]? parameters,
            string[]? metadataNames,
            bool[]? treatAsFlags       // May be null. In this case no metadata are treated as flags.
            )
        {
            Debug.Assert(treatAsFlags == null
                         || (metadataNames?.Length == treatAsFlags.Length),
                         "metadataNames and treatAsFlags should have the same length.");

            if (parameters != null)
            {
                foreach (ITaskItem parameter in parameters)
                {
                    AppendSwitchIfNotNull(switchName, parameter.ItemSpec);

                    if (metadataNames != null)
                    {
                        for (int i = 0; i < metadataNames.Length; ++i)
                        {
                            string metadataValue = parameter.GetMetadata(metadataNames[i]);

                            if ((metadataValue != null) && (metadataValue.Length > 0))
                            {
                                // Treat attribute as a boolean flag?
                                if (treatAsFlags == null || treatAsFlags[i] == false)
                                {
                                    // Not a boolean flag.
                                    CommandLine.Append(',');
                                    AppendTextWithQuoting(metadataValue);
                                }
                                else
                                {
                                    // A boolean flag.
                                    bool flagSet = Utilities.TryConvertItemMetadataToBool(parameter, metadataNames[i]);

                                    if (flagSet)
                                    {
                                        CommandLine.Append(',');
                                        AppendTextWithQuoting(metadataNames[i]);
                                    }
                                }
                            }
                            else
                            {
                                if (treatAsFlags == null || treatAsFlags[i] == false)
                                {
                                    // If the caller of this method asked us to add metadata
                                    // A, B, and C, and metadata A doesn't exist on the item,
                                    // then it doesn't make sense to check for B and C.  Because
                                    // since these metadata are just being appended on the
                                    // command-line switch with comma-separation, you can't pass
                                    // in the B metadata unless you've also passed in the A
                                    // metadata.  Otherwise the tool's command-line parser will
                                    // get totally confused.

                                    // This only applies to non-flag attributes.
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void AppendArgumentIfNotNull(string argument)
        {
            if (!string.IsNullOrEmpty(argument))
            {
                AppendSpaceIfNotEmpty();
                AppendTextWithQuoting(argument);
            }
        }
    }
}
