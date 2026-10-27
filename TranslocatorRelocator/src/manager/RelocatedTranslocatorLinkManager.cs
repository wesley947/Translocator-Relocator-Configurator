using System.Linq;
using System.Text;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Common;

namespace TranslocatorRelocator.src.manager
{
    public class RelocatedTranslocatorLinkManager
    {
        public ICoreServerAPI api;
        public Dictionary<BlockPos, string> rtData; //Relocated Translocator Data
        public Dictionary<string, List<BlockPos>> links = new();
        const string RTKEY = "rtData";

        public RelocatedTranslocatorLinkManager(ICoreServerAPI api)
        {
            this.api = api;
            RegisterCommands(api);
        }

        public void Loading()
        {
            byte[] data = api.WorldManager.SaveGame.GetData(RTKEY);
            if (data != null)
            {
                rtData = SerializerUtil.Deserialize<Dictionary<BlockPos, string>>(data);
                UpdateLinks();
            }
            else
            {
                rtData = new Dictionary<BlockPos, string>();
            }
        }

        public void Saving()
        {
            api.WorldManager.SaveGame.StoreData(RTKEY, SerializerUtil.Serialize(rtData));
        }

        private void RegisterCommands(ICoreServerAPI api)
        {
            CommandArgumentParsers parsers = new(api);
            api.ChatCommands

                .Create("translocatorRelocator")
                .WithAlias("tr")
                .RequiresPrivilege(Privilege.root)

                .BeginSubCommand("printAllData")
                .WithAlias("pad")
                .WithDescription("Print all translocator to the chat and to the log file. Used for debugging.")
                .HandleWith(OnRequestPrintAllData)
                .EndSubCommand()

                .BeginSubCommand("wipeAllData")
                .WithDescription("Wipes all translocator linking data. Should only be used for debugging or in case the mod's data is broken for any reasons, e.g, destroying relocated translocators after uninstalling the mod and then reinstalling the mod. Ask mod author (Windows98) for help!")
                .HandleWith(OnCleanAllDataRequest)
                .EndSubCommand();
        }

        private TextCommandResult OnRequestPrintAllData(TextCommandCallingArgs args)
        {
            StringBuilder stringBuilder = new();
            if (rtData.Count > 0)
            {
                stringBuilder.AppendLine("Here is all the data:");
                for (int i = 0; i < rtData.Count; i++)
                {
                    stringBuilder.AppendLine(string.Format("Entry {0} = (Pos = ({1}), Key = {2})", i.ToString(), rtData.ElementAt(i).Key, rtData.ElementAt(i).Value));
                }
                stringBuilder.AppendLine("That's it.");
            }
            else
            {
                stringBuilder.AppendLine("No data found.");
            }
            string builtString = stringBuilder.ToString();
            Print(builtString);
            return TextCommandResult.Success(builtString);
        }

        private TextCommandResult OnCleanAllDataRequest(TextCommandCallingArgs args)
        {
            rtData.Clear();
            UpdateLinks();
            return TextCommandResult.Success("Wiped all translocator data, hope this was intentional!");
        }

        // Bulk rename changes the authoritative registry. Unloaded block entities will
        // reconcile their own saved linkKey on the next server tick after loading.
        public int CountForNetwork(string linkKey)
        {
            if (rtData == null || string.IsNullOrEmpty(linkKey)) return 0;
            return rtData.Values.Count(value => value == linkKey);
        }

        public bool NetworkExists(string linkKey)
        {
            return rtData != null && rtData.Values.Any(value =>
                string.Equals(value, linkKey, System.StringComparison.OrdinalIgnoreCase));
        }

        public int RenameNetwork(string oldKey, string newKey)
        {
            if (string.IsNullOrWhiteSpace(oldKey) || string.IsNullOrWhiteSpace(newKey))
                throw new System.ArgumentException("Both network names must be non-empty.");
            if (newKey.Length > 16) throw new System.ArgumentException("Network names must be 16 characters or fewer.");
            if (NetworkExists(newKey)) throw new System.InvalidOperationException("Destination network already exists.");
            BlockPos[] positions = rtData.Where(pair => pair.Value == oldKey)
                .Select(pair => pair.Key).ToArray();
            if (positions.Length == 0) throw new System.InvalidOperationException("Source network no longer exists.");
            foreach (BlockPos position in positions) rtData[position] = newKey;
            UpdateLinks();
            return positions.Length;
        }

        public void AddOrSetKey(BlockPos key, string linkKey = "")
        {
            if (HasKey(key))
            {
                rtData[key] = linkKey;
            }
            else
            {
                rtData.Add(key, linkKey);
            }
            UpdateLinks();
        }

        public void RemoveKey(BlockPos key)
        {
            if (HasKey(key))
            {
                rtData.Remove(key);
                UpdateLinks();
                return;
            }
        }

        public void UpdateLinks()
        {
            links.Clear();

            foreach (BlockPos blockPos in rtData.Keys)
            {
                string linkKey = rtData.Get(blockPos, "");
                if (linkKey.Length > 0)
                {
                    if (links.TryGetValue(linkKey, out List<BlockPos> list))
                    {
                        list.Add(blockPos);
                    }
                    else
                    {
                        list = new List<BlockPos>()
                        {
                            blockPos
                        };
                        links.Add(linkKey, list);
                    }
                }
            }
        }

        public BlockPos[] GetLinksForKey(BlockPos key, string linkKey)
        {
            List<BlockPos> matchLinks = links.Get(linkKey);
            if (matchLinks != null && matchLinks.Contains(key))
            {
                return matchLinks.ToArray().Remove(key);
            }
            return null;
        }

        private bool HasKey(BlockPos key)
        {
            return rtData.ContainsKey(key);
        }

        private static void Print(string text)
        {
            Core.Print(string.Format("Relocated Translocator Manager: {0}", text));
        }

        private void PrintAllData()
        {
            for (int i = 0; i < rtData.Count; i++)
            {
                Print(string.Format("Entry {0}: Pos = {1}, Key = {2}", i.ToString(), rtData.ElementAt(i).Key, rtData.ElementAt(i).Value));
            }
        }
    }
}
