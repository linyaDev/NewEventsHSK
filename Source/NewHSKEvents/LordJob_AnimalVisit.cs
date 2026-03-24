using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewHSKEvents
{
    public class LordJob_AnimalVisit : LordJob
    {
        private IntVec3 chillSpot;
        private int durationTicks;

        public LordJob_AnimalVisit()
        {
        }

        public LordJob_AnimalVisit(IntVec3 chillSpot, int durationTicks)
        {
            this.chillSpot = chillSpot;
            this.durationTicks = durationTicks;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();

            // Toil 1: wander around the chill spot
            LordToil_DefendPoint wanderToil = new LordToil_DefendPoint(chillSpot, 28f);
            stateGraph.StartingToil = wanderToil;

            // Toil 2: exit map
            LordToil_ExitMap exitToil = new LordToil_ExitMap(LocomotionUrgency.Walk, canDig: false);
            stateGraph.AddToil(exitToil);

            // Toil 3: exit map (urgent, for edge cases)
            LordToil_ExitMap exitToilUrgent = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: true);
            stateGraph.AddToil(exitToilUrgent);

            // Transition: after durationTicks, leave the map
            Transition leaveTransition = new Transition(wanderToil, exitToil);
            leaveTransition.AddTrigger(new Trigger_TicksPassed(durationTicks));
            leaveTransition.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(leaveTransition);

            // Transition: if temperature is dangerous, leave early
            Transition tempTransition = new Transition(wanderToil, exitToil);
            tempTransition.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
            tempTransition.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(tempTransition);

            // Transition: if can't reach edge, dig out
            Transition trappedTransition = new Transition(wanderToil, exitToilUrgent);
            trappedTransition.AddSources(exitToil);
            trappedTransition.AddTrigger(new Trigger_PawnCannotReachMapEdge());
            stateGraph.AddTransition(trappedTransition);

            // Transition: if was trapped but now can reach edge, walk normally
            Transition untrappedTransition = new Transition(exitToilUrgent, exitToil);
            untrappedTransition.AddTrigger(new Trigger_PawnCanReachMapEdge());
            untrappedTransition.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(untrappedTransition);

            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref chillSpot, "chillSpot");
            Scribe_Values.Look(ref durationTicks, "durationTicks");
        }
    }
}
