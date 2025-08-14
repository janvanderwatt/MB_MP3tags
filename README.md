# Mandarin Blueprint Audio/Video File Tag Update Scripts

## 1. Why?

MP3 and MP4 files can have metadata tags that are used by
various media players to display information about the file,
such as title, artist, album, and more.

You can use these tags to organize and search for your media files more effectively.

Bu the **best use** of the tags is to create smart playlists in Apple Music that bundle
the audio/video files into playlists based on the tags.

## 2. What?

This project provides scripts to update the tags for audio and video files of the Mandarin Blueprint course.

## 3. MP3 (Audio File) Tags

This project maps the audio files of the Mandarin Blueprint course to tags as follows:

### Tag Summary

| Tag Name | Phrase Vault | MSLK | MB Stories | MB Combined Sentences | 
|----------|-------|-------|-------|-------|
| `title` | *Subject (without the \_ of the filename)* | MSLK Lesson *lesson number* | *Chinese story title* + *classifier* + *Chinese speaker gender & speed* + (*English speed*) <br> e.g. "耐心 Paragraph #5 - 男慢话 (Slower)"| L\<level\> All Sentences Combined |
| `album` | *LLR, TAP or IMMERSION* | *LLR, TAP or IMMERSION* | mbP\<phase\>L\<level\>, eg. "mbP5L32" | mbP\<phase\>L\<level\> |
| `discsubtitle` | Part *part number* | Part *lesson number* | \<empty\> | \<empty\> |
| `discnumber` | *part number* | *lesson number* | \<empty\> | \<empty\> |
| `comment` | The Phrase Vault | Mandarin Speaking & Listening Kickstarter |  The Mandarin Blueprint Method | The Mandarin Blueprint Method |
| `artist` | Mandarin Blueprint | Mandarin Blueprint | Mandarin Blueprint | Mandarin Blueprint |
| `genre` | Language Learning | Language Learning | Language Learning | Language Learning |

Other fields that could still be useful or populated are: `discsubtitle`, `date`, `albumartist`.

Use the following script to list all the "friendly aliases" available:

    list_possible_mp3_tags.py

### 3.1 The Phrase Vault

| Tag Name | Value |
|----------|-------| 
| `title` | *Subject (without the \_ of the filename)* |
| `album` | *LLR, TAP or IMMERSION* |
| `discsubtitle` | Part *part number* |
| `discnumber` | *part number* |
| `comment` | The Phrase Vault |

This information is derived from filename, e.g.:

    Leisure_Hobbies_Part_1_TAP_MANDARIN_BLUEPRINT.mp3

becomes `title`: "Leisure Hobbies", `album`: "TAP", `discsubtitle`: "Part 1", `discnumber`: "1".

### 3.2 Mandarin Speaking & Listening Kickstarter

| Tag Name | Value |
|----------|-------| 
| `title` | MSLK Lesson *lesson number* |
| `album` | *LLR, TAP or IMMERSION* |
| `discsubtitle` | Part *lesson number* |
| `discnumber` | *lesson number* |
| `comment` | Mandarin Speaking & Listening Kickstarter |

This information is derived from filename, e.g.:

    MSLK_Lesson_05_IMMERSION_MANDARIN_BLUEPRINT.mp3

becomes `title`: "MSLK Lesson 05", `album`: "IMMERSION", `discsubtitle`: "Part 05", `discnumber`: "05".

### 3.3 Stories (Mandarin Blueprint)

| Tag Name | Value |
|----------|-------| 
| `title` | *Chinese story title* + [ optional: *classifier* + ] <br> *Chinese speaker gender & speed* + (*English speed*) <br> e.g. "学习, 工作和生活 - 男对话 (Native Speed)", <br> or "三只小猪 - Full Story 90% - 女慢话 (Slower)" |
| `album` | mbP\<phase\>L\<level\> |
| `comment` | The Mandarin Blueprint Method |

I've organised my Mandarin Blueprint story audio files into folders named by phase and level, e.g.:

    ..\mandarin blueprint\mbP5L36\mbP5L36HonestyP1\

and added a text file that describes the file names and other information, e.g. :

    TITLE INFO - Honesty - Paragraph 1.json

contains

    {
        "title": {
            "Chinese": "诚实 - Paragraph #1",
            "English": [
                "Honesty - Paragraph #1",
                "Honesty - Paragraph 1"
            ]
        },
        "text": {
            "Chinese": "很久以前，有一个男人...",
            "English": "A long time ago, there was a man that said: \"I am a hunter\"..."
        }
    }

When you want to put a quotation mark around speech, in **JSON FILES** you need use `\"`.

**IMPORTANT:** The script **relies** on this folder structure and naming convention to set the tags correctly.

### 3.4 All Sentences Combined (Mandarin Blueprint)

| Tag Name | Value |
|----------|-------| 
| `title` | L\<level\> All Sentences Combined |
| `album` | mbP\<phase\>L\<level\> |
| `comment` | The Mandarin Blueprint Method |

I've organised my Mandarin Blueprint audio files into folders named by phase and level, e.g.:

    ..\mandarin blueprint\mbP4L24\

and renamed my audio files that contain all the sentences of a level using this format:

    L24 All Sentences Combined.mp3

**IMPORTANT:** The script **relies** on this folder structure and naming convention to set the tags correctly.

### 3.5 General
Some tags are applied to all audio files:

| Tag Name | Value |
|----------|-------| 
| `artist` | Mandarin Blueprint |
| `genre` | Language Learning |


## MP4 (Video File) Tags

This project provides scripts to update the tags for audio and video files of the Mandarin Blueprint course.

## Smart Playlists

In Apple Music (for Windows), you can create a smart playlist with rules like:
* **Title** contains "Full Story", and
* **Media Kind** is "Music"

which will create a playlist of all audio files that have "Full Story" in the title, ignoring all other music files.

**Important:** Selecting the **Live updating** option will ensure that the playlist is updated automatically when you add new files that match the rules.

    [x] Live updating

## Prerequisites

* You must have a working knowledge of Python scripts and how to run them.
* You should also be familiar with how to install Python packages.


These scripts require Python 3.x and the `mutagen` library to be installed.
Depending on your system, you can install it using:

    pip install mutagen
    python3 -m pip install mutagen
    py -3 -m pip install mutagen


