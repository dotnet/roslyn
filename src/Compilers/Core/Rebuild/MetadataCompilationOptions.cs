// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.Rebuild
{
    internal class MetadataCompilationOptions
    {
        private readonly ImmutableArray<(string optionName, string value)> _options;

        public MetadataCompilationOptions(ImmutableArray<(string optionName, string value)> options)
        {
            _options = options;
        }

        public int Length => _options.Length;

        public bool TryGetUniqueOption(ILogger logger, string optionName, [NotNullWhen(true)] out string? value)
        {
            var result = TryGetUniqueOption(optionName, out value);
            logger.LogInformation($"{optionName} - {value}");
            return result;
        }

        /// <summary>
        /// Attempts to get an option value. Returns false if the option value does not 
        /// exist OR if it exists more than once
        /// </summary>
        public bool TryGetUniqueOption(string optionName, [NotNullWhen(true)] out string? value)
        {
            value = null;

            var optionValues = _options.Where(pair => pair.optionName == optionName).ToArray();
            if (optionValues.Length != 1)
            {
                return false;
            }

            value = optionValues[0].value;
            return true;
        }

        public string GetUniqueOption(string optionName)
        {
            var optionValues = _options.Where(pair => pair.optionName == optionName).ToArray();
            if (optionValues.Length != 1)
            {
                throw new InvalidOperationException(string.Format(RebuildResources._0_exists_1_times_in_compilation_options, optionName, optionValues.Length));
            }

            return optionValues[0].value;
        }

        public string? OptionToString(string option) => TryGetUniqueOption(option, out var value) ? value : null;
        public bool? OptionToBool(string option) => TryGetUniqueOption(option, out var value) ? ToBool(value) : null;
        public T? OptionToEnum<T>(string option) where T : struct => TryGetUniqueOption(option, out var value) ? ToEnum<T>(value) : null;
        public static bool? ToBool(string value) => bool.TryParse(value, out var boolValue) ? boolValue : null;
        public static T? ToEnum<T>(string value) where T : struct => Enum.TryParse<T>(value, out var enumValue) ? enumValue : null;
    }
}
