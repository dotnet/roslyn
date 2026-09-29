// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.NET.ProjectData;

public sealed class ProjectDataDonorOptions
{
	private ConcurrentDictionary<string, byte>? reportedDonorRoots;

	public static ProjectDataDonorOptions Default { get; } = new();

	public bool Enabled { get; set; } = IsEnabledByEnvironmentValue(
		Environment.GetEnvironmentVariable(ProjectDataDonorConfiguration.EnabledEnvironmentVariableName));

	public string? IndexPath { get; set; }

	public string? WorkspaceRoot { get; set; }

	public int GitDistanceTopK { get; set; } = 4;

	/// <summary>
	/// Gets or sets the trace source that receives donor diagnostics.
	/// When unset, diagnostics are written to the process-wide <see cref="Trace"/>.
	/// </summary>
	public TraceSource? DiagnosticTraceSource { get; set; }

	internal static bool IsEnabledByEnvironmentValue(string? value)
		=> ProjectDataDonorConfiguration.IsEnabledByEnvironmentValue(value);

	internal void TraceDonorUsed(string workspaceRoot)
	{
		ConcurrentDictionary<string, byte> reportedDonorRoots = this.reportedDonorRoots ?? this.InitializeReportedDonorRoots();
		if (reportedDonorRoots.TryAdd(workspaceRoot, 0))
		{
			this.TraceInformation("[donor] Using ProjectData from {0}", workspaceRoot);
		}
	}

	private ConcurrentDictionary<string, byte> InitializeReportedDonorRoots()
	{
		ConcurrentDictionary<string, byte> created = new(ProjectDataDonorIndex.PathComparer);
		return Interlocked.CompareExchange(ref this.reportedDonorRoots, created, comparand: null) ?? created;
	}

	internal void TraceInformation(string format, params object?[] args)
	{
		if (this.DiagnosticTraceSource is TraceSource traceSource)
		{
			traceSource.TraceEvent(TraceEventType.Information, id: 0, format, args);
		}
		else
		{
			Trace.TraceInformation(format, args);
		}
	}

	internal void TraceWarning(string format, params object?[] args)
	{
		if (this.DiagnosticTraceSource is TraceSource traceSource)
		{
			traceSource.TraceEvent(TraceEventType.Warning, id: 0, format, args);
		}
		else
		{
			Trace.TraceWarning(format, args);
		}
	}
}
