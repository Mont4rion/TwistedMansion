using System;
using System.Collections.Generic;

public class GameLogic
{
    static Room currentRoom;
    static ObjectManager objectManager;
    static Player player;

    static void Main()
    {
        Console.WriteLine("------------------------------------");
        Console.WriteLine("Welcome to the Text-RPG!");
        Console.WriteLine("------------------------------------");

        objectManager = new ObjectManager();
        currentRoom = objectManager.GetRoom("Hallway");
        player = new Player("Hero");

        DisplayRoom(); // Initial display of the starting room

        while (true)
        {
            Console.Write("\nWhat do you want to do? (e.g., go north, look, take item, inventory, 'quit') ");
            string command = Console.ReadLine().ToLower().Trim();

            if (command == "quit")
            {
                Console.WriteLine("Thanks for playing!");
                break;
            }
            else if (command == "inventory")
            {
                player.DisplayInventory();
            }
            // --- "look" (general room description AND items in room) command logic ---
            else if (command == "look")
            {
                // Display the room description
                Console.WriteLine($"\n--- {currentRoom.Name.ToUpper()} ---");
                Console.WriteLine(currentRoom.Description);
                Console.WriteLine(currentRoom.GetAvailableExits());

                // Display items currently in the room
                if (currentRoom.ItemsInRoom.Count > 0)
                {
                    Console.WriteLine("You see the following items here:");
                    foreach (var item in currentRoom.ItemsInRoom)
                    {
                        if (item.ItemsInBox.Count > 0)
                        {
                            Console.WriteLine($"- {item.Name} (which contains something)");
                        }
                        else
                        {
                            Console.WriteLine($"- {item.Name}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("There are no notable items here.");
                }
            }
            // --- "look [item name]" command logic ---
            else if (command.StartsWith("look "))
            {
                string targetItemName = command.Substring("look ".Length).Trim();
                
                Item foundItem = null;

                // 1. Search for the item in the current room
                foundItem = currentRoom.ItemsInRoom.Find(item => item.Name.ToLower() == targetItemName.ToLower());

                // 2. If not found in the room, search in the player's inventory
                if (foundItem == null)
                {
                    foundItem = player.Inventory.Find(item => item.Name.ToLower() == targetItemName.ToLower());
                }

                // 3. Check if the item was found anywhere
                if (foundItem != null)
                {
                    if (foundItem.Moveable == false) // This is a non-movable item, likely a container
                    {
                        Console.WriteLine($"You look at the {foundItem.Name}. {foundItem.Description}");
                        
                        if (foundItem.ItemsInBox.Count > 0)
                        {
                            Console.WriteLine($"Inside the {foundItem.Name}, you see:");
                            foreach (var containedItem in foundItem.ItemsInBox)
                            {
                                Console.WriteLine($"- {containedItem.Name}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"The {foundItem.Name} appears to be empty.");
                        }
                    }
                    else // foundItem.Moveable == true
                    {
                        Console.WriteLine($"You look closely at the {foundItem.Name}. {foundItem.Description}");
                    }
                }
                else
                {
                    Console.WriteLine($"You don't see a '{targetItemName}' here or in your inventory.");
                }
            }
            // --- Take item command logic ---
            else if (command.StartsWith("take "))
            {
                string itemName = command.Substring("take ".Length).Trim();
                Item itemToTake = null;
                Item containerItem = null; // To store the container if the item is inside one

                // First, try to find the item directly in the current room
                itemToTake = currentRoom.ItemsInRoom.Find(item => item.Name.ToLower() == itemName.ToLower());

                // If not found directly, check if it's inside any container in the room
                if (itemToTake == null)
                {
                    foreach (var roomItem in currentRoom.ItemsInRoom)
                    {
                        // Check if the roomItem is a container and holds the target item
                        if (roomItem.ItemsInBox.Count > 0)
                        {
                            itemToTake = roomItem.ItemsInBox.Find(contained => contained.Name.ToLower() == itemName.ToLower());
                            if (itemToTake != null)
                            {
                                containerItem = roomItem; // Found it in this container!
                                break; // Stop searching containers
                            }
                        }
                    }
                }

                if (itemToTake != null) // Item was found (either directly or in a container)
                {
                    if (itemToTake.Moveable)
                    {
                        Console.WriteLine($"You take the {itemToTake.Name}.");
                        if (containerItem != null)
                        {
                            containerItem.ItemsInBox.Remove(itemToTake); // Remove from container
                        }
                        else
                        {
                            currentRoom.ItemsInRoom.Remove(itemToTake); // Remove from room directly
                        }
                        player.AddItem(itemToTake); // Add to player's inventory
                    }
                    else // Item found but is not movable
                    {
                        Console.WriteLine($"You cannot take the {itemToTake.Name}. It seems to be fixed in place.");
                    }
                }
                else // Item was not found at all
                {
                    Console.WriteLine($"You don't see a '{itemName}' here.");
                }
            }
            // --- Movement commands (only cardinal directions) ---
            else if (command.StartsWith("go "))
            {
                string direction = command.Substring(3);
                MovePlayer(direction);
            }
            else if (currentRoom.Exits.ContainsKey(command)) // This will now only match cardinal directions like "north"
            {
                MovePlayer(command);
            }
            else
            {
                Console.WriteLine("I don't understand that command, or you can't go that way.");
            }
        }
    }

    /// <summary>
    /// Displays the current room's information to the player, but NOT its items.
    /// Items are displayed separately with the 'look' command.
    /// </summary>
    static void DisplayRoom()
    {
        Console.WriteLine($"\n--- {currentRoom.Name.ToUpper()} ---");
        Console.WriteLine(currentRoom.Description);
        Console.WriteLine(currentRoom.GetAvailableExits());
    }

    /// <summary>
    /// Attempts to move the player in the specified direction.
    /// </summary>
    /// <param name="direction">The direction string provided by the player (e.g., "north").</param>
    static void MovePlayer(string direction)
    {
        if (currentRoom.Exits.TryGetValue(direction, out Room nextRoom))
        {
            currentRoom = nextRoom;
            DisplayRoom();
        }
        else
        {
            Console.WriteLine("You can't go that way.");
        }
    }
}

public class ObjectManager
{
    public Dictionary<string, Room> WorldRooms { get; private set; }
    public Dictionary<string, Item> WorldItems { get; private set; }

    public ObjectManager()
    {
        WorldRooms = new Dictionary<string, Room>();
        WorldItems = new Dictionary<string, Item>();

        InitializeRooms();
        InitializeItems();
    }

    private void InitializeRooms()
    {
        Room hallway = new Room("Hallway", "You are in a long, dimly lit hallway. The air smells musty.");
        Room library = new Room("Library", "A vast library filled with dusty tomes. A faint glow emanates from a corner.");
        Room kitchen = new Room("Kitchen", "A messy kitchen with an unwashed pan in the sink. The lingering scent of burnt toast hangs heavy.");

        WorldRooms.Add("Hallway", hallway);
        WorldRooms.Add("Library", library);
        WorldRooms.Add("Kitchen", kitchen);

        WorldRooms["Hallway"].AddExit("north", WorldRooms["Library"]);
        WorldRooms["Hallway"].AddExit("east", WorldRooms["Kitchen"]);
        
        WorldRooms["Library"].AddExit("south", WorldRooms["Hallway"]);

        WorldRooms["Kitchen"].AddExit("west", WorldRooms["Hallway"]);

        Console.WriteLine("Game world (rooms and exits) initialized successfully.");
    }

    private void InitializeItems()
    {
        Item rustyKey = new Item("Rusty Key", "A very old, rusty key. It looks like it might open something.", true); 
        WorldItems.Add("Rusty Key", rustyKey);

        Item oldBook = new Item("Old Book", "A dusty, leather-bound book. The title is unreadable.", true);
        WorldItems.Add("Old Book", oldBook);

        Item kitchenKnife = new Item("Kitchen Knife", "A dull and brittle knife.", true);
        WorldItems.Add("Kitchen Knife", kitchenKnife);

        // Container item
        Item kitchenShelf = new Item("Kitchen Shelf", "An old, worn-out shelf fixed to the wall. It looks like it could hold items.", false);
        WorldItems.Add("Kitchen Shelf", kitchenShelf); 

        // Place items in rooms
        WorldRooms["Hallway"].ItemsInRoom.Add(rustyKey);
        WorldRooms["Library"].ItemsInRoom.Add(oldBook);
        WorldRooms["Kitchen"].ItemsInRoom.Add(kitchenShelf); 

        // Place items INTO the container item
        kitchenShelf.ItemsInBox.Add(kitchenKnife); 

        Console.WriteLine("Game items initialized successfully.");
    }

    public Room GetRoom(string roomName)
    {
        if (WorldRooms.TryGetValue(roomName, out Room room))
        {
            return room;
        }
        Console.WriteLine($"Error: Room '{roomName}' not found in WorldRooms.");
        return null; 
    }

    public Item GetItem(string itemName)
    {
        if (WorldItems.TryGetValue(itemName, out Item item))
        {
            return item;
        }
        Console.WriteLine($"Error: Item '{itemName}' not found in WorldItems.");
        return null;
    }
}

public class Player
{
    public string Name { get; set; }
    public List<Item> Inventory { get; set; }

    public Player(string name)
    {
        Name = name;
        Inventory = new List<Item>();
    }

    public void AddItem(Item item)
    {
        Inventory.Add(item);
        Console.WriteLine($"You put the {item.Name} into your inventory.");
    }

    public void DisplayInventory()
    {
        if (Inventory.Count > 0)
        {
            Console.WriteLine("\n--- Your Inventory ---");
            foreach (var item in Inventory)
            {
                Console.WriteLine($"- {item.Name} ({item.Description})");
            }
            Console.WriteLine("----------------------");
        }
        else
        {
            Console.WriteLine("\nYour inventory is empty.");
        }
    }
}

public class Room
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, Room> Exits { get; set; }
    public List<Item> ItemsInRoom { get; set; }

    public Room(string name, string description)
    {
        this.Name = name;
        this.Description = description;
        this.Exits = new Dictionary<string, Room>();
        this.ItemsInRoom = new List<Item>();
    }

    public void AddExit(string direction, Room targetRoom)
    {
        Exits.Add(direction.ToLower(), targetRoom); 
    }

    public string GetAvailableExits()
    {
        List<string> exitDescriptions = new List<string>();
        foreach (var exit in Exits)
        {
            exitDescriptions.Add($"'{exit.Key}' leads to the {exit.Value.Name}");
        }
        return "You see exits: " + string.Join(", ", exitDescriptions) + ".";
    }
}

public class Item
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Moveable { get; set; }
    public List<Item> ItemsInBox { get; set; }

    public Item(string name, string description, bool moveable)
    {
        this.Name = name;
        this.Description = description;
        this.Moveable = moveable;
        this.ItemsInBox = new List<Item>();
    }
}

public class Interactions
{
}

public class Events
{
}