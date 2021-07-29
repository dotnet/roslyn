// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit
{
    internal interface IIdeSettingsAttribute
    {
        VisualStudioVersion MinVersion { get; }

        VisualStudioVersion MaxVersion { get; }
    }
}
