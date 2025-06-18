using System;
using System.Collections.Generic;
// Removed unused using System.Runtime.Serialization; // This namespace is not used and can be removed.

public class GameLogic
{
    // Static fields to hold the current state of the game
    static Room currentRoom; // Holds the room where the player currently is
    static ObjectManager objectManager; // Manages all game objects like rooms, items, etc.
    static Player player; // Player object needs to be initialized!

    static void Main()
    {
        Console.WriteLine("------------------------------------");
        Console.WriteLine("Welcome to the Text-RPG!");
        Console.WriteLine("------------------------------------");

        // --- Initialize the ObjectManager to create all game objects ---
        // This is where all your rooms, items, and their connections are set up.
        objectManager = new ObjectManager();

        // Set the starting room using the ObjectManager
        // You can choose any room defined in ObjectManager as the starting point.
        currentRoom = objectManager.GetRoom("Hallway"); 

        // --- Initialize the Player object here ---
        player = new Player("Hero"); // Create a new player instance with a name!

        // --- Optional: Give the player a starting item for testing ---
        Item startingKey = objectManager.GetItem("Rusty Key");
        if (startingKey != null)
        {
            // Remove the item from its initial room so the player can truly "take" it
            // if you want it to be initially in their inventory.
            // For now, let's just add it to the player without removing from room for simplicity
            // or ensure it's not placed in a room initially if it's a starting item.
            // If the key is *in* the Hallway, you'd want to remove it only when taken by command.
            // For this fix, we'll assume the player starts with nothing and picks it up.
        }

        // --- Start the game ---
        DisplayRoom(); // Initial display of the starting room

        // Main game loop: Continuously takes player input and processes commands
        while (true)
        {
            Console.Write("\nWhat do you want to do? (e.g., go north, look, take item, inventory, 'quit') ");
            string command = Console.ReadLine().ToLower().Trim();

            if (command == "quit")
            {
                Console.WriteLine("Thanks for playing!");
                break; // Exit the game loop
            }
            // --- Inventory command logic ---
            else if (command == "inventory")
            {
                player.DisplayInventory(); // Call the DisplayInventory method on your player object
            }
            else if (command == "look")
            {
                if (currentRoom.ItemsInRoom.Count > 0)
                {
                    Console.WriteLine("You see the following items here:");
                    foreach (var item in currentRoom.ItemsInRoom)
                    {
                        Console.WriteLine($"- {item.Name}");
                    }
                }
                else
                {
                    Console.WriteLine("There are no notable items here.");
                }
            }
            // --- Take item command logic ---
            else if (command.StartsWith("take "))
            {
                string itemName = command.Substring("take ".Length).Trim();
                Item itemToTake = currentRoom.ItemsInRoom.Find(item => item.Name.ToLower() == itemName.ToLower());

                if (itemToTake != null)
                {
                    Console.WriteLine($"You take the {itemToTake.Name}.");
                    currentRoom.ItemsInRoom.Remove(itemToTake); // Remove item from the room
                    player.AddItem(itemToTake); // Add item to player's inventory
                    DisplayRoom(); // Refresh room description to reflect item is gone
                }
                else
                {
                    Console.WriteLine($"You don't see a '{itemName}' here.");
                }
            }
            // --- Movement commands ---
            else if (command.StartsWith("go "))
            {
                string direction = command.Substring(3); // Get the part after "go "
                MovePlayer(direction);
            }
            else if (currentRoom.Exits.ContainsKey(command))
            {
                MovePlayer(command);
            }
            // --- Unrecognized command ---
            else
            {
                Console.WriteLine("I don't understand that command, or you can't go that way.");
            }
        }
    }

    /// <summary>
    /// Displays the current room's information to the player.
    /// </summary>
    static void DisplayRoom()
    {
        Console.WriteLine($"\n--- {currentRoom.Name.ToUpper()} ---");
        Console.WriteLine(currentRoom.Description);
        Console.WriteLine(currentRoom.GetAvailableExits()); // Show available exits from the current room
    }

    /// <summary>
    /// Attempts to move the player in the specified direction.
    /// </summary>
    /// <param name="direction">The direction string provided by the player (e.g., "north", "1").</param>
    static void MovePlayer(string direction)
    {
        // Try to get the target room from the current room's exits
        if (currentRoom.Exits.TryGetValue(direction, out Room nextRoom))
        {
            currentRoom = nextRoom; // Update the player's current room
            DisplayRoom(); // Display the new room's information
        }
        else
        {
            Console.WriteLine("You can't go that way."); // Inform the player if the exit doesn't exist
        }
    }
}

public class ObjectManager
{
    // A public property to hold all rooms, accessible from other classes by name
    public Dictionary<string, Room> WorldRooms { get; private set; }
    // A public property to hold all game items, accessible by name
    public Dictionary<string, Item> WorldItems { get; private set; }

    public ObjectManager()
    {
        WorldRooms = new Dictionary<string, Room>();
        WorldItems = new Dictionary<string, Item>(); // Initialize the items dictionary

        InitializeRooms(); // Set up all rooms and their connections
        InitializeItems(); // Set up all items and potentially place them in rooms
    }

    /// <summary>
    /// Creates and configures all rooms and their exits in the game world.
    /// </summary>
    private void InitializeRooms()
    {
        // Create individual room instances
        Room hallway = new Room("Hallway", "You are in a long, dimly lit hallway. The air smells musty.");
        Room library = new Room("Library", "A vast library filled with dusty tomes. A faint glow emanates from a corner.");
        Room kitchen = new Room("Kitchen", "A messy kitchen with an unwashed pan in the sink. The lingering scent of burnt toast hangs heavy.");

        // Add rooms to the WorldRooms dictionary for easy lookup by name
        WorldRooms.Add("Hallway", hallway);
        WorldRooms.Add("Library", library);
        WorldRooms.Add("Kitchen", kitchen);

        // Define exits between rooms using their names and the WorldRooms dictionary
        WorldRooms["Hallway"].AddExit("north", WorldRooms["Library"]);    // North from Hallway leads to Library
        WorldRooms["Hallway"].AddExit("east", WorldRooms["Kitchen"]);     // East from Hallway leads to Kitchen

        WorldRooms["Library"].AddExit("south", WorldRooms["Hallway"]);    // South from Library leads to Hallway

        WorldRooms["Kitchen"].AddExit("west", WorldRooms["Hallway"]);     // West from Kitchen leads to Hallway

        Console.WriteLine("Game world (rooms and exits) initialized successfully.");
    }

    /// <summary>
    /// Creates and configures all items in the game.
    /// </summary>
    private void InitializeItems()
    {
        // Create an Item instance and store it in the WorldItems dictionary
        Item rustyKey = new Item("Rusty Key", "A very old, rusty key. It looks like it might open something.");
        WorldItems.Add("Rusty Key", rustyKey); // Use the item's name as the key for easy lookup

        Item oldBook = new Item("Old Book", "A dusty, leather-bound book. The title is unreadable.");
        WorldItems.Add("Old Book", oldBook);

        // --- Place items in rooms ---
        // Access rooms via WorldRooms dictionary and add items to their ItemsInRoom list
        WorldRooms["Hallway"].ItemsInRoom.Add(rustyKey);
        WorldRooms["Library"].ItemsInRoom.Add(oldBook);

        Console.WriteLine("Game items initialized successfully.");
    }

    /// <summary>
    /// Retrieves a Room object by its name from the WorldRooms dictionary.
    /// </summary>
    /// <param name="roomName">The name of the room to retrieve.</param>
    /// <returns>The Room object if found; otherwise, null.</returns>
    public Room GetRoom(string roomName)
    {
        // Use TryGetValue for safe access, avoiding exceptions if the key doesn't exist
        if (WorldRooms.TryGetValue(roomName, out Room room))
        {
            return room;
        }
        Console.WriteLine($"Error: Room '{roomName}' not found in WorldRooms.");
        return null; 
    }

    /// <summary>
    /// Retrieves an Item object by its name from the WorldItems dictionary.
    /// </summary>
    /// <param name="itemName">The name of the item to retrieve.</param>
    /// <returns>The Item object if found; otherwise, null.</returns>
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
    public List<Item> Inventory { get; set; } // The player's personal list of items

    public Player(string name)
    {
        Name = name;
        Inventory = new List<Item>(); // Initialize the inventory when a player is created
    }

    /// <summary>
    /// Adds an item to the player's inventory.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void AddItem(Item item)
    {
        Inventory.Add(item);
        Console.WriteLine($"You put the {item.Name} into your inventory.");
    }

    /// <summary>
    /// Displays the contents of the player's inventory.
    /// </summary>
    public void DisplayInventory()
    {
        if (Inventory.Count > 0)
        {
            Console.WriteLine("\n--- Your Inventory ---");
            foreach (var item in Inventory)
            {
                Console.WriteLine($"- {item.Name} ({item.Description})"); // Show name and description
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
    public string Description { get; set; } // The primary text description of the room
    public Dictionary<string, Room> Exits { get; set; } // Stores possible exits and their target Room objects
    public List<Item> ItemsInRoom { get; set; } // List to hold items currently present in this room

    /// <summary>
    /// Constructor for the Room class.
    /// </summary>
    /// <param name="name">The name of the room.</param>
    /// <param name="description">The descriptive text for the room.</param>
    public Room(string name, string description)
    {
        this.Name = name;
        this.Description = description;
        this.Exits = new Dictionary<string, Room>(); // Initialize the dictionary for exits
        this.ItemsInRoom = new List<Item>(); // Initialize the list of items in the room
    }

    /// <summary>
    /// Adds an exit from this room to another room.
    /// </summary>
    /// <param name="direction">The keyword/direction (e.g., "north", "1") to take this exit.</param>
    /// <param name="targetRoom">The Room object that this exit leads to.</param>
    public void AddExit(string direction, Room targetRoom)
    {
        // Store the direction in lowercase to handle case-insensitive player input
        Exits.Add(direction.ToLower(), targetRoom); 
    }

    /// <summary>
    /// Generates a string describing all available exits from this room for the player.
    /// </summary>
    /// <returns>A formatted string listing the exits.</returns>
    public string GetAvailableExits()
    {
        List<string> exitDescriptions = new List<string>();
        foreach (var exit in Exits)
        {
            // Format each exit as "'direction' leads to the RoomName"
            exitDescriptions.Add($"'{exit.Key}' leads to the {exit.Value.Name}");
        }
        return "You see exits: " + string.Join(", ", exitDescriptions) + "."; // Join them with ", "
    }
}

public class Item
{
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    /// Constructor for the Item class.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="description">The descriptive text for the item.</param>
    public Item(string name, string description)
    {
        this.Name = name;
        this.Description = description;
    }
}

// --- Placeholder Classes for Future Development ---
// Removed the redundant 'Inventory' class as its functionality is now within the Player class.

public class Interactions
{
    // This class will handle player interactions with objects and NPCs in the game world.
    // Examples: Using an item, talking to an NPC, solving a puzzle.
}

public class Events
{
    // This class could manage in-game events, quests, or triggers.
    // Examples: A door opening after a specific action, a new NPC appearing.
}