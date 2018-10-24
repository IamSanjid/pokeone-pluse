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
            public DamageType DamageType => Category.ToLowerInvariant() == "special" ? DamageType.Special : DamageType.Physical;
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
            MoveData result = Moves.ToList().Find(move => move.BattleID.ToLowerInvariant() == id.ToString().ToLowerInvariant());
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

        public const int MovesCount = 720;
        private MoveDatas Datas;
        public MoveData[] Moves = new MoveData[MovesCount];
        public string[] MoveNames = new string[MovesCount];
        private Dictionary<string, MoveData> _namesToMoves;
        private Dictionary<string, int> _namesToIds = new Dictionary<string, int>();
        private MoveData[] _idsToMoves = new MoveData[MovesCount];
        private MovesManager()
        {
            LoadMoves();


            _namesToMoves = new Dictionary<string, MoveData>();

            for (int i = 0; i < MovesCount; i++)
            {
                if (Moves[i].BattleID != null && !_namesToMoves.ContainsKey(Moves[i].BattleID.ToLowerInvariant()))
                {
                    _namesToMoves.Add(Moves[i].BattleID.ToLowerInvariant(), Moves[i]);
                }
            }
            for (int i = 0; i < MovesCount; i++)
            {
                string lowerName = MoveNames[i].ToLowerInvariant();
                if (_namesToMoves.ContainsKey(lowerName))
                {
                    _idsToMoves[i] = _namesToMoves[lowerName];
                    if (!_namesToIds.ContainsKey(lowerName))
                    {
                        _namesToIds.Add(lowerName, i);
                    }
                }
            }
        }

        public MoveData GetMoveData(string moveName)
        {
            if (!string.IsNullOrEmpty(moveName))
            {
                for (int i = 0; i < MovesCount; i++)
                {
                    if (_idsToMoves[i].Name.ToLowerInvariant() == moveName.ToLowerInvariant())
                        return _idsToMoves[i];
                }
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
            var i = 0;
            //MoveNames[0] = string.Empty;
            foreach (var move in Moves)
            {
                if (i <= MovesCount)
                {
                    MoveNames[i] = move.BattleID;
                    i++;
                }
            }
        }
    }
}
