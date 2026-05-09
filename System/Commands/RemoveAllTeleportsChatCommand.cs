using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace TeleportationNetwork
{
    public class RemoveAllTeleportsChatCommand : ServerChatCommandBase
    {
        private const long ConfirmationWindowMs = 30_000;

        private bool _accepted = false;
        private IPlayer? _latestPlayer;
        private long _acceptedAtMs;

        public RemoveAllTeleportsChatCommand(ICoreServerAPI api) : base(api)
        {
            api.ChatCommands
                .GetOrCreate("tpnet")
                .BeginSubCommand("removeall")
                    .WithDescription("Remove all teleports")
                    .RequiresPrivilege(Privilege.controlserver)
                    .HandleWith(RemoveAll)
                .EndSubCommand();
        }

        private TextCommandResult RemoveAll(TextCommandCallingArgs args)
        {
            long nowMs = Api.World.ElapsedMilliseconds;
            bool windowValid = nowMs - _acceptedAtMs <= ConfirmationWindowMs;

            if (_accepted && windowValid && _latestPlayer == args.Caller.Player)
            {
                _accepted = false;
                var manager = Api.ModLoader.GetModSystem<TeleportManager>();
                int chunkSize = GlobalConstants.ChunkSize;

                var teleports = manager.Points.GetAll().ToList();
                int total = teleports.Count;
                if (total == 0)
                {
                    return TextCommandResult.Success("No teleports to remove");
                }

                var caller = args.Caller.Player as IServerPlayer;
                int[] completed = [0];

                foreach (Teleport teleport in teleports)
                {
                    int chunkX = teleport.Pos.X / chunkSize;
                    int chunkZ = teleport.Pos.Z / chunkSize;

                    Api.WorldManager.LoadChunkColumnPriority(chunkX, chunkZ, new ChunkLoadOptions
                    {
                        OnLoaded = () =>
                        {
                            Api.World.BlockAccessor.SetBlock(0, teleport.Pos);
                            int done = Interlocked.Increment(ref completed[0]);
                            if (done == total && caller != null)
                            {
                                Api.SendMessage(caller, GlobalConstants.AllChatGroups,
                                    $"Removed {total} teleport{(total == 1 ? "" : "s")}",
                                    EnumChatType.OwnMessage);
                            }
                        }
                    });
                }

                return TextCommandResult.Success($"Removing started ({total} teleport{(total == 1 ? "" : "s")})");
            }

            _latestPlayer = args.Caller.Player;
            _accepted = true;
            _acceptedAtMs = nowMs;
            return TextCommandResult.Success("<font color=#ffaaaa>Warning!</font> This will remove all existing teleport blocks. " +
                "It cannot be undone. If you are sure, enter the command again within 30 seconds");
        }
    }
}