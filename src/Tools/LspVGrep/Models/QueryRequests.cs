namespace LspVGrepTool.Models;

internal static class QueryTypes
{
    public const string FindTypeDefinition = "find-type-definition";
    public const string FindInterfaceImplementation = "find-interface-implementation";
    public const string FindDerivedTypes = "find-derived-types";
}

internal abstract record QueryRequest
{
    public abstract string Type { get; }

    public abstract IReadOnlyDictionary<string, string> GetDisplayFields();
}

internal sealed record FindTypeDefinitionQuery(string Name) : QueryRequest
{
    public override string Type => QueryTypes.FindTypeDefinition;

    public override IReadOnlyDictionary<string, string> GetDisplayFields() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = Name
        };
}

internal sealed record FindInterfaceImplementationQuery(string Name) : QueryRequest
{
    public override string Type => QueryTypes.FindInterfaceImplementation;

    public override IReadOnlyDictionary<string, string> GetDisplayFields() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = Name
        };
}

internal sealed record FindDerivedTypesQuery(string Name) : QueryRequest
{
    public override string Type => QueryTypes.FindDerivedTypes;

    public override IReadOnlyDictionary<string, string> GetDisplayFields() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = Name
        };
}
