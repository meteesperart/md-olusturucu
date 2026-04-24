using MDOlusturucu.Models;

namespace MDOlusturucu.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save(AppSettings settings);
    void SaveSilent(AppSettings settings);

    event EventHandler<AppSettings> SettingsChanged;
}
