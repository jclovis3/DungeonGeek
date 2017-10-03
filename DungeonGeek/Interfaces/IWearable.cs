using System.Collections.Generic;

namespace DungeonGeek
{
    interface IWearable
    {
        bool Equipped { get;  set; }
        string WornOn { get; }


        string Equip(List<InventoryItem> replacesItems);

        string Unequip();


    }
}
