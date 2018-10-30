using System.Collections.Generic;
using System.Linq;
namespace Poke1Protocol
{
    public class InventoryItem
    {
        public string Name => ItemsManager.Instance.ItemClass.items.ToList().Find(x => x.ID == Id).Name;
        public int Id { get; }
        public int Quantity { get; }
        public PSXAPI.Response.ItemPocket Pocket { get; }
        public PSXAPI.Response.ItemTarget Target { get; }
        public PSXAPI.Response.ItemCategory Category { get; }
        public PSXAPI.Response.InventoryItem Data { get; }
        public bool CanBeHeld => Data.CanHold;
        public bool CanBeUsedOutsideOfBattle => Data.CanUseOutsideBattle;
        public bool CanBeUsedOnPokemonOutsideOfBattle => Data.CanUseOutsideBattle && (Data.Target == PSXAPI.Response.ItemTarget.Pokemon || Data.Target == PSXAPI.Response.ItemTarget.Move);
        public bool CanBeUsedInBattle => Data.CanUseInBattle;
        public bool CanBeUsedOnPokemonInBattle => CanBeUsedInBattle &&
            (Category == PSXAPI.Response.ItemCategory.Medicine || Category == PSXAPI.Response.ItemCategory.Berry) 
            && (Data.Target == PSXAPI.Response.ItemTarget.Pokemon || Data.Target == PSXAPI.Response.ItemTarget.Move);
        public bool IsTradeAble => Data.CanTrade;
        public InventoryItem(PSXAPI.Response.InventoryItem data)
        {
            Id = data.ItemID;
            Pocket = data.Pocket;
            Target = data.Target;
            Category = data.Category;
            Quantity = (int)data.Count;
            Data = data;
        }

        public bool IsPokeball() => Category == PSXAPI.Response.ItemCategory.Pokeball;
    }
}
