public class Interactions
{
    private Player _player;
    private Room _currentRoom;
    private ObjectManager _objectManager;

    public Interactions(Player player, Room currentRoom, ObjectManager objectManager)
    {
        _player = player;
        _currentRoom = currentRoom;
        _objectManager = objectManager;
    }

    public void UpdateCurrentRoom(Room newRoom)
    {
        _currentRoom = newRoom;
    }

    public void HandleUnlockCommand(string itemToUse, string targetDirection)
    {
        Item itemInInventory = _player.Inventory.Find(item => item.Name.ToLower() == itemToUse.ToLower());

        if (itemInInventory == null)
        {
            Console.WriteLine($"You don't have a '{itemToUse}' in your inventory.");
            return;
        }

        if (!_currentRoom.Exits.TryGetValue(targetDirection.ToLower(), out Exit exit))
        {
            Console.WriteLine($"There's no exit in the '{targetDirection}' direction.");
            return;
        }

        if (!exit.IsLocked)
        {
            Console.WriteLine($"The {exit.TargetRoom.Name} door is not locked in that direction.");
            return;
        }

        if (exit.RequiredItem != null && itemInInventory.Name.ToLower() == exit.RequiredItem.ToLower())
        {
            exit.IsLocked = false;
            Console.WriteLine($"You use the {itemInInventory.Name} and successfully unlock the door to the {exit.TargetRoom.Name}!");
            _player.Inventory.Remove(itemInInventory); // Gegenstand nach Gebrauch entfernen
            Console.WriteLine($"The {itemInInventory.Name} has been used and is now gone from your inventory.");
        }
        else
        {
            // GEÄNDERT: Generischere Fehlermeldung
            Console.WriteLine($"The {itemInInventory.Name} doesn't seem to work on that door.");
        }
    }
}
