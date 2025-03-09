using System.Collections.Generic;
using System.Linq;
using ChiselingOverhaul.Item;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ChiselingOverhaul.Items;

public class BitPouch
{
    
    // Materials in the pouch
    private readonly IDictionary<int, int> _materials;
    // Original ItemStack
    private readonly ItemStack _stack;

    // TODO Not sure where to hold the methods for programatic object and in-game object overrides.
    public BitPouch(ItemStack itemStack)
    {
        _stack = itemStack;
        var attributes = _stack.Attributes.GetOrAddTreeAttribute("chiseling-overhaul-pouch-content");
        var materialQuantities = attributes.SortedCopy()
            .ToDictionary(attribute => int.Parse(attribute.Key), attribute => (int)attribute.Value.GetValue());
        _materials = materialQuantities;
    }
	
    public bool ContainsMaterial(int material, int quantity = 1)
    {
        if (!_materials.TryGetValue(material, out int value))
            return false;
        return value >= quantity;
    }

    public void AddMaterial(int materialId, int quantity)
    {
        _materials.TryGetValue(materialId, out int oldQuantity);
        _materials[materialId] = oldQuantity + quantity;
    }
	
    public void UpdateItemStack()
    {
        var tree = _stack.Attributes.GetOrAddTreeAttribute("chiseling-overhaul-pouch-content");
        foreach (var attribute in tree)
        {
            tree.RemoveAttribute(attribute.Key);
        }
        foreach (var material in _materials)
        {
            tree.SetInt(material.Key.ToString(), material.Value);
        }

        // TODO Mark Dirty or sth
    }
    
    public void TakeMaterial(int materialId, int quantity)
    {

        if (!_materials.TryGetValue(materialId, out int oldQuantity))
        {
            return;
        }
        if (oldQuantity > quantity)
        {
            _materials[materialId] = oldQuantity - quantity;
        } else if (oldQuantity == quantity)
        {
            _materials.Remove(materialId);
        }
    }

    public static Dictionary<int, int> GetMaterials(ItemStack pouch)
    {
        var attributes = pouch.Attributes.GetOrAddTreeAttribute("chiseling-overhaul-pouch-content");
        return attributes.ToDictionary(att => int.Parse(att.Key), att => (int)att.Value.GetValue());
    }
}