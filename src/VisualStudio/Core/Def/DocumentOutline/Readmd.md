# Document Outline


## Design

## Implementation Notes

### Queues

We have two queues that we use in document outline: `_documentSymbolQueue` and `_updateViewModelStateQueue`.

Each time the text document is updated the following flow happens:

```mermaid
sequenceDiagram
    participant LSP Server
    participant documentSymbolQueue
    participant updateViewModelStateQueue
    participant User Interface
    documentSymbolQueue->>LSP Server: Document symbols requested
    LSP Server->>documentSymbolQueue: Document symbols returned
    documentSymbolQueue->>updateViewModelStateQueue: Queue updating state
    updateViewModelStateQueue->>updateViewModelStateQueue: Delay by 250ms
    updateViewModelStateQueue->>documentSymbolQueue: Get latest model and set properties
    updateViewModelStateQueue->>User Interface: Send property changed notification
```

## Tests