# Hooked on Blazor
[![Nuget](https://github.com/dibble-james/blazor-hooked/actions/workflows/nuget.yml/badge.svg)](https://github.com/dibble-james/blazor-hooked/actions/workflows/nuget.yml)
[![NuGet Status](https://badgen.net/nuget/v/BlazorHooked)](https://www.nuget.org/packages/BlazorHooked/)

A minimal boiler-plate, state management framework for Blazor that resembles React Hooks. It also enables a slightly more functional approach to Blazor components by being a bit more forceful with immutability.

## Installation

TODO: Publish to Nuget

Add the obligitory using statement to `_Imports.razor` if you don't want `@using`s in every component.

```razor
@using BlazorHooked
```

## Hooks

Hooks are accessed via a `HookContext` which is provided by the `Hook` component. `HookContext` is scoped within a `Hook`, so you can have multiple `Hook`s within an application or even component if you really wanted too.

```razor
<Hook>
    @{
        context.UseX();
    }
    <div>Hello</div>
</Hook>
```

You can rename the context to something more helpful and/or to avoid collisions.

```razor
<Hook Context="Hook">
    @{
        Hook.UseX();
    }
    <div>Hello</div>
</Hook>
```

### `UseState`

`UseState` takes an initial value for the state and returns a tuple, the first item being the current state, and the second being a function to update the state AKA the `set` function. You should treat the state as immutable and only update it via the `set` function.

You would typically only really use `UseState` for small value types. The most simple usecase would be a counter:

```razor
<Hook>
    @{
        var (count, setCount) = context.UseState(0);
    }

    <p>Count: @count</p>
    <p>
        <button @onclick=@(() => setCount(count + 1))>Up</button>
        <button @onclick=@(() => setCount(count - 1))>Down</button>
    </p>
</Hook>
```

### `UseReducer`

This is yet another Flux like thing, but Hooks make it so much simpler. It acts much like `UseState` but can handle more granular updates to the state via Actions. Like `UseState` it takes an initial state but also a Reducer. Again like `UseState` it returns the current state, but the `set` function is replaced by a Dispatcher.

Lets define some of those words:

- **Action**: A command or event to inform the reducer that the state should be changed. These are usually best served by `record`s.
- **Reducer**: A function that takes the current state and an action and returns the new state. The framework doesn't care how you do the reduction; it could be a local function, a static method on a seperate class or a type that you inject. It's best practice, however, to not trigger things like http requests from the reducer, that's what Effects are for.
- **Dispatcher**: A function provided by the framework that you call with an Action to invoke the Reducer.

Lets refactor our counter:

```razor
@code {
    public record CounterState(int Count);

    public record Increment();
    public record Decrement();

    private CounterState Reducer(CounterState state, object action) => action switch
    {
        Increment => state with { Count = state.Count + 1 },
        Decrement => state with { Count = state.Count - 1 },
        _ => state,
    };
}

<Hook>
    @{
        var (state, dispatch) = context.UseReducer(Reducer, new CounterState(0));
    }

    <p>Count: @state!.Count</p>
    <p>
        <button @onclick=@(() => dispatch(new Increment()))>Up</button>
        <button @onclick=@(() => dispatch(new Decrement()))>Down</button>
    </p>
</Hook>
```

That's obviously a trivial example, but it gives you the idea of all you need. A common use for a reducer is to track an async request. In-fact is so common that to save you some more boiler plate, the `Loader` component is built in.

``` razor
<Loader Load=@LoadData T="object">
    <Loading>
        <p><em>Loading...</em></p>
    </Loading>
    <Loaded Context="data">
        @data
    </Loaded>
    <Failed>
        <p><em>Uhoh...</em></p>
    </Failed>
</Loader>

@code {
    private async Task LoadData(Dispatch dispatch)
    {
        try
        {
            var data = await SomeAsyncService();

            dispatch(new LoaderActions.Loaded<object>(data));
        }
        catch (Exception ex)
        {
            dispatch(new LoaderActions.Failed(ex));
        }
    }
}
```

### `UseEffect`
Effects are used to start background tasks and clean up after them when they're finished with.  The classic example would be to start listening on a websocket when the component is first rendered, then gracefully shutdown the connection when the component is unmounted and disposed.  If your Effect uses a variable like a value from `UseState` or a component paramenter and you'd like the Effect to re-run when that changes, you add that variable as a Dependency by letting `UseEffect` track it's value when you define the Effect.

``` razor
@inject WebSocketService UserNotificationService

@code {
    [Parameter]
    public Guid UserId { get; set; }

    public Func<Func<Task>> ListenForUserNotifications(SetState<string[]> setUserMessages) => async () =>
    {
        await UserNotificationService.StartListening(UserId, setUserMessages);

        return async () => await UserNotificationService.StopListening(UserId);
    };
}

<Hook>
    @{
        var (messages, setMessages) = context.UseState(new string[0]);

        context.UseEffect(ListenForUserNotifications(setMessages), new object[] { this.UserId });
    }

    @foreach(var message in messages)
    {
        <p>@message</p>
    }
</Hook>
```
