using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class GameLogic
{
    static Room currentRoom;
    static ObjectManager objectManager;
    static Player player;
    static Interactions interactions;
    static Events gameEvents;
    static bool inExclusiveEventMode = false;
    static SaveGameManager saveGameManager;

    static GameLogic()
    {
        objectManager = new ObjectManager();

        objectManager.InitializeRooms();
        objectManager.InitializeItems();
        objectManager.InitializeKombinations();

        saveGameManager = new SaveGameManager("savegame.json");
    }

    /// <summary>
    /// Displays the current room's name, description, available exits, and items.
    /// </summary>
    static void DisplayRoom()
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

    /// <summary>
    /// Adds test items to the player's inventory for development purposes.
    /// This method should ideally be removed or commented out for a release build.
    /// </summary>
    static void TestItems()
    {
        // Example: Give the player starting items for testing combinations
        // Ensure you add these items only if the player doesn't already have them,
        // especially important if calling TestItems multiple times or loading a game.
        if (!player.Inventory.Any(i => i.Name == "Empty Frame"))
        {
            player.AddItem(objectManager.WorldItems["Empty Frame"]);
        }
        if (!player.Inventory.Any(i => i.Name == "Butterfly Blue"))
        {
            player.AddItem(objectManager.WorldItems["Butterfly Blue"]);
        }
        if (!player.Inventory.Any(i => i.Name == "Butterfly Red"))
        {
            player.AddItem(objectManager.WorldItems["Butterfly Red"]);
        }
        if (!player.Inventory.Any(i => i.Name == "Butterfly Green"))
        {
            player.AddItem(objectManager.WorldItems["Butterfly Green"]);
        }
        if (!player.Inventory.Any(i => i.Name == "Butterfly Black"))
        {
            player.AddItem(objectManager.WorldItems["Butterfly Black"]);
        }
        if (!player.Inventory.Any(i => i.Name == "Glue"))
        {
            player.AddItem(objectManager.WorldItems["Glue"]);
        }
        if (!player.Inventory.Any(i => i.Name == "Stick"))
        {
            player.AddItem(objectManager.WorldItems["Stick"]);
        }
    }

    /// <summary>
    /// Attempts to combine two items from the player's inventory based on predefined combinations.
    /// This method is more generic and uses the Kombinations definitions directly.
    /// </summary>
    /// <param name="item1ToCombine">The first item object from the player's inventory.</param>
    /// <param name="item2ToCombine">The second item object from the player's inventory.</param>
   // GameLogic.cs (innerhalb der TryCombineItems-Methode)

    // GameLogic.cs (innerhalb der TryCombineItems-Methode)

    // GameLogic.cs (innerhalb der TryCombineItems-Methode)

    public static void TryCombineItems(Item item1ToCombine, Item item2ToCombine)
    {
        string name1 = item1ToCombine.Name;
        string name2 = item2ToCombine.Name;

        List<string> specificButterflyNames = new List<string> { "Butterfly Blue", "Butterfly Red", "Butterfly Green", "Butterfly Black" };

        bool foundCombination = false;

        foreach (var combinationEntry in objectManager.WorldKombinations)
        {
            Kombinations comboDefinition = combinationEntry.Value;

            if (comboDefinition.RequiredItems == null || comboDefinition.RequiredItems.Count != 2)
            {
                continue;
            }

            string required1 = comboDefinition.RequiredItems[0];
            string required2 = comboDefinition.RequiredItems[1];

            bool currentRuleMatches = false;
            Item frameItemUsed = null;
            Item butterflyItemUsed = null;

            // LOGIK FÜR SPEZIFISCHE KOMBINATIONEN (z.B. Stick + Glue)
            // Wenn die Regel keine "Butterfly"-Platzhalter enthält
            if (!required1.Equals("Butterfly", StringComparison.OrdinalIgnoreCase) &&
                !required2.Equals("Butterfly", StringComparison.OrdinalIgnoreCase))
            {
                if ((name1.Equals(required1, StringComparison.OrdinalIgnoreCase) && name2.Equals(required2, StringComparison.OrdinalIgnoreCase)) ||
                    (name1.Equals(required2, StringComparison.OrdinalIgnoreCase) && name2.Equals(required1, StringComparison.OrdinalIgnoreCase)))
                {
                    currentRuleMatches = true;
                }
            }
            // LOGIK FÜR SCHMETTERLING-KOMBINATIONEN (z.B. Frame + Butterfly)
            else
            {
                // Wenn die Regel einen "Butterfly"-Platzhalter enthält
                string specificRequiredName = required1.Equals("Butterfly", StringComparison.OrdinalIgnoreCase) ? required2 : required1;

                if ((name1.Equals(specificRequiredName, StringComparison.OrdinalIgnoreCase) && specificButterflyNames.Contains(name2, StringComparer.OrdinalIgnoreCase)))
                {
                    currentRuleMatches = true;
                    frameItemUsed = item1ToCombine;
                    butterflyItemUsed = item2ToCombine;
                }
                else if ((name2.Equals(specificRequiredName, StringComparison.OrdinalIgnoreCase) && specificButterflyNames.Contains(name1, StringComparer.OrdinalIgnoreCase)))
                {
                    currentRuleMatches = true;
                    frameItemUsed = item2ToCombine;
                    butterflyItemUsed = item1ToCombine;
                }
            }

            // Wenn die aktuelle Regel matched, verarbeite die Kombination
            if (currentRuleMatches)
            {
                Item resultItem = objectManager.GetItem(comboDefinition.Name);
                if (resultItem != null)
                {
                    // Logic for handling ItemsInBox (specific to frames)
                    if (frameItemUsed != null && resultItem.Name.Contains("Frame", StringComparison.OrdinalIgnoreCase))
                    {
                        // Copy existing butterflies from the old frame to the new one
                        foreach (var existingButterfly in frameItemUsed.ItemsInBox)
                        {
                            resultItem.ItemsInBox.Add(existingButterfly);
                        }
                    }

                    // Add the newly combined butterfly (if applicable)
                    if (butterflyItemUsed != null)
                    {
                        resultItem.ItemsInBox.Add(butterflyItemUsed);
                    }

                    // Update description for frame items based on content
                    if (resultItem.Name.Contains("Frame", StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateFrameDescription(resultItem);
                    }

                    // Remove the original items from player's inventory
                    player.RemoveItem(item1ToCombine);
                    player.RemoveItem(item2ToCombine);

                    // Add the resulting item to player's inventory
                    player.AddItem(resultItem);

                    Console.WriteLine($"You combined the {name1} and the {name2} to create a {comboDefinition.Name}!");
                    foundCombination = true;
                    break;
                }
                else
                {
                    Console.WriteLine($"Error: The resulting item '{comboDefinition.Name}' was not found in the game world. Check ObjectManager.InitializeItems().");
                    foundCombination = true;
                    break;
                }
            }
        }

        if (!foundCombination)
        {
            Console.WriteLine("These items cannot be combined.");
        }
    }

    // Füge diese Hilfsmethode in GameLogic.cs hinzu
    private static void UpdateFrameDescription(Item frame)
    {
        if (frame != null && frame.Name.Contains("Frame"))
        {
            if (frame.ItemsInBox.Count == 0)
            {
                frame.Description = "An empty wooden frame with a glass panel and 4 pins stuck in it.";
            }
            else if (frame.ItemsInBox.Count == 1)
            {
                frame.Description = $"An empty wooden frame with a glass panel and one beautiful butterfly ({frame.ItemsInBox[0].Name}).";
            }
            else if (frame.ItemsInBox.Count > 0)
            {
                string butterflyNames = string.Join(", ", frame.ItemsInBox.Select(b => b.Name));
                frame.Description = $"An empty wooden frame with a glass panel and {frame.ItemsInBox.Count} beautiful butterflies: {butterflyNames}.";
            }
        }
    }

    /// <summary>
    /// Handles player movement to a different room.
    /// </summary>
    /// <param name="direction">The direction the player wants to move (e.g., "north", "east").</param>
    static void MovePlayer(string direction)
    {
        if (currentRoom.Exits.TryGetValue(direction, out Exit exit))
        {
            if (exit.IsLocked)
            {
                Console.WriteLine($"The way to {exit.TargetRoom.Name} is locked. Maybe you need something to open it.");
            }
            else
            {
                currentRoom = exit.TargetRoom;
                interactions.UpdateCurrentRoom(currentRoom);
                gameEvents.UpdateCurrentRoom(currentRoom.Name);

                DisplayRoom();

                inExclusiveEventMode = gameEvents.CheckForRoomEvents();

                if (inExclusiveEventMode)
                {
                    // If an exclusive event started, it takes over the input
                    // No further action needed here, the event loop will handle it.
                }
            }
        }
        else
        {
            Console.WriteLine("You can't go that way.");
        }
    }

    static void Main()
    {
        Console.WriteLine("------------------------------------");
        Console.WriteLine("Welcome to the Text-RPG!");
        Console.WriteLine("------------------------------------");

        Console.WriteLine("\nChoose an option:");
        Console.WriteLine("1. New Game");
        Console.WriteLine("2. Load Game");
        Console.Write("Enter your choice (1 or 2): ");

        string choice = Console.ReadLine().Trim();

        SaveGameData loadedData = null;

        if (choice == "2")
        {
            loadedData = saveGameManager.LoadGame();
            if (loadedData == null)
            {
                Console.WriteLine("\nCould not load game. Starting a New Game instead.");
            }
        }
        else if (choice != "1")
        {
            Console.WriteLine("\nInvalid choice. Starting a New Game by default.");
        }

        // Initialize game components based on loaded data or new game
        if (loadedData != null)
        {
            currentRoom = objectManager.GetRoom(loadedData.CurrentRoomName);
            player = new Player(loadedData.PlayerName); // Re-initialize player
            foreach (var itemName in loadedData.PlayerInventoryItemNames)
            {
                Item item = objectManager.GetItem(itemName);
                if (item != null)
                {
                    player.Inventory.Add(item); // Add loaded items to inventory
                }
            }

            // Restore room exit locked states
            foreach (var roomEntry in loadedData.RoomExitLockedStates)
            {
                Room room = objectManager.GetRoom(roomEntry.Key);
                if (room != null)
                {
                    foreach (var exitEntry in roomEntry.Value)
                    {
                        if (room.Exits.ContainsKey(exitEntry.Key))
                        {
                            room.Exits[exitEntry.Key].IsLocked = exitEntry.Value;
                        }
                    }
                }
            }

            gameEvents = new Events(currentRoom.Name, player, objectManager);
            gameEvents.SetGuestroomEventCount(loadedData.GuestroomEventCount);
            gameEvents.SetLibraryEventCount(loadedData.LibraryEventCount);
            gameEvents.SetCorridorLibraryDoorUnlocked(loadedData.CorridorLibraryDoorUnlocked);

            Console.WriteLine("Game state restored successfully.");
        }
        else
        {
            currentRoom = objectManager.GetRoom("Hallway"); // Set initial room for new game
            player = new Player("Hero"); // Create new player
            gameEvents = new Events(currentRoom.Name, player, objectManager); // Initialize events for new game
            Console.WriteLine("Starting a new game...");
        }

        // Initialize interactions AFTER currentRoom and player are set
        interactions = new Interactions(player, currentRoom, objectManager);

        DisplayRoom(); // Show the initial or loaded room
        TestItems(); // Add test items (only for development)

        inExclusiveEventMode = gameEvents.CheckForRoomEvents();

        while (true)
        {
            if (inExclusiveEventMode)
            {
                Console.Write("\nEvent-Action: ");
                string eventCommand = Console.ReadLine().ToLower().Trim();
                inExclusiveEventMode = gameEvents.HandleExclusiveEventInput(eventCommand);

                if (!inExclusiveEventMode)
                {
                    if (eventCommand == "untersuchen" && gameEvents.CurrentRoomName == "Guestroom" && currentRoom.Name != "Hallway")
                    {
                        currentRoom = objectManager.GetRoom("Hallway");
                        interactions.UpdateCurrentRoom(currentRoom);
                        gameEvents.UpdateCurrentRoom(currentRoom.Name);
                        DisplayRoom();
                    }
                }
            }
            else
            {
                Console.Write("\nWhat do you want to do? (e.g., go north, look, take item, inventory, use [item] on [direction], combine [item1] [item2], 'quit', 'save', 'load') ");
                string command = Console.ReadLine().ToLower().Trim();

                if (command == "quit")
                {
                    Console.WriteLine("Thanks for playing!");
                    break;
                }
                else if (command == "save")
                {
                    SaveGameData dataToSave = new SaveGameData
                    {
                        PlayerName = player.Name,
                        CurrentRoomName = currentRoom.Name,
                        PlayerInventoryItemNames = player.Inventory.Select(item => item.Name).ToList(),
                        GuestroomEventCount = gameEvents.GetGuestroomEventCount(),
                        LibraryEventCount = gameEvents.GetLibraryEventCount(),
                        CorridorLibraryDoorUnlocked = gameEvents.GetCorridorLibraryDoorUnlocked()
                    };

                    dataToSave.RoomExitLockedStates = new Dictionary<string, Dictionary<string, bool>>();

                    foreach (var roomEntry in objectManager.WorldRooms)
                    {
                        string roomName = roomEntry.Key;
                        Room room = roomEntry.Value;
                        dataToSave.RoomExitLockedStates[roomName] = new Dictionary<string, bool>();
                        foreach (var exitEntry in room.Exits)
                        {
                            dataToSave.RoomExitLockedStates[roomName][exitEntry.Key] = exitEntry.Value.IsLocked;
                        }
                    }
                    saveGameManager.SaveGame(dataToSave);
                }
                else if (command == "load")
                {
                    Console.WriteLine("\nWarning: Loading will overwrite your current progress. Continue? (yes/no)");
                    string confirmLoad = Console.ReadLine().ToLower().Trim();
                    if (confirmLoad == "yes")
                    {
                        SaveGameData reloadedData = saveGameManager.LoadGame();
                        if (reloadedData != null)
                        {
                            currentRoom = objectManager.GetRoom(reloadedData.CurrentRoomName);
                            player.Inventory.Clear();
                            foreach (var itemName in reloadedData.PlayerInventoryItemNames)
                            {
                                Item item = objectManager.GetItem(itemName);
                                if (item != null)
                                {
                                    player.Inventory.Add(item);
                                }
                            }

                            foreach (var roomEntry in objectManager.WorldRooms)
                            {
                                Room room = roomEntry.Value;
                                if (reloadedData.RoomExitLockedStates.ContainsKey(room.Name))
                                {
                                    foreach (var exitEntry in reloadedData.RoomExitLockedStates[room.Name])
                                    {
                                        if (room.Exits.ContainsKey(exitEntry.Key))
                                        {
                                            room.Exits[exitEntry.Key].IsLocked = exitEntry.Value;
                                        }
                                    }
                                }
                            }

                            gameEvents.UpdateCurrentRoom(reloadedData.CurrentRoomName);
                            gameEvents.SetGuestroomEventCount(reloadedData.GuestroomEventCount);
                            gameEvents.SetLibraryEventCount(reloadedData.LibraryEventCount);
                            gameEvents.SetCorridorLibraryDoorUnlocked(reloadedData.CorridorLibraryDoorUnlocked);

                            interactions.UpdateCurrentRoom(currentRoom);
                            Console.WriteLine("Game state has been successfully loaded.");
                            DisplayRoom();
                            inExclusiveEventMode = gameEvents.CheckForRoomEvents();
                        }
                        else
                        {
                            Console.WriteLine("Could not load game. No save file found or data is corrupted.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Load cancelled.");
                    }
                }
                else if (command == "inventory")
                {
                    player.DisplayInventory();
                }
                else if (command == "look")
                {
                    DisplayRoom();
                }
                else if (command.StartsWith("look "))
                {
                    string targetItemName = command.Substring("look ".Length).Trim();
                    Item foundItem = null;
                    foundItem = currentRoom.ItemsInRoom.Find(item => item.Name.ToLower() == targetItemName.ToLower());
                    if (foundItem == null)
                    {
                        foundItem = player.Inventory.Find(item => item.Name.ToLower() == targetItemName.ToLower());
                    }
                    if (foundItem != null)
                    {
                        Console.WriteLine($"You look closely at the {foundItem.Name}. {foundItem.Description}");
                        if (foundItem.ItemsInBox.Any())
                        {
                            Console.WriteLine($"Inside the {foundItem.Name}, you see:");
                            foreach (var containedItem in foundItem.ItemsInBox)
                            {
                                Console.WriteLine($"- {containedItem.Name}");
                            }
                        }
                        else if (!foundItem.Moveable)
                        {
                            Console.WriteLine($"The {foundItem.Name} appears to be empty.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"You don't see a '{targetItemName}' here or in your inventory.");
                    }
                }
                else if (command.StartsWith("take "))
                {
                    string itemName = command.Substring("take ".Length).Trim();
                    Item itemToTake = null;
                    Item containerItem = null;

                    itemToTake = currentRoom.ItemsInRoom.Find(item => item.Name.ToLower() == itemName.ToLower());

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
                else if (command.StartsWith("combine ")) // Updated parsing for "combine item1 and item2"
                {
                    string remainder = command.Substring("combine ".Length).Trim();
                    string item1Name;
                    string item2Name;

                    int andIndex = remainder.IndexOf(" and ");
                    int withIndex = remainder.IndexOf(" with ");

                    if (andIndex != -1)
                    {
                        item1Name = remainder.Substring(0, andIndex).Trim();
                        item2Name = remainder.Substring(andIndex + " and ".Length).Trim();
                    }
                    else if (withIndex != -1)
                    {
                        item1Name = remainder.Substring(0, withIndex).Trim();
                        item2Name = remainder.Substring(withIndex + " with ".Length).Trim();
                    }
                    else
                    {
                        // Fallback for "combine item1 item2" if no 'and'/'with' is found
                        string[] parts = remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2) // At least two words after "combine"
                        {
                            item1Name = parts[0];
                            // If item names can be multi-word and no 'and'/'with' is used, this simple split is insufficient.
                            // For now, take the rest as item2. This is a common simplification for text parsers.
                            item2Name = string.Join(" ", parts.Skip(1));
                        }
                        else
                        {
                            Console.WriteLine("Invalid 'combine' command. Try 'combine [item1_name] and [item2_name]' or 'combine [item1_name] [item2_name]'.");
                            continue;
                        }
                    }
                    
                    Item item1 = player.Inventory.FirstOrDefault(i => i.Name.Equals(item1Name, StringComparison.OrdinalIgnoreCase));
                    Item item2 = player.Inventory.FirstOrDefault(i => i.Name.Equals(item2Name, StringComparison.OrdinalIgnoreCase));

                    if (item1 != null && item2 != null)
                    {
                        TryCombineItems(item1, item2); // Call the generic combination handler
                    }
                    else
                    {
                        Console.WriteLine("You need to have both items in your inventory to combine them.");
                        if (item1 == null) Console.WriteLine($"You don't have '{item1Name}' in your inventory.");
                        if (item2 == null) Console.WriteLine($"You don't have '{item2Name}' in your inventory.");
                    }
                }
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
    }
}