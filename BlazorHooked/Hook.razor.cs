namespace BlazorHooked;

public partial class Hook : IAsyncDisposable
{
    private readonly HookContext context;

    public Hook() => this.context = new HookContext(this.StateHasChanged);

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        await this.context.RunEffects();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        await this.context.RunEffects();
    }

    public ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)context).DisposeAsync();
    }
}
