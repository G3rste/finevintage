using System;
using System.ComponentModel;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.GameContent
{
    // this should be extending ItemLiquidPortion, but the class is internal in vanilla
    public class WinePortion : Item
    {
        public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            var props = base.GetNutritionProperties(world, itemstack, forEntity);
            if (itemstack == null || props == null)
            {
                return props;
            }
            props.Health *= unchecked((float)itemstack.Attributes.GetDouble("ripeness"));
            return props;
        }
        public static FoodNutritionProperties GetNutritionPropertiesPerLitre(ItemStack itemstack)
        {
            var props = BlockLiquidContainerBase.GetContainableProps(itemstack)?.NutritionPropsPerLitre;
            if (itemstack == null || props == null)
            {
                return props;
            }
            float ripeness = unchecked((float)itemstack.Attributes.GetDouble("ripeness", 1));
            props.Health *= ripeness;
            props.Satiety *= ripeness;
            return props;
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            entityItem.Die(EnumDespawnReason.Removed);

            if (entityItem.World.Side == EnumAppSide.Server)
            {
                WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(entityItem.Itemstack);
                float litres = (float)entityItem.Itemstack.StackSize / props.ItemsPerLitre;

                entityItem.World.SpawnCubeParticles(entityItem.SidedPos.XYZ, entityItem.Itemstack, 0.75f, (int)(litres * 2), 0.45f);
                entityItem.World.PlaySoundAt(new AssetLocation("sounds/environment/smallsplash"), (float)entityItem.SidedPos.X, (float)entityItem.SidedPos.Y, (float)entityItem.SidedPos.Z, null);
            }


            base.OnGroundIdle(entityItem);

        }

        public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe recipe, ItemSlot slot, double x, double y, double z, double size)
        {
            base.OnHandbookRecipeRender(capi, recipe, slot, x, y, z, size);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get("finevintage:wine-sealed-ripening", inSlot.Itemstack.Attributes.GetDouble("ripeness")));
        }
    }
}