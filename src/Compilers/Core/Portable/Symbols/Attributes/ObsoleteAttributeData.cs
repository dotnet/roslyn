﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from <see cref="ObsoleteAttribute"/>.
    /// </summary>
    internal class ObsoleteAttributeData
    {
        private readonly string _message;
        private readonly bool _isError;
        public static readonly ObsoleteAttributeData Uninitialized = new ObsoleteAttributeData();

        private ObsoleteAttributeData() : this(null, false)
        {
        }

        public ObsoleteAttributeData(string message, bool isError)
        {
            _message = message;
            _isError = isError;
        }

        /// <summary>
        /// True if an error should be thrown for the <see cref="ObsoleteAttribute"/>. Default is false in which case
        /// a warning is thrown.
        /// </summary>
        public bool IsError => _isError;

        /// <summary>
        /// The message that will be shown when an error/warning is created for <see cref="ObsoleteAttribute"/>.
        /// </summary>
        public string Message => _message;

        public bool IsUninitialized => ReferenceEquals(this, Uninitialized);
    }
}
