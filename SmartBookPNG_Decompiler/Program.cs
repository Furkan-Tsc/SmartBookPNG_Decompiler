// Program.cs
using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;

class Program
{
    static int Main(string[] args)
    {
        // Varsayılan 7-Zip Yolu
        string varsayilan7zYolu = @"C:\Program Files\7-Zip\7z.exe";

        Console.Write($"EXE dosyasının yolunu girin: ");
        string? exeDosyaYolu = Console.ReadLine()?.Trim();

        Console.Write($"7-Zip (7z.exe) yolunu girin [{varsayilan7zYolu}]: ");
        string? sevenZipYolu = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(sevenZipYolu)) sevenZipYolu = varsayilan7zYolu;

        Console.WriteLine();

        if (!File.Exists(sevenZipYolu))
        {
            Console.WriteLine($"HATA: 7-Zip bulunamadı: {sevenZipYolu}");
            return 1;
        }

        if (!File.Exists(exeDosyaYolu))
        {
            Console.WriteLine($"HATA: .exe dosyası bulunamadı: {exeDosyaYolu}");
            return 1;
        }

        string anaKlasor = Path.GetDirectoryName(exeDosyaYolu)!;
        string sonDecodeKlasoru = Path.Combine(anaKlasor, "DecodedPngFiles");
        string geciciKlasor = Path.Combine(Path.GetTempPath(), "png_extract_" + Guid.NewGuid().ToString("N"));

        try
        {
            if (Directory.Exists(sonDecodeKlasoru))
                Directory.Delete(sonDecodeKlasoru, true);

            Directory.CreateDirectory(sonDecodeKlasoru);
            Directory.CreateDirectory(geciciKlasor);

            // 7z komutu - *.png dosyalarını çıkar
            var psi = new ProcessStartInfo
            {
                FileName = sevenZipYolu,
                Arguments = $"e \"{exeDosyaYolu}\" \"*.png\" -o\"{geciciKlasor}\" -r -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                proc?.WaitForExit();
                string? stdout = proc?.StandardOutput.ReadToEnd();
                string? stderr = proc?.StandardError.ReadToEnd();

                if (proc?.ExitCode != 0)
                {
                    Console.WriteLine("7-Zip çıkarma hatası: ");
                    Console.WriteLine(stdout);
                    Console.WriteLine(stderr);
                    return 1;
                }
            }

            // Tüm .png dosyalarını al sadece isimleri sayısal olanları bırak
            var tumPngler = Directory.GetFiles(geciciKlasor, "*.png", SearchOption.TopDirectoryOnly);
            var sayisalRegex = new Regex(@"^\d+\.png$", RegexOptions.IgnoreCase);
            var sayisalPngler = tumPngler.Where(p => sayisalRegex.IsMatch(Path.GetFileName(p))).ToList();

            if (!sayisalPngler.Any())
            {
                return 0;
            }

            foreach (var s in sayisalPngler)
            {
                string hedef = Path.Combine(sonDecodeKlasoru, Path.GetFileName(s));
                DesifreVeKopyala(s, hedef);
            }

            Console.WriteLine();
            Console.WriteLine($"Tüm dosyaların şifresi çözüldü: {sonDecodeKlasoru}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata: " + ex);
            return 1;
        }
        finally
        {
            try
            {
                if (Directory.Exists(geciciKlasor))
                    Directory.Delete(geciciKlasor, true);
            }
            catch { }

            Console.WriteLine("Dosyalar çözüldü.");
        }
    }

    private static void DesifreVeKopyala(string kaynakDosya, string hedefDosya)
    {
        try
        {
            byte[] veri = File.ReadAllBytes(kaynakDosya);
            if (veri.Length < 50)
            {
                File.Copy(kaynakDosya, hedefDosya, overwrite: true);
                return;
            }

            for (int i = 0; i < 100; i++)
            {
                veri[i] = (byte)((256 - veri[i]) & 0xFF);
            }

            Console.WriteLine($"{hedefDosya} şifresi çözüldü.");
            File.WriteAllBytes(hedefDosya, veri);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Decrypt hatası ({Path.GetFileName(kaynakDosya)}): {e.Message}");
        }
    }
}
