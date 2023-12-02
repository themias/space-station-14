using Content.Shared.Anomaly;
using Content.Shared.Cryotron;
using JetBrains.Annotations;

namespace Content.Client.Cryotron.UI;

[UsedImplicitly]
public sealed class CryotronBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CryotronWindow? _window;

    [ViewVariables]
    private CryotronWakeUpWindow? _wakeUpWindow;

    public CryotronBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new();

        _window.OpenCentered();
        _window.OnClose += Close;

        _window.OnPermanentSleepButtonPressed += () =>
        {
            SendMessage(new CryotronPermanentSleepButtonPressedEvent());
            _window.Close();
        };

        _window.OnTemporarySleepButtonPressed += () =>
        {
            SendMessage(new CryotronTemporarySleepButtonPressedEvent());
            _window.Close();
            OpenWakeUpWindow();
        };
    }

    private void OpenWakeUpWindow()
    {
        _wakeUpWindow = new();

        _wakeUpWindow.OnWakeUpButtonPressed += () =>
        {
            SendMessage(new CryotronWakeUpButtonPressedEvent());
            _wakeUpWindow.Close();
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CryotronUiState msg)
            return;
        _window?.UpdateState(msg);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
