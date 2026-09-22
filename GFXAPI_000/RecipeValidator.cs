using Distech.Gpl.Model.Shapes.Blocks;
using GFX_BLOCK_CATALOGUE;
using System;
using System.Collections.Generic;
using System.Linq;


namespace GfxApi
{
    public class RecipeValidator
    {
        private readonly GfxRuntime _gfx;

        public RecipeValidator(
            GfxRuntime gfx)
        {
            if (gfx == null)
            {
                throw new ArgumentNullException(
                    "gfx"
                );
            }

            _gfx = gfx;
        }

        // ============================================================
        // Main validation entry point
        // ============================================================

        public List<string> Validate(
            GfxRecipe recipe)
        {
            List<string> errors =
                new List<string>();

            if (recipe == null)
            {
                errors.Add(
                    "Recipe is null."
                );

                return errors;
            }

            // --------------------------------------------------------
            // Basic recipe checks
            // --------------------------------------------------------


            if (
                string.IsNullOrWhiteSpace(
                    recipe.output
                ))
            {
                errors.Add(
                    "Recipe output path is missing."
                );
            }

            ValidateTarget(
                recipe,
                errors
            );

            if (recipe.blocks == null)
            {
                errors.Add(
                    "Recipe contains no blocks."
                );

                return errors;
            }
            // --------------------------------------------------------
            // Static / semantic validation
            // --------------------------------------------------------

            ValidateBlockIds(
                recipe,
                errors
            );

            ValidateBlocks(
                recipe,
                errors
            );

            ValidateLinkReferences(
                recipe,
                errors
            );

            // --------------------------------------------------------
            // Only ask Distech if the recipe is structurally sane
            // --------------------------------------------------------

            if (errors.Count == 0)
            {
                ValidateNativeGraph(
                    recipe,
                    errors
                );
            }

            return errors;
        }

        // ============================================================
        // Target
        // ============================================================

        private void ValidateTarget(
            GfxRecipe recipe,
            List<string> errors)
        {
            if (
                string.IsNullOrWhiteSpace(
                    recipe.target
                ))
            {
                errors.Add(
                    "Recipe target is missing."
                );

                return;
            }

            TargetDefinition target =
                _gfx.ResolveTarget(
                    recipe.target
                );

            if (target == null)
            {
                errors.Add(
                    "Unknown recipe target '" +
                    recipe.target +
                    "'."
                );
            }
        }

        // ============================================================
        // Block IDs
        // ============================================================

        private void ValidateBlockIds(
            GfxRecipe recipe,
            List<string> errors)
        {
            HashSet<string> seen =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                BlockRecipe block
                in recipe.blocks)
            {
                if (block == null)
                {
                    errors.Add(
                        "Recipe contains a null block definition."
                    );

                    continue;
                }

                if (
                    string.IsNullOrWhiteSpace(
                        block.id
                    ))
                {
                    errors.Add(
                        "A block is missing an id."
                    );

                    continue;
                }

                if (!seen.Add(block.id))
                {
                    errors.Add(
                        "Duplicate block id: " +
                        block.id
                    );
                }
            }
        }

        // ============================================================
        // Block definitions
        // ============================================================

        private void ValidateBlocks(
            GfxRecipe recipe,
            List<string> errors)
        {
            foreach (
                BlockRecipe definition
                in recipe.blocks)
            {
                if (definition == null)
                {
                    continue;
                }

                if (
                    string.IsNullOrWhiteSpace(
                        definition.id
                    ))
                {
                    continue;
                }

                if (
                    string.IsNullOrWhiteSpace(
                        definition.type
                    ))
                {
                    errors.Add(
                        "Block '" +
                        definition.id +
                        "' is missing a type."
                    );

                    continue;
                }

                BlockDefinition blockDefinition =
                    _gfx.Catalogue.Resolve(
                        definition.type
                    );

                if (blockDefinition == null)
                {
                    errors.Add(
                        "Block '" +
                        definition.id +
                        "' uses unknown type '" +
                        definition.type +
                        "'."
                    );

                    continue;
                }

                int errorCountBeforeTargetCheck =
                    errors.Count;

                ValidateTargetSupport(
                    recipe,
                    definition,
                    blockDefinition,
                    errors
                );

                if (
                    errors.Count >
                    errorCountBeforeTargetCheck
                )
                {
                    continue;
                }

                if (
                    !blockDefinition
                        .HasPublicParameterlessConstructor)
                {
                    errors.Add(
                        "Block '" +
                        definition.id +
                        "' type '" +
                        definition.type +
                        "' has no public parameterless constructor."
                    );

                    continue;
                }

                ValidateProperties(
                    definition,
                    blockDefinition,
                    errors
                );

                ValidateVisibleInputs(
                    definition,
                    blockDefinition,
                    errors
                );

                ValidateVisibleOutputs(
                    definition,
                    blockDefinition,
                    errors
                );
            }
        }

        // ============================================================
        // Target support
        // ============================================================

        private void ValidateTargetSupport(
            GfxRecipe recipe,
            BlockRecipe definition,
            BlockDefinition blockDefinition,
            List<string> errors)
        {
            if (
                recipe == null ||
                definition == null ||
                blockDefinition == null)
            {
                return;
            }

            if (
                string.IsNullOrWhiteSpace(
                    recipe.target
                ))
            {
                return;
            }

            TargetSupportState state =
                _gfx.GetBlockTargetSupportState(
                    blockDefinition.FullName,
                    recipe.target
                );

            // --------------------------------------------------------
            // Supported
            //
            // Cached catalogue explicitly says this block is valid
            // for the selected controller.
            // --------------------------------------------------------

            if (
                state ==
                TargetSupportState.Supported
            )
            {
                return;
            }

            // --------------------------------------------------------
            // Unsupported
            //
            // This is definitive target-specific evidence from the
            // native Distech compiler probe.
            // --------------------------------------------------------

            if (
                state ==
                TargetSupportState.Unsupported
            )
            {
                errors.Add(
                    "Block '" +
                    definition.id +
                    "' type '" +
                    blockDefinition.FullName +
                    "' is not supported by target '" +
                    recipe.target +
                    "'."
                );

                return;
            }

            // --------------------------------------------------------
            // Unknown
            //
            // Do NOT reject.
            //
            // Unknown means we do not have definitive cached evidence.
            // Native graph/compiler validation remains the final
            // authority.
            // --------------------------------------------------------
        }

        // ============================================================
        // Properties
        // ============================================================

        private void ValidateProperties(
            BlockRecipe definition,
            BlockDefinition blockDefinition,
            List<string> errors)
        {
            if (
                definition.properties
                == null)
            {
                return;
            }

            foreach (
                KeyValuePair<string, object> requested
                in definition.properties)
            {
                PropertyDefinition property;

                if (
                    !blockDefinition.Properties.TryGetValue(
                        requested.Key,
                        out property
                    ))
                {
                    errors.Add(
                        "Block '" +
                        definition.id +
                        "' has no property '" +
                        requested.Key +
                        "'."
                    );

                    continue;
                }

                if (!property.CanWrite)
                {
                    errors.Add(
                        "Property '" +
                        requested.Key +
                        "' on block '" +
                        definition.id +
                        "' is read-only."
                    );

                    continue;
                }

                if (
                    !CanConvertValue(
                        requested.Value,
                        property.PropertyType
                    ))
                {
                    errors.Add(
                        "Property '" +
                        requested.Key +
                        "' on block '" +
                        definition.id +
                        "' cannot accept value '" +
                        requested.Value +
                        "' as " +
                        property.PropertyType.Name +
                        "."
                    );
                }
            }
        }

        // ============================================================
        // Visible inputs
        // ============================================================

        private void ValidateVisibleInputs(
            BlockRecipe definition,
            BlockDefinition blockDefinition,
            List<string> errors)
        {
            if (
                definition.visibleInputs
                == null)
            {
                return;
            }

            foreach (
                string portName
                in definition.visibleInputs)
            {
                bool exists =
                    blockDefinition.Inputs.Any(
                        x =>
                            string.Equals(
                                x.Name,
                                portName,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );

                if (!exists)
                {
                    errors.Add(
                        "Block '" +
                        definition.id +
                        "' has no input port '" +
                        portName +
                        "'."
                    );
                }
            }
        }

        // ============================================================
        // Visible outputs
        // ============================================================

        private void ValidateVisibleOutputs(
            BlockRecipe definition,
            BlockDefinition blockDefinition,
            List<string> errors)
        {
            if (
                definition.visibleOutputs
                == null)
            {
                return;
            }

            foreach (
                string portName
                in definition.visibleOutputs)
            {
                bool exists =
                    blockDefinition.Outputs.Any(
                        x =>
                            string.Equals(
                                x.Name,
                                portName,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );

                if (!exists)
                {
                    errors.Add(
                        "Block '" +
                        definition.id +
                        "' has no output port '" +
                        portName +
                        "'."
                    );
                }
            }
        }

        // ============================================================
        // Link reference validation
        // ============================================================

        private void ValidateLinkReferences(
            GfxRecipe recipe,
            List<string> errors)
        {
            if (recipe.links == null)
            {
                return;
            }

            Dictionary<string, BlockRecipe> blocks =
                recipe.blocks
                .Where(
                    x =>
                        x != null &&
                        !string.IsNullOrWhiteSpace(
                            x.id
                        )
                )
                .GroupBy(
                    x => x.id,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                LinkRecipe link
                in recipe.links)
            {
                if (link == null)
                {
                    errors.Add(
                        "Recipe contains a null link."
                    );

                    continue;
                }

                PortReference from;

                PortReference to;

                if (
                    !TryParsePortReference(
                        link.from,
                        out from
                    ))
                {
                    errors.Add(
                        "Invalid source reference '" +
                        link.from +
                        "'. Expected blockId.PortName."
                    );

                    continue;
                }

                if (
                    !TryParsePortReference(
                        link.to,
                        out to
                    ))
                {
                    errors.Add(
                        "Invalid target reference '" +
                        link.to +
                        "'. Expected blockId.PortName."
                    );

                    continue;
                }

                BlockRecipe fromDefinition;

                if (
                    !blocks.TryGetValue(
                        from.BlockId,
                        out fromDefinition
                    ))
                {
                    errors.Add(
                        "Link source block '" +
                        from.BlockId +
                        "' does not exist."
                    );

                    continue;
                }

                BlockRecipe toDefinition;

                if (
                    !blocks.TryGetValue(
                        to.BlockId,
                        out toDefinition
                    ))
                {
                    errors.Add(
                        "Link target block '" +
                        to.BlockId +
                        "' does not exist."
                    );

                    continue;
                }

                ValidateSourcePort(
                    fromDefinition,
                    from,
                    errors
                );

                ValidateTargetPort(
                    toDefinition,
                    to,
                    errors
                );
            }
        }

        // ============================================================
        // Source port check
        // ============================================================

        private void ValidateSourcePort(
            BlockRecipe definition,
            PortReference reference,
            List<string> errors)
        {
            BlockDefinition blockDefinition =
                _gfx.Catalogue.Resolve(
                    definition.type
                );

            if (blockDefinition == null)
            {
                return;
            }

            bool exists =
                blockDefinition.Outputs.Any(
                    x =>
                        string.Equals(
                            x.Name,
                            reference.PortName,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            if (!exists)
            {
                errors.Add(
                    "Block '" +
                    reference.BlockId +
                    "' has no output port '" +
                    reference.PortName +
                    "'."
                );
            }
        }

        // ============================================================
        // Target port check
        // ============================================================

        private void ValidateTargetPort(
            BlockRecipe definition,
            PortReference reference,
            List<string> errors)
        {
            BlockDefinition blockDefinition =
                _gfx.Catalogue.Resolve(
                    definition.type
                );

            if (blockDefinition == null)
            {
                return;
            }

            bool exists =
                blockDefinition.Inputs.Any(
                    x =>
                        string.Equals(
                            x.Name,
                            reference.PortName,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            if (!exists)
            {
                errors.Add(
                    "Block '" +
                    reference.BlockId +
                    "' has no input port '" +
                    reference.PortName +
                    "'."
                );
            }
        }


        // ============================================================
        // NATIVE FULL-GRAPH VALIDATION
        // ============================================================

        private void ValidateNativeGraph(
            GfxRecipe recipe,
            List<string> errors)
        {
            Console.WriteLine(
                "Running native Distech graph validation..."
            );

            // --------------------------------------------------------
            // Completely separate temporary project.
            //
            // Nothing constructed here is saved.
            // --------------------------------------------------------

            GfxRuntime scratch =
                new GfxRuntime();

            Dictionary<string, Block> blocks =
                new Dictionary<string, Block>(
                    StringComparer.OrdinalIgnoreCase
                );

            // --------------------------------------------------------
            // Build every block in the scratch project
            // --------------------------------------------------------

            foreach (
                BlockRecipe definition
                in recipe.blocks)
            {
                try
                {
                    Block block =
                        scratch.CreateBlock(
                            definition.type,
                            definition.x,
                            definition.y
                        );

                    blocks.Add(
                        definition.id,
                        block
                    );

                    // ------------------------------------------------
                    // Apply native properties
                    // ------------------------------------------------

                    if (
                        definition.properties
                        != null)
                    {
                        foreach (
                            KeyValuePair<string, object> property
                            in definition.properties)
                        {
                            scratch.SetProperty(
                                block,
                                property.Key,
                                property.Value
                            );
                        }
                    }

                    // ------------------------------------------------
                    // Apply native input visibility
                    // ------------------------------------------------

                    if (
                        definition.visibleInputs
                        != null)
                    {
                        foreach (
                            string input
                            in definition.visibleInputs)
                        {
                            scratch.SetInputVisible(
                                block,
                                input,
                                true
                            );
                        }
                    }

                    // ------------------------------------------------
                    // Apply native output visibility
                    // ------------------------------------------------

                    if (
                        definition.visibleOutputs
                        != null)
                    {
                        foreach (
                            string output
                            in definition.visibleOutputs)
                        {
                            scratch.SetOutputVisible(
                                block,
                                output,
                                true
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(
                        "Native creation failed for block '" +
                        definition.id +
                        "': " +
                        GetDeepestMessage(ex)
                    );
                }
            }

            // --------------------------------------------------------
            // Don't attempt links if block creation failed.
            // --------------------------------------------------------

            if (errors.Count > 0)
            {
                return;
            }

            // --------------------------------------------------------
            // Build complete native link graph
            // --------------------------------------------------------

            if (recipe.links != null)
            {
                foreach (
                    LinkRecipe link
                    in recipe.links)
                {
                    PortReference from;
                    PortReference to;

                    if (
                        !TryParsePortReference(
                            link.from,
                            out from
                        ))
                    {
                        continue;
                    }

                    if (
                        !TryParsePortReference(
                            link.to,
                            out to
                        ))
                    {
                        continue;
                    }

                    Block source =
                        blocks[
                            from.BlockId
                        ];

                    Block target =
                        blocks[
                            to.BlockId
                        ];

                    try
                    {
                        scratch.Connect(
                            source,
                            from.PortName,
                            target,
                            to.PortName
                        );
                    }
                    catch (Exception ex)
                    {
                        errors.Add(
                            "Native link rejected: '" +
                            link.from +
                            "' -> '" +
                            link.to +
                            "'. " +
                            GetDeepestMessage(ex)
                        );
                    }
                }
            }

            // --------------------------------------------------------
            // Don't compile an invalid native graph.
            // --------------------------------------------------------

            if (errors.Count > 0)
            {
                return;
            }

            // --------------------------------------------------------
            // Run the REAL Distech compiler against the completed graph
            // --------------------------------------------------------

            Console.WriteLine(
                "Running native Distech compiler validation..."
            );

            List<string> compilerErrors =
                scratch.CompileWithDistech(
                    recipe.target
                );

            foreach (
                string compilerError
                in compilerErrors)
            {
                errors.Add(
                    "Distech compiler: " +
                    compilerError
                );
            }
        }

        // ============================================================
        // Conversion
        // ============================================================

        private bool CanConvertValue(
            object value,
            Type targetType)
        {
            try
            {
                if (value == null)
                {
                    return
                        !targetType.IsValueType ||
                        Nullable.GetUnderlyingType(
                            targetType
                        ) != null;
                }

                Type actualTarget =
                    Nullable.GetUnderlyingType(
                        targetType
                    )
                    ?? targetType;

                if (
                    actualTarget
                    .IsAssignableFrom(
                        value.GetType()
                    ))
                {
                    return true;
                }

                if (actualTarget.IsEnum)
                {
                    if (value is string)
                    {
                        Enum.Parse(
                            actualTarget,
                            value.ToString(),
                            true
                        );

                        return true;
                    }

                    Enum.ToObject(
                        actualTarget,
                        value
                    );

                    return true;
                }

                Convert.ChangeType(
                    value,
                    actualTarget
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // Helpers
        // ============================================================


        private bool TryParsePortReference(
            string value,
            out PortReference result)
        {
            result = null;

            if (
                string.IsNullOrWhiteSpace(
                    value
                ))
            {
                return false;
            }

            int separator =
                value.LastIndexOf('.');

            if (
                separator <= 0 ||
                separator >=
                value.Length - 1)
            {
                return false;
            }

            result =
                new PortReference
                {
                    BlockId =
                        value.Substring(
                            0,
                            separator
                        ),

                    PortName =
                        value.Substring(
                            separator + 1
                        )
                };

            return true;
        }

        private string GetDeepestMessage(
            Exception ex)
        {
            Exception current =
                ex;

            while (
                current.InnerException
                != null)
            {
                current =
                    current.InnerException;
            }

            return current.Message;
        }

        private class PortReference
        {
            public string BlockId
            {
                get;
                set;
            }

            public string PortName
            {
                get;
                set;
            }
        }
    }
}