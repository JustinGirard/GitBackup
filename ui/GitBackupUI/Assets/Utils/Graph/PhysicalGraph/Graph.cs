using System.Collections.Generic;
using UnityEditor;

namespace PhysicalModel
{
    /*
        A Physical graph of game objects who will operate on each
        other in well defined epochs. Reasons:
        1) Prep for DOTS
        2) Ability to seperate / monitor, Physics and powers
        3) Ability to handle "concurrent" actions, like sword blows and blocks which must be in sync
        4) Prep for Server side physics processing.

        Indended for: Operations that change health, resources, or otherwise are
        "Game important". Not for visual effects and powers.
    */


    public class Graph 
    {
        private  Dictionary<string,Transaction> epochOperations = null;
        public void StartEpoch()
        {
            epochOperations = new Dictionary<string,Transaction>();
        }

        public bool AddTransaction(Transaction t)
        {
            if (t.CanExecute())
            {
                epochOperations[t.GetUID()] = t;
                return true;
            }   
            else
            {
                return false;
            }
        }
        public void ProcessEpoch()
        {
            // Process the epoch
        }

    }
}