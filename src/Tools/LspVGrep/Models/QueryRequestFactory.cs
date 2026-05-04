namespace LspVGrepTool.Models;

internal static class QueryRequestFactory
{
    public static IReadOnlyList<QueryRequest> Create(InputDocument input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Directory))
        {
            throw new InvalidDataException("The input JSON must include a non-empty 'directory' value.");
        }

        if (input.Queries is null || input.Queries.Count == 0)
        {
            throw new InvalidDataException("The input JSON must include at least one query.");
        }

        var requests = new List<QueryRequest>(input.Queries.Count);
        for (var index = 0; index < input.Queries.Count; index++)
        {
            var definition = input.Queries[index];
            var request = Create(definition, index);
            requests.Add(request);
        }

        return requests;
    }

    private static QueryRequest Create(QueryDefinitionDto definition, int index)
    {
        if (string.IsNullOrWhiteSpace(definition.Type))
        {
            throw new InvalidDataException($"Query #{index + 1} is missing a non-empty 'type' field.");
        }

        return definition.Type switch
        {
            QueryTypes.FindTypeDefinition => CreateFindTypeDefinitionQuery(definition, index),
            QueryTypes.FindInterfaceImplementation => CreateNameOnlyQuery<FindInterfaceImplementationQuery>(definition, index),
            QueryTypes.FindDerivedTypes => CreateNameOnlyQuery<FindDerivedTypesQuery>(definition, index),
            QueryTypes.FindMemberDefinition => CreateNameOnlyQuery<FindMemberDefinitionQuery>(definition, index),
            _ => throw new InvalidDataException(
                $"Query #{index + 1} has unsupported type '{definition.Type}'.")
        };
    }

    private static QueryRequest CreateFindTypeDefinitionQuery(QueryDefinitionDto definition, int index)
    {
        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidDataException(
                $"Query #{index + 1} with type '{QueryTypes.FindTypeDefinition}' requires a non-empty 'name' field.");
        }

        return new FindTypeDefinitionQuery(definition.Name);
    }

    private static TQuery CreateNameOnlyQuery<TQuery>(QueryDefinitionDto definition, int index)
        where TQuery : QueryRequest
    {
        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidDataException(
                $"Query #{index + 1} with type '{definition.Type}' requires a non-empty 'name' field.");
        }

        return (TQuery)Activator.CreateInstance(typeof(TQuery), definition.Name)!;
    }
}
