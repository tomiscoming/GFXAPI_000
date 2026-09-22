using System.Collections.Generic;

namespace GfxApi
{
    public class GfxRecipe
    {
        public string target
        {
            get;
            set;
        }

        public string output
        {
            get;
            set;
        }

        public List<BlockRecipe> blocks
        {
            get;
            set;
        }

        public List<LinkRecipe> links
        {
            get;
            set;
        }
    }

    public class BlockRecipe
    {
        public string id { get; set; }

        public string type { get; set; }

        public int x { get; set; }

        public int y { get; set; }

        public Dictionary<string, object> properties { get; set; }

        public List<string> visibleInputs { get; set; }

        public List<string> visibleOutputs { get; set; }
    }

    public class LinkRecipe
    {
        public string from { get; set; }

        public string to { get; set; }
    }
}