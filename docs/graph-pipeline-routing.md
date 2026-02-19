# Graph Pipeline Routing

This document describes how `Flowxel.Graph` and `Flowxel.Imaging` execute a DAG with deterministic, port-addressed data flow.

## Core model

- `Graph<TNode>` stores executable nodes and directed edges.
- A graph edge is:
  - `FromNodeId`
  - `FromPortKey`
  - `ToNodeId`
  - `ToPortKey`
- `ResourcePool` stores outputs by `(nodeId, portKey)`.

Default port names are in `Flowxel.Graph/GraphPorts.cs`:

- input: `in`
- output: `out`

## Deterministic execution guarantees

The scheduler (`Graph.ExecuteAsync`) is dependency deterministic:

1. Compute in-degree (unique predecessor node count).
2. Execute all currently ready nodes in parallel.
3. Decrement successor in-degree after each predecessor completes.
4. Continue until all nodes complete.

This guarantees:

- no node runs before all predecessor nodes are complete.
- no cycle can execute.

## Deterministic routing guarantees

Routing is now explicit by ports:

- `graph.Connect(from, to, fromPortKey, toPortKey)` creates a typed data route.
- `Node<TIn, TOut>` resolves inputs by its declared `InputPorts` list.
- each declared input port must have exactly one incoming edge.
- output is published on `OutputPort` (default: `out`).

This removes ambiguity from predecessor enumeration order for multi-input operations.

## Node input/output contract

`Flowxel.Imaging.Operations.Node<TIn,TOut>` behavior:

- `InputPorts`:
  - default is `["in"]` for regular nodes.
  - default is `[]` for `Node<Empty, TOut>`.
- `OutputPort` default is `"out"`.
- execution:
  1. resolve one value per declared input port from `ResourcePool`.
  2. call `ExecuteInternal`.
  3. publish output to `(Id, OutputPort)`.

For multi-input operations, override `InputPorts`.

Examples:

- `SubtractOperation` uses `["left", "right"]`.
- `ConstructLineLineIntersectionOperation` uses `["first", "second"]`.
- `ConstructLineLineBisectorOperation` uses `["first", "second"]`.

## API usage

Single input (legacy/default ports):

```csharp
graph.Connect(load, blur); // out -> in
```

Explicit routing:

```csharp
graph.Connect(blurSmall, subtract, "out", "left");
graph.Connect(blurLarge, subtract, "out", "right");
```

Resource access:

```csharp
var mat = pool.Get<Mat>(node.Id);           // default output port
var left = pool.Get<Mat>(node.Id, "out");   // explicit port
```

## Why this is important

This aligns graph internals with process-port semantics already present in the UI model:

- edges carry port identity.
- nodes receive the right value on the right input.
- multi-input operation behavior is stable and predictable.
