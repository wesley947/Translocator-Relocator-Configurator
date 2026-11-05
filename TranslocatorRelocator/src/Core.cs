using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using TranslocatorRelocator.src.config;
using TranslocatorRelocator.src.manager;
using TranslocatorRelocator.src.block;
using HarmonyLib;
using Vintagestory.API.Client;

namespace TranslocatorRelocator.src
{
    public class Core : ModSystem
    {
        private static Harmony harmony;
        private static ICoreAPI api;
        private static Config config;
        public static RelocatedTranslocatorLinkManager linkManager;
        public static TranslocatorHomeManager homeManager;
        public static ParticleVisualConfig ParticleVisuals;
        private static ICoreClientAPI visualClientApi;

        public const string ServerPatchCategory = "server";
        public const string ClientPatchCategory = "client";

        const string ConfigFileName = "TranslocatorRelocator.json";

        public override void StartPre(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Server && loadConfig(api))
            {
                api.World.Config.SetInt("staticTranslocatorRequiredMiningTier", config.staticTranslocatorRequiredMiningTier);
            }
        }

        public override void Start(ICoreAPI api)
        {
            Core.api = api;

            harmony = new(Mod.Info.ModID);

            api.RegisterBlockClass("BlockRelocatedTranslocator", typeof(BlockRelocatedTranslocator));
            api.RegisterBlockEntityClass("RelocatedTranslocator", typeof(BlockEntityRelocatedTranslocator));
            api.RegisterBlockBehaviorClass("DefaultDropsHorizontalOrientable", typeof(BlockBehaviorDefaultDropsHorizontalOrientable));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            visualClientApi = api;
            try
            {
                ParticleVisuals = api.LoadModConfig<ParticleVisualConfig>(ParticleVisualConfig.FileName) ?? new ParticleVisualConfig();
                ParticleVisuals.Sanitize();
            }
            catch (Exception e)
            {
                Mod.Logger.Warning("Could not load particle preferences; using defaults: " + e.Message);
                ParticleVisuals = new ParticleVisualConfig();
            }
            harmony?.PatchCategory(ClientPatchCategory);
        }

        public static void SaveParticleVisuals()
        {
            if (visualClientApi == null || ParticleVisuals == null) return;
            try
            {
                ParticleVisuals.Sanitize();
                visualClientApi.StoreModConfig(ParticleVisuals, ParticleVisualConfig.FileName);
            }
            catch (Exception e)
            {
                visualClientApi.Logger.Warning("Could not save translocator particle preferences: " + e.Message);
            }
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            linkManager = new(sapi);

            sapi.Event.SaveGameLoaded += linkManager.Loading;
            sapi.Event.GameWorldSave += linkManager.Saving;

            homeManager = new TranslocatorHomeManager(sapi);
            sapi.Event.SaveGameLoaded += homeManager.Loading;
            sapi.Event.GameWorldSave += homeManager.Saving;

            harmony?.PatchCategory(ServerPatchCategory);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
            visualClientApi = null;
            ParticleVisuals = null;
        }

        private bool loadConfig(ICoreAPI api)
        {
            try
            {
                config = api.LoadModConfig<Config>(ConfigFileName);
                if (config == null)
                {
                    config = new Config();
                }
                api.StoreModConfig<Config>(config, ConfigFileName);
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Error trying to load Translocator Relocator config file.");
                Mod.Logger.Error(e);
                config = new Config();
            }
            return config != null;
        }

        public static void Print(string text)
        {
            api.Logger.Debug(text);
        }
    }
}
