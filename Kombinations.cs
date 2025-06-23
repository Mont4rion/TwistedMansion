// Kombinations.cs
using System.Collections.Generic;

public class Kombinations
{
    public string Name { get; set; } // The name of the item that results from the combination
    public string Description { get; set; } // Description of the resulting item
    public bool IsCombinable { get; set; } // Whether the resulting item can be combined further
    public List<string> RequiredItems { get; set; } // List of names of items needed for this combination

    public Kombinations(string name, string description, bool isCombinable, List<string> requiredItems)
    {
        Name = name;
        Description = description;
        IsCombinable = isCombinable;
        RequiredItems = requiredItems ?? new List<string>(); // Ensure it's not null
    }
}