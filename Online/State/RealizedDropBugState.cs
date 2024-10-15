using UnityEngine;

namespace RainMeadow
{
    public class RealizedDropBugState : RealizedCreatureState
    {
        [OnlineField]
        WorldCoordinate ceilingPos;

        public RealizedDropBugState() { }
        public RealizedDropBugState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            DropBug bug = (DropBug)onlineEntity.apo.realizedObject;
            ceilingPos = bug.AI.ceilingModule.ceilingPos;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is DropBug bug)
            {
                bug.AI.ceilingModule.ceilingPos = ceilingPos;
            }
            else
            {
                RainMeadow.Error("target not realized: " + onlineEntity);
            }
        }
    }
}

