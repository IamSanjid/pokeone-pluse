using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Poke1Protocol.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class MovesManager
    {
        public class MoveData
        {
            public int? ID;
            public string Name;
            public string BattleID;
            public string Description;
            public string Type;
            public string Category;
            public int? PP;
            public int? Power;
            public int? ACC;

            [JsonIgnore]
            public bool Status => Category == "status";

            [JsonIgnore]
            public DamageType DamageType => string.Equals(Category, "special", StringComparison.InvariantCultureIgnoreCase) ? DamageType.Special : DamageType.Physical;

            [JsonIgnore]
            public int Accuracy => ACC.HasValue ? ACC.Value : -1;

            [JsonIgnore]
            public int RealPower => Power.HasValue ? Power.Value : -1;

            [JsonIgnore]
            public int RealPP => PP.HasValue ? PP.Value : -1;
        }

        public string GetMoveNameFromEnum(PSXAPI.Response.Payload.PokemonMoveID id)
        {
            var foundMove = GetMoveDataFromEnum(id);
            return foundMove != null ? foundMove.Name : id.ToString();
        }

        public MoveData GetMoveDataFromEnum(PSXAPI.Response.Payload.PokemonMoveID id)
        {
            var result = Moves.ToList().Find(move => string.Equals(move.BattleID, id.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return result;
        }

        public class MoveDatas
        {
            public MoveData[] Moves;
        }

        public enum DamageType
        {
            Physical,
            Special
        }

        private static MovesManager _instance;

        public static MovesManager Instance
        {
            get
            {
                return _instance ?? (_instance = new MovesManager());
            }
        }

        public const int MovesCount = 721;
        private MoveDatas Datas;
        public MoveData[] Moves = new MoveData[MovesCount];
        private Dictionary<string, MoveData> _namesToMoves;
        private Dictionary<string, int> _namesToIds = new Dictionary<string, int>();
        private MoveData[] _idsToMoves = new MoveData[MovesCount];

        private MovesManager()
        {
            LoadMoves();

            _namesToMoves = new Dictionary<string, MoveData>();

            for (int i = 0; i < Moves.Length; ++i)
            {
                var move = Moves[i];
                _namesToMoves[move.BattleID.ToLowerInvariant()] = move;
                _namesToIds[move.BattleID.ToLowerInvariant()] = i + 1;
                _idsToMoves[i + 1] = move;
            }
        }

        public MoveData GetMoveData(string moveName)
        {
            if (!string.IsNullOrEmpty(moveName))
            {
                return Array.Find(_idsToMoves, m => string.Equals(m.BattleID, moveName, StringComparison.InvariantCultureIgnoreCase))
                    ?? Array.Find(Moves, m => string.Equals(m.Name, moveName, StringComparison.InvariantCultureIgnoreCase));
            }
            return null;
        }

        public int GetMoveId(string moveName)
        {
            moveName = moveName.ToLowerInvariant();
            if (_namesToIds.ContainsKey(moveName))
            {
                return _namesToIds[moveName];
            }
            return -1;
        }

        public MoveData GetMoveData(int moveId)
        {
            if (moveId > 0 && moveId < MovesCount)
            {
                return _idsToMoves[moveId];
            }
            return null;
        }

        private void LoadMoves()
        {
            var json = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Resources.moves)) as JObject;
            Datas = JsonConvert.DeserializeObject<MoveDatas>(json.ToString());
            Moves = Datas.Moves;
        }
    }
}
