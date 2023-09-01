using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using HarmonyLib;

namespace FineVintage
{

    public class FineVintage : ModSystem
    {
        Harmony harmony = new Harmony("oats.finevintage");
        public List<RipeningRecipe> RipeningRecipes = new List<RipeningRecipe>();
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockEntityClass("WineBarrel", typeof(BlockEntityWineBarrel));

            api.RegisterBlockClass("WineBarrel", typeof(BlockWineBarrel));

            api.RegisterItemClass("ItemWinePortion", typeof(WinePortion));

            RipeningRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<RipeningRecipe>>("ripeningrecipes").Recipes;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            
            WinePatch.Patch(harmony);
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            Dictionary<AssetLocation, RipeningRecipe> recipes = api.Assets.GetMany<RipeningRecipe>(api.Logger, "recipes/ripening");
            foreach(var recipe in recipes.Values){
                recipe.Resolve(api.World, "ripeningrecipes");
                RipeningRecipes.Add(recipe);
            }
        }
    }
}
