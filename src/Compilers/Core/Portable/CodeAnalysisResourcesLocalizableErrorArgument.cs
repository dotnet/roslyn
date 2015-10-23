﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal struct CodeAnalysisResourcesLocalizableErrorArgument : IFormattable, IMessageSerializable
    {
        private readonly string _targetResourceId;

        internal CodeAnalysisResourcesLocalizableErrorArgument(string targetResourceId)
        {
            Debug.Assert(targetResourceId != null);
            _targetResourceId = targetResourceId;
        }

        public override string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (_targetResourceId != null)
            {
                return CodeAnalysisResources.ResourceManager.GetString(_targetResourceId, formatProvider as System.Globalization.CultureInfo);
            }

            return string.Empty;
        }
    }
}
