using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace FineVintage{
    public class BlockEntityWineBarrel : BlockEntityLiquidContainer
    {
        public WineMakingState state = WineMakingState.Unprepared;
        public BlockEntityWineBarrel()
        {
            inventory = new InventoryGeneric(1, null, null);
        }

        public override string InventoryClassName => "winebarrel";

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            var barrelBlock = Api.World.BlockAccessor.GetBlock(Pos) as BlockWineBarrel;
            var mesh = barrelBlock.GenMesh(GetContent(), state, Pos);
            mesher.AddMeshData(mesh);
            return true;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("winestate", (int)state);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            state = (WineMakingState)tree.GetInt("winestate");
        }
    }
}