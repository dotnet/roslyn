// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptSemanticChangeNotificationServiceWrapper
    {
        private readonly ISemanticChangeNotificationService _underlyingObject;

        public VSTypeScriptSemanticChangeNotificationServiceWrapper(ISemanticChangeNotificationService underlyingObject)
        {
            _underlyingObject = underlyingObject;
        }
    }
}
