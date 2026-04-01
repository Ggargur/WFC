# 🌊 Wave Function Collapse — Unity Package

A Unity implementation of the **Wave Function Collapse (WFC)** algorithm for procedural map generation using tilemaps and prefabs.

## What is it?

This package provides a flexible WFC system that learns adjacency rules directly from a hand-crafted sample tilemap and uses them to generate new, coherent maps procedurally. Instead of manually defining which tiles can be placed next to each other, you just paint an example map in Unity and the tool does the rest.

## How it Works

The system operates in two phases:

**1. Data Generation (`WFCGenerator`)**  
Point the generator at a `Tilemap` containing your sample layout. It scans every tile, records which tiles appear next to which (in all directions), and stores this information as `CellType` and `Socket` assets inside a `WFCDatabase` ScriptableObject. You trigger this via the `Generate WFC Data` context menu entry on the component.

**2. Map Creation (`MapCreator`)**  
With the database populated, `MapCreator` fills a 3D grid of the configured size by running the WFC collapse loop:
- Selects the uncollapsed cell with the lowest entropy (fewest remaining possibilities).
- Randomly collapses it to one of its valid tile options, weighted by each tile's configured weight.
- Propagates the constraint to neighboring cells.
- Backtracks if a contradiction is reached, and retries the affected chunk.

The grid is divided into **chunks**, and each chunk is solved independently with configurable retry attempts, making generation more robust and easier to recover from contradictions.

## Key Concepts

| Concept | Class | Description |
|---|---|---|
| Cell type | `CellType` | Represents a unique tile or prefab and its allowed neighbors per direction |
| Socket | `Socket` | A directional connector that defines compatibility between cell types |
| Cell | `Cell` | A single slot in the grid, tracking remaining possibilities and collapse state |
| Database | `WFCDatabase` | ScriptableObject holding all cell types and sockets for a tileset |
| Map Creator | `MapCreator` | MonoBehaviour that runs the WFC algorithm and drives generation |
| WFC Generator | `WFCGenerator` | Editor tool that auto-generates the database from a sample tilemap |

## Output Modes

Two instantiation strategies are provided:

- **`TilemapInstantiator`** — places collapsed cells onto a Unity `Tilemap`.
- **`PrefabInstantiator`** — spawns a `GameObject` prefab for each collapsed cell, useful for 3D or object-based maps.

Both extend the abstract `Instantiator` base class, so you can implement your own.

## Configuration

On the `MapCreator` component you can configure:

| Property | Description |
|---|---|
| `matrixSize` | Width × Height × Depth of the grid to generate |
| `unitSize` | World-space size of each cell |
| `seed` | Fixed seed for reproducible results |
| `useRandomizedSeed` | Ignore the seed and use a random one each run |
| `chunkSize` | Size of each independently-solved chunk |
| `maxChunkRetries` | How many times to retry a chunk before throwing |
| `maxIterations` | Safety cap on iterations per chunk |

## Getting Started

1. **Import** the scripts into your Unity project (tested with Unity's Tilemap system).
2. **Paint** a small sample map on a `Tilemap` in your scene.
3. **Attach** `WFCGenerator` to the same GameObject and assign a `WFCDatabase` asset.
4. Right-click the component → **Generate WFC Data**. This populates the database.
5. **Attach** `MapCreator` (and a `TilemapInstantiator` or `PrefabInstantiator`) to a GameObject in the scene.
6. Assign the same `WFCDatabase` and configure the grid size.
7. Right-click `MapCreator` → **Create Map** — or call `MapCreator.Instance.Create()` at runtime.

## Error Handling

If WFC reaches an unsolvable state after all chunk retries are exhausted, an `UnfortunateSeedException` is thrown. Try a different seed, increase `maxChunkRetries`, or adjust your sample tilemap to allow more tile combinations.

## License

This project is released into the public domain under the [Unlicense](LICENSE). You are free to use, copy, modify, and distribute it for any purpose without restriction.
