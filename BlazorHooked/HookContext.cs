namespace BlazorHooked;

using OneOf;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

public delegate void SetState<T>(T newValue);

public delegate void Dispatch(object action);

public delegate T Reducer<T>(T state, object action);

public delegate void Effect();

public delegate Task AsyncEffect();

public delegate Action CallbackEffect();

public delegate Task<Func<Task>> AsyncCallbackEffect();

public class HookContext : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, object?> states = new();
    private readonly ConcurrentDictionary<int, (OneOf<Effect, AsyncEffect, CallbackEffect, AsyncCallbackEffect> Effect, object[] Dependencies)> effects = new();
    private readonly ConcurrentQueue<(int Index, OneOf<Effect, AsyncEffect, CallbackEffect, AsyncCallbackEffect> Effect)> effectQueue = new();
    private readonly ConcurrentDictionary<int, OneOf<Action, Func<Task>>> effectCallbacks = new();

    private readonly Action stateHasChanged;

    public HookContext(Action stateHasChanged) => this.stateHasChanged = stateHasChanged;

    public (T? state, SetState<T> setState) UseState<T>(T initialValue, [CallerLineNumber] int caller = 0)
        => ((T?)this.states.GetOrAdd(caller, initialValue), this.SetState<T>(caller));

    public (T? state, Dispatch dispatch) UseReducer<T>(Reducer<T> reducer, T initialState, [CallerLineNumber] int caller = 0)
        => ((T?)this.states.GetOrAdd(caller, initialState), this.Dispatch(caller, reducer));

    public void UseEffect(Effect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(Effect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    public void UseEffect(AsyncEffect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(AsyncEffect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    public void UseEffect(CallbackEffect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(CallbackEffect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    public void UseEffect(AsyncCallbackEffect effect, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, Array.Empty<object>(), caller);

    public void UseEffect(AsyncCallbackEffect effect, object[] dependencies, [CallerLineNumber] int caller = 0)
        => this.UseEffectInternal(effect, dependencies, caller);

    private void UseEffectInternal(OneOf<Effect, AsyncEffect, CallbackEffect, AsyncCallbackEffect> effect, object[] dependecies, int caller)
    {
        if (!this.effects.TryGetValue(caller, out var registeredEffect))
        {
            if (this.effects.TryAdd(caller, (effect, dependecies)))
            {
                this.effectQueue.Enqueue((caller, effect));
            }

            return;
        }

        if (!dependecies.Zip(registeredEffect.Dependencies).Any(z => z.First != z.Second))
        {
            return;
        }

        this.effects[caller] = (effect, dependecies);

        this.effectQueue?.Enqueue((caller, effect));
    }

    internal async Task RunEffects()
    {
        while (this.effectQueue.TryDequeue(out var effect))
        {
            if (this.effectCallbacks.TryGetValue(effect.Index, out var callback))
            {
                await callback.Match(action =>
                {
                    action();
                    return Task.CompletedTask;
                }, func => func());
            }

            await effect.Effect.Match(
                e =>
                {
                    e();
                    this.effectCallbacks.TryRemove(effect.Index, out var _);
                    return Task.CompletedTask;
                },
                async e =>
                {
                    await e();
                    this.effectCallbacks.TryRemove(effect.Index, out var _);
                },
                e =>
                {
                    this.effectCallbacks.AddOrUpdate(effect.Index, e(), (_, _) => e());
                    return Task.CompletedTask;
                },
                async e =>
                {
                    var cb = await e();
                    this.effectCallbacks.AddOrUpdate(effect.Index, cb, (_, _) => cb);
                });

        }
    }

    private SetState<T> SetState<T>(int index) => newState => this.states[index] = newState;

    private Dispatch Dispatch<T>(int index, Reducer<T> reducer) => action =>
    {
        this.states[index] = reducer((T?)this.states[index]!, action);
        this.stateHasChanged();
    };

    public async ValueTask DisposeAsync()
    {
        foreach (var callback in this.effectCallbacks.Values)
        {
            await callback.Match(
                cb =>
                {
                    cb();
                    return Task.CompletedTask;
                },
                cb => cb());
        }
    }
}
