using System.Windows;
using Microsoft.Win32;
using PicoCompanion.Core.Models;
using PicoCompanion.Ui.Services;

namespace PicoCompanion.Ui;

public partial class MainWindow : Window
{
    private readonly CompanionServiceClient _client = new();
    private DongleConfiguration _configuration = new();

    public MainWindow()
    {
        InitializeComponent();
        DefaultOutputModeBox.ItemsSource = Enum.GetValues<ControllerOutputMode>();
        Loaded += async (_, _) => await RefreshAsync().ConfigureAwait(true);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) =>
        await RunUiActionAsync(RefreshAsync).ConfigureAwait(true);

    private async void SaveConfiguration_Click(object sender, RoutedEventArgs e) =>
        await RunUiActionAsync(SaveConfigurationAsync).ConfigureAwait(true);

    private async void ApplyProfile_Click(object sender, RoutedEventArgs e) =>
        await RunUiActionAsync(ApplySelectedProfileAsync).ConfigureAwait(true);

    private async void Reboot_Click(object sender, RoutedEventArgs e) =>
        await RunUiActionAsync(ct => _client.RebootAsync(ct), "Reboot command sent.").ConfigureAwait(true);

    private async void UpdateFirmware_Click(object sender, RoutedEventArgs e) =>
        await RunUiActionAsync(UpdateFirmwareAsync).ConfigureAwait(true);

    private void BrowseFirmware_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "UF2 firmware (*.uf2)|*.uf2|All files (*.*)|*.*",
            Title = "Select Pico firmware image"
        };

        if (dialog.ShowDialog(this) == true)
        {
            FirmwarePathBox.Text = dialog.FileName;
        }
    }

    private void ProfilesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not ControllerProfile profile)
        {
            ProfileDetailsText.Text = "Select a profile to view settings.";
            return;
        }

        ProfileDetailsText.Text =
            $"Preferred mode: {profile.PreferredOutputMode}\n" +
            $"Low latency: {profile.LowLatencyMode}\n" +
            $"Audio: {profile.EnableAudio}\n" +
            $"Haptics: {profile.EnableHaptics}\n" +
            $"LED brightness: {profile.LedBrightnessPercent}%\n" +
            $"Keep alive: {profile.KeepAliveIntervalMs} ms\n\n" +
            profile.Notes;
    }

    private void GameRulesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (GameRulesList.SelectedItem is not GameModeRule rule)
        {
            GameRuleDetailsText.Text = "Select a game rule to view matching details.";
            return;
        }

        GameRuleDetailsText.Text =
            $"Output mode: {rule.OutputMode}\n" +
            $"Profile: {rule.ProfileId}\n" +
            $"Foreground only: {rule.MatchForegroundOnly}\n" +
            $"Processes: {FormatList(rule.ProcessNames)}\n" +
            $"Package families: {FormatList(rule.PackageFamilyNames)}\n" +
            $"Executable paths: {FormatList(rule.ExecutablePaths)}\n\n" +
            rule.Notes;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var status = await _client.GetStatusAsync(cancellationToken).ConfigureAwait(true);
        var gameModeStatus = await _client.GetGameModeStatusAsync(cancellationToken).ConfigureAwait(true);
        _configuration = await _client.GetConfigurationAsync(cancellationToken).ConfigureAwait(true);

        StatusText.Text = status.IsConnected
            ? $"Connected on {status.PortName} | firmware {status.FirmwareVersion} | {status.PairingState}"
            : $"Disconnected{FormatError(status.LastError)}";

        GameModeText.Text =
            $"Mode: {gameModeStatus.CurrentOutputMode} | rule: {gameModeStatus.ActiveRuleName}";

        DeviceNameBox.Text = _configuration.DeviceName;
        ControllerAddressBox.Text = _configuration.PreferredControllerAddress;
        AutoReconnectBox.IsChecked = _configuration.AutoReconnect;
        UsbWakeBox.IsChecked = _configuration.EnableUsbRemoteWake;
        AutoModeSwitchingBox.IsChecked = _configuration.EnableAutomaticModeSwitching;
        DefaultOutputModeBox.SelectedItem = _configuration.DefaultOutputMode;
        GameRulesList.ItemsSource = _configuration.GameModeRules;
        ProfilesList.ItemsSource = _configuration.Profiles;
        ProfilesList.SelectedItem = _configuration.Profiles
            .FirstOrDefault(profile => profile.Id == _configuration.ActiveProfileId);

        MessageText.Text = "Configuration loaded.";
    }

    private async Task SaveConfigurationAsync(CancellationToken cancellationToken)
    {
        _configuration = _configuration with
        {
            DeviceName = DeviceNameBox.Text.Trim(),
            PreferredControllerAddress = ControllerAddressBox.Text.Trim(),
            AutoReconnect = AutoReconnectBox.IsChecked == true,
            EnableUsbRemoteWake = UsbWakeBox.IsChecked == true,
            EnableAutomaticModeSwitching = AutoModeSwitchingBox.IsChecked == true,
            DefaultOutputMode = DefaultOutputModeBox.SelectedItem is ControllerOutputMode outputMode
                ? outputMode
                : ControllerOutputMode.NativeDualSenseHid
        };

        await _client.SaveConfigurationAsync(_configuration, cancellationToken).ConfigureAwait(true);
        MessageText.Text = "Configuration saved.";
    }

    private async Task ApplySelectedProfileAsync(CancellationToken cancellationToken)
    {
        if (ProfilesList.SelectedItem is not ControllerProfile profile)
        {
            MessageText.Text = "Select a profile first.";
            return;
        }

        await _client.ApplyProfileAsync(profile, cancellationToken).ConfigureAwait(true);
        _configuration = _configuration with { ActiveProfileId = profile.Id };
        MessageText.Text = $"Applied profile '{profile.Name}'.";
    }

    private async Task UpdateFirmwareAsync(CancellationToken cancellationToken)
    {
        var uf2Path = FirmwarePathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(uf2Path))
        {
            MessageText.Text = "Choose a UF2 firmware file first.";
            return;
        }

        await _client.UpdateFirmwareAsync(uf2Path, cancellationToken).ConfigureAwait(true);
        MessageText.Text = "Firmware copied. Waiting for the dongle to reconnect.";
    }

    private async Task RunUiActionAsync(
        Func<CancellationToken, Task> action,
        string successMessage = "")
    {
        try
        {
            IsEnabled = false;
            MessageText.Text = "Working...";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            await action(cts.Token).ConfigureAwait(true);

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                MessageText.Text = successMessage;
            }
        }
        catch (Exception ex)
        {
            MessageText.Text = ex.Message;
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private static string FormatError(string error) =>
        string.IsNullOrWhiteSpace(error) ? string.Empty : $" | {error}";

    private static string FormatList(IEnumerable<string> values)
    {
        var materialized = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return materialized.Length == 0 ? "(none)" : string.Join(", ", materialized);
    }
}
