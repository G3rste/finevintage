using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FineVintage
{
    public class BlockWineBarrel : BlockLiquidContainerBase
    {
        public override bool CanDrinkFrom => false;
        public override bool IsTopOpened => true;
        public override bool AllowHeldLiquidTransfer => false;
        public override float TransferSizeLitres => Attributes["transferSizeLitres"].AsInt(10);
        protected float liquidMaxYTranslate => Attributes["liquidMaxYTranslate"].AsFloat(1f);
        protected string contentShapeLoc => Attributes["opaqueContentShapeLoc"].AsString();
        protected string liquidContentShapeLoc => Attributes["liquidContentShapeLoc"].AsString();

        public MeshData GenMesh(ItemStack contentStack, WineMakingState state, BlockPos forBlockPos = null)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            string barrelshape;
            switch (state)
            {
                case WineMakingState.Unprepared:
                case WineMakingState.Prepared:
                    barrelshape = "open";
                    break;
                case WineMakingState.WithCandle:
                    barrelshape = "candle";
                    break;
                case WineMakingState.Preparing:
                case WineMakingState.Sealed:
                    barrelshape = "closed";
                    break;
                case WineMakingState.UnSealed:
                    barrelshape = "tap";
                    break;
                default:
                    barrelshape = "open";
                    break;
            }
            Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, "finevintage:shapes/block/winebarrel/" + barrelshape + ".json");
            MeshData containerMesh;
            capi.Tesselator.TesselateShape(this, shape, out containerMesh);

            if (contentStack != null)
            {
                WaterTightContainableProps props = GetContainableProps(contentStack);
                if (props == null)
                {
                    capi.World.Logger.Error("Contents ('{0}') has no liquid properties, contents of liquid container {1} will be invisible.", contentStack.GetName(), Code);
                    return containerMesh;
                }

                ContainerTextureSource contentSource = new ContainerTextureSource(capi, contentStack, props.Texture);

                var loc = props.IsOpaque ? contentShapeLoc : liquidContentShapeLoc;
                var contentShape = Vintagestory.API.Common.Shape.TryGet(capi, loc);

                MeshData contentMesh;
                capi.Tesselator.TesselateShape(GetType().Name, contentShape, out contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), props.GlowLevel);

                contentMesh.Translate(0, GameMath.Min(liquidMaxYTranslate, contentStack.StackSize / props.ItemsPerLitre * (liquidMaxYTranslate / capacityLitresFromAttributes)), 0);

                if (props.ClimateColorMap != null)
                {
                    int col;
                    if (forBlockPos != null)
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, false);
                    }
                    else
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, 196, 128, false);
                    }

                    byte[] rgba = ColorUtil.ToBGRABytes(col);

                    for (int i = 0; i < contentMesh.Rgba.Length; i++)
                    {
                        contentMesh.Rgba[i] = (byte)((contentMesh.Rgba[i] * rgba[i % 4]) / 255);
                    }
                }

                for (int i = 0; i < contentMesh.Flags.Length; i++)
                {
                    contentMesh.Flags[i] = contentMesh.Flags[i] & ~(1 << 12); // Remove water waving flag
                }

                containerMesh.AddMeshData(contentMesh);

                // Water flags
                if (forBlockPos != null)
                {
                    containerMesh.CustomInts = new CustomMeshDataPartInt(containerMesh.FlagsCount);
                    containerMesh.CustomInts.Count = containerMesh.FlagsCount;
                    containerMesh.CustomInts.Values.Fill(0x4000000); // light foam only

                    containerMesh.CustomFloats = new CustomMeshDataPartFloat(containerMesh.FlagsCount * 2);
                    containerMesh.CustomFloats.Count = containerMesh.FlagsCount * 2;
                }
            }

            return containerMesh;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var entityBarrel = world.BlockAccessor.GetBlockEntity<BlockEntityWineBarrel>(blockSel.Position);
            if (entityBarrel.state == WineMakingState.Prepared)
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }
            entityBarrel.state = (WineMakingState)(((int)entityBarrel.state + 1) % Enum.GetValues(typeof(WineMakingState)).Length);
            entityBarrel.MarkDirty(true);

            return true;
        }
    }

    public enum WineMakingState
    {
        Unprepared, WithCandle, Preparing, Prepared, Sealed, UnSealed
    }
}