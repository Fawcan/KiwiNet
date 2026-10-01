using KiwiNet.Core.Math;
using KiwiNet.InstanceServer.Areas;
using KiwiNet.InstanceServer.Items;
using KiwiNet.InstanceServer.Network;
using KiwiNet.InstanceServer.Objects;
using KiwiNet.InstanceServer.Resources;
using KiwiNet.InstanceServer.Resources.Tables;
using KiwiNet.InstanceServer.WorldObjects;
using KiwiNet.InstanceServer.WorldObjects.Components;
using KiwiNet.Protocols.Instance;

namespace KiwiNet.InstanceServer.Commands.Implementations
{
    [CommandGroup]
    public static class CheatCommands
    {
        [CommandHandler("areachange")]
        public static string AreaChange(object invoker, ReadOnlySpan<string> args)
        {
            if (invoker is not RemotePlayer remotePlayer)
                return "This command must be invoked in-game.";

            if (args.Length == 0)
                return "Please provide a valid world area id";

            string worldAreaId = args[0];
            using ResourceHandle<WorldAreas> worldAreas = ResourceManager.Get<WorldAreas>("Data/WorldAreas.dat");
            if (worldAreas.Resource.GetDataRowByKey(worldAreaId) == null)
                return $"'{worldAreaId}' is not a valid world area id.";

            Vector2Int startPosition = default;
            if (args.Length >= 3)
            {
                if (int.TryParse(args[1], out int x) == false)
                    return $"Failed to parse '{args[1]}' as an x coordinate.";

                if (x < 0)
                    return $"x coordinate must be positive.";

                if (int.TryParse(args[2], out int y) == false)
                    return $"Failed to parse '{args[2]}' as a y coordinate.";

                if (y < 0)
                    return "y coordinate must be positive.";

                startPosition = new(x, y);
            }

            remotePlayer.BeginAreaTransfer(worldAreaId, startPosition);

            return string.Empty;
        }

        [CommandHandler("item")]
        public static string Item(object invoker, ReadOnlySpan<string> args)
        {
            string itemShortName = args.Length > 0 ? args[0] : string.Empty;

            // Generate item
            Item item = ItemGenerator.Generate(itemShortName);
            if (item == null)
                return $"'{itemShortName}' is not a valid item name.";

            RemotePlayer player = (RemotePlayer)invoker;
            Area area = player.Area;
            Vector2Int position = player.Player.Positioned.GridPosition;

            ItemGenerator.DropItem(item, area, position);

            return string.Empty;
        }

        [CommandHandler("monster")]
        public static string Monster(object invoker, ReadOnlySpan<string> args)
        {
            string shortName = args.Length > 0 ? args[0] : string.Empty;

            using ResourceHandle<WorldObjectRegistry> worldObjectRegistry = ResourceManager.Get<WorldObjectRegistry>(ObjectSystem.WorldObjectRegistryFile);
            using ResourceHandle<WorldObjectTemplate> template = worldObjectRegistry.Resource.GetTemplate(shortName);

            if (template == null)
                return $"'{shortName}' is not a valid object name.";

            if (template.FileName.Contains("Monsters") == false)
                return $"'{shortName}' is not a monster.";

            RemotePlayer player = (RemotePlayer)invoker;
            Vector2Int position = player.Player.Positioned.GridPosition;

            WorldObject monster = new();
            monster.Initialize(template, player.Area);
            monster.Positioned.SetPosition(position);
            monster.Attackable = true;

            Life life = monster.GetComponent<Life>();
            if (life != null)
                life.CurrentLife = 100;

            monster.Wake();

            return string.Empty;
        }

        [CommandHandler("openscreen")]
        public static string OpenScreen(object invoker, ReadOnlySpan<string> args)
        {
            string screen = args.Length > 0 ? args[0] : string.Empty;

            if (Enum.TryParse(screen, true, out ScreenType screenType) == false)
                return $"'{screen}' is not a valid screen type.";

            RemotePlayer player = (RemotePlayer)invoker;
            player.SendOpenScreen(screenType);

            return string.Empty;
        }

        [CommandHandler("addexp")]
        public static string AddExp(object invoker, ReadOnlySpan<string> args)
        {
            string arg = args.Length > 0 ? args[0] : string.Empty;

            if (int.TryParse(arg, out int amount) == false)
                return $"'{arg}' is not a valid experience amount.";

            RemotePlayer remotePlayer = (RemotePlayer)invoker;
            WorldObject player = remotePlayer.Player;
            player.GetComponent<Player>().AdjustExperience(amount);
            remotePlayer.SendWorldObjectUpdate<Player>(player);

            return string.Empty;
        }
    }
}
