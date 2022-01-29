using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace BlazorHooked;

public delegate void SetState<T>(T newValue);

public delegate void Dispatch(object action);

public delegate T Reducer<T>(T state, object action);

public class HookContext
{
    private readonly ConcurrentDictionary<int, object?> states = new();
    private readonly ConcurrentDictionary<int, (Action Effect, object[] Dependencies)> effects = new();
    private readonly ConcurrentQueue<Action> effectQueue = new();
    private readonly Action stateHasChanged;

    public HookContext(Action stateHasChanged) => this.stateHasChanged = stateHasChanged;

    public (T? state, SetState<T> setState) UseState<T>(T initialValue, [CallerLineNumber] int caller = 0)
        => ((T?)this.states.GetOrAdd(caller, initialValue), this.SetState<T>(caller));

    public (T? state, Dispatch dispatch) UseReducer<T>(Reducer<T> reducer, T initialState, [CallerLineNumber] int caller = 0)
        => ((T?)this.states.GetOrAdd(caller, initialState), this.Dispatch(caller, reducer));

    public void UseEffect(Action effect, [CallerLineNumber] int caller = 0)
        => this.UseEffect(effect, Array.Empty<object>(), caller);

    public void UseEffect(Action effect, object[] dependecies, [CallerLineNumber] int caller = 0)
    {
        if (!this.effects.TryGetValue(caller, out var registeredEffect))
        {
            if (this.effects.TryAdd(caller, (effect, dependecies)))
            {
                this.effectQueue.Enqueue(effect);
            }
            
            return;
        }

        if (!dependecies.Zip(registeredEffect.Dependencies).Any(z => z.First != z.Second))
        {
            return;
        }

        this.effects[caller] = (effect, dependecies);

        this.effectQueue?.Enqueue(effect);
    }

    internal void RunEffects()
    {
        while (this.effectQueue.TryDequeue(out var effect))
        {
            effect();
        }
    }

    private SetState<T> SetState<T>(int index) => newState => this.states[index] = newState;

    private Dispatch Dispatch<T>(int index, Reducer<T> reducer) => action =>
    {
        this.states[index] = reducer((T?)this.states[index]!, action);
        this.stateHasChanged();
    };
}
