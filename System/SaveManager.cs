using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TeleportationNetwork
{
    public class SaveManager : ModSystem
    {
        private ICoreServerAPI _sapi = null!;
        private TeleportManager _teleportManager = null!;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void StartPre(ICoreAPI api)
        {
            if (api is ICoreServerAPI sapi)
            {
                _sapi = sapi;
                _teleportManager = sapi.ModLoader.GetModSystem<TeleportManager>();

                sapi.Event.GameWorldSave += OnSaveGame;
                sapi.Event.SaveGameLoaded += OnLoadGame;
            }
        }

        private void OnSaveGame()
        {
            var data = SerializerUtil.Serialize(_teleportManager.Points.GetAll().ToList());
            _sapi.WorldManager.SaveGame.StoreData("TPNetData", data);
        }

        private void OnLoadGame()
        {
            try
            {
                Mod.Logger.Event("Start loading data");

                byte[] data = _sapi.WorldManager.SaveGame.GetData("TPNetData");
                if (data != null)
                {
                    var list = DeserializeData(data);
                    _teleportManager.Points.SetFrom(list);
                    foreach (var teleport in list)
                    {
                        Mod.Logger.Debug($"Loaded teleport data for {teleport.Name} at {teleport.Pos}");
                    }
                    Mod.Logger.Event($"Data loaded for {list.Count} teleports");
                    Mod.Logger.Event("Check teleport exists (async)");
                    _sapi.ModLoader.GetModSystem<TeleportManager>().CheckAllTeleportExists();
                }
                else
                {
                    Mod.Logger.Event("No data for load");
                }
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Failed loading data:\n{0}", e);
            }
        }

        private List<Teleport> DeserializeData(byte[] data)
        {
            try
            {
                return SerializerUtil.Deserialize<List<Teleport>>(data);
            }
            catch (ProtoException)
            {
                Mod.Logger.Debug("Old world? Trying legacy loader");

                var list = new List<Teleport>();

                if (TryDeserializeFrom(new LegacyTeleportLoader_modv1_14_3(), data, ref list)) { }
                else if (TryDeserializeFrom(new LegacyTeleportLoader_v1_8_0_pre_6(), data, ref list)) { }
                else if (TryDeserializeFrom(new LegacyTeleportLoader_v1_8_0_pre_1(), data, ref list)) { }
                else if (TryDeserializeFrom(new LegacyTeleportLoader_v1_7(), data, ref list)) { }
                else if (TryDeserializeFrom(new LegacyTeleportLoader_v1_5(), data, ref list)) { }
                else
                {
                    Mod.Logger.Debug("Failed loadind data via legacy loader." +
                        " May be you mod version is too old or save corrupted");
                }

                return list;
            }
        }

        private bool TryDeserializeFrom(LegacyTeleportLoader loader, byte[] data, ref List<Teleport> list)
        {
            try
            {
                list = loader.Deserialize(_teleportManager, data);
                return true;
            }
            catch (ProtoException)
            {
                return false;
            }
        }

        private abstract class LegacyTeleportLoader
        {
            public abstract List<Teleport> Deserialize(TeleportManager manager, byte[] data);
        }

        private class LegacyTeleportLoader_modv1_14_3 : LegacyTeleportLoader
        {
            public override List<Teleport> Deserialize(TeleportManager manager, byte[] data)
            {
                return SerializerUtil.Deserialize<List<LegacyTeleport>>(data)
                    .Select(legacy =>
                    {
                        var teleport = new Teleport(legacy.Pos, legacy.Name, legacy.Enabled);
                        teleport.ActivatedByPlayers.AddRange(legacy.ActivatedByPlayers);
                        teleport.Neighbours.AddRange(legacy.Neighbours);
                        teleport.ClientData.AddRange(legacy.ClientData);
                        return teleport;
                    })
                    .ToList();
            }

            [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
            private class LegacyTeleport
            {
                public BlockPos Pos { get; private set; }
                public string Name { get; set; }
                public bool Enabled { get; set; }

                public List<Teleport> Neighbours { get; private set; }
                public List<string> ActivatedByPlayers { get; private set; }
                public Dictionary<string, TeleportClientData> ClientData { get; private set; }

                private LegacyTeleport()
                {
                    Name = "null";
                    Enabled = false;
                    Pos = new BlockPos(0);
                    Neighbours = [];
                    ActivatedByPlayers = [];
                    ClientData = new Dictionary<string, TeleportClientData>();
                }
            }
        }

        private class LegacyTeleportLoader_v1_8_0_pre_6 : LegacyTeleportLoader
        {
            public override List<Teleport> Deserialize(TeleportManager manager, byte[] data)
            {
                return SerializerUtil.Deserialize<List<LegacyTeleport>>(data)
                    .Select(legacy =>
                    {
                        var teleport = new Teleport(legacy.Pos, legacy.Name, legacy.Enabled);
                        teleport.ActivatedByPlayers.AddRange(legacy.ActivatedByPlayers);
                        teleport.Neighbours.AddRange(legacy.Neighbours);
                        return teleport;
                    })
                    .ToList();
            }

            [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
            private class LegacyTeleport
            {
                public BlockPos Pos { get; private set; }
                public string Name { get; set; }
                public bool Enabled { get; set; }

                public List<Teleport> Neighbours { get; private set; }
                public List<string> ActivatedByPlayers { get; private set; }

                public string Icon { get; set; }
                public int Color { get; set; }

                private LegacyTeleport()
                {
                    Name = "null";
                    Enabled = false;
                    Pos = new BlockPos(0);
                    Neighbours = [];
                    ActivatedByPlayers = [];
                    Icon = "";
                    Color = 0;
                }
            }
        }

        private class LegacyTeleportLoader_v1_8_0_pre_1 : LegacyTeleportLoader
        {
            public override List<Teleport> Deserialize(TeleportManager manager, byte[] data)
            {
                return SerializerUtil.Deserialize<List<LegacyTeleport>>(data)
                    .Select(legacy =>
                    {
                        var teleport = new Teleport(legacy.Pos, legacy.Name, legacy.Enabled);
                        teleport.ActivatedByPlayers.AddRange(legacy.ActivatedByPlayers);
                        teleport.Neighbours.AddRange(legacy.Neighbours);
                        return teleport;
                    })
                    .ToList();
            }

            [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
            private class LegacyTeleport
            {
                public BlockPos Pos { get; private set; }
                public string Name { get; set; }
                public bool Enabled { get; set; }

                public List<Teleport> Neighbours { get; private set; }
                public List<string> ActivatedByPlayers { get; private set; }

                private LegacyTeleport()
                {
                    Name = "null";
                    Enabled = false;
                    Pos = new BlockPos(0);
                    Neighbours = [];
                    ActivatedByPlayers = [];
                }
            }
        }

        private class LegacyTeleportLoader_v1_7 : LegacyTeleportLoader
        {
            public override List<Teleport> Deserialize(TeleportManager manager, byte[] data)
            {
                return SerializerUtil.Deserialize<List<LegacyTeleport>>(data)
                    .Select(legacy =>
                    {
                        var tp = new Teleport(legacy.Pos, legacy.Name, legacy.Enabled);
                        tp.ActivatedByPlayers.AddRange(legacy.ActivatedByPlayers);

                        foreach (BlockPos npos in legacy.Neighbours)
                        {
                            Teleport? ntp = manager.Points[npos];
                            if (ntp != null)
                            {
                                tp.Neighbours.Add(ntp);
                            }
                        }

                        return tp;
                    })
                    .ToList();
            }

            [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
            private class LegacyTeleport
            {
                public bool Enabled { get; set; }
                public string Name { get; set; }
                public BlockPos Pos { get; set; }
                public List<string> ActivatedByPlayers { get; set; }
                public List<BlockPos> Neighbours { get; set; }

                public LegacyTeleport()
                {
                    Enabled = false;
                    Name = "null";
                    Pos = new BlockPos(0);
                    ActivatedByPlayers = [];
                    Neighbours = [];
                }
            }
        }

        private class LegacyTeleportLoader_v1_5 : LegacyTeleportLoader
        {
            public override List<Teleport> Deserialize(TeleportManager manager, byte[] data)
            {
                return SerializerUtil.Deserialize<Dictionary<BlockPos, LegacyTeleportData>>(data)
                    .Select(legacy => new Teleport(legacy.Key, legacy.Value.name, legacy.Value.available))
                    .ToList();
            }

            [ProtoContract]
            private class LegacyTeleportData
            {
                [ProtoMember(1)] public string name = "";
                [ProtoMember(2)] public bool available = false;
                [ProtoMember(3)] public List<string> activatedBy = new();
            }
        }
    }
}
