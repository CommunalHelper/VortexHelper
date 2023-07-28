using Monocle;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.VortexHelper.Misc.Extensions;

// Thanks, Ja.
// https://github.com/JaThePlayer/FrostHelper/blob/master/FrostTempleHelper/StateMachineExt.cs
public static class StateMachineExt
{
    private readonly static FieldInfo f_StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly static FieldInfo f_StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly static FieldInfo f_StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly static FieldInfo f_StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// Adds a state to a StateMachine
    /// </summary>
    /// <returns>The index of the new state</returns>
    public static int AddState(this StateMachine machine, Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null)
    {
        var begins = (Action[]) f_StateMachine_begins.GetValue(machine);
        var updates = (Func<int>[]) f_StateMachine_updates.GetValue(machine);
        var ends = (Action[]) f_StateMachine_ends.GetValue(machine);
        var coroutines = (Func<IEnumerator>[]) f_StateMachine_coroutines.GetValue(machine);

        int nextIndex = begins.Length;

        // Now let's expand the arrays
        Array.Resize(ref begins, begins.Length + 1);
        Array.Resize(ref updates, begins.Length + 1);
        Array.Resize(ref ends, begins.Length + 1);
        Array.Resize(ref coroutines, coroutines.Length + 1);

        // Store the resized arrays back into the machine
        f_StateMachine_begins.SetValue(machine, begins);
        f_StateMachine_updates.SetValue(machine, updates);
        f_StateMachine_ends.SetValue(machine, ends);
        f_StateMachine_coroutines.SetValue(machine, coroutines);

        // And now we add the new functions
        machine.SetCallbacks(nextIndex, onUpdate, coroutine, begin, end);
        return nextIndex;
    }
}
