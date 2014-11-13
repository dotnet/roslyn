// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A localizable resource string that may possibly be formatted differently depending on culture.
    /// </summary>
    public sealed class LocalizableResourceString : LocalizableString, IObjectReadable, IObjectWritable
    {
        private readonly string nameOfLocalizableResource;
        private readonly ResourceManager resourceManager;
        private readonly Type resourceSource;

        /// <summary>
        /// Creates a localizable resource string that may possibly be formatted differently depending on culture.
        /// </summary>
        /// <param name="nameOfLocalizableResource">nameof the resource that needs to be localized.</param>
        /// <param name="resourceManager"><see cref="ResourceManager"/> for the calling assembly.</param>
        /// <param name="resourceSource">Type handling assembly's resource management. Typically, this is the static class generated for the resources file from which resources are accessed.</param>
        public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource)
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

            this.resourceManager = resourceManager;
            this.nameOfLocalizableResource = nameOfLocalizableResource;
            this.resourceSource = resourceSource;
        }

        private LocalizableResourceString(ObjectReader reader)
        {
            this.resourceSource = (Type)reader.ReadValue();
            this.nameOfLocalizableResource = reader.ReadString();
            this.resourceManager = new ResourceManager(this.resourceSource);
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            return reader => new LocalizableResourceString(reader);
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteValue(this.resourceSource);
            writer.WriteString(this.nameOfLocalizableResource);
        }

        public override string ToString(IFormatProvider formatProvider)
        {
            var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentUICulture;
            return this.resourceManager.GetString(this.nameOfLocalizableResource, culture);
        }
    }
}
