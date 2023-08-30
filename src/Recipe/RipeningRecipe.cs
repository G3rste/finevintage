using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace FineVintage
{
    public class RipeningRecipe : IByteSerializable, IRecipeBase<RipeningRecipe>
    {
        public double Base;
        public CraftingRecipeIngredient input;

        public RipeningRecipe()
        {
        }

        public AssetLocation Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IRecipeIngredient[] Ingredients => new IRecipeIngredient[]{input};

        public IRecipeOutput Output => throw new NotImplementedException();

        public RipeningRecipe Clone()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            throw new NotImplementedException();
        }

        public float GetRipeness(ICoreAPI api, double sealedAt)
        {
            double years = api.World.Calendar.ElapsedDays / api.World.Calendar.DaysPerYear - sealedAt;
            return unchecked(1 - (float)Math.Pow(Base, years));
        }

        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            throw new NotImplementedException();
        }

        public void ToBytes(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            throw new NotImplementedException();
        }
    }
}