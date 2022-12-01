namespace AzureFunctionAlert2Slack.Tests
{
    public class ConfigurationHelpers
    {
        public static IEnumerable<(string key, string value)> ObjectToFlatDictionary(object obj, string path = "")
        {
            var type = obj.GetType();
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(o => o.CanWrite);
            foreach (var prop in properties)
            {
                var fullPath = $"{(path.Any() ? $"{path}:" : "")}{prop.Name}";
                var val = prop.GetValue(obj);
                if (val == null)
                    ; // yield return (fullPath, "");
                else if (val.GetType().IsClass && val is not string)
                    foreach (var item in ObjectToFlatDictionary(val, fullPath))
                        yield return item;
                else
                    yield return (fullPath, val.ToString() ?? "");

            }
        }

        public static string ConfigToAzureSettingsString(Dictionary<string, string> dictionary)
        {
            return System.Text.Json.JsonSerializer.Serialize(
                dictionary.Select(o => new
                { 
                    name = o.Key,
                    value = o.Value,
                    slotSetting = false
                }),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
    }
}
