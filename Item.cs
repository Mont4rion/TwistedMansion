public class Item
{
    public string Name { get; private set; }
    public string Description { get; set; }
    public bool Moveable { get; private set; }
    public List<Item> ItemsInBox { get; private set; } = new List<Item>(); // For containers

    // This static list will hold the "test items" that ObjectManager populates.
    public static List<Item> ItemsToTest { get; } = new List<Item>(); // <-- This line

    public Item(string name, string description, bool moveable)
    {
        Name = name;
        Description = description;
        Moveable = moveable;
    }
}