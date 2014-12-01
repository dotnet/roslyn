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
        private readonly string[] formatArguments;
        private static readonly string[] EmptyArguments = new string[0];

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

            this.resourceManager = resourceManager;
            this.nameOfLocalizableResource = nameOfLocalizableResource;
            this.resourceSource = resourceSource;
            this.formatArguments = formatArguments;
        }

        private LocalizableResourceString(ObjectReader reader)
        {
            this.resourceSource = (Type)reader.ReadValue();
            this.nameOfLocalizableResource = reader.ReadString();
            this.resourceManager = new ResourceManager(this.resourceSource);

            var length = (int)reader.ReadCompressedUInt();
            if (length == 0)
            {
                this.formatArguments = EmptyArguments;
            }
            else
            {
                var argumentsBuilder = ArrayBuilder<string>.GetInstance(length);
                for (int i = 0; i < length; i++)
                {
                    argumentsBuilder.Add(reader.ReadString());
                }

                this.formatArguments = argumentsBuilder.ToArrayAndFree();
            }
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            return reader => new LocalizableResourceString(reader);
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteValue(this.resourceSource);
            writer.WriteString(this.nameOfLocalizableResource);
            var length = (uint)this.formatArguments.Length;
            writer.WriteCompressedUInt(length);
            for (int i = 0; i < length; i++)
            {
                writer.WriteString(this.formatArguments[i]);
            }
        }

        public override string ToString(IFormatProvider formatProvider)
        {
            var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentUICulture;
            var resourceString = this.resourceManager.GetString(this.nameOfLocalizableResource, culture);
            return resourceString != null ?
                (this.formatArguments.Length > 0 ? string.Format(resourceString, this.formatArguments) : resourceString) :
                string.Empty;
        }
    }
}
