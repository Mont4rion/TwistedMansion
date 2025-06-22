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