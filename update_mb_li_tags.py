import os
import re
import msvcrt
from mutagen.easyid3 import EasyID3
from mutagen.id3 import ID3, COMM
import argparse

ARTIST_NAME = "Mandarin Blueprint"
GENRE = "Language Learning"
COMMENT_TEXT = "Language Islands"

# Ensure files can be tagged if no ID3 tag exists
def ensure_id3(filename):
    try:
        EasyID3(filename)
    except Exception:
        audio = ID3()
        audio.save(filename)

def clean_title(parts):
    return " ".join(parts).replace("_", " ")

def set_audio_tags(file, full_path):
    ensure_id3(full_path)
    base = os.path.splitext(file)[0]

    # Extract metadata from filename
    # Example: SAI-Intro-Part1-JanVanDerWatt
    # Try match with Part number
    match = re.match(r"SAI-(.+?)-Part(\d+)-(.*)", base)
    if match:
        raw_title, part_num, person = match.groups()
    else:
        # Fallback match without Part number, default to Part 1
        match = re.match(r"SAI-(.+?)-(.+?)", base)
        if match:
            raw_title, person = match.groups()
            part_num = "1"
        else:
            print(f"Skipping {file}: format not matched")
            return

    # print(f" - Title: {clean_title(raw_title.split('_'))}")
    # print(f" - Disc Subtitle: {person}")
    # print(f" - Disc Number: {part_num}")

    expected_tags = {
        'title': clean_title(raw_title.split('_')) + f" - part {part_num}",
        'discsubtitle': person,
        'discnumber': "",
        'tracknumber': part_num,
        'album': "LI IMMERSION",
        'artist': ARTIST_NAME,
        'genre': GENRE
    }

    audio = EasyID3(full_path)
    modified = False

    for key, value in expected_tags.items():
        current_value = audio.get(key, [None])[0]
        if current_value != value:
            audio[key] = value
            modified = True

    if modified:
        audio.save()

    # Add comment using full ID3
    full_tags = ID3(full_path)
    current_comm = full_tags.getall("COMM")
    comment_needed = True
    for comm in current_comm:
        if comm.lang == 'eng' and comm.desc == '' and comm.text == [COMMENT_TEXT]:
            comment_needed = False
            break
        
    if comment_needed:
        full_tags.delall("COMM")
        full_tags.add(COMM(encoding=3, lang='eng', desc='', text=COMMENT_TEXT))
        full_tags.save(v2_version=3)
        modified = True

    if modified:
        print(f"--> UPDATED [{file}]")
    else:
        print(f"-- No change [{file}]")


def find_files_with_extension(path, extension):
    """Find all files with the given extension in the current directory and subdirectories."""
    print(f"<-| {path} | ->")  # current directory path

    for root, dirs, files in os.walk(path):
        print(f"-| {root} | -")  # current directory path
        if ".git" in root:
            continue
        
        print(f"-| {root} | -")  # current directory path
        for file in files:
            if file.lower().endswith(extension):
                full_path = os.path.join(root, file)
                set_audio_tags(file, full_path)

    print("Done.")

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("scan_dir", help="folder to scan for MP3 files", nargs="?", default='.', type=str)
    args = p.parse_args()

    print(f"Scanning directory [{args.scan_dir}] for MP3 files...")
    find_files_with_extension(args.scan_dir, '.mp3')

    # print("Press any key to exit...")
    # msvcrt.getch()