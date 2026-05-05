#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = [
#     "google-cloud-spanner>=3.0.0",
#     "google-genai>=1.0.0",
#     "langchain-core>=0.2.0",
#     "langchain-google-vertexai>=1.0.0",
#     "langchain-google-spanner>=1.0.0",
# ]
# ///
"""
Self-contained Spanner Graph query tools for the spanner-graph-search skill.

Provides deterministic CLI subcommands that replicate the sophisticated query
patterns from the ADK-based SpannerQueryTools — but using only the
google-cloud-spanner SDK (no ADK dependency).

Usage (with uv — recommended, no venv needed):
    uv run --script spanner_tools.py <command> [options]

Usage (with globally installed Python):
    pip install google-cloud-spanner
    python spanner_tools.py <command> [options]

Commands:
    schema             Get graph metadata (node types, edge types, properties)
    keyword-search     Full-text keyword search with auto-discovery & content fetch
    fetch-neighbours   Graph traversal with automatic edge-label mapping
    fetch-content      Fetch full content of specific nodes by ID
    semantic-search    Embedding-based similarity search
    raw-query          Execute arbitrary GQL / SQL query
"""

import argparse
import json
import logging
import os
import sys
from pathlib import Path
from typing import Any

# Disable Spanner's built-in metrics exporter to avoid noisy gRPC errors
# when the client tries to export OpenTelemetry metrics to Cloud Monitoring.
# These MUST be set before importing google.cloud.spanner.
os.environ["GOOGLE_CLOUD_SPANNER_ENABLE_BUILTIN_METRICS"] = "false"
os.environ["OTEL_SDK_DISABLED"] = "true"
os.environ.setdefault("OTEL_METRICS_EXPORTER", "none")
os.environ.setdefault("OTEL_TRACES_EXPORTER", "none")

from google.cloud import spanner  # noqa: E402
from google.cloud.spanner_v1 import param_types  # noqa: E402

logging.basicConfig(
    level=logging.INFO,
    format="%(levelname)s: %(message)s",
    stream=sys.stderr,
)
logger = logging.getLogger(__name__)

# Silence noisy loggers from the Spanner metrics / OTel export background threads
logging.getLogger("opentelemetry.sdk.metrics").setLevel(logging.CRITICAL)
logging.getLogger("google.cloud.spanner_v1.metrics").setLevel(logging.CRITICAL)
# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------


def load_env_config() -> dict[str, str]:
    """Load configuration from env.config in the same directory as this script."""
    config_path = Path(__file__).parent / "env.config"
    config: dict[str, str] = {}
    if config_path.exists():
        for line in config_path.read_text().splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            key, _, value = line.partition("=")
            config[key.strip()] = value.strip()
    return config


ENV = load_env_config()

DEFAULT_PROJECT = ENV.get("SPANNER_PROJECT_ID", "")
DEFAULT_INSTANCE = ENV.get("SPANNER_INSTANCE_ID", "")
DEFAULT_DATABASE = ENV.get("SPANNER_DATABASE_ID", "")
DEFAULT_GRAPH = ENV.get("SPANNER_GRAPH_NAME", "")
DEFAULT_EMBEDDING_MODEL = ENV.get(
    "SPANNER_EMBEDDING_MODEL_NAME", "gemini-embedding-001")
DEFAULT_LLM_MODEL = ENV.get("SPANNER_LLM_MODEL_NAME", "gemini-2.5-pro")
DEFAULT_TOP_K = int(ENV.get("SPANNER_SEARCH_TOP_K", "10"))

TOKEN_COL = "SearchTokens"
EMBEDDING_COL = "Embedding"
NODE_ID_COL = "NodeId"

# Columns that should never be returned in content (binary / large)
EXCLUDED_COLS = {TOKEN_COL, EMBEDDING_COL, NODE_ID_COL}


# ---------------------------------------------------------------------------
# Spanner helpers
# ---------------------------------------------------------------------------

def get_database(
    project_id: str, instance_id: str, database_id: str
) -> spanner.Client:
    client = spanner.Client(project=project_id)
    instance = client.instance(instance_id)
    return instance.database(database_id)


def get_graph_metadata(
    project_id: str, instance_id: str, database_id: str, graph_name: str
) -> dict[str, Any]:
    """Retrieve the full graph schema JSON from INFORMATION_SCHEMA."""
    db = get_database(project_id, instance_id, database_id)
    query = (
        "SELECT PROPERTY_GRAPH_METADATA_JSON "
        "FROM INFORMATION_SCHEMA.PROPERTY_GRAPHS "
        "WHERE PROPERTY_GRAPH_NAME = @graph_name;"
    )
    with db.snapshot() as snapshot:
        result = snapshot.execute_sql(
            query,
            params={"graph_name": graph_name},
            param_types={"graph_name": param_types.STRING},
            request_options={
                "request_tag": "app=skill:spanner-graph-search,action=get_graph_metadata",
            },
        ).one_or_none()
        if result is None:
            raise RuntimeError(f"No graph found with name: '{graph_name}'")
        return next(iter(result))


# ---------------------------------------------------------------------------
# Derived schema helpers
# ---------------------------------------------------------------------------

def get_searchable_node_tables(metadata: dict) -> list[str]:
    """Return base table names of node tables that have a SearchTokens column."""
    return [
        t["baseTableName"]
        for t in metadata["nodeTables"]
        if any(
            pd["propertyDeclarationName"] == TOKEN_COL
            for pd in t["propertyDefinitions"]
        )
    ]


def get_content_columns(metadata: dict) -> dict[str, str]:
    """
    Return {baseTableName: "n.Col1, n.Col2, ..."} for content fetching,
    excluding binary / large columns.
    """
    return {
        t["baseTableName"]: ", ".join(
            f"n.{pd['valueExpressionSql']}"
            for pd in t["propertyDefinitions"]
            if pd["valueExpressionSql"] not in EXCLUDED_COLS
        )
        for t in metadata["nodeTables"]
    }


def get_edge_table_to_label_map(metadata: dict) -> dict[str, str]:
    """Return {edgeBaseTableName: edgeLabelName} mapping."""
    return {
        t["baseTableName"]: t["labelNames"][0]
        for t in metadata["edgeTables"]
        if t.get("labelNames")
    }


# ---------------------------------------------------------------------------
# Command: schema
# ---------------------------------------------------------------------------

def cmd_schema(args: argparse.Namespace) -> None:
    """Print graph metadata as JSON."""
    metadata = get_graph_metadata(
        args.project, args.instance, args.database, args.graph
    )

    if args.summary:
        summary = {
            "graph_name": metadata.get("name", args.graph),
            "node_tables": [
                {
                    "name": t["baseTableName"],
                    "labels": t["labelNames"],
                    "properties": [
                        pd["propertyDeclarationName"]
                        for pd in t["propertyDefinitions"]
                    ],
                    "has_search_tokens": any(
                        pd["propertyDeclarationName"] == TOKEN_COL
                        for pd in t["propertyDefinitions"]
                    ),
                }
                for t in metadata["nodeTables"]
            ],
            "edge_tables": [
                {
                    "name": t["baseTableName"],
                    "labels": t["labelNames"],
                    "source_node": t["sourceNodeTable"]["nodeTableName"],
                    "target_node": t["destinationNodeTable"]["nodeTableName"],
                    "properties": [
                        pd["propertyDeclarationName"]
                        for pd in t["propertyDefinitions"]
                    ],
                }
                for t in metadata["edgeTables"]
            ],
        }
        print(json.dumps(summary, indent=2))
    else:
        print(json.dumps(metadata, indent=2))


# ---------------------------------------------------------------------------
# Command: keyword-search
# ---------------------------------------------------------------------------

def cmd_keyword_search(args: argparse.Namespace) -> None:
    """
    Full-text keyword search with:
    - Auto-discovery of searchable node tables
    - Automatic content fetching for matched nodes
    - Chunked queries to avoid Spanner limits
    """
    metadata = get_graph_metadata(
        args.project, args.instance, args.database, args.graph
    )
    searchable = get_searchable_node_tables(metadata)

    if args.node_tables:
        tables = [t for t in args.node_tables if t in searchable]
        if not tables:
            print(
                json.dumps(
                    {
                        "error": "None of the specified tables are searchable",
                        "searchable_tables": searchable,
                    }
                )
            )
            return
    else:
        tables = searchable
        logger.info("Auto-discovered searchable tables: %s", tables)

    db = get_database(args.project, args.instance, args.database)

    # Chunk tables to avoid Spanner operation limits (max ~50 UNION ALLs)
    chunk_size = 50
    table_chunks = [tables[i: i + chunk_size]
                    for i in range(0, len(tables), chunk_size)]

    all_results: list[dict] = []

    for chunk in table_chunks:
        gql_query = (
            f"GRAPH {args.graph}\n"
            + "\n\nUNION ALL\n\n".join(
                f"MATCH (n:{table_name})\n"
                f"WHERE n.{TOKEN_COL} IS NOT NULL\n"
                f"AND SEARCH(n.{TOKEN_COL}, @query)\n"
                f"RETURN\n"
                f"  LABELS(n) AS node_type,\n"
                f"  n.{NODE_ID_COL} AS node_id,\n"
                f"  SCORE(n.{TOKEN_COL}, @query) AS node_score\n"
                for table_name in chunk
            )
            + f"\nORDER BY node_score DESC\nLIMIT {args.limit};"
        )

        logger.info(
            "Executing keyword search query across %d tables", len(chunk))

        for query_text in args.queries:
            try:
                with db.snapshot() as snapshot:
                    result = snapshot.execute_sql(
                        sql=gql_query, params={
                            "query": query_text}, param_types={
                            "query": param_types.STRING}, request_options={
                            "request_tag": "app=skill:spanner-graph-search,action=keyword_search", }, )
                    matches = []
                    for row in result:
                        cols = [f.name for f in result.fields]
                        row_dict = dict(zip(cols, row))
                        matches.append(
                            {
                                "node_type": row_dict["node_type"][0],
                                "node_id": row_dict["node_id"],
                                "score": row_dict["node_score"],
                            }
                        )

                    # Auto-fetch content for matched nodes
                    if args.fetch_content and matches:
                        matches = fetch_node_contents(
                            args.project,
                            args.instance,
                            args.database,
                            args.graph,
                            matches,
                            metadata,
                        )

                    all_results.append(
                        {
                            "query": query_text,
                            "tables_searched": chunk,
                            "match_count": len(matches),
                            "matches": matches,
                        }
                    )
            except Exception as e:
                logger.error(
                    "Keyword search failed for '%s': %s", query_text, e)
                all_results.append(
                    {"query": query_text, "error": str(e)}
                )

    print(json.dumps(all_results, indent=2, default=str))


# ---------------------------------------------------------------------------
# Command: fetch-content
# ---------------------------------------------------------------------------

def fetch_node_contents(
    project_id: str,
    instance_id: str,
    database_id: str,
    graph_name: str,
    nodes: list[dict],
    metadata: dict | None = None,
) -> list[dict]:
    """
    Fetch full content for a list of nodes.
    Each node dict must have 'node_type' and 'node_id'.
    Returns nodes enriched with 'content' field.
    """
    if not nodes:
        return []

    if metadata is None:
        metadata = get_graph_metadata(
            project_id, instance_id, database_id, graph_name)

    cols_map = get_content_columns(metadata)
    db = get_database(project_id, instance_id, database_id)

    # Chunk to avoid join limits (max ~30 UNION ALLs for content)
    chunk_size = 30
    node_chunks = [nodes[i: i + chunk_size]
                   for i in range(0, len(nodes), chunk_size)]

    enriched: list[dict] = []

    for chunk in node_chunks:
        match_clauses = " UNION ALL ".join(
            f"\nMATCH (n:{n['node_type']}) WHERE n.{NODE_ID_COL} = '{n['node_id']}'"
            f"\nRETURN LABELS(n) AS node_type, n.{NODE_ID_COL} AS node_id, "
            f"TO_JSON_STRING(SAFE_TO_JSON(STRUCT({cols_map.get(n['node_type'], 'n.*')}))) AS node_content"
            for n in chunk
            if n["node_type"] in cols_map
        )

        if not match_clauses:
            continue

        try:
            with db.snapshot() as snapshot:
                result = snapshot.execute_sql(
                    f"GRAPH {graph_name}\n{match_clauses}", request_options={
                        "request_tag": "app=skill:spanner-graph-search,action=fetch_content", }, )
                content_map: dict[str, dict] = {}
                for row in result:
                    cols = [f.name for f in result.fields]
                    row_dict = dict(zip(cols, row))
                    content_map[row_dict["node_id"]] = {
                        "node_type": row_dict["node_type"][0],
                        "node_id": row_dict["node_id"],
                        "content": row_dict["node_content"],
                    }

                for n in chunk:
                    if n["node_id"] in content_map:
                        merged = {**n, **content_map[n["node_id"]]}
                        enriched.append(merged)
                    else:
                        enriched.append(n)
        except Exception as e:
            logger.error("Content fetch failed: %s", e)
            enriched.extend(chunk)

    return enriched


def cmd_fetch_content(args: argparse.Namespace) -> None:
    """Fetch content for specific nodes."""
    nodes = [
        {"node_type": nt, "node_id": nid}
        for nt, nid in zip(args.node_types, args.node_ids)
    ]
    result = fetch_node_contents(
        args.project, args.instance, args.database, args.graph, nodes
    )
    print(json.dumps(result, indent=2, default=str))


# ---------------------------------------------------------------------------
# Command: fetch-neighbours
# ---------------------------------------------------------------------------

def cmd_fetch_neighbours(args: argparse.Namespace) -> None:
    """
    Graph traversal with:
    - Automatic edge table name → label mapping
    - Support for both edge table names and edge label names
    - Multi-hop traversal
    """
    metadata = get_graph_metadata(
        args.project, args.instance, args.database, args.graph
    )
    edge_map = get_edge_table_to_label_map(metadata)
    all_labels = set(edge_map.values())

    if not args.edge_types:
        print(
            json.dumps(
                {
                    "error": "Edge types are required for efficient traversal",
                    "available_edge_tables": list(edge_map.keys()),
                    "available_edge_labels": sorted(all_labels),
                }
            )
        )
        return

    # Resolve edge types: accept both table names and label names
    resolved_labels = []
    for et in args.edge_types:
        if et in edge_map:
            resolved_labels.append(edge_map[et])
        elif et in all_labels:
            resolved_labels.append(et)
        else:
            logger.warning(
                "Unknown edge type '%s' — skipping. Available: %s",
                et,
                sorted(list(edge_map.keys()) + list(all_labels)),
            )

    if not resolved_labels:
        print(json.dumps(
            {"error": "No valid edge types resolved", "input": args.edge_types}))
        return

    edges_gql = ":" + " | ".join(resolved_labels)
    target_gql = ""
    if args.target_types:
        target_gql = ":" + " | ".join(args.target_types)

    query = (
        f"GRAPH {args.graph}\n"
        f"MATCH (A:{args.start_type} {{{NODE_ID_COL}: '{args.start_id}'}})"
        f"-[E{edges_gql}]-{{{1}, {args.hops}}}(B{target_gql})\n"
        f"RETURN LABELS(B) AS node_type, B.{NODE_ID_COL} AS node_id\n"
        f"GROUP BY B;"
    )

    logger.info("Neighbour query:\n%s", query)

    db = get_database(args.project, args.instance, args.database)

    try:
        with db.snapshot() as snapshot:
            result = snapshot.execute_sql(
                sql=query, request_options={
                    "request_tag": "app=skill:spanner-graph-search,action=fetch_neighbours", }, )
            neighbours = []
            for row in result:
                cols = [f.name for f in result.fields]
                row_dict = dict(zip(cols, row))
                neighbours.append(
                    {
                        "node_type": row_dict["node_type"][0],
                        "node_id": row_dict["node_id"],
                    }
                )

            # Optionally fetch content
            if args.fetch_content and neighbours:
                neighbours = fetch_node_contents(
                    args.project,
                    args.instance,
                    args.database,
                    args.graph,
                    neighbours,
                    metadata,
                )

            output = {
                "start_node": {
                    "type": args.start_type,
                    "id": args.start_id,
                },
                "traversal": {
                    "edge_types_requested": args.edge_types,
                    "edge_labels_resolved": resolved_labels,
                    "target_types": args.target_types,
                    "max_hops": args.hops,
                },
                "neighbour_count": len(neighbours),
                "neighbours": neighbours,
            }
            print(json.dumps(output, indent=2, default=str))

    except Exception as e:
        logger.error("Neighbour traversal failed: %s", e)
        print(json.dumps({"error": str(e)}))


# ---------------------------------------------------------------------------
# Embedding helper using Google GenAI SDK
# ---------------------------------------------------------------------------

def generate_embedding(text: str, model_name: str, project_id: str) -> list[float]:
    """Generate an embedding vector using the Google GenAI SDK."""
    from google import genai

    client = genai.Client(
        vertexai=True,
        project=project_id,
        location="europe-west4",  # Adjust as needed for your embedding model's location
    )
    result = client.models.embed_content(
        model=model_name,
        contents=[text],
    )
    return list(result.embeddings[0].values)


# ---------------------------------------------------------------------------
# Command: semantic-search
# ---------------------------------------------------------------------------

def cmd_semantic_search(args: argparse.Namespace) -> None:
    """Embedding-based semantic similarity search using external embedding generation."""
    metadata = get_graph_metadata(
        args.project, args.instance, args.database, args.graph
    )

    # Find tables with Embedding column
    embeddable = [
        t["baseTableName"]
        for t in metadata["nodeTables"]
        if any(
            pd["propertyDeclarationName"] == EMBEDDING_COL
            for pd in t["propertyDefinitions"]
        )
    ]

    if args.node_tables:
        tables = [t for t in args.node_tables if t in embeddable]
    else:
        tables = embeddable

    if not tables:
        print(json.dumps(
            {"error": "No embeddable tables found", "available": embeddable}))
        return

    db = get_database(args.project, args.instance, args.database)

    all_results: list[dict] = []

    # Pre-compute embeddings for all unique queries
    unique_queries = list(set(args.queries))
    query_embeddings: dict[str, list[float]] = {}
    for q in unique_queries:
        try:
            logger.info("Generating embedding for query: '%s'", q)
            query_embeddings[q] = generate_embedding(
                q, args.embedding_model, args.project)
        except Exception as e:
            logger.error("Failed to generate embedding for '%s': %s", q, e)
            for table in tables:
                all_results.append(
                    {"query": q, "table": table,
                     "error": f"Embedding generation failed: {e}"})

    for table in tables:
        # Build column list for content (exclude binary cols)
        content_cols = ", ".join(
            pd["valueExpressionSql"]
            for t in metadata["nodeTables"]
            if t["baseTableName"] == table
            for pd in t["propertyDefinitions"]
            if pd["valueExpressionSql"] not in EXCLUDED_COLS
        )

        for query_text in args.queries:
            if query_text not in query_embeddings:
                continue  # Already reported error above

            embedding_vector = query_embeddings[query_text]
            # Build the embedding array literal for Spanner SQL
            # Use FLOAT32 to match the stored embedding type
            embedding_literal = "[" + ", ".join(
                f"CAST({v} AS FLOAT32)" for v in embedding_vector) + "]"

            sql = (
                f"SELECT {NODE_ID_COL}, {content_cols} "
                f"FROM {table} "
                f"WHERE {EMBEDDING_COL} IS NOT NULL "
                f"ORDER BY COSINE_DISTANCE({EMBEDDING_COL}, "
                f"ARRAY<FLOAT32>{embedding_literal}) "
                f"LIMIT {args.limit};"
            )

            logger.info("Semantic search on %s for '%s'", table, query_text)

            try:
                with db.snapshot() as snapshot:
                    result = snapshot.execute_sql(
                        sql=sql, request_options={
                            "request_tag": "app=skill:spanner-graph-search,action=semantic_search", }, )
                    matches = []
                    for row in result:
                        cols = [f.name for f in result.fields]
                        row_dict = dict(zip(cols, row))
                        matches.append(row_dict)

                    all_results.append(
                        {
                            "query": query_text,
                            "table": table,
                            "match_count": len(matches),
                            "matches": matches,
                        }
                    )
            except Exception as e:
                logger.error("Semantic search failed on %s: %s", table, e)
                all_results.append(
                    {"query": query_text, "table": table, "error": str(e)})

    print(json.dumps(all_results, indent=2, default=str))


# ---------------------------------------------------------------------------
# Command: raw-query
# ---------------------------------------------------------------------------

def cmd_raw_query(args: argparse.Namespace) -> None:
    """Execute an arbitrary SQL or GQL query."""
    db = get_database(args.project, args.instance, args.database)

    try:
        with db.snapshot() as snapshot:
            result = snapshot.execute_sql(
                sql=args.query,
                request_options={
                    "request_tag": "app=skill:spanner-graph-search,action=raw_query",
                },
            )
            rows = []
            for row in result:
                cols = [f.name for f in result.fields]
                rows.append(dict(zip(cols, row)))

            print(json.dumps({"row_count": len(rows),
                  "rows": rows}, indent=2, default=str))
    except Exception as e:
        logger.error("Raw query failed: %s", e)
        print(json.dumps({"error": str(e)}))


# ---------------------------------------------------------------------------
# Command: nl2gql
# ---------------------------------------------------------------------------

def cmd_nl2gql(args: argparse.Namespace) -> None:
    """Execute a natural language query against the Spanner Graph by translating it to GQL."""
    from langchain_core.output_parsers import StrOutputParser
    from langchain_google_spanner import SpannerGraphStore
    from langchain_google_spanner.graph_qa import (
        GQL_GENERATION_PROMPT,
        GQL_FIX_PROMPT,
        GQL_VERIFY_PROMPT,
        verify_gql_output_parser,
    )
    from langchain_google_spanner.graph_utils import extract_gql, fix_gql_syntax
    from langchain_google_vertexai import ChatVertexAI

    project_id = args.project
    instance_id = args.instance
    database_id = args.database
    graph_name = args.graph
    question = args.query
    model_name = args.model
    max_fix_retries = getattr(args, "max_fix_retries", 3)
    top_k = getattr(args, "limit", DEFAULT_TOP_K)

    try:
        # Create Spanner client with ADC (same auth pattern as other tools)
        client = get_database(project_id, instance_id, database_id)._instance._client

        # Create SpannerGraphStore for schema introspection and query execution
        graph_store = SpannerGraphStore(
            instance_id=instance_id,
            database_id=database_id,
            graph_name=graph_name,
            client=client,
        )

        # Create LLM for GQL generation with Priority PayGo header
        # Must use REST transport because ChatVertexAI defaults to gRPC.
        PRIORITY_HEADERS = {"x-vertex-ai-llm-shared-request-type": "priority"}

        # Use Vertex AI location if available in environment, default to global
        location = ENV.get("SPANNER_VERTEXAI_LOCATION", "global")

        llm = ChatVertexAI(
            model=model_name,
            temperature=0,
            additional_headers=PRIORITY_HEADERS,
            api_transport="rest",
            location=location,
        )

        # Build GQL generation chain (prompt → LLM → parse output)
        gql_generation_chain = GQL_GENERATION_PROMPT | llm | StrOutputParser()

        # Build GQL verification chain (prompt → LLM → parse JSON output)
        gql_verify_chain = GQL_VERIFY_PROMPT | llm | verify_gql_output_parser

        # Build GQL fix chain (prompt → LLM → parse output)
        gql_fix_chain = GQL_FIX_PROMPT | llm | StrOutputParser()

        schema = graph_store.get_schema

        # Step 1: Generate GQL from natural language question
        gen_response = gql_generation_chain.invoke(
            {"question": question, "schema": schema}
        )
        generated_gql = extract_gql(gen_response)
        logger.info("Generated GQL: %s", generated_gql)

        # Step 2: Verify and potentially fix the generated GQL
        verify_response = gql_verify_chain.invoke(
            {
                "question": question,
                "generated_gql": generated_gql,
                "graph_schema": schema,
            }
        )
        if "verified_gql" in verify_response:
            verified_gql = fix_gql_syntax(verify_response["verified_gql"])
            logger.info("Verified GQL: %s", verified_gql)
        else:
            verified_gql = generated_gql

        # Step 3: Execute with retry
        gql_to_execute = verified_gql
        retries = 0
        while retries <= max_fix_retries:
            try:
                results = graph_store.query(gql_to_execute)[:top_k]
                output = {
                    "generated_gql": gql_to_execute,
                    "results": results,
                }
                print(json.dumps(output, indent=2, default=str))
                return
            except Exception as exec_err:
                logger.warning(
                    "GQL execution failed (attempt %d/%d): %s",
                    retries + 1, max_fix_retries + 1, str(exec_err),
                )
                if retries < max_fix_retries:
                    fix_response = gql_fix_chain.invoke(
                        {
                            "question": question,
                            "generated_gql": gql_to_execute,
                            "err_msg": str(exec_err),
                            "schema": schema,
                        }
                    )
                    gql_to_execute = extract_gql(fix_response)
                    logger.info("Fixed GQL: %s", gql_to_execute)
                retries += 1

        print(json.dumps({
            "error": f"Failed to generate valid GQL after {max_fix_retries + 1} attempts",
            "generated_gql": gql_to_execute,
        }, indent=2, default=str))

    except Exception as e:
        logger.error("NL2GQL query failed: %s", str(e), exc_info=True)
        print(json.dumps({"error": str(e)}))


# ---------------------------------------------------------------------------
# CLI argument parser
# ---------------------------------------------------------------------------

def add_common_args(parser: argparse.ArgumentParser) -> None:
    """Add common Spanner connection arguments."""
    parser.add_argument("--project", default=DEFAULT_PROJECT,
                        help="GCP Project ID")
    parser.add_argument("--instance", default=DEFAULT_INSTANCE,
                        help="Spanner Instance ID")
    parser.add_argument("--database", default=DEFAULT_DATABASE,
                        help="Spanner Database ID")
    parser.add_argument("--graph", default=DEFAULT_GRAPH, help="Graph name")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Spanner Graph query tools for the spanner-graph-search skill",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    # --- schema ---
    p_schema = subparsers.add_parser(
        "schema", help="Get graph schema metadata")
    add_common_args(p_schema)
    p_schema.add_argument(
        "--summary",
        action="store_true",
        help="Print a summarised view (tables, labels, properties) instead of raw JSON",
    )

    # --- keyword-search ---
    p_kw = subparsers.add_parser(
        "keyword-search",
        help="Full-text keyword search with auto-discovery and content fetch",
    )
    add_common_args(p_kw)
    p_kw.add_argument(
        "queries",
        nargs="+",
        help="One or more search queries (RQuery syntax supported)")
    p_kw.add_argument(
        "--node-tables",
        nargs="*",
        default=None,
        help="Specific node tables to search (default: auto-discover all searchable)",
    )
    p_kw.add_argument(
        "--no-content",
        dest="fetch_content",
        action="store_false",
        help="Skip automatic content fetching for matched nodes",
    )
    p_kw.add_argument("--limit", type=int, default=DEFAULT_TOP_K,
                      help="Max results per query")

    # --- fetch-content ---
    p_fc = subparsers.add_parser(
        "fetch-content", help="Fetch content for specific nodes")
    add_common_args(p_fc)
    p_fc.add_argument(
        "--node-types",
        nargs="+",
        required=True,
        help="Node type(s) — must match 1:1 with --node-ids",
    )
    p_fc.add_argument(
        "--node-ids",
        nargs="+",
        required=True,
        help="Node ID(s) — must match 1:1 with --node-types",
    )

    # --- fetch-neighbours ---
    p_fn = subparsers.add_parser(
        "fetch-neighbours",
        help="Graph traversal with edge-label auto-mapping",
    )
    add_common_args(p_fn)
    p_fn.add_argument("--start-type", required=True, help="Starting node type")
    p_fn.add_argument("--start-id", required=True, help="Starting node ID")
    p_fn.add_argument(
        "--edge-types",
        nargs="+",
        required=True,
        help="Edge types (table names or labels both accepted)",
    )
    p_fn.add_argument("--target-types", nargs="*",
                      default=None, help="Target node types")
    p_fn.add_argument("--hops", type=int, default=1,
                      help="Max hops (degree of separation)")
    p_fn.add_argument(
        "--fetch-content",
        action="store_true",
        help="Also fetch content for found neighbours",
    )

    # --- semantic-search ---
    p_ss = subparsers.add_parser(
        "semantic-search", help="Embedding-based similarity search")
    add_common_args(p_ss)
    p_ss.add_argument("queries", nargs="+",
                      help="One or more conceptual search queries")
    p_ss.add_argument("--node-tables", nargs="*",
                      default=None, help="Tables to search")
    p_ss.add_argument("--limit", type=int, default=DEFAULT_TOP_K,
                      help="Max results per query")
    p_ss.add_argument(
        "--embedding-model",
        default=DEFAULT_EMBEDDING_MODEL,
        help="Name of the embedding model endpoint",
    )

    # --- raw-query ---
    p_rq = subparsers.add_parser(
        "raw-query", help="Execute arbitrary SQL or GQL")
    add_common_args(p_rq)
    p_rq.add_argument("query", help="The SQL or GQL query to execute")

    # --- nl2gql ---
    p_nl = subparsers.add_parser(
        "nl2gql", help="Execute a natural language query against the Spanner Graph"
    )
    add_common_args(p_nl)
    p_nl.add_argument(
        "query",
        help="The natural language query (e.g. 'How many nodes are there?')")
    p_nl.add_argument(
        "--model",
        default=DEFAULT_LLM_MODEL,
        help="Name of the LLM model to use for translation",
    )
    p_nl.add_argument(
        "--max-fix-retries",
        type=int,
        default=3,
        help="Maximum number of attempts to fix an invalid generated GQL query",
    )
    p_nl.add_argument("--limit", type=int, default=DEFAULT_TOP_K,
                      help="Max results to return")

    return parser


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

COMMAND_MAP = {
    "schema": cmd_schema,
    "keyword-search": cmd_keyword_search,
    "fetch-content": cmd_fetch_content,
    "fetch-neighbours": cmd_fetch_neighbours,
    "semantic-search": cmd_semantic_search,
    "raw-query": cmd_raw_query,
    "nl2gql": cmd_nl2gql,
}


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    handler = COMMAND_MAP.get(args.command)
    if handler:
        handler(args)
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
