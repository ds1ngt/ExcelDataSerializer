// using MemoryPack;
//
// namespace ExcelDataSerializer;
//
// public static class DataTable
// {
//     public static string Serialize<T>(T data)
//     {
//         if (data == null)
//             return string.Empty;
//
//         var bytes = MemoryPackSerializer.Serialize<T>(data);
//         return Convert.ToBase64String(bytes);
//     }
//
//     public static T? Deserialize<T>(string base64Str)
//     {
//         if (string.IsNullOrWhiteSpace(base64Str))
//             return default;
//
//         var bytes = Convert.FromBase64String(base64Str);
//         return MemoryPackSerializer.Deserialize<T>(bytes);
//     }
// }