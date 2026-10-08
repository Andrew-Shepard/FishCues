"""Build the Thunderstore package for FishCues.

    python3 package.py            # writes ..\\releases\\FishCues-<version>-thunderstore.zip

Layout is verified against Advize/PlantEverything and Thunderstore's own validator:
icon.png, manifest.json, README.md and the DLL all sit at the ZIP ROOT - no folders.
The icon must be exactly 256x256 PNG (validation/icon.py rejects anything else), and
manifest.json needs exactly name, version_number, description, website_url, dependencies
(package_manifest.py:ManifestV1Serializer). network_mode and mod_loaders are NOT fields.
"""
import hashlib
import json
import os
import re
import struct
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.abspath(os.path.join(HERE, "..", "releases"))


def png_size(path):
    with open(path, "rb") as fh:
        head = fh.read(24)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        raise SystemExit(f"{path}: not a PNG, Thunderstore requires icon.png to be PNG")
    return struct.unpack(">II", head[16:24])


def main():
    manifest_path = os.path.join(HERE, "package", "manifest.json")
    icon_path = os.path.join(HERE, "package", "icon.png")
    readme_path = os.path.join(HERE, "README.md")
    dll_path = os.path.join(HERE, "bin", "Release", "net472", "FishCues.dll")

    for p in (manifest_path, icon_path, readme_path, dll_path):
        if not os.path.isfile(p):
            raise SystemExit(f"missing {p}")

    manifest = json.load(open(manifest_path, encoding="utf-8"))
    required = {"name", "version_number", "description", "website_url", "dependencies"}
    missing = required - set(manifest)
    if missing:
        raise SystemExit(f"manifest.json missing fields: {missing}")
    if len(manifest["description"]) >= 256:
        raise SystemExit(f"description is {len(manifest['description'])} chars, keep it under 256")

    version = manifest["version_number"]

    # The version string lives in three files and nothing syncs them, so a 0.1.0-labelled 0.1.1
    # is the easiest way to ship a confusing update. Fail loudly instead.
    plugin_cs = open(os.path.join(HERE, "Plugin.cs"), encoding="utf-8").read()
    csproj = open(os.path.join(HERE, "FishCues.csproj"), encoding="utf-8").read()
    for label, m in (("Plugin.cs PluginVersion", re.search(r'PluginVersion\s*=\s*"([^"]+)"', plugin_cs)),
                     ("csproj <Version>", re.search(r"<Version>([^<]+)</Version>", csproj))):
        got = m.group(1) if m else "missing"
        if got != version:
            raise SystemExit(f"{label} is {got!r} but manifest version_number is {version!r} - bump all three")

    w, h = png_size(icon_path)
    if (w, h) != (256, 256):
        raise SystemExit(f"icon.png is {w}x{h}, Thunderstore requires exactly 256x256")
    if os.path.getsize(icon_path) > 6 * 1024 * 1024:
        raise SystemExit("icon.png over 6 MB")

    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, f"FishCues-{version}-thunderstore.zip")
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        z.write(icon_path, "icon.png")
        z.write(manifest_path, "manifest.json")
        z.write(readme_path, "README.md")
        z.write(dll_path, "FishCues.dll")

    with zipfile.ZipFile(out) as z:
        names = z.namelist()
        bad = [n for n in names if n != os.path.basename(n) or n.startswith("/") or ".." in n]
        if bad:
            raise SystemExit(f"unsafe or nested paths in zip: {bad}")
        if sorted(names) != sorted(["icon.png", "manifest.json", "README.md", "FishCues.dll"]):
            raise SystemExit(f"unexpected zip contents: {names}")
        dll = z.read("FishCues.dll")
        want = open(dll_path, "rb").read()
        if dll != want:
            raise SystemExit("DLL inside the zip does not match bin/Release")

    print(f"built {out}")
    print(f"  {os.path.getsize(out)} bytes")
    print(f"  contents: {', '.join(names)}")
    print(f"  icon.png  {w}x{h}")
    print(f"  dll sha256 {hashlib.sha256(dll).hexdigest()[:12]}  (the Release build)")
    print(f"  manifest  {json.dumps(manifest['dependencies'])}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
