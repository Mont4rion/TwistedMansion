using System;
using System.Collections.Generic; // Make sure this is present

public class ObjectManager
{
    public Dictionary<string, Room> WorldRooms { get; private set; }
    public Dictionary<string, Item> WorldItems { get; private set; }
    public Dictionary<string, Kombinations> WorldKombinations { get; private set; }

    public ObjectManager()
    {
        WorldRooms = new Dictionary<string, Room>();
        WorldItems = new Dictionary<string, Item>();
        WorldKombinations = new Dictionary<string, Kombinations>();

        // IMPORTANT: Removed initialization calls from here.
        // GameLogic's static constructor will now call InitializeRooms(), InitializeItems(), etc.
        // This prevents double initialization and centralizes the game's startup sequence in GameLogic.
    }

    public void InitializeRooms()
    {
        Room hallway = new Room("Hallway", "You are in a long, dimly lit hallway. The air smells musty.");
        Room library = new Room("Library", "A vast library filled with dusty tomes. A faint glow emanates from a corner.");
        Room kitchen = new Room("Kitchen", "A messy kitchen with an unwashed pan in the sink. The lingering scent of burnt toast hangs heavy.");
        Room guestroom = new Room("Guestroom", "A small and messy Bedroom.");
        Room bathroom = new Room("Bathroom", "A sterile bathroom with a chipped porcelain sink and a mirror showing your tired reflection.");
        Room corridor = new Room("Corridor", "A narrow, dusty corridor, seemingly long disused. Light barely penetrates here.");

        WorldRooms.Add("Hallway", hallway);
        WorldRooms.Add("Library", library);
        WorldRooms.Add("Kitchen", kitchen);
        WorldRooms.Add("Guestroom", guestroom);
        WorldRooms.Add("Bathroom", bathroom);
        WorldRooms.Add("Corridor", corridor);

        WorldRooms["Hallway"].AddExit("north", WorldRooms["Library"]);
        WorldRooms["Hallway"].AddExit("east", WorldRooms["Kitchen"]);

        WorldRooms["Library"].AddExit("south", WorldRooms["Hallway"]);
        WorldRooms["Library"].AddExit("east", WorldRooms["Corridor"], isLocked: true, requiredItem: "Blocked Path");

        WorldRooms["Kitchen"].AddExit("west", WorldRooms["Hallway"]);
        WorldRooms["Kitchen"].AddExit("south", WorldRooms["Guestroom"], isLocked: true, requiredItem: "Rusty Key");

        WorldRooms["Guestroom"].AddExit("north", WorldRooms["Kitchen"]);
        WorldRooms["Guestroom"].AddExit("east", WorldRooms["Bathroom"]);

        WorldRooms["Bathroom"].AddExit("west", WorldRooms["Guestroom"]);
        WorldRooms["Bathroom"].AddExit("north", WorldRooms["Corridor"]);

        WorldRooms["Corridor"].AddExit("south", WorldRooms["Bathroom"]);
        WorldRooms["Corridor"].AddExit("west", WorldRooms["Library"]);

        Console.WriteLine("Game world (rooms and exits) initialized successfully.");
    }

    // In ObjectManager.cs

    public void InitializeKombinations()
    {
        Console.WriteLine("Initializing Kombinations...");

        // Important: The "Butterfly" string here acts as a generic placeholder
        // for ANY butterfly. The TryCombineItems logic handles this.
        WorldKombinations.Add("Frame with one butterfly in it", new Kombinations(
            "Frame with one butterfly in it",
            "An empty wooden frame with a glass panel and one beautiful butterfly.",
            true,
            new List<string> { "Empty Frame", "Butterfly" } // "Butterfly" is the generic type
        ));

        WorldKombinations.Add("Frame with two butterflies in it", new Kombinations(
            "Frame with two butterflies in it",
            "An empty wooden frame with a glass panel and two beautiful butterflies.",
            true,
            new List<string> { "Frame with one butterfly in it", "Butterfly" }
        ));

        WorldKombinations.Add("Frame with three butterflies in it", new Kombinations(
            "Frame with three butterflies in it",
            "An empty wooden frame with a glass panel and three beautiful butterflies.",
            true,
            new List<string> { "Frame with two butterflies in it", "Butterfly" }
        ));

        WorldKombinations.Add("Frame with four butterflies in it", new Kombinations(
            "Frame with four butterflies in it",
            "An empty wooden frame with a glass panel and all four beautiful butterflies.",
            true,
            new List<string> { "Frame with three butterflies in it", "Butterfly" }
        ));

        Console.WriteLine("Kombinations initialized successfully.");
    }

    public void InitializeItems()
    {
        Item rustyKey = new Item("Rusty Key", "A very old, rusty key. It looks like it might open something.", true);
        WorldItems.Add("Rusty Key", rustyKey);

        Item oldBook = new Item("Old Book", "Als du die Bibliothek betrittst, fällt dein Blick auf ein seltsam leuchtendes Buch. Es pulsiert sanft auf einem der oberen Regale.", false);
        WorldItems.Add("Old Book", oldBook);

        Item kitchenKnife = new Item("Kitchen Knife", "A dull and brittle knife.", true);
        WorldItems.Add("Kitchen Knife", kitchenKnife);

        Item kitchenShelf = new Item("Kitchen Shelf", "An old, worn-out shelf fixed to the wall. It looks like it could hold items.", false);
        WorldItems.Add("Kitchen Shelf", kitchenShelf);

        Item frame = new Item("Empty Frame", "An empty wooden frame with a glass panel and 4 pins stuck in it.", true); // Changed name to "Empty Frame" for clarity with combinations
        WorldItems.Add("Empty Frame", frame); // Use "Empty Frame" as key

        Item butterflyBlue = new Item("Butterfly Blue", "A blue butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Blue", butterflyBlue);

        Item butterflyRed = new Item("Butterfly Red", "A red butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Red", butterflyRed);

        Item butterflyGreen = new Item("Butterfly Green", "A green butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Green", butterflyGreen);

        Item butterflyBlack = new Item("Butterfly Black", "A black butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Black", butterflyBlack);

        // Add the combined items directly to WorldItems as well, so they can exist in the game world
        // This is crucial if these are items players can gain and lose.
        // Their details (description, combinability) are defined within the Kombinations objects above.
        // Here, we just ensure the Item objects exist in WorldItems.
        // NOTE: The description below is just a placeholder. The Kombinations class should manage the final description.
        WorldItems.Add("Frame with one butterfly in it", new Item("Frame with one butterfly in it", "A frame with one butterfly attached.", true));
        WorldItems.Add("Frame with two butterflies in it", new Item("Frame with two butterflies in it", "A frame with two butterflies attached.", true));
        WorldItems.Add("Frame with three butterflies in it", new Item("Frame with three butterflies in it", "A frame with three butterflies attached.", true));
        WorldItems.Add("Frame with four butterflies in it", new Item("Frame with four butterflies in it", "A frame with four butterflies attached.", true));


        WorldRooms["Hallway"].ItemsInRoom.Add(rustyKey);
        WorldRooms["Library"].ItemsInRoom.Add(oldBook);
        WorldRooms["Kitchen"].ItemsInRoom.Add(kitchenShelf);

        kitchenShelf.ItemsInBox.Add(kitchenKnife);

        // Populate Item.ItemsToTest with items if needed for debugging or specific test scenarios.
        // Consider if this list should be cleared before adding if InitializeItems can be called multiple times.
        Item.ItemsToTest.Add(frame); // This is now "Empty Frame"
        Item.ItemsToTest.Add(butterflyBlue);
        Item.ItemsToTest.Add(butterflyGreen);
        Item.ItemsToTest.Add(butterflyRed);
        Item.ItemsToTest.Add(butterflyBlack);

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