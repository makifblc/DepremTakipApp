using System.Collections.Generic;

public class Earthquake
{
    public string title { get; set; }     // Örn: "MARMARA DENIZI"
    public double mag { get; set; }       // Büyüklük
    public double depth { get; set; }     // Derinlik (km)

    public string date { get; set; }      // API'den gelen tarih (Örn: "2026.11.06")
    public string time { get; set; }      // API'den gelen saat (Örn: "14:30:00")

    // İki alanı otomatik birleştirerek tek sütunda kusursuz görünmesini sağlar
    public string TarihSaat => $"{date} {time}";

    public double lng { get; set; }       // Boylam
    public double lat { get; set; }       // Enlem
}

public class ApiResponse
{
    public bool status { get; set; }
    public List<Earthquake> result { get; set; }
}