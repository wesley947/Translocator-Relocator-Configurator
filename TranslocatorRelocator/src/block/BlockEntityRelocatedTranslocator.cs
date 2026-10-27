using System;
using System.Text;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using TranslocatorRelocator.src.gui;


namespace TranslocatorRelocator.src.block
{
    public class BlockEntityRelocatedTranslocator : BlockEntityTeleporterBase
    {
        public BlockPos tpLocation;

        private BlockRelocatedTranslocator ownBlock;

        private Vec3d posvec;

        private ICoreServerAPI sapi;

        private string linkKey = "";

        private bool isLinked;

        private bool travelHome;
        private bool homeDesignationVisual;
        private GuiDialogHomeReplaceConfirm homeConfirmDialog;
        private GuiDialogNetworkRenameConfirm networkRenameConfirmDialog;

        private BlockPos[] links;

        private GuiDialogLinkKeyInput guiDialogLinkKeyInput;

        public ILoadedSound translocatingSound;

        private float particleAngle;

        private float translocVolume;

        private float translocPitch;

        private long somebodyIsTeleportingReceivedTotalMs;

        public BlockPos TargetLocation => tpLocation;

        private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

        public override Vec3d GetTarget(Entity forEntity)
        {
            BlockPos destPos = null;
            if (travelHome && Api.Side == EnumAppSide.Server)
            {
                var home = Core.homeManager?.GetHome(linkKey);
                if (home != null && !home.Equals(Pos) && Array.Exists(links, p => p.Equals(home)))
                {
                    destPos = home;
                }
            }
            if (destPos == null)
            {
                Random random = new Random();
                destPos = links[random.Next(links.Length)];
            }

            Block destBlock = Api.World.BlockAccessor.GetBlock(destPos);
            BlockFacing facing = BlockFacing.FromCode(destBlock?.Variant?["side"])?.Opposite ?? BlockFacing.NORTH;

            return destPos.ToVec3d().Add(0.5 + facing.Normali.X, 1.0, 0.5 + facing.Normali.Z);
        }

        public BlockEntityRelocatedTranslocator()
        {
            TeleportWarmupSec = 3f;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            setupGameTickers();

            ownBlock = base.Block as BlockRelocatedTranslocator;
            posvec = new Vec3d((double)Pos.X + 0.5, Pos.Y, (double)Pos.Z + 0.5);
            if (api.World.Side == EnumAppSide.Client)
            {
                float rotateY = base.Block.Shape.rotateY;
                animUtil.InitializeAnimator("translocator", null, null, new Vec3f(0f, rotateY, 0f));
                updateSoundState();
            }
        }

        public void updateSoundState()
        {
            if (translocVolume > 0f)
            {
                startSound();
            }
            else
            {
                stopSound();
            }
        }

        public void startSound()
        {
            if (translocatingSound == null)
            {
                ICoreAPI api = Api;
                if (api != null && api.Side == EnumAppSide.Client)
                {
                    translocatingSound = (Api as ICoreClientAPI).World.LoadSound(new SoundParams
                    {
                        Location = new AssetLocation("sounds/effect/translocate-active.ogg"),
                        ShouldLoop = true,
                        Position = Pos.ToVec3f(),
                        RelativePosition = false,
                        DisposeOnFinish = false,
                        Volume = 0.5f
                    });
                    translocatingSound.Start();
                }
            }
        }

        public void stopSound()
        {
            if (translocatingSound != null)
            {
                translocatingSound.Stop();
                translocatingSound.Dispose();
                translocatingSound = null;
            }
        }

        public void setupGameTickers()
        {
            if (Api.Side == EnumAppSide.Server)
            {
                sapi = Api as ICoreServerAPI;
                RegisterGameTickListener(OnServerGameTick250ms, 250);
                RegisterGameTickListener(OnServerGameTick1s, 1000);
            }
            else
            {
                RegisterGameTickListener(OnClientGameTick, 50);
            }
        }

        public override void OnEntityCollide(Entity entity)
        {
            if (isLinked)
            {
                base.OnEntityCollide(entity);
            }
        }

        private void OnClientGameTick(float dt)
        {
            if (ownBlock == null || Api?.World == null || !isLinked)
            {
                return;
            }

            if (Api.World.ElapsedMilliseconds - somebodyIsTeleportingReceivedTotalMs > 6000)
            {
                somebodyIsTeleporting = false;
            }

            HandleSoundClient(dt);
            int num;
            int num2;
            if (Api.World.ElapsedMilliseconds > 100)
            {
                num = ((Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs < 100) ? 1 : 0);
                if (num != 0)
                {
                    num2 = 1;
                    goto IL_009a;
                }
            }
            else
            {
                num = 0;
            }

            num2 = (somebodyIsTeleporting ? 1 : 0);
            goto IL_009a;
        IL_009a:
            bool flag = (byte)num2 != 0;
            bool flag2 = animUtil.activeAnimationsByAnimCode.ContainsKey("teleport");
            if (num == 0 && flag)
            {
                manager.lastTranslocateCollideMsOtherPlayer = Api.World.ElapsedMilliseconds;
            }

            SimpleParticleProperties simpleParticleProperties = (flag2 ? ownBlock.insideParticles : ownBlock.idleParticles);
            if (flag)
            {
                AnimationMetaData meta = new AnimationMetaData
                {
                    Animation = "teleport",
                    Code = "teleport",
                    AnimationSpeed = 1f,
                    EaseInSpeed = 1f,
                    EaseOutSpeed = 2f,
                    Weight = 1f,
                    BlendMode = EnumAnimationBlendMode.Add
                };
                animUtil.StartAnimation(meta);
                animUtil.StartAnimation(new AnimationMetaData
                {
                    Animation = "idle",
                    Code = "idle",
                    AnimationSpeed = 1f,
                    EaseInSpeed = 1f,
                    EaseOutSpeed = 1f,
                    Weight = 1f,
                    BlendMode = EnumAnimationBlendMode.Average
                });
            }
            else
            {
                animUtil.StopAnimation("teleport");
            }

            if (animUtil.activeAnimationsByAnimCode.Count > 0 && Api.World.ElapsedMilliseconds - lastOwnPlayerCollideMs > 10000 && Api.World.ElapsedMilliseconds - manager.lastTranslocateCollideMsOtherPlayer > 10000)
            {
                animUtil.StopAnimation("idle");
            }

            // Normal retains original green/opacity. Home and Go Home are more saturated
            // and less translucent so that red and purple remain distinct in-game.
            int num3 = homeDesignationVisual ? 170 : (travelHome ? 255 : 53);
            int num4 = homeDesignationVisual ? 112 : (travelHome ? 100 : 221);
            int num5 = homeDesignationVisual ? 255 : (travelHome ? 100 : 172);
            int particleAlpha = (homeDesignationVisual || travelHome) ? 0x70000000 : 0x32000000;
            simpleParticleProperties.Color = (num3 << 16) | (num4 << 8) | num5 | particleAlpha;
            simpleParticleProperties.AddPos.Set(0.0, 0.0, 0.0);
            simpleParticleProperties.BlueEvolve = EvolvingNatFloat.NoValueSet;
            simpleParticleProperties.RedEvolve = EvolvingNatFloat.NoValueSet;
            simpleParticleProperties.GreenEvolve = EvolvingNatFloat.NoValueSet;
            simpleParticleProperties.MinSize = 0.1f;
            simpleParticleProperties.MaxSize = 0.2f;
            simpleParticleProperties.SizeEvolve = EvolvingNatFloat.NoValueSet;
            simpleParticleProperties.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 100f);
            bool flag3 = base.Block.Variant["side"] == "east" || base.Block.Variant["side"] == "west";
            particleAngle = (flag2 ? (particleAngle + 5f * dt) : 0f);
            double num6 = GameMath.Cos(particleAngle + (flag3 ? (MathF.PI / 2f) : 0f)) * 0.35f;
            double num7 = 1.9 + Api.World.Rand.NextDouble() * 0.2;
            double num8 = GameMath.Sin(particleAngle + (flag3 ? (MathF.PI / 2f) : 0f)) * 0.35f;
            simpleParticleProperties.LifeLength = GameMath.Sqrt(num6 * num6 + num8 * num8) / 10f;
            simpleParticleProperties.MinPos.Set(posvec.X + num6, posvec.Y + num7, posvec.Z + num8);
            simpleParticleProperties.MinVelocity.Set((0f - (float)num6) / 2f, -1f - (float)Api.World.Rand.NextDouble() / 2f, (0f - (float)num8) / 2f);
            simpleParticleProperties.MinQuantity = (flag2 ? 3f : 0.25f);
            simpleParticleProperties.AddVelocity.Set(0f, 0f, 0f);
            simpleParticleProperties.AddQuantity = 0.5f;
            Api.World.SpawnParticles(simpleParticleProperties);
            simpleParticleProperties.MinPos.Set(posvec.X - num6, posvec.Y + num7, posvec.Z - num8);
            simpleParticleProperties.MinVelocity.Set((float)num6 / 2f, -1f - (float)Api.World.Rand.NextDouble() / 2f, (float)num8 / 2f);
            Api.World.SpawnParticles(simpleParticleProperties);
        }

        protected virtual void HandleSoundClient(float dt)
        {
            ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
            bool flag = coreClientAPI.World.ElapsedMilliseconds - lastOwnPlayerCollideMs <= 200;
            bool flag2 = coreClientAPI.World.ElapsedMilliseconds - lastEntityCollideMs <= 200;
            if (flag || flag2)
            {
                translocVolume = Math.Min(0.5f, translocVolume + dt / 3f);
                translocPitch = Math.Min(translocPitch + dt / 3f, 2.5f);
                if (flag)
                {
                    coreClientAPI.World.AddCameraShake(0.0575f);
                }
            }
            else
            {
                translocVolume = Math.Max(0f, translocVolume - 2f * dt);
                translocPitch = Math.Max(translocPitch - dt, 0.5f);
            }

            updateSoundState();
            if (translocVolume > 0f)
            {
                translocatingSound.SetVolume(translocVolume);
                translocatingSound.SetPitch(translocPitch);
            }
        }

        private void OnServerGameTick250ms(float dt)
        {
            ReconcileNetworkName();
            if (linkKey.Length > 0 && isLinked)
            {
                try
                {
                    HandleTeleportingServer(dt);
                }
                catch (Exception e)
                {
                    Api.Logger.Warning("Exception when ticking Relocated Translocator at {0}", Pos);
                    Api.Logger.Error(e);
                }
            }
        }

        private void OnServerGameTick1s(float dt)
        {
            ReconcileNetworkName();
            CheckLinks();
            if (Core.homeManager == null) return;
            bool isNetworkHome = Core.homeManager.IsHome(Pos, linkKey);
            bool change = homeDesignationVisual != isNetworkHome;
            homeDesignationVisual = isNetworkHome;
            if (travelHome && (isNetworkHome || Core.homeManager.GetHome(linkKey) == null))
            {
                travelHome = false;
                change = true;
            }
            if (change) MarkDirty();
        }

        // rtData is authoritative for bulk renames; this handles even unloaded locations.
        private void ReconcileNetworkName()
        {
            if (Api?.Side != EnumAppSide.Server || Core.linkManager?.rtData == null) return;
            if (!Core.linkManager.rtData.TryGetValue(Pos, out string registeredKey)) return;
            if (registeredKey == linkKey) return;
            linkKey = registeredKey ?? "";
            homeDesignationVisual = Core.homeManager?.IsHome(Pos, linkKey) == true;
            if (travelHome && (homeDesignationVisual || Core.homeManager?.GetHome(linkKey) == null))
                travelHome = false;
            MarkDirty();
            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
        }

        public long MapRegionIndex2D(int regionX, int regionZ)
        {
            return (long)regionZ * (long)(sapi.WorldManager.MapSizeX / sapi.WorldManager.RegionSize) + regionX;
        }

        protected override void didTeleport(Entity entity)
        {
            if (entity is EntityPlayer)
            {
                manager.DidTranslocateServer((entity as EntityPlayer).Player as IServerPlayer);
            }

            ownBlock.teleportParticles.MinPos.Set(Pos.X, Pos.Y, Pos.Z);
            ownBlock.teleportParticles.AddPos.Set(1.0, 1.8, 1.0);
            ownBlock.teleportParticles.MinVelocity.Set(-1f, -1f, -1f);
            ownBlock.teleportParticles.AddVelocity.Set(2f, 2f, 2f);
            ownBlock.teleportParticles.MinQuantity = 150f;
            ownBlock.teleportParticles.AddQuantity = 0.5f;
            int num = homeDesignationVisual ? 170 : (travelHome ? 255 : 53);
            int num2 = homeDesignationVisual ? 112 : (travelHome ? 100 : 221);
            int num3 = homeDesignationVisual ? 255 : (travelHome ? 100 : 172);
            int burstAlpha = (homeDesignationVisual || travelHome) ? 0x7F000000 : 0x64000000;
            ownBlock.teleportParticles.Color = (num << 16) | (num2 << 8) | num3 | burstAlpha;
            ownBlock.teleportParticles.BlueEvolve = EvolvingNatFloat.NoValueSet;
            ownBlock.teleportParticles.RedEvolve = EvolvingNatFloat.NoValueSet;
            ownBlock.teleportParticles.GreenEvolve = EvolvingNatFloat.NoValueSet;
            ownBlock.teleportParticles.MinSize = 0.1f;
            ownBlock.teleportParticles.MaxSize = 0.2f;
            ownBlock.teleportParticles.SizeEvolve = EvolvingNatFloat.NoValueSet;
            ownBlock.teleportParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -10f);
            Api.World.SpawnParticles(ownBlock.teleportParticles);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (linkKey.Length > 0)
            {
                dsc.AppendLine(Lang.Get("translocatorrelocatorfork:block-info-link-key-text", linkKey));
                if (isLinked)
                {
                    dsc.AppendLine(Lang.Get("translocatorrelocatorfork:block-info-linked-status"));
                }
            }
            dsc.AppendLine("Travel mode: " + (travelHome ? "HOME" : "RANDOM"));
            if (homeDesignationVisual) dsc.AppendLine("Network Home");
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (Api.Side == EnumAppSide.Server)
            {
                Core.linkManager.AddOrSetKey(Pos);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (Api.Side == EnumAppSide.Server)
            {
                Core.linkManager.RemoveKey(Pos);
                Core.homeManager?.RemoveAt(Pos);
            }

            translocatingSound?.Dispose();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            translocatingSound?.Dispose();
        }

        public void CheckLinks()
        {
            BlockPos[] newLinks = Core.linkManager.GetLinksForKey(Pos, linkKey);
            links = newLinks ?? Array.Empty<BlockPos>();
            if (isLinked != links.Length > 0)
            {
                isLinked = links.Length > 0;
                MarkDirty(redrawOnClient: true);
            }
        }

        private void HandleRenameRequest(IServerPlayer player, int packetid, byte[] data)
        {
            NetworkRenamePacket request = SerializerUtil.Deserialize<NetworkRenamePacket>(data);
            string newKey = (request?.NewKey ?? "").Trim();
            string oldKey = linkKey;
            if (request == null || request.OldKey != oldKey || !Core.homeManager.IsHome(Pos, oldKey))
            {
                Notify(player, "Only the designated Home can rename its current network. Reopen its configuration.");
                return;
            }
            if (newKey.Length == 0 || newKey.Length > 16 || newKey == oldKey)
            {
                Notify(player, "Enter a different network name (1-16 characters).");
                return;
            }
            if (Core.linkManager.NetworkExists(newKey))
            {
                Notify(player, "Cannot rename: a network named '" + newKey + "' already exists. Networks will not be merged.");
                return;
            }
            int count = Core.linkManager.CountForNetwork(oldKey);
            if (count < 1) { Notify(player, "Network has no registered translocators."); return; }

            if (packetid == 1004)
            {
                sapi.Network.SendBlockEntityPacket(player, Pos, 1005, SerializerUtil.Serialize(new NetworkRenamePacket
                {
                    OldKey = oldKey, NewKey = newKey, ExpectedCount = count
                }));
                return;
            }
            if (packetid != 1006) return;
            if (request.ExpectedCount != count)
            {
                Notify(player, "The network changed since you opened the confirmation. Please start the rename again.");
                return;
            }
            try
            {
                int renamed = Core.linkManager.RenameNetwork(oldKey, newKey);
                // The same Home block remains Home, now under the renamed network.
                Core.homeManager.SetHome(newKey, Pos);
                ReconcileNetworkName();
                CheckLinks();
                homeDesignationVisual = true;
                MarkDirty();
                Notify(player, "Renamed '" + oldKey + "' to '" + newKey + "' (" + renamed + " translocators). Home preserved.");
            }
            catch (System.Exception ex)
            {
                Api.Logger.Error(ex);
                Notify(player, "Network rename failed. Please check the game log before retrying.");
            }
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (Api.Side != EnumAppSide.Server || player is not IServerPlayer serverPlayer) return;
            if (packetid == 1004 || packetid == 1006)
            {
                HandleRenameRequest(serverPlayer, packetid, data);
                return;
            }
            if (packetid != 1002) return;

            EditLinkKeyPacket request = SerializerUtil.Deserialize<EditLinkKeyPacket>(data);
            string newKey = (request?.LinkKey ?? "").Trim();
            if (newKey.Length > 16)
            {
                Notify(serverPlayer, "Network name must be 16 characters or fewer.");
                return;
            }
            if (request.IsHome && newKey.Length == 0)
            {
                Notify(serverPlayer, "Name the network before setting its Home.");
                return;
            }

            BlockPos existingHome = Core.homeManager.GetHome(newKey);
            if (request.IsHome && existingHome != null && !existingHome.Equals(Pos) &&
                (!request.ReplaceHome || request.ExpectedHomePos != existingHome.ToString()))
            {
                var conflict = new EditLinkKeyPacket
                {
                    LinkKey = newKey,
                    IsHome = true,
                    ExpectedHomePos = existingHome.ToString()
                };
                sapi.Network.SendBlockEntityPacket(serverPlayer, Pos, 1003, SerializerUtil.Serialize(conflict));
                return;
            }

            bool wasHome = Core.homeManager.IsHome(Pos, linkKey);
            if (linkKey != newKey)
            {
                Core.homeManager.RemoveAt(Pos);
                linkKey = newKey;
                Core.linkManager.AddOrSetKey(Pos, linkKey);
            }
            if (request.IsHome)
            {
                Core.homeManager.SetHome(linkKey, Pos);
                travelHome = false;
            }
            else if (wasHome)
            {
                Core.homeManager.RemoveAt(Pos);
            }

            homeDesignationVisual = Core.homeManager.IsHome(Pos, linkKey);
            if (travelHome && Core.homeManager.GetHome(linkKey) == null) travelHome = false;
            MarkDirty();
            Notify(serverPlayer, "Network settings saved" + (homeDesignationVisual ? "; this portal is Home." : "."));
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == 1001)
            {
                if (guiDialogLinkKeyInput != null && guiDialogLinkKeyInput.IsOpened()) return;
                EditLinkKeyPacket current = SerializerUtil.Deserialize<EditLinkKeyPacket>(data);
                guiDialogLinkKeyInput = new GuiDialogLinkKeyInput(
                    Lang.Get("translocatorrelocatorfork:dialog-title-configurator"),
                    Pos, current.LinkKey ?? "", current.IsHome, current.NetworkNames, Api as ICoreClientAPI);
                guiDialogLinkKeyInput.TryOpen();
            }
            else if (packetid == 1003)
            {
                EditLinkKeyPacket existing = SerializerUtil.Deserialize<EditLinkKeyPacket>(data);
                if (homeConfirmDialog != null && homeConfirmDialog.IsOpened()) return;
                homeConfirmDialog = new GuiDialogHomeReplaceConfirm(Pos, existing.LinkKey,
                    existing.ExpectedHomePos, Api as ICoreClientAPI);
                homeConfirmDialog.TryOpen();
            }
            else if (packetid == 1005)
            {
                NetworkRenamePacket details = SerializerUtil.Deserialize<NetworkRenamePacket>(data);
                if (networkRenameConfirmDialog != null && networkRenameConfirmDialog.IsOpened()) return;
                networkRenameConfirmDialog = new GuiDialogNetworkRenameConfirm(Pos, details, Api as ICoreClientAPI);
                networkRenameConfirmDialog.TryOpen();
            }
        }

        public void OnRightClick(IPlayer byPlayer)
        {
            if (Api is not ICoreServerAPI coreServerAPI || byPlayer is not IServerPlayer serverPlayer) return;
            if (byPlayer.Entity?.Controls?.ShiftKey == true)
            {
                var current = new EditLinkKeyPacket
                {
                    LinkKey = linkKey,
                    IsHome = Core.homeManager.IsHome(Pos, linkKey),
                    NetworkNames = Core.linkManager.links.Keys
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()
                };
                coreServerAPI.Network.SendBlockEntityPacket(serverPlayer, Pos, 1001, SerializerUtil.Serialize(current));
                return;
            }

            if (string.IsNullOrEmpty(linkKey))
            {
                Notify(serverPlayer, "This translocator has no network key. Shift + Right-click to configure it.");
                return;
            }
            if (Core.homeManager.IsHome(Pos, linkKey))
            {
                if (travelHome) { travelHome = false; MarkDirty(); }
                Notify(serverPlayer, "This is the network Home. Travel mode: RANDOM.");
                return;
            }
            if (!travelHome && Core.homeManager.GetHome(linkKey) == null)
            {
                Notify(serverPlayer, "No Home is set for network '" + linkKey + "'. Travel mode: RANDOM.");
                return;
            }
            travelHome = !travelHome;
            MarkDirty();
            Notify(serverPlayer, "Network '" + linkKey + "' - travel mode: " + (travelHome ? "HOME" : "RANDOM"));
        }

        private static void Notify(IServerPlayer player, string message)
        {
            player.SendMessage(GlobalConstants.GeneralChatGroup, "[Translocator] " + message, EnumChatType.Notification);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            linkKey = tree.GetString("linkkey");
            isLinked = tree.GetBool("isLinked");
            travelHome = tree.GetBool("trfTravelHome");
            homeDesignationVisual = tree.GetBool("trfIsHomeVisual");

            if (worldAccessForResolve == null || worldAccessForResolve.Side != EnumAppSide.Client)
            {
                return;
            }

            somebodyIsTeleportingReceivedTotalMs = worldAccessForResolve.ElapsedMilliseconds;
            if (tree.GetBool("somebodyDidTeleport"))
            {
                worldAccessForResolve.Api.Event.EnqueueMainThreadTask(delegate
                {
                    worldAccessForResolve.PlaySoundAt(new AssetLocation("sounds/effect/translocate-breakdimension"), Pos, 0.0, null, randomizePitch: false, 16f);
                }, "playtelesound");
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("linkkey", linkKey);
            tree.SetBool("isLinked", isLinked);
            tree.SetBool("trfTravelHome", travelHome);
            tree.SetBool("trfIsHomeVisual", homeDesignationVisual);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (animUtil.activeAnimationsByAnimCode.Count > 0 || (animUtil.animator != null && animUtil.animator.ActiveAnimationCount > 0))
            {
                return true;
            }
            return false;
        }

        public string GetLinkKey()
        {
            return linkKey;
        }
    }
}
