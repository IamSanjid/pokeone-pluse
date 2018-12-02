using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class PokemonMove
    {
        public int Position { get; private set; }
        public int Id { get; private set; }
        public int MaxPoints { get; set; }
        public int CurrentPoints { get; set; }

        private readonly TextInfo ti = CultureInfo.CurrentCulture.TextInfo;

        public MovesManager.MoveData Data => MovesManager.Instance.GetMoveData(Id);

        public string Name => Data?.Name != null ? ti.ToTitleCase(Data?.Name) : Data?.Name;

        public string PP => Name != null ? CurrentPoints + " / " + MaxPoints : "";

        public PokemonMove(int position, int id, int maxPoints, int currentPoints)
        {
            Position = position;
            Id = id;
            MaxPoints = maxPoints;
            CurrentPoints = currentPoints;
        }
    }
}
