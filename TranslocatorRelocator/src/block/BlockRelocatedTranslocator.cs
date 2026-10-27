using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace TranslocatorRelocator.src.block
{
    public class BlockRelocatedTranslocator : Block
    {
        public SimpleParticleProperties idleParticles;

        public SimpleParticleProperties insideParticles;

        public SimpleParticleProperties teleportParticles;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            idleParticles = new SimpleParticleProperties(0.5f, 1f, ColorUtil.ToRgba(150, 34, 47, 44), new Vec3d(), new Vec3d(), new Vec3f(-0.1f, -0.1f, -0.1f), new Vec3f(0.1f, 0.1f, 0.1f), 1.5f, 0f, 0.5f, 0.75f, EnumParticleModel.Quad);
            idleParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f);
            idleParticles.AddPos.Set(1.0, 2.0, 1.0);
            idleParticles.addLifeLength = 0.5f;
            idleParticles.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 80f);
            insideParticles = new SimpleParticleProperties(0.5f, 1f, ColorUtil.ToRgba(150, 92, 111, 107), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, -0.2f, -0.2f), new Vec3f(0.2f, 0.2f, 0.2f), 1.5f, 0f, 0.5f, 0.75f, EnumParticleModel.Quad);
            insideParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f);
            insideParticles.AddPos.Set(1.0, 2.0, 1.0);
            insideParticles.addLifeLength = 0.5f;
            teleportParticles = new SimpleParticleProperties(0.5f, 1f, ColorUtil.ToRgba(150, 92, 111, 107), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, -0.2f, -0.2f), new Vec3f(0.2f, 0.2f, 0.2f), 4.5f, 0f, 0.5f, 0.75f, EnumParticleModel.Quad);
            teleportParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -1f);
            teleportParticles.AddPos.Set(1.0, 2.0, 1.0);
            teleportParticles.addLifeLength = 0.5f;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityRelocatedTranslocator blockEntity)
            {
                blockEntity.OnRightClick(byPlayer);
                return true;
            }
            return true;
        }

        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityRelocatedTranslocator blockEntity)
            {
                blockEntity.OnEntityCollide(entity);
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[2]
            {
                new WorldInteraction
                {
                    ActionLangCode = "translocatorrelocatorfork:block-interaction-toggle-travel-mode-text",
                    MouseButton = EnumMouseButton.Right
                },
                new WorldInteraction
                {
                    ActionLangCode = "translocatorrelocatorfork:block-interaction-edit-link-key-text",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityRelocatedTranslocator blockEntity)
            {
                blockEntity.GetBlockInfo(forPlayer, stringBuilder);
            }
            return stringBuilder.ToString().TrimEnd();
        }

        public override AssetLocation GetRotatedBlockCode(int angle)
        {
            int num = GameMath.Mod(BlockFacing.FromCode(LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
            BlockFacing blockFacing = BlockFacing.HORIZONTALS_ANGLEORDER[num];
            return CodeWithParts(blockFacing.Code);
        }

        public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
        {
            BlockFacing blockFacing = BlockFacing.FromCode(LastCodePart());
            if (blockFacing.Axis == axis)
            {
                return CodeWithParts(blockFacing.Opposite.Code);
            }

            return Code;
        }
    }
}
