import os
import subprocess
import glob
import shutil
import re
import tempfile
import sys

#Bunları Düzenle
EXE_DOSYA_YOLU = "Kitap Yolu"

yZIP_YOLU = r"C:\Program Files\7-Zip\7z.exe"


def desifre_ve_kopyala(kaynak_dosya: str, hedef_dosya: str) -> None:
    try:
        with open(kaynak_dosya, 'rb') as f:
            veri = f.read()

        if len(veri) < 100:
            shutil.copyfile(kaynak_dosya, hedef_dosya)
            return

        b = bytearray(veri)
        for i in range(100):
            b[i] = (256 - b[i]) & 0xFF

        with open(hedef_dosya, 'wb') as out:
            out.write(b)

    except Exception as e:
        print(f"Decryt hatası ({os.path.basename(kaynak_dosya)}): {e}")


def main():
    if not os.path.exists(yZIP_YOLU):
        print(f"HATA: 7-Zip bulunamadı: {yZIP_YOLU}")
        return

    if not os.path.exists(EXE_DOSYA_YOLU):
        print(f"HATA: .exe dosyası bulunamadı: {EXE_DOSYA_YOLU}")
        return

    ana_klasor = os.path.dirname(EXE_DOSYA_YOLU)
    SON_DECODE_KLASORU = os.path.join(ana_klasor, "decoded_png")
    gecici_klasor = tempfile.mkdtemp(prefix="png_extract_")

    shutil.rmtree(SON_DECODE_KLASORU, ignore_errors=True)
    os.makedirs(SON_DECODE_KLASORU, exist_ok=True)

    try:
        # Arşivden .png dosyaları çıkarmma
        komut = [
            yZIP_YOLU, 'e', EXE_DOSYA_YOLU,
            '*.png',
            f'-o{gecici_klasor}',
            '-r', '-y'
        ]

        subprocess.run(komut, check=True, capture_output=True, text=True)

        desen = os.path.join(gecici_klasor, "*.png")
        tum_pngler = glob.glob(desen)
        sayisal_pngler = [p for p in tum_pngler if re.match(r'^\d+\.png$', os.path.basename(p))]

        if not sayisal_pngler:
            return

        for s in sayisal_pngler:
            hedef = os.path.join(SON_DECODE_KLASORU, os.path.basename(s))
            desifre_ve_kopyala(s, hedef)

        print(f"Dosyalar kaydedildi: {SON_DECODE_KLASORU}")

    except subprocess.CalledProcessError as e:
        print("7-Zip Hatası: ", e.stdout, e.stderr)
    except Exception as e:
        print(f"Hata: {e}")
    finally:
        shutil.rmtree(gecici_klasor, ignore_errors=True)
        print("Dosyalar Çözüldü")


if __name__ == '__main__':
    main()
