# MACET! — Phase 1

Proyek Unity 6.3 LTS (6000.3.0f1), C#, URP untuk Android portrait. Phase 1 berisi 3 level NORMAL dengan primitive mobil, tap/touch, waypoint movement, collision, win/fail, restart, pause, dan next level. Tidak ada backend, akun, atau runtime internet requirement.

**Status:** implementasi source dan data selesai. Unity Editor, Android SDK/NDK/OpenJDK, dan device tidak tersedia di lingkungan pembuatan. Proyek belum dikompilasi di Unity, Unity tests belum dijalankan, dan APK/AAB belum dihasilkan. Pemeriksaan offline data dan integritas proyek lulus. Resolusi package juga belum diverifikasi; akses proxy lingkungan gagal saat pemeriksaan registry.

## Buka dan mainkan

1. Install Unity **6.3 LTS** melalui Unity Hub; tambahkan Android Build Support, Android SDK & NDK Tools, dan OpenJDK. Proyek dipin ke 6000.3.0f1; patch 6.3 LTS yang lebih baru boleh dipakai.
2. Hub → Add project from disk → pilih folder `Macet` yang berisi `Assets`, `Packages`, dan `ProjectSettings`.
3. Tunggu package import. `ProjectSetup` otomatis mengatur URP, Player Settings, TMP Essential Resources, prefab `Car`, dan scene `Assets/_Game/Scenes/Game.unity`. Restart Editor jika diminta setelah Input System diaktifkan. Setup juga dapat dijalankan dari **Macet → Setup Phase 1 Project**.
4. Buka `Game.unity`. Game view: portrait 9:16, misalnya 540 × 960. Tekan Play, pilih level, lalu klik mobil. Android memakai satu sentuhan. Warna garis menunjukkan rute, marker gelap di bagian depan mobil menunjukkan arah.
5. Tap hanya bekerja pada mobil WAITING dan saat Playing. Mobil bergerak sendiri. Tabrakan dengan mobil menunggu maupun bergerak langsung gagal. Tunggu mobil keluar untuk urutan solusi paling aman.

Setup membuat scene dan prefab pada import pertama; keduanya belum diserialisasi oleh Unity dalam paket ini. Setup berikutnya mempertahankan scene/prefab dan level yang sudah ada. Setelah import berhasil, simpan dan commit aset hasil setup serta `Packages/packages-lock.json`.

## Tiga level

| Level | Map | Mobil | Pelajaran | Solusi aman (tunggu EXITED setiap langkah) |
|---|---|---|---|---|
| 1 | Pertigaan | 2 | Tap dan antrean | A → B |
| 2 | Perempatan | 3 | Urutan dan crossing | A → B → C |
| 3 | Perempatan | 4 | Dua antrean | A → B → C → D |

Beberapa urutan lain valid. Level 1 sengaja forgiving; spam semua kendaraan di level 2/3 mempertemukan A dan C di perempatan. Tap mobil belakang sebelum mobil depan keluar dapat menabrak antrean. Tidak ada random timing, timer, atau deadlock detection. Durasi metadata awal 12/18/24 detik adalah estimasi untuk tutorial dan perlu playtest.

## Android

- **Macet → Build Android APK** menghasilkan development build `Builds/Macet.apk`.
- **Macet → Build Android AAB** menghasilkan `Builds/Macet.aab`. Signing untuk publikasi perlu diatur sendiri melalui Player Settings.
- Build & Run: File → Build Profiles → Android → Switch Platform. Pastikan Game scene masuk scene list, hubungkan perangkat dengan USB debugging, lalu Build And Run.
- Setting terpusat di `Editor/ProjectSetup.cs`: portrait, ARM64, IL2CPP, minimum API 26, target API 36, package ID `com.macetstudio.macet`. API 36 harus terpasang di SDK yang dipilih Unity. Jika Google Play mengubah requirement, ubah satu konfigurasi tersebut.
- Target 60 FPS, MSAA 2×, HDR/depth/opaque texture dimatikan, tanpa realtime shadows/post-processing. Target performa belum diukur pada perangkat.

Batch build dari root proyek, dengan Editor berlisensi dan toolchain terpasang:

```sh
Unity -batchmode -quit -projectPath "$PWD" -buildTarget Android -executeMethod Macet.Editor.ProjectSetup.BuildApk -logFile Builds/android-build.log
```

Ganti `Unity` dengan path executable Editor dan buat folder `Builds` sebelum memakai log path tersebut.

## Verifikasi

Window → General → Test Runner:

- EditMode: validasi 3 ScriptableObjects, rute, difficulty NORMAL, jumlah kendaraan, dan referensi node rusak.
- PlayMode: urutan crossing aman → complete; crossing bersamaan → fail; rear car menabrak waiting front car; pause; sweep kendaraan cepat; semua intended solutions dari aset level → complete.

Batch test (buat folder `TestResults` terlebih dahulu):

```sh
Unity -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testResults TestResults/edit.xml -logFile TestResults/edit.log
Unity -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults TestResults/play.xml -logFile TestResults/play.log
```

Offline integrity check (Python 3 + PyYAML):

```sh
python3 Tools/validate_project.py
```

Pemeriksaan Python memvalidasi data, GUID, dan clearance geometris intended solution terhadap mobil yang masih menunggu. Pemeriksaan ini **tidak menggantikan** Unity compilation/Physics tests.

Manual Android acceptance: launch → menu → level 1 → complete → next → level 2 → tap A dan C bersamaan → fail → restart → complete → level 3. Periksa touch, pause/resume tanpa gerakan, tombol HUD tidak men-tap mobil di belakangnya, safe area/notch, portrait, airplane mode, dan FPS. Acceptance save/progression dari PRD masuk Phase 2, belum diimplementasikan.

## Arsitektur

- `Core/GameManager`: high-level Loading/Playing/Paused/Failed/Completed; orchestrasi UI dan level.
- `Path/Route`: node dan route; tanpa NavMesh.
- `Traffic/TrafficEntity`, `Vehicle`: identitas, state, bounds, waypoint dan MoveTowards.
- `Traffic/TrafficManager`: satu FixedUpdate untuk semua kendaraan, remaining count dan event.
- `Traffic/CollisionManager`: non-alloc overlap + BoxCast; collider primitive dengan kinematic Rigidbody. Movement tidak memakai vehicle physics/WheelCollider.
- `Traffic/TapInput`: Input System touch, mouse fallback, UI raycast guard.
- `Level/LevelData`: ScriptableObject yang sudah tersimpan di repo; node, exit, roads, spawn, route, solution dan metadata.
- `Level/LevelLoader`: instansiasi per level/restart; framing fixed isometric camera.
- `UI/UIManager`: TextMeshPro HUD, menu, pause dan results dengan safe area.
- `Editor/ProjectSetup`: bootstrap aset URP/scene/prefab, player configuration, APK/AAB entry points.

Kendaraan hanya dibuat saat level dimuat. Tidak ada pooling pada Phase 1; tidak ada instantiate/destroy atau pencarian objek global pada movement setiap frame. Collision checks serial pada fixed timestep, tanpa solver realistik. Uji lagi saat menambah rute belok rapat, kendaraan lebih besar, atau jumlah entity meningkat.

## Scope berikutnya

Phase 2: progression/local save, level select dengan unlock, 10 easy levels. Motorcycle, angkot, pedestrian, obstacle, dedicated level editor dan 30 polished levels belum masuk Phase 1. Aset Phase 1 sudah dapat diedit via Inspector ScriptableObject tanpa mengubah scene logic.
