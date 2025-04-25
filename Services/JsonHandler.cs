using DioramaEngine.Models;
using System.IO;
using System.Text.Json;

namespace DioramaEngine.Services
{
    public class JsonHandler
    {
        public static List<DioramaRef>? Read(string filepath) => JsonSerializer.Deserialize<List<DioramaRef>>(File.ReadAllText(filepath));
    }
}
