#!/usr/bin/env python3

from __future__ import annotations

import shutil
import struct
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ASSETS_DIR = ROOT / "assets"
SOURCE_IMAGE = ASSETS_DIR / "image.png"
ICONSET_DIR = ASSETS_DIR / "app.iconset"


def run_sips_resize(source: Path, destination: Path, size: int) -> None:
    subprocess.run(
        [
            "sips",
            "-z",
            str(size),
            str(size),
            str(source),
            "--out",
            str(destination),
        ],
        check=True,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )


def create_ico(path: Path, png_files: list[Path]) -> None:
    entries: list[bytes] = []
    image_data: list[bytes] = []
    offset = 6 + 16 * len(png_files)

    for png_file in png_files:
        data = png_file.read_bytes()
        size = read_png_size(data)
        width = 0 if size >= 256 else size
        height = 0 if size >= 256 else size
        entries.append(
            struct.pack(
                "<BBBBHHII",
                width,
                height,
                0,
                0,
                1,
                32,
                len(data),
                offset,
            )
        )
        image_data.append(data)
        offset += len(data)

    path.write_bytes(struct.pack("<HHH", 0, 1, len(png_files)) + b"".join(entries) + b"".join(image_data))


def read_png_size(data: bytes) -> int:
    width = struct.unpack(">I", data[16:20])[0]
    height = struct.unpack(">I", data[20:24])[0]
    if width != height:
        raise ValueError("Expected a square PNG.")
    return width


def generate_assets() -> None:
    if not SOURCE_IMAGE.exists():
        raise FileNotFoundError(f"Missing source image: {SOURCE_IMAGE}")

    ASSETS_DIR.mkdir(exist_ok=True)
    if ICONSET_DIR.exists():
        shutil.rmtree(ICONSET_DIR)
    ICONSET_DIR.mkdir()

    outputs = [
        (1024, ASSETS_DIR / "logo.png"),
        (512, ASSETS_DIR / "app.png"),
        (16, ICONSET_DIR / "icon_16x16.png"),
        (32, ICONSET_DIR / "icon_16x16@2x.png"),
        (32, ICONSET_DIR / "icon_32x32.png"),
        (64, ICONSET_DIR / "icon_32x32@2x.png"),
        (128, ICONSET_DIR / "icon_128x128.png"),
        (256, ICONSET_DIR / "icon_128x128@2x.png"),
        (256, ICONSET_DIR / "icon_256x256.png"),
        (512, ICONSET_DIR / "icon_256x256@2x.png"),
        (512, ICONSET_DIR / "icon_512x512.png"),
        (1024, ICONSET_DIR / "icon_512x512@2x.png"),
    ]

    for size, output in outputs:
        run_sips_resize(SOURCE_IMAGE, output, size)

    create_ico(
        ASSETS_DIR / "app.ico",
        [
            ICONSET_DIR / "icon_16x16.png",
            ICONSET_DIR / "icon_32x32.png",
            ICONSET_DIR / "icon_32x32@2x.png",
            ICONSET_DIR / "icon_128x128.png",
            ICONSET_DIR / "icon_256x256.png",
            ICONSET_DIR / "icon_512x512.png",
        ],
    )

    iconutil = shutil.which("iconutil")
    if iconutil is not None:
        try:
            subprocess.run(
                [iconutil, "-c", "icns", str(ICONSET_DIR), "-o", str(ASSETS_DIR / "app.icns")],
                check=True,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )
        except subprocess.CalledProcessError:
            pass


if __name__ == "__main__":
    generate_assets()
