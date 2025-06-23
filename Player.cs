// Player.cs
using System;
using System.Collections.Generic;
using System.Linq; // Für die Nutzung von LINQ-Methoden wie .FirstOrDefault() oder .Any()

public class Player
{
    public string Name { get; private set; }
    public List<Item> Inventory { get; private set; }

    // Konstruktor des Players
    public Player(string name)
    {
        Name = name;
        Inventory = new List<Item>(); // Wichtig: Inventar hier initialisieren!
    }

    /// <summary>
    /// Fügt dem Inventar des Spielers einen Gegenstand hinzu.
    /// </summary>
    /// <param name="item">Der hinzuzufügende Gegenstand.</param>
    public void AddItem(Item item)
    {
        if (item != null)
        {
            Inventory.Add(item);
            Console.WriteLine($"You picked up the {item.Name}.");
        }
    }

    /// <summary>
    /// Entfernt einen spezifischen Gegenstand aus dem Inventar des Spielers.
    /// </summary>
    /// <param name="itemToRemove">Der zu entfernende Gegenstand.</param>
    public void RemoveItem(Item itemToRemove)
    {
        if (itemToRemove != null && Inventory.Contains(itemToRemove))
        {
            Inventory.Remove(itemToRemove);
            Console.WriteLine($"You used the {itemToRemove.Name}.");
        }
        else if (itemToRemove != null)
        {
            Console.WriteLine($"Error: {itemToRemove.Name} was not found in your inventory to remove.");
        }
    }

    /// <summary>
    /// Entfernt einen Gegenstand anhand seines Namens aus dem Inventar des Spielers.
    /// Dies ist nützlich, wenn man den genauen Objekt-Referenz nicht hat, aber den Namen kennt.
    /// </summary>
    /// <param name="itemName">Der Name des zu entfernenden Gegenstands.</param>
    public void RemoveItemByName(string itemName)
    {
        Item itemFound = Inventory.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (itemFound != null)
        {
            RemoveItem(itemFound);
        }
        else
        {
            Console.WriteLine($"Error: No item named '{itemName}' found in your inventory to remove.");
        }
    }

    /// <summary>
    /// Ruft einen Gegenstand aus dem Inventar anhand seines Namens ab.
    /// </summary>
    /// <param name="itemName">Der Name des Gegenstands.</param>
    /// <returns>Der gefundene Gegenstand oder null, wenn er nicht im Inventar ist.</returns>
    public Item GetItemFromInventory(string itemName)
    {
        return Inventory.FirstOrDefault(item => item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// Zeigt den Inhalt des Spielerinventars an.
    /// </summary>
    public void DisplayInventory()
    {
        if (Inventory.Count == 0)
        {
            Console.WriteLine("Your inventory is empty.");
            return;
        }

        Console.WriteLine("\n--- Your Inventory ---");
        foreach (var item in Inventory)
        {
            Console.WriteLine($"- {item.Name}: {item.Description}");
        }
        Console.WriteLine("----------------------");
    }
}