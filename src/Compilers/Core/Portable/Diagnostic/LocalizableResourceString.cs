// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Resources;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A localizable resource string that may possibly be formatted differently depending on culture.
    /// </summary>
    public sealed class LocalizableResourceString : LocalizableString
    {
        private readonly string _nameOfLocalizableResource;
        private readonly ResourceManager _resourceManager;
        private readonly Type _resourceSource;
        private readonly string[] _formatArguments;

        /// <summary>
        /// Creates a localizable resource string with no formatting arguments.
        /// </summary>
        /// <param name="nameOfLocalizableResource">nameof the resource that needs to be localized.</param>
        /// <param name="resourceManager"><see cref="ResourceManager"/> for the calling assembly.</param>
        /// <param name="resourceSource">Type handling assembly's resource management. Typically, this is the static class generated for the resources file from which resources are accessed.</param>
        public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource)
            : this(nameOfLocalizableResource, resourceManager, resourceSource, Array.Empty<string>())
        {
        }

        /// <summary>
        /// Creates a localizable resource string that may possibly be formatted differently depending on culture.
        /// </summary>
        /// <param name="nameOfLocalizableResource">nameof the resource that needs to be localized.</param>
        /// <param name="resourceManager"><see cref="ResourceManager"/> for the calling assembly.</param>
        /// <param name="resourceSource">Type handling assembly's resource management. Typically, this is the static class generated for the resources file from which resources are accessed.</param>
        /// <param name="formatArguments">Optional arguments for formatting the localizable resource string.</param>
        public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource, params string[] formatArguments)
        {
            if (nameOfLocalizableResource == null)
            {
                throw new ArgumentNullException(nameof(nameOfLocalizableResource));
            }

            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            if (formatArguments == null)
            {
                throw new ArgumentNullException(nameof(formatArguments));
            }

            _resourceManager = resourceManager;
            _nameOfLocalizableResource = nameOfLocalizableResource;
            _resourceSource = resourceSource;
            _formatArguments = formatArguments;
        }

        protected override string GetText(IFormatProvider? formatProvider)
        {
            var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentUICulture;
            var resourceString = _resourceManager.GetString(_nameOfLocalizableResource, culture);
            return resourceString != null ?
                (_formatArguments.Length > 0 ? string.Format(resourceString, _formatArguments) : resourceString) :
                string.Empty;
        }

        protected override bool AreEqual(object? other)
        {
            var otherResourceString = other as LocalizableResourceString;
            return otherResourceString != null &&
                _nameOfLocalizableResource == otherResourceString._nameOfLocalizableResource &&
                _resourceManager == otherResourceString._resourceManager &&
                _resourceSource == otherResourceString._resourceSource &&
                _formatArguments.SequenceEqual(otherResourceString._formatArguments, (a, b) => a == b);
        }

        protected override int GetHash()
        {
            return Hash.Combine(_nameOfLocalizableResource.GetHashCode(),
                Hash.Combine(_resourceManager.GetHashCode(),
                Hash.Combine(_resourceSource.GetHashCode(),
                Hash.CombineValues(_formatArguments))));
        }
    }
}
