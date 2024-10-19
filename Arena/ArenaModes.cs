using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow
{
    public class OnlineArenaModes: ExtEnum<ArenaSetup.GameTypeID>
    {
        public OnlineArenaModes(string value, bool register = false) : base(value, register) { }
        public static ArenaSetup.GameTypeID FFA = new("FFA", true);
        public static ArenaSetup.GameTypeID TeamBattle = new("TeamBattle", true);
    }

}
