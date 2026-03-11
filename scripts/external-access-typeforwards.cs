// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Usage:
//   dotnet run --file scripts/external-access-typeforwards.cs -- \
//     --source <path-to-implementation-assembly> \
//     --forwarder <path-to-forwarder-assembly> \
//     [--namespace-prefix <prefix>]...
//
// Optional generation:
//   dotnet run --file scripts/external-access-typeforwards.cs -- \
//     --source <path-to-implementation-assembly> \
//     --generate <path-to-TypeForwards.cs> \
//     [--namespace-prefix <prefix>]...

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

var arguments = ParseArgs(args);
if (arguments is null)
{
    PrintUsage();
    return 1;
}

var sourceTypes = ReadDefinedTopLevelTypes(arguments.SourceAssemblyPath, arguments.NamespacePrefixes);

if (arguments.GenerateOutputPath is not null)
{
    var generated = GenerateTypeForwards(sourceTypes);
    Directory.CreateDirectory(Path.GetDirectoryName(arguments.GenerateOutputPath) ?? ".");
    File.WriteAllText(arguments.GenerateOutputPath, generated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"Generated {sourceTypes.Count} forwards in '{arguments.GenerateOutputPath}'.");
}

if (arguments.ForwarderAssemblyPath is not null)
{
    var forwardedTypes = ReadForwardedTopLevelTypes(arguments.ForwarderAssemblyPath, arguments.NamespacePrefixes);

    var missing = sourceTypes.Except(forwardedTypes, StringComparer.Ordinal).OrderBy(static t => t, StringComparer.Ordinal).ToArray();
    var extra = forwardedTypes.Except(sourceTypes, StringComparer.Ordinal).OrderBy(static t => t, StringComparer.Ordinal).ToArray();

    if (missing.Length == 0 && extra.Length == 0)
    {
        Console.WriteLine($"OK: {sourceTypes.Count} source types are forwarded.");
        return 0;
    }

    Console.Error.WriteLine("Type-forward verification failed.");

    if (missing.Length > 0)
    {
        Console.Error.WriteLine("Missing forwards:");
        foreach (var typeName in missing)
            Console.Error.WriteLine($"  {typeName}");
    }

    if (extra.Length > 0)
    {
        Console.Error.WriteLine("Unexpected forwards:");
        foreach (var typeName in extra)
            Console.Error.WriteLine($"  {typeName}");
    }

    return 2;
}

return 0;

static HashSet<string> ReadDefinedTopLevelTypes(string assemblyPath, IReadOnlyList<string> namespacePrefixes)
{
    using var stream = File.OpenRead(assemblyPath);
    using var peReader = new PEReader(stream);
    var metadataReader = peReader.GetMetadataReader();

    var result = new HashSet<string>(StringComparer.Ordinal);

    foreach (var handle in metadataReader.TypeDefinitions)
    {
        var definition = metadataReader.GetTypeDefinition(handle);
        if (!definition.GetDeclaringType().IsNil)
            continue;

        var ns = metadataReader.GetString(definition.Namespace);
        var name = metadataReader.GetString(definition.Name);
        if (name == "<Module>")
            continue;

        var fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        if (!MatchesNamespace(fullName, namespacePrefixes))
            continue;

        result.Add(fullName);
    }

    return result;
}

static HashSet<string> ReadForwardedTopLevelTypes(string assemblyPath, IReadOnlyList<string> namespacePrefixes)
{
    using var stream = File.OpenRead(assemblyPath);
    using var peReader = new PEReader(stream);
    var metadataReader = peReader.GetMetadataReader();

    var result = new HashSet<string>(StringComparer.Ordinal);

    foreach (var handle in metadataReader.ExportedTypes)
    {
        var exportedType = metadataReader.GetExportedType(handle);
        if (!exportedType.IsForwarder)
            continue;

        if (!exportedType.Implementation.IsNil)
        {
            // Skip nested forwarded types; we only compare top-level declarations.
            switch (exportedType.Implementation.Kind)
            {
                case HandleKind.ExportedType:
                    continue;
            }
        }

        var ns = metadataReader.GetString(exportedType.Namespace);
        var name = metadataReader.GetString(exportedType.Name);
        var fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        if (!MatchesNamespace(fullName, namespacePrefixes))
            continue;

        result.Add(fullName);
    }

    return result;
}

static bool MatchesNamespace(string fullTypeName, IReadOnlyList<string> namespacePrefixes)
{
    if (namespacePrefixes.Count == 0)
        return true;

    foreach (var prefix in namespacePrefixes)
    {
        if (fullTypeName.StartsWith(prefix, StringComparison.Ordinal))
            return true;
    }

    return false;
}

static string GenerateTypeForwards(IEnumerable<string> fullTypeNames)
{
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated />");
    sb.AppendLine("using System.Runtime.CompilerServices;");
    sb.AppendLine();

    foreach (var fullName in fullTypeNames.OrderBy(static t => t, StringComparer.Ordinal))
    {
        sb.Append("[assembly: TypeForwardedTo(typeof(");
        sb.Append(ConvertMetadataTypeNameToTypeofSyntax(fullName));
        sb.AppendLine("))]");
    }

    return sb.ToString();
}

static string ConvertMetadataTypeNameToTypeofSyntax(string metadataTypeName)
{
    var tick = metadataTypeName.LastIndexOf('`');
    if (tick < 0)
        return metadataTypeName;

    var arityPart = metadataTypeName[(tick + 1)..];
    if (!int.TryParse(arityPart, out var arity) || arity <= 0)
        return metadataTypeName;

    return metadataTypeName[..tick] + "<" + new string(',', arity - 1) + ">";
}

static Arguments? ParseArgs(string[] args)
{
    string? source = null;
    string? forwarder = null;
    string? generate = null;
    var prefixes = new List<string>();

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--source":
                source = ReadValue(args, ref i);
                break;
            case "--forwarder":
                forwarder = ReadValue(args, ref i);
                break;
            case "--generate":
                generate = ReadValue(args, ref i);
                break;
            case "--namespace-prefix":
                prefixes.Add(ReadValue(args, ref i));
                break;
            default:
                Console.Error.WriteLine($"Unknown argument: {args[i]}");
                return null;
        }
    }

    if (source is null)
        return null;

    if (forwarder is null && generate is null)
        return null;

    return new Arguments(source, forwarder, generate, prefixes);
}

static string ReadValue(string[] args, ref int index)
{
    if (index + 1 >= args.Length)
        throw new ArgumentException($"Missing value for '{args[index]}'.");

    index++;
    return args[index];
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  --source <assembly> [--forwarder <assembly>] [--generate <file>] [--namespace-prefix <prefix>]...");
    Console.WriteLine("At least one of --forwarder or --generate is required.");
}

internal sealed record Arguments(
    string SourceAssemblyPath,
    string? ForwarderAssemblyPath,
    string? GenerateOutputPath,
    IReadOnlyList<string> NamespacePrefixes);
