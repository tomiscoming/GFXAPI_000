# Architecture

[Overview](README.md) | [Recipe format](RECIPE_FORMAT.md)

This document describes the supplied implementation, not an idealised future compiler. Proposed changes are explicitly identified as open work.

Documentation baseline: 24 September 2026. The source snapshot and later console observations are not assumed to be identical revisions.

## Design principle

Construct native Distech objects and let the native serializer write `.gfx` files. Recipes express graph structure and supported configuration; they do not contain hand-written EC-gfx XML or executable C#.

The project is an integration layer around vendor objects. Static checks improve diagnostics, while native graph construction and target-aware compilation provide additional checks that the recipe layer cannot reproduce reliably from metadata alone.

## High-level flow

```text
Program.Main()
    |
    +-- Read <executable directory>/recipe.json
    |
    +-- RecipeSchemaValidator.Validate(json)
    |       Reject reported schema errors
    |
    +-- JavaScriptSerializer.Deserialize<GfxRecipe>(json)
    |
    +-- Create output GfxRuntime
    |
    +-- RecipeValidator.Validate(recipe)
    |       |
    |       +-- Version, target, block, property and link checks
    |       +-- Required-connection checks
    |       +-- Create separate scratch GfxRuntime
    |       +-- Construct scratch blocks and links
    |       +-- scratch.CompileWithDistech(recipe.target)
    |
    +-- GfxRecipeRunner.Execute(recipe)
            |
            +-- Construct output blocks and custom contents
            +-- Apply top-level properties and visibility
            +-- Create outer links
            +-- GfxRuntime.Save(recipe.output)
                    |
                    +-- Project.SaveToFile(...)
```

**Two native graphs are built.** The graph compiled during validation is not the same object graph later saved. Shared construction is a design objective, not an implemented guarantee of identical results.

Source: `Program.Main()`, `RecipeValidator.ValidateNativeGraph()`, `GfxRecipeRunner.Execute()` and `GfxRuntime.Save()`.

## Responsibilities and boundaries

| Component | Responsibility | Boundary |
| --- | --- | --- |
| `Program` | Load, deserialize, validate, execute and report. | No implemented recipe-path command-line option. |
| `RecipeSchemaValidator` | Check object/array structure and allowed keys. | Not a complete JSON Schema implementation. |
| `RecipeModels` | Carry typed JSON fields and resolved type identity. | A field existing in a model does not mean every construction path applies it. |
| `RecipeValidator` | Top-level semantics and scratch native validation. | Custom internals do not receive all top-level static checks. |
| `GfxRecipeRunner` | Build and save the output project. | Does not call the native compiler before saving. |
| `GfxRuntime` | Wrap native construction, linking, property setting, target setup and serialization. | Does not implement the recipe-defined custom builder. |
| `BlockCatalogue` | Discover native types and resolve aliases. | Describes constructor-time metadata, not every configured instance. |
| `TargetCatalogue` | Resolve target keys to controller identities. | The supplied defaults register ECY-S1000. |

The application classes use the `GfxApi` namespace; catalogue/runtime classes use `GFX_BLOCK_CATALOGUE`.

## Raw JSON validation

`RecipeSchemaValidator` uses `JavaScriptSerializer.DeserializeObject()` and traverses dictionaries and arrays. It checks allowed fields at the recipe, custom definition, custom port, block and link levels. Nested `blocks` and `links` inside a custom definition are checked structurally.

The allowed-key sets are case-insensitive, but collection lookups use literal keys such as `"customBlocks"`. This is not a complete, consistent promise of case-insensitive recipe syntax. Author recipes with the exact documented spelling.

The class does not perform complete scalar type checks, require every field at every level, or enforce all semantic rules. For example, accepting a custom port's `name` field is different from proving that it is unique, nonblank and connected correctly.

Source: `RecipeSchemaValidator.Validate()`, `ValidateCustomBlocks()`, `ValidateCustomPorts()` and `ValidateUnknownFields()`.

## Typed recipe identity

The principal models are `GfxRecipe`, `BlockRecipe`, `LinkRecipe`, `CustomBlockRecipe` and `CustomPortRecipe`.

A block carries both the requested `type` and an internal `resolvedType`:

```text
Requested native alias:
    type = Add
    resolvedType = Distech.Gpl.Model.Shapes.Blocks.Add

Requested recipe definition:
    type = $custom.threeInputAdder
    resolvedType = Distech.Gpl.Model.Shapes.Blocks.SimpleCompositeBlock
```

`resolvedType` is populated during semantic validation. It is deliberately absent from the raw block-field whitelist and should not be authored in JSON.

The original custom reference must be retained: knowing that a block is a `SimpleCompositeBlock` is insufficient to identify which recipe definition supplies its contents.

Source: `RecipeModels.cs`, `RecipeValidator.ValidateBlocks()` and `ValidateCustomBlockReference()`.

## Catalogue discovery and alias resolution

Each new `GfxRuntime` loads model assemblies, creates its own `TargetCatalogue` and `BlockCatalogue`, and creates a `Project` with a `Main` drawing.

Assembly discovery starts with `typeof(Block).Assembly` and searches its directory for `DC.Gpl.Model*.dll`. Existing loaded assemblies are reused where found. Assembly/type/constructor failures are handled defensively in several places; incomplete discovery can therefore appear later as an unknown type or missing metadata rather than as a startup error.

`BlockCatalogue` records full CLR identity, friendly name, public properties, constructor-time ports, visibility, connection requirements, program-only flags, formats, geometry and target support.

The actual `Resolve()` lookup order is:

1. Full CLR name.
2. An unambiguous programmatic name.
3. An unambiguous short CLR name.

Ambiguity is tracked separately in the two alias indexes. Full names are the least ambiguous recipe spelling; this implementation is not a universal cross-index ambiguity resolver.

Format capture uses `Format.GetType().FullName` and `Format.GetTextToSerialize()`. The recipe validator does not reject a link solely because the format names differ.

Source: `GfxRuntime.LoadAssemblies()`, `BlockCatalogue.Discover()`, `Resolve()`, `BuildIndexes()` and `CreatePortDefinition()`.

## Target support and cache loading

The default target definition is ECY-S1000, model type `10016C000502040D`, with `PlatformFamily = "IP"` in the supplied source.

Cache lookup prefers:

```text
<executable directory>/TargetSupport/S1000.json
```

and falls back to:

```text
C:\Temp\S1000.json
```

Entries attach to discovered blocks by full CLR name. `Supported` passes the cache check; `Unsupported` produces an error; `Unknown` is allowed to proceed to native validation. Missing cache entries therefore do not automatically reject a block.

The loader records support state, source and notes. It does not compare assembly hashes or enforce that the cache was generated against the installed DLL versions. Cache freshness remains a maintenance concern.

Source: `TargetCatalogue.RegisterDefaults()`, `BlockCatalogue.LoadDefaultTargetSupport()`, `ApplyTargetSupportEntry()` and `RecipeValidator.ValidateTargetSupport()`.

## Static semantic validation

For ordinary top-level blocks, the validator checks type resolution, constructor availability, cached target support, writable properties and convertible values, plus requested visible port names. It also checks block IDs, link references, directions, exact duplicate links, multiple incoming links and required connections.

Source and target port checks search the appropriate catalogue collection. Required-connection checks skip ports marked `ProgramOnly`. Compatibility checks reject program-only connections where both endpoint definitions can be resolved, but do not invent Boolean-versus-numeric restrictions from display formats.

For a `$custom.*` block, `ValidateBlocks()` resolves its definition, assigns the composite CLR type and continues. It does not then run the ordinary property/target/visibility checks against the custom interface. Outer custom-port checks also do not obtain that interface from the recipe definition. Native construction currently catches many errors left by those gaps.

Metadata is a constructor-time snapshot. Hidden ports already present in an Add block can be discovered, but ports created later by a property change may not be represented by static metadata. General configured-instance validation is not complete.

Source: `RecipeValidator.ValidateBlocks()`, `ValidateLinkReferences()`, `ValidatePortCompatibility()` and `ValidateRequiredConnections()`.

## Native scratch graph

When earlier stages report no errors, `ValidateNativeGraph()` creates a separate `GfxRuntime`. Ordinary top-level blocks are created, configured and made visible before the outer links are connected.

Custom instances take a separate path through `CreateCustomBlockForValidation()`. This helper constructs the native composite, internal blocks, boundary ports and internal links.

The runtime's `Connect()` resolves an `IOutputPort` and `IInputPort`, then calls `Block.CreateLink()`. Rejection is converted to an exception and reported as a native-link error. The scratch builder does not contain a separate explicit traversal of every block's `IsConnectionsValid`; the next stage is native compilation.

Source: `RecipeValidator.ValidateNativeGraph()` and `GfxRuntime.Connect()`.

## Native compilation

`CompileWithDistech(targetKey)` resolves the configured target, creates an offline `DynamicDevice`, attaches `VirtualDeviceInformationService`, obtains the matching device template and applies it. `CompileWithDistechDevice()` then creates an `IPPlatform`, attaches it to the project, creates the compiler and calls `CompileProject(Project)`.

The wrapper reads a public `Errors` property through reflection and formats available fields such as severity, message, block and port. Every returned non-null entry becomes a validation error; severity-aware warning handling is not implemented. Conversely, if the expected property/collection cannot be obtained, this path does not explicitly report that diagnostics were unavailable.

The platform is disposed in `finally`. This is not evidence that every object created by discovery and both runtimes has a fully managed lifecycle.

Source: `GfxRuntime.CompileWithDistech()`, `CompileWithDistechDevice()` and `FormatCompilerError()`.

## Custom Block construction

Ordinary recipe-defined Custom Blocks use `SimpleCompositeBlock`. Conditional custom blocks are outside the current recipe-defined custom path.

Each instance gets a new composite and a fresh dictionary of internal native blocks. Internal IDs are local lookup keys, not instructions to rename every native block. The custom definition's `name` is assigned to the composite's `Name`; a top-level instance `properties.Name` can subsequently override it.

```text
Definition: threeInputAdder

Instance: moduleA             Instance: moduleB
    add1                         add1
    add2                         add2
```

These are separately constructed native objects. Where a native block consumes a controller resource, object separation alone is not proof of distinct correctly assigned runtime resources; inspect the resulting resource allocation as part of testing.

The helpers interpret three internal link forms:

```text
$input.Name -> internalBlock.Input
internalBlock.Output -> $output.Name
internalBlock.Output -> anotherInternalBlock.Input
```

The first uses `CreateExportedInput()`, the second `CreateExportedOutput()`, and the third `Block.CreateLink()`. An exported input supplies the internal graph; an exported output receives from it. From the outer graph, those same boundaries behave as normal custom inputs and outputs.

Ports are created while processing links, not simply by traversing the `inputs` and `outputs` declaration arrays. In the runner, those arrays provide positions for matching exported ports. Declaring an unused port does not by itself create it.

Source: `GfxRecipeRunner.CreateCustomBlock()` and `RecipeValidator.CreateCustomBlockForValidation()`.

## Geometry and configuration

`GfxRuntime.CreateBlock()` applies catalogue geometry when known, otherwise retaining constructor dimensions. The supplied explicit override is the 96 by 12 Internal Constant geometry.

The custom helpers directly instantiate internal blocks and set `Location`; they bypass this runtime geometry path. Their internal block construction also does not explicitly apply `properties`, `visibleInputs` or `visibleOutputs` in the uploaded snapshot.

The runner positions exported tags from `CustomPortRecipe.x` and `.y`. The uploaded scratch custom builder does not mirror that positioning. These fields are non-nullable integers, so absent coordinates are indistinguishable in the typed model from a requested zero.

At top level, `visibleInputs` and `visibleOutputs` are additive requests to set named native ports visible. They are not a replacement list that hides everything omitted, and they do not implement arbitrary new-port creation.

Source: `GfxRuntime.ApplyDefaultGeometry()`, `SetInputVisible()`, `SetOutputVisible()` and both custom helpers.

## Validation, execution and save are different milestones

The output runner reconstructs the recipe after the scratch compile. It calls `Save()` without recompiling that output project. Target setup occurs on the scratch graph in the normal validation path; the runner's `target` property is not used by its supplied execution method.

Therefore, retain these distinctions:

```text
Recipe passed this validator
    != every requested field was applied
    != final saved graph was recompiled
    != native resources were independently verified
    != sequence executed correctly on a controller
    != commissioned application
```

This is especially important for PID and AHU examples. A successful save does not prove tuning, direction, feedback semantics, output shutdown behaviour or safety interlocks.

## Current limitations and open work

The following are implementation observations unless labelled as proposals.

| Area | Current limitation | Proposed next step |
| --- | --- | --- |
| Shared construction | Validator and runner duplicate custom construction. | Extract a common builder used by both paths. |
| Internal configuration | Internal properties and visibility lists are not explicitly applied. | Reuse the same configuration sequence as top-level blocks. |
| Custom nesting | Internal types resolve through the native catalogue only. | Add deliberate recursive custom resolution with cycle detection. |
| Exported-input fan-out | Each boundary link calls the export constructor; the helper has no export-reuse map. | Create each declared boundary port once and wire all its consumers. |
| Boundary validation | Custom interfaces and contents lack full top-level semantic parity. | Validate definitions, declarations, references and internal graphs explicitly. |
| Malformed internal references | Scratch parsing can skip a bad link where runner parsing throws. | Share parsing and reject malformed references consistently. |
| Dynamic interfaces | Catalogue metadata describes fresh instances. | Inspect configured instances when properties change ports. |
| Geometry | Top-level/native and internal/custom paths differ. | Share geometry handling and distinguish absent from zero coordinates. |
| Diagnostics | Deepest-message reporting loses stacks; compiler severity handling is coarse. | Preserve diagnostic structure and unavailable-diagnostics failures. |
| Resource use | Each runtime constructs a catalogue; the scratch runtime is separate. | Investigate shared immutable metadata and explicit lifecycle handling. |
| Final graph assurance | The compiled scratch graph is not the saved graph. | Build once or use shared construction plus final-graph validation. |

These proposals are not promises of current functionality. The preferred short-term authoring subset is documented in [RECIPE_FORMAT.md](RECIPE_FORMAT.md).

## Verification discipline

Keep positive and negative recipe examples for changes. Suggested coverage includes ordinary arithmetic, hidden Add inputs, duplicate links, multiple incoming links, missing required connections, one custom instance, repeated instances, boundary fan-out and internal configuration.

Record what was actually checked: JSON parsing, static checks, native construction, native compile, save, reopen, resource allocation and functional execution are separate results. The supplied development session demonstrates some of these manually; no automated test harness is established by the source snapshot.

## Documentation notes

The earlier overview draft treated exact .NET/C# versions, uniform custom-internal configuration and deterministic validation/execution parity too broadly. Those are not established by the supplied files. This document records the source behaviour and labels the gaps rather than promoting the intended architecture to a finished feature set.
