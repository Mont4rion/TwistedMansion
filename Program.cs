using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq; // Für .Any() und .Select()

// Start der GameLogic Klasse
public class GameLogic
{
    static Room currentRoom;
    static ObjectManager objectManager;
    static Player player;
    static Interactions interactions;
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
            gameEvents.SetLibraryEventTriggered(loadedData.LibraryEventTriggered);
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
                        LibraryEventTriggered = gameEvents.GetLibraryEventTriggered(),
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
                            gameEvents.SetLibraryEventTriggered(reloadedData.LibraryEventTriggered);
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
        Room bathroom = new Room("Bathroom", "A sterile bathroom with a chipped porcelain sink and a mirror showing your tired reflection.");
        Room corridor = new Room("Corridor", "A narrow, dusty corridor, seemingly long disused. Light barely penetrates here."); // New: Corridor

        WorldRooms.Add("Hallway", hallway);
        WorldRooms.Add("Library", library);
        WorldRooms.Add("Kitchen", kitchen);
        WorldRooms.Add("Guestroom", guestroom);
        WorldRooms.Add("Bathroom", bathroom);
        WorldRooms.Add("Corridor", corridor);

        WorldRooms["Hallway"].AddExit("north", WorldRooms["Library"]);
        WorldRooms["Hallway"].AddExit("east", WorldRooms["Kitchen"]);

        WorldRooms["Library"].AddExit("south", WorldRooms["Hallway"]);
        // GEÄNDERT: requiredItem ist nur für die Logik, nicht für die Ausgabe
        WorldRooms["Library"].AddExit("east", WorldRooms["Corridor"], isLocked: true, requiredItem: "Blocked Path");

        WorldRooms["Kitchen"].AddExit("west", WorldRooms["Hallway"]);
        // GEÄNDERT: requiredItem ist nur für die Logik, nicht für die Ausgabe
        WorldRooms["Kitchen"].AddExit("south", WorldRooms["Guestroom"], isLocked: true, requiredItem: "Rusty Key");

        WorldRooms["Guestroom"].AddExit("north", WorldRooms["Kitchen"]);
        WorldRooms["Guestroom"].AddExit("east", WorldRooms["Bathroom"]);

        WorldRooms["Bathroom"].AddExit("west", WorldRooms["Guestroom"]);
        // New: Exit from Bathroom to Corridor, not locked
        WorldRooms["Bathroom"].AddExit("north", WorldRooms["Corridor"]);

        WorldRooms["Corridor"].AddExit("south", WorldRooms["Bathroom"]);
        // New: Exit from Corridor to Library (will be unlocked by event)
        WorldRooms["Corridor"].AddExit("west", WorldRooms["Library"]);

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

        Item kitchenShelf = new Item("Kitchen Shelf", "An old, worn-out shelf fixed to the wall. It looks like it could hold items.", false);
        WorldItems.Add("Kitchen Shelf", kitchenShelf);

        WorldRooms["Hallway"].ItemsInRoom.Add(rustyKey);
        WorldRooms["Library"].ItemsInRoom.Add(oldBook);
        WorldRooms["Kitchen"].ItemsInRoom.Add(kitchenShelf);

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
        if (Inventory.Any())
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
    public Dictionary<string, Exit> Exits { get; set; }
    public List<Item> ItemsInRoom { get; set; }

    public Room(string name, string description)
    {
        this.Name = name;
        this.Description = description;
        this.Exits = new Dictionary<string, Exit>();
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
                // GEÄNDERT: requiredItem wird NICHT mehr angezeigt
                exitDescriptions.Add($"'{direction}' leads to a locked {exit.TargetRoom.Name} door");
            }
            else
            {
                exitDescriptions.Add($"'{direction}' leads to the {exit.TargetRoom.Name}");
            }
        }
        if (exitDescriptions.Any())
        {
             return "You see exits: " + string.Join(", ", exitDescriptions) + ".";
        }
        else
        {
            return "There are no obvious exits.";
        }
    }
}

// ---
public class Exit
{
    public Room TargetRoom { get; set; }
    public bool IsLocked { get; set; }
    public string RequiredItem { get; set; } // Name of the item needed to unlock (internal for logic)

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

// ---
public class Events
{
    public string CurrentRoomName { get; private set; }
    private int guestroomEventCount = 0;
    private bool libraryEventTriggered = false;
    private bool corridorLibraryDoorUnlocked = false;
    private Player _player;
    private ObjectManager _objectManager;

    public int GetGuestroomEventCount() { return guestroomEventCount; }
    public void SetGuestroomEventCount(int value) { guestroomEventCount = value; }

    public bool GetLibraryEventTriggered() { return libraryEventTriggered; }
    public void SetLibraryEventTriggered(bool value) { libraryEventTriggered = value; }

    public bool GetCorridorLibraryDoorUnlocked() { return corridorLibraryDoorUnlocked; }
    public void SetCorridorLibraryDoorUnlocked(bool value) { corridorLibraryDoorUnlocked = value; }


    public Events(string initialRoomName, Player player, ObjectManager objectManager)
    {
        CurrentRoomName = initialRoomName;
        _player = player;
        _objectManager = objectManager;
    }

    public void UpdateCurrentRoom(string newRoomName)
    {
        CurrentRoomName = newRoomName;
    }

    public bool CheckForRoomEvents()
    {
        if (CurrentRoomName == "Guestroom")
        {
            if (guestroomEventCount == 0)
            {
                Console.WriteLine("\n------------------------------------");
                Console.WriteLine("Ein unerklärlicher, eisiger Hauch umgibt dich, als du das Gästezimmer betrittst.");
                Console.WriteLine("Du hörst ein leises, beunruhigendes Wispern, das direkt aus den Schatten zu kommen scheint.");
                Console.WriteLine("------------------------------------");
                Console.WriteLine("Was tust du? (untersuchen / ignorieren)");

                guestroomEventCount = 1;
                return true;
            }
        }
        else if (CurrentRoomName == "Library")
        {
            if (!libraryEventTriggered)
            {
                if (_player.Inventory.Any(item => item.Name.ToLower() == "kitchen knife"))
                {
                    Console.WriteLine("\n------------------------------------");
                    Console.WriteLine("Als du die Bibliothek betrittst, fällt dein Blick auf ein seltsam leuchtendes Buch.");
                    Console.WriteLine("Es pulsiert sanft auf einem der oberen Regale. ");
                    Console.WriteLine("Mit dem Küchenmesser könntest du versuchen, es herunterzuholen.");
                    Console.WriteLine("------------------------------------");
                    Console.WriteLine("Möchtest du versuchen, es zu erreichen? (ja / nein)");

                    libraryEventTriggered = true;
                    return true;
                }
                else
                {
                    Console.WriteLine("Die Bibliothek ist still, abgesehen vom Rascheln der Blätter im Wind.");
                }
            }
        }
        else if (CurrentRoomName == "Corridor")
        {
            if (!corridorLibraryDoorUnlocked)
            {
                Console.WriteLine("\n------------------------------------");
                Console.WriteLine("Du bemerkst, dass die Wand zum Westen hin rissig ist. Ein schwacher Lichtschein dringt hindurch.");
                Console.WriteLine("Es scheint, als ob man von dieser Seite aus einen Weg in die Bibliothek schaffen könnte.");
                Console.WriteLine("------------------------------------");

                Room libraryRoom = _objectManager.GetRoom("Library");
                if (libraryRoom != null && libraryRoom.Exits.ContainsKey("east"))
                {
                    libraryRoom.Exits["east"].IsLocked = false;
                    libraryRoom.Exits["east"].RequiredItem = null; // Entferne den RequiredItem-Hinweis hier auch
                    Console.WriteLine("Du schiebst ein lose sitzendes Brett beiseite und hörst ein Klicken.");
                    Console.WriteLine("Der Weg zur Bibliothek ist nun frei!");
                }
                corridorLibraryDoorUnlocked = true;
                return false;
            }
        }
        return false;
    }

    public bool HandleExclusiveEventInput(string command)
    {
        if (CurrentRoomName == "Guestroom")
        {
            if (guestroomEventCount == 1)
            {
                if (command == "untersuchen")
                {
                    Console.WriteLine("Du versuchst, die Quelle des Wisperns zu finden. Der Raum scheint sich zu verdunkeln.");
                    Console.WriteLine("Plötzlich spürst du eine kalte Hand an deinem Nacken. Ein Schrei entweicht dir!");
                    Console.WriteLine("Du stolperst rückwärts und fliehst panisch aus dem Raum.");
                    guestroomEventCount = 2;
                    return false;
                }
                else if (command == "ignorieren")
                {
                    Console.WriteLine("Du versuchst, das Wispern zu ignorieren und schnell den Raum zu verlassen.");
                    Console.WriteLine("Die Stimmen werden lauter, aber du schaffst es, die Tür hinter dir zu schließen.");
                    guestroomEventCount = 2;
                    return false;
                }
                // Ergänzung für den Fall, dass kein passender Befehl eingegeben wird
                else
                {
                    Console.WriteLine("Das ist keine gültige Aktion für dieses Event.");
                    return true; // Bleibe im Event-Modus
                }
            }
        }
        else if (CurrentRoomName == "Library")
        {
            if (libraryEventTriggered)
            {
                if (command == "ja")
                {
                    if (_player.Inventory.Any(item => item.Name.ToLower() == "kitchen knife"))
                    {
                        Console.WriteLine("Du benutzt das Küchenmesser, um das leuchtende Buch vom Regal zu hebeln.");
                        Item glowingBook = _objectManager.GetItem("Old Book"); // Annahme: "Old Book" ist das leuchtende Buch
                        if (glowingBook != null)
                        {
                            Console.WriteLine($"Das {glowingBook.Name} fällt in deine Hände.");
                            // Entferne das Buch aus dem Raum, falls es noch dort ist (sollte es nicht, da es "genommen" wird)
                            _objectManager.GetRoom("Library").ItemsInRoom.Remove(glowingBook);
                            _player.AddItem(glowingBook); // Füge es dem Inventar hinzu
                        }
                        Console.WriteLine("Das Leuchten des Buches erlischt, sobald du es in der Hand hältst.");
                        libraryEventTriggered = false; // Event ist abgeschlossen
                        return false; // Beende den exklusiven Event-Modus
                    }
                    else
                    {
                        Console.WriteLine("Du hast nichts, womit du das Buch erreichen könntest.");
                        return true; // Bleibe im Event-Modus, bis der Spieler eine andere Aktion wählt oder das richtige Item hat
                    }
                }
                else if (command == "nein")
                {
                    Console.WriteLine("Du lässt das leuchtende Buch vorerst in Ruhe.");
                    libraryEventTriggered = false; // Event ist abgeschlossen
                    return false; // Beende den exklusiven Event-Modus
                }
                else
                {
                    Console.WriteLine("Das ist keine gültige Aktion für dieses Event. (ja / nein)");
                    return true; // Bleibe im Event-Modus
                }
            }
        }
        return false; // Event ist nicht aktiv oder wurde abgeschlossen
    }
}

// ---
// SaveGameData: Eine einfache Klasse zum Speichern des Spielstands
// Benötigt System.Text.Json NuGet-Paket
public class SaveGameData
{
    public string PlayerName { get; set; }
    public string CurrentRoomName { get; set; }
    public List<string> PlayerInventoryItemNames { get; set; } = new List<string>();
    public Dictionary<string, Dictionary<string, bool>> RoomExitLockedStates { get; set; } = new Dictionary<string, Dictionary<string, bool>>();

    // Event-Zustände
    public int GuestroomEventCount { get; set; }
    public bool LibraryEventTriggered { get; set; }
    public bool CorridorLibraryDoorUnlocked { get; set; }
}

// ---
// SaveGameManager: Verwaltet das Speichern und Laden von Spielständen
// Benötigt System.Text.Json NuGet-Paket
public class SaveGameManager
{
    private const string SaveFileName = "savegame.json";

    public void SaveGame(SaveGameData data)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(SaveFileName, jsonString);
            Console.WriteLine("\nGame saved successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving game: {ex.Message}");
        }
    }

    public SaveGameData LoadGame()
    {
        if (File.Exists(SaveFileName))
        {
            try
            {
                string jsonString = File.ReadAllText(SaveFileName);
                SaveGameData data = JsonSerializer.Deserialize<SaveGameData>(jsonString);
                Console.WriteLine("Game loaded successfully!");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading game: {ex.Message}");
                return null;
            }
        }
        else
        {
            Console.WriteLine("No saved game found.");
            return null;
        }
    }
}