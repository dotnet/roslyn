// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.ProjectData;

internal static class ProjectDataDonorConfiguration
{
	internal const string EnabledEnvironmentVariableName = "DOTNET_PROJECTDATA_DONOR_ENABLED";

	internal static bool IsEnabledByEnvironmentValue(string? value)
		=> !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
}
