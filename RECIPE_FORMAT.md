# Recipe Format

[Overview](README.md) | [Architecture](ARCHITECTURE.md)

This is the authoring reference for the current JSON recipe language. It documents schema version 1 as represented by `RecipeModels.cs`, the raw validator and the construction code supplied on 24 September 2026.

**Read the implementation caveats below.** The hand-written raw validator is not a complete JSON Schema validator, and custom-block internals do not yet have every capability of top-level blocks.

## Root object

```json
{
  "schemaVersion": 1,
  "target": "ECY-S1000",
  "output": "C:\\Temp\\Example.gfx",
  "customBlocks": [],
  "blocks": [],
  "links": []
}
```

This shows the root structure, not a useful control program.

| Field | Model type | Authoring rule |
| --- | --- | --- |
| `schemaVersion` | Integer | Supply `1`. Other values are rejected by semantic validation. |
| `target` | String | Supply a registered target, currently `ECY-S1000`. |
| `output` | String | Supply a nonblank output path. Use an absolute `.gfx` path and create its directory before running. |
| `customBlocks` | Array of custom definitions | Optional when no `$custom.*` instance is used. Omit it or supply an array. |
| `blocks` | Array of block definitions | Supply the top-level graph. The implementation rejects a null/missing list but does not explicitly reject an empty one at this stage. |
| `links` | Array of link definitions | Supply connections. It may be omitted, but block connection requirements still apply. |

Use the field casing shown here. Do not add `$schema`, comments as fields, documentation fields, raw XML fields or other extensions unless the parser and model have been updated to support them. A standard JSON document is the portable authoring format.

`Program.cs` loads a file named `recipe.json` next to the executable. This document does not define a command-line recipe selector.

## Block definitions

```json
{
  "id": "addMany",
  "type": "Distech.Gpl.Model.Shapes.Blocks.Add",
  "x": 348,
  "y": 144,
  "properties": {
    "Name": "Five Input Total"
  },
  "visibleInputs": ["Input3", "Input4", "Input5"],
  "visibleOutputs": ["Output"]
}
```

| Field | Meaning |
| --- | --- |
| `id` | Recipe lookup key. Use a nonblank, unique ID within the containing graph. |
| `type` | Native catalogue name or top-level `$custom.<definitionId>` reference. |
| `x`, `y` | Integer drawing coordinates. They are not equipment dimensions. |
| `properties` | Optional mapping of exact native public property names to values. |
| `visibleInputs` | Optional array of native input names to expose. |
| `visibleOutputs` | Optional array of native output names to expose. |

`resolvedType` exists in the C# model but is internal bookkeeping. The raw validator does not allow it in authored JSON.

### Type names

Prefer full CLR names in reusable recipes. The supplied catalogue resolves a full CLR name first, then an unambiguous programmatic name, then an unambiguous short CLR name. Friendly labels can be shared by different native families.

For example, a recipe can request `Add`, but the canonical identity is:

```text
Distech.Gpl.Model.Shapes.Blocks.Add
```

A block appearing in the catalogue is not proof that it is supported by the selected controller. The target cache and native compiler provide additional checks.

### IDs and visible names

`id` identifies a recipe instance; it does not automatically set the native block's visible `Name`. For top-level blocks, use `properties.Name` when a specific display name is needed.

Keep IDs simple and consistent. Outer block IDs and custom-definition references are looked up case-insensitively by the supplied application, but native property lookup does not use the same comparer. Use exact property and port spelling rather than relying on uniform case-insensitive behaviour.

### Properties

```json
{
  "id": "setpoint",
  "type": "Distech.Gpl.Model.Shapes.Blocks.InternalConstantNumeric",
  "x": 96,
  "y": 96,
  "properties": {
    "Value": 22.5
  }
}
```

Properties are assigned by reflection to writable public native properties. Values pass through conversion based on the target CLR type. Do not assume that every EC-gfx XML element is a writable property, or that every editor configuration dialog is represented by this simple mechanism.

For custom-block internals, the uploaded builders do not explicitly apply the `properties` dictionary. Avoid relying on internal property assignments until that code path is updated in both builders.

### Visibility is not new-port creation

`visibleInputs` sets each named port visible. It does not hide unlisted ports. An empty array makes no visibility changes.

The Add examples demonstrated exposing existing hidden ports such as `Input3` through `Input5`. This is different from creating ports that do not exist until another configuration property changes the native block.

Use visibility fields rather than inventing an `IPV` property. Native serialization writes the underlying port state. Internal custom-block visibility lists are currently accepted structurally but not explicitly applied by the uploaded custom builders.

## Links

```json
{
  "from": "setpoint.Output",
  "to": "controller.Setpoint"
}
```

Both fields are strings in `blockId.PortName` form. The application splits at the last dot. Use simple block IDs and exact native port names to avoid ambiguity.

For the ordinary outer graph, `from` resolves through a block's output collection and `to` through its input collection. The top-level validator rejects an exact repeated link and two incoming links to the same input. One output feeding different inputs is not the same error and is allowed by these checks.

```json
[
  { "from": "setpoint.Output", "to": "controller.Setpoint" },
  { "from": "setpoint.Output", "to": "setpointMonitor.Input" }
]
```

The array above is a link-list fragment, not a complete recipe.

An input/output being visible does not prove it is optional or legal to connect. The native metadata includes `MustBeConnected` and `ProgramOnly`; the current static layer and native validation apply their respective checks.

## Custom Block definitions

Custom definitions belong in the root `customBlocks` array. A definition contains:

| Field | Meaning |
| --- | --- |
| `id` | Definition key referenced by `$custom.<id>`. Use a unique nonblank value. |
| `name` | Default visible `Name` assigned to each constructed composite. |
| `inputs` | Array of exported input declarations with names and positions. |
| `outputs` | Array of exported output declarations with names and positions. |
| `blocks` | Internal native block definitions. |
| `links` | Internal connections and boundary connections. |

The recipe-defined custom implementation always creates a `SimpleCompositeBlock`. It does not provide a conditional-custom option.

### Boundary references

Inside a custom definition:

```json
[
  { "from": "$input.Input1", "to": "add.Input1" },
  { "from": "$input.Input2", "to": "add.Input2" },
  { "from": "add.Output", "to": "$output.Output" }
]
```

`$input` supplies values from the custom interface to its internal graph. `$output` receives an internal value for the outer graph. These are boundary markers, not ordinary block IDs, and they are interpreted only by the custom builder.

Ordinary internal links still use `internalBlock.Output -> otherInternalBlock.Input`.

### Port positions

```json
{
  "inputs": [
    { "name": "Input1", "x": 36, "y": 96 },
    { "name": "Input2", "x": 36, "y": 168 }
  ],
  "outputs": [
    { "name": "Output", "x": 504, "y": 132 }
  ]
}
```

These coordinates position the exported tags in the internal drawing. They do not position the outer composite; the outer instance's `x` and `y` do that.

All coordinate fields are non-nullable integers. Omitted coordinates become zero in the typed model. Specify both coordinates for each custom port to avoid accidental overlap at the origin.

The runner applies exported-port coordinates. The uploaded scratch builder does not, so geometry parity between the two paths is not yet complete.

### Definition versus instance

```json
{
  "id": "moduleA",
  "type": "$custom.twoInputAdder",
  "x": 348,
  "y": 144,
  "properties": {
    "Name": "Module A"
  }
}
```

`twoInputAdder` is the definition ID. `moduleA` is the outer recipe instance ID. `Module A` is a visible native name supplied for that instance. An internal block named `add` remains local to that instance's internal lookup dictionary.

A second top-level instance of the same definition is constructed from new native objects; the helper does not reuse the first instance's block objects. This is recipe expansion, not a live shared definition object in the saved editor project.

## Complete custom-block example

This example uses the basic construction path already exercised by the arithmetic tests. It intentionally avoids internal property assignments, repeated boundary-input consumers and nested custom instances.

```json
{
  "schemaVersion": 1,
  "target": "ECY-S1000",
  "output": "C:\\Temp\\CustomBlockExample.gfx",
  "customBlocks": [
    {
      "id": "twoInputAdder",
      "name": "Two Input Adder",
      "inputs": [
        { "name": "Input1", "x": 36, "y": 96 },
        { "name": "Input2", "x": 36, "y": 168 }
      ],
      "outputs": [
        { "name": "Output", "x": 504, "y": 132 }
      ],
      "blocks": [
        {
          "id": "add",
          "type": "Distech.Gpl.Model.Shapes.Blocks.Add",
          "x": 252,
          "y": 120
        }
      ],
      "links": [
        { "from": "$input.Input1", "to": "add.Input1" },
        { "from": "$input.Input2", "to": "add.Input2" },
        { "from": "add.Output", "to": "$output.Output" }
      ]
    }
  ],
  "blocks": [
    {
      "id": "constant1",
      "type": "Distech.Gpl.Model.Shapes.Blocks.InternalConstantNumeric",
      "x": 96,
      "y": 96,
      "properties": { "Value": 10 }
    },
    {
      "id": "constant2",
      "type": "Distech.Gpl.Model.Shapes.Blocks.InternalConstantNumeric",
      "x": 96,
      "y": 192,
      "properties": { "Value": 20 }
    },
    {
      "id": "moduleA",
      "type": "$custom.twoInputAdder",
      "x": 348,
      "y": 144,
      "properties": { "Name": "Module A" }
    },
    {
      "id": "monitor",
      "type": "Distech.Gpl.Model.Shapes.Blocks.Monitor",
      "x": 648,
      "y": 144
    }
  ],
  "links": [
    { "from": "constant1.Output", "to": "moduleA.Input1" },
    { "from": "constant2.Output", "to": "moduleA.Input2" },
    { "from": "moduleA.Output", "to": "monitor.Input" }
  ]
}
```

## Custom-block implementation caveats

The arrays and models permit more than the current builders implement. In particular:

- **Internal configuration:** internal native blocks are instantiated and positioned, but their property and visibility requests are not explicitly applied in the uploaded source.
- **Boundary creation and fan-out:** exported ports are created from boundary links, not declaration arrays alone. Each `$input` link calls `CreateExportedInput()` again; the helper has no explicit reuse map. Do not assume multiple internal consumers share a single exported input correctly without verification.
- **Nested custom instances:** internal `type` values are resolved against the native catalogue. `$custom.*` is not recursively expanded there. Direct `$input -> $output` passthrough also has no dedicated branch.
- **Validation parity:** duplicate custom-definition IDs, boundary consistency and internal graph semantics are not all checked with the top-level rules. The scratch parser may skip malformed internal references that the runner rejects.

For initial authored recipes, declare unique port names, use each exported input once internally, connect each exported output once internally, and keep custom definitions one level deep. These are conservative authoring guidelines, not claims that the raw validator enforces every condition.

## Error stages

| Console stage | Meaning |
| --- | --- |
| `RECIPE SCHEMA FAILED` | Raw JSON/structural/unknown-field checks reported errors. |
| `VALIDATION FAILED` before native messages | The semantic layer reported errors. |
| `Native creation failed for block ...` | Scratch block or custom-content construction threw an exception. |
| `Native link rejected ...` | Scratch outer linking failed. |
| `Distech compiler: ...` | The target-aware compiler wrapper returned a diagnostic. |
| `FAILED` during execution | Output construction or save failed after earlier validation passed. |

A missing `schemaVersion` becomes zero in the current model and is rejected. Adding a field to a model is not enough to make it legal JSON: the raw whitelist must allow it, and both construction paths must implement its effect.

## Authoring checklist

Supply schema version, target and output path. Use exact full CLR names where possible, unique IDs, exact port/property names, and fully connected required inputs and outputs. Keep custom boundary declarations aligned with their boundary links and give tags explicit positions.

Run the entire pipeline, inspect the output in EC-gfx, and verify any target-specific resource configuration. Do not infer functional PID/AHU behaviour solely from a successful graph compile or save.

### Source references

The field tables follow `RecipeModels.cs` and the field sets in `RecipeSchemaValidator.cs`. Lookup and conversion behaviour follows `BlockCatalogue.Resolve()` and `GfxRuntime.SetProperty()` / `ConvertValue()`. Custom behaviour and limitations follow `GfxRecipeRunner.CreateCustomBlock()` and `RecipeValidator.CreateCustomBlockForValidation()`.
