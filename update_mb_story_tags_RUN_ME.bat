@echo off
pushd "%DROPBOX_TEAM%\Documents\Language\Chinese\mandarin blueprint"
rem python update_mb_story_tags.py %*
python "scripts\update_mb_story_tags.py" mbP5L33
popd
pause