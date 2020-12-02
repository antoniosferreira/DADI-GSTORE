using System.Collections.Concurrent;
using System.Collections.Generic;
using GSTORE_Server.Storage;
using System;

namespace GSTORE_Server.Communication
{
    class View
    {
        public int ViewID { get; set; }
        public string ViewLeader { get; set; }
        public List<string> ViewParticipants = new List<string>();
        public readonly ConcurrentDictionary<int, WriteData> Buffer = new ConcurrentDictionary<int, WriteData>();

        public View(int id, string leader, List<string> participants)
        {
            ViewID = id;
            ViewLeader = leader;
            ViewParticipants = participants;
        }

        public View(View view, List<string> toExclude)
        {
            ViewID = view.ViewID + 1;
            ViewLeader = view.ViewLeader;
            ViewParticipants = view.ViewParticipants;
            ViewParticipants.RemoveAll(t => toExclude.Contains(t));
        }
    }
}
