---
name: spanner-graph-search
description: >-
  provides instructions for searching and querying a Google Cloud Spanner Graph
  database using self-contained Python CLI tools. The graph contains project
  knowledge such as architecture documents, code analysis reports, Jira tickets,
  Confluence pages, and code entities.
---

# Spanner Graph Search Skill

This skill provides **deterministic Python CLI tools** for searching and querying a Google Cloud Spanner Graph database. The tools handle sophisticated logic automatically — auto-discovery of searchable tables, automatic content fetching, edge-label mapping, and query chunking — so the agent only needs to call the right command.

## Prerequisites

- **Python 3.10+** available on the system PATH
- **`uv`** installed (recommended — handles dependencies automatically) **OR** `google-cloud-spanner` installed in your Python environment
- Google Cloud authentication configured (`gcloud auth application-default login`)
- Access to the Spanner instance containing the project's knowledge graph

## Environment Configuration

Connection parameters must be configured in [`scripts/env.config`](scripts/env.config.example). Copy `env.config.example` to `env.config` and edit the values. The scripts read them automatically — **no need to pass connection flags** unless overriding defaults.

| Variable                       | Description                | Default            |
| ------------------------------ | -------------------------- | ------------------ |
| `SPANNER_PROJECT_ID`           | GCP Project ID             | from env.config    |
| `SPANNER_INSTANCE_ID`          | Spanner Instance ID        | from env.config    |
| `SPANNER_DATABASE_ID`          | Spanner Database ID        | from env.config    |
| `SPANNER_GRAPH_NAME`           | Name of the property graph | from env.config    |
| `SPANNER_EMBEDDING_MODEL_NAME` | Embedding model name       | GeminiEmbedding001 |
| `SPANNER_SEARCH_TOP_K`         | Default result limit       | 10                 |
| `SPANNER_LLM_MODEL_NAME`       | LLM model for nl2gql       | gemini-2.5-pro     |
| `SPANNER_VERTEXAI_LOCATION`    | Vertex AI region           | europe-west4       |

## How to Run Tools

All tools are subcommands of the [`scripts/spanner_tools.py`](scripts/spanner_tools.py) script. Execute them using **`uv run --script`** (recommended — automatically manages dependencies):

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py <command> [options]
```

> **Note:** All commands output **JSON to stdout**. Diagnostic/progress messages go to stderr.
> Connection parameters are loaded from `env.config` automatically — you do NOT need to pass `--project`, `--instance`, `--database`, or `--graph` unless overriding.

> **Alternative (if `uv` is not available):** Use `python .agents/skills/spanner-graph-search/scripts/spanner_tools.py <command> [options]` with `google-cloud-spanner` installed in your Python environment.

---

## Available Tools

### 1. Schema Discovery (`schema`)

Retrieve graph metadata — node types, edge types, labels, and properties. **Always start here** when you don't know the graph structure.

**Full raw metadata:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema
```

**Summarised view** (recommended — shows tables, labels, properties, and which tables are searchable):

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema --summary
```

**Output format (summary):**

```json
{
  "graph_name": "SiemensGraph",
  "node_tables": [
    {
      "name": "Page",
      "labels": ["Page"],
      "properties": ["NodeId", "Title", "Content", "SearchTokens", "Embedding"],
      "has_search_tokens": true
    }
  ],
  "edge_tables": [
    {
      "name": "TextChunkPartOfPage",
      "labels": ["TEXT_CHUNK_PART_OF_PAGE"],
      "source_node": "TextChunk",
      "target_node": "Page",
      "properties": []
    }
  ]
}
```

### 2. Keyword Search (`keyword-search`)

Full-text search using RQuery syntax. This tool **automatically**:

- Discovers which node tables have `SearchTokens` (if `--node-tables` not specified)
- Chunks queries across tables to avoid Spanner limits
- Fetches full content for matched nodes (unless `--no-content`)

**Basic search (searches ALL searchable tables, returns content):**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py keyword-search "telegram configuration"
```

**Multiple queries at once:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py keyword-search "telegram configuration" "I/O mapping format"
```

**Search specific tables only:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py keyword-search "telegram configuration" --node-tables Page TextChunk
```

**Search without fetching content (faster, IDs only):**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py keyword-search "telegram configuration" --no-content
```

**Control result count:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py keyword-search "telegram configuration" --limit 20
```

**RQuery Syntax Rules:**

- Multiple terms imply AND: `big time` → big AND time
- `OR` operator: `big OR time` — OR only applies to the two adjacent terms
- Pipe `|` is shortcut for OR
- Double quotes for phrase search: `"fast car"` matches exact phrase
- `AROUND` operator for proximity: `fast AROUND(10) car`
- Dash `-` for negation: `-dog` excludes documents containing "dog"
- Punctuation is ignored; search is case-insensitive

**Output format:**

```json
[
  {
    "query": "telegram configuration",
    "tables_searched": ["Page", "TextChunk", "Class"],
    "match_count": 5,
    "matches": [
      {
        "node_type": "Page",
        "node_id": "abc123",
        "score": 0.95,
        "content": "{\"Title\": \"Telegram Config\", \"Content\": \"...\"}"
      }
    ]
  }
]
```

### 3. Fetch Node Content (`fetch-content`)

Retrieve full content/properties of specific nodes by their type and ID. Automatically excludes binary columns (`Embedding`, `SearchTokens`).

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py fetch-content --node-types Page TextChunk --node-ids abc123 def456
```

> **Note:** `--node-types` and `--node-ids` must be paired 1:1 (same count, same order).

**Output format:**

```json
[
  {
    "node_type": "Page",
    "node_id": "abc123",
    "content": "{\"Title\": \"My Page\", \"Content\": \"...\"}"
  }
]
```

### 4. Fetch Neighbours (`fetch-neighbours`)

Graph traversal to find connected nodes. This tool **automatically**:

- Maps edge table names to edge labels (accepts both)
- Validates edge types against the schema
- Supports multi-hop traversal

**Basic 1-hop traversal:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py fetch-neighbours --start-type Page --start-id abc123 --edge-types TEXT_CHUNK_PART_OF_PAGE
```

**Multi-hop with target filtering:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py fetch-neighbours --start-type Page --start-id abc123 --edge-types TEXT_CHUNK_PART_OF_PAGE CLASS_IS_RELEVANT_TO_TEXT_CHUNK --target-types Class --hops 2
```

**With content fetching for found neighbours:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py fetch-neighbours --start-type Page --start-id abc123 --edge-types TEXT_CHUNK_PART_OF_PAGE --fetch-content
```

**Edge type resolution:** You can pass either the edge **table name** (e.g., `TextChunkPartOfPage`) or the edge **label** (e.g., `TEXT_CHUNK_PART_OF_PAGE`). The tool auto-resolves both.

**Best practices:**

- **Always specify edge types** — omitting them returns an error with available options
- **Use `--target-types`** when you know what node type you're looking for
- **Start with `--hops 1`** and increase only if needed
- **Run `schema --summary`** first to understand which edges connect which nodes

**Output format:**

```json
{
  "start_node": { "type": "Page", "id": "abc123" },
  "traversal": {
    "edge_types_requested": ["TEXT_CHUNK_PART_OF_PAGE"],
    "edge_labels_resolved": ["TEXT_CHUNK_PART_OF_PAGE"],
    "target_types": null,
    "max_hops": 1
  },
  "neighbour_count": 12,
  "neighbours": [{ "node_type": "TextChunk", "node_id": "xyz789" }]
}
```

### 5. Semantic Similarity Search (`semantic-search`)

Embedding-based conceptual search using Spanner's `ML.PREDICT` and `COSINE_DISTANCE`. Auto-discovers tables with `Embedding` columns.

**Basic semantic search:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py semantic-search "how does the telegram format handle I/O mapping"
```

**Search specific tables:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py semantic-search "I/O mapping architecture" --node-tables Page TextChunk
```

**Multiple queries:**

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py semantic-search "telegram format" "axis configuration" --limit 5
```

**Output format:**

```json
[
  {
    "query": "telegram format",
    "table": "Page",
    "match_count": 5,
    "matches": [
      { "NodeId": "abc123", "Title": "Telegram Format Spec", "Content": "..." }
    ]
  }
]
```

### 6. Raw Query (`raw-query`)

Execute arbitrary SQL or GQL queries directly. Use for custom queries not covered by the other tools.

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py raw-query "GRAPH SiemensGraph MATCH (n:Page) WHERE n.Title LIKE '%%telegram%%' RETURN n.NodeId AS id, n.Title AS title LIMIT 10;"
```

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py raw-query "GRAPH SiemensGraph MATCH (n:Page) RETURN COUNT(*) AS page_count;"
```

**Output format:**

```json
{
  "row_count": 3,
  "rows": [{ "id": "abc123", "title": "Telegram Configuration Guide" }]
}
```

### 7. Natural Language to GQL (`nl2gql`)

Convert a natural language question into a GQL query, execute it, and return the results. This tool automatically fetches the graph schema, sends it to an LLM alongside your question, and executes the generated GQL query against the database.

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py nl2gql "What modules contain files that define classes with constructors?"
```

**Output format:**

```json
{
  "generated_gql": "GRAPH SiemensGraph\nMATCH (m:Module)-[c:CONTAINS]->(f:File)-[d:DEFINES]->(cl:Class)-[h:HAS_METHOD]->(me:Method)\nWHERE me.Name = 'constructor'\nRETURN m.Name AS module_name, f.Name AS file_name, cl.Name AS class_name;",
  "results": [
    { "module_name": "core", "file_name": "engine.py", "class_name": "Engine" }
  ]
}
```

---

## Search Strategy

1. **Start with schema discovery:** If unsure which tables to query, first run `schema --summary` to understand the node types, edge types, and their properties.

2. **Use multiple search approaches** for comprehensive results:
   - Run **keyword search** with relevant terms
   - Run **semantic search** for conceptual matches
   - Use **fetch neighbours** to explore relationships around discovered nodes
   - Use **fetch content** to get full details of interesting nodes
   - Cross-reference and validate results across all approaches

3. **Be autonomous:** Don't ask the user which tables to query — use `schema --summary` and the auto-discovery features.

4. **Present findings with references:** Always provide a search report with references to the source nodes/documents found.

## Example Workflow

```
1. User asks: "How does the telegram configuration work?"
2. You:
   a. Run `schema --summary` to understand available node/edge types
   b. Run `keyword-search "telegram configuration"` (auto-discovers tables, fetches content)
   c. Run `nl2gql "What nodes mention telegram configuration and how are they connected?"` for an LLM-assisted graph query
   d. Run `semantic-search "how does telegram configuration work"` for conceptual matches
   e. Run `fetch-neighbours` on top result nodes to discover related entities
   f. Cross-reference results from all approaches
   g. Present consolidated findings with source references
```

## Error Handling

- All tools return JSON — errors appear as `{"error": "..."}` in the output
- If authentication fails, instruct the user to run `gcloud auth application-default login`
- If a keyword search returns no results, try alternative query terms or broader search
- If neighbour traversal fails with unknown edge types, the error lists available options
- Always provide partial results even if some queries fail
- If a query times out, try reducing `--limit` or adding `--node-tables` to narrow the search

## Overriding Connection Parameters

All commands accept optional flags to override `env.config`:

```
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema --project my-project --instance my-instance --database my-db --graph MyGraph
```
