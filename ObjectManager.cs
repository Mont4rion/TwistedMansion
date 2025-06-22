public class ObjectManager
{
    public Dictionary<string, Room> WorldRooms { get; private set; }
    public Dictionary<string, Item> WorldItems { get; private set; }
    public Item Item;

    public ObjectManager()
    {
        WorldRooms = new Dictionary<string, Room>();
        WorldItems = new Dictionary<string, Item>();

        // Important: ItemsToTest is a STATIC list. It will only be initialized ONCE
        // even if you create multiple ObjectManager instances.
        // It's populated in InitializeItems.
        InitializeRooms();
        InitializeItems();
        // Crucially, we DO NOT call EquipPlayerWithTestItems here.
        // The ObjectManager's job is to set up the world, not to manage players directly.
    }

    private void InitializeRooms()
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

    private void InitializeItems()
    {
        Item rustyKey = new Item("Rusty Key", "A very old, rusty key. It looks like it might open something.", true);
        WorldItems.Add("Rusty Key", rustyKey);

        Item oldBook = new Item("Old Book", "Als du die Bibliothek betrittst, fällt dein Blick auf ein seltsam leuchtendes Buch. Es pulsiert sanft auf einem der oberen Regale. ", false);
        WorldItems.Add("Old Book", oldBook);

        Item kitchenKnife = new Item("Kitchen Knife", "A dull and brittle knife.", true);
        WorldItems.Add("Kitchen Knife", kitchenKnife);

        Item kitchenShelf = new Item("Kitchen Shelf", "An old, worn-out shelf fixed to the wall. It looks like it could hold items.", false);
        WorldItems.Add("Kitchen Shelf", kitchenShelf);

        Item frame = new Item("Frame", "A empty wooden frame with a glass pannel and 4 pins sticked in it", true);
        WorldItems.Add("Frame", frame);

        Item butterflyBlue = new Item("Butterfly Blue", "A blue butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Blue", butterflyBlue);

        Item butterflyRed = new Item("Butterfly Red", "A red butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Red", butterflyRed);

        Item butterflyGreen = new Item("Butterfly Green", "A green butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Green", butterflyGreen);

        Item butterflyBlack = new Item("Butterfly Black", "A black butterfly, you can't miss it is rare", true);
        WorldItems.Add("Butterfly Black", butterflyBlack);

        WorldRooms["Hallway"].ItemsInRoom.Add(rustyKey);
        WorldRooms["Library"].ItemsInRoom.Add(oldBook);
        WorldRooms["Kitchen"].ItemsInRoom.Add(kitchenShelf);

        kitchenShelf.ItemsInBox.Add(kitchenKnife);

        // --- Test Items Population ---
        // This is where the static list Item.ItemsToTest is populated.
        // It's crucial to understand that this list is global and will retain its contents
        // across different ObjectManager instances (though you typically only have one).
        // If you call InitializeItems multiple times, these items will be added repeatedly
        // to Item.ItemsToTest unless you clear it first.
        // For a game, this kind of static global list for "test items" is often
        // better handled by a dedicated debug or utility class.
        Item.ItemsToTest.Add(frame);
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