using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Reflection.Metadata.Ecma335; // Für .Any() und .Select()

// Start der GameLogic Klasse
public class GameLogic
{
    static Room currentRoom;
    static ObjectManager objectManager;
    static Player player;
    static Interactions interactions;
    static Item item;
    static Events gameEvents;
    static bool inExclusiveEventMode = false;
    static SaveGameManager saveGameManager;

    static void Main()
    {
        Console.WriteLine("------------------------------------");
        Console.WriteLine("Welcome to the Text-RPG!");
        Console.WriteLine("------------------------------------");

        // ObjectManager und SaveGameManager müssen immer als Erstes initialisiert werden
        objectManager = new ObjectManager();
        saveGameManager = new SaveGameManager();

        // --- Startbildschirm ---
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
                // Wenn Laden fehlschlägt, starten wir trotzdem ein neues Spiel.
            }
        }
        else if (choice != "1")
        {
            Console.WriteLine("\nInvalid choice. Starting a New Game by default.");
        }

        // --- Spielzustand initialisieren oder laden ---
        if (loadedData != null)
        {
            // Spielzustand aus geladenen Daten wiederherstellen
            currentRoom = objectManager.GetRoom(loadedData.CurrentRoomName);
            player = new Player(loadedData.PlayerName);
            foreach (var itemName in loadedData.PlayerInventoryItemNames)
            {
                Item item = objectManager.GetItem(itemName);
                if (item != null)
                {
                    player.Inventory.Add(item);
                }
            }

            // Exits-Zustände wiederherstellen
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

            // Event-Zustände wiederherstellen
            gameEvents = new Events(loadedData.CurrentRoomName, player, objectManager);
            gameEvents.SetGuestroomEventCount(loadedData.GuestroomEventCount);
            gameEvents.SetLibraryEventCount(loadedData.LibraryEventCount);
            gameEvents.SetCorridorLibraryDoorUnlocked(loadedData.CorridorLibraryDoorUnlocked);

            Console.WriteLine("Game state restored successfully.");
        }
        else
        {
            // Neues Spiel starten
            currentRoom = objectManager.GetRoom("Hallway"); // Startraum für neues Spiel
            player = new Player("Hero"); // Standardspieler für neues Spiel
            gameEvents = new Events(currentRoom.Name, player, objectManager);
            Console.WriteLine("Starting a new game...");
        }

        // Interactions muss immer nach currentRoom und player initialisiert werden
        interactions = new Interactions(player, currentRoom, objectManager);

        DisplayRoom(); // Zeige den initialen oder geladenen Raum an
        TestItems();


        // Checkt, ob beim Start ein Event im aktuellen Raum triggert (z.B. im Hallway)
        inExclusiveEventMode = gameEvents.CheckForRoomEvents();

        // --- Hauptspielschleife ---
        while (true)
        {
            if (inExclusiveEventMode)
            {
                Console.Write("\nEvent-Action: ");
                string eventCommand = Console.ReadLine().ToLower().Trim();
                inExclusiveEventMode = gameEvents.HandleExclusiveEventInput(eventCommand);

                if (!inExclusiveEventMode)
                {
                    // Spezialfall für Guestroom-Event: wenn Flucht, Raumwechsel erzwingen
                    if (eventCommand == "untersuchen" && gameEvents.CurrentRoomName == "Guestroom" && currentRoom.Name != "Hallway")
                    {
                        currentRoom = objectManager.GetRoom("Hallway");
                        interactions.UpdateCurrentRoom(currentRoom);
                        gameEvents.UpdateCurrentRoom(currentRoom.Name);
                        DisplayRoom();
                    }
                    // Optional: Nach einem beendeten Event ohne Raumwechsel erneut den Raum beschreiben
                    // DisplayRoom(); // Oder spezifischere Nachrichten je nach Event
                }
            }
            else // Normale Spielschleife
            {
                Console.Write("\nWhat do you want to do? (e.g., go north, look, take item, inventory, use [item] on [direction], 'quit', 'save', 'load') ");
                string command = Console.ReadLine().ToLower().Trim();

                if (command == "quit")
                {
                    Console.WriteLine("Thanks for playing!");
                    break;
                }
                else if (command == "save") // 'save' Befehl
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

                    // Zustand aller Türen speichern
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
                else if (command == "load") // 'load' Befehl
                {
                    Console.WriteLine("\nWarning: Loading will overwrite your current progress. Continue? (yes/no)");
                    string confirmLoad = Console.ReadLine().ToLower().Trim();
                    if (confirmLoad == "yes")
                    {
                        SaveGameData reloadedData = saveGameManager.LoadGame();
                        if (reloadedData != null)
                        {
                            // Spielzustand aus geladenen Daten wiederherstellen
                            currentRoom = objectManager.GetRoom(reloadedData.CurrentRoomName);
                            player.Inventory.Clear(); // Aktuelles Inventar leeren
                            foreach (var itemName in reloadedData.PlayerInventoryItemNames)
                            {
                                Item item = objectManager.GetItem(itemName);
                                if (item != null)
                                {
                                    player.Inventory.Add(item);
                                }
                            }

                            // Exits-Zustände wiederherstellen (setzt alle Türen auf den gespeicherten Zustand)
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

                            // Event-Zustände wiederherstellen
                            gameEvents.UpdateCurrentRoom(reloadedData.CurrentRoomName); // Aktualisiere den Raumnamen im Events-Objekt
                            gameEvents.SetGuestroomEventCount(reloadedData.GuestroomEventCount);
                            gameEvents.SetLibraryEventCount(reloadedData.LibraryEventCount);
                            gameEvents.SetCorridorLibraryDoorUnlocked(reloadedData.CorridorLibraryDoorUnlocked);

                            interactions.UpdateCurrentRoom(currentRoom); // Interactions muss auch aktualisiert werden
                            Console.WriteLine("Game state has been successfully loaded.");
                            DisplayRoom(); // Den geladenen Raum anzeigen
                            inExclusiveEventMode = gameEvents.CheckForRoomEvents(); // Überprüfen, ob ein Event im geladenen Raum triggern soll
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
                else if (command == "look") // Hinzugefügter 'look' Befehl
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
                        if (foundItem.Moveable == false)
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
                        else
                        {
                            Console.WriteLine($"You look closely at the {foundItem.Name}. {foundItem.Description}");
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

    static void DisplayRoom()
    {
        Console.WriteLine($"\n--- {currentRoom.Name.ToUpper()} ---");
        Console.WriteLine(currentRoom.Description);
        Console.WriteLine(currentRoom.GetAvailableExits());
    }

    static void TestItems()
    {
        foreach (var testItem in Item.ItemsToTest)
        {
            player.Inventory.Add(testItem);
        }
    }

    static void MovePlayer(string direction)
    {
        if (currentRoom.Exits.TryGetValue(direction, out Exit exit))
        {
            if (exit.IsLocked)
            {
                // GEÄNDERT: Allgemeine Meldung ohne Item-Name
                Console.WriteLine($"The door is locked. Maybe you need something to open it.");
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
                }
            }
        }
        else
        {
            Console.WriteLine("You can't go that way.");
        }
    }
}

