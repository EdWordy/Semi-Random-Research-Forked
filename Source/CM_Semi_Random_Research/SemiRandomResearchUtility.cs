using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CM_Semi_Random_Research
{
    public static class SemiRandomResearchUtility
    {
        // This little gumdrop is to make my life easy with a transpiler patch for hiding the normal research button
        public static bool CanSelectNormalResearchNow()
        {
            return !SemiRandomResearchMod.settings.featureEnabled;
        }
    }
}
