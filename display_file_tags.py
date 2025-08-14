import os
from mutagen.id3 import ID3, ID3NoHeaderError
from mutagen.easyid3 import EasyID3
from mutagen import File
from mutagen._constants import GENRES
from collections import defaultdict
import argparse

def get_all_tags(filepath):
    """Get all available tags from an audio file"""
    try:
        # Get EasyID3 tags (simplified interface)
        easy_tags = EasyID3(filepath)
    except:
        easy_tags = None
    
    try:
        # Get raw ID3 frames
        id3_tags = ID3(filepath)
    except ID3NoHeaderError:
        id3_tags = None
    
    return easy_tags, id3_tags

def display_tag_info(filepath):
    """Display complete tag information for a file"""
    print(f"\n=== Analyzing: {os.path.basename(filepath)} ===")
    
    # Get both types of tags
    easy_tags, id3_tags = get_all_tags(filepath)
    
    # Display EasyID3 tags if available
    if easy_tags:
        print("\n[Standard Tags (EasyID3)]")
        for key, value in easy_tags.items():
            print(f"{key.upper():<15}: {value}")
    
    # Display raw ID3 frames if available
    if id3_tags:
        print("\n[Raw ID3 Frames]")
        for frame in id3_tags.values():
            frame_id = frame.FrameID
            # Get text value safely
            raw_text = getattr(frame, 'text', str(frame))
            if isinstance(raw_text, list):
                text = str(raw_text[0]) if raw_text else ''
            else:
                text = str(raw_text)
                        
            print(f"{frame_id:<5} ({frame.__class__.__name__}): {text[:80]}{'...' if len(str(text)) > 80 else ''}")
    
    # Display file extensions and technical info
    print("\n[Technical Information]")
    audio = File(filepath)
    if audio:
        print(f"File Type: {audio.mime[0]}")
        print(f"Length: {audio.info.length:.2f} seconds")
        print(f"Bitrate: {audio.info.bitrate} kbps")
        if hasattr(audio.info, 'sample_rate'):
            print(f"Sample Rate: {audio.info.sample_rate} Hz")
        if hasattr(audio.info, 'channels'):
            print(f"Channels: {audio.info.channels}")

def scan_directory(directory):
    """Scan a directory for MP3 files and analyze their tags"""
    supported_extensions = ('.mp3', '.mp2', '.mp1')
    mp3_files = []
    
    for root, _, files in os.walk(directory):
        for file in files:
            if file.lower().endswith(supported_extensions):
                mp3_files.append(os.path.join(root, file))
    
    print(f"Found {len(mp3_files)} MP3 files in directory")
    return mp3_files

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("scan_dir", help="folder to scan for MP3 files", nargs="?", default='.', type=str)
    args = p.parse_args()

    files = scan_directory(args.scan_dir)
    for file in files:
        display_tag_info(file)
