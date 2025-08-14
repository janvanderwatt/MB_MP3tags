@echo off
pushd "%DROPBOX_TEAM%\Documents\Language\Chinese\mandarin blueprint"
python "scripts\update_mb_sentence_tags.py"
popd
pause