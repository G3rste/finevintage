using Vintagestory.API.Common;

namespace FineVintage{
    public class RipeningRecipeIngredient : IRecipeIngredient
    {
        public string Name => throw new System.NotImplementedException();

        public AssetLocation Code { get; set; }
    }
}