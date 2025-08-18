import os
import re
# import msvcrt
import json
import sys
from mutagen.easyid3 import EasyID3
from mutagen.id3 import ID3, COMM
import argparse

ARTIST_NAME = "Mandarin Blueprint"
GENRE = "Language Learning"
COMMENT_TEXT = "The Mandarin Blueprint Method"

current_dir = ""
mbPL = None  # Global variable to store the current mbP and L
file_status = {}
filter = None  # Global variable to store the filter, if any

def add_file(full_path):
    if full_path not in file_status:
        file_status[full_path] = "<== UNMATCHED"
    
def file_processed(full_path, status):
    file_status[full_path] = status
        
def find_files_with_extension(path, extension, callback, p = None):
    """Find all files with the given extension in the current directory and subdirectories."""
    global current_dir
    print(f"Scanning {path}\\ for [{extension}] files...")
    for root, dirs, files in os.walk(path):
        if not current_dir == root:
            if ".git" in root:
                continue  # Skip git directories
            
            print("==============================================================================================")
            print(f"--| {root} |--")  # current directory path
            print("----------------------------------------------------------------------------------------------")
            current_dir = root

            # check if there is a filter and whether the file matches it
            if filter:
                match = re.search(filter, root, re.IGNORECASE) 
                if not match:
                    print(f"Skipping [{root}]: does not match filter [{filter}]")
                    continue

        for file in files:
            if file.lower().endswith(extension):
                full_path = os.path.join(root, file)

                print(f"Processing file: {full_path}")
                callback(file, full_path, p)

# Ensure files can be tagged if no ID3 tag exists
def ensure_id3(filename):
    try:
        EasyID3(filename)
    except Exception:
        audio = ID3()
        audio.save(filename)

def set_audio_tags(file, full_path, title):
    ensure_id3(full_path)

    expected_tags = {
        'title': title,
        'album': mbPL,
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
        full_tags.save()

    if modified:
        print(f">> === Updated [{file}]")
        file_processed(full_path, "<== updated")
    else:
        print(f">> -- No change [{file}]")
        file_processed(full_path, "<-- no change")
        
def check_audio_filename_pattern(file, full_path, param):
    global mbPL
    mbPL, mbP, mbL = find_mp_PandL(full_path)
    if mbPL is None:
        return
    
    # Remove the file extension from the filename
    base = os.path.splitext(file)[0]
    
    # Only bother with files containing "All Sentences Combined"
    if "All Sentences Combined" not in base:
        return

    add_file(full_path)

    # Example: L24 All Sentences Combined.mp3
    pat = rf"L{mbL} All Sentences Combined"
    match = re.match(pat, base)
    if match:
        print(f"MATCH: title=[{pat}]")
        set_audio_tags(file, full_path, pat)

    else:
        print(f"Skipping [{base}]:")
        print(f"#### --- format [{pat}] not matched")

def find_mp_PandL(full_path):
    full_path = os.path.abspath(full_path)
    # check if the path contains a valid Mandarin Blueprint Phase and Level
    pat = r".*mandarin blueprint.(mbP(\d{1,2})L(\d{1,3}))"
    match = re.match(pat, full_path)
    if match:
        return match.groups()[0], match.groups()[1], match.groups()[2]
    return None, None, None
      
      
if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("scan_dir", help="folder to scan for MP3 files", nargs="?", default='.', type=str)
    p.add_argument("--filter", help="regex file filter", required=False, default='.*', type=str)
    args = p.parse_args()

    print(f"Scanning directory [{args.scan_dir}] for MP3 files...")
          
    filter = args.filter
    try:
        compiled_filter = re.compile(filter)
    except re.error as e:
        print(f"#### ---- Invalid filter regex: [{filter}]: error [{e}]")
        sys.exit(1)
        
    print(f"File filter: [{filter}]")
            
    print(">> ------- looking for SENTENCE files -------")
    find_files_with_extension(args.scan_dir, '.mp3', check_audio_filename_pattern)

    for fname, status in file_status.items():
        print(f"{fname}: {status}")
        
    #print("Press any key to exit...")
    #msvcrt.getch()