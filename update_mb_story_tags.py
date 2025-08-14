# /// script
# requires-python = ">=3.8"
# dependencies = [
#     "mutagen",
# ]
# ///

import os
import re
# import msvcrt
import json
import sys
from mutagen.easyid3 import EasyID3
from mutagen.id3 import ID3, COMM
from mutagen.mp4 import MP4
import argparse

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
    print(f"Scanning [{path}]\\ for [{extension}] files...")
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

def clean_title(parts):
    return " ".join(parts).replace("_", " ")

def set_audio_tags(file, full_path, title):
    ensure_id3(full_path)

    expected_tags = {
        'title': title,
        'album': mbPL,
        'artist': "Mandarin Blueprint",
        'genre': "Language Learning"
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
        if comm.lang == 'eng' and comm.desc == '' and comm.text == ['The Mandarin Blueprint Method']:
            comment_needed = False
            break

    if comment_needed:
        full_tags.delall("COMM")
        full_tags.add(COMM(encoding=3, lang='eng', desc='', text='The Mandarin Blueprint Method'))
        full_tags.save()

    if modified:
        print(f">> === Updated [{file}]")
        file_processed(full_path, "<== updated")
    else:
        print(f">> -- No change [{file}]")
        file_processed(full_path, "<-- no change")
        
def set_video_tags(file, full_path, title):
    video = MP4(full_path)
    expected_tags = {
        '\xa9nam': title,  # Title
        '\xa9alb': mbPL,  # Album
        '\xa9ART': "Mandarin Blueprint",  # Artist
        '\xa9gen': "Language Learning",  # Genre
        '\xa9cmt': "The Mandarin Blueprint Method"  # Comment
    }

    modified = False

    for key, value in expected_tags.items():
        current_value = video.get(key, [None])[0]
        if current_value != value:
            video[key] = value
            modified = True

    if modified:
        video.save()
        
    if modified:
        print(f">> === Updated [{file}]")
        file_processed(full_path, "<== updated")
    else:
        print(f">> -- No change [{file}]")
        file_processed(full_path, "<-- no change")
        
        
def load_and_validate(path):
    with open(path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # Check top-level keys
    if not all(k in data for k in ("title", "text")):
        raise ValueError("Missing 'title' or 'text' keys")

    for section in ("title", "text"):
        item = data[section]

        # Each section must be a dict with 'Chinese' and 'English'
        if not isinstance(item, dict):
            raise ValueError(f"{section} must be a dict")
        if "Chinese" not in item or "English" not in item:
            raise ValueError(f"{section} missing keys")

        if not isinstance(item["Chinese"], str):
            raise ValueError(f"{section}.Chinese must be a string")

        # Normalize English to list of strings
        eng = item["English"]
        if isinstance(eng, str):
            item["English"] = [eng]
        elif isinstance(eng, list):
            if not all(isinstance(s, str) for s in eng):
                raise ValueError(f"{section}.English must be a string or list of strings")
        else:
            raise ValueError(f"{section}.English must be string or list of strings")

    return data

def check_audio_filename_pattern(file, full_path, content):
    add_file(full_path)
    
    # Remove the file extension from the filename
    base = os.path.splitext(file)[0]

    for title in content["title"]["English"]:
        print(f"- English title: [{title}]")
        """Check if the audio filename matches the expected pattern ."""
        # Example: AUDIO - <title> - [Paragraph #x] - EN<gender> (EN<speed>).mp3
        pat = rf"AUDIO - {title} - (Male|Female) \((Slower|Native Speed)\)"
        match = re.match(pat, base)
        if match:
            gender, speed = match.groups()
            if gender.lower() == "female":
                cn_gender = "女"
            else:
                cn_gender = "男"
            if speed.lower() == "slower":
                cn_speed = "慢话"
            else:
                cn_speed = "对话"
            # print(f"MATCH: gender=[{gender},{cn_gender}] speed=[{speed},{cn_speed}]")
            cn_title = f"{content['title']['Chinese']} - {cn_gender}{cn_speed} ({speed})"
            print(f"MATCH: title=[{cn_title}]")
            set_audio_tags(file, full_path, cn_title)

        else:
            print(f"Skipping [{base}]:")
            print(f"#### --- format [{pat}] not matched")

def find_audio_files(full_path, content):
    print(">> ------- looking for AUDIO files -------")
    print(f"Chinese title: [{content['title']['Chinese']}]")
    for title in content["title"]["English"]:
        print(f"English title: [{title}]")
    print(">> ------- looking for AUDIO files -------")
    find_files_with_extension(full_path, '.mp3', check_audio_filename_pattern, content)
    
def check_video_filename_pattern(file, full_path, content):
    add_file(full_path)
    
    # Remove the file extension from the filename
    base = os.path.splitext(file)[0]

    for title in content["title"]["English"]:
        print(f"English title: [{title}]")
        """Check if the audio filename matches the expected pattern ."""
        # Example: AUDIO - <title> - [Paragraph #x] - EN<gender> (EN<speed>).mp3
        pat = rf"VIDEO (MALE|FEMALE) - {title}"
        match = re.match(pat, base)
        if match:
            gender = match.groups()[0]
            print(f"gender = {gender}")
            if gender.lower() == "female":
                cn_gender = "女"
            else:
                cn_gender = "男"
            # print(f"MATCH: gender=[{gender},{cn_gender}] speed=[{speed},{cn_speed}]")
            cn_title = f"{content['title']['Chinese']} - {cn_gender}"
            print(f"MATCH: title=[{cn_title}]")
            set_video_tags(file, full_path, cn_title)
        else:
            print(f"Skipping [{base}]:")
            print(f"#### --- format [{pat}] not matched")
        
def find_video_files(full_path, content):
    print(">> ------- looking for VIDEO files -------")
    print(f"Chinese title: [{content['title']['Chinese']}]")
    for title in content["title"]["English"]:
        print(f"English title: [{title}]")
    print(">> ------- looking for VIDEO files -------")
    find_files_with_extension(full_path, '.mp4', check_video_filename_pattern, content)

def find_mp_PandL(full_path):
    full_path = os.path.abspath(full_path)
    # check if the path contains a valid Mandarin Blueprint Phase and Level
    pat = r".*mandarin blueprint.(mbP\d{1,2}L\d{1,3})"
    match = re.match(pat, full_path)
    if match:
        return match.groups()[0]
    return None
    
def process_JSON_file(file, full_path, p):
    global mbPL
    # check if the path contains a valid Mandarin Blueprint Phase and Level
    mbPL = find_mp_PandL(full_path)
    if not mbPL:
        return
    print(f"Found mb P and L: [{mbPL}]")
    
    # Remove the file extension from the filename
    base = os.path.splitext(file)[0]

    # Extract metadata from filename
    # Example: TITLE INFO - Sleeping Beauty - Paragraph 1
    # Try match with Paragraph number
    match = re.match(r"TITLE INFO - (.*) - Paragraph (\d+)", base)
    if match:
        raw_title, parag_num = match.groups()
    else:
        # Fallback match without Part number, default to Part 1
        match = re.match(r"TITLE INFO - (.*)", base)
        if match:
            raw_title = match.groups()[0]
            parag_num  = "0"
        else:
            print(f"#### ---- Skipping [{file}]: format not matched")
            return

    """Process JSON files to extract and print relevant information."""
    print("------- found JSON file -------")
    if (parag_num == "0"):
        print(f"-- Title: [{raw_title}] --")
    else:
        print(f"-- Title: [{raw_title}], Paragraph: [{parag_num}] --")
    
    try:
        content = load_and_validate(full_path)
        if not content is None:
            find_audio_files(os.path.dirname(full_path), content)
            find_video_files(os.path.dirname(full_path), content)
    
    except json.JSONDecodeError as e:
        print(f"#### ---- Error decoding JSON in file [{file}]: error [{e}]")
        
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

    find_files_with_extension(args.scan_dir, '.json', process_JSON_file)

    for fname, status in file_status.items():
        print(f"{fname}: {status}")
        
    #print("Press any key to exit...")
    #msvcrt.getch()