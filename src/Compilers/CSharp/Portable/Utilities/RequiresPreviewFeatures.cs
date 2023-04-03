// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Versioning
{
#if !NETCOREAPP

    [AttributeUsage(AttributeTargets.Assembly |
                AttributeTargets.Module |
                AttributeTargets.Class |
                AttributeTargets.Interface |
                AttributeTargets.Delegate |
                AttributeTargets.Struct |
                AttributeTargets.Enum |
                AttributeTargets.Constructor |
                AttributeTargets.Method |
                AttributeTargets.Property |
                AttributeTargets.Field |
                AttributeTargets.Event, Inherited = false)]
    internal sealed class RequiresPreviewFeaturesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresPreviewFeaturesAttribute"/> class.
        /// </summary>
        public RequiresPreviewFeaturesAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresPreviewFeaturesAttribute"/> class with the specified message.
        /// </summary>
        /// <param name="message">An optional message associated with this attribute instance.</param>
        public RequiresPreviewFeaturesAttribute(string? message)
        {
            Message = message;
        }

        /// <summary>
        /// Returns the optional message associated with this attribute instance.
        /// </summary>
        public string? Message { get; }

        /// <summary>
        /// Returns the optional URL associated with this attribute instance.
        /// </summary>
        public string? Url { get; set; }
    }

#endif
}
