namespace MDOlusturucu;

public static class L
{
    public static string Get(string key) =>
        System.Windows.Application.Current?.Resources[key] as string ?? key;
}
