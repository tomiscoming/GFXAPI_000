using Distech.Gpl.Model.Shapes.Blocks;
using GFX_BLOCK_CATALOGUE;
using System;
using System.Collections.Generic;

namespace GfxApi
{
    public class GfxRecipeRunner
    {
        private readonly GfxRuntime _gfx;

        private readonly Dictionary<string, Block> _blocks;

        public string target { get; set; }

        // ------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------

        public GfxRecipeRunner(
            GfxRuntime gfx)
        {
            if (gfx == null)
            {
                throw new ArgumentNullException(
                    "gfx"
                );
            }

            _gfx = gfx;

            _blocks =
                new Dictionary<string, Block>(
                    StringComparer.OrdinalIgnoreCase
                );
        }

        // ------------------------------------------------------------
        // Execute recipe
        // ------------------------------------------------------------

        public void Execute(
            GfxRecipe recipe)
        {
            if (recipe == null)
            {
                throw new ArgumentNullException(
                    "recipe"
                );
            }

            Console.WriteLine();
            Console.WriteLine(
                "Creating blocks..."
            );

            CreateBlocks(
                recipe
            );

            Console.WriteLine();
            Console.WriteLine(
                "Creating links..."
            );

            CreateLinks(
                recipe
            );

            Console.WriteLine();
            Console.WriteLine(
                "Saving project..."
            );

            _gfx.Save(
                recipe.output
            );

            Console.WriteLine();
            Console.WriteLine(
                "Saved:"
            );

            Console.WriteLine(
                recipe.output
            );
        }

        // ------------------------------------------------------------
        // Create blocks
        // ------------------------------------------------------------

        private void CreateBlocks(
            GfxRecipe recipe)
        {
            if (recipe.blocks == null)
            {
                return;
            }

            foreach (
                BlockRecipe definition
                in recipe.blocks)
            {
                ValidateBlockDefinition(
                    definition
                );

                Console.WriteLine(
                    "  " +
                    definition.id +
                    " : " +
                    definition.type
                );

                Block block =
                    _gfx.CreateBlock(
                        definition.type,
                        definition.x,
                        definition.y
                    );

                _blocks.Add(
                    definition.id,
                    block
                );

                ApplyProperties(
                    block,
                    definition
                );

                ApplyPortVisibility(
                    block,
                    definition
                );
            }
        }

        // ------------------------------------------------------------
        // Properties
        // ------------------------------------------------------------

        private void ApplyProperties(
            Block block,
            BlockRecipe definition)
        {
            if (
                definition.properties
                == null)
            {
                return;
            }

            foreach (
                KeyValuePair<string, object>
                property
                in definition.properties)
            {
                Console.WriteLine(
                    "      property " +
                    property.Key +
                    " = " +
                    property.Value
                );

                _gfx.SetProperty(
                    block,
                    property.Key,
                    property.Value
                );
            }
        }

        // ------------------------------------------------------------
        // Port visibility
        // ------------------------------------------------------------

        private void ApplyPortVisibility(
            Block block,
            BlockRecipe definition)
        {
            if (
                definition.visibleInputs
                != null)
            {
                foreach (
                    string portName
                    in definition.visibleInputs)
                {
                    Console.WriteLine(
                        "      expose input " +
                        portName
                    );

                    _gfx.SetInputVisible(
                        block,
                        portName,
                        true
                    );
                }
            }

            if (
                definition.visibleOutputs
                != null)
            {
                foreach (
                    string portName
                    in definition.visibleOutputs)
                {
                    Console.WriteLine(
                        "      expose output " +
                        portName
                    );

                    _gfx.SetOutputVisible(
                        block,
                        portName,
                        true
                    );
                }
            }
        }

        // ------------------------------------------------------------
        // Create links
        // ------------------------------------------------------------

        private void CreateLinks(
            GfxRecipe recipe)
        {
            if (recipe.links == null)
            {
                return;
            }

            foreach (
                LinkRecipe link
                in recipe.links)
            {
                if (link == null)
                {
                    throw new InvalidOperationException(
                        "Recipe contains a null link."
                    );
                }

                PortReference from =
                    ParsePortReference(
                        link.from
                    );

                PortReference to =
                    ParsePortReference(
                        link.to
                    );

                Block sourceBlock =
                    GetBlock(
                        from.BlockId
                    );

                Block targetBlock =
                    GetBlock(
                        to.BlockId
                    );

                Console.WriteLine(
                    "  " +
                    link.from +
                    " -> " +
                    link.to
                );

                _gfx.Connect(
                    sourceBlock,
                    from.PortName,
                    targetBlock,
                    to.PortName
                );
            }
        }

        // ------------------------------------------------------------
        // Block validation
        // ------------------------------------------------------------

        private void ValidateBlockDefinition(
            BlockRecipe definition)
        {
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Recipe contains a null block definition."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    definition.id
                ))
            {
                throw new InvalidOperationException(
                    "Block is missing an id."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    definition.type
                ))
            {
                throw new InvalidOperationException(
                    "Block '" +
                    definition.id +
                    "' is missing a type."
                );
            }

            if (
                _blocks.ContainsKey(
                    definition.id
                ))
            {
                throw new InvalidOperationException(
                    "Duplicate block id: " +
                    definition.id
                );
            }
        }

        // ------------------------------------------------------------
        // Block lookup
        // ------------------------------------------------------------

        private Block GetBlock(
            string id)
        {
            Block block;

            if (
                !_blocks.TryGetValue(
                    id,
                    out block
                ))
            {
                throw new InvalidOperationException(
                    "Unknown block id: " +
                    id
                );
            }

            return block;
        }

        // ------------------------------------------------------------
        // Port reference parsing
        // ------------------------------------------------------------

        private PortReference ParsePortReference(
            string value)
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                ))
            {
                throw new InvalidOperationException(
                    "Invalid empty port reference."
                );
            }

            int separator =
                value.LastIndexOf('.');

            if (
                separator <= 0 ||
                separator >=
                value.Length - 1)
            {
                throw new InvalidOperationException(
                    "Invalid port reference '" +
                    value +
                    "'. Expected format blockId.PortName"
                );
            }

            return new PortReference
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
        }


        // ------------------------------------------------------------
        // Internal port reference model
        // ------------------------------------------------------------

        private class PortReference
        {
            public string BlockId { get; set; }

            public string PortName { get; set; }
        }
    }
}