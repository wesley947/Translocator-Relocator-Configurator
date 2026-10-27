using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TranslocatorRelocator.src
{
    public class TranslocatorHomeManager
    {
        private const string SaveKey = "trfNetworkHomesV1";
        private readonly ICoreServerAPI sapi;
        private Dictionary<string, BlockPos> homes = new();

        public TranslocatorHomeManager(ICoreServerAPI sapi) { this.sapi = sapi; }

        public void Loading()
        {
            byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
            if (data == null) { homes = new(); return; }
            try
            {
                homes = SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(data) ?? new();
                PruneInvalidHomes();
            }
            catch (Exception ex)
            {
                sapi.Logger.Error("Could not read Translocator Home data. Keeping all original links; Home designations will be empty. " + ex);
                homes = new();
            }
        }

        public void Saving()
        {
            PruneInvalidHomes();
            sapi.WorldManager.SaveGame.StoreData(SaveKey, SerializerUtil.Serialize(homes));
        }

        public BlockPos GetHome(string key)
        {
            if (string.IsNullOrEmpty(key) || !homes.TryGetValue(key, out BlockPos pos)) return null;
            if (!IsValid(key, pos)) { homes.Remove(key); return null; }
            return pos;
        }

        public bool IsHome(BlockPos pos, string key)
        {
            return GetHome(key)?.Equals(pos) == true;
        }

        public void SetHome(string key, BlockPos pos)
        {
            if (string.IsNullOrEmpty(key) || !IsValid(key, pos)) throw new InvalidOperationException("Home must be in the requested network.");
            RemoveAt(pos);
            homes[key] = pos.Copy();
        }

        public void RemoveAt(BlockPos pos)
        {
            foreach (string key in homes.Where(p => p.Value.Equals(pos)).Select(p => p.Key).ToArray())
                homes.Remove(key);
        }

        private bool IsValid(string key, BlockPos pos)
        {
            return pos != null && Core.linkManager?.rtData != null &&
                Core.linkManager.rtData.TryGetValue(pos, out string current) && current == key;
        }

        private void PruneInvalidHomes()
        {
            foreach (var pair in homes.ToArray()) if (!IsValid(pair.Key, pair.Value)) homes.Remove(pair.Key);
        }
    }
}