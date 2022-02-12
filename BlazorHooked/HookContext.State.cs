namespace BlazorHooked;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

public delegate void SetState<T>(T newValue);

public delegate void Dispatch(object action);

public delegate T Reducer<T>(T state, object action);

public partial class HookContext : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, object?> states = new();

    public (T? state, SetState<T> setState) UseState<T>(T initialValue, [CallerLineNumber] int caller = 0)
        => ((T?)this.states.GetOrAdd(caller, initialValue), this.SetState<T>(caller));

    public (T? state, Dispatch dispatch) UseReducer<T>(Reducer<T> reducer, T initialState, [CallerLineNumber] int caller = 0)
        => ((T?)this.states.GetOrAdd(caller, initialState), this.Dispatch(caller, reducer));

    private SetState<T> SetState<T>(int index) => newState => this.states[index] = newState;

    private Dispatch Dispatch<T>(int index, Reducer<T> reducer) => action =>
    {
        this.states[index] = reducer((T?)this.states[index]!, action);
        this.stateHasChanged();
    };
}
