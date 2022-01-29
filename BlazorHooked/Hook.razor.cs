namespace BlazorHooked;

public partial class Hook
{
    private readonly HookContext context;

    public Hook() => this.context = new HookContext(this.StateHasChanged);

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            this.context.RunEffects();
        }
    }
}
