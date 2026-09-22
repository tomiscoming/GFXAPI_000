using GFX_BLOCK_CATALOGUE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace GfxApi
{
    internal class Program
    {
        static void Main(
            string[] args)
        {
            Console.WriteLine();

            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "STARTING RECIPE PIPELINE"
            );

            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine();

            try
            {
                // ====================================================
                // Locate recipe
                // ====================================================

                string recipePath =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "recipe.json"
                    );

                Console.WriteLine(
                    "Loading recipe:"
                );

                Console.WriteLine(
                    recipePath
                );

                // ====================================================
                // Load recipe JSON
                // ====================================================

                string json =
                    File.ReadAllText(
                        recipePath
                    );

                JavaScriptSerializer serializer =
                    new JavaScriptSerializer();

                serializer.MaxJsonLength =
                    int.MaxValue;

                GfxRecipe recipe =
                    serializer.Deserialize<GfxRecipe>(
                        json
                    );

                if (recipe == null)
                {
                    throw new InvalidOperationException(
                        "Recipe JSON could not be deserialized."
                    );
                }

                // ====================================================
                // Create runtime
                // ====================================================

                GfxRuntime gfx =
                    new GfxRuntime();

                // ====================================================
                // Validate recipe
                // ====================================================

                Console.WriteLine();

                Console.WriteLine(
                    "Target:"
                );

                Console.WriteLine(
                    "  " +
                    (
                        recipe.target ??
                        "<not specified>"
                    )
                );

                Console.WriteLine();

                Console.WriteLine(
                    "Validating recipe..."
                );

                RecipeValidator validator =
                    new RecipeValidator(
                        gfx
                    );

                List<string> errors =
                    validator.Validate(
                        recipe
                    );

                // ====================================================
                // Stop on validation failure
                // ====================================================

                if (errors.Count > 0)
                {
                    Console.WriteLine();

                    Console.WriteLine(
                        "VALIDATION FAILED"
                    );

                    Console.WriteLine();

                    foreach (
                        string error
                        in errors)
                    {
                        Console.WriteLine(
                            "  ERROR: " +
                            error
                        );
                    }

                    Console.WriteLine();

                    Console.WriteLine(
                        errors.Count +
                        " validation error(s)."
                    );

                    Console.WriteLine();

                    Console.WriteLine(
                        "Project was NOT generated."
                    );

                    Console.WriteLine();

                    Console.WriteLine(
                        "Press any key to exit."
                    );

                    Console.ReadKey();

                    return;
                }

                Console.WriteLine(
                    "Validation passed."
                );

                // ====================================================
                // Execute recipe
                // ====================================================

                Console.WriteLine();

                Console.WriteLine(
                    "Executing recipe..."
                );

                GfxRecipeRunner runner =
                    new GfxRecipeRunner(
                        gfx
                    );

                runner.Execute(
                    recipe
                );

                // ====================================================
                // Complete
                // ====================================================

                Console.WriteLine();

                Console.WriteLine(
                    "Recipe completed successfully."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine();

                Console.WriteLine(
                    "FAILED"
                );

                Console.WriteLine();

                PrintException(
                    ex
                );
            }

            Console.WriteLine();

            Console.WriteLine(
                "Press any key to exit."
            );

            Console.ReadKey();
        }

        // ============================================================
        // Exception reporting
        // ============================================================

        private static void PrintException(
            Exception ex)
        {
            int level =
                0;

            while (ex != null)
            {
                Console.WriteLine(
                    new string(
                        ' ',
                        level * 2
                    ) +
                    ex.GetType().Name +
                    ": " +
                    ex.Message
                );

                ex =
                    ex.InnerException;

                level++;
            }
        }
    }
}