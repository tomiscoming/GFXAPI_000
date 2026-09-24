# GFX API

Experimental JSON-to-EC-gfx program generation using Distech Controls' native .NET object model.

GFX API reads a declarative recipe, validates it, builds a temporary native graph, runs the Distech compiler against a controller target, then constructs and saves an output `.gfx` project. It does not generate EC-gfx XML by hand.

**Status:** experimental. The primary configured target is ECY-S1000. Successful generation is not a commissioning result or proof that a control sequence is safe or functionally correct.

## Documentation

| Document | Purpose |
| --- | --- |
| [README.md](README.md) | Project overview, setup, first run and troubleshooting. |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Pipeline, class responsibilities, native integration and implementation gaps. |
| [RECIPE_FORMAT.md](RECIPE_FORMAT.md) | Recipe fields, naming, links, visibility and Custom Block examples. |

Documentation baseline: 24 September 2026. These notes describe the supplied C# source snapshot, with development-run observations identified separately. Check the current checkout before assuming a later change is covered here.

## Project goal

Provide a predictable intermediate layer between a control-program description and the native EC-gfx representation. A human or higher-level tool can describe blocks, configuration and connections in JSON; the application resolves those requests against known native types and builds the graph.

The intended separation is:

```text
Recipe author or higher-level generator
                  |
                  v
          Declarative JSON
                  |
                  v
      Validation and construction
                  |
                  v
       Native Distech object graph
                  |
                  v
        Native serializer -> .gfx
```

The project can support machine-generated recipes without requiring a language model to invent vendor XML or generate arbitrary C# as part of the recipe. There is no LLM service or autonomous agent integration in the supplied pipeline itself.

## Current capabilities

The top-level recipe path supports block type resolution, public property assignment, positioning, input/output visibility, reference checks, duplicate-link rejection, one incoming link per input, required connections and normal `ProgramOnly` restrictions. Target support combines a cached catalogue with native compilation.

Recipe-defined Custom Blocks use `Distech.Gpl.Model.Shapes.Blocks.SimpleCompositeBlock`. The implementation can place native blocks inside a custom block, create internal links, export inputs and outputs, and instantiate the same definition more than once. The runner also positions exported port tags from their recipe coordinates.

Development runs have demonstrated ordinary arithmetic graphs, five visible inputs on an Add block, multiple instances of an arithmetic Custom Block, and construction/compilation/save of a two-instance PID recipe. Screenshots demonstrated reopening and inspecting the arithmetic examples in EC-gfx. These are manual observations, not a supplied automated regression suite.

**Important:** support is not uniform between top-level blocks and custom-block internals. In the uploaded source, internal block `properties` and `visibleInputs` / `visibleOutputs` are not explicitly applied by the custom builders. Nested `$custom.*` instances and general exported-input fan-out also need further implementation or verification. See [current limitations](ARCHITECTURE.md#current-limitations-and-open-work).

## Environment and dependencies

Use the existing Windows/Visual Studio project and the Distech assemblies referenced by that project. `GfxRuntime` discovers model assemblies next to the assembly containing Distech's `Block` class; it does not use a web service or download an SDK at runtime.

The sources reference `System.Web.Script.Serialization.JavaScriptSerializer`, `System.Drawing` and Distech libraries. The supplied files do not include the `.csproj` or solution metadata, so they do not establish an exact target framework, C# language version, dependency-installation procedure or supported architecture matrix. Take those settings from the actual project rather than treating an assumed .NET or C# version as a requirement.

A successful development run reported a 64-bit process on a 64-bit OS. A previous run reported `OutOfMemoryException`; its root cause was not established. Do not interpret that sequence as proof that changing process architecture fixed the exception.

## First run

1. Open the existing solution and restore its local Distech assembly references. Confirm the project builds in the configured environment.
2. Save the example below as `recipe.json` beside the built executable. Ensure any build-copy step does not replace it with an older recipe.
3. Place the support-cache file at `TargetSupport\S1000.json` beside the executable. The current development fallback is `C:\Temp\S1000.json`.
4. Create the output directory specified in the recipe, run the console application, and inspect the path printed under `Loading recipe:`.
5. Open the generated `.gfx` in EC-gfx and inspect the blocks and connections.

`Program.cs` always loads `recipe.json` from `AppDomain.CurrentDomain.BaseDirectory`. Although `Main` receives arguments, the supplied implementation does not use them to choose a recipe. It also waits for a keypress before exiting.

The support-cache file is not the recipe. Its `Target` field records the probe target; the recipe must separately contain its own `target` and `schemaVersion` fields.

### Minimal connected example

```json
{
  "schemaVersion": 1,
  "target": "ECY-S1000",
  "output": "C:\\Temp\\Example.gfx",
  "blocks": [
    {
      "id": "value1",
      "type": "Distech.Gpl.Model.Shapes.Blocks.InternalConstantNumeric",
      "x": 96,
      "y": 96,
      "properties": { "Value": 10 }
    },
    {
      "id": "value2",
      "type": "Distech.Gpl.Model.Shapes.Blocks.InternalConstantNumeric",
      "x": 96,
      "y": 192,
      "properties": { "Value": 20 }
    },
    {
      "id": "add",
      "type": "Distech.Gpl.Model.Shapes.Blocks.Add",
      "x": 348,
      "y": 144
    },
    {
      "id": "monitor",
      "type": "Distech.Gpl.Model.Shapes.Blocks.Monitor",
      "x": 600,
      "y": 144
    }
  ],
  "links": [
    { "from": "value1.Output", "to": "add.Input1" },
    { "from": "value2.Output", "to": "add.Input2" },
    { "from": "add.Output", "to": "monitor.Input" }
  ]
}
```

This describes two constants connected to an Add block, with its output connected to a monitor. The intended arithmetic result is 30; an offline file being generated does not itself demonstrate live evaluation of that result.

## Pipeline and success criteria

```text
Read recipe.json
    -> raw structure / unknown-field checks
    -> deserialize GfxRecipe
    -> semantic checks
    -> construct separate native scratch graph
    -> compile scratch graph for recipe.target
    -> construct output graph through GfxRecipeRunner
    -> save through GfxRuntime.Save()
```

The runner is not called when the earlier stages return errors. Execution can still fail after validation, because the output graph is constructed separately. The current implementation does not save the compiled scratch graph or perform a second native compile of the final graph before saving.

A complete generation run ends with a saved path and `Recipe completed successfully.` This means the current pipeline reached its save step successfully. Reopening, resource checks, simulation or controller testing, and engineering acceptance remain separate tasks.

## Target support

The default target in `TargetCatalogue.RegisterDefaults()` is:

```text
Key:             ECY-S1000
DeviceModelName:  ECY-S1000
DeviceModelType:  10016C000502040D
PlatformFamily:  IP
```

Support entries are matched by full CLR name, not by the friendly block label. The states are `Supported`, `Unsupported` and `Unknown`. A known unsupported block is rejected; unknown support is left to native validation. Supported means that the target-support check permits the type, not that every configuration of that type is valid.

The in-memory block catalogue is discovered from assemblies. The large `DistechBlockCatalogue_v2` JSON export is reference material, not the file that `GfxRuntime` loads to discover blocks. The S1000 support JSON is loaded separately to annotate discovered types.

## Source map

| File | Main responsibility |
| --- | --- |
| `Program.cs` | Loads the recipe and coordinates validation, execution and reporting. |
| `RecipeModels.cs` | Typed recipe, block, link and custom-port models. |
| `RecipeSchemaValidator.cs` | Hand-written structural and unknown-field checks. |
| `RecipeValidator.cs` | Semantic checks, scratch construction and native compilation. |
| `GfxRecipeRunner.cs` | Constructs the output graph and saves it. |
| `GfxRuntime.cs` | Native project, block creation, properties, links, compilation and save. |
| `BlockCatalogue.cs` / `BlockDefinition.cs` | Block discovery, lookup and metadata. |
| `TargetCatalogue.cs` / `TargetDefinition.cs` | Controller identities and support-state models. |

## Troubleshooting

| Symptom | Check first |
| --- | --- |
| Recipe changes do not appear | The exact path printed under `Loading recipe:` and any build-copy behaviour. |
| `Unsupported recipe schemaVersion '0'` | Add `"schemaVersion": 1` to the recipe being loaded. |
| `Unknown field 'x'` on a custom port | Check that `CustomPortFields` and `CustomPortRecipe` both contain `x` and `y`. |
| A type is unknown or ambiguous | Use its exact full CLR name and check assembly loading/catalogue discovery. |
| A custom block has no requested port | Check its boundary links and both custom-builder implementations. Declarations alone do not create ports. |
| Validation passes but execution fails | Compare `CreateCustomBlockForValidation()` with `CreateCustomBlock()`. They are separate implementations. |
| `OutOfMemoryException` | Preserve the complete exception/stack and process diagnostics. A later successful run does not identify the earlier cause. |

## Development rules and scope

Prefer native construction and serialization over hand-written EC-gfx XML. Preserve working JSON examples when adding behaviour, and keep changes to the validator and runner consistent until construction is shared.

Do not equate a port's display format with a strict connection type. Do not equate five exposed Add inputs with arbitrary dynamic-port support for every native block. Do not describe a proposed feature as implemented simply because the typed model accepts its fields.

This is a graph-generation experiment, not a ready-made AHU application or a plant-safety system. PID/AHU examples require functional review, correct native configuration and testing before any real equipment use.

### Documentation basis

Implementation descriptions come from the named source files above, particularly `Program.Main()`, `GfxRuntime.LoadAssemblies()`, `BlockCatalogue.LoadDefaultTargetSupport()` and the two custom-block construction methods. Manual run observations come from the development session through 24 September 2026. Exact build settings and broad vendor compatibility are not established by that evidence.
