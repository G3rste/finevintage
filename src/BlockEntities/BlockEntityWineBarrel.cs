using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace FineVintage
{
    public class BlockEntityWineBarrel : BlockEntityLiquidContainer
    {
        public WineMakingState State = WineMakingState.Unprepared;
        public double SealedAt;
        public int StackSize;
        public string SourceLiquid;
        public string TargetLiquid;
        public double ExponentialBasis;
        public BlockEntityWineBarrel()
        {
            inventory = new InventoryGeneric(1, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            api.World.RegisterGameTickListener(OnBarrelTick, 5000, 0);
        }

        private void OnBarrelTick(float obj)
        {
            if (State == WineMakingState.Preparing && SealedAt + 15 < Api.World.Calendar.ElapsedHours)
            {
                State = (WineMakingState)(((int)State + 1) % Enum.GetValues(typeof(WineMakingState)).Length);
                MarkDirty(true);
            }
        }

        public override string InventoryClassName => "winebarrel";

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            var barrelBlock = Api.World.BlockAccessor.GetBlock(Pos) as BlockWineBarrel;
            var mesh = barrelBlock.GenMesh(GetContent(), State, Pos);
            mesher.AddMeshData(mesh);
            return true;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("winestate", (int)State);
            tree.SetInt("stacksize", StackSize);
            tree.SetString("sourceliquid", SourceLiquid);
            tree.SetString("targetliquid", TargetLiquid);
            tree.SetDouble("exponentialbasis", ExponentialBasis);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            State = (WineMakingState)tree.GetInt("winestate");
            StackSize = tree.GetInt("stacksize");
            SourceLiquid = tree.GetString("sourceliquid");
            TargetLiquid = tree.GetString("targetliquid");
            ExponentialBasis = tree.GetDouble("exponentialbasis");
        }
        public void TryOpen()
        {
            if (CanOpen())
            {
                State = (WineMakingState)(((int)State + 1) % Enum.GetValues(typeof(WineMakingState)).Length);
                inventory[0].Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation(TargetLiquid)), StackSize);
                inventory[0].Itemstack.Attributes.SetDouble("ripeness", CalculateRipeness());
                MarkDirty(true);
            }
        }
        public bool CanOpen()
        {
            return DaysLeft() == 0;
        }

        public double DaysLeft(){
            return Math.Max(15 + SealedAt - Api.World.Calendar.ElapsedDays, 0);
        }
        public double CalculateRipeness()
        {
            double elapsedYears = (Api.World.Calendar.ElapsedDays - SealedAt) / Api.World.Calendar.DaysPerYear;
            return 1 - Math.Pow(ExponentialBasis, elapsedYears);
        }
    }
}