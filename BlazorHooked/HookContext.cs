namespace BlazorHooked;

public partial class HookContext
{
    private readonly Action stateHasChanged;

    public HookContext(Action stateHasChanged) => this.stateHasChanged = stateHasChanged;
}
