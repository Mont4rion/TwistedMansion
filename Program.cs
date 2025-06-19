using System;
using System.Collections.Generic;
using System.Linq; // Added for .Any()

public class GameLogic
{
    static Room currentRoom;
    static ObjectManager objectManager;
    static Player player;
    static Interactions interactions;
    static Events gameEvents; // Instance of the Events class

    static void Main()
    {
        Console.WriteLine("------------------------------------");
        Console.WriteLine("Welcome to the Text-RPG!");
        Console.WriteLine("------------------------------------");

        objectManager = new ObjectManager();
        currentRoom = objectManager.GetRoom("Hallway"); // Set initial room
        player = new Player("Hero");
        interactions = new Interactions(player, currentRoom, objectManager); // Initialize Interactions
        gameEvents = new Events(currentRoom.Name); // Initialize Events with the starting room name

        DisplayRoom(); // Initial display of the starting room
        gameEvents.CheckForRoomEvents(); // Check for events in the starting room

        while (true)
        {
            Console.Write("\nWhat do you want to do? (e.g., go north, look, take item, inventory, use [item] on [direction], 'quit') ");
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
                Console.WriteLine($"\n--- {currentRoom.Name.ToUpper()} ---");
                Console.WriteLine(currentRoom.Description);
                Console.WriteLine(currentRoom.GetAvailableExits());

                if (currentRoom.ItemsInRoom.Any())
                {
                    Console.WriteLine("You see the following items here:");
                    foreach (var item in currentRoom.ItemsInRoom)
                    {
                        if (item.ItemsInBox.Any())
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

                        if (foundItem.ItemsInBox.Any())
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
                Item containerItem = null;

                // First, try to find the item directly in the current room
                itemToTake = currentRoom.ItemsInRoom.Find(item => item.Name.ToLower() == itemName.ToLower());

                // If not found directly, check if it's inside any container in the room
                if (itemToTake == null)
                {
                    foreach (var roomItem in currentRoom.ItemsInRoom)
                    {
                        if (roomItem.ItemsInBox.Any())
                        {
                            itemToTake = roomItem.ItemsInBox.Find(contained => contained.Name.ToLower() == itemName.ToLower());
                            if (itemToTake != null)
                            {
                                containerItem = roomItem;
                                break;
                            }
                        }
                    }
                }

                if (itemToTake != null)
                {
                    if (itemToTake.Moveable)
                    {
                        Console.WriteLine($"You take the {itemToTake.Name}.");
                        if (containerItem != null)
                        {
                            containerItem.ItemsInBox.Remove(itemToTake);
                        }
                        else
                        {
                            currentRoom.ItemsInRoom.Remove(itemToTake);
                        }
                        player.AddItem(itemToTake);
                    }
                    else
                    {
                        Console.WriteLine($"You cannot take the {itemToTake.Name}. It seems to be fixed in place.");
                    }
                }
                else
                {
                    Console.WriteLine($"You don't see a '{itemName}' here.");
                }
            }
            // --- "use [item] on [direction]" command logic ---
            else if (command.StartsWith("use ") && command.Contains(" on "))
            {
                string[] parts = command.Split(new string[] { " on " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    string itemToUse = parts[0].Substring("use ".Length).Trim();
                    string targetDirection = parts[1].Trim();
                    interactions.HandleUnlockCommand(itemToUse, targetDirection);
                }
                else
                {
                    Console.WriteLine("Invalid 'use' command. Try 'use [item] on [direction]'.");
                }
            }
            // --- Movement commands (only cardinal directions) ---
            else if (command.StartsWith("go ") || currentRoom.Exits.ContainsKey(command))
            {
                string direction = command.StartsWith("go ") ? command.Substring(3) : command;
                MovePlayer(direction);
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
        if (currentRoom.Exits.TryGetValue(direction, out Exit exit)) // Check for Exit object
        {
            if (exit.IsLocked)
            {
                Console.WriteLine($"The door to the {exit.TargetRoom.Name} is locked. You need a '{exit.RequiredItem}' to open it.");
            }
            else
            {
                currentRoom = exit.TargetRoom; // Move to the target room
                interactions.UpdateCurrentRoom(currentRoom); // Update current room in interactions

                // Update Events with the new room name and check for room-specific events
                gameEvents.UpdateCurrentRoom(currentRoom.Name);

                DisplayRoom(); // Display the new room's description
            }
        }
        else
        {
            Console.WriteLine("You can't go that way.");
        }
    }
}

// ---

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
        Room guestroom = new Room("Guestroom", "A small and messy Bedroom.");
        Room bathroom = new Room("Bathroom", "A smelly and disgusting room");
        Room floor = new Room("Floor", "Somthing is watching you from the dark");

        WorldRooms.Add("Hallway", hallway);
        WorldRooms.Add("Library", library);
        WorldRooms.Add("Kitchen", kitchen);
        WorldRooms.Add("Guestroom", guestroom);
        WorldRooms.Add("Bathroom", bathroom);
        WorldRooms.Add("Floor", floor);

        WorldRooms["Hallway"].AddExit("north", WorldRooms["Library"]);
        WorldRooms["Hallway"].AddExit("east", WorldRooms["Kitchen"]);

        WorldRooms["Library"].AddExit("south", WorldRooms["Hallway"]);

        WorldRooms["Kitchen"].AddExit("west", WorldRooms["Hallway"]);
        // THIS IS THE KEY CHANGE: Add the exit as locked, requiring "Rusty Key"
        WorldRooms["Kitchen"].AddExit("south", WorldRooms["Guestroom"], isLocked: true, requiredItem: "Rusty Key");

        // The guestroom exit back to kitchen should be unlocked by default
        WorldRooms["Guestroom"].AddExit("north", WorldRooms["Kitchen"]);
        WorldRooms["Guestroom"].AddExit("east", WorldRooms["Bathroom"]);
        WorldRooms["Guestroom"].AddExit("north east", WorldRooms["Floor"]);

        WorldRooms["Bathroom"].AddExit("west", WorldRooms["Guestroom"]);

        WorldRooms["Floor"].AddExit("south", WorldRooms["Guestroom"]);

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
        WorldRooms["Hallway"].ItemsInRoom.Add(rustyKey); // The key is in the hallway
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

// ---

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
        if (Inventory.Any()) // Use .Any()
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

// ---

public class Room
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, Exit> Exits { get; set; } // Change type here
    public List<Item> ItemsInRoom { get; set; }

    public Room(string name, string description)
    {
        this.Name = name;
        this.Description = description;
        this.Exits = new Dictionary<string, Exit>(); // Initialize with the new type
        this.ItemsInRoom = new List<Item>();
    }

    public void AddExit(string direction, Room targetRoom, bool isLocked = false, string requiredItem = null)
    {
        Exits.Add(direction.ToLower(), new Exit(targetRoom, isLocked, requiredItem));
    }

    public string GetAvailableExits()
    {
        List<string> exitDescriptions = new List<string>();
        foreach (var exitEntry in Exits)
        {
            string direction = exitEntry.Key;
            Exit exit = exitEntry.Value;

            if (exit.IsLocked)
            {
                exitDescriptions.Add($"'{direction}' leads to a locked {exit.TargetRoom.Name} door");
            }
            else
            {
                exitDescriptions.Add($"'{direction}' leads to the {exit.TargetRoom.Name}");
            }
        }
        return "You see exits: " + string.Join(", ", exitDescriptions) + ".";
    }
}

// ---

public class Exit
{
    public Room TargetRoom { get; set; }
    public bool IsLocked { get; set; }
    public string RequiredItem { get; set; } // Name of the item needed to unlock

    public Exit(Room targetRoom, bool isLocked = false, string requiredItem = null)
    {
        TargetRoom = targetRoom;
        IsLocked = isLocked;
        RequiredItem = requiredItem;
    }
}

// ---

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

// ---

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
        // 1. Check if the player has the item
        Item itemInInventory = _player.Inventory.Find(item => item.Name.ToLower() == itemToUse.ToLower());

        if (itemInInventory == null)
        {
            Console.WriteLine($"You don't have a '{itemToUse}' in your inventory.");
            return;
        }

        // 2. Check if the target direction exists and is locked
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

        // 3. Check if the item used matches the required item for this exit
        if (exit.RequiredItem != null && itemInInventory.Name.ToLower() == exit.RequiredItem.ToLower())
        {
            exit.IsLocked = false; // Unlock the door!
            Console.WriteLine($"You use the {itemInInventory.Name} and successfully unlock the door to the {exit.TargetRoom.Name}!");

            // --- ADDED LOGIC: Remove the item from inventory ---
            _player.Inventory.Remove(itemInInventory);
            Console.WriteLine($"The {itemInInventory.Name} has been used and is now gone from your inventory.");
            // ----------------------------------------------------
        }
        else
        {
            Console.WriteLine($"The {itemInInventory.Name} doesn't seem to work on the door to the {exit.TargetRoom.Name}.");
        }
    }
}

// ---
public class Events
{
    // Store the name of the current room
    public string CurrentRoomName { get; private set; } // Changed to string and made public getter
    private int bathroomEventCount = 0; // Renamed for clarity and better practice

    // Constructor to initialize with the starting room's name
    public Events(string initialRoomName)
    {
        CurrentRoomName = initialRoomName;
    }

    // Method to update the current room name when the player moves
    public void UpdateCurrentRoom(string newRoomName)
    {
        CurrentRoomName = newRoomName;
        // You might want to trigger events immediately after updating the room
        // if events should happen automatically when entering a room.
        CheckForRoomEvents();
    }

    // This method will be called to check for room-specific events
    public void CheckForRoomEvents()
    {
        if (CurrentRoomName == "Bathroom") 
        {
            if (bathroomEventCount == 0) // Use '==' for comparison
            {
                Console.WriteLine("\n--- A strange chill runs down your spine as you step into the Bathroom. ---");
                Console.WriteLine("You hear a faint whisper, almost imperceptible.");
                bathroomEventCount = 1; // Increment to ensure it only happens once
            }
            else if (bathroomEventCount == 1)
            {
                // Optional: A different message if they re-enter
                // Console.WriteLine("The guestroom is still chilly, but the whispers are gone.");
            }
        }
        // Add more room-specific event checks here
        // else if (CurrentRoomName == "Library") { ... }
    }
}