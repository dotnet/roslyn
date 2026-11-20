// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal static class SnapshotHistoryDiagnostics
{
    private static readonly object s_gate = new();
    private static readonly HashSet<ProjectId> s_projects = [];
    private static readonly ConditionalWeakTable<object, Identity> s_identities = new();
    private static readonly Queue<string> s_pending = new();
    private static readonly Encoding s_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Lazy<string> s_logPath = new(CreateLogPath);
    private static int s_nextIdentity;

    internal static void Record(string phase, ProjectState project, object versionSource, bool versionCached)
    {
        var documents = 0;
        var parsedDocuments = 0;
        var designerDocuments = 0;
        var parsedDesignerDocuments = 0;

        foreach (var (_, document) in project.DocumentStates.States)
        {
            documents++;
            var parsed = document.TryGetSyntaxTree(out _);
            var name = document.Attributes.Name;
            var designer = name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Designer.vb", StringComparison.OrdinalIgnoreCase);

            if (parsed)
                parsedDocuments++;

            if (designer)
            {
                designerDocuments++;
                if (parsed)
                    parsedDesignerDocuments++;
            }
        }

        lock (s_gate)
        {
            if (!s_projects.Contains(project.Id))
            {
                if (designerDocuments == 0)
                    return;

                s_projects.Add(project.Id);
            }

            var line = string.Format(
                CultureInfo.InvariantCulture,
                "utcTicks={0}\tclockTicks={1}\tphase={2}\tproject={3:D}\tstate={4}\tlazy={5}\tdocuments={6}\tparsed={7}\tdesigners={8}\tparsedDesigners={9}\tcached={10}\n",
                DateTime.UtcNow.Ticks, Stopwatch.GetTimestamp(), phase, project.Id.Id,
                GetIdentity(project), GetIdentity(versionSource), documents, parsedDocuments,
                designerDocuments, parsedDesignerDocuments, versionCached);
            s_pending.Enqueue(line);
            if (phase == "version-complete")
            {
                // Keep file I/O out of the snapshot-construction and version-computation intervals.
                using (var writer = new StreamWriter(s_logPath.Value, append: true, encoding: s_encoding))
                {
                    foreach (var pending in s_pending)
                        writer.Write(pending);
                }

                s_pending.Clear();
            }
        }
    }

    private static int GetIdentity(object value)
    {
        if (!s_identities.TryGetValue(value, out var identity))
        {
            identity = new Identity(++s_nextIdentity);
            s_identities.Add(value, identity);
        }

        return identity.Value;
    }

    private static string CreateLogPath()
    {
        using var process = Process.GetCurrentProcess();
        var directory = Path.Combine(Path.GetTempPath(), "ServiceHub", "Logs");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"roslyn-snapshot-history-{process.Id}-{Guid.NewGuid():N}.log");
        var version = typeof(ProjectState).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Contract.ThrowIfNull(version);
        File.WriteAllText(
            path,
            string.Format(
                CultureInfo.InvariantCulture,
                "phase=process\tpid={0}\truntime={1}\tclockFrequency={2}\tassembly={3}\n",
                process.Id, Environment.Version, Stopwatch.Frequency, version.InformationalVersion),
            s_encoding);
        return path;
    }

    private sealed class Identity(int value)
    {
        public int Value { get; } = value;
    }
}
