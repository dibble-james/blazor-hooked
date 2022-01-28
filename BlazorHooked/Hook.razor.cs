namespace BlazorHooked;

public partial class Hook
{
    public delegate void SetState<T>(T newValue, bool forceUpdate = false);

    private readonly HookContext context;

    public Hook() => this.context = new HookContext(this);

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        this.context.StateCaptured();
    }

    public class HookContext
    {
        private readonly Hook hook;
        private readonly Dictionary<int, object?> states = new();
        private Queue<KeyValuePair<int, object?>> renderStates = new();
        private bool initialValuesCaptured;

        public HookContext(Hook hook) => this.hook = hook;

        public (T? state, SetState<T> setState) UseState<T>(T initialValue)
        {
            if (this.initialValuesCaptured)
            {
                var state = this.renderStates.Dequeue();

                return ((T?)state.Value, this.SetState<T>(state.Key));
            }

            var index = this.states.Count;

            this.states.Add(index, initialValue);

            return (initialValue, this.SetState<T>(index));
        }

        public void StateCaptured()
        {
            this.initialValuesCaptured = true;
            this.renderStates = new Queue<KeyValuePair<int, object?>>(this.states);
        }

        private SetState<T> SetState<T>(int index) => (newState, forceUpdate) =>
        {
            this.states[index] = newState;

            this.renderStates = new Queue<KeyValuePair<int, object?>>(this.states);

            if (forceUpdate)
            {
                this.hook.StateHasChanged();
            }
        };
    }
}
