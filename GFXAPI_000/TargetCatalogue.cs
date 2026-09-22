using System;
using System.Collections.Generic;

namespace GFX_BLOCK_CATALOGUE
{
    public class TargetCatalogue
    {
        private readonly Dictionary<string, TargetDefinition>
            _targets;

        public TargetCatalogue()
        {
            _targets =
                new Dictionary<string, TargetDefinition>(
                    StringComparer.OrdinalIgnoreCase
                );

            RegisterDefaults();
        }

        public IEnumerable<TargetDefinition> Targets
        {
            get
            {
                return _targets.Values;
            }
        }

        public TargetDefinition Resolve(
            string key)
        {
            if (
                string.IsNullOrWhiteSpace(
                    key
                ))
            {
                return null;
            }

            TargetDefinition target;

            if (
                _targets.TryGetValue(
                    key,
                    out target
                ))
            {
                return target;
            }

            return null;
        }

        private void RegisterDefaults()
        {
            Add(
                new TargetDefinition
                {
                    Key =
                        "ECY-S1000",

                    DisplayName =
                        "ECY-S1000",

                    DeviceModelName =
                        "ECY-S1000",

                    DeviceModelType =
                        "10016C000502040D",

                    PlatformFamily =
                        "IP",

                    IsDefault =
                        true
                }
            );
        }

        private void Add(
            TargetDefinition target)
        {
            if (
                target == null ||
                string.IsNullOrWhiteSpace(
                    target.Key
                ))
            {
                return;
            }

            _targets[
                target.Key
            ] =
                target;
        }
    }
}