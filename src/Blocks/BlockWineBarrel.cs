using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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
                case WineMakingState.Unprepared:
                case WineMakingState.Prepared:
                default:
                    barrelshape = "open";
                    break;
            }
            Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, "finevintage:shapes/block/winebarrel/" + barrelshape + ".json");
            capi.Tesselator.TesselateShape(this, shape, out MeshData containerMesh);

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
                    containerMesh.CustomInts = new CustomMeshDataPartInt(containerMesh.FlagsCount)
                    {
                        Count = containerMesh.FlagsCount
                    };
                    containerMesh.CustomInts.Values.Fill(0x4000000); // light foam only

                    containerMesh.CustomFloats = new CustomMeshDataPartFloat(containerMesh.FlagsCount * 2)
                    {
                        Count = containerMesh.FlagsCount * 2
                    };
                }
            }

            return containerMesh;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var entityBarrel = world.BlockAccessor.GetBlockEntity<BlockEntityWineBarrel>(blockSel.Position);
            switch (entityBarrel.State)
            {
                case WineMakingState.Unprepared:
                    return tryAddCandle(entityBarrel, byPlayer);
                case WineMakingState.WithCandle:
                    entityBarrel.State = (WineMakingState)(((int)entityBarrel.State + 1) % Enum.GetValues(typeof(WineMakingState)).Length);
                    entityBarrel.SealedAt = world.Calendar.ElapsedHours;
                    entityBarrel.MarkDirty(true);
                    return true;
                case WineMakingState.Preparing:
                    return true;
                case WineMakingState.Prepared:
                    if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty && !entityBarrel.Inventory[0].Empty)
                    {
                        var itemstack = entityBarrel.Inventory[0].Itemstack;
                        entityBarrel.State = (WineMakingState)(((int)entityBarrel.State + 1) % Enum.GetValues(typeof(WineMakingState)).Length);
                        entityBarrel.SealedAt = world.Calendar.ElapsedDays;
                        entityBarrel.StackSize = itemstack.StackSize;
                        entityBarrel.SourceLiquid = itemstack.Collectible.Code.Domain + ":" + itemstack.Collectible.Code.Path;
                        entityBarrel.TargetLiquid = "game:ciderportion-mead";
                        entityBarrel.ExponentialBasis = 0.8;
                        entityBarrel.MarkDirty(true);
                        return true;
                    }
                    else
                    {
                        return base.OnBlockInteractStart(world, byPlayer, blockSel);
                    }
                case WineMakingState.Sealed:
                    entityBarrel.TryOpen();
                    return true;
                case WineMakingState.UnSealed:
                    bool interaction = base.OnBlockInteractStart(world, byPlayer, blockSel);
                    if (entityBarrel.Inventory.Empty)
                    {
                        entityBarrel.State = (WineMakingState)(((int)entityBarrel.State + 1) % Enum.GetValues(typeof(WineMakingState)).Length);
                        entityBarrel.MarkDirty(true);
                    }
                    return interaction;
                default:
                    return true;
            }
        }

        private void checkContentRecipe(ItemStack inputStack)
        {
            RipeningRecipe recipe;
        }

        private bool tryAddCandle(BlockEntityWineBarrel barrel, IPlayer byPlayer)
        {
            var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack?.Item is ItemCandle)
            {
                barrel.State = (WineMakingState)(((int)barrel.State + 1) % Enum.GetValues(typeof(WineMakingState)).Length);
                barrel.MarkDirty(true);
                slot.TakeOut(1);
            }
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var entityBarrel = world.BlockAccessor.GetBlockEntity<BlockEntityWineBarrel>(selection.Position);
            switch (entityBarrel?.State)
            {
                case WineMakingState.Unprepared:
                    return new WorldInteraction[]{
                            new WorldInteraction(){
                                Itemstacks = new ItemStack[]{new ItemStack(world.GetItem(new AssetLocation("candle")))},
                                MouseButton = EnumMouseButton.Right,
                                ActionLangCode = "finevintage:winebarrel-add-candle"
                            }
                        };
                case WineMakingState.WithCandle:
                    return new WorldInteraction[]{
                            new WorldInteraction(){
                                RequireFreeHand = true,
                                MouseButton = EnumMouseButton.Right,
                                ActionLangCode = "finevintage:winebarrel-close-with-candle"
                            }
                        };
                case WineMakingState.Preparing:
                    return new WorldInteraction[0];
                case WineMakingState.Prepared:
                    var interactions = new List<WorldInteraction>(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                    if(!entityBarrel.Inventory.Empty){
                        interactions.Add(new WorldInteraction(){
                                RequireFreeHand = true,
                                MouseButton = EnumMouseButton.Right,
                                ActionLangCode = "finevintage:winebarrel-seal"
                            });
                    }
                    return interactions.ToArray();
                case WineMakingState.Sealed:
                    if (entityBarrel.CanOpen())
                    {
                        return new WorldInteraction[]{
                            new WorldInteraction(){
                                RequireFreeHand = true,
                                MouseButton = EnumMouseButton.Right,
                                ActionLangCode = "finevintage:barrel-open"
                            }
                        };
                    }
                    return new WorldInteraction[0];
                case WineMakingState.UnSealed:
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
                default:
                    return new WorldInteraction[0];
            }
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            var entityBarrel = world.BlockAccessor.GetBlockEntity<BlockEntityWineBarrel>(pos);
            switch (entityBarrel?.State)
            {
                case WineMakingState.Preparing:
                    double timeLeft = Math.Max(15 + entityBarrel.SealedAt - world.Calendar.ElapsedHours, 0);
                    int hoursLeft = (int)timeLeft;
                    int minutesLeft = (int)((timeLeft - hoursLeft) * 60);
                    return Lang.Get("finevintage:candle-sealed", hoursLeft, minutesLeft);
                case WineMakingState.Sealed:
                    if (entityBarrel.CanOpen())
                    {
                        return Lang.Get("finevintage:wine-sealed-ripening", entityBarrel.CalculateRipeness());
                    }
                    return Lang.Get("finevintage:wine-sealed-transforming", (long)entityBarrel.DaysLeft()) + "\n" + Lang.Get("finevintage:wine-sealed-ripening", entityBarrel.CalculateRipeness());
                default:
                    return base.GetPlacedBlockInfo(world, pos, forPlayer);
            }
        }
    }

    public enum WineMakingState
    {
        Unprepared, WithCandle, Preparing, Prepared, Sealed, UnSealed
    }
}