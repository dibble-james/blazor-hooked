Get Started
-----------

Install from Nuget [![NuGet Status](https://badgen.net/nuget/v/BlazorHooked)](https://www.nuget.org/packages/BlazorHooked/)

Add the obligitory `@@using BlazorHooked` statement to `_Imports.razor`.

If you're intending on using a global store wrap your app in your `App.razor`.
```razor
@code {
    public record AppState();

    private Reducer<AppState> RootReducer(Reducer<AppState> reducer) => reducer;
}

<ConnectStore RootReducer=@RootReducer InitialStateFactory=@(() => new AppState())>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p role="alert">Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</ConnectStore>
```

### The `HookContext`

Hooks are accessed via a `HookContext` which you can get one of two ways.

Inherit from `HookComponentBase` in which case `this.Hook` exposes a single `HookContext` for the child component.
```razor
@inherits HookComponentBase

@{
    this.Hook.UseState(0);
}
```

Or use the `Hook` component, in which case the `HookContext` is scoped within the `Hook`. This gives more flexibility for
you to inherit from other base components and even to create multiple contexts within a component.

```razor
<Hook>
    @{
        context.UseState(0);
    }
    <div>Hello</div>
</Hook>
```

You can rename the context to something more helpful and/or to avoid collisions.

```razor
<Hook Context="Hook">
    @{
        var (state, _) = Hook.UseState(0);
    }

    @state
</Hook>
<Hook Context="Hook2">
    @{
        var (state, _) = Hook2.UseState(1);
    }

    @state
</Hook>
```

You'll find there are very few classes or interfaces to inherit or implement in BlazorHooked. Actions and state in the 
examples are usually defined as records. The more you embrace immutibility the easier the Model View Update pattern becomes 
because you stop fighting the render loop and BlazorHooked is designed to foster that by using functional constructs 
wherever possible.

Read on to find out more about [Hooks](https://dibble-james.github.io/blazor-hooked/hooks).